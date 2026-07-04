using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class LocalMp3Player : MonoBehaviour
{
    [SerializeField] private Button loadButton;
    [SerializeField] private Button playButton;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button stopButton;
    [SerializeField] private AudioSource audioSource;

    private string currentBlobUrl;
    private Coroutine loadingRoutine;
    private bool wasPlaying;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void OpenLocalMp3File(string targetObjectName);

    [DllImport("__Internal")]
    private static extern void RevokeLocalMp3Url(string blobUrl);
#endif

    public bool HasClip => audioSource != null && audioSource.clip != null;

    private void Awake()
    {
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

    private void OnDestroy()
    {
        RevokeCurrentBlobUrl();
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

        if (audioSource.time >= audioSource.clip.length)
        {
            audioSource.time = 0f;
        }

        audioSource.Play();
        UpdateButtons();
    }

    public void Pause()
    {
        if (audioSource == null)
        {
            return;
        }

        audioSource.Pause();
        UpdateButtons();
    }

    public void Stop()
    {
        if (audioSource == null)
        {
            return;
        }

        audioSource.Stop();
        audioSource.time = 0f;
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

        if (loadingRoutine != null)
        {
            StopCoroutine(loadingRoutine);
        }

        loadingRoutine = StartCoroutine(LoadMp3Clip(blobUrl, fileName));
    }

    public void OnLocalMp3SelectionCanceled(string message)
    {
        Debug.Log(message);
    }

    public void OnLocalMp3SelectionFailed(string message)
    {
        Debug.LogError(message);
    }

    private void Update()
    {
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
        UpdateButtons();
    }

    private void UpdateButtons(bool canLoad = true)
    {
        if (loadButton != null)
        {
            loadButton.interactable = canLoad;
        }

        bool hasClip = HasClip;

        if (playButton != null)
        {
            playButton.interactable = hasClip && !audioSource.isPlaying;
        }

        if (pauseButton != null)
        {
            pauseButton.interactable = hasClip && audioSource.isPlaying;
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
