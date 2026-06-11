using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CameraController : MonoBehaviour
{
    [Header("Limites de Zoom")]
    public float minZoom = 2f;
    public float maxZoom = 20f;
    public float zoomSpeed = 2f;

   public float MinZoom => minZoom;
    public float MaxZoom => maxZoom;

    [Header("Velocidade de Pan")]
    public float panSpeed = 1f;

    [Header("Foco Automatico")]
    [Tooltip("Margem visual ao focar no tabuleiro (0 = nenhuma, 0.1 = 10%)")]
    public float focusMargin = 0.05f;

    [Header("Constricoes de UI (Em Pixels)")]
    public float leftPanelWidth = 240f;
    public float rightPanelWidth = 276f;
    private Canvas _mainCanvas;

    private Camera _cam;
    private Vector3 _dragOrigin;
    private bool _isDragging = false;
    private DefaultBoardRenderer _defaultBoard;

    public float CurrentZoom => _cam != null ? _cam.orthographicSize : 0f;

    private void Awake()
    {
        _cam = GetComponent<Camera>();
        if (_cam == null) _cam = Camera.main;
        _defaultBoard = FindAnyObjectByType<DefaultBoardRenderer>();
    }

    private void Start()
    {
       GMUIController gmUI = FindAnyObjectByType<GMUIController>();
        if (gmUI != null) _mainCanvas = gmUI.GetComponentInParent<Canvas>();

        AdjustCameraViewport();
        FocusOnActiveBoard();
    }

    private void OnEnable()
    {
        MapEvents.OnMapLoaded += OnMapLoaded;
        MapEvents.OnActiveLayerChanged += OnLayerChanged;
    }

    private void OnDisable()
    {
        MapEvents.OnMapLoaded -= OnMapLoaded;
        MapEvents.OnActiveLayerChanged -= OnLayerChanged;
    }

    private void Update()
    {
        AdjustCameraViewport();
        HandleZoom();
        HandlePan();
    }

    // Mantem a camera do mapa dentro da area central, sem invadir os paineis de UI.
    private void AdjustCameraViewport()
    {
        if (_mainCanvas == null || _cam == null) return;

        float scale = _mainCanvas.scaleFactor;
        if (scale <= 0) scale = 1f;

        float leftPx = leftPanelWidth * scale;
        float rightPx = rightPanelWidth * scale;

        float rectX = leftPx / Screen.width;
        float rectW = (Screen.width - leftPx - rightPx) / Screen.width;

       _cam.rect = new Rect(rectX, 0f, rectW, 1f);
    }

    private void HandleZoom()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            float newSize = _cam.orthographicSize - (scroll * zoomSpeed);
            SetZoom(newSize);
        }
    }

    private void HandlePan()
    {
        if (Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            _dragOrigin = _cam.ScreenToWorldPoint(Input.mousePosition);
            _isDragging = true;
        }

        if (Input.GetMouseButtonUp(1) || Input.GetMouseButtonUp(2)) _isDragging = false;

        if (_isDragging && (Input.GetMouseButton(1) || Input.GetMouseButton(2)))
        {
            Vector3 currentPos = _cam.ScreenToWorldPoint(Input.mousePosition);
            Vector3 diff = _dragOrigin - currentPos;
            transform.position += diff;

            ClampToBoardBounds();
        }
    }

    public void SetZoom(float orthoSize)
    {
        float clamped = Mathf.Clamp(orthoSize, minZoom, maxZoom);
        _cam.orthographicSize = clamped;
        ClampToBoardBounds();
    }

    public void FocusOnActiveBoard()
    {
        Bounds b = GetBoardBounds();
        transform.position = new Vector3(b.center.x, b.center.y, transform.position.z);

        float boardFit = ComputeBoardMaxZoom();
        _cam.orthographicSize = Mathf.Clamp(boardFit * (1f - focusMargin), minZoom, maxZoom);

        ClampToBoardBounds();
    }

    public void CenterOnActiveBoard()
    {
        Bounds b = GetBoardBounds();
        transform.position = new Vector3(b.center.x, b.center.y, transform.position.z);
        ClampToBoardBounds();
    }

    private void OnMapLoaded(Texture2D tex) { FocusOnActiveBoard(); }
    private void OnLayerChanged(string layerId) { FocusOnActiveBoard(); }

    private Bounds GetBoardBounds()
    {
        if (LayerManager.Instance != null)
        {
            var activeLayer = LayerManager.Instance.GetActiveLayer();
            if (activeLayer != null && activeLayer.renderer != null) return activeLayer.renderer.bounds;
        }
        if (_defaultBoard != null) return _defaultBoard.GetBoardBounds();
        return new Bounds(Vector3.zero, new Vector3(24f, 14f, 1f));
    }

    private float ComputeBoardMaxZoom()
    {
        Bounds b = GetBoardBounds();
        float screenRatio = _cam.aspect;
        float targetRatio = b.size.x / b.size.y;

        if (screenRatio >= targetRatio) return b.size.y / 2f;
        else return (b.size.y / 2f) * (targetRatio / screenRatio);
    }

    private void ClampToBoardBounds()
    {
        Bounds b = GetBoardBounds();
        float camHeight = _cam.orthographicSize;
        float camWidth = camHeight * _cam.aspect;

        float minX = b.min.x + camWidth;
        float maxX = b.max.x - camWidth;
        float minY = b.min.y + camHeight;
        float maxY = b.max.y - camHeight;

        Vector3 pos = transform.position;

        if (maxX < minX) pos.x = b.center.x; else pos.x = Mathf.Clamp(pos.x, minX, maxX);
        if (maxY < minY) pos.y = b.center.y; else pos.y = Mathf.Clamp(pos.y, minY, maxY);

        transform.position = pos;
    }
}
