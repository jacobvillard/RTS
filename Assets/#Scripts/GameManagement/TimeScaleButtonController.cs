using UnityEngine;
using UnityEngine.UI;

public class TimeScaleButtonController : MonoBehaviour
{
    public enum TimeMode
    {
        Pause,
        Play,
        Speed
    }

    [Header("Buttons")]
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button playButton;
    [SerializeField] private Button speedButton;

    [Header("Colours")]
    [SerializeField] private Color selectedColour = Color.white;
    [SerializeField] private Color unselectedColour = new Color32(0x6D, 0x6D, 0x6D, 0xFF);

    [Header("Current State")]
    [SerializeField] private TimeMode currentMode = TimeMode.Play;

    private void Awake()
    {
        pauseButton.onClick.AddListener(() => TrySetTimeMode(TimeMode.Pause));
        playButton.onClick.AddListener(() => TrySetTimeMode(TimeMode.Play));
        speedButton.onClick.AddListener(() => TrySetTimeMode(TimeMode.Speed));
    }

    private void Start()
    {
        ApplyTimeMode(currentMode);
    }

    public void TrySetTimeMode(TimeMode mode)
    {
        if (GameManager.Instance != null &&
            GameManager.Instance.GameState == GameState.PreGame)
        {
            return;
        }

        ApplyTimeMode(mode);
    }

    public void ApplyTimeMode(TimeMode mode)
    {
        currentMode = mode;

        switch (mode)
        {
            case TimeMode.Pause:
                Time.timeScale = 0f;
                break;

            case TimeMode.Play:
                Time.timeScale = 1f;
                break;

            case TimeMode.Speed:
                Time.timeScale = 2f;
                break;
        }

        UpdateButtonVisuals();
    }

    private void UpdateButtonVisuals()
    {
        SetButtonColour(pauseButton, currentMode == TimeMode.Pause);
        SetButtonColour(playButton, currentMode == TimeMode.Play);
        SetButtonColour(speedButton, currentMode == TimeMode.Speed);
    }

    private void SetButtonColour(Button button, bool selected)
    {
        if (button == null)
            return;

        Image image = button.GetComponent<Image>();

        if (image != null)
        {
            image.color = selected ? selectedColour : unselectedColour;
        }
    }

    private void OnDestroy()
    {
        pauseButton.onClick.RemoveAllListeners();
        playButton.onClick.RemoveAllListeners();
        speedButton.onClick.RemoveAllListeners();
    }
}