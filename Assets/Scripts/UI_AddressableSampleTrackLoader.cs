using System;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

[Serializable]
public class AddressableSampleTrack
{
    public string displayName;
    public AssetReferenceT<AudioClip> audioClip;
    public Button button;

    public string Label => string.IsNullOrWhiteSpace(displayName) ? "Sample Track" : displayName;
}

public class UI_AddressableSampleTrackLoader : MonoBehaviour
{
    [SerializeField] private LocalMp3Player mp3Player;
    [SerializeField] private AddressableSampleTrack[] sampleTracks;
    [SerializeField] private TMP_Dropdown trackDropdown;
    [SerializeField] private Button loadSelectedButton;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private bool playAfterLoad;

    private AsyncOperationHandle<AudioClip> loadedClipHandle;
    private UnityAction[] trackButtonActions;
    private bool hasLoadedClipHandle;
    private bool isLoading;

    private void Awake()
    {
        ResolveMp3Player();
        PopulateDropdown();
    }

    private void OnEnable()
    {
        if (loadSelectedButton != null)
        {
            loadSelectedButton.onClick.AddListener(LoadSelectedTrack);
        }

        for (int i = 0; i < sampleTracks.Length; i++)
        {
            int trackIndex = i;
            if (sampleTracks[i].button != null)
            {
                trackButtonActions ??= new UnityAction[sampleTracks.Length];
                trackButtonActions[i] = () => LoadTrack(trackIndex);
                sampleTracks[i].button.onClick.AddListener(trackButtonActions[i]);
            }
        }

        UpdateControls();
    }

    private void OnDisable()
    {
        if (loadSelectedButton != null)
        {
            loadSelectedButton.onClick.RemoveListener(LoadSelectedTrack);
        }

        for (int i = 0; i < sampleTracks.Length; i++)
        {
            if (sampleTracks[i].button != null && trackButtonActions != null && trackButtonActions[i] != null)
            {
                sampleTracks[i].button.onClick.RemoveListener(trackButtonActions[i]);
            }
        }

        trackButtonActions = null;
    }

    private void OnDestroy()
    {
        ReleaseLoadedClip();
    }

    public void LoadSelectedTrack()
    {
        int selectedIndex = trackDropdown != null ? trackDropdown.value : 0;
        LoadTrack(selectedIndex);
    }

    public void LoadTrack(int trackIndex)
    {
        if (isLoading || trackIndex < 0 || trackIndex >= sampleTracks.Length)
        {
            return;
        }

        if (!ResolveMp3Player())
        {
            SetStatus("MP3 player is not ready.");
            return;
        }

        AddressableSampleTrack track = sampleTracks[trackIndex];
        if (track.audioClip == null || !track.audioClip.RuntimeKeyIsValid())
        {
            SetStatus("Sample track address is empty.");
            return;
        }

        isLoading = true;
        UpdateControls();
        SetStatus($"Loading {track.Label}...");

        AsyncOperationHandle<AudioClip> handle = track.audioClip.LoadAssetAsync();
        handle.Completed += completedHandle => OnTrackLoaded(track, completedHandle);
    }

    private void OnTrackLoaded(AddressableSampleTrack track, AsyncOperationHandle<AudioClip> handle)
    {
        isLoading = false;

        if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
        {
            Addressables.Release(handle);
            SetStatus($"Failed to load {track.Label}.");
            UpdateControls();
            return;
        }

        bool hadPreviousHandle = hasLoadedClipHandle;
        AsyncOperationHandle<AudioClip> previousHandle = loadedClipHandle;

        loadedClipHandle = handle;
        hasLoadedClipHandle = true;
        mp3Player.LoadAudioClip(handle.Result, track.Label);

        if (hadPreviousHandle)
        {
            Addressables.Release(previousHandle);
        }

        if (playAfterLoad)
        {
            mp3Player.Play();
        }

        SetStatus($"Loaded {track.Label}.");
        UpdateControls();
    }

    private bool ResolveMp3Player()
    {
        if (mp3Player != null)
        {
            return true;
        }

        return LocalMp3Player.TryGetInstance(out mp3Player);
    }

    private void PopulateDropdown()
    {
        if (trackDropdown == null)
        {
            return;
        }

        trackDropdown.ClearOptions();

        for (int i = 0; i < sampleTracks.Length; i++)
        {
            trackDropdown.options.Add(new TMP_Dropdown.OptionData(sampleTracks[i].Label));
        }

        trackDropdown.RefreshShownValue();
    }

    private void UpdateControls()
    {
        if (loadSelectedButton != null)
        {
            loadSelectedButton.interactable = !isLoading && sampleTracks.Length > 0;
        }

        for (int i = 0; i < sampleTracks.Length; i++)
        {
            if (sampleTracks[i].button != null)
            {
                sampleTracks[i].button.interactable = !isLoading;
            }
        }
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private void ReleaseLoadedClip()
    {
        if (!hasLoadedClipHandle)
        {
            return;
        }

        Addressables.Release(loadedClipHandle);
        hasLoadedClipHandle = false;
    }
}
