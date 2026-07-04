using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


public enum MetronomeState
{
    Stopped,
    Playing,
    Paused
}
public enum MetronomeInfo
{
    BPM,
    Beat,
}
public enum AccentType
{
    None,
    Weak,
    Strong
}

public class Metronome : Singleton<Metronome>
{
    public const int MinBeats = 1;
    public const int MaxBeats = 12;

    public int bpm { get; private set; } = 120;
    public int beats { get; private set; } = 4;
    public MetronomeState State { get; private set; } = MetronomeState.Stopped;

    private float interval = 0;
    private float timer = 0;

    private int currentBeatIndex = 0;

    public UnityEvent<int, AccentType> OnBeat = new UnityEvent<int, AccentType>();

    public UnityEvent<int> OnChangeBeat = new UnityEvent<int>();

    public List<AccentType> BeatInfo = new List<AccentType>();

    private void OnValidate()
    {
        if (bpm <= 0)
        {
            Debug.LogError("BPM must be greater than 0.");
            bpm = 120; // Default to 120 BPM if invalid
        }
    }

    protected override void Awake()
    {
        base.Awake();
    }
    private void Start()
    {
        UpdateInterval();
        SyncAccentTypes();
    }

    void Update()
    {
        if (State != MetronomeState.Playing)
        {
            return;
        }

        timer += Time.deltaTime;
        if (timer >= interval)
        {
            timer -= interval;
            Beat();
        }
    }
    private void UpdateInterval()
    {
        interval = 60f / bpm;
    }
    private void Beat()
    {
        if (BeatInfo.Count == 0)
        {
            SyncAccentTypes();
        }

        if (BeatInfo.Count == 0)
        {
            return;
        }

        if (currentBeatIndex >= BeatInfo.Count)
        {
            currentBeatIndex = 0;
        }

        AccentType currentAccent = BeatInfo[currentBeatIndex];
        OnBeat?.Invoke(currentBeatIndex, currentAccent);

        print($"Beat {currentBeatIndex + 1}: {currentAccent}");

        currentBeatIndex = (currentBeatIndex + 1) % BeatInfo.Count;
    }

    public void UpdateInfo(MetronomeInfo infoType, int value)
    {
        switch (infoType)
        {
            case MetronomeInfo.BPM:
                bpm = value;
                UpdateInterval();
                break;
            case MetronomeInfo.Beat:
                beats = Mathf.Clamp(value, MinBeats, MaxBeats);
                SyncAccentTypes();
                OnChangeBeat?.Invoke(beats);
                break;
            default:
                Debug.LogError("Invalid MetronomeInfo type.");
                break;
        }
    }

    public void SetBeatAccent(int beatIndex, AccentType accentType)
    {
        if (beatIndex < 0 || beatIndex >= BeatInfo.Count)
        {
            return;
        }

        BeatInfo[beatIndex] = accentType == AccentType.None ? AccentType.Weak : accentType;
    //    ResetBeatSequence();
    }

    public void Play()
    {
        if (State == MetronomeState.Playing)
        {
            return;
        }

        if (State == MetronomeState.Stopped)
        {
            ResetBeatSequence();
        }

        timer = 0f;
        State = MetronomeState.Playing;
        Beat();
        timer = 0f;
    }

    public void Pause()
    {
        if (State == MetronomeState.Playing)
        {
            State = MetronomeState.Paused;
        }
    }

    public void Stop()
    {
        State = MetronomeState.Stopped;
        timer = 0f;
        ResetBeatSequence();
    }

    private void SyncAccentTypes()
    {
        while (BeatInfo.Count < beats)
        {
            BeatInfo.Add(BeatInfo.Count == 0 ? AccentType.Strong : AccentType.Weak);
        }

        while (BeatInfo.Count > beats)
        {
            BeatInfo.RemoveAt(BeatInfo.Count - 1);
        }

        for (int i = 0; i < BeatInfo.Count; i++)
        {
            if (BeatInfo[i] == AccentType.None)
            {
                BeatInfo[i] = i == 0 ? AccentType.Strong : AccentType.Weak;
            }
        }

        ResetBeatSequence();
    }

    private void ResetBeatSequence()
    {
        currentBeatIndex = 0;
    }
}
