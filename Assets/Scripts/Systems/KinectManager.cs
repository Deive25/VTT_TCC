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
    [DllImport(dllName)] public static extern void GetPieceData(int index, ref int id, ref int x, ref int y, ref int isLost);
    [DllImport(dllName)] public static extern void StopTracker();

    // ==========================================
    // VARIÁVEIS DA UNITY
    // ==========================================
    [Header("Mapeamento do Projetor/Câmera")]
    [Tooltip("Inverter eixo X se a câmera estiver espelhada em relação ao projetor")]
    public bool invertX = false;
    [Tooltip("Inverter eixo Y (O padrão é true, pois o pixel Y=0 no Kinect é no topo, e na Unity o Y cresce para cima)")]
    public bool invertY = true;

    [Header("Sistema de Recuperação")]
    public float autoRebindDistance = 3.0f; // Distância para "adotar" um fantasma

    private Dictionary<int, TokenController> boundTokens = new Dictionary<int, TokenController>();
    private List<TokenController> orphanedTokens = new List<TokenController>();
    private TokenController pendingBindingToken = null;

    // --- CONTROLE DE INICIALIZAÇÃO PREGUIÇOSA ---
    private bool isKinectReady = false;
    private bool initAttempted = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // Deixado vazio de propósito para a inicialização preguiçosa no Update()
    }

    public void StartBinding(TokenController token)
    {
        pendingBindingToken = token;
        Debug.Log("VINCULAÇÃO: Coloque a miniatura física na mesa agora.");
    }

    void Update()
    {
        // 1. CHECAGEM DE MODO: Se não for mesa física, aborta o frame inteiro.
        if (PlayerDisplaySystem.Instance != null && PlayerDisplaySystem.Instance.currentMode != VTTMode.Physical_Table)
        {
            return;
        }

        // 2. INICIALIZAÇÃO PREGUIÇOSA (Lazy Init): Tenta ligar o Kinect na primeira vez necessária
        if (!isKinectReady)
        {
            if (!initAttempted)
            {
                initAttempted = true;
                if (InitTracker())
                {
                    Debug.Log("[KinectManager] Kinect Conectado com Sucesso!");
                    isKinectReady = true;
                }
                else
                {
                    Debug.LogError("[KinectManager] ERRO: Kinect não encontrado ou DLL faltando.");
                }
            }
            return; // Se não inicializou com sucesso, não tenta processar o frame
        }

        // --- CONTROLES E ATUALIZAÇÕES DO KINECT ---
        if (Input.GetKeyDown(KeyCode.B))
        {
            Debug.Log("Kinect: Botão B apertado! Iniciando Calibração...");
            StartCalibration();
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("Kinect: Botão R apertado! Resetando memórias...");
            ResetTracker();
            boundTokens.Clear();
            orphanedTokens.Clear();
        }

        ProcessFrame();

        int count = GetPieceCount();
        HashSet<int> idsThisFrame = new HashSet<int>();

        orphanedTokens.RemoveAll(t => t == null);

        // --- PEGA AS BORDAS DO MAPA ATUAL ---
        Bounds mapBounds = new Bounds(Vector3.zero, new Vector3(24f, 14f, 0f)); // Limites padrão se não houver mapa
        if (LayerManager.Instance != null)
        {
            var activeLayer = LayerManager.Instance.GetActiveLayer();
            if (activeLayer != null && activeLayer.renderer != null)
            {
                mapBounds = activeLayer.renderer.bounds;
            }
        }

        for (int i = 0; i < count; i++)
        {
            int id = 0, x = 0, y = 0, isLost = 0;
            GetPieceData(i, ref id, ref x, ref y, ref isLost);

            idsThisFrame.Add(id);

            // 1. Converte o pixel do Kinect (640x480) para uma porcentagem (0.0 a 1.0)
            float normX = x / 640f;
            float normY = y / 480f;

            // 2. Aplica as inversões da câmera se necessário
            if (invertX) normX = 1f - normX;
            if (invertY) normY = 1f - normY;

            // 3. Mapeia a porcentagem para os limites reais do mapa na cena da Unity
            float worldX = Mathf.Lerp(mapBounds.min.x, mapBounds.max.x, normX);
            float worldY = Mathf.Lerp(mapBounds.min.y, mapBounds.max.y, normY);

            Vector3 worldPosition = new Vector3(worldX, worldY, 0f);

            // ========================================================
            // A PARTIR DAQUI A LÓGICA CONTINUA IGUAL...
            // ========================================================
            if (!boundTokens.ContainsKey(id))
            {
                TokenController bestOrphan = null;
                float bestDist = autoRebindDistance;

                foreach (TokenController orphan in orphanedTokens)
                {
                    float dist = Vector2.Distance(new Vector2(orphan.transform.position.x, orphan.transform.position.y),
                                                  new Vector2(worldPosition.x, worldPosition.y));
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestOrphan = orphan;
                    }
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
                if (tk != null)
                {
                    tk.SetLostState(true);
                    orphanedTokens.Add(tk);
                }
                idsToRemove.Add(kvp.Key);
            }
        }

        foreach (int id in idsToRemove) boundTokens.Remove(id);
    }

    private void OnGUI()
    {
        if (pendingBindingToken != null)
        {
            GUI.color = Color.yellow;
            GUI.Label(new Rect(Screen.width / 2 - 150, 20, 300, 30), "AGUARDANDO MINIATURA FISICA NA MESA...");
        }
    }

    void OnApplicationQuit() => StopTracker();
}