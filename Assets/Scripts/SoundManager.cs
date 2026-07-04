using UnityEngine;

public class SoundManager : Singleton<SoundManager>
{
    public AudioSource audioSource;
    private Metronome metronome;

    [SerializeField]
    private AudioClip sfx_beatSound_Strong;
    [SerializeField]
    private AudioClip sfx_beatSound_Weak;

    protected override void Awake()
    {
        base.Awake();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
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
        if (accentType == AccentType.Strong)
        {
            PlaySound(sfx_beatSound_Strong);
        }
        else if (accentType == AccentType.Weak)
        {
            PlaySound(sfx_beatSound_Weak);
        }
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
