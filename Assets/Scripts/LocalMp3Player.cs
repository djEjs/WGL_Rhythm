using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class LocalMp3Player : Singleton<LocalMp3Player>
{
    [SerializeField] private Button loadButton;
    [SerializeField] private Button playButton;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button stopButton;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private TMP_Text fileNameText;

    private string currentBlobUrl;
    private Coroutine loadingRoutine;
    private bool wasPlaying;

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
#endif

#if UNITY_WEBGL && !UNITY_EDITOR
    private bool hasWebGLTrack;
    private bool isWebGLPlaying;
#endif

    public bool HasClip
    {
        get
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return hasWebGLTrack;
#else
            return audioSource != null && audioSource.clip != null;
#endif
        }
    }

    public bool IsPlaying
    {
        get
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return isWebGLPlaying;
#else
            return audioSource != null && audioSource.isPlaying;
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
        PlayLocalMp3File();
        isWebGLPlaying = true;
#else
        if (audioSource.time >= audioSource.clip.length)
        {
            audioSource.time = 0f;
        }

        audioSource.Play();
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
        PauseLocalMp3File();
        isWebGLPlaying = false;
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
        StopLocalMp3File();
        isWebGLPlaying = false;
#else
        audioSource.Stop();
        audioSource.time = 0f;
#endif
        UpdateButtons();
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
        bool playing = IsLocalMp3Playing() == 1;
        if (wasPlaying == playing)
        {
            return;
        }

        wasPlaying = playing;
        isWebGLPlaying = playing;
        UpdateButtons();
#else
        if (audioSource == null || wasPlaying == audioSource.isPlaying)
        {
            return;
        }

        wasPlaying = audioSource.isPlaying;
        UpdateButtons();
#endif
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

        Stop();
        if (audioSource.clip != null)
        {
            Destroy(audioSource.clip);
        }

        RevokeCurrentBlobUrl();
        currentBlobUrl = blobUrl;
        audioSource.clip = clip;
        loadingRoutine = null;

        Debug.Log($"Loaded local MP3: {fileName}");
        if (fileNameText != null)
        {
            fileNameText.text = fileName;
        }

        UpdateButtons();
        OnClipLoaded?.Invoke(clip);
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

    private static void RevokeBlobUrl(string blobUrl)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        RevokeLocalMp3Url(blobUrl);
#endif
    }
}
