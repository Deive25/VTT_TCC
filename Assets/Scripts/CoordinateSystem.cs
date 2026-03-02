// ============================================================
// CoordinateSystem.cs
// Sistema de coordenadas normalizadas do mapa.
//
//   (0,0) = canto inferior esquerdo do mapa
//   (1,1) = canto superior direito do mapa
//
// Conversões disponíveis:
//   WorldToNormalized   - Posição mundo → coordenada normalizada
//   NormalizedToWorld   - Coordenada normalizada → posição mundo
//   GetMouseNormalized  - Coordenada normalizada do mouse (null se fora do mapa)
// ============================================================
using UnityEngine;

/// <summary>
/// Responsável por todas as conversões de coordenadas do VTT.
/// Opera em 2D, ignorando o eixo Z.
/// </summary>
public class CoordinateSystem : MonoBehaviour
{
    // --------------------------------------------------------
    // Dependências
    // --------------------------------------------------------
    private MapController mapController;

    // --------------------------------------------------------
    // Unity Lifecycle
    // --------------------------------------------------------
    private void Awake()
    {
        mapController = FindFirstObjectByType<MapController>();

        if (mapController == null)
            Debug.LogError("[CoordinateSystem] MapController não encontrado na cena!");
    }

    // --------------------------------------------------------
    // API Pública
    // --------------------------------------------------------

    /// <summary>
    /// Converte uma posição no espaço de mundo para coordenadas normalizadas.
    /// </summary>
    /// <param name="worldPos">Posição no espaço de mundo 2D.</param>
    /// <returns>
    /// Coordenada normalizada (0-1, 0-1), ou null se fora dos bounds do mapa.
    /// </returns>
    public Vector2? WorldToNormalized(Vector2 worldPos)
    {
        if (mapController == null || !mapController.IsMapLoaded)
            return null;

        Bounds bounds = mapController.MapBounds;

        // Evita divisão por zero
        if (bounds.size.x < Mathf.Epsilon || bounds.size.y < Mathf.Epsilon)
            return null;

        float nx = (worldPos.x - bounds.min.x) / bounds.size.x;
        float ny = (worldPos.y - bounds.min.y) / bounds.size.y;

        // Retorna null se o ponto estiver fora dos limites do mapa
        if (nx < 0f || nx > 1f || ny < 0f || ny > 1f)
            return null;

        return new Vector2(nx, ny);
    }

    /// <summary>
    /// Converte coordenadas normalizadas para posição no espaço de mundo.
    /// </summary>
    /// <param name="normalized">Coordenada normalizada (0-1, 0-1).</param>
    /// <returns>Posição no espaço de mundo 2D.</returns>
    public Vector2 NormalizedToWorld(Vector2 normalized)
    {
        if (mapController == null || !mapController.IsMapLoaded)
            return Vector2.zero;

        Bounds bounds = mapController.MapBounds;

        float wx = bounds.min.x + normalized.x * bounds.size.x;
        float wy = bounds.min.y + normalized.y * bounds.size.y;

        return new Vector2(wx, wy);
    }

    /// <summary>
    /// Retorna a coordenada normalizada da posição atual do mouse sobre o mapa.
    /// </summary>
    /// <returns>Coordenada normalizada, ou null se o mouse estiver fora do mapa.</returns>
    public Vector2? GetMouseNormalized()
    {
        if (Camera.main == null) return null;

        Vector3 mouseScreenPos = Input.mousePosition;
        Vector3 mouseWorldPos  = Camera.main.ScreenToWorldPoint(mouseScreenPos);

        return WorldToNormalized(new Vector2(mouseWorldPos.x, mouseWorldPos.y));
    }

    /// <summary>
    /// Verifica se uma coordenada normalizada está dentro dos limites válidos.
    /// </summary>
    public bool IsWithinBounds(Vector2 normalized)
    {
        return normalized.x >= 0f && normalized.x <= 1f
            && normalized.y >= 0f && normalized.y <= 1f;
    }
}
