using _Scripts.GameManagement;
using UnityEngine;

/// <summary>
/// Handles the transition from pre-game placement into active battle.
/// </summary>
public class GameStarter : MonoBehaviour {

    #region Variables

    [Header("Scene References")]
    [SerializeField] private UnitPlacer unitPlacer;     // Tracks whether the player has placed units.
    [SerializeField] private GameObject preGameUI;      // UI shown during unit placement.

    #endregion
    #region Public Methods

    /// <summary>
    /// Starts the battle if the player has placed at least one unit.
    /// </summary>
    public void StartGame() {
        if (unitPlacer != null && unitPlacer.placedUnitsCount == 0) {
            Debug.LogWarning("No units placed! Starting the game without any units may lead to unexpected behaviour.");
            return;
        }

        GameManager.Instance.StartGame();
        AudioManager.Instance?.PlayRoundStart();
        AudioManager.Instance?.PlayRandomRoundMusic();

        if (preGameUI != null) {
            preGameUI.SetActive(false);
        }
    }

    #endregion
}
