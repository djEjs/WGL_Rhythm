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
    public const int MaxBeats = 8;

    public int bpm { get; private set; } = 120;
    public int beats { get; private set; } = 4;
    public MetronomeState State { get; private set; } = MetronomeState.Stopped;

    private double interval = 0;
    private double nextBeatTime = 0;

    private int currentBeatIndex = 0;

    public UnityEvent<int, AccentType> OnBeat = new UnityEvent<int, AccentType>();

    public UnityEvent<int> OnChangeBPM = new UnityEvent<int>();

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

        double now = AudioSettings.dspTime;
        int processedBeats = 0;

        while (now >= nextBeatTime && processedBeats < BeatInfo.Count)
        {
            Beat();
            nextBeatTime += interval;
            processedBeats++;
        }

        if (processedBeats >= BeatInfo.Count && now >= nextBeatTime)
        {
            nextBeatTime = now + interval;
        }
    }
    private void UpdateInterval()
    {
        interval = 60.0 / bpm;

        if (State == MetronomeState.Playing)
        {
            nextBeatTime = AudioSettings.dspTime + interval;
        }
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

#if UNITY_EDITOR
        Debug.Log($"Beat {currentBeatIndex + 1}: {currentAccent}");
#endif

        currentBeatIndex = (currentBeatIndex + 1) % BeatInfo.Count;
    }

    public void UpdateInfo(MetronomeInfo infoType, int value)
    {
        switch (infoType)
        {
            case MetronomeInfo.BPM:
                bpm = value;
                UpdateInterval();
                OnChangeBPM?.Invoke(bpm);
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

        State = MetronomeState.Playing;
        Beat();
        nextBeatTime = AudioSettings.dspTime + interval;
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
        nextBeatTime = 0;
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
