// ============================================================
// CameraController.cs  –  VTT Camera System v5
//
// CONTROLES:
//   Zoom → Scroll do mouse
//   Pan  → Segurar botão DIREITO ou MEIO e arrastar
//
// ─── IMPORTANTE: CONFIGURAÇÃO DO INPUT SYSTEM ───────────────
//
//   Este script usa a API clássica (Input.GetAxis / Input.GetMouseButton).
//   Verifique em:
//     Edit → Project Settings → Player → Other Settings
//       → Active Input Handling → "Input Manager (Old)" OU "Both"
//
//   Se estiver como "New Input System Package" APENAS, os controles
//   não vão funcionar. Mude para "Both" e reinicie o Editor.
//
// ─── COMO VERIFICAR SE O SCRIPT ESTÁ FUNCIONANDO ────────────
//
//   Ative "debugMode = true" no Inspector e observe o Console.
//   Você deve ver logs de Zoom e Pan quando usar o mouse.
//   Se não aparecer nada, o script não está no objeto certo
//   ou o Input System está errado.
//
// ─── SETUP DA CENA ──────────────────────────────────────────
//
//   1. Selecione a Main Camera na Hierarchy
//   2. Adicione este script (Add Component → CameraController)
//   3. Verifique: Camera → Projection = Orthographic
//   4. A cena deve ter EventSystem (criado automaticamente com Canvas)
//
// ============================================================

using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    // ─── Inspector ───────────────────────────────────────────

    [Header("Zoom")]
    [Tooltip("Sensibilidade do scroll do mouse. Padrão: 0.15")]
    [SerializeField] private float zoomSensitivity = 0.15f;

    [Tooltip("Zoom-in máximo (menor valor = mais perto).")]
    [SerializeField] private float minZoom = 0.5f;

    [Tooltip("Zoom-out máximo em unidades ortográficas. Aumente para permitir visão mais afastada.\n" +
             "O board sempre fica centrado quando a câmera ultrapassa seus limites.")]
    [SerializeField] private float maxZoom = 30f;

    [Header("Pan")]
    [Tooltip("Sensibilidade do pan. 1 = movimento 1:1 com o cursor.")]
    [SerializeField] private float panSensitivity = 1f;

    [Header("Foco")]
    [Tooltip("Margem ao enquadrar o board (0.05 = 5%).")]
    [SerializeField] private float focusMargin = 0.05f;

    [Header("Debug")]
    [Tooltip("Ative para ver logs de Zoom e Pan no Console.")]
    [SerializeField] private bool debugMode = false;

    // ─── Referências ─────────────────────────────────────────

    private Camera _cam;
    private MapController _mapCtrl;
    private DefaultBoardRenderer _defaultBoard;

    // ─── Estado do Pan ───────────────────────────────────────

    private bool _panning;
    private Vector3 _panAnchorWorld; // ponto de mundo ancorado ao cursor

    // ============================================================
    // Lifecycle
    // ============================================================

    private void Awake()
    {
        _cam = GetComponent<Camera>();

        if (!_cam.orthographic)
        {
            Debug.LogWarning("[Camera] Câmera não está em modo Orthographic. Corrigindo...");
            _cam.orthographic = true;
        }
    }

    private void Start()
    {
        _mapCtrl = FindAnyObjectByType<MapController>();
        _defaultBoard = FindAnyObjectByType<DefaultBoardRenderer>();

        if (_mapCtrl == null)
            Debug.LogWarning("[Camera] MapController não encontrado na cena.");
        if (_defaultBoard == null)
            Debug.LogWarning("[Camera] DefaultBoardRenderer não encontrado na cena.");

        StartCoroutine(FocusNextFrame());
    }

    private void OnEnable() => MapEvents.OnMapLoaded += OnMapLoaded;
    private void OnDisable() => MapEvents.OnMapLoaded -= OnMapLoaded;

    private void Update()
    {
        HandleZoom();
        HandlePan();

        // Clamp SEMPRE no final do frame.
        ClampToBoardBounds();
    }

    // ============================================================
    // ZOOM — scroll do mouse
    // ============================================================

    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(scroll) < 0.0001f) return;

        // Ignora scroll quando o cursor está sobre a UI (painel do GM).
        if (IsPointerOverUI()) return;

        // NOVO: Ignora o zoom se o Shift estiver pressionado (pois está sendo usado para girar o token)
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) return;

        if (debugMode)
            Debug.Log($"[Camera] Zoom scroll={scroll:F4}  orthoSize={_cam.orthographicSize:F3}");

        // Captura o ponto de mundo sob o cursor ANTES de alterar o zoom.
        Vector3 worldBefore = CursorToWorld();

        // scroll > 0  = roda pra frente = zoom IN = orthographicSize diminui
        // scroll < 0  = roda pra trás  = zoom OUT = orthographicSize aumenta
        float newSize = _cam.orthographicSize * (1f - scroll * zoomSensitivity * 10f);
        _cam.orthographicSize = Mathf.Clamp(newSize, minZoom, maxZoom);

        // Move a câmera para que o ponto sob o cursor não se desloque.
        Vector3 worldAfter = CursorToWorld();
        transform.position += worldBefore - worldAfter;

        if (debugMode)
            Debug.Log($"[Camera] Novo orthoSize={_cam.orthographicSize:F3}  maxZoom={maxZoom:F3}  boardMax={ComputeBoardMaxZoom():F3}");
    }

    // ============================================================
    // PAN — botão direito (1) ou meio (2) pressionado + arrastar
    // ============================================================

    private void HandlePan()
    {
        bool justPressed = Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2);
        bool held = Input.GetMouseButton(1) || Input.GetMouseButton(2);
        bool released = Input.GetMouseButtonUp(1) || Input.GetMouseButtonUp(2);

        // Inicia o pan: registra o ponto de mundo sob o cursor.
        if (justPressed && !IsPointerOverUI())
        {
            _panAnchorWorld = CursorToWorld();
            _panning = true;

            if (debugMode)
                Debug.Log($"[Camera] Pan INICIADO  anchor={_panAnchorWorld}");
        }

        if (released)
        {
            _panning = false;

            if (debugMode)
                Debug.Log("[Camera] Pan FINALIZADO");
        }

        if (!_panning || !held) return;

        // Ponto de mundo atualmente sob o cursor.
        Vector3 worldNow = CursorToWorld();

        // Delta: mover a câmera para que worldNow "volte" ao anchor.
        Vector3 delta = (_panAnchorWorld - worldNow) * panSensitivity;
        transform.position += delta;

        // Recalcula o anchor com a câmera já na nova posição.
        // Sem isso, o próximo frame teria um delta errado (deriva).
        _panAnchorWorld = CursorToWorld();
    }

    // ============================================================
    // CLAMP — câmera dentro dos limites do tabuleiro
    // ============================================================

    private void ClampToBoardBounds()
    {
        Bounds b = GetBoardBounds();

        float halfH = _cam.orthographicSize;
        float halfW = _cam.orthographicSize * _cam.aspect;

        // Intervalo válido para o centro da câmera:
        //   cx ∈ [b.min.x + halfW,  b.max.x - halfW]
        //   cy ∈ [b.min.y + halfH,  b.max.y - halfH]
        float xMin = b.min.x + halfW;
        float xMax = b.max.x - halfW;
        float yMin = b.min.y + halfH;
        float yMax = b.max.y - halfH;

        // Se viewport > board em algum eixo → centraliza naquele eixo.
        float cx = (xMin < xMax) ? Mathf.Clamp(transform.position.x, xMin, xMax) : b.center.x;
        float cy = (yMin < yMax) ? Mathf.Clamp(transform.position.y, yMin, yMax) : b.center.y;

        transform.position = new Vector3(cx, cy, transform.position.z);
    }

    // ============================================================
    // Helpers privados
    // ============================================================

    /// <summary>
    /// Orthographic size em que a viewport passa a ser MAIOR que o board.
    /// Acima desse valor o clamp centraliza automaticamente — o board fica
    /// menor que a tela, mas ainda visível e sem revelar "infinito".
    /// Usado apenas para FocusOnActiveBoard (não como hard-limit de zoom-out).
    /// </summary>
    private float ComputeBoardMaxZoom()
    {
        Bounds b = GetBoardBounds();
        float aspect = Mathf.Max(_cam.aspect, 0.001f);
        float byH = b.extents.y;
        float byW = b.extents.x / aspect;
        return Mathf.Max(Mathf.Min(byH, byW), minZoom);
    }

    /// <summary>
    /// Converte a posição atual do cursor de tela para world-space no plano Z=0.
    /// </summary>
    private Vector3 CursorToWorld()
    {
        Vector3 pos = Input.mousePosition;
        pos.z = -transform.position.z; // distância câmera → plano Z=0
        return _cam.ScreenToWorldPoint(pos);
    }

    /// <summary>
    /// Retorna true se o cursor estiver sobre algum elemento da UI.
    /// Evita que zoom/pan disparem quando o usuário interage com o painel do GM.
    /// </summary>
    private bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    /// <summary>
    /// Retorna os bounds do tabuleiro ativo em world-space.
    /// SpriteRenderer.bounds já incorpora posição e escala do Transform,
    /// portanto o cálculo funciona para qualquer escala de mapa.
    /// </summary>
    private Bounds GetBoardBounds()
    {
        if (_mapCtrl != null && _mapCtrl.IsMapLoaded)
        {
            return _mapCtrl.MapBounds;
        }

        if (_defaultBoard != null)
            return _defaultBoard.GetBoardBounds();

        return new Bounds(Vector3.zero, new Vector3(24f, 14f, 1f));
    }

    // ============================================================
    // API Pública
    // ============================================================

    /// <summary>Valor mínimo de orthographicSize (zoom-in máximo).</summary>
    public float MinZoom => minZoom;

    /// <summary>Valor máximo de orthographicSize (zoom-out máximo configurável).</summary>
    public float MaxZoom => maxZoom;

    /// <summary>orthographicSize atual da câmera.</summary>
    public float CurrentZoom => _cam != null ? _cam.orthographicSize : 5f;

    /// <summary>
    /// Define o zoom (orthographicSize) diretamente, como se fosse o scroll do mouse.
    /// Clampeia dentro dos limites e reposiciona se necessário.
    /// Usado pelo slider de zoom da UI do GM.
    /// </summary>
    public void SetZoom(float orthoSize)
    {
        float clamped = Mathf.Clamp(orthoSize, minZoom, maxZoom);
        _cam.orthographicSize = clamped;
        ClampToBoardBounds();
    }

    /// <summary>
    /// Enquadra o board inteiro na viewport com margem visual.
    /// Chamado ao iniciar e ao carregar novo mapa.
    /// Também acionado pelo botão "Reset Zoom" da UI do GM.
    /// </summary>
    public void FocusOnActiveBoard()
    {
        Bounds b = GetBoardBounds();
        transform.position = new Vector3(b.center.x, b.center.y, transform.position.z);

        float boardFit = ComputeBoardMaxZoom();
        _cam.orthographicSize = Mathf.Clamp(boardFit * (1f - focusMargin), minZoom, maxZoom);

        ClampToBoardBounds();
    }

    /// <summary>Centraliza no board sem alterar o zoom atual.</summary>
    public void CenterOnActiveBoard()
    {
        Bounds b = GetBoardBounds();
        transform.position = new Vector3(b.center.x, b.center.y, transform.position.z);
        ClampToBoardBounds();
    }

    // ─── Evento: novo mapa carregado ─────────────────────────

    private void OnMapLoaded(Texture2D _) => StartCoroutine(FocusNextFrame());

    private IEnumerator FocusNextFrame()
    {
        yield return null; // aguarda 1 frame para SpriteRenderer atualizar os bounds
        FocusOnActiveBoard();
    }
}