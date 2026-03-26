#include <Windows.h>
#include <Ole2.h>
#include <NuiApi.h>
#include <iostream>
#include <vector>
#include <opencv2/opencv.hpp>

using namespace std;
using namespace cv;

// ==========================================
// ESTRUTURA DE MEMÓRIA (TRACKING)
// ==========================================
struct TrackedPiece {
    int id;
    Point position;
    int framesMissing; // Há quantos frames a peça sumiu (ex: escondida pela mão)
};

vector<TrackedPiece> activePieces;
int globalIdCounter = 1; // Para nunca repetir um ID
// ==========================================

INuiSensor* sensor = nullptr;
HANDLE depthStream;
HANDLE nextDepthFrameEvent;

const int width = 640;
const int height = 480;

Mat bgDepth;
bool hasBackground = false;

bool InitKinect() {
    int numSensors = 0;
    NuiGetSensorCount(&numSensors);
    if (numSensors == 0) return false;

    if (FAILED(NuiCreateSensorByIndex(0, &sensor))) return false;
    if (FAILED(sensor->NuiInitialize(NUI_INITIALIZE_FLAG_USES_DEPTH))) return false;

    nextDepthFrameEvent = CreateEvent(NULL, TRUE, FALSE, NULL);
    if (FAILED(sensor->NuiImageStreamOpen(NUI_IMAGE_TYPE_DEPTH, NUI_IMAGE_RESOLUTION_640x480, 0, 2, nextDepthFrameEvent, &depthStream))) return false;

    cout << "Kinect Inicializado com Sucesso!" << endl;
    return true;
}

int main() {
    if (!InitKinect()) return -1;

    cout << "\n=======================================================" << endl;
    cout << " CONTROLES DO TESTE DE VISAO (COM MEMORIA DE ID):" << endl;
    cout << " Pressione 'B' - Gravar o fundo" << endl;
    cout << " Pressione 'R' - Resetar IDs (Voltar para 1)" << endl;
    cout << "=======================================================\n" << endl;

    while (true) {
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

                Mat mask(height, width, CV_8UC1, Scalar(0));
                Mat outputView;
                cvtColor(displayDepth, outputView, COLOR_GRAY2BGR);

                if (hasBackground) {
                    Mat diff;
                    absdiff(bgDepth, currentDepth, diff);

                    // 1. LIMIARIZAÇÃO (Altura entre 15mm e 80mm)
                    for (int i = 0; i < width * height; i++) {
                        USHORT currD = currentDepth.at<USHORT>(i);
                        if (currD == 0) continue;
                        USHORT d = diff.at<USHORT>(i);
                        if (d > 15 && d < 80) mask.at<uchar>(i) = 255;
                    }

                    // 2. FILTRAGEM DE RUÍDO
                    medianBlur(mask, mask, 5);
                    Mat kernelErode = getStructuringElement(MORPH_ELLIPSE, Size(7, 7));
                    erode(mask, mask, kernelErode);
                    Mat kernelDilate = getStructuringElement(MORPH_ELLIPSE, Size(15, 15));
                    dilate(mask, mask, kernelDilate);

                    vector<vector<Point>> contours;
                    findContours(mask, contours, RETR_EXTERNAL, CHAIN_APPROX_SIMPLE);

                    // Lista de centróides detectados NESTE exato momento
                    vector<Point> currentCentroids;

                    for (size_t i = 0; i < contours.size(); i++) {
                        double area = contourArea(contours[i]);

                        // Tamanho da peça
                        if (area > 150.0 && area < 3500.0) {

                            // 3. FÓRMULA DE CIRCULARIDADE (Destrói manchas deformadas e fantasmas)
                            double perimeter = arcLength(contours[i], true);
                            double circularity = 4 * CV_PI * (area / (perimeter * perimeter));

                            // Se for maior que 0.65, o objeto é redondo/quadrado o suficiente!
                            if (circularity > 0.65) {
                                Moments m = moments(contours[i]);
                                if (m.m00 > 0) {
                                    int cx = m.m10 / m.m00;
                                    int cy = m.m01 / m.m00;
                                    currentCentroids.push_back(Point(cx, cy));

                                    // Desenha o contorno real validado
                                    drawContours(outputView, contours, (int)i, Scalar(0, 255, 0), 2);
                                }
                            }
                        }
                    }

                    // ===============================================================
                    // 4. LÓGICA DE RASTREAMENTO E MEMÓRIA (ID Persistente)
                    // ===============================================================
                    vector<bool> centroidUsed(currentCentroids.size(), false);

                    // A. Tenta combinar as peças antigas com as peças que estamos vendo agora
                    for (auto& piece : activePieces) {
                        int bestMatchIdx = -1;
                        double minDistance = 100000; // Começa com um valor bem alto

                        for (size_t i = 0; i < currentCentroids.size(); i++) {
                            if (centroidUsed[i]) continue; // Se esse ponto já tem dono, pula

                            // Calcula a distância entre a peça velha e a posição nova
                            double dist = norm(piece.position - currentCentroids[i]);

                            // Se a peça andou no máximo 80 pixels de distância (para não roubar peça dos outros)
                            if (dist < 80.0 && dist < minDistance) {
                                minDistance = dist;
                                bestMatchIdx = i;
                            }
                        }

                        if (bestMatchIdx != -1) {
                            // Achou a peça! Atualiza a posição e zera o tempo de sumiço
                            piece.position = currentCentroids[bestMatchIdx];
                            piece.framesMissing = 0;
                            centroidUsed[bestMatchIdx] = true;

                            // Desenha na tela
                            circle(outputView, piece.position, 4, Scalar(0, 0, 255), -1);
                            putText(outputView, "ID:" + to_string(piece.id), Point(piece.position.x + 10, piece.position.y), FONT_HERSHEY_SIMPLEX, 0.5, Scalar(255, 255, 0), 2);
                        }
                        else {
                            // Não achou a peça (A mão deve estar em cima!). Aumenta o tempo de sumiço.
                            piece.framesMissing++;
                        }
                    }

                    // B. Se sobrou alguma mancha nova sem dono, é uma PEÇA NOVA na mesa!
                    for (size_t i = 0; i < currentCentroids.size(); i++) {
                        if (!centroidUsed[i]) {
                            TrackedPiece newPiece;
                            newPiece.id = globalIdCounter++;
                            newPiece.position = currentCentroids[i];
                            newPiece.framesMissing = 0;
                            activePieces.push_back(newPiece);
                        }
                    }

                    // C. Faxina: Remove da memória peças que sumiram por mais de 45 frames (1.5 segundos)
                    activePieces.erase(remove_if(activePieces.begin(), activePieces.end(),
                        [](const TrackedPiece& p) { return p.framesMissing > 45; }), activePieces.end());

                    // Conta apenas as peças ativas na mesa que não estão escondidas
                    int piecesOnTable = 0;
                    for (const auto& p : activePieces) if (p.framesMissing == 0) piecesOnTable++;

                    putText(outputView, "Pecas na mesa: " + to_string(piecesOnTable), Point(10, 20), FONT_HERSHEY_SIMPLEX, 0.5, Scalar(0, 255, 255), 2);
                }
                else {
                    putText(outputView, "APERTE 'B' PARA GRAVAR O FUNDO", Point(10, 20), FONT_HERSHEY_SIMPLEX, 0.4, Scalar(0, 0, 255), 1);
                }

                imshow("1. Profundidade Bruta (Kinect)", displayDepth);
                imshow("2. Mascara (Subtracao de Fundo)", mask);
                imshow("3. Resultado Final (Rastreamento)", outputView);
            }

            texture->UnlockRect(0);
            sensor->NuiImageStreamReleaseFrame(depthStream, &imageFrame);
        }

        char key = (char)waitKey(30);
        if (key == 27) break;
        if (key == 'b' || key == 'B') {
            bgDepth = currentDepth.clone();
            hasBackground = true;
            activePieces.clear(); // Limpa as peças da memória ao recalibrar
            globalIdCounter = 1;
        }
        if (key == 'r' || key == 'R') {
            // Tecla de pânico para resetar a contagem para 1
            activePieces.clear();
            globalIdCounter = 1;
        }
    }

    if (sensor) {
        sensor->NuiShutdown();
        sensor->Release();
    }
    return 0;
}