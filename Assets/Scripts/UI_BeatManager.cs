using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class UI_BeatManager : MonoBehaviour
{
    public Toggle pf_beatToggle;
    public Button btn_beatIncrease;
    public Button btn_beatDecrease;
    public Transform beatContainer;
    [SerializeField] private Color activeBeatColor = new Color(1f, 0.85f, 0.25f, 1f);
    [SerializeField] private float activeBeatDuration = 0.12f;

    private readonly List<Toggle> beatToggles = new List<Toggle>();
    private Metronome metronome;
    private Coroutine activeBeatCoroutine;
    private int activeBeatIndex = -1;
    private Color activeBeatOriginalColor;

    private void Awake()
    {
        btn_beatIncrease.onClick.AddListener(() => ChangeBeat(1));
        btn_beatDecrease.onClick.AddListener(() => ChangeBeat(-1));
    }
    private void ChangeBeat(int change)
    {
        if (metronome != null)
        {
            int newBeat = Mathf.Clamp(metronome.beats + change, Metronome.MinBeats, Metronome.MaxBeats);
            metronome.UpdateInfo(MetronomeInfo.Beat, newBeat);
        }
    }
    private void Start()
    {
        if (!Metronome.TryGetInstance(out metronome))
        {
            Debug.LogError("Metronome instance is not found. Please ensure that the Metronome script is attached to a GameObject in the scene.");
            return;
        }

        if (metronome != null)
        {
            metronome.OnBeat.AddListener(OnBeat);
            metronome.OnChangeBeat.AddListener(SettingBeat);
            metronome.UpdateInfo(MetronomeInfo.Beat, metronome.beats);
        }
    }
    private void OnDestroy()
    {
        if (metronome != null)
        {
            metronome.OnBeat.RemoveListener(OnBeat);
            metronome.OnChangeBeat.RemoveListener(SettingBeat);
        }
    }

    private void SettingBeat(int newBeat)
    {
        if (metronome == null || pf_beatToggle == null || beatContainer == null)
        {
            return;
        }

        ClearBeatToggles();
        UpdateBeatButtons(newBeat);

        for (int i = 0; i < newBeat; i++)
        {
            if (i >= metronome.BeatInfo.Count)
            {
                break;
            }

            Toggle toggle = Instantiate(pf_beatToggle, beatContainer);
            toggle.name = $"Accent Toggle {i + 1}";
            toggle.SetIsOnWithoutNotify(metronome.BeatInfo[i] == AccentType.Strong);

            int beatIndex = i;
            toggle.onValueChanged.AddListener(isOn =>
            {
                metronome.SetBeatAccent(beatIndex, isOn ? AccentType.Strong : AccentType.Weak);
            });

            beatToggles.Add(toggle);
        }
    }

    private void UpdateBeatButtons(int beatCount)
    {
        if (btn_beatDecrease != null)
        {
            btn_beatDecrease.interactable = beatCount > Metronome.MinBeats;
        }

        if (btn_beatIncrease != null)
        {
            btn_beatIncrease.interactable = beatCount < Metronome.MaxBeats;
        }
    }

    private void ClearBeatToggles()
    {
        StopActiveBeatHighlight();

        foreach (Toggle toggle in beatToggles)
        {
            if (toggle != null)
            {
                Destroy(toggle.gameObject);
            }
        }

        beatToggles.Clear();
    }

    private void OnBeat(int beatIndex, AccentType accentType)
    {
        FlashBeatToggle(beatIndex);
    }

    private void FlashBeatToggle(int beatIndex)
    {
        if (beatIndex < 0 || beatIndex >= beatToggles.Count)
        {
            return;
        }

        StopActiveBeatHighlight();

        Toggle toggle = beatToggles[beatIndex];
        if (toggle == null || toggle.targetGraphic == null)
        {
            return;
        }

        activeBeatIndex = beatIndex;
        activeBeatOriginalColor = toggle.targetGraphic.color;
        toggle.targetGraphic.color = activeBeatColor;
        activeBeatCoroutine = StartCoroutine(RestoreBeatToggleColor());
    }

    private IEnumerator RestoreBeatToggleColor()
    {
        yield return new WaitForSeconds(activeBeatDuration);
        RestoreActiveBeatColor();
        activeBeatCoroutine = null;
    }

    private void StopActiveBeatHighlight()
    {
        if (activeBeatCoroutine != null)
        {
            StopCoroutine(activeBeatCoroutine);
            activeBeatCoroutine = null;
        }

        RestoreActiveBeatColor();
    }

    private void RestoreActiveBeatColor()
    {
        if (activeBeatIndex < 0 || activeBeatIndex >= beatToggles.Count)
        {
            activeBeatIndex = -1;
            return;
        }

        Toggle toggle = beatToggles[activeBeatIndex];
        if (toggle != null && toggle.targetGraphic != null)
        {
            toggle.targetGraphic.color = activeBeatOriginalColor;
        }

        activeBeatIndex = -1;
    }
}
