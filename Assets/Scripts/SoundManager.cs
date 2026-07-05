using System.Collections.Generic;
using UnityEngine;

public class SoundManager : Singleton<SoundManager>
{
    public AudioSource audioSource;
    private Metronome metronome;

    [SerializeField]
    private AudioClip sfx_beatSound_Strong;
    [SerializeField]
    private AudioClip sfx_beatSound_Weak;

    private Dictionary<AccentType, AudioClip> beatSoundMap;

    protected override void Awake()
    {
        base.Awake();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        beatSoundMap = new Dictionary<AccentType, AudioClip>
        {
            { AccentType.None, null },
            { AccentType.Strong, sfx_beatSound_Strong },
            { AccentType.Weak, sfx_beatSound_Weak }
        };
    }
    private void Start()
    {
        if (sfx_beatSound_Strong == null)
        {
            Debug.LogWarning("Beat sound clip is not assigned in the inspector.");
        }

        if (!Metronome.TryGetInstance(out metronome))
        {
            Debug.LogError("Metronome instance is not found. Cannot register beat sound.");
            return;
        }

        metronome.OnBeat.AddListener(PlayBeatSound);
    }
    
    private void PlayBeatSound(int beatIndex, AccentType accentType)
    {
        PlaySound(beatSoundMap.ContainsKey(accentType) ? beatSoundMap[accentType] : null);
    }

    public void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
        else
        {
            Debug.LogWarning("AudioSource or AudioClip is null. Cannot play sound.");
        }
    }
    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (metronome != null)
        {
            metronome.OnBeat.RemoveListener(PlayBeatSound);
        }
    }
}
