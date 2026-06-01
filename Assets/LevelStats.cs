using TMPro;
using _Scripts.GameManagement;
using _Scripts.UI;
using UnityEngine;

/// <summary>
/// Stores and displays level-level resource values.
/// </summary>
public class LevelStats : MonoBehaviour {

    #region Variables

    [Header("Money")]
    public int startMoney = 220;                        // Starting money available before the battle.
    public int moneyAmt;                                // Current money amount displayed to the player.
    [SerializeField] private TextMeshProUGUI moneyText; // Optional money label fallback.

    #endregion
    #region Unity Methods

    private void Awake() {
        ApplyLevelSettings();
        ResolveTextReferences();
    }

    private void Start() {
        ResolveTextReferences();
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

    /// <summary>
    /// Refreshes level money and UI references after a new scene loads.
    /// </summary>
    public void RefreshForSceneLoad() {
        moneyText = null;
        ApplyLevelSettings();
        ResolveTextReferences();
        UpdateMoney(startMoney);
    }

    #endregion
    #region Initialization

    /// <summary>
    /// Applies per-level starting money from the Resources-loaded level settings database.
    /// </summary>
    private void ApplyLevelSettings() {
        var database = LevelSettingsDatabase.Load();
        if (database == null) return;

        var settings = database.GetCurrentLevelSettings();
        startMoney = settings.startMoney;
    }

    /// <summary>
    /// Finds UI text from prefab-root references when no direct fallback is assigned.
    /// </summary>
    private void ResolveTextReferences() {
        if (moneyText != null) return;

        if (PrefabTextReferences.TryGetText(PrefabTextReferences.TextSlot.Money, out var text)) {
            moneyText = text;
        }
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
