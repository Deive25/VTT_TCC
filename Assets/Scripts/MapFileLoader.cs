// ============================================================
// MapFileLoader.cs
// ============================================================
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
using System.Threading;
using System.Windows.Forms;
#endif

public class MapFileLoader : MonoBehaviour
{
    public static MapFileLoader Instance { get; private set; }

    private bool isLoading = false;
    private System.Action<Texture2D> pendingCallback = null;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Se o callback for nulo, importa como Mapa Base. Se tiver callback, importa como Camada.
    public void OpenFilePicker(System.Action<Texture2D> onLoaded = null)
    {
        if (isLoading) return;
        pendingCallback = onLoaded;

#if UNITY_EDITOR
        string path = UnityEditor.EditorUtility.OpenFilePanel("Selecionar Imagem", "", "png,jpg,jpeg");
        if (!string.IsNullOrEmpty(path)) StartCoroutine(LoadTextureRoutine(path));
#elif UNITY_STANDALONE_WIN
        OpenFilePickerWindows();
#else
        Debug.LogWarning("[MapFileLoader] File picker não disponível nesta plataforma.");
#endif
    }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    private string pendingFilePath = null;
    private bool fileDialogCompleted = false;

    private void OpenFilePickerWindows()
    {
        fileDialogCompleted = false;
        pendingFilePath = null;

        Thread t = new Thread(() =>
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Title = "Selecionar Imagem",
                Filter = "Imagens|*.png;*.jpg;*.jpeg",
                CheckFileExists = true
            };

            if (dialog.ShowDialog() == DialogResult.OK)
                pendingFilePath = dialog.FileName;

            fileDialogCompleted = true;
        });

        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        StartCoroutine(WaitForWindowsDialog());
    }

    private IEnumerator WaitForWindowsDialog()
    {
        while (!fileDialogCompleted) yield return null;
        if (!string.IsNullOrEmpty(pendingFilePath)) 
            yield return StartCoroutine(LoadTextureRoutine(pendingFilePath));
    }
#endif

    public void LoadFromPath(string filePath, System.Action<Texture2D> onLoaded = null)
    {
        if (isLoading || !File.Exists(filePath)) return;
        pendingCallback = onLoaded;
        StartCoroutine(LoadTextureRoutine(filePath));
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
                Texture2D texture = DownloadHandlerTexture.GetContent(request);

                // Redireciona a textura carregada para onde foi pedida
                if (pendingCallback != null)
                {
                    pendingCallback.Invoke(texture);
                    pendingCallback = null;
                }
                else
                {
                    MapEvents.FireMapLoaded(texture);
                }
            }
            else
            {
                Debug.LogError($"[MapFileLoader] Erro ao carregar: {request.error}");
            }
        }
        isLoading = false;
    }
}