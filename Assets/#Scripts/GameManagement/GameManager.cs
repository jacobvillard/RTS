using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    [SerializeField] private GameState gameState = GameState.PreGame;               // Current game phase.
    [SerializeField] private float endGameDelaySeconds = 10f;                       // Delay before pausing after battle end.

    [Header("Scene References")]
    [SerializeField] private BoxCollider2D unitPlacementArea;                       // Placement area collider disabled at battle start.
    [SerializeField] private TimeScaleButtonController timeScaleButtonController;   // Time controls synced to state changes.

    [Header("End Game UI")]
    [SerializeField] private GameObject postGameUI;                                 // UI shown after the battle ends.
    [SerializeField] private GameObject wonBtn;                                     // Button shown when the player wins.
    [SerializeField] private GameObject lostBtn;                                    // Button shown when the player loses.

    [Header("Pre-Game Placement")]
    [SerializeField] private List<GameObject> placeableAreas = new();               // Colliders that should be enabled during pre-game placement.

    private Coroutine _endGameCoroutine;                                            // Active delayed end-game pause coroutine.

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

        foreach (var area in placeableAreas.Where(area => area != null)) {
            area.SetActive(false);
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

    /// <summary>
    /// Quits the application. Note that this will not have an effect in the editor or WebGL builds.
    /// </summary>
    public void QuitApplication() {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    #endregion
    #region Time

    /// <summary>
    /// Waits before pausing so end-of-battle animations can complete.
    /// </summary>
    private IEnumerator PauseAfterEndGameDelay() {
        yield return new WaitForSecondsRealtime(endGameDelaySeconds);

        SetTimeMode(TimeScaleButtonController.TimeMode.Pause);
        postGameUI?.SetActive(true);
        if(BattleController.Instance != null) {
            if(BattleController.Instance.winningTeam == "Player") {
                wonBtn?.SetActive(true);
                lostBtn?.SetActive(false);
            }
            else {
                wonBtn?.SetActive(false);
                lostBtn?.SetActive(true);
            }
        }
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
