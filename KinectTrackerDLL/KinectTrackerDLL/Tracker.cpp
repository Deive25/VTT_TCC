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
    int framesMissing = 0;
    int framesVisible = 0;
    int framesOccluded = 0;
    bool isConfirmed = false;
};

vector<TrackedPiece> activePieces;
vector<TrackedPiece> exportPieces; // Lista filtrada só com as peças válidas para a Unity

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

                vector<Point> currentCentroids;

                for (size_t i = 0; i < contours.size(); i++) {
                    double area = contourArea(contours[i]);

                    if (area > 5.0 && area < 1500.0) {
                        Rect bbox = boundingRect(contours[i]);
                        float aspect = (float)bbox.width / (float)bbox.height;

                        if (aspect > 0.4f && aspect < 2.5f) {
                            Moments m = moments(contours[i]);
                            if (m.m00 > 0) {
                                int cx = (int)(m.m10 / m.m00);
                                int cy = (int)(m.m01 / m.m00);
                                currentCentroids.push_back(Point(cx, cy));
                                drawContours(outputView, contours, (int)i, Scalar(150, 255, 150), -1);
                            }
                        }
                    }
                }

                vector<Match> matches;
                for (int p = 0; p < (int)activePieces.size(); p++) {
                    for (int c = 0; c < (int)currentCentroids.size(); c++) {
                        double d = norm(activePieces[p].position - currentCentroids[c]);
                        matches.push_back({ p, c, d });
                    }
                }

                sort(matches.begin(), matches.end(), [](const Match& a, const Match& b) {
                    return a.dist < b.dist;
                    });

                vector<bool> pieceMatched(activePieces.size(), false);
                vector<bool> centroidMatched(currentCentroids.size(), false);

                // Passagem 1
                for (const auto& m : matches) {
                    if (!pieceMatched[m.pieceIdx] && !centroidMatched[m.centroidIdx]) {
                        double maxDist = (activePieces[m.pieceIdx].framesMissing > 0) ? 150.0 : 40.0;

                        if (m.dist < maxDist) {
                            activePieces[m.pieceIdx].position = currentCentroids[m.centroidIdx];
                            activePieces[m.pieceIdx].framesMissing = 0;
                            activePieces[m.pieceIdx].framesOccluded = 0;
                            activePieces[m.pieceIdx].framesVisible++;

                            if (!activePieces[m.pieceIdx].isConfirmed && activePieces[m.pieceIdx].framesVisible > 10) {
                                activePieces[m.pieceIdx].isConfirmed = true;
                                activePieces[m.pieceIdx].id = getAvailableId();
                            }

                            pieceMatched[m.pieceIdx] = true;
                            centroidMatched[m.centroidIdx] = true;
                        }
                    }
                }

                // Passagem 2
                for (const auto& m : matches) {
                    if (!pieceMatched[m.pieceIdx] && !centroidMatched[m.centroidIdx]) {
                        if (activePieces[m.pieceIdx].framesMissing > 0 && m.dist < 150.0) {
                            activePieces[m.pieceIdx].position = currentCentroids[m.centroidIdx];
                            activePieces[m.pieceIdx].framesMissing = 0;
                            activePieces[m.pieceIdx].framesVisible++;
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
                                if (activePieces[p].framesOccluded < 150) {
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
                for (size_t c = 0; c < currentCentroids.size(); c++) {
                    if (!centroidMatched[c]) {
                        TrackedPiece newPiece;
                        newPiece.id = -1;
                        newPiece.position = currentCentroids[c];
                        newPiece.framesMissing = 0;
                        newPiece.framesVisible = 1;
                        newPiece.framesOccluded = 0;
                        newPiece.isConfirmed = false;
                        activePieces.push_back(newPiece);
                    }
                }

                // Faxina
                activePieces.erase(remove_if(activePieces.begin(), activePieces.end(),
                    [](const TrackedPiece& p) {
                        if (!p.isConfirmed && p.framesMissing > 0) return true;
                        return p.framesMissing > 90;
                    }), activePieces.end());

                // Atualiza a lista de exportação e desenha o HUD
                exportPieces.clear();
                for (const auto& p : activePieces) {
                    if (p.isConfirmed) {
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