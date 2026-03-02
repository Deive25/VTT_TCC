// ============================================================
// CameraController.cs
// Controla a câmera ortográfica 2D do VTT.
//
// Funcionalidades:
//   - Pan: arrastar com o botão direito ou do meio do mouse
//   - Zoom: scroll do mouse
//   - Clamp: a câmera nunca ultrapassa as bordas do tabuleiro
//   - Adapta o tamanho inicial à resolução do monitor
// ============================================================
using UnityEngine;

/// <summary>
/// Câmera 2D com pan, zoom e limites baseados no tabuleiro ativo.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    // --------------------------------------------------------
    // Inspector
    // --------------------------------------------------------
    [Header("Pan")]
    [Tooltip("Velocidade de pan ao arrastar (multiplicador).")]
    [SerializeField] private float panSensitivity = 1f;

    [Header("Zoom")]
    [Tooltip("Velocidade de zoom ao usar o scroll.")]
    [SerializeField] private float zoomSpeed = 0.15f;

    [Tooltip("Tamanho ortográfico mínimo (mais zoom).")]
    [SerializeField] private float minOrthoSize = 0.5f;

    [Tooltip("Tamanho ortográfico máximo (menos zoom).")]
    [SerializeField] private float maxOrthoSize = 30f;

    // --------------------------------------------------------
    // Dependências
    // --------------------------------------------------------
    private Camera cam;
    private MapController mapController;
    private DefaultBoardRenderer defaultBoard;

    // --------------------------------------------------------
    // Estado de Pan
    // --------------------------------------------------------
    private Vector3 panOriginWorld; // Posição de mundo onde o drag começou
    private bool isDragging = false;

    // --------------------------------------------------------
    // Unity Lifecycle
    // --------------------------------------------------------

    private void Awake()
    {
        cam = GetComponent<Camera>();
        mapController  = FindFirstObjectByType<MapController>();
        defaultBoard   = FindFirstObjectByType<DefaultBoardRenderer>();

        // Garante que a câmera é ortográfica
        cam.orthographic = true;

        // Inicializa o tamanho ortográfico com base na resolução do monitor
        InitializeOrthoSize();
    }

    private void Update()
    {
        HandlePan();
        HandleZoom();
        ClampCameraToActiveBounds();
    }

    // --------------------------------------------------------
    // Inicialização
    // --------------------------------------------------------

    /// <summary>
    /// Define o tamanho ortográfico inicial para que o tabuleiro padrão
    /// apareça centralizado e totalmente visível na resolução atual.
    /// </summary>
    private void InitializeOrthoSize()
    {
        if (defaultBoard == null) return;

        Vector2 boardSize = defaultBoard.GetBoardSize();

        // Calcula tamanho para mostrar toda a altura do board com 5% de margem
        float requiredByHeight = (boardSize.y / 2f) * 1.05f;
        // Garante que a largura também caiba
        float requiredByWidth  = (boardSize.x / 2f / cam.aspect) * 1.05f;

        cam.orthographicSize = Mathf.Max(requiredByHeight, requiredByWidth);
        transform.position   = new Vector3(0f, 0f, -10f);
    }

    // --------------------------------------------------------
    // Pan
    // --------------------------------------------------------

    private void HandlePan()
    {
        // Botão do meio (2) ou direito (1)
        bool panButtonDown = Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2);
        bool panButtonHeld = Input.GetMouseButton(1)    || Input.GetMouseButton(2);
        bool panButtonUp   = Input.GetMouseButtonUp(1)  || Input.GetMouseButtonUp(2);

        if (panButtonDown)
        {
            panOriginWorld = cam.ScreenToWorldPoint(Input.mousePosition);
            isDragging     = true;
        }

        if (panButtonUp)
        {
            isDragging = false;
        }

        if (isDragging && panButtonHeld)
        {
            // Calcula o delta em espaço de mundo e move a câmera na direção oposta
            Vector3 currentWorld = cam.ScreenToWorldPoint(Input.mousePosition);
            Vector3 delta        = panOriginWorld - currentWorld;

            transform.position += delta * panSensitivity;

            // Atualiza a origem para o frame atual (evita acúmulo)
            panOriginWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        }
    }

    // --------------------------------------------------------
    // Zoom
    // --------------------------------------------------------

    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) < 0.001f) return;

        // Zoom proporcional ao tamanho atual (sentimento constante)
        float newSize = cam.orthographicSize * (1f - scroll * zoomSpeed * 10f);
        cam.orthographicSize = Mathf.Clamp(newSize, minOrthoSize, maxOrthoSize);
    }

    // --------------------------------------------------------
    // Clamp nos Bounds
    // --------------------------------------------------------

    private void ClampCameraToActiveBounds()
    {
        Bounds activeBounds = GetActiveBounds();

        float camHalfH = cam.orthographicSize;
        float camHalfW = cam.orthographicSize * cam.aspect;

        // Calcula limites permitidos para o centro da câmera
        float xMin = activeBounds.min.x + camHalfW;
        float xMax = activeBounds.max.x - camHalfW;
        float yMin = activeBounds.min.y + camHalfH;
        float yMax = activeBounds.max.y - camHalfH;

        float clampedX, clampedY;

        // Se a câmera for mais larga que o tabuleiro, centraliza no eixo X
        if (xMin > xMax)
            clampedX = activeBounds.center.x;
        else
            clampedX = Mathf.Clamp(transform.position.x, xMin, xMax);

        // Se a câmera for mais alta que o tabuleiro, centraliza no eixo Y
        if (yMin > yMax)
            clampedY = activeBounds.center.y;
        else
            clampedY = Mathf.Clamp(transform.position.y, yMin, yMax);

        transform.position = new Vector3(clampedX, clampedY, transform.position.z);
    }

    /// <summary>
    /// Retorna os bounds do elemento ativo: mapa (se carregado) ou board padrão.
    /// </summary>
    private Bounds GetActiveBounds()
    {
        if (mapController != null && mapController.IsMapLoaded)
            return mapController.MapBounds;

        if (defaultBoard != null)
            return defaultBoard.GetBoardBounds();

        // Fallback: bounds genérico
        return new Bounds(Vector3.zero, new Vector3(20f, 12f, 1f));
    }

    // --------------------------------------------------------
    // API Pública
    // --------------------------------------------------------

    /// <summary>
    /// Enquadra a câmera para mostrar todo o tabuleiro ativo.
    /// Chamado pelo botão "Reset Zoom" da UI.
    /// </summary>
    public void FocusOnActiveBoard()
    {
        Bounds bounds = GetActiveBounds();

        // Centraliza na origem do board
        transform.position = new Vector3(bounds.center.x, bounds.center.y, transform.position.z);

        // Ajusta zoom para mostrar tudo
        float requiredH = bounds.extents.y * 1.05f;
        float requiredW = bounds.extents.x / cam.aspect * 1.05f;
        cam.orthographicSize = Mathf.Clamp(Mathf.Max(requiredH, requiredW), minOrthoSize, maxOrthoSize);
    }
}
