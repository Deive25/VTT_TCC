using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

public class KinectManager : MonoBehaviour
{
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
    [Header("Configurações Virtuais")]
    public GameObject tokenPrefab; // Arraste um cilindro ou miniatura 3D aqui
    public float scaleFactor = 0.05f; // Ajusta a conversão de Pixel do Kinect para Metros na Unity

    // Dicionário que guarda as peças que estão vivas no jogo
    private Dictionary<int, GameObject> activeTokens = new Dictionary<int, GameObject>();

    void Start()
    {
        // Liga o laser do Kinect ao dar Play
        if (InitTracker())
        {
            Debug.Log("Kinect Conectado com Sucesso!");
        }
        else
        {
            Debug.LogError("ERRO: Kinect não encontrado ou DLL faltando.");
        }
    }

    void Update()
    {
        // 1. Teclas de atalho para Calibrar e Resetar direto da Unity
        if (Input.GetKeyDown(KeyCode.B)) StartCalibration();
        if (Input.GetKeyDown(KeyCode.R)) ResetTracker();

        // 2. Manda o C++ processar a imagem (O coração pulsando)
        ProcessFrame();

        // 3. Lê quantas peças o C++ encontrou
        int count = GetPieceCount();

        // Lista para sabermos quem sobreviveu neste frame
        HashSet<int> idsThisFrame = new HashSet<int>();

        for (int i = 0; i < count; i++)
        {
            int id = 0, x = 0, y = 0, isLost = 0;
            GetPieceData(i, ref id, ref x, ref y, ref isLost);

            idsThisFrame.Add(id);

            // Converte a coordenada X e Y da câmera (640x480) para o mundo 3D da Unity
            // Centralizamos subtraindo 320 e 240. E invertemos o Y (porque na câmera o Y cresce pra baixo).
            Vector3 worldPosition = new Vector3((x - 320) * scaleFactor, 0, -(y - 240) * scaleFactor);

            if (activeTokens.ContainsKey(id))
            {
                // A peça JÁ EXISTE: Vamos apenas movê-la suavemente
                GameObject token = activeTokens[id];
                token.transform.position = Vector3.Lerp(token.transform.position, worldPosition, Time.deltaTime * 10f);

                // Efeito visual do "Fantasma" (3 segundos de persistência)
                Renderer rend = token.GetComponent<Renderer>();
                if (isLost == 1)
                {
                    rend.material.color = new Color(1f, 1f, 0f, 0.5f); // Fica Amarelo Transparente (LOST)
                }
                else
                {
                    rend.material.color = Color.red; // Fica Vermelho normal
                }
            }
            else
            {
                // A peça É NOVA: Vamos spawnar um novo monstro/token na mesa!
                GameObject newToken = Instantiate(tokenPrefab, worldPosition, Quaternion.identity);
                newToken.name = "Token_ID_" + id;
                newToken.GetComponent<Renderer>().material.color = Color.red;
                activeTokens.Add(id, newToken);
                Debug.Log("Novo Token spawnado: ID " + id);
            }
        }

        // 4. Faxina: Se alguma peça foi apagada da memória do C++, deletamos da Unity
        List<int> idsToRemove = new List<int>();
        foreach (int existingId in activeTokens.Keys)
        {
            if (!idsThisFrame.Contains(existingId))
            {
                Destroy(activeTokens[existingId]); // Destrói o GameObject
                idsToRemove.Add(existingId);
                Debug.Log("Token removido: ID " + existingId);
            }
        }

        foreach (int id in idsToRemove)
        {
            activeTokens.Remove(id);
        }
    }

    void OnApplicationQuit()
    {
        // Desliga o hardware do Kinect ao fechar o jogo
        StopTracker();
    }
}