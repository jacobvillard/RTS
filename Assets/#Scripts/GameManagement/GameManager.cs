using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _Scripts.GameManagement;
using _Scripts.UI;
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

    private const string PlaceableAreaTag = "PlaceableArea";
    [Header("Game State")]
    [SerializeField] private GameState gameState = GameState.PreGame;               // Current game phase.
    [SerializeField] private float endGameDelaySeconds = 10f;                       // Delay before pausing after battle end.

    [Header("Scene References")]
    [SerializeField] private BoxCollider2D unitPlacementArea;                       // Placement area collider disabled at battle start.
    [SerializeField] private TimeScaleButtonController timeScaleButtonController;   // Time controls synced to state changes.

    [Header("Pre-Game Placement")]
    [SerializeField] private List<GameObject> placeableAreas = new();               // Optional fallback areas if the tag lookup is not ready yet.

    private Coroutine _endGameCoroutine;                                            // Active delayed end-game pause coroutine.
    private readonly List<GameObject> _runtimePlaceableAreas = new();               // Placeable areas found by tag at runtime.

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

    private void Start() {
        SetEndGameUiActive(false);
        RefreshPlaceableAreas();
        SetPlaceableAreasActive(IsPreGame());
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
        Debug.Log("[BattleDebug] GameManager.StartGame called. State set to Playing.");
        SetTimeMode(TimeScaleButtonController.TimeMode.Play);
        BattleController.Instance?.PrepareUnitsForBattle();
        SetPlaceableAreasActive(false);
    }

    /// <summary>
    /// Returns the game to pre-battle setup.
    /// </summary>
    public void SetPreGame() {
        StopEndGameDelay();
        SetGameState(GameState.PreGame);
        Time.timeScale = 1f;
        SetPlaceableAreasActive(true);
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
    /// Rebinds scene-owned placement areas after a new scene loads.
    /// </summary>
    public void RefreshForSceneLoad() {
        StopEndGameDelay();
        SetGameState(GameState.PreGame);
        SetEndGameUiActive(false);
        RefreshPlaceableAreas();
        SetPlaceableAreasActive(true);
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
    #region End Game UI

    /// <summary>
    /// Toggles the end-game panel and result buttons.
    /// </summary>
    /// <param name="isActive">Whether the end-game UI should be active.</param>
    private void SetEndGameUiActive(bool isActive) {
        var gameOverUi = GameOverUiReferences.GetCurrent();
        if (gameOverUi == null) {
            Debug.LogWarning("[BattleDebug] Cannot toggle game-over UI because no GameOverUiReferences are registered.");
            return;
        }

        if (isActive) {
            gameOverUi.Show(BattleController.Instance != null ? BattleController.Instance.winningTeam : null);
        }
        else {
            gameOverUi.Hide();
        }
    }

    #endregion
    #region Placement Areas

    /// <summary>
    /// Finds placeable areas by tag, with old Inspector references kept as a fallback.
    /// </summary>
    private void RefreshPlaceableAreas() {
        _runtimePlaceableAreas.Clear();

        try {
            _runtimePlaceableAreas.AddRange(GameObject.FindGameObjectsWithTag(PlaceableAreaTag));
        }
        catch (UnityException) {
            Debug.LogWarning(
                $"Tag '{PlaceableAreaTag}' does not exist yet. " +
                "Create it in Unity Tags and assign it to placement-area GameObjects to remove scene references.");
        }

        if (_runtimePlaceableAreas.Count > 0) return;

        if (unitPlacementArea != null) {
            _runtimePlaceableAreas.Add(unitPlacementArea.gameObject);
        }

        _runtimePlaceableAreas.AddRange(placeableAreas.Where(area => area != null));
    }

    /// <summary>
    /// Enables or disables placement-area GameObjects for the current game state.
    /// </summary>
    /// <param name="isActive">Whether placement areas should be active.</param>
    private void SetPlaceableAreasActive(bool isActive) {
        if (_runtimePlaceableAreas.Count == 0) {
            RefreshPlaceableAreas();
        }

        foreach (var area in _runtimePlaceableAreas.Where(area => area != null)) {
            area.SetActive(isActive);
        }
    }

    #endregion
    #region Time

    /// <summary>
    /// Waits before pausing so end-of-battle animations can complete.
    /// </summary>
    private IEnumerator PauseAfterEndGameDelay() {
        yield return new WaitForSecondsRealtime(endGameDelaySeconds);

        SetTimeMode(TimeScaleButtonController.TimeMode.Pause);
        if (!TryShowEndGameUi()) {
            yield return null;
            TryShowEndGameUi();
        }

        _endGameCoroutine = null;
    }

    /// <summary>
    /// Attempts to show the current scene's game-over UI.
    /// </summary>
    /// <returns>True when a game-over UI provider was available.</returns>
    private bool TryShowEndGameUi() {
        var gameOverUi = GameOverUiReferences.GetCurrent();
        if (gameOverUi == null) {
            Debug.LogWarning("[BattleDebug] Cannot show game-over UI yet because no GameOverUiReferences are registered.");
            return false;
        }

        gameOverUi.Show(BattleController.Instance != null ? BattleController.Instance.winningTeam : null);
        return true;
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
        var controller = TimeScaleButtonController.Instance != null
            ? TimeScaleButtonController.Instance
            : timeScaleButtonController;

        if (controller != null) {
            controller.ApplyTimeMode(timeMode);
            return;
        }

        Time.timeScale = timeMode == TimeScaleButtonController.TimeMode.Pause ? 0f : 1f;
    }

    #endregion
}
