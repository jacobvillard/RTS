using TMPro;
using UnityEngine;

/// <summary>
/// Stores and displays level-level resource values.
/// </summary>
public class LevelStats : MonoBehaviour {

    #region Variables

    [Header("Money")]
    public int startMoney = 022;                        // Starting money available before the battle.
    public int moneyAmt;                                // Current money amount displayed to the player.
    [SerializeField] private TextMeshProUGUI moneyText; // Money label in the UI.

    #endregion
    #region Unity Methods

    private void Start() {
        UpdateMoney(moneyAmt);
    }

    #endregion
    #region Public Methods

    /// <summary>
    /// Updates the current money value and refreshes the UI.
    /// </summary>
    /// <param name="amount">The new money amount.</param>
    public void UpdateMoney(int amount) {
        moneyAmt = amount;
        SetText(moneyText, moneyAmt.ToString());
    }

    #endregion
    #region UI

    /// <summary>
    /// Updates a TextMeshPro label when the reference exists.
    /// </summary>
    /// <param name="text">The text component to update.</param>
    /// <param name="value">The display value.</param>
    private void SetText(TextMeshProUGUI text, string value) {
        if (text == null) return;

        text.text = value;
    }

    #endregion
}
