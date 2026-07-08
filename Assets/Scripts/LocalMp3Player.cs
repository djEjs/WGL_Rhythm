using System.Collections;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;

public class LocalMp3Player : Singleton<LocalMp3Player>
{
    private enum TrackSource
    {
        None,
        UnityAudioClip,
        WebGLLocalFile
    }

    [SerializeField] private Button loadButton;
    [SerializeField] private Button playButton;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button stopButton;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private TMP_Text fileNameText;

    private string currentBlobUrl;
    private Coroutine loadingRoutine;
    private bool wasPlaying;
    private bool destroyCurrentAudioClipOnReplace;
    private TrackSource currentTrackSource = TrackSource.None;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void OpenLocalMp3File(string targetObjectName);

    [DllImport("__Internal")]
    private static extern void RevokeLocalMp3Url(string blobUrl);

    [DllImport("__Internal")]
    private static extern void PlayLocalMp3File();

    [DllImport("__Internal")]
    private static extern void PauseLocalMp3File();

    [DllImport("__Internal")]
    private static extern void StopLocalMp3File();

    [DllImport("__Internal")]
    private static extern int IsLocalMp3Playing();

    private bool hasWebGLTrack;
    private bool isWebGLPlaying;
#endif

    public bool HasClip
    {
        get
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return hasWebGLTrack || audioSource != null && audioSource.clip != null;
#else
            return currentTrackSource != TrackSource.None && audioSource != null && audioSource.clip != null;
#endif
        }
    }

    public bool IsPlaying
    {
        get
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return currentTrackSource == TrackSource.WebGLLocalFile
                ? isWebGLPlaying
                : audioSource != null && audioSource.isPlaying;
#else
            return currentTrackSource != TrackSource.None && audioSource != null && audioSource.isPlaying;
#endif
        }
    }

    public bool IsUsingWebGLLocalFile
    {
        get
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return currentTrackSource == TrackSource.WebGLLocalFile;
#else
            return false;
#endif
        }
    }

    public AudioSource AudioSource
    {
        get => audioSource;
        private set => audioSource = value;
    }

    public UnityEvent<AudioClip> OnClipLoaded = new UnityEvent<AudioClip>();
    public UnityEvent OnPlay = new UnityEvent();
    public UnityEvent OnPause = new UnityEvent();

    protected override void Awake()
    {
        base.Awake();

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        if (audioSource.clip != null)
        {
            currentTrackSource = TrackSource.UnityAudioClip;
        }
    }

    private void OnEnable()
    {
        if (loadButton != null)
        {
            loadButton.onClick.AddListener(OpenFilePicker);
        }

        if (playButton != null)
        {
            playButton.onClick.AddListener(Play);
        }

        if (pauseButton != null)
        {
            pauseButton.onClick.AddListener(Pause);
        }

        if (stopButton != null)
        {
            stopButton.onClick.AddListener(Stop);
        }

        UpdateButtons();
    }

    private void OnDisable()
    {
        if (loadButton != null)
        {
            loadButton.onClick.RemoveListener(OpenFilePicker);
        }

        if (playButton != null)
        {
            playButton.onClick.RemoveListener(Play);
        }

        if (pauseButton != null)
        {
            pauseButton.onClick.RemoveListener(Pause);
        }

        if (stopButton != null)
        {
            stopButton.onClick.RemoveListener(Stop);
        }
    }

    protected override void OnDestroy()
    {
        RevokeCurrentBlobUrl();

        if (audioSource != null && audioSource.clip != null && destroyCurrentAudioClipOnReplace)
        {
            Destroy(audioSource.clip);
        }

        base.OnDestroy();
    }

    public void OpenFilePicker()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        OpenLocalMp3File(gameObject.name);
#else
        Debug.LogWarning("Local MP3 file picker is available only in WebGL builds.");
#endif
    }

    public void Play()
    {
        if (!HasClip)
        {
            return;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        if (currentTrackSource == TrackSource.WebGLLocalFile)
        {
            PlayLocalMp3File();
            isWebGLPlaying = true;
        }
        else
        {
            PlayUnityAudioSource();
        }
#else
        PlayUnityAudioSource();
#endif

        OnPlay?.Invoke();
        UpdateButtons();
    }

    public void Pause()
    {
        if (audioSource == null)
        {
            return;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        if (currentTrackSource == TrackSource.WebGLLocalFile)
        {
            PauseLocalMp3File();
            isWebGLPlaying = false;
        }
        else
        {
            audioSource.Pause();
        }
#else
        audioSource.Pause();
#endif

        OnPause?.Invoke();
        UpdateButtons();
    }

    public void Stop()
    {
        if (audioSource == null)
        {
            return;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        if (currentTrackSource == TrackSource.WebGLLocalFile)
        {
            StopLocalMp3File();
            isWebGLPlaying = false;
        }
        else
        {
            StopUnityAudioSource();
        }
#else
        StopUnityAudioSource();
#endif

        UpdateButtons();
    }

    public void LoadAudioClip(AudioClip clip, string displayName, bool destroyOnReplace = false)
    {
        if (clip == null)
        {
            Debug.LogError("Cannot load a null audio clip.");
            return;
        }

        Stop();
        RevokeCurrentBlobUrl();
        ReplaceAudioClip(clip, destroyOnReplace);
        currentTrackSource = TrackSource.UnityAudioClip;

#if UNITY_WEBGL && !UNITY_EDITOR
        hasWebGLTrack = false;
        isWebGLPlaying = false;
#endif

        if (fileNameText != null)
        {
            fileNameText.text = string.IsNullOrWhiteSpace(displayName) ? clip.name : displayName;
        }

        UpdateButtons();
        OnClipLoaded?.Invoke(clip);
    }

    public void OnLocalMp3Selected(string payload)
    {
        int separatorIndex = payload.IndexOf('\n');
        if (separatorIndex < 0)
        {
            Debug.LogError("Invalid local MP3 payload.");
            return;
        }

        string blobUrl = payload.Substring(0, separatorIndex);
        string fileName = payload.Substring(separatorIndex + 1);

#if UNITY_WEBGL && !UNITY_EDITOR
        RevokeCurrentBlobUrl();
        currentBlobUrl = blobUrl;
        currentTrackSource = TrackSource.WebGLLocalFile;
        hasWebGLTrack = true;
        isWebGLPlaying = false;
        loadingRoutine = null;

        Debug.Log($"Loaded local MP3: {fileName}");
        if (fileNameText != null)
        {
            fileNameText.text = fileName;
        }

        UpdateButtons();
        OnClipLoaded?.Invoke(null);
#else
        if (loadingRoutine != null)
        {
            StopCoroutine(loadingRoutine);
        }

        loadingRoutine = StartCoroutine(LoadMp3Clip(blobUrl, fileName));
#endif
    }

    public void OnLocalMp3SelectionCanceled(string message)
    {
        Debug.Log(message);
    }

    public void OnLocalMp3SelectionFailed(string message)
    {
        Debug.LogError(message);
    }

    public void OnLocalMp3Ended(string message)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        isWebGLPlaying = false;
        UpdateButtons();
#endif
    }

    private void Update()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (currentTrackSource == TrackSource.WebGLLocalFile)
        {
            bool playing = IsLocalMp3Playing() == 1;
            if (wasPlaying == playing)
            {
                return;
            }

            wasPlaying = playing;
            isWebGLPlaying = playing;
            UpdateButtons();
            return;
        }
#endif

        if (audioSource == null || wasPlaying == audioSource.isPlaying)
        {
            return;
        }

        wasPlaying = audioSource.isPlaying;
        UpdateButtons();
    }

    private IEnumerator LoadMp3Clip(string blobUrl, string fileName)
    {
        UpdateButtons(false);

        using UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(blobUrl, AudioType.MPEG);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Failed to load local MP3 '{fileName}': {request.error}");
            RevokeBlobUrl(blobUrl);
            loadingRoutine = null;
            UpdateButtons();
            yield break;
        }

        AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
        clip.name = fileName;
        currentBlobUrl = blobUrl;
        loadingRoutine = null;

        Debug.Log($"Loaded local MP3: {fileName}");
        LoadAudioClip(clip, fileName, true);
    }

    private void UpdateButtons(bool canLoad = true)
    {
        if (loadButton != null)
        {
            loadButton.interactable = canLoad;
        }

        bool hasClip = HasClip;
        bool isPlaying = IsPlaying;

        if (playButton != null)
        {
            playButton.interactable = hasClip && !isPlaying;
        }

        if (pauseButton != null)
        {
            pauseButton.interactable = hasClip && isPlaying;
        }

        if (stopButton != null)
        {
            stopButton.interactable = hasClip;
        }
    }

    private void RevokeCurrentBlobUrl()
    {
        if (string.IsNullOrEmpty(currentBlobUrl))
        {
            return;
        }

        RevokeBlobUrl(currentBlobUrl);
        currentBlobUrl = null;
    }

    private void ReplaceAudioClip(AudioClip clip, bool destroyOnReplace)
    {
        if (audioSource.clip != null && destroyCurrentAudioClipOnReplace)
        {
            Destroy(audioSource.clip);
        }

        audioSource.clip = clip;
        destroyCurrentAudioClipOnReplace = destroyOnReplace;
    }

    private void PlayUnityAudioSource()
    {
        if (audioSource == null || audioSource.clip == null)
        {
            return;
        }

        if (audioSource.time >= audioSource.clip.length)
        {
            audioSource.time = 0f;
        }

        audioSource.Play();
    }

    private void StopUnityAudioSource()
    {
        audioSource.Stop();
        audioSource.time = 0f;
    }

    private static void RevokeBlobUrl(string blobUrl)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        RevokeLocalMp3Url(blobUrl);
#endif
    }
}
