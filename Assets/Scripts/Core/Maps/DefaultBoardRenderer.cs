using UnityEngine;

public class DefaultBoardRenderer : MonoBehaviour
{
    [Header("Tamanho do Tabuleiro")]
    [Tooltip("Largura do tabuleiro em unidades Unity.")]
    [SerializeField] private float boardWidth = 24f;

    [Tooltip("Altura do tabuleiro em unidades Unity.")]
    [SerializeField] private float boardHeight = 14f;

    [Header("Visual")]
    [SerializeField] private Color backgroundColor = new Color(0.12f, 0.14f, 0.18f, 1f);
    [SerializeField] private Color gridColor = new Color(0.22f, 0.26f, 0.32f, 1f);
    [SerializeField] private Color borderColor = new Color(0.35f, 0.45f, 0.60f, 1f);

    [Tooltip("Numero de celulas da grade (horizontal e vertical).")]
    [SerializeField] private int gridCellCount = 24;

    private GameObject boardRoot;
    private bool boardVisible = true;

    private void Awake()
    {
        BuildBoard();
    }

    private void OnEnable()
    {
        MapEvents.OnMapLoaded += HandleMapLoaded;
    }

    private void OnDisable()
    {
        MapEvents.OnMapLoaded -= HandleMapLoaded;
    }

    private void HandleMapLoaded(Texture2D tex)
    {
        SetVisible(false);
    }

    private void BuildBoard()
    {
        boardRoot = new GameObject("DefaultBoard_Root");
        boardRoot.transform.SetParent(transform, false);
        boardRoot.transform.localPosition = Vector3.zero;

        CreateBackground();
        CreateGrid();
        CreateBorder();
    }

    private void CreateBackground()
    {
        GameObject bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
        bg.name = "Board_Background";
        bg.transform.SetParent(boardRoot.transform, false);
        bg.transform.localPosition = new Vector3(0f, 0f, 0.1f);
        bg.transform.localScale = new Vector3(boardWidth, boardHeight, 1f);

        Destroy(bg.GetComponent<MeshCollider>());

        MeshRenderer mr = bg.GetComponent<MeshRenderer>();
        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = backgroundColor;
        mr.material = mat;
        mr.sortingOrder = -20;
    }

    private void CreateGrid()
    {
        GameObject gridParent = new GameObject("Board_Grid");
        gridParent.transform.SetParent(boardRoot.transform, false);
        gridParent.transform.localPosition = Vector3.zero;

        float halfW = boardWidth / 2f;
        float halfH = boardHeight / 2f;
        float cellW = boardWidth / gridCellCount;
        float cellH = boardHeight / gridCellCount;
        float lineZ = -0.01f;

        for (int i = 1; i < gridCellCount; i++)
        {
            float x = -halfW + i * cellW;
            CreateLine(gridParent, $"GridV_{i}",
                new Vector3(x, -halfH, lineZ),
                new Vector3(x, halfH, lineZ),
                gridColor, 0.015f, -19);
        }

        for (int i = 1; i < gridCellCount; i++)
        {
            float y = -halfH + i * cellH;
            CreateLine(gridParent, $"GridH_{i}",
                new Vector3(-halfW, y, lineZ),
                new Vector3(halfW, y, lineZ),
                gridColor, 0.015f, -19);
        }
    }

    private void CreateBorder()
    {
        GameObject borderParent = new GameObject("Board_Border");
        borderParent.transform.SetParent(boardRoot.transform, false);
        borderParent.transform.localPosition = Vector3.zero;

        float halfW = boardWidth / 2f;
        float halfH = boardHeight / 2f;
        float z = -0.02f;
        float w = 0.06f;
        int order = -18;

        CreateLine(borderParent, "Border_Top",
            new Vector3(-halfW, halfH, z), new Vector3(halfW, halfH, z),
            borderColor, w, order);

        CreateLine(borderParent, "Border_Bottom",
            new Vector3(-halfW, -halfH, z), new Vector3(halfW, -halfH, z),
            borderColor, w, order);

        CreateLine(borderParent, "Border_Left",
            new Vector3(-halfW, -halfH, z), new Vector3(-halfW, halfH, z),
            borderColor, w, order);

        CreateLine(borderParent, "Border_Right",
            new Vector3(halfW, -halfH, z), new Vector3(halfW, halfH, z),
            borderColor, w, order);
    }

    private void CreateLine(GameObject parent, string lineName,
                            Vector3 start, Vector3 end,
                            Color color, float width, int sortOrder)
    {
        GameObject lineObj = new GameObject(lineName);
        lineObj.transform.SetParent(parent.transform, false);
        lineObj.transform.localPosition = Vector3.zero;
        lineObj.transform.localScale = Vector3.one;

        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.positionCount = 2;
        lr.startColor = color;
        lr.endColor = color;
        lr.startWidth = width;
        lr.endWidth = width;
        lr.sortingOrder = sortOrder;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.material.color = color;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
    }

    public Vector2 GetBoardSize() => new Vector2(boardWidth, boardHeight);

    public Bounds GetBoardBounds()
    {
        Vector3 worldCenter = boardRoot != null
            ? boardRoot.transform.position
            : Vector3.zero;

        return new Bounds(worldCenter, new Vector3(boardWidth, boardHeight, 1f));
    }

    public void SetVisible(bool visible)
    {
        boardVisible = visible;
        if (boardRoot != null)
            boardRoot.SetActive(visible);
    }
}
