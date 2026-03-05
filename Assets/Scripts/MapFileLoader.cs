// ============================================================
// MapFileLoader.cs
// Responsável por carregar imagens do disco como Texture2D.
//
// Suporte a formatos: PNG e JPG/JPEG
//
// Estratégia de file picker:
//   EDITOR:      Usa EditorUtility.OpenFilePanel (nativo Unity)
//   STANDALONE:  Usa System.Windows.Forms.OpenFileDialog (Windows)
//               → Para Linux/macOS, veja a nota sobre StandaloneFileBrowser
//
// USO:
//   MapFileLoader.Instance.OpenFilePicker();
// ============================================================
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
using System.Threading;
using System.Windows.Forms;
#endif

/// <summary>
/// Singleton que gerencia o processo de seleção e carregamento de mapas.
/// </summary>
public class MapFileLoader : MonoBehaviour
{
    // --------------------------------------------------------
    // Singleton
    // --------------------------------------------------------
    public static MapFileLoader Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }

    // --------------------------------------------------------
    // Estado
    // --------------------------------------------------------
    private bool isLoading = false;

    // --------------------------------------------------------
    // Ponto de entrada principal
    // --------------------------------------------------------

    /// <summary>
    /// Abre o seletor de arquivo.
    /// No editor usa EditorUtility; em build Windows usa OpenFileDialog.
    /// Em outros sistemas, utiliza o caminho digitado pelo usuário na UI.
    /// </summary>
    public void OpenFilePicker()
    {
        if (isLoading)
        {
            Debug.LogWarning("[MapFileLoader] Já existe um carregamento em progresso.");
            return;
        }

#if UNITY_EDITOR
        OpenFilePickerEditor();
#elif UNITY_STANDALONE_WIN
        OpenFilePickerWindows();
#else
        Debug.Log("[MapFileLoader] File picker nativo não disponível nesta plataforma. " +
                  "Use o campo de texto na UI para digitar o caminho do arquivo.");
#endif
    }

    // --------------------------------------------------------
    // Editor: usa EditorUtility
    // --------------------------------------------------------
#if UNITY_EDITOR
    private void OpenFilePickerEditor()
    {
        string path = UnityEditor.EditorUtility.OpenFilePanel(
            "Selecionar Mapa", "", "png,jpg,jpeg");

        if (!string.IsNullOrEmpty(path))
            StartCoroutine(LoadTextureFromPath(path));
    }
#endif

    // --------------------------------------------------------
    // Windows Standalone: usa OpenFileDialog em thread separada
    // --------------------------------------------------------
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    private string pendingFilePath = null;
    private bool fileDialogCompleted = false;

    private void OpenFilePickerWindows()
    {
        fileDialogCompleted = false;
        pendingFilePath     = null;

        Thread t = new Thread(() =>
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Title  = "Selecionar Mapa",
                Filter = "Imagens|*.png;*.jpg;*.jpeg|PNG|*.png|JPEG|*.jpg;*.jpeg",
                CheckFileExists = true
            };

            if (dialog.ShowDialog() == DialogResult.OK)
                pendingFilePath = dialog.FileName;

            fileDialogCompleted = true;
        });

        t.SetApartmentState(ApartmentState.STA); // obrigatório para WinForms
        t.Start();

        StartCoroutine(WaitForWindowsDialog());
    }

    private IEnumerator WaitForWindowsDialog()
    {
        // Aguarda a thread do dialog terminar
        while (!fileDialogCompleted)
            yield return null;

        if (!string.IsNullOrEmpty(pendingFilePath))
            yield return StartCoroutine(LoadTextureFromPath(pendingFilePath));
    }
#endif

    // --------------------------------------------------------
    // Carregamento por Caminho
    // --------------------------------------------------------

    /// <summary>
    /// Carrega uma textura a partir de um caminho de arquivo local.
    /// Funciona com caminhos absolutos em qualquer plataforma.
    /// </summary>
    /// <param name="filePath">Caminho absoluto para a imagem (PNG ou JPG).</param>
    /// 

    // NOVO: Permite passar um callback específico em vez de disparar o evento global de mapa base
    private System.Action<Texture2D> currentCallback;

    public void OpenFilePickerWithCallback(System.Action<Texture2D> onSuccess)
    {
        if (isLoading) return;
        currentCallback = onSuccess;

#if UNITY_EDITOR
        string path = UnityEditor.EditorUtility.OpenFilePanel("Selecionar Imagem", "", "png,jpg,jpeg");
        if (!string.IsNullOrEmpty(path)) StartCoroutine(LoadTextureRoutine(path));
#elif UNITY_STANDALONE_WIN
        OpenFilePickerWindows(true); // Precisaria adaptar o Windows picker para usar callback
#else
        Debug.Log("[MapFileLoader] File picker não disponível.");
#endif
    }

    private IEnumerator LoadTextureRoutine(string filePath)
    {
        isLoading = true;
        string url = "file://" + filePath;
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D tex = DownloadHandlerTexture.GetContent(request);
                if (currentCallback != null)
                {
                    currentCallback.Invoke(tex);
                    currentCallback = null;
                }
                else
                {
                    MapEvents.FireMapLoaded(tex);
                }
            }
        }
        isLoading = false;
    }

    public void LoadFromPath(string filePath)
    {
        if (isLoading) return;

        if (!File.Exists(filePath))
        {
            Debug.LogError($"[MapFileLoader] Arquivo não encontrado: {filePath}");
            return;
        }

        string ext = Path.GetExtension(filePath).ToLower();
        if (ext != ".png" && ext != ".jpg" && ext != ".jpeg")
        {
            Debug.LogError($"[MapFileLoader] Formato não suportado: {ext}. Use PNG ou JPG.");
            return;
        }

        StartCoroutine(LoadTextureFromPath(filePath));
    }

    // --------------------------------------------------------
    // Coroutine de carregamento
    // --------------------------------------------------------

    private IEnumerator LoadTextureFromPath(string filePath)
    {
        isLoading = true;
        Debug.Log($"[MapFileLoader] Carregando: {filePath}");

        // Adiciona prefixo "file://" para UnityWebRequest
        string url = "file://" + filePath;

        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(request);

                if (texture != null)
                {
                    // Dispara o evento central — MapController e outros irão reagir
                    MapEvents.FireMapLoaded(texture);
                    Debug.Log($"[MapFileLoader] Mapa carregado com sucesso: {texture.width}x{texture.height}");
                }
                else
                {
                    Debug.LogError("[MapFileLoader] Textura retornada é nula.");
                }
            }
            else
            {
                Debug.LogError($"[MapFileLoader] Erro ao carregar arquivo: {request.error}");
            }
        }

        isLoading = false;
    }


}
