using System.Collections.Generic;
using _Scripts.GameManagement;
using _Scripts.Units;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Handles pre-round unit purchasing and placement on valid map tiles.
/// </summary>
public class UnitPlacer : MonoBehaviour {
    private enum SelectedUnitType { None, Infantry, Cavalry, Musket }

    [Header("Budget")]
    [SerializeField] private LevelStats levelStats;
    [SerializeField] private int infantryUnitCost = 50;
    [SerializeField] private int cavalryUnitCost = 100;
    [SerializeField] private int musketUnitCost = 60;

    [Header("Placement")]
    [SerializeField] private LayerMask placeableLayer = 1 << 8;
    [SerializeField] private LayerMask blockedByUnitLayers;
    [SerializeField] private Grid placementGrid;
    [SerializeField] private float occupiedCheckRadius = 0.35f;
    [SerializeField] private List<GameObject> placedUnits = new();

    [Header("Cost Text")]
    [SerializeField] private TextMeshProUGUI infantryUnitCostText;
    [SerializeField] private TextMeshProUGUI cavalryUnitCostText;
    [SerializeField] private TextMeshProUGUI musketUnitCostText;
    [SerializeField] private Color affordableCostColour = Color.white;
    [SerializeField] private Color unaffordableCostColour = Color.red;

    [Header("Buttons")]
    [SerializeField] private Button infantryUnitButton;
    [SerializeField] private Button cavalryUnitButton;
    [SerializeField] private Button musketUnitButton;
    [SerializeField] private Color selectedButtonColour = new(0.25f, 0.55f, 1f);
    [SerializeField] private Color unselectedButtonColour = Color.white;

    [Header("Prefabs")]
    [SerializeField] private GameObject infantryUnitPrefab;
    [SerializeField] private GameObject cavalryUnitPrefab;
    [SerializeField] private GameObject musketUnitPrefab;

    private int _money;
    private int _placedUnitsCost;
    private SelectedUnitType _selectedUnitType = SelectedUnitType.None;
    
    public int placedUnitsCount => placedUnits.Count;

    /// <summary>
    /// Initializes budget state, finds optional scene references, and refreshes the UI.
    /// </summary>
    private void Start() {
        levelStats ??= GameManager.Instance != null ? GameManager.Instance.LevelStats : null;
        placementGrid ??= FindObjectOfType<Grid>();

        if (blockedByUnitLayers.value == 0) {
            blockedByUnitLayers = LayerMask.GetMask("Player", "AI");
        }

        _money = levelStats != null ? levelStats.startMoney : 0;
        UpdateMoneyText();
        UpdateUnitCostText();
        UpdateButtonVisuals();
    }

    /// <summary>
    /// Handles pre-game clicks for selecting placed units or placing a selected unit type.
    /// </summary>
    private void Update() {
        if (!Input.GetMouseButtonDown(0)) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        if (GameManager.Instance != null && !GameManager.Instance.IsPreGame()) return;

        if (TrySelectPlacedUnitAtMousePosition()) return;
        if (_selectedUnitType == SelectedUnitType.None) return;

        TryPlaceUnitAtMousePosition();
    }

    /// <summary>
    /// Selects the unit type used by the next valid placement click.
    /// </summary>
    /// <param name="unitType">The unit type name sent by the UI button.</param>
    public void SetSelectedUnitType(string unitType) {
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
    /// Removes all units placed during setup and refunds their cost.
    /// </summary>
    public void ClearUnits() {
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

    /// <summary>
    /// Converts the mouse position into a world position and places a unit there if valid.
    /// </summary>
    private void TryPlaceUnitAtMousePosition() {
        var mainCamera = Camera.main;
        if (mainCamera == null) return;

        var worldPosition = GetMouseWorldPosition(mainCamera);
        PlaceUnit(SnapToGrid(worldPosition));
    }

    /// <summary>
    /// Selects an existing player unit under the mouse during pre-game setup.
    /// </summary>
    /// <returns>True when a selectable placed unit was found.</returns>
    private bool TrySelectPlacedUnitAtMousePosition() {
        var mainCamera = Camera.main;
        if (mainCamera == null) return false;

        var worldPosition = GetMouseWorldPosition(mainCamera);
        var hits = Physics2D.OverlapPointAll(worldPosition);

        foreach (var hit in hits) {
            if (!hit.TryGetComponent(out Unit unit)) {
                unit = hit.GetComponentInParent<Unit>();
            }

            if (unit == null || unit.team != Team.Player || !unit.IsAlive) continue;

            var selection = unit.GetComponentInChildren<SelectedUnit>();
            if (selection != null) {
                selection.SelectUnit();
            }
            else if (BattleController.Instance != null) {
                BattleController.Instance.SelectUnit(unit);
            }

            _selectedUnitType = SelectedUnitType.None;
            UpdateButtonVisuals();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Converts the current mouse position into a 2D world position.
    /// </summary>
    /// <param name="mainCamera">The camera used to project screen coordinates.</param>
    /// <returns>The mouse position in world space with z reset to zero.</returns>
    private static Vector3 GetMouseWorldPosition(Camera mainCamera) {
        var mousePosition = Input.mousePosition;
        mousePosition.z = -mainCamera.transform.position.z;

        var worldPosition = mainCamera.ScreenToWorldPoint(mousePosition);
        worldPosition.z = 0f;
        return worldPosition;
    }

    /// <summary>
    /// Places the selected unit at the given position when budget and map rules allow it.
    /// </summary>
    /// <param name="position">The target world position for the new unit.</param>
    private void PlaceUnit(Vector3 position) {
        var unitPrefab = GetSelectedUnitPrefab();
        var unitCost = GetSelectedUnitCost();

        if (unitPrefab == null || unitCost <= 0) return;

        if (_money < unitCost) {
            Debug.Log("Not enough money to place unit.");
            UpdateUnitCostText();
            return;
        }

        if (!IsPositionPlaceable(position)) {
            LogPlaceableLayerMiss(position);
            return;
        }

        if (IsPositionOccupied(position)) {
            Debug.Log("That placement cell is already occupied.");
            return;
        }

        var newUnit = Instantiate(unitPrefab, position, Quaternion.identity);
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
    /// <returns>The grid cell center, or the original position when no grid exists.</returns>
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
    /// Logs which 2D colliders are under a failed placement click and which layer each uses.
    /// </summary>
    /// <param name="position">The world position that failed the placeable-layer check.</param>
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
    /// Converts a layer mask into readable Unity layer names for debug output.
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

    /// <summary>
    /// Checks whether an existing placed unit or unit collider blocks the target position.
    /// </summary>
    /// <param name="position">The world position being tested.</param>
    /// <returns>True when another unit occupies the same placement space.</returns>
    private bool IsPositionOccupied(Vector3 position) {
        RemoveMissingPlacedUnits();

        foreach (var placedUnit in placedUnits) {
            if (placedUnit == null) continue;
            if (placementGrid != null &&
                placementGrid.WorldToCell(placedUnit.transform.position) == placementGrid.WorldToCell(position)) {
                return true;
            }

            if (Vector2.Distance(placedUnit.transform.position, position) <= occupiedCheckRadius) {
                return true;
            }
        }

        return Physics2D.OverlapCircle(position, occupiedCheckRadius, blockedByUnitLayers) != null;
    }

    /// <summary>
    /// Removes destroyed unit references from the placement list.
    /// </summary>
    private void RemoveMissingPlacedUnits() {
        placedUnits.RemoveAll(unit => unit == null);
    }

    /// <summary>
    /// Gets the prefab for the currently selected unit type.
    /// </summary>
    /// <returns>The selected prefab, or null when no valid unit type is selected.</returns>
    private GameObject GetSelectedUnitPrefab() {
        return _selectedUnitType switch {
            SelectedUnitType.Infantry => infantryUnitPrefab,
            SelectedUnitType.Cavalry => cavalryUnitPrefab,
            SelectedUnitType.Musket => musketUnitPrefab,
            _ => null
        };
    }

    /// <summary>
    /// Gets the cost for the currently selected unit type.
    /// </summary>
    /// <returns>The selected unit cost, or zero when no valid unit type is selected.</returns>
    private int GetSelectedUnitCost() {
        return _selectedUnitType switch {
            SelectedUnitType.Infantry => infantryUnitCost,
            SelectedUnitType.Cavalry => cavalryUnitCost,
            SelectedUnitType.Musket => musketUnitCost,
            _ => 0
        };
    }

    /// <summary>
    /// Sends the current money amount to the level stats UI.
    /// </summary>
    private void UpdateMoneyText() {
        if (levelStats != null) {
            levelStats.UpdateMoney(_money);
        }
    }

    /// <summary>
    /// Updates each cost label and marks unaffordable unit types in red.
    /// </summary>
    private void UpdateUnitCostText() {
        UpdateCostText(infantryUnitCostText, infantryUnitCost);
        UpdateCostText(cavalryUnitCostText, cavalryUnitCost);
        UpdateCostText(musketUnitCostText, musketUnitCost);
    }

    /// <summary>
    /// Updates a single cost label with text and affordability colour.
    /// </summary>
    /// <param name="costText">The text component to update.</param>
    /// <param name="unitCost">The cost shown in the text component.</param>
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
}
