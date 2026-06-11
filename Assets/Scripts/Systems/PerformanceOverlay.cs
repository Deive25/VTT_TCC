using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using UnityEngine;

public class PerformanceOverlay : MonoBehaviour
{
    [Header("Overlay")]
    public bool showOverlay = true;
    public KeyCode toggleKey = KeyCode.F3;

    [Header("Logging")]
    public bool writeCsv = true;
    public float sampleIntervalSeconds = 0.5f; // coleta a cada 0.5s
    public string csvFileName = "vtt_metrics.csv";

    // Dependência opcional: se existir KinectManager, vamos ler métricas dele
    private KinectManager _kinect;

    private float _accumTime;
    private int _frames;
    private float _fps;
    private float _frameMs;

    private float _nextSampleTime;
    private StreamWriter _writer;
    private string _csvPath;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        _kinect = FindFirstObjectByType<KinectManager>();

        if (writeCsv)
        {
            _csvPath = Path.Combine(Application.persistentDataPath, csvFileName);
            bool newFile = !File.Exists(_csvPath);
            _writer = new StreamWriter(_csvPath, append: true);

            if (newFile)
            {
                _writer.WriteLine("timestamp_iso,fps,frame_ms,track_ms,track_hz,tracked_count");
                _writer.Flush();
            }

            UnityEngine.Debug.Log($"[Perf] CSV em: {_csvPath}");
        }

        _nextSampleTime = Time.unscaledTime + sampleIntervalSeconds;
    }

    private void OnDestroy()
    {
        _writer?.Flush();
        _writer?.Dispose();
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            showOverlay = !showOverlay;

        // FPS (usando unscaledDeltaTime para não depender de TimeScale)
        float dt = Time.unscaledDeltaTime;
        _accumTime += dt;
        _frames++;

        if (_accumTime >= 0.5f)
        {
            _fps = _frames / _accumTime;
            _frameMs = (_accumTime / Mathf.Max(1, _frames)) * 1000f;
            _accumTime = 0f;
            _frames = 0;
        }

        // Log periódico
        if (writeCsv && Time.unscaledTime >= _nextSampleTime)
        {
            _nextSampleTime = Time.unscaledTime + sampleIntervalSeconds;

            double trackMs = _kinect != null ? _kinect.LastTrackingStepMs : double.NaN;
            double trackHz = (!double.IsNaN(trackMs) && trackMs > 0.0) ? (1000.0 / trackMs) : double.NaN;
            int trackedCount = _kinect != null ? _kinect.LastTrackedCount : -1;

            string ts = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            string line =
                $"{ts}," +
                $"{_fps.ToString("F2", CultureInfo.InvariantCulture)}," +
                $"{_frameMs.ToString("F2", CultureInfo.InvariantCulture)}," +
                $"{(double.IsNaN(trackMs) ? "" : trackMs.ToString("F3", CultureInfo.InvariantCulture))}," +
                $"{(double.IsNaN(trackHz) ? "" : trackHz.ToString("F2", CultureInfo.InvariantCulture))}," +
                $"{trackedCount}";

            _writer.WriteLine(line);
            _writer.Flush();
        }

        // atualizar referência caso cena recarregue
        if (_kinect == null)
            _kinect = FindFirstObjectByType<KinectManager>();
    }

    private void OnGUI()
    {
        if (!showOverlay) return;

        GUI.depth = 0;
        var style = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.UpperLeft,
            fontSize = 14
        };

        double trackMs = _kinect != null ? _kinect.LastTrackingStepMs : double.NaN;
        double trackHz = (!double.IsNaN(trackMs) && trackMs > 0.0) ? (1000.0 / trackMs) : double.NaN;
        int trackedCount = _kinect != null ? _kinect.LastTrackedCount : -1;

        string text =
            $"VTT Diagnostics (F3 toggle)\n" +
            $"FPS: {_fps:F1} | Frame: {_frameMs:F1} ms\n" +
            $"Tracking: {(double.IsNaN(trackMs) ? "N/A" : trackMs.ToString("F2"))} ms | " +
            $"Hz: {(double.IsNaN(trackHz) ? "N/A" : trackHz.ToString("F1"))}\n" +
            $"Tracked pieces: {trackedCount}\n" +
            $"CSV: {(writeCsv ? Application.persistentDataPath : "off")}";

        GUILayout.BeginArea(new Rect(10, 10, 420, 110));
        GUILayout.Box(text, style);
        GUILayout.EndArea();
    }
}
