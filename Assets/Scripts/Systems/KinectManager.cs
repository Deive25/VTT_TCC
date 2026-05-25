using System.Collections.Generic;
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
    [DllImport(dllName)] public static extern void GetPieceData(int index, ref int id, ref int x, ref int y, ref int isLost, ref float area);
    [DllImport(dllName)] public static extern void StopTracker();

    // ==========================================
    // VARIÁVEIS DE CALIBRAÇÃO 
    // ==========================================
    private enum CalibStep { None, TopLeft, TopRight, BottomLeft, BottomRight }
    private CalibStep currentCalibStep = CalibStep.None;

    [Header("Pontos Calibrados (Pixels)")]
    public Vector2 calibTopLeft = new Vector2(0, 0);
    public Vector2 calibTopRight = new Vector2(640, 0);
    public Vector2 calibBottomLeft = new Vector2(0, 480);
    public Vector2 calibBottomRight = new Vector2(640, 480);

    [Header("Ajuste Fino de Precisão")]
    [Tooltip("Move os tokens virtualmente para os lados (em centímetros/unidades) para casar perfeitamente com a lente do projetor.")]
    public float fineTuneX = 0f;
    [Tooltip("Move os tokens virtualmente para cima/baixo.")]
    public float fineTuneY = 0f;

    [Header("Sistema de Recuperação")]
    public float autoRebindDistance = 3.0f;

    private Dictionary<int, TokenController> boundTokens = new Dictionary<int, TokenController>();
    private List<TokenController> orphanedTokens = new List<TokenController>();
    private TokenController pendingBindingToken = null;

    private GameObject calibTargetVisual = null;
    private bool kinectBlinkWarning = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (InitTracker()) Debug.Log("Kinect Conectado com Sucesso!");
        else Debug.LogError("ERRO: Kinect não encontrado ou DLL faltando.");

        LoadCalibration();
    }

    public void StartBinding(TokenController token)
    {
        pendingBindingToken = token;
        Debug.Log("VINCULAÇÃO: Coloque a miniatura física na mesa agora.");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B)) StartCalibration();
        if (Input.GetKeyDown(KeyCode.R)) { ResetTracker(); boundTokens.Clear(); orphanedTokens.Clear(); }

        if (Input.GetKeyDown(KeyCode.C))
        {
            currentCalibStep = CalibStep.TopLeft;
            UpdateCalibVisual();
            Debug.Log("CALIBRAÇÃO: Coloque uma peça no ALVO VERMELHO.");
        }

        ProcessFrame();
        int count = GetPieceCount();

        if (currentCalibStep != CalibStep.None && Input.GetKeyDown(KeyCode.Space))
        {
            if (count > 0)
            {
                kinectBlinkWarning = false;
                int cId = 0, cX = 0, cY = 0, cLost = 0; float cArea = 0f;
                GetPieceData(0, ref cId, ref cX, ref cY, ref cLost, ref cArea);
                AdvanceCalibration(new Vector2(cX, cY));
            }
            else
            {
                kinectBlinkWarning = true;
                Debug.LogWarning("O Kinect não enxergou a peça neste frame. Mexa um pouco e aperte Espaço novamente!");
            }
            return;
        }

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
            float rawArea = 0f;
            GetPieceData(i, ref id, ref x, ref y, ref isLost, ref rawArea);

            Vector2 currentKinectPixel = new Vector2(x, y);
            Vector2 normalizedCoords = BilinearMapping(currentKinectPixel);

            // =================================================================
            // ISOLAMENTO DE ÁREA (CLIPPING): Ignora peças fora da calibração!
            // Dou uma margem de 5% (-0.05 a 1.05) para não bugar peças que estão exatamente em cima da linha.
            // =================================================================
            if (normalizedCoords.x < -0.05f || normalizedCoords.x > 1.05f ||
                normalizedCoords.y < -0.05f || normalizedCoords.y > 1.05f)
            {
                continue; // Pula esta peça e finge que o Kinect não a viu
            }

            // Se a peça passou do filtro, validamos o ID
            idsThisFrame.Add(id);

            // Aplica a posição no mapa somando o Ajuste Fino Manual (Fine Tune)
            float worldX = Mathf.Lerp(mapBounds.min.x, mapBounds.max.x, normalizedCoords.x) + fineTuneX;
            float worldY = Mathf.Lerp(mapBounds.max.y, mapBounds.min.y, normalizedCoords.y) + fineTuneY;
            Vector3 worldPosition = new Vector3(worldX, worldY, 0f);

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

            // Centraliza o ponto (0.5f, 0.5f) para alinhar a miniatura exatamente no meio
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            sr.sortingOrder = 9999;
        }

        float idealSize = Mathf.Min(mapBounds.size.x, mapBounds.size.y) * 0.05f;
        calibTargetVisual.transform.localScale = new Vector3(idealSize, idealSize, 1f);

        // Removemos o offset. Agora o centro do alvo é a quina absoluta do mapa.
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
            case CalibStep.BottomRight: calibBottomRight = pixel; currentCalibStep = CalibStep.None; SaveCalibration(); break;
        }
        UpdateCalibVisual();
    }

    private Vector2 BilinearMapping(Vector2 p)
    {
        // Alterado de InverseLerp (que trava em 0 e 1) para matemática pura,
        // permitindo que o sistema detecte quando a peça escapou para fora da área projetada (< 0 ou > 1).
        float u = InverseLerpUnclamped(Mathf.Lerp(calibTopLeft.x, calibBottomLeft.x, (p.y / 480f)), Mathf.Lerp(calibTopRight.x, calibBottomRight.x, (p.y / 480f)), p.x);
        float v = InverseLerpUnclamped(Mathf.Lerp(calibTopLeft.y, calibTopRight.y, (p.x / 640f)), Mathf.Lerp(calibBottomLeft.y, calibBottomRight.y, (p.x / 640f)), p.y);

        return new Vector2(u, v);
    }

    // Função de interpolação livre auxiliar
    private float InverseLerpUnclamped(float a, float b, float value)
    {
        if (a != b) return (value - a) / (b - a);
        return 0f;
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
    }

    void OnApplicationQuit() => StopTracker();
}