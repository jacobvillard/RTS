using System.Collections.Generic;
using _Scripts.GameManagement;
using _Scripts.Units;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Handles pre-round unit purchasing, selection, placement, and setup reset.
/// </summary>
public class UnitPlacer : MonoBehaviour {

    #region Types

    private enum SelectedUnitType { None, Infantry, Cavalry, Musket }

    #endregion
    #region Variables

    [Header("Budget")]
    [SerializeField] private LevelStats levelStats; // Level money display and starting money source.
    [SerializeField] private int infantryUnitCost = 50; // Infantry purchase cost.
    [SerializeField] private int cavalryUnitCost = 100; // Cavalry purchase cost.
    [SerializeField] private int musketUnitCost = 60;   // Musket purchase cost.

    [Header("Placement")]
    [SerializeField] private LayerMask placeableLayer = 1 << 8; // Valid placement layer.
    [SerializeField] private LayerMask blockedByUnitLayers;     // Unit layers that block placement.
    [SerializeField] private Grid placementGrid;                // Optional grid used to snap placement.
    [SerializeField] private float occupiedCheckRadius = 0.35f; // Radius used to detect occupied spaces.
    [SerializeField] private List<GameObject> placedUnits = new(); // Units placed during setup.

    [Header("Cost Text")]
    [SerializeField] private TextMeshProUGUI infantryUnitCostText; // Infantry cost label.
    [SerializeField] private TextMeshProUGUI cavalryUnitCostText;  // Cavalry cost label.
    [SerializeField] private TextMeshProUGUI musketUnitCostText;   // Musket cost label.
    [SerializeField] private Color affordableCostColour = Color.white; // Cost label colour when affordable.
    [SerializeField] private Color unaffordableCostColour = Color.red; // Cost label colour when unaffordable.

    [Header("Buttons")]
    [SerializeField] private Button infantryUnitButton; // Infantry selection button.
    [SerializeField] private Button cavalryUnitButton;  // Cavalry selection button.
    [SerializeField] private Button musketUnitButton;   // Musket selection button.
    [SerializeField] private Color selectedButtonColour = new(0.25f, 0.55f, 1f); // Selected unit button colour.
    [SerializeField] private Color unselectedButtonColour = Color.white;         // Unselected unit button colour.

    [Header("Prefabs")]
    [SerializeField] private GameObject infantryUnitPrefab; // Infantry unit prefab.
    [SerializeField] private GameObject cavalryUnitPrefab;  // Cavalry unit prefab.
    [SerializeField] private GameObject musketUnitPrefab;   // Musket unit prefab.

    private int _money;                         // Current placement money.
    private int _placedUnitsCost;               // Total cost of placed units.
    private SelectedUnitType _selectedUnitType = SelectedUnitType.None; // Unit selected for placement.

    public int placedUnitsCount => placedUnits.Count;

    #endregion
    #region Unity Methods

    private void Start() {
        InitializeReferences();
        InitializeMoney();
        UpdateUnitCostText();
        UpdateButtonVisuals();
    }

    private void Update() {
        if (!CanHandlePlacementInput()) return;
        if (TrySelectPlacedUnitAtMousePosition()) return;
        if (_selectedUnitType == SelectedUnitType.None) return;

        TryPlaceUnitAtMousePosition();
    }

    #endregion
    #region Public Methods

    /// <summary>
    /// Selects the unit type used by the next valid placement click.
    /// </summary>
    /// <param name="unitType">The unit type name sent by the UI button.</param>
    public void SetSelectedUnitType(string unitType) {
        AudioManager.Instance?.PlayDefaultButtonSound();

        switch (unitType) {
            case "Infantry":
                _selectedUnitType = SelectedUnitType.Infantry;
                break;
            case "Cavalry":
                _selectedUnitType = SelectedUnitType.Cavalry;
                break;
            case "Musket":
                _selectedUnitType = SelectedUnitType.Musket;
                break;
            default:
                _selectedUnitType = SelectedUnitType.None;
                Debug.LogWarning("Unknown unit type selected: " + unitType);
                break;
        }

        UpdateButtonVisuals();
    }

    /// <summary>
    /// Removes all setup-placed units and resets the placement budget.
    /// </summary>
    public void ClearUnits() {
        AudioManager.Instance?.PlayClearUnits();

        foreach (var unit in placedUnits) {
            if (unit != null) {
                Destroy(unit);
            }
        }

        placedUnits.Clear();
        _placedUnitsCost = 0;
        _selectedUnitType = SelectedUnitType.None;
        _money = levelStats != null ? levelStats.startMoney : 0;

        UpdateMoneyText();
        UpdateUnitCostText();
        UpdateButtonVisuals();
        
        BattleController.Instance?.ClearAllPlayerUnits();
    }

    #endregion
    #region Initialization

    /// <summary>
    /// Finds optional references when the Inspector did not provide them.
    /// </summary>
    private void InitializeReferences() {
        levelStats ??= GameManager.Instance != null ? GameManager.Instance.LevelStats : null;
        placementGrid ??= FindObjectOfType<Grid>();

        if (blockedByUnitLayers.value == 0) {
            blockedByUnitLayers = LayerMask.GetMask("Player", "AI");
        }
    }

    /// <summary>
    /// Sets starting money and refreshes the money display.
    /// </summary>
    private void InitializeMoney() {
        _money = levelStats != null ? levelStats.startMoney : 0;
        UpdateMoneyText();
    }

    #endregion
    #region Input

    /// <summary>
    /// Checks whether the current frame should process placement input.
    /// </summary>
    /// <returns>True when placement input should be handled.</returns>
    private bool CanHandlePlacementInput() {
        if (!Input.GetMouseButtonDown(0)) return false;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return false;
        if (GameManager.Instance != null && !GameManager.Instance.IsPreGame()) return false;

        return true;
    }

    /// <summary>
    /// Attempts to place the selected unit type at the clicked world position.
    /// </summary>
    private void TryPlaceUnitAtMousePosition() {
        var mainCamera = Camera.main;
        if (mainCamera == null) return;

        var worldPosition = GetMouseWorldPosition(mainCamera);
        PlaceUnit(SnapToGrid(worldPosition));
    }

    /// <summary>
    /// Selects an existing player unit under the mouse during setup.
    /// </summary>
    /// <returns>True when a selectable placed unit was found.</returns>
    private bool TrySelectPlacedUnitAtMousePosition() {
        var mainCamera = Camera.main;
        if (mainCamera == null) return false;

        var worldPosition = GetMouseWorldPosition(mainCamera);
        var hits = Physics2D.OverlapPointAll(worldPosition);

        foreach (var hit in hits) {
            var unit = GetUnitFromCollider(hit);
            if (unit == null || unit.team != Team.Player || !unit.IsAlive) continue;

            SelectPlacedUnit(unit);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Converts the current mouse position into a 2D world position.
    /// </summary>
    /// <param name="mainCamera">The camera used to project the mouse position.</param>
    /// <returns>The mouse position in world space.</returns>
    private static Vector3 GetMouseWorldPosition(Camera mainCamera) {
        var mousePosition = Input.mousePosition;
        mousePosition.z = -mainCamera.transform.position.z;

        var worldPosition = mainCamera.ScreenToWorldPoint(mousePosition);
        worldPosition.z = 0f;
        return worldPosition;
    }

    #endregion
    #region Selection

    /// <summary>
    /// Gets a Unit from a clicked collider or one of its parents.
    /// </summary>
    /// <param name="hit">The collider hit by the placement click.</param>
    /// <returns>The related unit, or null.</returns>
    private static Unit GetUnitFromCollider(Collider2D hit) {
        if (hit.TryGetComponent(out Unit unit)) {
            return unit;
        }

        return hit.GetComponentInParent<Unit>();
    }

    /// <summary>
    /// Selects a placed unit and clears the current placement button selection.
    /// </summary>
    /// <param name="unit">The placed unit to select.</param>
    private void SelectPlacedUnit(Unit unit) {
        var selection = unit.GetComponentInChildren<SelectedUnit>();
        if (selection != null) {
            selection.SelectUnit();
        }
        else if (BattleController.Instance != null) {
            BattleController.Instance.SelectUnit(unit);
        }

        _selectedUnitType = SelectedUnitType.None;
        UpdateButtonVisuals();
    }

    #endregion
    #region Placement

    /// <summary>
    /// Places the selected unit at the given position when budget and map rules allow it.
    /// </summary>
    /// <param name="position">The target world position.</param>
    private void PlaceUnit(Vector3 position) {
        var unitPrefab = GetSelectedUnitPrefab();
        var unitCost = GetSelectedUnitCost();

        if (unitPrefab == null || unitCost <= 0) return;

        if (_money < unitCost) {
            Debug.Log("Not enough money to place unit.");
            AudioManager.Instance?.PlayPlacementFailed();
            UpdateUnitCostText();
            return;
        }

        if (!IsPositionPlaceable(position)) {
            AudioManager.Instance?.PlayPlacementFailed();
            LogPlaceableLayerMiss(position);
            return;
        }

        if (IsPositionOccupied(position)) {
            Debug.Log("That placement cell is already occupied.");
            AudioManager.Instance?.PlayPlacementFailed();
            return;
        }

        var newUnit = Instantiate(unitPrefab, position, Quaternion.identity);
        AudioManager.Instance?.PlayPlaceUnit(position);
        placedUnits.Add(newUnit);

        _money -= unitCost;
        _placedUnitsCost += unitCost;

        UpdateMoneyText();
        UpdateUnitCostText();
    }

    /// <summary>
    /// Snaps a world position to the center of the configured grid cell.
    /// </summary>
    /// <param name="position">The unsnapped world position.</param>
    /// <returns>The snapped world position, or the original position if no grid exists.</returns>
    private Vector3 SnapToGrid(Vector3 position) {
        if (placementGrid == null) return position;

        var cellPosition = placementGrid.WorldToCell(position);
        var snappedPosition = placementGrid.GetCellCenterWorld(cellPosition);
        snappedPosition.z = 0f;
        return snappedPosition;
    }

    /// <summary>
    /// Checks whether the target position overlaps a collider on the placeable layer.
    /// </summary>
    /// <param name="position">The world position being tested.</param>
    /// <returns>True when the position can receive placed units.</returns>
    private bool IsPositionPlaceable(Vector3 position) {
        return Physics2D.OverlapPoint(position, placeableLayer) != null;
    }

    /// <summary>
    /// Checks whether an existing placed unit or unit collider blocks the target position.
    /// </summary>
    /// <param name="position">The world position being tested.</param>
    /// <returns>True when another unit occupies the placement space.</returns>
    private bool IsPositionOccupied(Vector3 position) {
        RemoveMissingPlacedUnits();

        foreach (var placedUnit in placedUnits) {
            if (placedUnit == null) continue;
            if (IsSamePlacementCell(placedUnit.transform.position, position)) return true;
            if (Vector2.Distance(placedUnit.transform.position, position) <= occupiedCheckRadius) return true;
        }

        return Physics2D.OverlapCircle(position, occupiedCheckRadius, blockedByUnitLayers) != null;
    }

    /// <summary>
    /// Checks whether two positions occupy the same optional grid cell.
    /// </summary>
    /// <param name="firstPosition">The first world position.</param>
    /// <param name="secondPosition">The second world position.</param>
    /// <returns>True when both positions are in the same placement grid cell.</returns>
    private bool IsSamePlacementCell(Vector3 firstPosition, Vector3 secondPosition) {
        return placementGrid != null &&
               placementGrid.WorldToCell(firstPosition) == placementGrid.WorldToCell(secondPosition);
    }

    /// <summary>
    /// Removes destroyed unit references from the placement list.
    /// </summary>
    private void RemoveMissingPlacedUnits() {
        placedUnits.RemoveAll(unit => unit == null);
    }

    #endregion
    #region Unit Lookup

    /// <summary>
    /// Gets the prefab for the selected unit type.
    /// </summary>
    /// <returns>The selected prefab, or null.</returns>
    private GameObject GetSelectedUnitPrefab() {
        return _selectedUnitType switch {
            SelectedUnitType.Infantry => infantryUnitPrefab,
            SelectedUnitType.Cavalry => cavalryUnitPrefab,
            SelectedUnitType.Musket => musketUnitPrefab,
            _ => null
        };
    }

    /// <summary>
    /// Gets the cost for the selected unit type.
    /// </summary>
    /// <returns>The selected unit cost, or zero.</returns>
    private int GetSelectedUnitCost() {
        return _selectedUnitType switch {
            SelectedUnitType.Infantry => infantryUnitCost,
            SelectedUnitType.Cavalry => cavalryUnitCost,
            SelectedUnitType.Musket => musketUnitCost,
            _ => 0
        };
    }

    #endregion
    #region UI

    /// <summary>
    /// Sends the current money amount to the level stats UI.
    /// </summary>
    private void UpdateMoneyText() {
        if (levelStats != null) {
            levelStats.UpdateMoney(_money);
        }
    }

    /// <summary>
    /// Updates cost labels and affordability colours.
    /// </summary>
    private void UpdateUnitCostText() {
        UpdateCostText(infantryUnitCostText, infantryUnitCost);
        UpdateCostText(cavalryUnitCostText, cavalryUnitCost);
        UpdateCostText(musketUnitCostText, musketUnitCost);
    }

    /// <summary>
    /// Updates a single cost label.
    /// </summary>
    /// <param name="costText">The text component to update.</param>
    /// <param name="unitCost">The unit cost to display.</param>
    private void UpdateCostText(TextMeshProUGUI costText, int unitCost) {
        if (costText == null) return;

        costText.text = unitCost.ToString();
        costText.color = _money >= unitCost ? affordableCostColour : unaffordableCostColour;
    }

    /// <summary>
    /// Highlights the selected unit button and resets the others.
    /// </summary>
    private void UpdateButtonVisuals() {
        SetButtonColour(infantryUnitButton, _selectedUnitType == SelectedUnitType.Infantry);
        SetButtonColour(cavalryUnitButton, _selectedUnitType == SelectedUnitType.Cavalry);
        SetButtonColour(musketUnitButton, _selectedUnitType == SelectedUnitType.Musket);
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
            image.color = selected ? selectedButtonColour : unselectedButtonColour;
        }
    }

    #endregion
    #region Debug

    /// <summary>
    /// Logs colliders hit by a failed placeable-layer check.
    /// </summary>
    /// <param name="position">The failed placement position.</param>
    private void LogPlaceableLayerMiss(Vector3 position) {
        var hits = Physics2D.OverlapPointAll(position);
        var placeableLayerNames = LayerMaskToNames(placeableLayer);

        if (hits.Length == 0) {
            Debug.Log(
                "That position is not on the placeable layer. " +
                $"Clicked world position {position}, but no 2D collider was found there. " +
                $"Expected layer mask: {placeableLayerNames}.");
            return;
        }

        var message =
            "That position is not on the placeable layer. " +
            $"Clicked world position {position}. Expected layer mask: {placeableLayerNames}. " +
            "Colliders hit:";

        foreach (var hit in hits) {
            var hitLayerName = LayerMask.LayerToName(hit.gameObject.layer);
            if (string.IsNullOrEmpty(hitLayerName)) {
                hitLayerName = "Layer " + hit.gameObject.layer;
            }

            message +=
                $"\n- {hit.name} on GameObject '{hit.gameObject.name}', " +
                $"layer '{hitLayerName}' ({hit.gameObject.layer}), trigger={hit.isTrigger}";
        }

        Debug.Log(message);
    }

    /// <summary>
    /// Converts a layer mask into readable Unity layer names.
    /// </summary>
    /// <param name="layerMask">The layer mask to describe.</param>
    /// <returns>A comma-separated list of layer names and indexes.</returns>
    private static string LayerMaskToNames(LayerMask layerMask) {
        var names = new List<string>();

        for (var layer = 0; layer < 32; layer++) {
            if ((layerMask.value & (1 << layer)) == 0) continue;

            var layerName = LayerMask.LayerToName(layer);
            if (string.IsNullOrEmpty(layerName)) {
                layerName = "Layer " + layer;
            }

            names.Add($"{layerName} ({layer})");
        }

        return names.Count > 0 ? string.Join(", ", names) : "Nothing";
    }

    #endregion
}
