using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
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
        [SerializeField] private LevelLoader levelLoader;                 // Scene loading helper.

        [Header("Pages")]
        [SerializeField] private List<Image> pageIcons = new();           // Page indicators; count controls page count.
        [SerializeField] private Sprite selectedPageIcon;                 // Sprite used by the selected page indicator.
        [SerializeField] private Sprite unselectedPageIcon;               // Sprite used by unselected page indicators.

        [Header("Navigation")]
        [SerializeField] private Button previousPageButton;               // Button that moves to the previous page.
        [SerializeField] private Button nextPageButton;                   // Button that moves to the next page.

        private readonly List<UnityAction> _levelButtonActions = new();   // Runtime level button listeners.
        private int _currentPageIndex;                                    // Zero-based current page.
        private UnityAction _previousPageAction;                          // Cached previous-page listener.
        private UnityAction _nextPageAction;                              // Cached next-page listener.

        #endregion
        #region Unity Methods

        private void Awake() {
            ResolveReferences();
            AddButtonListeners();
            SetPage(GetPageIndexForLevel(lastUnlockedLevel));
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
            if (levelLoader == null) {
                Debug.LogWarning("Cannot load level because no LevelLoader has been assigned.");
                return;
            }

            levelLoader.LoadLevel(levelNumber.ToString());
        }

        #endregion
        #region Setup

        /// <summary>
        /// Finds scene references that were not assigned in the Inspector.
        /// </summary>
        private void ResolveReferences() {
            levelLoader ??= FindObjectOfType<LevelLoader>();
        }

        /// <summary>
        /// Connects page and level button listeners.
        /// </summary>
        private void AddButtonListeners() {
            _previousPageAction = ShowPreviousPage;
            _nextPageAction = ShowNextPage;

            if (previousPageButton != null) previousPageButton.onClick.AddListener(_previousPageAction);
            if (nextPageButton != null) nextPageButton.onClick.AddListener(_nextPageAction);

            for (var i = 0; i < levelButtons.Count; i++) {
                var button = levelButtons[i];
                if (button == null) continue;

                var buttonIndex = i;
                UnityAction clickAction = () => LoadLevel(GetLevelNumberForButton(buttonIndex));
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
        }

        #endregion
        #region Refresh

        /// <summary>
        /// Updates level numbers and button interactability for the current page.
        /// </summary>
        private void RefreshLevelButtons() {
            for (var i = 0; i < levelButtons.Count; i++) {
                var button = levelButtons[i];
                if (button == null) continue;

                var levelNumber = GetLevelNumberForButton(i);
                var label = GetButtonLabel(button);
                if (label != null) {
                    label.text = levelNumber.ToString();
                }

                var isUnlocked = levelNumber <= lastUnlockedLevel;
                var lockedImage = GetLockImage(button);
                if (lockedImage != null) {
                    lockedImage.gameObject.SetActive(!isUnlocked);
                }

                button.interactable = isUnlocked;
            }
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
            return Mathf.Max(0, pageIcons.Count);
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
