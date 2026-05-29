using System.Collections;
using _Scripts.GameManagement;
using UnityEngine;

/// <summary>
/// Represents the high-level phase of the battle flow.
/// </summary>
public enum GameState {
    PreGame,
    Playing,
    Paused,
    GameOver
}

/// <summary>
/// Owns game-state transitions and global time scaling.
/// </summary>
[RequireComponent(typeof(LevelStats))]
public class GameManager : Singleton<GameManager> {

    #region Variables

    [Header("Game State")]
    [SerializeField] private GameState gameState = GameState.PreGame; // Current game phase.
    [SerializeField] private float endGameDelaySeconds = 10f;         // Delay before pausing after battle end.

    [Header("Scene References")]
    [SerializeField] private BoxCollider2D unitPlacementArea;                 // Placement area collider disabled at battle start.
    [SerializeField] private TimeScaleButtonController timeScaleButtonController; // Time controls synced to state changes.

    private Coroutine _endGameCoroutine; // Active delayed end-game pause coroutine.

    public GameState GameState => gameState;
    public LevelStats LevelStats { get; private set; }

    #endregion
    #region Unity Methods

    protected override void Awake() {
        base.Awake();

        if (Instance != this) {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
        LevelStats = GetComponent<LevelStats>();
    }

    #endregion
    #region State

    /// <summary>
    /// Sets the current game state.
    /// </summary>
    /// <param name="newState">The state to enter.</param>
    public void SetGameState(GameState newState) {
        gameState = newState;
    }

    /// <summary>
    /// Starts active battle play.
    /// </summary>
    public void StartGame() {
        StopEndGameDelay();
        SetGameState(GameState.Playing);
        SetTimeMode(TimeScaleButtonController.TimeMode.Play);

        if (unitPlacementArea != null) {
            unitPlacementArea.enabled = false;
        }
    }

    /// <summary>
    /// Returns the game to pre-battle setup.
    /// </summary>
    public void SetPreGame() {
        StopEndGameDelay();
        SetGameState(GameState.PreGame);
        Time.timeScale = 1f;
    }

    /// <summary>
    /// Marks the game as over and delays the final pause so animations can finish.
    /// </summary>
    public void EndGame() {
        SetGameState(GameState.GameOver);
        AudioManager.Instance?.StopMusic();
        AudioManager.Instance?.PlayRoundEnd();
        StopEndGameDelay();
        _endGameCoroutine = StartCoroutine(PauseAfterEndGameDelay());
    }

    /// <summary>
    /// Checks if the game is currently in unit-placement setup.
    /// </summary>
    /// <returns>True when the game is in pre-game.</returns>
    public bool IsPreGame() {
        return gameState == GameState.PreGame;
    }

    #endregion
    #region Time

    /// <summary>
    /// Waits before pausing so end-of-battle animations can complete.
    /// </summary>
    private IEnumerator PauseAfterEndGameDelay() {
        yield return new WaitForSecondsRealtime(endGameDelaySeconds);

        SetTimeMode(TimeScaleButtonController.TimeMode.Pause);
        _endGameCoroutine = null;
    }

    /// <summary>
    /// Stops the pending end-game pause when the state changes again.
    /// </summary>
    private void StopEndGameDelay() {
        if (_endGameCoroutine == null) return;

        StopCoroutine(_endGameCoroutine);
        _endGameCoroutine = null;
    }

    /// <summary>
    /// Applies a time mode through the button controller when possible.
    /// </summary>
    /// <param name="timeMode">The mode to apply.</param>
    private void SetTimeMode(TimeScaleButtonController.TimeMode timeMode) {
        if (timeScaleButtonController != null) {
            timeScaleButtonController.ApplyTimeMode(timeMode);
            return;
        }

        Time.timeScale = timeMode == TimeScaleButtonController.TimeMode.Pause ? 0f : 1f;
    }

    #endregion
}
