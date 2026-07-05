using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class UI_Mp3Visualizer : MonoBehaviour
{
    [SerializeField] private LocalMp3Player mp3Player;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private int sampleCount = 1024;
    [SerializeField] private float visualizerScale = 900f;
    [SerializeField] private float minBarScale = 0.05f;
    [SerializeField] private float maxBarScale = 1.35f;
    [SerializeField] private float smoothing = 12f;
    [SerializeField] private float frequencyCurve = 1.15f;
    [SerializeField] private float highFrequencyBoost = 1.8f;
    [SerializeField] private GameObject barPrefab;
    [SerializeField] private int numberOfBars = 32;

    private float[] spectrumData;
    private float[] listenerSpectrumData;
    private float[] outputData;
    private float[] webGLBarData;
    private readonly List<RectTransform> bars = new List<RectTransform>();

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern float GetLocalMp3Spectrum(float[] buffer, int length);
#endif

    private void Awake()
    {
        sampleCount = Mathf.ClosestPowerOfTwo(Mathf.Max(64, sampleCount));
        spectrumData = new float[sampleCount];
        listenerSpectrumData = new float[spectrumData.Length];
        outputData = new float[spectrumData.Length];
    }

    private void Start()
    {
        ResolveAudioSource();
        CreateBars();
    }

    private void Update()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        UpdateWebGLBars();
#else
        if (!ResolveAudioSource() || !audioSource.isPlaying)
        {
            DecayBars();
            return;
        }

        float peak = ReadSpectrumData();
        if (peak <= 0.000001f)
        {
            peak = ReadOutputData();
        }

        if (peak <= 0.000001f)
        {
            DecayBars();
            return;
        }

        UpdateBars();
#endif
    }

    private float ReadSpectrumData()
    {
        audioSource.GetSpectrumData(spectrumData, 0, FFTWindow.BlackmanHarris);
        float peak = GetPeak(spectrumData);
        if (peak > 0.000001f)
        {
            return peak;
        }

        AudioListener.GetSpectrumData(listenerSpectrumData, 0, FFTWindow.BlackmanHarris);
        float listenerPeak = GetPeak(listenerSpectrumData);
        if (listenerPeak > 0.000001f)
        {
            (spectrumData, listenerSpectrumData) = (listenerSpectrumData, spectrumData);
            return listenerPeak;
        }

        return 0f;
    }

    private float ReadOutputData()
    {
        audioSource.GetOutputData(outputData, 0);
        float peak = GetPeak(outputData);
        if (peak <= 0.000001f)
        {
            return 0f;
        }

        int outputGroupSize = Mathf.Max(1, outputData.Length / spectrumData.Length);
        for (int i = 0; i < spectrumData.Length; i++)
        {
            float sum = 0f;
            int start = i * outputGroupSize;
            int end = Mathf.Min(start + outputGroupSize, outputData.Length);

            for (int j = start; j < end; j++)
            {
                sum += Mathf.Abs(outputData[j]);
            }

            spectrumData[i] = sum / Mathf.Max(1, end - start);
        }

        return peak;
    }

    private bool ResolveAudioSource()
    {
        if (audioSource != null)
        {
            return true;
        }

        if (mp3Player == null)
        {
            LocalMp3Player.TryGetInstance(out mp3Player);
        }

        if (mp3Player == null)
        {
            return false;
        }

        audioSource = mp3Player.AudioSource;
        return audioSource != null;
    }

    private void CreateBars()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        bars.Clear();

        if (barPrefab == null)
        {
            Debug.LogError("Spectrum bar prefab is not assigned.");
            return;
        }

        for (int i = 0; i < numberOfBars; i++)
        {
            GameObject bar = Instantiate(barPrefab, transform);
            RectTransform barRect = bar.GetComponent<RectTransform>();
            Vector3 scale = barRect.localScale;
            scale.y = minBarScale;
            barRect.localScale = scale;

            bars.Add(barRect);
        }

        webGLBarData = new float[bars.Count];
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    private void UpdateWebGLBars()
    {
        if (bars.Count == 0)
        {
            return;
        }

        if (mp3Player == null)
        {
            LocalMp3Player.TryGetInstance(out mp3Player);
        }

        if (mp3Player == null || !mp3Player.IsPlaying)
        {
            DecayBars();
            return;
        }

        float peak = GetLocalMp3Spectrum(webGLBarData, webGLBarData.Length);
        if (peak <= 0.000001f)
        {
            DecayBars();
            return;
        }

        for (int i = 0; i < bars.Count; i++)
        {
            float targetScale = Mathf.Clamp(webGLBarData[i] * visualizerScale, minBarScale, maxBarScale);
            Vector3 scale = bars[i].localScale;
            scale.y = Mathf.Lerp(scale.y, targetScale, Time.unscaledDeltaTime * smoothing);
            bars[i].localScale = scale;
        }
    }
#endif

    private void UpdateBars()
    {
        if (bars.Count == 0)
        {
            return;
        }

        int usableSamples = spectrumData.Length / 2;

        for (int i = 0; i < bars.Count; i++)
        {
            int start = Mathf.FloorToInt(Mathf.Pow((float)i / bars.Count, frequencyCurve) * usableSamples);
            int end = Mathf.FloorToInt(Mathf.Pow((float)(i + 1) / bars.Count, frequencyCurve) * usableSamples);
            end = Mathf.Clamp(end, start + 1, usableSamples);

            float sum = 0f;
            for (int sampleIndex = start; sampleIndex < end; sampleIndex++)
            {
                sum += spectrumData[sampleIndex];
            }

            float average = sum / (end - start);
            float bandPosition = bars.Count <= 1 ? 0f : (float)i / (bars.Count - 1);
            float bandGain = Mathf.Lerp(1f, highFrequencyBoost, bandPosition);
            float targetScale = Mathf.Clamp(average * bandGain * visualizerScale, minBarScale, maxBarScale);
            Vector3 scale = bars[i].localScale;
            scale.y = Mathf.Lerp(scale.y, targetScale, Time.unscaledDeltaTime * smoothing);
            bars[i].localScale = scale;
        }
    }

    private void DecayBars()
    {
        for (int i = 0; i < bars.Count; i++)
        {
            Vector3 scale = bars[i].localScale;
            scale.y = Mathf.Lerp(scale.y, minBarScale, Time.unscaledDeltaTime * smoothing);
            bars[i].localScale = scale;
        }
    }

    private static float GetPeak(float[] data)
    {
        float peak = 0f;
        for (int i = 0; i < data.Length; i++)
        {
            peak = Mathf.Max(peak, data[i]);
        }

        return peak;
    }
}
