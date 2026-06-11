using System;
using UnityEngine;

public static class MapEvents
{
    public static event Action<Texture2D> OnMapLoaded;
    public static event Action<float> OnScaleChangeRequested;
    public static event Action OnCenterMapRequested;
    public static event Action OnResetZoomRequested;
    public static event Action<MapInfo> OnMapInfoUpdated;

    public static event Action OnLayersChanged;
    public static event Action<string> OnActiveLayerChanged;

    public static void FireMapLoaded(Texture2D tex) => OnMapLoaded?.Invoke(tex);
    public static void FireScaleChangeRequested(float scale) => OnScaleChangeRequested?.Invoke(scale);
    public static void FireCenterMapRequested() => OnCenterMapRequested?.Invoke();
    public static void FireResetZoomRequested() => OnResetZoomRequested?.Invoke();
    public static void FireMapInfoUpdated(MapInfo info) => OnMapInfoUpdated?.Invoke(info);

    public static void FireLayersChanged() => OnLayersChanged?.Invoke();
    public static void FireActiveLayerChanged(string layerId) => OnActiveLayerChanged?.Invoke(layerId);
}
