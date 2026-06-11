using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;

public class MapFileLoader : MonoBehaviour
{
    public static MapFileLoader Instance { get; private set; }

    private bool isLoading = false;
    private System.Action<Texture2D> pendingCallback = null;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public class OpenFileName
    {
        public int structSize = 0;
        public System.IntPtr dlgOwner = System.IntPtr.Zero;
        public System.IntPtr instance = System.IntPtr.Zero;
        public string filter = null;
        public string customFilter = null;
        public int maxCustFilter = 0;
        public int filterIndex = 0;
        public string file = null;
        public int maxFile = 0;
        public string fileTitle = null;
        public int maxFileTitle = 0;
        public string initialDir = null;
        public string title = null;
        public int flags = 0;
        public short fileOffset = 0;
        public short fileExtension = 0;
        public string defExt = null;
        public System.IntPtr custData = System.IntPtr.Zero;
        public System.IntPtr hook = System.IntPtr.Zero;
        public string templateName = null;
        public System.IntPtr reservedPtr = System.IntPtr.Zero;
        public int reservedInt = 0;
        public int flagsEx = 0;
    }

    [DllImport("Comdlg32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Auto)]
    public static extern bool GetOpenFileName([In, Out] OpenFileName ofn);

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void OpenFilePicker(System.Action<Texture2D> onLoaded = null)
    {
        if (isLoading) return;
        pendingCallback = onLoaded;

        OpenFileName ofn = new OpenFileName();
        ofn.structSize = Marshal.SizeOf(ofn);
        ofn.filter = "Imagens (*.jpg;*.jpeg;*.png)\0*.jpg;*.jpeg;*.png\0Todos os Arquivos (*.*)\0*.*\0";
        ofn.file = new string(new char[256]);
        ofn.maxFile = ofn.file.Length;
        ofn.fileTitle = new string(new char[64]);
        ofn.maxFileTitle = ofn.fileTitle.Length;
        ofn.title = "Selecione uma Imagem";
        ofn.flags = 0x00080000 | 0x00001000 | 0x00000008;

        if (GetOpenFileName(ofn))
        {
            string selectedPath = ofn.file;
            LoadFromPath(selectedPath, pendingCallback);
        }
        else
        {
            pendingCallback = null;
        }
    }

    public void LoadFromPath(string filePath, System.Action<Texture2D> onLoaded = null)
    {
        if (isLoading || !File.Exists(filePath)) return;
        pendingCallback = onLoaded;
        StartCoroutine(LoadTextureRoutine(filePath));
    }

    private IEnumerator LoadTextureRoutine(string filePath)
    {
        isLoading = true;
        string url = "file:///" + filePath.Replace("\\", "/");

        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(request);

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
