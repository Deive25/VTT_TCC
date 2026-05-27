using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System.Runtime.InteropServices;

public class KinectManager : MonoBehaviour
{
    public static KinectManager Instance { get; private set; }

    // ==========================================
    // PONTE COM O C++ (DLL IMPORT)
    // ==========================================
    const string dllName = "KinectTrackerDLL";

    [DllImport(dllName)] public static extern bool InitTracker();
    [DllImport(dllName)] public static extern void StartCalibration();
    [DllImport(dllName)] public static extern void ResetTracker();
    [DllImport(dllName)] public static extern void ProcessFrame();
    [DllImport(dllName)] public static extern int GetPieceCount();
    [DllImport(dllName)] public static extern void GetPieceData(int index, ref int id, ref int x, ref int y, ref int isLost);
    [DllImport(dllName)] public static extern void SetProjectionROI(int tlx, int tly, int trx, int try_, int blx, int bly, int brx, int bry);
    [DllImport(dllName)] public static extern void ClearProjectionROI();
    [DllImport(dllName)] public static extern void StopTracker();

    // ==========================================
    // VARIAVEIS DE CALIBRACAO
    // ==========================================
    private enum CalibStep { None, TopLeft, TopRight, BottomLeft, BottomRight }
    private CalibStep currentCalibStep = CalibStep.None;

    [Header("Pontos Calibrados (Pixels)")]
    public Vector2 calibTopLeft = new Vector2(0, 0);
    public Vector2 calibTopRight = new Vector2(640, 0);
    public Vector2 calibBottomLeft = new Vector2(0, 480);
    public Vector2 calibBottomRight = new Vector2(640, 480);

    [Header("Ajuste Fino de Precisao")]
    [Tooltip("Move os tokens virtualmente para os lados (em centimetros/unidades) para casar perfeitamente com a lente do projetor.")]
    public float fineTuneX = 0f;
    [Tooltip("Move os tokens virtualmente para cima/baixo.")]
    public float fineTuneY = 0f;

    [Header("Area Util da Projecao")]
    [Tooltip("Margem normalizada aceita fora da area calibrada. 0.03 = 3% para tolerar ruido nas bordas.")]
    [Range(0f, 0.2f)]
    public float roiMargin = 0.03f;

    [Tooltip("Mostra dados de calibracao e mapeamento na tela para diagnostico.")]
    public bool showCalibrationDebug = true;

    [Tooltip("Quantidade de frames usados para calcular cada canto. Mais frames reduzem jitter do centroide.")]
    [Range(1, 60)]
    public int calibrationSampleFrames = 15;

    [Tooltip("Se ligado, a calibracao so aceita um objeto detectado por frame. Ajuda a nao pegar a peca errada.")]
    public bool requireSinglePieceForCornerCalibration = true;

    [Header("Sistema de Recuperacao")]
    public float autoRebindDistance = 3.0f;

    private Dictionary<int, TokenController> boundTokens = new Dictionary<int, TokenController>();
    private List<TokenController> orphanedTokens = new List<TokenController>();
    private TokenController pendingBindingToken = null;

    private GameObject calibTargetVisual = null;
    private bool kinectBlinkWarning = false;

    // Homografia Kinect pixel -> projection normalized space.
    // h[8] is fixed to 1, so only 8 coefficients are stored.
    private double[] kinectToProjectionHomography = null;
    private Vector2 lastRawKinectPixel = Vector2.zero;
    private Vector2 lastProjectionNormalized = Vector2.zero;
    private Vector3 lastMappedWorldPosition = Vector3.zero;
    private bool lastPointInsideProjection = false;
    private bool isCapturingCalibrationPoint = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (InitTracker()) Debug.Log("Kinect Conectado com Sucesso!");
        else Debug.LogError("ERRO: Kinect nao encontrado ou DLL faltando.");

        LoadCalibration();
        RebuildHomography();
        ApplyProjectionROIToTracker();
    }

    public void StartBinding(TokenController token)
    {
        pendingBindingToken = token;
        Debug.Log("VINCULACAO: Coloque a miniatura fisica na mesa agora.");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B)) StartCalibration();
        if (Input.GetKeyDown(KeyCode.R)) { ResetTracker(); boundTokens.Clear(); orphanedTokens.Clear(); }

        if (Input.GetKeyDown(KeyCode.C))
        {
            ClearProjectionROISafe();
            ResetTracker();
            currentCalibStep = CalibStep.TopLeft;
            UpdateCalibVisual();
            Debug.Log("CALIBRACAO: Coloque uma peca no ALVO VERMELHO.");
        }

        ProcessFrame();
        int count = GetPieceCount();

        if (currentCalibStep != CalibStep.None && Input.GetKeyDown(KeyCode.Space) && !isCapturingCalibrationPoint)
        {
            StartCoroutine(CaptureCalibrationPointRoutine());
            return;
        }

        if (isCapturingCalibrationPoint)
            return;

        HashSet<int> idsThisFrame = new HashSet<int>();
        orphanedTokens.RemoveAll(t => t == null);

        Bounds mapBounds = new Bounds(Vector3.zero, new Vector3(24f, 14f, 0f));
        if (LayerManager.Instance != null)
        {
            var activeLayer = LayerManager.Instance.GetActiveLayer();
            if (activeLayer != null && activeLayer.renderer != null) mapBounds = activeLayer.renderer.bounds;
        }

        for (int i = 0; i < count; i++)
        {
            int id = 0, x = 0, y = 0, isLost = 0;
            GetPieceData(i, ref id, ref x, ref y, ref isLost);

            Vector2 currentKinectPixel = new Vector2(x, y);
            Vector2 normalizedCoords = KinectPixelToProjection(currentKinectPixel);

            lastRawKinectPixel = currentKinectPixel;
            lastProjectionNormalized = normalizedCoords;

            lastPointInsideProjection =
                !float.IsNaN(normalizedCoords.x) && !float.IsNaN(normalizedCoords.y) &&
                normalizedCoords.x >= -roiMargin && normalizedCoords.x <= 1f + roiMargin &&
                normalizedCoords.y >= -roiMargin && normalizedCoords.y <= 1f + roiMargin;

            if (!lastPointInsideProjection)
            {
                continue;
            }

            normalizedCoords.x = Mathf.Clamp01(normalizedCoords.x);
            normalizedCoords.y = Mathf.Clamp01(normalizedCoords.y);

            idsThisFrame.Add(id);

            float worldX = Mathf.Lerp(mapBounds.min.x, mapBounds.max.x, normalizedCoords.x) + fineTuneX;
            float worldY = Mathf.Lerp(mapBounds.max.y, mapBounds.min.y, normalizedCoords.y) + fineTuneY;
            Vector3 worldPosition = new Vector3(worldX, worldY, 0f);
            lastMappedWorldPosition = worldPosition;

            if (!boundTokens.ContainsKey(id))
            {
                TokenController bestOrphan = null;
                float bestDist = autoRebindDistance;

                foreach (TokenController orphan in orphanedTokens)
                {
                    float dist = Vector2.Distance(new Vector2(orphan.transform.position.x, orphan.transform.position.y), new Vector2(worldPosition.x, worldPosition.y));
                    if (dist < bestDist) { bestDist = dist; bestOrphan = orphan; }
                }

                if (bestOrphan != null)
                {
                    orphanedTokens.Remove(bestOrphan);
                    boundTokens.Add(id, bestOrphan);
                    bestOrphan.kinectTrackingId = id;
                    bestOrphan.SetLostState(false);
                }
                else if (pendingBindingToken != null)
                {
                    boundTokens.Add(id, pendingBindingToken);
                    pendingBindingToken.kinectTrackingId = id;
                    pendingBindingToken.OnPlacedInMap();
                    pendingBindingToken = null;
                }
            }

            if (boundTokens.ContainsKey(id))
            {
                TokenController token = boundTokens[id];
                if (token != null)
                {
                    token.UpdatePositionFromKinect(worldPosition);
                    token.SetLostState(isLost == 1);
                }
            }
        }

        List<int> idsToRemove = new List<int>();
        foreach (var kvp in boundTokens)
        {
            if (!idsThisFrame.Contains(kvp.Key))
            {
                TokenController tk = kvp.Value;
                if (tk != null) { tk.SetLostState(true); orphanedTokens.Add(tk); }
                idsToRemove.Add(kvp.Key);
            }
        }
        foreach (int id in idsToRemove) boundTokens.Remove(id);
    }

    private void UpdateCalibVisual()
    {
        if (currentCalibStep == CalibStep.None)
        {
            if (calibTargetVisual != null) Destroy(calibTargetVisual);
            kinectBlinkWarning = false;
            return;
        }

        Bounds mapBounds = new Bounds(Vector3.zero, new Vector3(24f, 14f, 0f));
        if (LayerManager.Instance != null && LayerManager.Instance.GetActiveLayer() != null)
            mapBounds = LayerManager.Instance.GetActiveLayer().renderer.bounds;

        if (calibTargetVisual == null)
        {
            calibTargetVisual = new GameObject("CalibTargetVisual");
            SpriteRenderer sr = calibTargetVisual.AddComponent<SpriteRenderer>();

            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.red);
            tex.Apply();

            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            sr.sortingOrder = 9999;
        }

        float idealSize = Mathf.Min(mapBounds.size.x, mapBounds.size.y) * 0.05f;
        calibTargetVisual.transform.localScale = new Vector3(idealSize, idealSize, 1f);

        Vector3 pos = Vector3.zero;
        if (currentCalibStep == CalibStep.TopLeft) pos = new Vector3(mapBounds.min.x, mapBounds.max.y, -5f);
        if (currentCalibStep == CalibStep.TopRight) pos = new Vector3(mapBounds.max.x, mapBounds.max.y, -5f);
        if (currentCalibStep == CalibStep.BottomLeft) pos = new Vector3(mapBounds.min.x, mapBounds.min.y, -5f);
        if (currentCalibStep == CalibStep.BottomRight) pos = new Vector3(mapBounds.max.x, mapBounds.min.y, -5f);

        calibTargetVisual.transform.position = pos;
    }

    private void AdvanceCalibration(Vector2 pixel)
    {
        switch (currentCalibStep)
        {
            case CalibStep.TopLeft: calibTopLeft = pixel; currentCalibStep = CalibStep.TopRight; break;
            case CalibStep.TopRight: calibTopRight = pixel; currentCalibStep = CalibStep.BottomLeft; break;
            case CalibStep.BottomLeft: calibBottomLeft = pixel; currentCalibStep = CalibStep.BottomRight; break;
            case CalibStep.BottomRight:
                calibBottomRight = pixel;
                currentCalibStep = CalibStep.None;
                RebuildHomography();
                SaveCalibration();
                ApplyProjectionROIToTracker();
                break;
        }
        UpdateCalibVisual();
    }

    private IEnumerator CaptureCalibrationPointRoutine()
    {
        isCapturingCalibrationPoint = true;
        kinectBlinkWarning = false;

        Vector2 sum = Vector2.zero;
        int samples = 0;

        for (int frame = 0; frame < calibrationSampleFrames; frame++)
        {
            yield return null;

            if (TryGetCalibrationPiecePixel(out Vector2 pixel))
            {
                sum += pixel;
                samples++;
            }
        }

        if (samples > 0)
        {
            AdvanceCalibration(sum / samples);
        }
        else
        {
            kinectBlinkWarning = true;
            Debug.LogWarning("CALIBRACAO: nenhum frame valido capturado. Deixe apenas uma peca no alvo vermelho e tente novamente.");
        }

        isCapturingCalibrationPoint = false;
    }

    private bool TryGetCalibrationPiecePixel(out Vector2 pixel)
    {
        pixel = Vector2.zero;

        int count = GetPieceCount();
        if (count <= 0)
            return false;

        if (requireSinglePieceForCornerCalibration && count != 1)
        {
            Debug.LogWarning($"CALIBRACAO: {count} pecas detectadas. Remova objetos extras para nao calibrar pelo centroide errado.");
            return false;
        }

        int id = 0, x = 0, y = 0, isLost = 0;
        GetPieceData(0, ref id, ref x, ref y, ref isLost);

        if (isLost == 1)
            return false;

        pixel = new Vector2(x, y);
        return true;
    }

    private Vector2 KinectPixelToProjection(Vector2 p)
    {
        if (kinectToProjectionHomography == null)
            RebuildHomography();

        if (kinectToProjectionHomography == null)
            return new Vector2(float.NaN, float.NaN);

        double x = p.x;
        double y = p.y;
        double den = kinectToProjectionHomography[6] * x + kinectToProjectionHomography[7] * y + 1.0;

        if (System.Math.Abs(den) < 0.000001)
            return new Vector2(float.NaN, float.NaN);

        double u = (kinectToProjectionHomography[0] * x + kinectToProjectionHomography[1] * y + kinectToProjectionHomography[2]) / den;
        double v = (kinectToProjectionHomography[3] * x + kinectToProjectionHomography[4] * y + kinectToProjectionHomography[5]) / den;

        return new Vector2((float)u, (float)v);
    }

    private void RebuildHomography()
    {
        Vector2[] src =
        {
            calibTopLeft,
            calibTopRight,
            calibBottomLeft,
            calibBottomRight
        };

        Vector2[] dst =
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f)
        };

        kinectToProjectionHomography = SolveHomography(src, dst);
    }

    private void ApplyProjectionROIToTracker()
    {
        if (kinectToProjectionHomography == null)
            return;

        SetProjectionROISafe(
            Mathf.RoundToInt(calibTopLeft.x), Mathf.RoundToInt(calibTopLeft.y),
            Mathf.RoundToInt(calibTopRight.x), Mathf.RoundToInt(calibTopRight.y),
            Mathf.RoundToInt(calibBottomLeft.x), Mathf.RoundToInt(calibBottomLeft.y),
            Mathf.RoundToInt(calibBottomRight.x), Mathf.RoundToInt(calibBottomRight.y));
    }

    private void SetProjectionROISafe(int tlx, int tly, int trx, int try_, int blx, int bly, int brx, int bry)
    {
        try
        {
            SetProjectionROI(tlx, tly, trx, try_, blx, bly, brx, bry);
        }
        catch (System.EntryPointNotFoundException)
        {
            Debug.LogWarning("[KinectManager] DLL atual ainda nao possui SetProjectionROI. Recompile a DLL para ativar a ROI nativa.");
        }
    }

    private void ClearProjectionROISafe()
    {
        try
        {
            ClearProjectionROI();
        }
        catch (System.EntryPointNotFoundException)
        {
            Debug.LogWarning("[KinectManager] DLL atual ainda nao possui ClearProjectionROI. Recompile a DLL para ativar a ROI nativa.");
        }
    }

    private double[] SolveHomography(Vector2[] src, Vector2[] dst)
    {
        double[,] a = new double[8, 9];

        for (int i = 0; i < 4; i++)
        {
            double x = src[i].x;
            double y = src[i].y;
            double u = dst[i].x;
            double v = dst[i].y;
            int r = i * 2;

            a[r, 0] = x; a[r, 1] = y; a[r, 2] = 1.0;
            a[r, 6] = -u * x; a[r, 7] = -u * y; a[r, 8] = u;

            a[r + 1, 3] = x; a[r + 1, 4] = y; a[r + 1, 5] = 1.0;
            a[r + 1, 6] = -v * x; a[r + 1, 7] = -v * y; a[r + 1, 8] = v;
        }

        for (int col = 0; col < 8; col++)
        {
            int pivot = col;
            double best = System.Math.Abs(a[pivot, col]);
            for (int row = col + 1; row < 8; row++)
            {
                double candidate = System.Math.Abs(a[row, col]);
                if (candidate > best)
                {
                    best = candidate;
                    pivot = row;
                }
            }

            if (best < 0.000001)
            {
                Debug.LogError("[KinectManager] Calibracao invalida: os 4 pontos nao formam um quadrilatero utilizavel.");
                return null;
            }

            if (pivot != col)
            {
                for (int k = col; k < 9; k++)
                {
                    double temp = a[col, k];
                    a[col, k] = a[pivot, k];
                    a[pivot, k] = temp;
                }
            }

            double div = a[col, col];
            for (int k = col; k < 9; k++)
                a[col, k] /= div;

            for (int row = 0; row < 8; row++)
            {
                if (row == col) continue;
                double factor = a[row, col];
                for (int k = col; k < 9; k++)
                    a[row, k] -= factor * a[col, k];
            }
        }

        double[] h = new double[8];
        for (int i = 0; i < 8; i++)
            h[i] = a[i, 8];

        return h;
    }

    private void SaveCalibration()
    {
        PlayerPrefs.SetFloat("CalibTL_X", calibTopLeft.x); PlayerPrefs.SetFloat("CalibTL_Y", calibTopLeft.y);
        PlayerPrefs.SetFloat("CalibTR_X", calibTopRight.x); PlayerPrefs.SetFloat("CalibTR_Y", calibTopRight.y);
        PlayerPrefs.SetFloat("CalibBL_X", calibBottomLeft.x); PlayerPrefs.SetFloat("CalibBL_Y", calibBottomLeft.y);
        PlayerPrefs.SetFloat("CalibBR_X", calibBottomRight.x); PlayerPrefs.SetFloat("CalibBR_Y", calibBottomRight.y);
        PlayerPrefs.Save();
    }

    private void LoadCalibration()
    {
        if (PlayerPrefs.HasKey("CalibTL_X"))
        {
            calibTopLeft = new Vector2(PlayerPrefs.GetFloat("CalibTL_X"), PlayerPrefs.GetFloat("CalibTL_Y"));
            calibTopRight = new Vector2(PlayerPrefs.GetFloat("CalibTR_X"), PlayerPrefs.GetFloat("CalibTR_Y"));
            calibBottomLeft = new Vector2(PlayerPrefs.GetFloat("CalibBL_X"), PlayerPrefs.GetFloat("CalibBL_Y"));
            calibBottomRight = new Vector2(PlayerPrefs.GetFloat("CalibBR_X"), PlayerPrefs.GetFloat("CalibBR_Y"));
        }
        RebuildHomography();
    }

    private void OnGUI()
    {
        if (pendingBindingToken != null)
        {
            GUI.color = Color.yellow;
            GUI.Label(new Rect(Screen.width / 2 - 150, 20, 300, 30), "AGUARDANDO MINIATURA FISICA NA MESA...");
        }

        if (currentCalibStep != CalibStep.None)
        {
            if (kinectBlinkWarning)
            {
                GUI.color = Color.red;
                GUI.Box(new Rect(Screen.width / 2 - 300, Screen.height / 2 - 40, 600, 40), "FALHA: Nenhuma peca detectada. Mexa nela e aperte Espaco de novo.");
            }

            GUI.color = Color.white;
            GUI.Box(new Rect(Screen.width / 2 - 250, Screen.height / 2 - 0, 500, 40), "CALIBRACAO: Coloque a peca sobre o quadrado VERMELHO e aperte ESPACO.");
        }

        if (showCalibrationDebug)
        {
            GUI.color = lastPointInsideProjection ? Color.green : Color.red;
            GUI.Box(
                new Rect(20, Screen.height - 145, 460, 120),
                "KINECT DEBUG\n" +
                $"Raw px: {lastRawKinectPixel.x:F0}, {lastRawKinectPixel.y:F0}\n" +
                $"Proj 0..1: {lastProjectionNormalized.x:F3}, {lastProjectionNormalized.y:F3}\n" +
                $"World: {lastMappedWorldPosition.x:F2}, {lastMappedWorldPosition.y:F2}\n" +
                $"ROI: {(lastPointInsideProjection ? "DENTRO" : "FORA")}  margem={roiMargin:F2}\n" +
                $"Amostrando canto: {(isCapturingCalibrationPoint ? "SIM" : "NAO")}");
        }
    }

    void OnApplicationQuit() => StopTracker();
}
