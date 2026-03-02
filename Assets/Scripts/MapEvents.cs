// ============================================================
// MapEvents.cs
// Sistema de eventos central do VTT (Event Bus pattern)
// Desacopla completamente a UI da lógica do mapa.
// ============================================================
using System;
using UnityEngine;

/// <summary>
/// Hub central de eventos do sistema de mapa.
/// Todos os componentes se comunicam exclusivamente por aqui.
/// </summary>
public static class MapEvents
{
    // --- Eventos de Mapa ---

    /// <summary>Disparado quando uma nova textura de mapa é carregada.</summary>
    public static event Action<Texture2D> OnMapLoaded;

    /// <summary>Disparado quando a escala do mapa é alterada (via slider ou código).</summary>
    public static event Action<float> OnScaleChangeRequested;

    /// <summary>Requisição para centralizar o mapa na câmera.</summary>
    public static event Action OnCenterMapRequested;

    /// <summary>Requisição para resetar o zoom para o valor padrão (fit-to-screen).</summary>
    public static event Action OnResetZoomRequested;

    /// <summary>Disparado pelo MapController quando qualquer informação do mapa muda.</summary>
    public static event Action<MapInfo> OnMapInfoUpdated;

    // --- Disparadores (FireXxx) ---

    public static void FireMapLoaded(Texture2D tex) => OnMapLoaded?.Invoke(tex);
    public static void FireScaleChangeRequested(float scale) => OnScaleChangeRequested?.Invoke(scale);
    public static void FireCenterMapRequested() => OnCenterMapRequested?.Invoke();
    public static void FireResetZoomRequested() => OnResetZoomRequested?.Invoke();
    public static void FireMapInfoUpdated(MapInfo info) => OnMapInfoUpdated?.Invoke(info);
}

/// <summary>
/// Estrutura que carrega informações do estado atual do mapa.
/// Usada pelo painel do Mestre para exibir dados.
/// </summary>
[Serializable]
public struct MapInfo
{
    /// <summary>Largura original da imagem em pixels.</summary>
    public int widthPx;

    /// <summary>Altura original da imagem em pixels.</summary>
    public int heightPx;

    /// <summary>Escala atual aplicada ao sprite do mapa.</summary>
    public float scale;

    /// <summary>Coordenada normalizada do mouse sobre o mapa. Null se fora do mapa.</summary>
    public Vector2? mouseNormalized;

    /// <summary>True se um mapa foi carregado.</summary>
    public bool isLoaded;
}
