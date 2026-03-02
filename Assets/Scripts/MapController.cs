// ============================================================
// MapController.cs
// Responsável pela lógica central do mapa:
//   - Receber a textura carregada e criar o Sprite
//   - Aplicar escala (normalizada pelo fit-to-screen)
//   - Centralizar o mapa no mundo
//   - Expor os Bounds do mapa para outros sistemas
// ============================================================
using UnityEngine;

/// <summary>
/// Controla o sprite de mapa: criação, escala e posicionamento.
/// Requer um SpriteRenderer no mesmo GameObject.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class MapController : MonoBehaviour
{
    // --------------------------------------------------------
    // Inspector
    // --------------------------------------------------------
    [Header("Configurações de Escala")]
    [Tooltip("Escala mínima permitida pelo slider.")]
    [SerializeField] private float minScale = 0.1f;

    [Tooltip("Escala máxima permitida pelo slider.")]
    [SerializeField] private float maxScale = 5f;

    [Tooltip("Pixels por unidade Unity (PPU) do sprite gerado.")]
    [SerializeField] private float pixelsPerUnit = 100f;

    // --------------------------------------------------------
    // Estado interno
    // --------------------------------------------------------
    private SpriteRenderer spriteRenderer;
    private Texture2D currentTexture;
    private float currentScale = 1f;
    private bool mapLoaded = false;

    // --------------------------------------------------------
    // Propriedades públicas
    // --------------------------------------------------------

    /// <summary>Bounding box do mapa no espaço de mundo atual.</summary>
    public Bounds MapBounds => spriteRenderer.bounds;

    /// <summary>True se um mapa foi carregado com sucesso.</summary>
    public bool IsMapLoaded => mapLoaded;

    public float MinScale => minScale;
    public float MaxScale => maxScale;
    public float CurrentScale => currentScale;

    // --------------------------------------------------------
    // Unity Lifecycle
    // --------------------------------------------------------

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.enabled = false; // oculta até um mapa ser carregado
    }

    private void OnEnable()
    {
        MapEvents.OnMapLoaded += HandleMapLoaded;
        MapEvents.OnScaleChangeRequested += HandleScaleChange;
        MapEvents.OnCenterMapRequested += HandleCenterMap;
        MapEvents.OnResetZoomRequested += HandleResetZoom;
    }

    private void OnDisable()
    {
        MapEvents.OnMapLoaded -= HandleMapLoaded;
        MapEvents.OnScaleChangeRequested -= HandleScaleChange;
        MapEvents.OnCenterMapRequested -= HandleCenterMap;
        MapEvents.OnResetZoomRequested -= HandleResetZoom;
    }

    // --------------------------------------------------------
    // Handlers de Eventos
    // --------------------------------------------------------

    private void HandleMapLoaded(Texture2D texture)
    {
        if (texture == null)
        {
            Debug.LogError("[MapController] Textura nula recebida em HandleMapLoaded.");
            return;
        }

        // Limpa textura anterior (evita memory leak)
        if (currentTexture != null && currentTexture != texture)
        {
            Destroy(currentTexture);
        }

        currentTexture = texture;

        // Cria o Sprite a partir da textura
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),   // Pivot no centro
            pixelsPerUnit
        );

        spriteRenderer.sprite = sprite;
        spriteRenderer.sortingOrder = 0;
        spriteRenderer.enabled = true;

        mapLoaded = true;

        // Coloca o mapa na origem e ajusta escala ao tamanho da tela
        transform.position = Vector3.zero;
        FitMapToScreen();

        NotifyMapInfoUpdated();

        Debug.Log($"[MapController] Mapa carregado: {texture.width}x{texture.height}px");
    }

    private void HandleScaleChange(float newScale)
    {
        ApplyScale(newScale);
    }

    private void HandleCenterMap()
    {
        // Centraliza o mapa na câmera principal
        Camera cam = Camera.main;
        if (cam != null)
        {
            transform.position = new Vector3(
                cam.transform.position.x,
                cam.transform.position.y,
                0f
            );
        }
        else
        {
            transform.position = Vector3.zero;
        }
    }

    private void HandleResetZoom()
    {
        // Reseta para fit-to-screen e centraliza
        transform.position = Vector3.zero;
        FitMapToScreen();
        NotifyMapInfoUpdated();
    }

    // --------------------------------------------------------
    // Lógica de Escala
    // --------------------------------------------------------

    /// <summary>
    /// Calcula e aplica a escala necessária para o mapa caberr na tela
    /// com uma margem de 5%. Mantém o aspect ratio.
    /// </summary>
    private void FitMapToScreen()
    {
        if (spriteRenderer.sprite == null) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        float screenHeight = cam.orthographicSize * 2f;
        float screenWidth  = screenHeight * cam.aspect;

        float spriteHeight = spriteRenderer.sprite.bounds.size.y;
        float spriteWidth  = spriteRenderer.sprite.bounds.size.x;

        float scaleX = screenWidth  / spriteWidth;
        float scaleY = screenHeight / spriteHeight;

        // Escolhe a menor escala para garantir que o mapa cabe inteiro
        currentScale = Mathf.Min(scaleX, scaleY) * 0.95f;
        currentScale = Mathf.Clamp(currentScale, minScale, maxScale);

        transform.localScale = new Vector3(currentScale, currentScale, 1f);
    }

    /// <summary>Aplica uma escala direta, com clamp nos limites definidos.</summary>
    private void ApplyScale(float scale)
    {
        currentScale = Mathf.Clamp(scale, minScale, maxScale);
        transform.localScale = new Vector3(currentScale, currentScale, 1f);
        NotifyMapInfoUpdated();
    }

    // --------------------------------------------------------
    // Utilitários
    // --------------------------------------------------------

    private void NotifyMapInfoUpdated()
    {
        MapEvents.FireMapInfoUpdated(new MapInfo
        {
            widthPx  = currentTexture != null ? currentTexture.width  : 0,
            heightPx = currentTexture != null ? currentTexture.height : 0,
            scale    = currentScale,
            isLoaded = mapLoaded
        });
    }
}
