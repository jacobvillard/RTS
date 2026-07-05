using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace _Scripts.GameManagement {
    /// <summary>
    /// Handles paged level selection with dynamic button labels and page indicators.
    /// </summary>
    public class LevelSelector : MonoBehaviour {

        #region Variables

        [Header("Levels")]
        [SerializeField] private int levelsPerPage = 20;                  // Number of level buttons shown per page.
        [SerializeField] private int lastUnlockedLevel = 50;              // Used to choose the starting page.
        [SerializeField] private List<Button> levelButtons = new();       // Level button slots on this selector.
        [SerializeField] private NumberStyle numberStyle = NumberStyle.Roman;
        [SerializeField, Range(25, 100)] private int numberPrefixSizePercent = 65;
        [SerializeField] private string numberPrefixFontAssetName;
        [SerializeField] private Transform generatedButtonParent;         // Optional parent used when auto-finding buttons.
        [SerializeField] private bool cloneFirstButtonToMatchLevels = true;// Creates enough rows for all Resources levels.
        [SerializeField] private bool loadLevelOnClick;                   // Optional old behavior: load instead of select.
        [SerializeField] private LevelLoader levelLoader;                 // Scene loading helper.

        [Header("Selection")]
        [SerializeField] private int selectedLevel = 1;                   // One-based selected level index.
        [SerializeField] private TextMeshProUGUI selectedLevelText;       // Optional selected level title.
        [SerializeField] private TextMeshProUGUI selectedSceneText;       // Optional selected scene name.
        [SerializeField] private TextMeshProUGUI selectedBudgetText;      // Optional selected start money.
        [SerializeField] private TextMeshProUGUI selectedStatsText;       // Optional multi-line stats preview.
        [SerializeField] private TextMeshProUGUI bestTimeText;            // Optional saved best time label.

        [Header("Preview Image")]
        [SerializeField] private Image levelPreviewImage;                 // Optional level PNG/sprite preview.
        [SerializeField] private Sprite missingPreviewSprite;             // Optional placeholder when no level preview exists.

        [Header("Medal Images")]
        [SerializeField] private Image bronzeMedalImage;                  // Optional bronze medal display.
        [SerializeField] private Image silverMedalImage;                  // Optional silver medal display.
        [SerializeField] private Image goldMedalImage;                    // Optional gold medal display.
        [SerializeField, Range(0f, 1f)] private float unearnedMedalAlpha = 0.25f;

        [Header("Unit Preview")]
        [SerializeField] private List<GameObject> baseUnitViews = new();  // Always visible, non-toggleable units.
        [SerializeField] private GameObject officerUnitView;              // Optional unit controlled by level settings.
        [SerializeField] private GameObject scoutUnitView;
        [SerializeField] private GameObject pikemenUnitView;
        [SerializeField] private GameObject skirmishersUnitView;
        [SerializeField] private GameObject dragoonsUnitView;
        [SerializeField] private GameObject bannermenUnitView;

        [Header("Pages")]
        [SerializeField] private List<Image> pageIcons = new();           // Page indicators; count controls page count.
        [SerializeField] private Sprite selectedPageIcon;                 // Sprite used by the selected page indicator.
        [SerializeField] private Sprite unselectedPageIcon;               // Sprite used by unselected page indicators.

        [Header("Navigation")]
        [SerializeField] private Button previousPageButton;               // Button that moves to the previous page.
        [SerializeField] private Button nextPageButton;                   // Button that moves to the next page.

        private readonly List<UnityAction> _levelButtonActions = new();   // Runtime level button listeners.
        private LevelSettingsDatabase _database;                          // Resources-loaded level data.
        private int _currentPageIndex;                                    // Zero-based current page.
        private UnityAction _previousPageAction;                          // Cached previous-page listener.
        private UnityAction _nextPageAction;                              // Cached next-page listener.
        private int _lastPreviewedLevel = -1;                             // Tracks inspector/index changes.
        private LevelSelector _selectionTarget;                           // Detail-owning selector to update from button clicks.
        private bool _referencesResolved;                                 // Prevents selection from rebuilding button state.

        public int SelectedLevel => selectedLevel;

        private enum NumberStyle {
            PaddedDecimal,
            Decimal,
            Roman
        }

        private enum MedalTier {
            None,
            Bronze,
            Silver,
            Gold
        }

        #endregion
        #region Unity Methods

        private void Awake() {
            ResolveReferences();
            AddButtonListeners();
        }

        private void OnEnable() {
            RefreshFromSave();
        }

        private void Update() {
            if (selectedLevel == _lastPreviewedLevel) return;

            SelectLevel(selectedLevel);
        }

        private void OnDestroy() {
            RemoveButtonListeners();
        }

        #endregion
        #region Public Methods

        /// <summary>
        /// Shows the next page, wrapping to the first page after the last page.
        /// </summary>
        public void ShowNextPage() {
            SetPage(_currentPageIndex + 1);
        }

        /// <summary>
        /// Shows the previous page, wrapping to the last page before the first page.
        /// </summary>
        public void ShowPreviousPage() {
            SetPage(_currentPageIndex - 1);
        }

        /// <summary>
        /// Loads the currently selected level scene.
        /// </summary>
        public void StartGame() {
            LoadLevel(selectedLevel);
        }

        /// <summary>
        /// Shows a page by zero-based index.
        /// </summary>
        /// <param name="pageIndex">The page index to display.</param>
        public void SetPage(int pageIndex) {
            var pageCount = GetPageCount();
            if (pageCount <= 0) return;

            _currentPageIndex = WrapPageIndex(pageIndex, pageCount);
            RefreshLevelButtons();
            RefreshPageIcons();
        }

        /// <summary>
        /// Loads a level scene by number.
        /// </summary>
        /// <param name="levelNumber">The numeric level scene name.</param>
        public void LoadLevel(int levelNumber) {
            SceneManager.LoadScene(levelNumber.ToString());
        }

        /// <summary>
        /// Selects a level row without loading it, then refreshes preview TMP fields.
        /// </summary>
        /// <param name="levelNumber">The one-based level number.</param>
        public void SelectLevel(int levelNumber) {
            selectedLevel = Mathf.Clamp(levelNumber, 1, Mathf.Max(1, GetLevelCountWithoutResolving()));
            _lastPreviewedLevel = selectedLevel;
            RefreshSelectedLevelPreview();
        }

        /// <summary>
        /// Refreshes unlock data from the save file and redraws the current page.
        /// </summary>
        public void RefreshFromSave() {
            RefreshLastUnlockedLevel();
            SetPage(GetPageIndexForLevel(lastUnlockedLevel));
        }

        #endregion
        #region Setup

        /// <summary>
        /// Finds scene references that were not assigned in the Inspector.
        /// </summary>
        private void ResolveReferences() {
            if (_referencesResolved) return;

            _database ??= LevelSettingsDatabase.Load();
            levelLoader ??= LevelLoader.Instance != null ? LevelLoader.Instance : FindObjectOfType<LevelLoader>();
            generatedButtonParent ??= transform;
            _selectionTarget = ResolveSelectionTarget();
            ResolveOptionalPreviewFields();
            ResolveOptionalUnitViews();
            RefreshGeneratedButtons();
            _referencesResolved = true;
        }

        /// <summary>
        /// Reads the highest played level from the persistent save file.
        /// </summary>
        private void RefreshLastUnlockedLevel() {
            lastUnlockedLevel = PersistentGameSettings.GetHighestLevelPlayed(1);
        }

        /// <summary>
        /// Connects page and level button listeners.
        /// </summary>
        private void AddButtonListeners() {
            RemoveButtonListeners();

            _previousPageAction = ShowPreviousPage;
            _nextPageAction = ShowNextPage;

            if (previousPageButton != null) previousPageButton.onClick.AddListener(_previousPageAction);
            if (nextPageButton != null) nextPageButton.onClick.AddListener(_nextPageAction);

            var activeButtonCount = GetActiveLevelButtonCount();
            for (var i = 0; i < levelButtons.Count; i++) {
                var button = levelButtons[i];
                if (button == null) {
                    _levelButtonActions.Add(null);
                    continue;
                }

                if (i >= activeButtonCount) {
                    _levelButtonActions.Add(null);
                    button.gameObject.SetActive(false);
                    continue;
                }

                var clickedButton = button;
                UnityAction clickAction = () => HandleLevelButtonPressed(clickedButton);
                _levelButtonActions.Add(clickAction);
                button.onClick.AddListener(clickAction);
            }
        }

        /// <summary>
        /// Removes listeners owned by this selector.
        /// </summary>
        private void RemoveButtonListeners() {
            if (previousPageButton != null && _previousPageAction != null) {
                previousPageButton.onClick.RemoveListener(_previousPageAction);
            }

            if (nextPageButton != null && _nextPageAction != null) {
                nextPageButton.onClick.RemoveListener(_nextPageAction);
            }

            for (var i = 0; i < levelButtons.Count && i < _levelButtonActions.Count; i++) {
                var button = levelButtons[i];
                var clickAction = _levelButtonActions[i];
                if (button == null || clickAction == null) continue;
                button.onClick.RemoveListener(clickAction);
            }

            _levelButtonActions.Clear();
            _previousPageAction = null;
            _nextPageAction = null;
        }

        #endregion
        #region Refresh

        /// <summary>
        /// Updates level numbers and button interactability for the current page.
        /// </summary>
        private void RefreshLevelButtons() {
            var activeButtonCount = GetActiveLevelButtonCount();
            for (var i = 0; i < levelButtons.Count; i++) {
                var button = levelButtons[i];
                if (button == null) continue;
                var levelNumber = GetLevelNumberForButton(i);
                var isActiveSlot = i < activeButtonCount && levelNumber <= GetLevelCount();
                button.gameObject.SetActive(isActiveSlot);
                if (!isActiveSlot) continue;

                var label = GetButtonLabel(button);
                if (label != null) {
                    label.text = GetLevelButtonText(levelNumber);
                }

                var isUnlocked = levelNumber <= lastUnlockedLevel;
                var lockedImage = GetLockImage(button);
                if (lockedImage != null) {
                    lockedImage.gameObject.SetActive(!isUnlocked);
                }

                button.interactable = isUnlocked;
            }

            RefreshSelectedLevelPreview();
        }

        private void RefreshSelectedLevelPreview() {
            var settings = GetSettingsForLevel(selectedLevel);
            if (settings == null) return;

            var displayName = GetDisplayName(settings, selectedLevel);
            SetText(selectedLevelText, $"{GetLevelNumberPrefix(selectedLevel)} {displayName}");
            SetText(selectedSceneText, settings.sceneName);
            SetText(selectedBudgetText, $"Budget: + {settings.startMoney}");
            RefreshLevelPreviewImage(settings);
            RefreshBestTimeAndMedals(settings);
            RefreshUnitPreview(settings);
            SetText(selectedStatsText, BuildStatsText(settings));
        }

        /// <summary>
        /// Updates page indicator sprites.
        /// </summary>
        private void RefreshPageIcons() {
            for (var i = 0; i < pageIcons.Count; i++) {
                var pageIcon = pageIcons[i];
                if (pageIcon == null) continue;

                pageIcon.sprite = i == _currentPageIndex
                    ? selectedPageIcon
                    : unselectedPageIcon;
            }
        }

        #endregion
        #region Helpers

        /// <summary>
        /// Gets the number of pages from the page icon list.
        /// </summary>
        /// <returns>The page count.</returns>
        private int GetPageCount() {
            var dataPageCount = Mathf.CeilToInt(GetLevelCount() / (float)Mathf.Max(1, levelsPerPage));
            return Mathf.Max(1, pageIcons.Count > 0 ? pageIcons.Count : dataPageCount);
        }

        /// <summary>
        /// Gets the number of level button slots that should be visible on each page.
        /// </summary>
        /// <returns>The visible level button count.</returns>
        private int GetActiveLevelButtonCount() {
            return Mathf.Min(levelsPerPage, levelButtons.Count);
        }

        private int GetLevelCount() {
            ResolveReferences();
            return GetLevelCountWithoutResolving();
        }

        private int GetLevelCountWithoutResolving() {
            return _database != null ? _database.LevelCount : levelButtons.Count;
        }

        private void RefreshGeneratedButtons() {
            if (levelButtons.Count == 0 && generatedButtonParent != null) {
                levelButtons.AddRange(generatedButtonParent.GetComponentsInChildren<Button>(true));
            }

            if (!cloneFirstButtonToMatchLevels || generatedButtonParent == null || levelButtons.Count == 0) return;

            var levelCount = _database != null ? _database.LevelCount : levelButtons.Count;
            var template = levelButtons[0];
            if (template == null) return;

            while (levelButtons.Count < levelCount) {
                var clone = Instantiate(template, generatedButtonParent);
                clone.name = $"Level Button {levelButtons.Count + 1}";
                levelButtons.Add(clone);
            }
        }

        private LevelSettingsDatabase.LevelSettings GetSettingsForLevel(int levelNumber) {
            ResolveReferences();
            return _database != null ? _database.GetSettingsAt(levelNumber - 1) : null;
        }

        private void RefreshLevelPreviewImage(LevelSettingsDatabase.LevelSettings settings) {
            if (levelPreviewImage == null) return;

            var sprite = settings.previewImage != null
                ? settings.previewImage
                : LoadPreviewSprite(settings.previewImageResourcePath);

            levelPreviewImage.sprite = sprite != null ? sprite : missingPreviewSprite;
            levelPreviewImage.enabled = levelPreviewImage.sprite != null;
        }

        private void RefreshBestTimeAndMedals(LevelSettingsDatabase.LevelSettings settings) {
            var hasBestTime = PersistentGameSettings.TryGetBestTime(selectedLevel, out var bestTimeSeconds);
            var medalTier = hasBestTime ? GetMedalTier(bestTimeSeconds, settings) : MedalTier.None;

            SetText(bestTimeText, $"Best Time: {(hasBestTime ? FormatTime(bestTimeSeconds) : "N/A")}");

            SetMedalImage(bronzeMedalImage, medalTier >= MedalTier.Bronze);
            SetMedalImage(silverMedalImage, medalTier >= MedalTier.Silver);
            SetMedalImage(goldMedalImage, medalTier >= MedalTier.Gold);
        }

        private void RefreshUnitPreview(LevelSettingsDatabase.LevelSettings settings) {
            foreach (var baseUnitView in baseUnitViews) {
                if (baseUnitView != null) baseUnitView.SetActive(true);
            }

            SetActive(officerUnitView, settings.officer);
            SetActive(scoutUnitView, settings.Scout);
            SetActive(pikemenUnitView, settings.Pikemen);
            SetActive(skirmishersUnitView, settings.Skirmishers);
            SetActive(dragoonsUnitView, settings.Dragoons);
            SetActive(bannermenUnitView, settings.Bannermen);
        }

        /// <summary>
        /// Gets the page index that should contain a level number.
        /// </summary>
        /// <param name="levelNumber">The level number.</param>
        /// <returns>The zero-based page index.</returns>
        private int GetPageIndexForLevel(int levelNumber) {
            var safeLevelNumber = Mathf.Max(1, levelNumber);
            return (safeLevelNumber - 1) / Mathf.Max(1, levelsPerPage);
        }

        /// <summary>
        /// Gets the level number represented by a button slot on the current page.
        /// </summary>
        /// <param name="buttonIndex">The button slot index.</param>
        /// <returns>The displayed level number.</returns>
        private int GetLevelNumberForButton(int buttonIndex) {
            return (_currentPageIndex * levelsPerPage) + buttonIndex + 1;
        }

        private int GetLevelNumberForButton(Button button) {
            if (button == null) return selectedLevel;

            return (_currentPageIndex * levelsPerPage) + button.transform.GetSiblingIndex() + 1;
        }

        private void HandleLevelButtonPressed(Button button) {
            var levelNumber = GetLevelNumberForButton(button);
            
            selectedLevel = levelNumber;
            Debug.Log($"Level button pressed: {levelNumber} (selectedLevel updated to {selectedLevel})");
            if (loadLevelOnClick) {
                LoadLevel(levelNumber);
                return;
            }

            (_selectionTarget != null ? _selectionTarget : this).SelectLevel(levelNumber);
        }

        private string GetLevelButtonText(int levelNumber) {
            var settings = GetSettingsForLevel(levelNumber);
            return $"{GetLevelNumberPrefix(levelNumber)} {GetDisplayName(settings, levelNumber)}";
        }

        private string GetLevelNumberPrefix(int levelNumber) {
            var rawPrefix = numberStyle switch {
                NumberStyle.Decimal => $"{levelNumber}.",
                NumberStyle.Roman => $"{ToRoman(levelNumber)}.",
                _ => $"{levelNumber:00}."
            };

            if (numberStyle != NumberStyle.Roman) return rawPrefix;

            var sizeTag = $"<size={numberPrefixSizePercent}%>";
            if (string.IsNullOrWhiteSpace(numberPrefixFontAssetName)) {
                return $"{sizeTag}{rawPrefix}</size>";
            }

            return $"{sizeTag}<font=\"{numberPrefixFontAssetName}\">{rawPrefix}</font></size>";
        }

        private static string GetDisplayName(LevelSettingsDatabase.LevelSettings settings, int levelNumber) {
            if (settings == null) return $"Level {levelNumber}";
            if (!string.IsNullOrWhiteSpace(settings.levelName)) return settings.levelName.Trim();
            if (!string.IsNullOrWhiteSpace(settings.sceneName)) return settings.sceneName.Trim();

            return $"Level {levelNumber}";
        }

        private static string BuildStatsText(LevelSettingsDatabase.LevelSettings settings) {
            var builder = new StringBuilder();
            builder.AppendLine($"Bronze: {FormatTime(settings.bronzeTimeSeconds)}");
            builder.AppendLine($"Silver: {FormatTime(settings.silverTimeSeconds)}");
            builder.AppendLine($"Gold: {FormatTime(settings.goldTimeSeconds)}");

            var units = GetAvailableUnits(settings);
            builder.Append("Units: ");
            builder.Append(units.Count > 0 ? string.Join(", ", units) : "Base units only");

            return builder.ToString();
        }

        private static List<string> GetAvailableUnits(LevelSettingsDatabase.LevelSettings settings) {
            var units = new List<string>();
            if (settings.officer) units.Add("Officer");
            if (settings.Scout) units.Add("Scout");
            if (settings.Pikemen) units.Add("Pikemen");
            if (settings.Skirmishers) units.Add("Skirmishers");
            if (settings.Dragoons) units.Add("Dragoons");
            if (settings.Bannermen) units.Add("Bannermen");
            return units;
        }

        private void ResolveOptionalUnitViews() {
            officerUnitView ??= FindUnitViewByName("Officer");
            scoutUnitView ??= FindUnitViewByName("Scout");
            pikemenUnitView ??= FindUnitViewByName("Pikemen");
            skirmishersUnitView ??= FindUnitViewByName("Skirmishers");
            dragoonsUnitView ??= FindUnitViewByName("Dragoons");
            bannermenUnitView ??= FindUnitViewByName("Bannermen");
        }

        private void ResolveOptionalPreviewFields() {
            bestTimeText ??= FindTextByName("BestTime");
            levelPreviewImage ??= FindImageByName("LevelPreview") ?? FindImageByName("Preview") ?? FindImageByName("LevelImage");
            bronzeMedalImage ??= FindImageByName("Bronze");
            silverMedalImage ??= FindImageByName("Silver");
            goldMedalImage ??= FindImageByName("Gold");
        }

        private LevelSelector ResolveSelectionTarget() {
            if (HasDetailFields()) return this;

            var childSelectors = GetComponentsInChildren<LevelSelector>(true);
            foreach (var childSelector in childSelectors) {
                if (childSelector == null || childSelector == this) continue;
                if (childSelector.HasDetailFields()) return childSelector;
            }

            return this;
        }

        private bool HasDetailFields() {
            return selectedLevelText != null
                || selectedBudgetText != null
                || selectedStatsText != null
                || bestTimeText != null
                || levelPreviewImage != null
                || bronzeMedalImage != null
                || silverMedalImage != null
                || goldMedalImage != null;
        }

        private GameObject FindUnitViewByName(string unitName) {
            var searchRoot = GetSearchRoot();
            var transforms = searchRoot.GetComponentsInChildren<Transform>(true);
            foreach (var child in transforms) {
                if (child == null || child == transform) continue;
                if (child.name.Equals(unitName, StringComparison.OrdinalIgnoreCase)) return child.gameObject;
            }

            return null;
        }

        private TextMeshProUGUI FindTextByName(string objectName) {
            var searchRoot = GetSearchRoot();
            var texts = searchRoot.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var text in texts) {
                if (text == null) continue;
                if (text.name.Equals(objectName, StringComparison.OrdinalIgnoreCase)) return text;
            }

            return null;
        }

        private Image FindImageByName(string objectName) {
            var searchRoot = GetSearchRoot();
            var images = searchRoot.GetComponentsInChildren<Image>(true);
            foreach (var image in images) {
                if (image == null) continue;
                if (image.name.Equals(objectName, StringComparison.OrdinalIgnoreCase)) return image;
            }

            return null;
        }

        private Transform GetSearchRoot() {
            var ancestor = transform;
            while (ancestor != null) {
                if (ancestor.name.Equals("LevelSelect", StringComparison.OrdinalIgnoreCase)) return ancestor;
                ancestor = ancestor.parent;
            }

            var parentCanvas = GetComponentInParent<Canvas>();
            return parentCanvas != null ? parentCanvas.transform : transform.root;
        }

        private static Sprite LoadPreviewSprite(string resourcePath) {
            if (string.IsNullOrWhiteSpace(resourcePath)) return null;

            return Resources.Load<Sprite>(resourcePath.Trim());
        }

        private static MedalTier GetMedalTier(float bestTimeSeconds, LevelSettingsDatabase.LevelSettings settings) {
            if (bestTimeSeconds <= 0f) return MedalTier.None;
            if (settings.goldTimeSeconds > 0f && bestTimeSeconds <= settings.goldTimeSeconds) return MedalTier.Gold;
            if (settings.silverTimeSeconds > 0f && bestTimeSeconds <= settings.silverTimeSeconds) return MedalTier.Silver;
            if (settings.bronzeTimeSeconds > 0f && bestTimeSeconds <= settings.bronzeTimeSeconds) return MedalTier.Bronze;

            return MedalTier.None;
        }

        private void SetMedalImage(Image image, bool earned) {
            if (image == null) return;

            image.enabled = true;
            var color = image.color;
            color.a = earned ? 1f : unearnedMedalAlpha;
            image.color = color;
        }

        private static void SetActive(GameObject target, bool active) {
            if (target != null) target.SetActive(active);
        }

        private static string FormatTime(float seconds) {
            if (seconds <= 0f) return "N/A";

            var span = TimeSpan.FromSeconds(seconds);
            return span.Hours > 0
                ? $"{span.Hours}:{span.Minutes:00}:{span.Seconds:00}"
                : $"{span.Minutes}:{span.Seconds:00}";
        }

        private static void SetText(TextMeshProUGUI text, string value) {
            if (text == null) return;

            text.text = value;
        }

        private static string ToRoman(int number) {
            if (number <= 0) return number.ToString();

            var values = new[] { 1000, 900, 500, 400, 100, 90, 50, 40, 10, 9, 5, 4, 1 };
            var numerals = new[] { "M", "CM", "D", "CD", "C", "XC", "L", "XL", "X", "IX", "V", "IV", "I" };
            var result = new StringBuilder();

            for (var i = 0; i < values.Length; i++) {
                while (number >= values[i]) {
                    result.Append(numerals[i]);
                    number -= values[i];
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// Wraps page indexes so navigation cycles between first and last pages.
        /// </summary>
        /// <param name="pageIndex">The requested page index.</param>
        /// <param name="pageCount">The page count.</param>
        /// <returns>The wrapped page index.</returns>
        private static int WrapPageIndex(int pageIndex, int pageCount) {
            return ((pageIndex % pageCount) + pageCount) % pageCount;
        }

        /// <summary>
        /// Gets the first TMP label found under a level button.
        /// </summary>
        /// <param name="button">The level button.</param>
        /// <returns>The child TMP label, or null.</returns>
        private static TextMeshProUGUI GetButtonLabel(Button button) {
            return button != null ? button.GetComponentInChildren<TextMeshProUGUI>() : null;
        }

        /// <summary>
        /// Gets the first child image under a level button that is not the button's own target graphic.
        /// </summary>
        /// <param name="button">The level button.</param>
        /// <returns>The child image used as a locked overlay, or null.</returns>
        private static Image GetLockImage(Button button) {
            if (button == null) return null;

            var images = button.GetComponentsInChildren<Image>(true);
            foreach (var image in images) {
                if (image == null || image.gameObject == button.gameObject) continue;
                if (button.targetGraphic != null && image == button.targetGraphic) continue;
                return image;
            }

            return null;
        }

        #endregion
    }
}
