using UnityEngine;
using UnityEngine.UI;
using _Scripts.GameManagement;

/// <summary>
/// Controls pause/play/speed buttons and applies the selected time scale.
/// </summary>
public class TimeScaleButtonController : MonoBehaviour {

    #region Types

    /// <summary>
    /// Available time-scale modes for the battle.
    /// </summary>
    public enum TimeMode {
        Pause,
        Play,
        Speed
    }

    #endregion
    #region Variables

    [Header("Buttons")]
    [SerializeField] private Button pauseButton; // Button that pauses time.
    [SerializeField] private Button playButton;  // Button that restores normal speed.
    [SerializeField] private Button speedButton; // Button that enables fast-forward.

    [Header("Colours")]
    [SerializeField] private Color selectedColour = Color.white;                            // Active button colour.
    [SerializeField] private Color unselectedColour = new Color32(0x6D, 0x6D, 0x6D, 0xFF); // Inactive button colour.

    [Header("Current State")]
    [SerializeField] private TimeMode currentMode = TimeMode.Play; // Currently applied time mode.

    #endregion
    #region Unity Methods

    private void Awake() {
        AddButtonListeners();
    }

    private void Start() {
        ApplyTimeMode(currentMode);
    }

    private void OnDestroy() {
        RemoveButtonListeners();
    }

    #endregion
    #region Public Methods

    /// <summary>
    /// Applies a time mode when the game is allowed to change time.
    /// </summary>
    /// <param name="mode">The mode requested by the UI.</param>
    public void TrySetTimeMode(TimeMode mode) {
        if (GameManager.Instance != null && GameManager.Instance.GameState == GameState.PreGame) return;

        AudioManager.Instance?.PlayDefaultButtonSound();
        ApplyTimeMode(mode);
    }
    
    public void PauseGame() {
        TrySetTimeMode(TimeMode.Pause);
    }
    
    public void ResumeGame() {
        TrySetTimeMode(TimeMode.Play);
    }

    /// <summary>
    /// Applies the requested time mode immediately.
    /// </summary>
    /// <param name="mode">The mode to apply.</param>
    public void ApplyTimeMode(TimeMode mode) {
        currentMode = mode;

        switch (mode) {
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

    #endregion
    #region Button Setup

    /// <summary>
    /// Connects button click events.
    /// </summary>
    private void AddButtonListeners() {
        if (pauseButton != null) pauseButton.onClick.AddListener(() => TrySetTimeMode(TimeMode.Pause));
        if (playButton != null) playButton.onClick.AddListener(() => TrySetTimeMode(TimeMode.Play));
        if (speedButton != null) speedButton.onClick.AddListener(() => TrySetTimeMode(TimeMode.Speed));
    }

    /// <summary>
    /// Removes button click events owned by this controller.
    /// </summary>
    private void RemoveButtonListeners() {
        if (pauseButton != null) pauseButton.onClick.RemoveAllListeners();
        if (playButton != null) playButton.onClick.RemoveAllListeners();
        if (speedButton != null) speedButton.onClick.RemoveAllListeners();
    }

    #endregion
    #region UI

    /// <summary>
    /// Updates button colours to show the selected mode.
    /// </summary>
    private void UpdateButtonVisuals() {
        SetButtonColour(pauseButton, currentMode == TimeMode.Pause);
        SetButtonColour(playButton, currentMode == TimeMode.Play);
        SetButtonColour(speedButton, currentMode == TimeMode.Speed);
    }

    /// <summary>
    /// Applies the selected or unselected colour to a button image.
    /// </summary>
    /// <param name="button">The button to colour.</param>
    /// <param name="selected">Whether the button is currently selected.</param>
    private void SetButtonColour(Button button, bool selected) {
        if (button == null) return;

        var image = button.GetComponent<Image>();
        if (image != null) {
            image.color = selected ? selectedColour : unselectedColour;
        }
    }

    #endregion
}
