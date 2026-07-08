using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
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
    [SerializeField] private string sampleTrackLabel = "sample-track";
    [SerializeField] private bool autoPopulateFromLabel = true;
    [SerializeField] private AddressableSampleTrack[] fallbackSampleTracks;
    [SerializeField] private TMP_Dropdown trackDropdown;
    [SerializeField] private Button loadSelectedButton;
    [SerializeField] private bool useLoadSelectedButton;
    [SerializeField] private bool loadOnDropdownValueChanged = true;
    [SerializeField] private string emptyDropdownLabel = "";
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private bool playAfterLoad;

    private readonly List<IResourceLocation> autoTrackLocations = new List<IResourceLocation>();
    private AsyncOperationHandle<IList<IResourceLocation>> locationsHandle;
    private AsyncOperationHandle<AudioClip> loadedClipHandle;
    private UnityAction[] trackButtonActions;
    private bool hasLocationsHandle;
    private bool hasLoadedClipHandle;
    private bool isLoading;
    private bool isPopulating;

    private void Awake()
    {
        ResolveMp3Player();
    }

    private void Start()
    {
        if (autoPopulateFromLabel)
        {
            PopulateDropdownFromLabel();
        }
        else
        {
            PopulateDropdownFromFallback();
        }
    }

    private void OnEnable()
    {
        ResolveMp3Player();

        if (useLoadSelectedButton && loadSelectedButton != null)
        {
            loadSelectedButton.onClick.AddListener(LoadSelectedTrack);
        }

        if (loadOnDropdownValueChanged && trackDropdown != null)
        {
            trackDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
        }

        if (mp3Player != null)
        {
            mp3Player.OnLocalMp3Loaded.AddListener(OnLocalMp3Loaded);
        }

        RegisterFallbackButtons();
        UpdateControls();
    }

    private void OnDisable()
    {
        if (useLoadSelectedButton && loadSelectedButton != null)
        {
            loadSelectedButton.onClick.RemoveListener(LoadSelectedTrack);
        }

        if (loadOnDropdownValueChanged && trackDropdown != null)
        {
            trackDropdown.onValueChanged.RemoveListener(OnDropdownValueChanged);
        }

        if (mp3Player != null)
        {
            mp3Player.OnLocalMp3Loaded.RemoveListener(OnLocalMp3Loaded);
        }

        UnregisterFallbackButtons();
    }

    private void OnDestroy()
    {
        ReleaseLoadedClip();
        ReleaseLocations();
    }

    public void RefreshDropdown()
    {
        if (autoPopulateFromLabel)
        {
            PopulateDropdownFromLabel();
        }
        else
        {
            PopulateDropdownFromFallback();
        }
    }

    public void LoadSelectedTrack()
    {
        int selectedIndex = trackDropdown != null ? trackDropdown.value : 0;
        LoadTrack(selectedIndex, playAfterLoad);
    }

    public void LoadTrack(int trackIndex)
    {
        LoadTrack(trackIndex, playAfterLoad);
    }

    public void LoadAndPlaySelectedTrack()
    {
        int selectedIndex = trackDropdown != null ? trackDropdown.value : 0;
        LoadTrack(selectedIndex, true);
    }

    private void OnDropdownValueChanged(int trackIndex)
    {
        LoadTrack(trackIndex, false);
    }

    public void ClearDropdownSelection()
    {
        if (trackDropdown == null)
        {
            return;
        }

        trackDropdown.SetValueWithoutNotify(0);
        trackDropdown.RefreshShownValue();
        UpdateControls();
    }

    private void OnLocalMp3Loaded()
    {
        ReleaseLoadedClip();
        ClearDropdownSelection();
    }

    private void LoadTrack(int trackIndex, bool playWhenLoaded)
    {
        if (isLoading || isPopulating)
        {
            return;
        }

        if (trackIndex <= 0)
        {
            SetStatus("Select a sample track.");
            UpdateControls();
            return;
        }

        int assetIndex = trackIndex - 1;
        if (autoTrackLocations.Count > 0)
        {
            LoadAutoTrack(assetIndex, playWhenLoaded);
            return;
        }

        LoadFallbackTrack(assetIndex, playWhenLoaded);
    }

    private void PopulateDropdownFromLabel()
    {
        if (isPopulating)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(sampleTrackLabel))
        {
            SetStatus("Sample track label is empty.");
            PopulateDropdownFromFallback();
            return;
        }

        isPopulating = true;
        autoTrackLocations.Clear();
        ReleaseLocations();
        ClearDropdown();
        SetStatus($"Finding tracks: {sampleTrackLabel}");
        UpdateControls();

        locationsHandle = Addressables.LoadResourceLocationsAsync(sampleTrackLabel, typeof(AudioClip));
        hasLocationsHandle = true;
        locationsHandle.Completed += OnLocationsLoaded;
    }

    private void OnLocationsLoaded(AsyncOperationHandle<IList<IResourceLocation>> handle)
    {
        isPopulating = false;

        if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null || handle.Result.Count == 0)
        {
            SetStatus($"No sample tracks found for label: {sampleTrackLabel}");
            PopulateDropdownFromFallback();
            UpdateControls();
            return;
        }

        autoTrackLocations.Clear();
        autoTrackLocations.AddRange(handle.Result);
        PopulateDropdown(autoTrackLocations.ConvertAll(GetLocationDisplayName));
        SetStatus($"Found {autoTrackLocations.Count} sample track(s).");
        UpdateControls();
    }

    private void PopulateDropdownFromFallback()
    {
        List<string> labels = new List<string>();
        for (int i = 0; i < fallbackSampleTracks.Length; i++)
        {
            labels.Add(fallbackSampleTracks[i].Label);
        }

        PopulateDropdown(labels);
        UpdateControls();
    }

    private void LoadAutoTrack(int trackIndex, bool playWhenLoaded)
    {
        if (trackIndex < 0 || trackIndex >= autoTrackLocations.Count)
        {
            return;
        }

        if (!ResolveMp3Player())
        {
            SetStatus("MP3 player is not ready.");
            return;
        }

        IResourceLocation location = autoTrackLocations[trackIndex];
        string trackName = GetLocationDisplayName(location);

        isLoading = true;
        SetStatus($"Loading {trackName}...");
        UpdateControls();

        AsyncOperationHandle<AudioClip> handle = Addressables.LoadAssetAsync<AudioClip>(location);
        handle.Completed += completedHandle => OnTrackLoaded(trackName, completedHandle, playWhenLoaded);
    }

    private void LoadFallbackTrack(int trackIndex, bool playWhenLoaded)
    {
        if (trackIndex < 0 || trackIndex >= fallbackSampleTracks.Length)
        {
            return;
        }

        if (!ResolveMp3Player())
        {
            SetStatus("MP3 player is not ready.");
            return;
        }

        AddressableSampleTrack track = fallbackSampleTracks[trackIndex];
        if (track.audioClip == null || !track.audioClip.RuntimeKeyIsValid())
        {
            SetStatus("Sample track address is empty.");
            return;
        }

        isLoading = true;
        SetStatus($"Loading {track.Label}...");
        UpdateControls();

        AsyncOperationHandle<AudioClip> handle = track.audioClip.LoadAssetAsync();
        handle.Completed += completedHandle => OnTrackLoaded(track.Label, completedHandle, playWhenLoaded);
    }

    private void OnTrackLoaded(string trackName, AsyncOperationHandle<AudioClip> handle, bool playWhenLoaded)
    {
        isLoading = false;

        if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
        {
            Addressables.Release(handle);
            SetStatus($"Failed to load {trackName}.");
            UpdateControls();
            return;
        }

        bool hadPreviousHandle = hasLoadedClipHandle;
        AsyncOperationHandle<AudioClip> previousHandle = loadedClipHandle;

        loadedClipHandle = handle;
        hasLoadedClipHandle = true;
        mp3Player.LoadAudioClip(handle.Result, trackName);

        if (hadPreviousHandle)
        {
            Addressables.Release(previousHandle);
        }

        if (playWhenLoaded)
        {
            mp3Player.Play();
        }

        SetStatus($"Loaded {trackName}.");
        UpdateControls();
    }

    private void RegisterFallbackButtons()
    {
        for (int i = 0; i < fallbackSampleTracks.Length; i++)
        {
            int trackIndex = i;
            if (fallbackSampleTracks[i].button != null)
            {
                trackButtonActions ??= new UnityAction[fallbackSampleTracks.Length];
                trackButtonActions[i] = () => LoadFallbackTrack(trackIndex, playAfterLoad);
                fallbackSampleTracks[i].button.onClick.AddListener(trackButtonActions[i]);
            }
        }
    }

    private void UnregisterFallbackButtons()
    {
        for (int i = 0; i < fallbackSampleTracks.Length; i++)
        {
            if (fallbackSampleTracks[i].button != null && trackButtonActions != null && trackButtonActions[i] != null)
            {
                fallbackSampleTracks[i].button.onClick.RemoveListener(trackButtonActions[i]);
            }
        }

        trackButtonActions = null;
    }

    private bool ResolveMp3Player()
    {
        if (mp3Player != null)
        {
            return true;
        }

        return LocalMp3Player.TryGetInstance(out mp3Player);
    }

    private void PopulateDropdown(List<string> labels)
    {
        if (trackDropdown == null)
        {
            return;
        }

        trackDropdown.ClearOptions();
        List<string> dropdownLabels = new List<string> { emptyDropdownLabel };
        dropdownLabels.AddRange(labels);
        trackDropdown.AddOptions(dropdownLabels);
        trackDropdown.SetValueWithoutNotify(0);
        trackDropdown.RefreshShownValue();
    }

    private void ClearDropdown()
    {
        if (trackDropdown == null)
        {
            return;
        }

        trackDropdown.ClearOptions();
        trackDropdown.RefreshShownValue();
    }

    private void UpdateControls()
    {
        bool hasAutoTracks = autoTrackLocations.Count > 0;
        bool hasFallbackTracks = fallbackSampleTracks.Length > 0;

        if (useLoadSelectedButton && loadSelectedButton != null)
        {
            bool hasValidSelection = trackDropdown != null && trackDropdown.value > 0;
            loadSelectedButton.interactable = !isLoading && !isPopulating && hasValidSelection && (hasAutoTracks || hasFallbackTracks);
        }

        for (int i = 0; i < fallbackSampleTracks.Length; i++)
        {
            if (fallbackSampleTracks[i].button != null)
            {
                fallbackSampleTracks[i].button.interactable = !isLoading && !isPopulating;
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

    private void ReleaseLocations()
    {
        if (!hasLocationsHandle)
        {
            return;
        }

        Addressables.Release(locationsHandle);
        hasLocationsHandle = false;
    }

    private static string GetLocationDisplayName(IResourceLocation location)
    {
        if (location == null)
        {
            return "Sample Track";
        }

        if (!string.IsNullOrWhiteSpace(location.PrimaryKey))
        {
            return location.PrimaryKey;
        }

        return string.IsNullOrWhiteSpace(location.InternalId) ? "Sample Track" : location.InternalId;
    }
}
