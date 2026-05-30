#include <Windows.h>
#include <Ole2.h>
#include <NuiApi.h>
#include <iostream>
#include <vector>
#include <algorithm>
#include <opencv2/opencv.hpp>

using namespace std;
using namespace cv;

// ==========================================
// MACRO PARA EXPORTAR FUNÇÕES PARA A UNITY
// ==========================================
#define EXPORT_API extern "C" __declspec(dllexport)

// ==========================================
// ESTRUTURA DE MEMÓRIA (TRACKING VTT PRO)
// ==========================================
struct TrackedPiece {
    int id = -1;
    Point position = Point(0, 0);
    Point2f filteredPosition = Point2f(0, 0);
    Point2f velocity = Point2f(0, 0);
    double lastArea = 0.0;
    int framesMissing = 0;
    int framesVisible = 0;
    int framesOccluded = 0;
    bool isConfirmed = false;
    bool initialized = false;
};

vector<TrackedPiece> activePieces;
vector<TrackedPiece> exportPieces; // Lista filtrada só com as peças válidas para a Unity
vector<Point> projectionROI;
bool hasProjectionROI = false;

int getAvailableId() {
    int id = 1;
    while (true) {
        bool used = false;
        for (const auto& p : activePieces) {
            if (p.id == id) { used = true; break; }
        }
        if (!used) return id;
        id++;
    }
}

struct Match {
    int pieceIdx;
    int centroidIdx;
    double dist;
};

struct PieceCandidate {
    Point centroid = Point(0, 0);
    double area = 0.0;
    double circularity = 0.0;
    Rect bbox;
    bool nearHand = false;
};

// ==========================================
// VARIÁVEIS GLOBAIS
// ==========================================
bool isCalibrating = false;
int calibFrameCount = 0;
Mat bgAccumulator;
Mat bgDepth;
bool hasBackground = false;

INuiSensor* sensor = nullptr;
HANDLE depthStream;
HANDLE nextDepthFrameEvent;

const int width = 640;
const int height = 480;

const double minPieceArea = 12.0;
const double maxPieceArea = 900.0;
const double minPieceCircularity = 0.32;
const double maxPieceAspect = 2.0;
const double duplicateCentroidDistance = 18.0;
const double baseMatchDistance = 45.0;
const double reacquireMatchDistance = 125.0;
const double handReacquireDistance = 75.0;
const double smoothingAlpha = 0.45;
const int framesToConfirmPiece = 6;
const int maxOcclusionFrames = 120;
const int maxMissingFrames = 75;

bool isInsideProjectionROI(const Point& p) {
    if (!hasProjectionROI || projectionROI.size() < 4) return true;
    return pointPolygonTest(projectionROI, Point2f((float)p.x, (float)p.y), false) >= 0.0;
}

static Point toPoint(const Point2f& p) {
    return Point((int)round(p.x), (int)round(p.y));
}

static double distanceBetween(const Point& a, const Point& b) {
    return norm(a - b);
}

static void updatePiecePosition(TrackedPiece& piece, const Point& measured, double area) {
    Point2f measuredF((float)measured.x, (float)measured.y);

    if (!piece.initialized) {
        piece.filteredPosition = measuredF;
        piece.velocity = Point2f(0, 0);
        piece.initialized = true;
    }
    else {
        Point2f previous = piece.filteredPosition;
        piece.filteredPosition = previous + (measuredF - previous) * (float)smoothingAlpha;
        piece.velocity = piece.filteredPosition - previous;
    }

    piece.position = toPoint(piece.filteredPosition);
    piece.lastArea = area;
    piece.framesMissing = 0;
    piece.framesOccluded = 0;
    piece.framesVisible++;
}

static bool isPointInMask(const Mat& mask, const Point& p) {
    if (p.x < 0 || p.x >= mask.cols || p.y < 0 || p.y >= mask.rows) return false;
    return mask.at<uchar>(p.y, p.x) > 0;
}

static bool isNearTrackedPiece(const Point& point, double maxDistance) {
    for (const auto& piece : activePieces) {
        if (distanceBetween(piece.position, point) <= maxDistance) return true;
    }
    return false;
}

static void addMergedCandidate(vector<PieceCandidate>& candidates, const PieceCandidate& candidate) {
    int bestIndex = -1;
    double bestDist = duplicateCentroidDistance;

    for (int i = 0; i < (int)candidates.size(); i++) {
        double dist = distanceBetween(candidates[i].centroid, candidate.centroid);
        if (dist < bestDist) {
            bestDist = dist;
            bestIndex = i;
        }
    }

    if (bestIndex < 0) {
        candidates.push_back(candidate);
        return;
    }

    PieceCandidate& existing = candidates[bestIndex];
    double totalArea = max(1.0, existing.area + candidate.area);
    Point2f mixed =
        Point2f((float)existing.centroid.x, (float)existing.centroid.y) * (float)(existing.area / totalArea) +
        Point2f((float)candidate.centroid.x, (float)candidate.centroid.y) * (float)(candidate.area / totalArea);

    existing.centroid = toPoint(mixed);
    existing.area = max(existing.area, candidate.area);
    existing.circularity = max(existing.circularity, candidate.circularity);
    existing.bbox = existing.bbox | candidate.bbox;
    existing.nearHand = existing.nearHand || candidate.nearHand;
}

// ==========================================
// FUNÇÕES EXPORTADAS PARA A UNITY
// ==========================================

EXPORT_API bool InitTracker() {
    int numSensors = 0;
    NuiGetSensorCount(&numSensors);
    if (numSensors == 0) return false;

    if (FAILED(NuiCreateSensorByIndex(0, &sensor))) return false;
    if (FAILED(sensor->NuiInitialize(NUI_INITIALIZE_FLAG_USES_DEPTH))) return false;

    nextDepthFrameEvent = CreateEvent(NULL, TRUE, FALSE, NULL);
    if (FAILED(sensor->NuiImageStreamOpen(NUI_IMAGE_TYPE_DEPTH, NUI_IMAGE_RESOLUTION_640x480, 0, 2, nextDepthFrameEvent, &depthStream))) return false;

    return true;
}

// A Unity chama esta função quando o jogador clica no botão "Calibrar"
EXPORT_API void StartCalibration() {
    isCalibrating = true;
    calibFrameCount = 0;
    activePieces.clear();
    exportPieces.clear();
}

// A Unity chama esta função para limpar a mesa virtual
EXPORT_API void ResetTracker() {
    activePieces.clear();
    exportPieces.clear();
}

// A Unity chama depois da calibracao dos 4 cantos da area projetada.
// A ordem deve ser: TopLeft, TopRight, BottomLeft, BottomRight.
EXPORT_API void SetProjectionROI(int tlx, int tly, int trx, int try_, int blx, int bly, int brx, int bry) {
    projectionROI.clear();
    projectionROI.push_back(Point(tlx, tly));
    projectionROI.push_back(Point(trx, try_));
    projectionROI.push_back(Point(brx, bry));
    projectionROI.push_back(Point(blx, bly));
    hasProjectionROI = true;

    activePieces.clear();
    exportPieces.clear();
}

// Usado durante recalibracao para nao deixar uma ROI antiga bloquear os novos cantos.
EXPORT_API void ClearProjectionROI() {
    hasProjectionROI = false;
    projectionROI.clear();
    activePieces.clear();
    exportPieces.clear();
}

// A Unity chama esta função TODO FRAME (no método Update)
EXPORT_API void ProcessFrame() {
    if (!sensor) return;

    Mat currentDepth(height, width, CV_16UC1, Scalar(0));
    NUI_IMAGE_FRAME imageFrame;
    HRESULT hr = sensor->NuiImageStreamGetNextFrame(depthStream, 100, &imageFrame);

    if (SUCCEEDED(hr)) {
        INuiFrameTexture* texture = imageFrame.pFrameTexture;
        NUI_LOCKED_RECT lockedRect;
        texture->LockRect(0, &lockedRect, NULL, 0);

        if (lockedRect.Pitch != 0) {
            const USHORT* curr = (const USHORT*)lockedRect.pBits;

            Mat displayDepth(height, width, CV_8UC1);
            Mat maskTokens(height, width, CV_8UC1, Scalar(0));
            Mat maskHands(height, width, CV_8UC1, Scalar(0));
            Mat coreMask(height, width, CV_8UC1, Scalar(0));

            for (int i = 0; i < width * height; i++) {
                USHORT depthInMm = curr[i] >> 3;
                currentDepth.at<USHORT>(i) = depthInMm;

                if (depthInMm == 0) {
                    displayDepth.at<uchar>(i) = 0;
                }
                else {
                    float norm = (depthInMm - 600.0f) / 1000.0f;
                    if (norm < 0.0f) norm = 0.0f;
                    if (norm > 1.0f) norm = 1.0f;
                    displayDepth.at<uchar>(i) = (uchar)(255 - (norm * 255));
                }
            }

            Mat outputView;
            cvtColor(displayDepth, outputView, COLOR_GRAY2BGR);

            // Calibração
            if (isCalibrating) {
                if (calibFrameCount == 0) bgAccumulator = Mat::zeros(height, width, CV_32FC1);

                Mat floatDepth;
                currentDepth.convertTo(floatDepth, CV_32FC1);
                accumulate(floatDepth, bgAccumulator);
                calibFrameCount++;

                putText(outputView, "CALIBRANDO: " + to_string(calibFrameCount) + "/30", Point(width / 2 - 100, height / 2), FONT_HERSHEY_SIMPLEX, 0.8, Scalar(0, 0, 255), 2);

                if (calibFrameCount >= 30) {
                    bgAccumulator.convertTo(bgDepth, CV_16UC1, 1.0 / 30.0);
                    hasBackground = true;
                    isCalibrating = false;
                }
            }

            // Processamento Principal
            if (hasBackground && !isCalibrating) {
                Mat diff;
                absdiff(bgDepth, currentDepth, diff);

                for (int i = 0; i < width * height; i++) {
                    USHORT currD = currentDepth.at<USHORT>(i);
                    if (currD == 0) continue;

                    USHORT d = diff.at<USHORT>(i);
                    if (d >= 8 && d <= 100) maskTokens.at<uchar>(i) = 255;
                    else if (d > 100 && d < 300) maskHands.at<uchar>(i) = 255;
                }

                Mat handDilate = getStructuringElement(MORPH_ELLIPSE, Size(21, 21));
                dilate(maskHands, maskHands, handDilate);

                medianBlur(maskTokens, maskTokens, 3);
                Mat kernelErode = getStructuringElement(MORPH_ELLIPSE, Size(3, 3));
                erode(maskTokens, maskTokens, kernelErode);

                Mat distTransform;
                distanceTransform(maskTokens, distTransform, DIST_L2, 3);

                threshold(distTransform, coreMask, 4.0, 255, THRESH_BINARY);
                coreMask.convertTo(coreMask, CV_8UC1);

                vector<vector<Point>> contours;
                findContours(coreMask, contours, RETR_EXTERNAL, CHAIN_APPROX_SIMPLE);

                vector<PieceCandidate> currentCandidates;

                if (hasProjectionROI && projectionROI.size() >= 4) {
                    polylines(outputView, projectionROI, true, Scalar(0, 255, 255), 2);
                }

                for (size_t i = 0; i < contours.size(); i++) {
                    double area = contourArea(contours[i]);

                    if (area >= minPieceArea && area <= maxPieceArea) {
                        Rect bbox = boundingRect(contours[i]);
                        float aspect = (float)bbox.width / (float)bbox.height;
                        if (aspect < 1.0f) aspect = 1.0f / max(aspect, 0.001f);

                        double perimeter = arcLength(contours[i], true);
                        double circularity = perimeter > 0.0 ? (4.0 * CV_PI * area) / (perimeter * perimeter) : 0.0;

                        if (aspect <= maxPieceAspect && circularity >= minPieceCircularity) {
                            Moments m = moments(contours[i]);
                            if (m.m00 > 0) {
                                int cx = (int)(m.m10 / m.m00);
                                int cy = (int)(m.m01 / m.m00);
                                Point centroid(cx, cy);

                                if (!isInsideProjectionROI(centroid)) {
                                    circle(outputView, centroid, 4, Scalar(80, 80, 80), 1);
                                    continue;
                                }

                                bool nearHand = isPointInMask(maskHands, centroid);
                                bool strongPieceShape = circularity >= 0.58 && area >= minPieceArea * 1.5 && area <= maxPieceArea * 0.75;
                                if (nearHand && !isNearTrackedPiece(centroid, handReacquireDistance) && !strongPieceShape) {
                                    circle(outputView, centroid, 5, Scalar(80, 80, 255), 1);
                                    continue;
                                }

                                PieceCandidate candidate;
                                candidate.centroid = centroid;
                                candidate.area = area;
                                candidate.circularity = circularity;
                                candidate.bbox = bbox;
                                candidate.nearHand = nearHand;

                                addMergedCandidate(currentCandidates, candidate);
                                drawContours(outputView, contours, (int)i, Scalar(150, 255, 150), -1);
                            }
                        }
                    }
                }

                vector<Match> matches;
                for (int p = 0; p < (int)activePieces.size(); p++) {
                    Point predicted = activePieces[p].position + toPoint(activePieces[p].velocity);
                    for (int c = 0; c < (int)currentCandidates.size(); c++) {
                        double d = norm(predicted - currentCandidates[c].centroid);
                        matches.push_back({ p, c, d });
                    }
                }

                sort(matches.begin(), matches.end(), [](const Match& a, const Match& b) {
                    return a.dist < b.dist;
                    });

                vector<bool> pieceMatched(activePieces.size(), false);
                vector<bool> centroidMatched(currentCandidates.size(), false);

                // Passagem 1: associacao principal com gate dinamico.
                for (const auto& m : matches) {
                    if (!pieceMatched[m.pieceIdx] && !centroidMatched[m.centroidIdx]) {
                        const PieceCandidate& candidate = currentCandidates[m.centroidIdx];
                        double maxDist = (activePieces[m.pieceIdx].framesMissing > 0) ? reacquireMatchDistance : baseMatchDistance;
                        maxDist += min(45.0, norm(activePieces[m.pieceIdx].velocity) * 2.0);
                        if (candidate.nearHand) maxDist = min(maxDist, handReacquireDistance);

                        if (m.dist < maxDist) {
                            updatePiecePosition(activePieces[m.pieceIdx], candidate.centroid, candidate.area);

                            if (!activePieces[m.pieceIdx].isConfirmed && activePieces[m.pieceIdx].framesVisible >= framesToConfirmPiece) {
                                activePieces[m.pieceIdx].isConfirmed = true;
                                activePieces[m.pieceIdx].id = getAvailableId();
                            }

                            pieceMatched[m.pieceIdx] = true;
                            centroidMatched[m.centroidIdx] = true;
                        }
                    }
                }

                // Passagem 2: reacquisicao conservadora durante oclusao curta.
                for (const auto& m : matches) {
                    if (!pieceMatched[m.pieceIdx] && !centroidMatched[m.centroidIdx]) {
                        const PieceCandidate& candidate = currentCandidates[m.centroidIdx];
                        if (activePieces[m.pieceIdx].framesMissing > 0 && m.dist < reacquireMatchDistance && !candidate.nearHand) {
                            updatePiecePosition(activePieces[m.pieceIdx], candidate.centroid, candidate.area);
                            pieceMatched[m.pieceIdx] = true;
                            centroidMatched[m.centroidIdx] = true;
                        }
                    }
                }

                // Oclusão
                for (int p = 0; p < (int)activePieces.size(); p++) {
                    if (!pieceMatched[p]) {
                        if (activePieces[p].position.x >= 0 && activePieces[p].position.x < width &&
                            activePieces[p].position.y >= 0 && activePieces[p].position.y < height) {

                            if (maskHands.at<uchar>(activePieces[p].position.y, activePieces[p].position.x) == 255) {
                                activePieces[p].framesOccluded++;
                                if (activePieces[p].framesOccluded < maxOcclusionFrames) {
                                    if (activePieces[p].framesMissing > 5) activePieces[p].framesMissing = 5;
                                    circle(outputView, activePieces[p].position, 12, Scalar(200, 100, 255), 2);
                                }
                                else {
                                    activePieces[p].framesMissing++;
                                }
                            }
                            else {
                                activePieces[p].framesOccluded = 0;
                                activePieces[p].framesMissing++;
                            }
                        }
                        else {
                            activePieces[p].framesOccluded = 0;
                            activePieces[p].framesMissing++;
                        }
                    }
                }

                // Novos Fantasmas
                for (size_t c = 0; c < currentCandidates.size(); c++) {
                    if (!centroidMatched[c]) {
                        if (currentCandidates[c].nearHand && currentCandidates[c].circularity < 0.62)
                            continue;

                        TrackedPiece newPiece;
                        newPiece.id = -1;
                        newPiece.position = currentCandidates[c].centroid;
                        newPiece.filteredPosition = Point2f((float)newPiece.position.x, (float)newPiece.position.y);
                        newPiece.velocity = Point2f(0, 0);
                        newPiece.lastArea = currentCandidates[c].area;
                        newPiece.framesMissing = 0;
                        newPiece.framesVisible = 1;
                        newPiece.framesOccluded = 0;
                        newPiece.isConfirmed = false;
                        newPiece.initialized = true;
                        activePieces.push_back(newPiece);
                    }
                }

                // Faxina
                activePieces.erase(remove_if(activePieces.begin(), activePieces.end(),
                    [](const TrackedPiece& p) {
                        if (!p.isConfirmed && p.framesMissing > 0) return true;
                        return p.framesMissing > maxMissingFrames;
                    }), activePieces.end());

                // Atualiza a lista de exportação e desenha o HUD
                exportPieces.clear();
                for (const auto& p : activePieces) {
                    if (p.isConfirmed && isInsideProjectionROI(p.position)) {
                        exportPieces.push_back(p); // Salva para enviar para a Unity

                        if (p.framesMissing == 0) {
                            circle(outputView, p.position, 6, Scalar(0, 0, 255), -1);
                            putText(outputView, "ID:" + to_string(p.id), Point(p.position.x + 10, p.position.y), FONT_HERSHEY_SIMPLEX, 0.6, Scalar(255, 255, 0), 2);
                        }
                        else {
                            circle(outputView, p.position, 8, Scalar(0, 255, 255), 2);
                            putText(outputView, "ID:" + to_string(p.id) + " (LOST)", Point(p.position.x + 10, p.position.y), FONT_HERSHEY_SIMPLEX, 0.5, Scalar(0, 150, 255), 1);
                        }
                    }
                }
            }

            // Mostra as janelas de debug visual (Opcional, mas útil para ver o que a câmera está enxergando)
            imshow("VTT Kinect - Tracker Debug", outputView);
            imshow("VTT Kinect - Miolos", coreMask);
        }

        texture->UnlockRect(0);
        sensor->NuiImageStreamReleaseFrame(depthStream, &imageFrame);
    }

    // Atualiza as janelas do OpenCV (Necessário para o imshow funcionar sem congelar)
    waitKey(1);
}

// Retorna quantas peças válidas estão na mesa no momento
EXPORT_API int GetPieceCount() {
    return exportPieces.size();
}

// A Unity pede os dados de cada peça (ID, Coordenada X, Coordenada Y e Estado de Perda)
EXPORT_API void GetPieceData(int index, int& id, int& x, int& y, int& isLost) {
    if (index >= 0 && index < exportPieces.size()) {
        id = exportPieces[index].id;
        x = exportPieces[index].position.x;
        y = exportPieces[index].position.y;
        isLost = (exportPieces[index].framesMissing > 0) ? 1 : 0;
    }
}

// A Unity chama esta função quando o jogo for fechado para desligar o laser do Kinect
EXPORT_API void StopTracker() {
    if (sensor) {
        sensor->NuiShutdown();
        sensor->Release();
    }
}
