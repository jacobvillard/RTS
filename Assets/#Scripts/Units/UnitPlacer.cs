using System;
using System.Collections.Generic;
using _Scripts.GameManagement;
using _Scripts.UI;
using _Scripts.Units;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Handles pre-round unit purchasing, selection, placement, and setup reset.
/// </summary>
public class UnitPlacer : Singleton<UnitPlacer> {

    #region Types

    private enum SelectedUnitType {
        None,
        Infantry,
        Cavalry,
        Musket,
        Officer,
        Scout,
        Pikemen,
        Skirmisher,
        Dragoon,
        Bannermen
    }

    #endregion
    #region Variables

    [Header("Budget")]
    [SerializeField] private LevelStats levelStats; // Level money display and starting money source.
    [SerializeField] private int infantryUnitCost = 50; // Infantry purchase cost.
    [SerializeField] private int cavalryUnitCost = 100; // Cavalry purchase cost.
    [SerializeField] private int musketUnitCost = 60;   // Musket purchase cost.
    [SerializeField] private int officerUnitCost = 70;  // Officer purchase cost.
    [SerializeField] private int scoutUnitCost = 35;    // Scout purchase cost.
    [SerializeField] private int pikemenUnitCost = 90;  // Pikemen purchase cost.
    [SerializeField] private int skirmisherUnitCost = 120; // Skirmisher purchase cost.
    [SerializeField] private int dragoonUnitCost = 130; // Dragoon purchase cost.
    [SerializeField] private int bannermenUnitCost = 80; // Bannermen purchase cost.

    [Header("Placement")]
    [SerializeField] private LayerMask placeableLayer = 1 << 8; // Valid placement layer.
    [SerializeField] private LayerMask blockedByUnitLayers;     // Unit layers that block placement.
    [SerializeField] private Grid placementGrid;                // Optional grid used to snap placement.
    [SerializeField] private float occupiedCheckRadius = 0.35f; // Radius used to detect occupied spaces.
    [SerializeField] private float tapMovementThreshold = 20f;  // Maximum screen movement still treated as a tap.
    [SerializeField] private List<GameObject> placedUnits = new(); // Units placed during setup.

    [Header("Cost Text")]
    [SerializeField] private TextMeshProUGUI infantryUnitCostText; // Infantry cost label.
    [SerializeField] private TextMeshProUGUI cavalryUnitCostText;  // Cavalry cost label.
    [SerializeField] private TextMeshProUGUI musketUnitCostText;   // Musket cost label.
    [SerializeField] private TextMeshProUGUI officerUnitCostText;  // Officer cost label.
    [SerializeField] private TextMeshProUGUI scoutUnitCostText;    // Scout cost label.
    [SerializeField] private TextMeshProUGUI pikemenUnitCostText;  // Pikemen cost label.
    [SerializeField] private TextMeshProUGUI skirmisherUnitCostText; // Skirmisher cost label.
    [SerializeField] private TextMeshProUGUI dragoonUnitCostText;  // Dragoon cost label.
    [SerializeField] private TextMeshProUGUI bannermenUnitCostText; // Bannermen cost label.
    [SerializeField] private Color affordableCostColour = Color.white; // Cost label colour when affordable.
    [SerializeField] private Color unaffordableCostColour = Color.red; // Cost label colour when unaffordable.
    [SerializeField] private Color disabledIconBackgroundColour = new(0.42f, 0.42f, 0.42f, 1f); // Icon background colour when unaffordable.

    [Header("Buttons")]
    [SerializeField] private Button infantryUnitButton; // Infantry selection button.
    [SerializeField] private Button cavalryUnitButton;  // Cavalry selection button.
    [SerializeField] private Button musketUnitButton;   // Musket selection button.
    [SerializeField] private Button officerUnitButton;  // Officer selection button.
    [SerializeField] private Button scoutUnitButton;    // Scout selection button.
    [SerializeField] private Button pikemenUnitButton;  // Pikemen selection button.
    [SerializeField] private Button skirmisherUnitButton; // Skirmisher selection button.
    [SerializeField] private Button dragoonUnitButton;  // Dragoon selection button.
    [SerializeField] private Button bannermenUnitButton; // Bannermen selection button.

    [Header("Icon Backgrounds")]
    [SerializeField] private Image infantryIconBackground; // Infantry icon background.
    [SerializeField] private Image cavalryIconBackground;  // Cavalry icon background.
    [SerializeField] private Image musketIconBackground;   // Musket icon background.
    [SerializeField] private Image officerIconBackground;  // Officer icon background.
    [SerializeField] private Image scoutIconBackground;    // Scout icon background.
    [SerializeField] private Image pikemenIconBackground;  // Pikemen icon background.
    [SerializeField] private Image skirmisherIconBackground; // Skirmisher icon background.
    [SerializeField] private Image dragoonIconBackground;  // Dragoon icon background.
    [SerializeField] private Image bannermenIconBackground; // Bannermen icon background.

    [Header("Clear Button")]
    [SerializeField] private Button clearButton; // Button used to clear all units or the selected setup unit.
    [SerializeField] private TextMeshProUGUI clearButtonText; // Label updated with the current clear mode.
    [SerializeField] private string clearAllButtonLabel = "Clear All"; // Text shown when no placed unit is selected.
    [SerializeField] private string clearSelectedButtonLabel = "Clear Selected"; // Text shown when a placed unit is selected.
    [SerializeField] private Color clearAllButtonColour = Color.red; // Button colour for clear-all mode.
    [SerializeField] private Color clearSelectedButtonColour = new(1f, 0.28f, 0f, 1f); // Button colour for clear-selected mode.
    [SerializeField] private Button confirmButton; // Button used to start/confirm the setup.
    [SerializeField] private TextMeshProUGUI confirmButtonText; // Confirm button label used for hover scale.
    [SerializeField] private Color confirmButtonColour = new(0.55f, 0.84f, 0.23f, 1f); // Confirm button normal/hover colour.
    [SerializeField] private float buttonTextHoverScale = 1.08f; // Scale applied to clear/confirm text on hover.

    [Header("Prefabs")]
    [SerializeField] private GameObject infantryUnitPrefab; // Infantry unit prefab.
    [SerializeField] private GameObject cavalryUnitPrefab;  // Cavalry unit prefab.
    [SerializeField] private GameObject musketUnitPrefab;   // Musket unit prefab.
    [SerializeField] private GameObject officerUnitPrefab;  // Officer unit prefab.
    [SerializeField] private GameObject scoutUnitPrefab;    // Scout unit prefab.
    [SerializeField] private GameObject pikemenUnitPrefab;  // Pikemen unit prefab.
    [SerializeField] private GameObject skirmisherUnitPrefab; // Skirmisher unit prefab.
    [SerializeField] private GameObject dragoonUnitPrefab;  // Dragoon unit prefab.
    [SerializeField] private GameObject bannermenUnitPrefab; // Bannermen unit prefab.

    private int _money;                         // Current placement money.
    private int _placedUnitsCost;               // Total cost of placed units.
    private Vector2 _currentPointerScreenPosition; // Pointer position approved for this placement frame.
    private Vector2 _pointerStartScreenPosition; // Pointer position when the current tap began.
    private bool _isPointerPressActive;         // True while waiting to decide if a press is a tap or drag.
    private SelectedUnitType _selectedUnitType = SelectedUnitType.None; // Unit selected for placement.
    private readonly HashSet<Button> _runtimeBoundButtons = new(); // Buttons bound by this script at runtime.
    private readonly HashSet<Button> _hoverStyledButtons = new(); // Buttons with runtime hover text scaling.
    private readonly Dictionary<Transform, Vector3> _buttonTextBaseScales = new(); // Original button label scales.
    private readonly Dictionary<Image, Color> _iconBackgroundBaseColours = new(); // Original icon background colours.
    private bool _lastClearSelectedMode; // Cached clear-button state to avoid redundant visual updates.

    public int placedUnitsCount => placedUnits.Count;

    #endregion
    #region Unity Methods

    protected override void Awake() {
        base.Awake();

        if (Instance != this) {
            enabled = false;
        }
    }

    private void Start() {
        InitializeReferences();
        InitializeMoney();
        UpdateUnitCostText();
        UpdateButtonVisuals();
    }

    private void Update() {
        RefreshClearButtonStateIfNeeded();

        if (!TryGetPlacementTap(out _currentPointerScreenPosition)) return;
        if (TrySelectPlacedUnitAtPointerPosition()) return;
        if (_selectedUnitType == SelectedUnitType.None) return;

        TryPlaceUnitAtPointerPosition();
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
            case "Officer":
                _selectedUnitType = SelectedUnitType.Officer;
                break;
            case "Scout":
                _selectedUnitType = SelectedUnitType.Scout;
                break;
            case "Pikemen":
                _selectedUnitType = SelectedUnitType.Pikemen;
                break;
            case "Skirmisher":
                _selectedUnitType = SelectedUnitType.Skirmisher;
                break;
            case "Dragoon":
                _selectedUnitType = SelectedUnitType.Dragoon;
                break;
            case "Bannermen":
            case "Bannerman":
                _selectedUnitType = SelectedUnitType.Bannermen;
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
        if (TryClearSelectedPlacedUnit()) return;

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

        BattleController.Instance?.ClearAllPlayerUnits();
        UpdateMoneyText();
        UpdateUnitCostText();
        UpdateButtonVisuals();
    }

    /// <summary>
    /// Rebinds scene UI references and resets placement money after a scene load.
    /// </summary>
    public void RefreshForSceneLoad() {
        levelStats = GameManager.Instance != null ? GameManager.Instance.LevelStats : null;
        infantryUnitCostText = null;
        cavalryUnitCostText = null;
        musketUnitCostText = null;
        officerUnitCostText = null;
        scoutUnitCostText = null;
        pikemenUnitCostText = null;
        skirmisherUnitCostText = null;
        dragoonUnitCostText = null;
        bannermenUnitCostText = null;
        clearButton = null;
        clearButtonText = null;
        confirmButton = null;
        confirmButtonText = null;
        infantryUnitButton = null;
        cavalryUnitButton = null;
        musketUnitButton = null;
        officerUnitButton = null;
        scoutUnitButton = null;
        pikemenUnitButton = null;
        skirmisherUnitButton = null;
        dragoonUnitButton = null;
        bannermenUnitButton = null;
        infantryIconBackground = null;
        cavalryIconBackground = null;
        musketIconBackground = null;
        officerIconBackground = null;
        scoutIconBackground = null;
        pikemenIconBackground = null;
        skirmisherIconBackground = null;
        dragoonIconBackground = null;
        bannermenIconBackground = null;
        _runtimeBoundButtons.Clear();
        _iconBackgroundBaseColours.Clear();
        placedUnits.Clear();
        _placedUnitsCost = 0;
        _selectedUnitType = SelectedUnitType.None;

        InitializeReferences();
        InitializeMoney();
        UpdateUnitCostText();
        UpdateButtonVisuals();
    }

    #endregion
    #region Initialization

    /// <summary>
    /// Finds optional references when the Inspector did not provide them.
    /// </summary>
    private void InitializeReferences() {
        levelStats ??= GameManager.Instance != null ? GameManager.Instance.LevelStats : null;
        placementGrid ??= FindObjectOfType<Grid>();
        ResolveCostTextReferences();
        ResolveButtonReferences();
        ResolveIconBackgroundReferences();
        ResolveClearButtonReferences();
        ResolveConfirmButtonReferences();
        BindUnitButtons();
        BindHoverStyles();
        ApplyLevelUnitAvailability();
        NormalizeShopRowLayout();
        UpdateClearButtonState(true);
        UpdateConfirmButtonVisuals();

        if (blockedByUnitLayers.value == 0) {
            blockedByUnitLayers = LayerMask.GetMask("Player", "AI");
        }
    }

    /// <summary>
    /// Finds cost text from prefab-root references when no direct fallback has been assigned.
    /// </summary>
    private void ResolveCostTextReferences() {
        infantryUnitCostText = FindUnitCostText("InfantryUnit") ??
                               infantryUnitCostText ??
                               GetRegisteredText(PrefabTextReferences.TextSlot.InfantryUnitCost);
        cavalryUnitCostText = FindUnitCostText("Calvary", "CavalryUnit") ??
                              cavalryUnitCostText ??
                              GetRegisteredText(PrefabTextReferences.TextSlot.CavalryUnitCost);
        musketUnitCostText = FindUnitCostText("MusketUnit") ??
                             musketUnitCostText ??
                             GetRegisteredText(PrefabTextReferences.TextSlot.MusketUnitCost);
        officerUnitCostText = FindUnitCostText("OfficerUnit") ?? officerUnitCostText;
        scoutUnitCostText = FindUnitCostText("ScoutUnit") ?? scoutUnitCostText;
        pikemenUnitCostText = FindUnitCostText("PikemenUnit") ?? pikemenUnitCostText;
        skirmisherUnitCostText = FindUnitCostText("SkirmisherUnit") ?? skirmisherUnitCostText;
        dragoonUnitCostText = FindUnitCostText("DragoonUnit") ?? dragoonUnitCostText;
        bannermenUnitCostText = FindUnitCostText("BannermenUnit") ?? bannermenUnitCostText;
    }

    /// <summary>
    /// Finds unit buttons by shop row names when the Inspector did not provide them.
    /// </summary>
    private void ResolveButtonReferences() {
        infantryUnitButton = FindUnitButton("InfantryUnit") ?? infantryUnitButton;
        cavalryUnitButton = FindUnitButton("Calvary", "CavalryUnit") ?? cavalryUnitButton;
        musketUnitButton = FindUnitButton("MusketUnit") ?? musketUnitButton;
        officerUnitButton = FindUnitButton("OfficerUnit") ?? officerUnitButton;
        scoutUnitButton = FindUnitButton("ScoutUnit") ?? scoutUnitButton;
        pikemenUnitButton = FindUnitButton("PikemenUnit") ?? pikemenUnitButton;
        skirmisherUnitButton = FindUnitButton("SkirmisherUnit") ?? skirmisherUnitButton;
        dragoonUnitButton = FindUnitButton("DragoonUnit") ?? dragoonUnitButton;
        bannermenUnitButton = FindUnitButton("BannermenUnit") ?? bannermenUnitButton;
    }

    /// <summary>
    /// Finds unit icon background images by shop row names.
    /// </summary>
    private void ResolveIconBackgroundReferences() {
        infantryIconBackground = FindUnitIconBackground("InfantryUnit") ?? infantryIconBackground;
        cavalryIconBackground = FindUnitIconBackground("Calvary", "CavalryUnit") ?? cavalryIconBackground;
        musketIconBackground = FindUnitIconBackground("MusketUnit") ?? musketIconBackground;
        officerIconBackground = FindUnitIconBackground("OfficerUnit") ?? officerIconBackground;
        scoutIconBackground = FindUnitIconBackground("ScoutUnit") ?? scoutIconBackground;
        pikemenIconBackground = FindUnitIconBackground("PikemenUnit") ?? pikemenIconBackground;
        skirmisherIconBackground = FindUnitIconBackground("SkirmisherUnit") ?? skirmisherIconBackground;
        dragoonIconBackground = FindUnitIconBackground("DragoonUnit") ?? dragoonIconBackground;
        bannermenIconBackground = FindUnitIconBackground("BannermenUnit") ?? bannermenIconBackground;

        CacheIconBackgroundColour(infantryIconBackground);
        CacheIconBackgroundColour(cavalryIconBackground);
        CacheIconBackgroundColour(musketIconBackground);
        CacheIconBackgroundColour(officerIconBackground);
        CacheIconBackgroundColour(scoutIconBackground);
        CacheIconBackgroundColour(pikemenIconBackground);
        CacheIconBackgroundColour(skirmisherIconBackground);
        CacheIconBackgroundColour(dragoonIconBackground);
        CacheIconBackgroundColour(bannermenIconBackground);
    }

    /// <summary>
    /// Finds the clear button and label when the Inspector did not provide them.
    /// </summary>
    private void ResolveClearButtonReferences() {
        clearButton = FindButtonByName("ClearBtn") ?? FindButtonByChildText("Clear All", "Clear Units", "CLEAR") ?? clearButton;
        if (clearButtonText == null && clearButton != null) {
            clearButtonText = clearButton.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        clearButtonText ??= FindTextByValue("Clear All", "Clear Units", "CLEAR", clearAllButtonLabel, clearSelectedButtonLabel);
    }

    /// <summary>
    /// Finds the confirm/start button and label when the Inspector did not provide them.
    /// </summary>
    private void ResolveConfirmButtonReferences() {
        confirmButton = FindButtonByChildText("Confirm", "CONFIRM") ?? confirmButton;
        if (confirmButtonText == null && confirmButton != null) {
            confirmButtonText = confirmButton.GetComponentInChildren<TextMeshProUGUI>(true);
        }
    }

    /// <summary>
    /// Applies the current level's optional-unit unlocks to the shop rows.
    /// </summary>
    private void ApplyLevelUnitAvailability() {
        var settings = LevelSettingsDatabase.Load()?.GetCurrentLevelSettings();

        SetUnitAvailable(infantryUnitButton, true, SelectedUnitType.Infantry);
        SetUnitAvailable(cavalryUnitButton, true, SelectedUnitType.Cavalry);
        SetUnitAvailable(musketUnitButton, true, SelectedUnitType.Musket);

        SetUnitAvailable(officerUnitButton, settings != null && settings.officer, SelectedUnitType.Officer);
        SetUnitAvailable(scoutUnitButton, settings != null && settings.Scout, SelectedUnitType.Scout);
        SetUnitAvailable(pikemenUnitButton, settings != null && settings.Pikemen, SelectedUnitType.Pikemen);
        SetUnitAvailable(skirmisherUnitButton, settings != null && settings.Skirmishers, SelectedUnitType.Skirmisher);
        SetUnitAvailable(dragoonUnitButton, settings != null && settings.Dragoons, SelectedUnitType.Dragoon);
        SetUnitAvailable(bannermenUnitButton, settings != null && settings.Bannermen, SelectedUnitType.Bannermen);
    }

    /// <summary>
    /// Shows or hides a unit row.
    /// </summary>
    /// <param name="button">The row button.</param>
    /// <param name="isAvailable">Whether the unit is unlocked for this level.</param>
    /// <param name="unitType">The unit type represented by this row.</param>
    private void SetUnitAvailable(Button button, bool isAvailable, SelectedUnitType unitType) {
        if (button == null) return;

        button.gameObject.SetActive(isAvailable);
        if (!isAvailable && _selectedUnitType == unitType) {
            _selectedUnitType = SelectedUnitType.None;
        }
    }

    /// <summary>
    /// Adds runtime click handlers for shop buttons that do not already have Inspector handlers.
    /// </summary>
    private void BindUnitButtons() {
        BindUnitButton(infantryUnitButton, "Infantry");
        BindUnitButton(cavalryUnitButton, "Cavalry");
        BindUnitButton(musketUnitButton, "Musket");
        BindUnitButton(officerUnitButton, "Officer");
        BindUnitButton(scoutUnitButton, "Scout");
        BindUnitButton(pikemenUnitButton, "Pikemen");
        BindUnitButton(skirmisherUnitButton, "Skirmisher");
        BindUnitButton(dragoonUnitButton, "Dragoon");
        BindUnitButton(bannermenUnitButton, "Bannermen");
    }

    /// <summary>
    /// Adds hover text scaling to configured setup action buttons.
    /// </summary>
    private void BindHoverStyles() {
        BindHoverStyle(clearButton, clearButtonText);
        BindHoverStyle(confirmButton, confirmButtonText);
    }

    /// <summary>
    /// Adds a button listener when the scene has not already wired one in the Inspector.
    /// </summary>
    /// <param name="button">The button to bind.</param>
    /// <param name="unitType">The unit selection id.</param>
    private void BindUnitButton(Button button, string unitType) {
        if (button == null || _runtimeBoundButtons.Contains(button)) return;
        if (button.onClick.GetPersistentEventCount() > 0) return;

        button.onClick.AddListener(() => SetSelectedUnitType(unitType));
        _runtimeBoundButtons.Add(button);
    }

    /// <summary>
    /// Adds pointer enter/exit scaling to a button label.
    /// </summary>
    /// <param name="button">The button receiving hover events.</param>
    /// <param name="label">The label to scale.</param>
    private void BindHoverStyle(Button button, TextMeshProUGUI label) {
        if (button == null || label == null || _hoverStyledButtons.Contains(button)) return;

        _buttonTextBaseScales.TryAdd(label.transform, label.transform.localScale);

        var trigger = button.GetComponent<EventTrigger>();
        if (trigger == null) {
            trigger = button.gameObject.AddComponent<EventTrigger>();
        }

        AddHoverTrigger(trigger, EventTriggerType.PointerEnter, () => SetButtonTextHover(label, true));
        AddHoverTrigger(trigger, EventTriggerType.PointerExit, () => SetButtonTextHover(label, false));
        _hoverStyledButtons.Add(button);
    }

    /// <summary>
    /// Adds one runtime EventTrigger callback.
    /// </summary>
    /// <param name="trigger">The trigger to update.</param>
    /// <param name="eventType">The pointer event type.</param>
    /// <param name="callback">The callback to invoke.</param>
    private static void AddHoverTrigger(EventTrigger trigger, EventTriggerType eventType, Action callback) {
        var entry = new EventTrigger.Entry { eventID = eventType };
        entry.callback.AddListener(_ => callback());
        trigger.triggers.Add(entry);
    }

    /// <summary>
    /// Finds a shop row button by one of its possible GameObject names.
    /// </summary>
    /// <param name="unitRowNames">Possible row names.</param>
    /// <returns>The row button, or null.</returns>
    private static Button FindUnitButton(params string[] unitRowNames) {
        var row = FindSceneTransform(unitRowNames);
        return row != null ? row.GetComponent<Button>() : null;
    }

    /// <summary>
    /// Finds a button by GameObject name, including inactive UI.
    /// </summary>
    /// <param name="buttonName">The expected button GameObject name.</param>
    /// <returns>The matching button, or null.</returns>
    private static Button FindButtonByName(string buttonName) {
        foreach (var button in FindObjectsOfType<Button>(true)) {
            if (string.Equals(button.name, buttonName, StringComparison.OrdinalIgnoreCase)) {
                return button;
            }
        }

        return null;
    }

    /// <summary>
    /// Finds a button by one of the text labels inside it.
    /// </summary>
    /// <param name="values">Allowed label values.</param>
    /// <returns>The matching button, or null.</returns>
    private static Button FindButtonByChildText(params string[] values) {
        foreach (var button in FindObjectsOfType<Button>(true)) {
            var labels = button.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var label in labels) {
                foreach (var value in values) {
                    if (string.Equals(label.text, value, StringComparison.OrdinalIgnoreCase)) {
                        return button;
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Finds a shop row's CostText label by one of its possible GameObject names.
    /// </summary>
    /// <param name="unitRowNames">Possible row names.</param>
    /// <returns>The cost label, or null.</returns>
    private static TextMeshProUGUI FindUnitCostText(params string[] unitRowNames) {
        var row = FindSceneTransform(unitRowNames);
        if (row == null) return null;

        foreach (var text in row.GetComponentsInChildren<TextMeshProUGUI>(true)) {
            if (text.name == "CostText") return text;
        }

        return null;
    }

    /// <summary>
    /// Finds a shop row's icon background image by one of its possible GameObject names.
    /// </summary>
    /// <param name="unitRowNames">Possible row names.</param>
    /// <returns>The icon background image, or null.</returns>
    private static Image FindUnitIconBackground(params string[] unitRowNames) {
        var row = FindSceneTransform(unitRowNames);
        if (row == null) return null;

        foreach (var image in row.GetComponentsInChildren<Image>(true)) {
            if (image.name == "IconBg") return image;
        }

        return null;
    }

    /// <summary>
    /// Stores an icon background's scene colour so affordable units restore correctly.
    /// </summary>
    /// <param name="iconBackground">The icon background image to cache.</param>
    private void CacheIconBackgroundColour(Image iconBackground) {
        if (iconBackground == null || _iconBackgroundBaseColours.ContainsKey(iconBackground)) return;

        _iconBackgroundBaseColours[iconBackground] = iconBackground.color;
    }

    /// <summary>
    /// Finds a TMP label by one of its current text values.
    /// </summary>
    /// <param name="values">Allowed text values.</param>
    /// <returns>The matching label, or null.</returns>
    private static TextMeshProUGUI FindTextByValue(params string[] values) {
        foreach (var text in FindObjectsOfType<TextMeshProUGUI>(true)) {
            foreach (var value in values) {
                if (string.Equals(text.text, value, StringComparison.OrdinalIgnoreCase)) {
                    return text;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Finds a scene transform, including inactive rows.
    /// </summary>
    /// <param name="names">Allowed GameObject names.</param>
    /// <returns>The matching transform, or null.</returns>
    private static Transform FindSceneTransform(params string[] names) {
        foreach (var transform in FindObjectsOfType<Transform>(true)) {
            foreach (var name in names) {
                if (string.Equals(transform.name, name, StringComparison.OrdinalIgnoreCase)) {
                    return transform;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Keeps shop rows, visual layout, and button raycast rects aligned after scene/prefab overrides load.
    /// </summary>
    private void NormalizeShopRowLayout() {
        var rows = GetOrderedShopRows();
        var content = GetShopContent(rows);
        if (content == null) return;

        for (var index = 0; index < rows.Count; index++) {
            if (rows[index] != null) {
                rows[index].SetSiblingIndex(index);
            }
        }

        var layoutGroup = content.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup != null) {
            layoutGroup.enabled = true;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childScaleHeight = false;
            layoutGroup.spacing = 13.1f;
        }

        ResizeShopContentToActiveRows(content, rows, layoutGroup);
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        Canvas.ForceUpdateCanvases();
    }

    /// <summary>
    /// Gets shop row transforms in the intended top-to-bottom order.
    /// </summary>
    /// <returns>Ordered row transforms.</returns>
    private List<RectTransform> GetOrderedShopRows() {
        return new List<RectTransform> {
            GetButtonRect(infantryUnitButton),
            GetButtonRect(cavalryUnitButton),
            GetButtonRect(musketUnitButton),
            GetButtonRect(bannermenUnitButton),
            GetButtonRect(pikemenUnitButton),
            GetButtonRect(dragoonUnitButton),
            GetButtonRect(skirmisherUnitButton),
            GetButtonRect(scoutUnitButton),
            GetButtonRect(officerUnitButton)
        };
    }

    /// <summary>
    /// Gets a button's RectTransform.
    /// </summary>
    /// <param name="button">The source button.</param>
    /// <returns>The RectTransform, or null.</returns>
    private static RectTransform GetButtonRect(Button button) {
        return button != null ? button.transform as RectTransform : null;
    }

    /// <summary>
    /// Gets the common shop content RectTransform from the known unit rows.
    /// </summary>
    /// <param name="rows">Known shop rows.</param>
    /// <returns>The content transform, or null.</returns>
    private static RectTransform GetShopContent(List<RectTransform> rows) {
        foreach (var row in rows) {
            if (row != null && row.parent is RectTransform parent) {
                return parent;
            }
        }

        return null;
    }

    /// <summary>
    /// Resizes the scroll content so inactive unit rows collapse out of the scroll range.
    /// </summary>
    /// <param name="content">The scroll content transform.</param>
    /// <param name="rows">Known shop rows.</param>
    /// <param name="layoutGroup">The content layout group.</param>
    private static void ResizeShopContentToActiveRows(
        RectTransform content,
        List<RectTransform> rows,
        VerticalLayoutGroup layoutGroup) {
        var activeRowCount = 0;
        var rowHeight = 0f;

        foreach (var row in rows) {
            if (row == null || !row.gameObject.activeSelf) continue;

            activeRowCount++;
            var height = row.rect.height > 0f ? row.rect.height : row.sizeDelta.y;
            rowHeight = Mathf.Max(rowHeight, height);
        }

        if (activeRowCount == 0 || rowHeight <= 0f) return;

        var spacing = layoutGroup != null ? layoutGroup.spacing : 0f;
        var padding = layoutGroup != null ? layoutGroup.padding.vertical : 0;
        var contentHeight = padding + (activeRowCount * rowHeight) + ((activeRowCount - 1) * spacing);
        content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);
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
    /// Checks whether the current frame has a completed tap that can place or select units.
    /// </summary>
    /// <param name="screenPosition">The completed tap position.</param>
    /// <returns>True when placement input should be handled.</returns>
    private bool TryGetPlacementTap(out Vector2 screenPosition) {
        screenPosition = default;

        if (GameManager.Instance != null && !GameManager.Instance.IsPreGame()) return false;

#if UNITY_EDITOR || UNITY_STANDALONE
        return TryGetMouseTap(out screenPosition);
#elif UNITY_ANDROID || UNITY_IOS
        return TryGetTouchTap(out screenPosition);
#else
        return TryGetMouseTap(out screenPosition);
#endif
    }

    /// <summary>
    /// Gets a completed mouse tap while rejecting camera drags.
    /// </summary>
    /// <param name="screenPosition">The completed mouse tap position.</param>
    /// <returns>True when the mouse press ended as a tap.</returns>
    private bool TryGetMouseTap(out Vector2 screenPosition) {
        screenPosition = default;

        if (Input.GetMouseButtonDown(0)) {
            _isPointerPressActive = !IsPointerOverUi();
            _pointerStartScreenPosition = Input.mousePosition;
            return false;
        }

        if (Input.GetMouseButtonUp(0)) {
            if (!_isPointerPressActive) return false;

            _isPointerPressActive = false;
            screenPosition = Input.mousePosition;
            return Vector2.Distance(_pointerStartScreenPosition, screenPosition) <= tapMovementThreshold;
        }

        return false;
    }

    /// <summary>
    /// Gets a completed touch tap while rejecting camera drags.
    /// </summary>
    /// <param name="screenPosition">The completed touch tap position.</param>
    /// <returns>True when the touch ended as a tap.</returns>
    private bool TryGetTouchTap(out Vector2 screenPosition) {
        screenPosition = default;

        if (Input.touchCount != 1) {
            _isPointerPressActive = false;
            return false;
        }

        var touch = Input.GetTouch(0);
        switch (touch.phase) {
            case TouchPhase.Began:
                _isPointerPressActive = !IsPointerOverUi(touch.fingerId);
                _pointerStartScreenPosition = touch.position;
                return false;
            case TouchPhase.Ended:
                if (!_isPointerPressActive) return false;

                _isPointerPressActive = false;
                screenPosition = touch.position;
                return Vector2.Distance(_pointerStartScreenPosition, screenPosition) <= tapMovementThreshold;
            case TouchPhase.Canceled:
                _isPointerPressActive = false;
                return false;
            default:
                return false;
        }
    }

    /// <summary>
    /// Attempts to place the selected unit type at the clicked world position.
    /// </summary>
    private void TryPlaceUnitAtPointerPosition() {
        var mainCamera = Camera.main;
        if (mainCamera == null) return;

        var worldPosition = GetPointerWorldPosition(mainCamera, _currentPointerScreenPosition);
        PlaceUnit(SnapToGrid(worldPosition));
    }

    /// <summary>
    /// Selects an existing player unit under the mouse during setup.
    /// </summary>
    /// <returns>True when a selectable placed unit was found.</returns>
    private bool TrySelectPlacedUnitAtPointerPosition() {
        var mainCamera = Camera.main;
        if (mainCamera == null) return false;

        var worldPosition = GetPointerWorldPosition(mainCamera, _currentPointerScreenPosition);
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
    /// Converts the current pointer position into a 2D world position.
    /// </summary>
    /// <param name="mainCamera">The camera used to project the pointer position.</param>
    /// <param name="screenPosition">The pointer position on screen.</param>
    /// <returns>The pointer position in world space.</returns>
    private static Vector3 GetPointerWorldPosition(Camera mainCamera, Vector2 screenPosition) {
        var pointerPosition = new Vector3(screenPosition.x, screenPosition.y, -mainCamera.transform.position.z);

        var worldPosition = mainCamera.ScreenToWorldPoint(pointerPosition);
        worldPosition.z = 0f;
        return worldPosition;
    }

    /// <summary>
    /// Checks whether the pointer began over a UI element.
    /// </summary>
    /// <param name="pointerId">The touch pointer id, or -1 for mouse.</param>
    /// <returns>True when the pointer is over Unity UI.</returns>
    private static bool IsPointerOverUi(int pointerId = -1) {
        if (EventSystem.current == null) return false;

        return pointerId >= 0
            ? EventSystem.current.IsPointerOverGameObject(pointerId)
            : EventSystem.current.IsPointerOverGameObject();
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
    #region Removal

    /// <summary>
    /// Removes the selected placed unit during setup and refunds its cost.
    /// </summary>
    /// <returns>True when a selected placed unit was removed.</returns>
    private bool TryClearSelectedPlacedUnit() {
        if (GameManager.Instance != null && !GameManager.Instance.IsPreGame()) return false;
        if (BattleController.Instance == null || BattleController.Instance.SelectedUnit == null) return false;

        var selectedUnit = BattleController.Instance.SelectedUnit;
        var selectedObject = selectedUnit.gameObject;
        if (!placedUnits.Contains(selectedObject)) return false;

        var refundAmount = GetUnitCost(selectedUnit.ClassType);

        placedUnits.Remove(selectedObject);
        _money += refundAmount;
        _placedUnitsCost = Mathf.Max(0, _placedUnitsCost - refundAmount);
        _selectedUnitType = SelectedUnitType.None;

        BattleController.Instance.RemoveUnit(selectedUnit);
        BattleController.Instance.ClearSelectedUnit();
        Destroy(selectedObject);

        AudioManager.Instance?.PlayClearUnits();
        UpdateMoneyText();
        UpdateUnitCostText();
        UpdateButtonVisuals();
        return true;
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
        UpdateButtonVisuals();
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
            SelectedUnitType.Officer => officerUnitPrefab,
            SelectedUnitType.Scout => scoutUnitPrefab,
            SelectedUnitType.Pikemen => pikemenUnitPrefab,
            SelectedUnitType.Skirmisher => skirmisherUnitPrefab,
            SelectedUnitType.Dragoon => dragoonUnitPrefab,
            SelectedUnitType.Bannermen => bannermenUnitPrefab,
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
            SelectedUnitType.Officer => officerUnitCost,
            SelectedUnitType.Scout => scoutUnitCost,
            SelectedUnitType.Pikemen => pikemenUnitCost,
            SelectedUnitType.Skirmisher => skirmisherUnitCost,
            SelectedUnitType.Dragoon => dragoonUnitCost,
            SelectedUnitType.Bannermen => bannermenUnitCost,
            _ => 0
        };
    }

    /// <summary>
    /// Gets the placement cost for a placed unit class.
    /// </summary>
    /// <param name="unitType">The unit class being refunded.</param>
    /// <returns>The matching unit cost.</returns>
    private int GetUnitCost(UnitType unitType) {
        return unitType switch {
            UnitType.Infantry => infantryUnitCost,
            UnitType.Cavalry => cavalryUnitCost,
            UnitType.Ranged => musketUnitCost,
            UnitType.Officer => officerUnitCost,
            UnitType.Scout => scoutUnitCost,
            UnitType.Pikemen => pikemenUnitCost,
            UnitType.Skirmisher => skirmisherUnitCost,
            UnitType.Dragoon => dragoonUnitCost,
            UnitType.Bannerman => bannermenUnitCost,
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
    /// Gets a registered prefab-root TMP label.
    /// </summary>
    /// <param name="slot">The text slot being requested.</param>
    /// <returns>The registered TMP label, or null.</returns>
    private static TextMeshProUGUI GetRegisteredText(PrefabTextReferences.TextSlot slot) {
        return PrefabTextReferences.TryGetText(slot, out var text) ? text : null;
    }

    /// <summary>
    /// Updates cost labels and affordability colours.
    /// </summary>
    private void UpdateUnitCostText() {
        UpdateCostText(infantryUnitCostText, infantryUnitCost);
        UpdateCostText(cavalryUnitCostText, cavalryUnitCost);
        UpdateCostText(musketUnitCostText, musketUnitCost);
        UpdateCostText(officerUnitCostText, officerUnitCost);
        UpdateCostText(scoutUnitCostText, scoutUnitCost);
        UpdateCostText(pikemenUnitCostText, pikemenUnitCost);
        UpdateCostText(skirmisherUnitCostText, skirmisherUnitCost);
        UpdateCostText(dragoonUnitCostText, dragoonUnitCost);
        UpdateCostText(bannermenUnitCostText, bannermenUnitCost);
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
    /// Updates button availability without overwriting button artwork colours.
    /// </summary>
    private void UpdateButtonVisuals() {
        SetButtonInteractable(infantryUnitButton, infantryUnitCost, SelectedUnitType.Infantry, infantryIconBackground);
        SetButtonInteractable(cavalryUnitButton, cavalryUnitCost, SelectedUnitType.Cavalry, cavalryIconBackground);
        SetButtonInteractable(musketUnitButton, musketUnitCost, SelectedUnitType.Musket, musketIconBackground);
        SetButtonInteractable(officerUnitButton, officerUnitCost, SelectedUnitType.Officer, officerIconBackground);
        SetButtonInteractable(scoutUnitButton, scoutUnitCost, SelectedUnitType.Scout, scoutIconBackground);
        SetButtonInteractable(pikemenUnitButton, pikemenUnitCost, SelectedUnitType.Pikemen, pikemenIconBackground);
        SetButtonInteractable(skirmisherUnitButton, skirmisherUnitCost, SelectedUnitType.Skirmisher, skirmisherIconBackground);
        SetButtonInteractable(dragoonUnitButton, dragoonUnitCost, SelectedUnitType.Dragoon, dragoonIconBackground);
        SetButtonInteractable(bannermenUnitButton, bannermenUnitCost, SelectedUnitType.Bannermen, bannermenIconBackground);
        UpdateClearButtonState(false);
    }

    /// <summary>
    /// Enables a unit button only when the current budget can afford that unit.
    /// </summary>
    /// <param name="button">The button to update.</param>
    /// <param name="unitCost">The cost to compare against current money.</param>
    /// <param name="unitType">The unit type represented by this button.</param>
    /// <param name="iconBackground">The icon background to tint for affordable/unaffordable state.</param>
    private void SetButtonInteractable(Button button, int unitCost, SelectedUnitType unitType, Image iconBackground) {
        if (button == null) return;

        var canAfford = _money >= unitCost;
        button.interactable = canAfford;
        SetIconBackgroundAffordable(iconBackground, canAfford);

        if (!canAfford && _selectedUnitType == unitType) {
            _selectedUnitType = SelectedUnitType.None;
        }
    }

    /// <summary>
    /// Sets a unit icon background to grey when the unit cannot be afforded.
    /// </summary>
    /// <param name="iconBackground">The icon background image to update.</param>
    /// <param name="canAfford">Whether the unit is currently affordable.</param>
    private void SetIconBackgroundAffordable(Image iconBackground, bool canAfford) {
        if (iconBackground == null) return;

        CacheIconBackgroundColour(iconBackground);
        iconBackground.color = canAfford ? _iconBackgroundBaseColours[iconBackground] : disabledIconBackgroundColour;
    }

    /// <summary>
    /// Updates the clear button if the selection changed outside this script's normal refresh points.
    /// </summary>
    private void RefreshClearButtonStateIfNeeded() {
        var clearSelectedMode = HasClearableSelectedUnit();
        if (clearSelectedMode == _lastClearSelectedMode) return;

        UpdateClearButtonState(true);
    }

    /// <summary>
    /// Updates clear button text and colour for clear-all or clear-selected mode.
    /// </summary>
    /// <param name="force">Whether to refresh even if the mode did not change.</param>
    private void UpdateClearButtonState(bool force) {
        var clearSelectedMode = HasClearableSelectedUnit();
        if (!force && clearSelectedMode == _lastClearSelectedMode) return;

        _lastClearSelectedMode = clearSelectedMode;

        if (clearButtonText != null) {
            clearButtonText.text = clearSelectedMode ? clearSelectedButtonLabel : clearAllButtonLabel;
        }

        SetClearButtonColour(clearSelectedMode ? clearSelectedButtonColour : clearAllButtonColour);
    }

    /// <summary>
    /// Applies the configured confirm button colour.
    /// </summary>
    private void UpdateConfirmButtonVisuals() {
        SetButtonNormalAndHoverColour(confirmButton, confirmButtonColour);
    }

    /// <summary>
    /// Applies the clear-mode colour to the button component's normal and hover tint.
    /// </summary>
    /// <param name="colour">The clear button colour.</param>
    private void SetClearButtonColour(Color colour) {
        SetButtonNormalAndHoverColour(clearButton, colour);
    }

    /// <summary>
    /// Applies a button colour to normal, highlighted, and selected tint states.
    /// </summary>
    /// <param name="button">The button to update.</param>
    /// <param name="colour">The colour to apply.</param>
    private static void SetButtonNormalAndHoverColour(Button button, Color colour) {
        if (button == null) return;

        var colours = button.colors;
        colours.normalColor = colour;
        colours.highlightedColor = colour;
        colours.selectedColor = colour;
        button.colors = colours;
    }

    /// <summary>
    /// Scales a button label in or out of hover state.
    /// </summary>
    /// <param name="label">The label to scale.</param>
    /// <param name="isHovered">Whether the pointer is hovering.</param>
    private void SetButtonTextHover(TextMeshProUGUI label, bool isHovered) {
        if (label == null) return;

        if (!_buttonTextBaseScales.TryGetValue(label.transform, out var baseScale)) {
            baseScale = label.transform.localScale;
            _buttonTextBaseScales[label.transform] = baseScale;
        }

        label.transform.localScale = isHovered ? baseScale * buttonTextHoverScale : baseScale;
    }

    /// <summary>
    /// Checks whether the current battle selection is a setup-placed unit that can be cleared individually.
    /// </summary>
    /// <returns>True when the clear button should clear the selected unit.</returns>
    private bool HasClearableSelectedUnit() {
        if (GameManager.Instance != null && !GameManager.Instance.IsPreGame()) return false;
        if (BattleController.Instance == null || BattleController.Instance.SelectedUnit == null) return false;

        RemoveMissingPlacedUnits();
        return placedUnits.Contains(BattleController.Instance.SelectedUnit.gameObject);
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
