// ============================================================
// MapEvents.cs
// ============================================================
using System;
using UnityEngine;

public static class MapEvents
{
    // --- Eventos Base do Mapa ---
    public static event Action<Texture2D> OnMapLoaded;
    public static event Action<float> OnScaleChangeRequested;
    public static event Action OnCenterMapRequested;
    public static event Action OnResetZoomRequested;
    public static event Action<MapInfo> OnMapInfoUpdated;

    // --- Eventos de Camadas (NOVO) ---
    public static event Action OnLayersChanged; // Disparado quando adiciona, remove ou reordena
    public static event Action<string> OnActiveLayerChanged; // Disparado quando seleciona uma camada

    // --- Disparadores ---
    public static void FireMapLoaded(Texture2D tex) => OnMapLoaded?.Invoke(tex);
    public static void FireScaleChangeRequested(float scale) => OnScaleChangeRequested?.Invoke(scale);
    public static void FireCenterMapRequested() => OnCenterMapRequested?.Invoke();
    public static void FireResetZoomRequested() => OnResetZoomRequested?.Invoke();
    public static void FireMapInfoUpdated(MapInfo info) => OnMapInfoUpdated?.Invoke(info);

    public static void FireLayersChanged() => OnLayersChanged?.Invoke();
    public static void FireActiveLayerChanged(string layerId) => OnActiveLayerChanged?.Invoke(layerId);
}

[Serializable]
public struct MapInfo
{
    public int widthPx;
    public int heightPx;
    public float scale;
    public Vector2? mouseNormalized;
    public bool isLoaded;
}