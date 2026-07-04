using UnityEngine;
using UnityEngine.UI;

public class UI_MetronomeController : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button stopButton;

    private Metronome metronome;

    private void Awake()
    {
        if (startButton != null)
        {
            startButton.onClick.AddListener(PlayMetronome);
        }

        if (pauseButton != null)
        {
            pauseButton.onClick.AddListener(PauseMetronome);
        }

        if (stopButton != null)
        {
            stopButton.onClick.AddListener(StopMetronome);
        }
    }

    private void Start()
    {
        if (!Metronome.TryGetInstance(out metronome))
        {
            Debug.LogError("Metronome instance is not found. Cannot initialize metronome controller.");
            UpdateButtons();
            return;
        }

        UpdateButtons();
    }

    private void OnDestroy()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(PlayMetronome);
        }

        if (pauseButton != null)
        {
            pauseButton.onClick.RemoveListener(PauseMetronome);
        }

        if (stopButton != null)
        {
            stopButton.onClick.RemoveListener(StopMetronome);
        }
    }

    private void PlayMetronome()
    {
        if (metronome == null)
        {
            return;
        }

        metronome.Play();
        UpdateButtons();
    }

    private void PauseMetronome()
    {
        if (metronome == null)
        {
            return;
        }

        metronome.Pause();
        UpdateButtons();
    }

    private void StopMetronome()
    {
        if (metronome == null)
        {
            return;
        }

        metronome.Stop();
        UpdateButtons();
    }

    private void UpdateButtons()
    {
        MetronomeState state = metronome != null ? metronome.State : MetronomeState.Stopped;

        if (startButton != null)
        {
            startButton.interactable = state != MetronomeState.Playing;
        }

        if (pauseButton != null)
        {
            pauseButton.interactable = state == MetronomeState.Playing;
        }

        if (stopButton != null)
        {
            stopButton.interactable = state != MetronomeState.Stopped;
        }
    }
}
