using System.Collections.Generic;
using _Scripts.GameManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace _Scripts.UI {
    /// <summary>Six-card pages for the Main Menu, using existing level settings and saved medals.</summary>
    [ExecuteAlways]
    public class LevelSelectionCarousel : MonoBehaviour {
        [SerializeField] private LevelSettingsDatabase database;
        [SerializeField] private RectTransform cardArea;
        [SerializeField] private LevelSelectionCard[] cards;
        [SerializeField] private Button previousButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button startButton;
        [SerializeField] private int selectedLevel = 1;
        [SerializeField, Min(0)] private float selectionAnimationSeconds = 0.16f;

        private int page;
        private int highestUnlocked = 1;
        private bool bound;
        private Vector2 lastSize;
        private readonly HashSet<string> loadableScenes = new HashSet<string>();
        public int SelectedLevel => selectedLevel;
        public int CurrentPage => page;
        private bool SelectionIsVisible => cards != null && selectedLevel > page * cards.Length && selectedLevel <= (page + 1) * cards.Length;
        public int PageCount => database != null && cards != null && cards.Length > 0 ? Mathf.CeilToInt(database.LevelCount / (float)cards.Length) : 0;

        private void OnEnable() {
            if (database == null) database = LevelSettingsDatabase.Load();
            CacheBuildScenes();
            if (Application.isPlaying) {
                highestUnlocked = PersistentGameSettings.GetHighestLevelPlayed(1);
                Bind();
                selectedLevel = PersistentGameSettings.CurrentLevel;
                if (!CanSelect(selectedLevel)) selectedLevel = FirstPlayableLevel();
                page = cards != null && cards.Length > 0 ? (Mathf.Max(1, selectedLevel) - 1) / cards.Length : 0;
            }
            Refresh();
            Layout(true);
        }

        private void Bind() {
            if (bound || cards == null) return;
            for (var i = 0; i < cards.Length; i++) {
                var slot = i;
                cards[i].button.onClick.AddListener(() => SelectLevel(page * cards.Length + slot + 1));
            }
            previousButton.onClick.AddListener(PreviousPage);
            nextButton.onClick.AddListener(NextPage);
            bound = true;
        }

        public void Configure(LevelSettingsDatabase data, RectTransform area, LevelSelectionCard[] views, Button previous, Button next, Button start) {
            database = data;
            cardArea = area;
            cards = views;
            previousButton = previous;
            nextButton = next;
            startButton = start;
            CacheBuildScenes();
            Refresh();
            Layout(true);
        }

        private void CacheBuildScenes() {
            loadableScenes.Clear();
            for (var i = 0; i < SceneManager.sceneCountInBuildSettings; i++) {
                var path = SceneUtility.GetScenePathByBuildIndex(i);
                loadableScenes.Add(System.IO.Path.GetFileNameWithoutExtension(path));
            }
        }

        public bool CanSelect(int level) {
            var settings = database != null ? database.GetSettingsAt(level - 1) : null;
            return settings != null && level <= highestUnlocked && !string.IsNullOrWhiteSpace(settings.sceneName) && loadableScenes.Contains(settings.sceneName);
        }

        private int FirstPlayableLevel() {
            if (database != null)
                for (var i = 1; i <= database.LevelCount; i++) if (CanSelect(i)) return i;
            return 0;
        }

        public void SelectLevel(int level) {
            if (!CanSelect(level)) return;
            selectedLevel = level;
            page = (level - 1) / cards.Length;
            Refresh();
        }

        public void PreviousPage() => SetPage(page - 1);
        public void NextPage() => SetPage(page + 1);

        public void SetPage(int index) {
            page = Mathf.Clamp(index, 0, Mathf.Max(0, PageCount - 1));
            Refresh();
            Layout(true);
        }

        public void StartSelectedLevel() {
            if (!CanSelect(selectedLevel) || !SelectionIsVisible) return;
            var settings = database.GetSettingsAt(selectedLevel - 1);
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
            SceneManager.LoadScene(settings.sceneName);
        }

        public static int StarsForTime(float seconds, LevelSettingsDatabase.LevelSettings settings) {
            if (settings == null || seconds <= 0) return 0;
            if (settings.goldTimeSeconds > 0 && seconds <= settings.goldTimeSeconds) return 3;
            if (settings.silverTimeSeconds > 0 && seconds <= settings.silverTimeSeconds) return 2;
            if (settings.bronzeTimeSeconds > 0 && seconds <= settings.bronzeTimeSeconds) return 1;
            return 0;
        }

        public void Refresh() {
            if (cards == null || database == null) return;
            for (var slot = 0; slot < cards.Length; slot++) {
                var level = page * cards.Length + slot + 1;
                var settings = database.GetSettingsAt(level - 1);
                cards[slot].gameObject.SetActive(settings != null);
                if (settings == null) continue;
                var thumbnail = settings.previewImage;
                if (thumbnail == null && !string.IsNullOrWhiteSpace(settings.previewImageResourcePath)) thumbnail = Resources.Load<Sprite>(settings.previewImageResourcePath.Trim());
                var earned = Application.isPlaying && PersistentGameSettings.TryGetBestTime(level, out var seconds) ? StarsForTime(seconds, settings) : 0;
                var title = string.IsNullOrWhiteSpace(settings.levelName) ? $"Level {level}" : settings.levelName.Trim();
                cards[slot].Display(level, title, thumbnail, CanSelect(level), earned, level == selectedLevel);
            }
            if (previousButton != null) previousButton.interactable = page > 0;
            if (nextButton != null) nextButton.interactable = page + 1 < PageCount;
            if (startButton != null) {
                startButton.interactable = CanSelect(selectedLevel) && SelectionIsVisible;
                var visuals = startButton.GetComponent<CanvasGroup>();
                if (visuals != null) visuals.alpha = startButton.interactable ? 1 : 0.35f;
            }
        }

        private void LateUpdate() => Layout(!Application.isPlaying);

        private void Layout(bool instant) {
            if (cardArea == null || cards == null || cards.Length == 0) return;
            var size = cardArea.rect.size;
            if (size.x <= 0 || size.y <= 0) return;
            if (size != lastSize) instant = true;
            lastSize = size;
            var count = Mathf.Min(cards.Length, Mathf.Max(0, (database != null ? database.LevelCount : 0) - page * cards.Length));
            if (count == 0) return;
            const float gap = 14f;
            var selectedOnPage = selectedLevel > page * cards.Length && selectedLevel <= page * cards.Length + count;
            var totalWeight = count + (selectedOnPage ? 0.22f : 0);
            var width = (size.x - gap * (count - 1)) / totalWeight;
            var x = -size.x / 2;
            var t = instant || selectionAnimationSeconds <= 0 ? 1 : 1 - Mathf.Exp(-Time.unscaledDeltaTime * 5 / selectionAnimationSeconds);
            for (var i = 0; i < count; i++) {
                var selected = page * cards.Length + i + 1 == selectedLevel;
                var w = width * (selected ? 1.22f : 1);
                var rect = (RectTransform)cards[i].transform;
                var h = size.y * (selected ? 1 : 0.86f);
                rect.sizeDelta = Vector2.Lerp(rect.sizeDelta, new Vector2(w, h), t);
                rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition, new Vector2(x + w / 2, selected ? 0 : -5), t);
                x += w + gap;
            }
        }
    }
}
