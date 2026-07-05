using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_TapTempo : MonoBehaviour
{
    [SerializeField] private Button tapButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private TMP_Text bpmText;
    [SerializeField] private TMP_Text tapCountText;
    [SerializeField] private int minBpm = 40;
    [SerializeField] private int maxBpm = 240;
    [SerializeField] private int maxTapSamples = 8;
    [SerializeField] private double resetAfterSeconds = 2.5d;
    [SerializeField] private bool applyToMetronome = true;

    private readonly List<double> tapTimes = new List<double>();
    private Metronome metronome;

    private void Awake()
    {
        if (tapButton != null)
        {
            tapButton.onClick.AddListener(Tap);
        }

        if (resetButton != null)
        {
            resetButton.onClick.AddListener(ResetTaps);
        }
    }

    private void Start()
    {
        Metronome.TryGetInstance(out metronome);
        UpdateView(0);
    }

    private void OnDestroy()
    {
        if (tapButton != null)
        {
            tapButton.onClick.RemoveListener(Tap);
        }

        if (resetButton != null)
        {
            resetButton.onClick.RemoveListener(ResetTaps);
        }
    }

    public void Tap()
    {
        double now = Time.unscaledTimeAsDouble;

        // ^1 = tapTimes.count - 1
        if (tapTimes.Count > 0 && now - tapTimes[^1] > resetAfterSeconds)
        {
            tapTimes.Clear();
        }

        tapTimes.Add(now);

        while (tapTimes.Count > maxTapSamples)
        {
            tapTimes.RemoveAt(0);
        }

        int bpm = CalculateBpm();
        if (bpm > 0 && applyToMetronome && metronome != null)
        {
            metronome.UpdateInfo(MetronomeInfo.BPM, bpm);
        }

        UpdateView(bpm);
    }

    public void ResetTaps()
    {
        tapTimes.Clear();
        UpdateView(0);
    }

    private int CalculateBpm()
    {
        if (tapTimes.Count < 2)
        {
            return 0;
        }

        double totalInterval = 0d;
        for (int i = 1; i < tapTimes.Count; i++)
        {
            totalInterval += tapTimes[i] - tapTimes[i - 1];
        }

        double averageInterval = totalInterval / (tapTimes.Count - 1);
        if (averageInterval <= 0d)
        {
            return 0;
        }

        int bpm = Mathf.RoundToInt((float)(60d / averageInterval));
        return Mathf.Clamp(bpm, minBpm, maxBpm);
    }

    private void UpdateView(int bpm)
    {
        if (bpmText != null)
        {
            bpmText.text = bpm > 0 ? bpm.ToString() : "--";
        }

        if (tapCountText != null)
        {
            tapCountText.text = tapTimes.Count.ToString();
        }
    }
}
