using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public enum GameState
{
    PreGame,
    Playing,
    Paused,
    GameOver
}

[RequireComponent(typeof(LevelStats))]
public class GameManager : _Scripts.GameManagement.Singleton<GameManager>
{
    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    [SerializeField] private GameState gameState = GameState.PreGame;

    public GameState GameState => gameState;
    public LevelStats LevelStats { get; private set; }
    [SerializeField] private BoxCollider2D unitPlacementArea;
    
    [SerializeField] private TimeScaleButtonController timeScaleButtonController;
    [SerializeField] private float endGameDelaySeconds = 10f;
    private Coroutine _endGameCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        LevelStats = GetComponent<LevelStats>();
    }

    public void SetGameState(GameState newState)
    {
        gameState = newState;
    }

    public void StartGame()
    {
        if (_endGameCoroutine != null)
        {
            StopCoroutine(_endGameCoroutine);
            _endGameCoroutine = null;
        }

        SetGameState(GameState.Playing);
        Time.timeScale = 1f;
        timeScaleButtonController.ApplyTimeMode(TimeScaleButtonController.TimeMode.Play);
        unitPlacementArea.enabled = false;
    }

    public void SetPreGame()
    {
        SetGameState(GameState.PreGame);
        Time.timeScale = 1f;
    }

    public void EndGame()
    {
        SetGameState(GameState.GameOver);

        if (_endGameCoroutine != null)
        {
            StopCoroutine(_endGameCoroutine);
        }

        _endGameCoroutine = StartCoroutine(PauseAfterEndGameDelay());
    }

    private IEnumerator PauseAfterEndGameDelay()
    {
        yield return new WaitForSecondsRealtime(endGameDelaySeconds);

        Time.timeScale = 0f;
        timeScaleButtonController.ApplyTimeMode(TimeScaleButtonController.TimeMode.Pause);
        _endGameCoroutine = null;
    }

    public bool IsPreGame()
    {
        return gameState == GameState.PreGame;
    }
}
