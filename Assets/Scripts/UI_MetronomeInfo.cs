using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_MetronomeInfo : MonoBehaviour
{
    public Metronome metronome;
    public TMP_Text bpmText;
    public Slider BPMSlider;

    private void Awake()
    {
        if(BPMSlider != null)
        {
            BPMSlider.onValueChanged.AddListener(OnBPMSliderChanged);
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        if (metronome == null)
        {
            Metronome.TryGetInstance(out metronome);
        }

        if (metronome == null)
        {
            Debug.LogError("Metronome instance is not found. Cannot initialize metronome info UI.");
            return;
        }

        metronome.UpdateInfo(MetronomeInfo.BPM, 120);

        if (BPMSlider != null)
        {
            BPMSlider.value = metronome.bpm;
        }

        UpdateMetronomeInfo();
    }

    private void OnBPMSliderChanged(float value)
    {
        if (metronome == null)
        {
            return;
        }

        metronome.UpdateInfo(MetronomeInfo.BPM, (int)value);
        UpdateMetronomeInfo();
    }

    private void UpdateMetronomeInfo()
    {
        if (bpmText == null || metronome == null)
        {
            return;
        }

        bpmText.text = metronome.bpm.ToString();
    }
}
