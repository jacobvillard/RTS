using System;
using System.IO;
using System.Linq;
using _Scripts.GameManagement;
using _Scripts.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>Installs editable card objects into the existing Main Menu screen.</summary>
public static class LevelSelectionCardSetup {
    private static readonly Color Green = new Color(0.55f, 0.85f, 0.08f);
    private const string ScenePath = "Assets/Scenes/Main Menu.unity";

    [MenuItem("Tools/RTS/Install Level Selection Cards %&l")]
    public static void Install() {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode before installing cards.");
        var scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.isLoaded) throw new InvalidOperationException("Open Main Menu before installing cards.");
        var root = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<RectTransform>(true))
            .Single(t => t.name == "LevelSelect" && t.parent != null && t.parent.name == "Canvas");
        if (root.GetComponentInChildren<LevelSelectionCarousel>(true) != null) {
            foreach (var graphic in root.GetComponentsInChildren<LevelCardGraphic>(true))
                if (graphic.GetComponent<CanvasRenderer>() == null) Undo.AddComponent<CanvasRenderer>(graphic.gameObject);
            Polish(root);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Debug.Log("Level selection cards are already installed.");
            return;
        }
        var start = root.Find("Page/StartGameBtn").GetComponent<Button>();
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UI/BoldPixels/Assets/BoldsPixels SDF.asset");
        var database = AssetDatabase.LoadAssetAtPath<LevelSettingsDatabase>("Assets/Resources/LevelSettingsDatabase.asset");
        if (font == null || database == null || start == null) throw new InvalidOperationException("Missing font, database, or existing StartGameBtn.");

        Undo.IncrementCurrentGroup();
        var undo = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Install level selection cards");
        Undo.RegisterFullObjectHierarchyUndo(root.gameObject, "Install level selection cards");
        // Preserve the existing controls' world positions while making the screen responsive.
        Canvas.ForceUpdateCanvases();
        var children = root.Cast<RectTransform>().ToArray();
        var positions = children.Select(t => t.position).ToArray();
        var sizes = children.Select(t => t.rect.size).ToArray();
        Stretch(root, Vector2.zero, Vector2.one);
        Canvas.ForceUpdateCanvases();
        for (var i = 0; i < children.Length; i++) {
            children[i].SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, sizes[i].x);
            children[i].SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, sizes[i].y);
            children[i].position = positions[i];
        }

        // The full mockup is a reference image, not interactive UI.
        root.Find("Bg (1)")?.gameObject.SetActive(false);
        var background = root.Find("Bg") as RectTransform;
        if (background != null) {
            Stretch(background, Vector2.zero, Vector2.one);
            background.GetComponent<Image>().color = new Color(0.025f, 0.035f, 0.012f, 0.88f);
        }
        var page = root.Find("Page");
        foreach (Transform child in page) if (child != start.transform) child.gameObject.SetActive(false);

        var host = Rect("Level Cards", root);
        Undo.RegisterCreatedObjectUndo(host.gameObject, "Create level cards");
        Stretch(host, Vector2.zero, Vector2.one);
        var area = Rect("Cards", host);
        Stretch(area, new Vector2(0.06f, 0.25f), new Vector2(0.94f, 0.79f));
        var views = Enumerable.Range(1, 6).Select(i => CreateCard(area, i, font)).ToArray();
        var previous = Arrow(host, "Previous Levels", 0.025f, LevelCardGraphic.Shape.LeftArrow);
        var next = Arrow(host, "Next Levels", 0.975f, LevelCardGraphic.Shape.RightArrow);
        var controller = host.gameObject.AddComponent<LevelSelectionCarousel>();
        controller.Configure(database, area, views, previous, next, start);

        // Repair only the obsolete selection callback; preserve unrelated existing actions.
        for (var i = start.onClick.GetPersistentEventCount() - 1; i >= 0; i--) {
            var method = start.onClick.GetPersistentMethodName(i);
            if (method == "StartGame" && (start.onClick.GetPersistentTarget(i) == null || start.onClick.GetPersistentTarget(i) is LevelSelector))
                UnityEventTools.RemovePersistentListener(start.onClick, i);
        }
        UnityEventTools.AddPersistentListener(start.onClick, controller.StartSelectedLevel);
        EditorUtility.SetDirty(start);
        Polish(root);
        Undo.CollapseUndoOperations(undo);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = host.gameObject;
        File.WriteAllText("Temp/LevelCardsInstalled.txt", "Installed in Main Camera/Canvas/LevelSelect. " + DateTime.Now);
        Debug.Log("Level cards installed. Existing Start Game, Back and logo retained.");
    }

    private static LevelSelectionCard CreateCard(RectTransform parent, int slot, TMP_FontAsset font) {
        var root = Rect("Level Card " + slot, parent);
        root.sizeDelta = new Vector2(125, 240);
        var card = root.gameObject.AddComponent<LevelSelectionCard>();
        card.frame = root.gameObject.AddComponent<LevelCardGraphic>();
        card.frame.Configure(LevelCardGraphic.Shape.Frame, Green);
        card.frame.raycastTarget = true;
        card.button = root.gameObject.AddComponent<Button>();
        card.button.targetGraphic = card.frame;
        var colors = card.button.colors;
        colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f);
        colors.selectedColor = Color.white;
        colors.pressedColor = new Color(0.8f, 0.9f, 0.65f);
        colors.disabledColor = Color.white;
        card.button.colors = colors;

        var previewRoot = Rect("Map Window", root);
        Stretch(previewRoot, new Vector2(0, 0.23f), Vector2.one);
        previewRoot.offsetMin = new Vector2(8, 0);
        previewRoot.offsetMax = new Vector2(-8, -9);
        previewRoot.gameObject.AddComponent<RectMask2D>();
        var preview = Rect("Map Preview", previewRoot);
        Stretch(preview, Vector2.zero, Vector2.one);
        card.preview = preview.gameObject.AddComponent<Image>();
        card.preview.raycastTarget = false;

        var missing = Rect("Uncharted Map", previewRoot);
        Stretch(missing, Vector2.zero, Vector2.one);
        missing.gameObject.AddComponent<Image>().color = new Color(0.10f, 0.14f, 0.055f);
        // Subtle map grid for levels which have not received a thumbnail yet.
        for (var i = 1; i < 5; i++) {
            var line = Rect("Grid " + i, missing);
            Stretch(line, new Vector2(i / 5f, 0), new Vector2(i / 5f, 1));
            line.sizeDelta = new Vector2(1, 0);
            line.gameObject.AddComponent<Image>().color = new Color(0.25f, 0.30f, 0.12f, 0.25f);
            line.GetComponent<Image>().raycastTarget = false;
        }
        missing.GetComponent<Image>().raycastTarget = false;
        card.missingPreview = missing.gameObject;

        var shade = Rect("Locked Shade", previewRoot);
        Stretch(shade, Vector2.zero, Vector2.one);
        card.lockedShade = shade.gameObject.AddComponent<Image>();
        card.lockedShade.color = new Color(0.065f, 0.075f, 0.07f, 0.43f);
        card.lockedShade.raycastTarget = false;
        var lockRect = Rect("Lock", previewRoot);
        lockRect.sizeDelta = new Vector2(30, 38);
        lockRect.gameObject.AddComponent<LevelCardGraphic>().Configure(LevelCardGraphic.Shape.Lock, new Color(0.70f, 0.72f, 0.66f));
        card.lockIcon = lockRect.gameObject;

        var footer = Rect("Footer", root);
        Stretch(footer, Vector2.zero, new Vector2(1, 0.23f));
        footer.offsetMin = new Vector2(8, 8);
        footer.offsetMax = new Vector2(-8, 0);
        card.titleLabel = Label("Level Name", footer, font, 14);
        Stretch(card.titleLabel.rectTransform, new Vector2(0.025f, 0.45f), new Vector2(0.975f, 1));
        card.titleLabel.enableAutoSizing = true;
        card.titleLabel.fontSizeMin = 9;
        card.titleLabel.fontSizeMax = 14;
        card.titleLabel.enableWordWrapping = true;
        card.titleLabel.overflowMode = TextOverflowModes.Ellipsis;
        card.stars = new LevelCardGraphic[3];
        for (var i = 0; i < 3; i++) {
            var star = Rect("Star " + (i + 1), footer);
            star.anchorMin = star.anchorMax = new Vector2(0.25f + i * 0.25f, 0.24f);
            star.sizeDelta = new Vector2(19, 19);
            card.stars[i] = star.gameObject.AddComponent<LevelCardGraphic>();
            card.stars[i].Configure(LevelCardGraphic.Shape.Star, new Color(0.3f, 0.32f, 0.24f));
        }

        var badge = Rect("Level Number", root);
        badge.anchorMin = badge.anchorMax = new Vector2(0.5f, 1);
        badge.sizeDelta = new Vector2(43, 31);
        card.numberFrame = badge.gameObject.AddComponent<LevelCardGraphic>();
        card.numberFrame.Configure(LevelCardGraphic.Shape.Frame, Green);
        card.numberLabel = Label("Number", badge, font, 24);
        Stretch(card.numberLabel.rectTransform, Vector2.zero, Vector2.one);
        card.numberLabel.rectTransform.offsetMin = new Vector2(4, 2);
        card.numberLabel.rectTransform.offsetMax = new Vector2(-4, -2);
        return card;
    }

    private static Button Arrow(RectTransform parent, string name, float x, LevelCardGraphic.Shape shape) {
        var rect = Rect(name, parent);
        rect.anchorMin = rect.anchorMax = new Vector2(x, 0.52f);
        rect.sizeDelta = new Vector2(38, 64);
        var hit = rect.gameObject.AddComponent<Image>();
        hit.color = Color.clear;
        var icon = Rect("Arrow", rect);
        icon.sizeDelta = new Vector2(22, 44);
        var graphic = icon.gameObject.AddComponent<LevelCardGraphic>();
        graphic.Configure(shape, Green);
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = graphic;
        var colors = button.colors;
        colors.disabledColor = new Color(0.3f, 0.33f, 0.23f, 0.3f);
        colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f);
        colors.selectedColor = Color.white;
        button.colors = colors;
        return button;
    }

    private static RectTransform Rect(string name, Transform parent) {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.gameObject.layer = parent.gameObject.layer;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        return rect;
    }

    private static void Stretch(RectTransform rect, Vector2 min, Vector2 max) {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static TextMeshProUGUI Label(string name, Transform parent, TMP_FontAsset font, float size) {
        var label = Rect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.fontSharedMaterial = CardTextMaterial(font);
        label.fontSize = size;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        return label;
    }

    private static Material CardTextMaterial(TMP_FontAsset font) {
        const string path = "Assets/UI/LevelCardText.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;
        material = new Material(font.material) { name = "LevelCardText" };
        material.SetColor("_FaceColor", Color.white);
        material.SetColor("_OutlineColor", new Color(0.025f, 0.03f, 0.012f));
        material.SetFloat("_OutlineWidth", 0.12f);
        material.SetColor("_UnderlayColor", Color.black);
        material.SetFloat("_UnderlayOffsetX", 0.5f);
        material.SetFloat("_UnderlayOffsetY", -0.5f);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void Polish(RectTransform root) {
        Undo.RegisterFullObjectHierarchyUndo(root.gameObject, "Style level selection");
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UI/BoldPixels/Assets/BoldsPixels SDF.asset");
        var carousel = root.GetComponentInChildren<LevelSelectionCarousel>(true);
        foreach (var label in carousel.GetComponentsInChildren<TextMeshProUGUI>(true)) label.fontSharedMaterial = CardTextMaterial(font);
        foreach (var card in carousel.GetComponentsInChildren<LevelSelectionCard>(true)) {
            var fitter = card.preview.GetComponent<AspectRatioFitter>();
            if (fitter == null) fitter = card.preview.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        }

        // Retain the original buttons and their callbacks; replace their decorative children.
        var page = (RectTransform)root.Find("Page");
        Stretch(page, Vector2.zero, Vector2.one);
        var start = page.Find("StartGameBtn").GetComponent<Button>();
        StyleExistingButton(start, "START GAME", font, new Vector2(0.5f, 0.105f), new Vector2(310, 58), true);
        var back = root.Find("Button").GetComponent<Button>();
        StyleExistingButton(back, "BACK", font, new Vector2(0.095f, 0.105f), new Vector2(120, 42), false);
        if (root.Find("Campaign Decoration") == null) {
            var decoration = Rect("Campaign Decoration", root);
            Stretch(decoration, Vector2.zero, Vector2.one);
            decoration.SetSiblingIndex(1);
            var top = Rect("Top Rule", decoration);
            Stretch(top, new Vector2(0.205f, 0.93f), new Vector2(0.985f, 0.93f));
            top.sizeDelta = new Vector2(0, 2);
            var line = top.gameObject.AddComponent<Image>();
            line.color = new Color(0.24f, 0.36f, 0.06f);
            line.raycastTarget = false;
            var banner = Rect("Sword Banner", decoration);
            banner.anchorMin = banner.anchorMax = new Vector2(0.105f, 0.82f);
            banner.sizeDelta = new Vector2(52, 64);
            banner.gameObject.AddComponent<LevelCardGraphic>().Configure(LevelCardGraphic.Shape.Banner, new Color(0.39f, 0.56f, 0.07f));
            var swords = Rect("Crossed Swords", banner);
            swords.sizeDelta = new Vector2(40, 42);
            swords.gameObject.AddComponent<LevelCardGraphic>().Configure(LevelCardGraphic.Shape.CrossedSwords, new Color(0.035f, 0.065f, 0.012f));
            foreach (var x in new[] { 0.015f, 0.985f }) {
                var edge = Rect("Edge Rule", decoration);
                Stretch(edge, new Vector2(x, 0.20f), new Vector2(x, 0.72f));
                edge.sizeDelta = new Vector2(1, 0);
                var image = edge.gameObject.AddComponent<Image>();
                image.color = new Color(0.19f, 0.28f, 0.07f, 0.65f);
                image.raycastTarget = false;
            }
        }
        var logo = (RectTransform)root.Find("TITLE");
        logo.anchorMin = logo.anchorMax = new Vector2(0.105f, 0.915f);
        logo.anchoredPosition = Vector2.zero;
        logo.localScale = Vector3.one;
        logo.localRotation = Quaternion.identity;
        logo.sizeDelta = new Vector2(175, 68);
        var logoText = logo.GetComponent<TextMeshProUGUI>();
        logoText.fontSize = 34;
        logoText.raycastTarget = false;
        var area = (RectTransform)carousel.transform.Find("Cards");
        Stretch(area, new Vector2(0.06f, 0.245f), new Vector2(0.94f, 0.715f));
        // Opened through the existing Play transition, so the initial menu remains usable.
        root.gameObject.SetActive(false);
        carousel.Refresh();
        AssetDatabase.SaveAssets();
    }

    private static void StyleExistingButton(Button button, string caption, TMP_FontAsset font, Vector2 anchor, Vector2 size, bool primary) {
        var rect = (RectTransform)button.transform;
        if (primary && button.GetComponent<CanvasGroup>() == null) button.gameObject.AddComponent<CanvasGroup>();
        rect.anchorMin = rect.anchorMax = anchor;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
        foreach (Transform child in rect) if (child.name != "Card Button Style") child.gameObject.SetActive(false);
        var style = rect.Find("Card Button Style") as RectTransform;
        if (style == null) {
            style = Rect("Card Button Style", rect);
            Stretch(style, Vector2.zero, Vector2.one);
            var border = style.gameObject.AddComponent<LevelCardGraphic>();
            border.Configure(LevelCardGraphic.Shape.Frame, primary ? Green : new Color(0.40f, 0.46f, 0.20f));
            if (primary) {
                var fill = Rect("Green Inset", style);
                Stretch(fill, Vector2.zero, Vector2.one);
                fill.offsetMin = new Vector2(9, 9);
                fill.offsetMax = new Vector2(-9, -9);
                var image = fill.gameObject.AddComponent<Image>();
                image.color = new Color(0.31f, 0.51f, 0.025f);
                image.raycastTarget = false;
                var trim = Rect("Top Highlight", fill);
                Stretch(trim, new Vector2(0, 1), Vector2.one);
                trim.sizeDelta = new Vector2(0, 2);
                trim.gameObject.AddComponent<Image>().color = new Color(0.65f, 0.84f, 0.19f);
                trim.GetComponent<Image>().raycastTarget = false;
            }
            var text = Label("Caption", style, font, primary ? 32 : 18);
            text.text = caption;
            text.color = primary ? Color.white : new Color(0.73f, 0.79f, 0.40f);
            Stretch(text.rectTransform, new Vector2(primary ? 0.04f : 0.30f, 0), new Vector2(0.96f, 1));
            if (!primary) {
                var arrow = Rect("Back Arrow", style);
                arrow.anchorMin = arrow.anchorMax = new Vector2(0.19f, 0.5f);
                arrow.sizeDelta = new Vector2(14, 24);
                arrow.gameObject.AddComponent<LevelCardGraphic>().Configure(LevelCardGraphic.Shape.LeftArrow, Green);
            }
        }
        var hit = button.GetComponent<Image>();
        if (hit != null) { hit.color = Color.clear; hit.raycastTarget = true; }
        button.targetGraphic = style.GetComponent<LevelCardGraphic>();
        button.transition = Selectable.Transition.ColorTint;
        var colors = ColorBlock.defaultColorBlock;
        colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f);
        colors.selectedColor = Color.white;
        colors.pressedColor = new Color(0.72f, 0.82f, 0.5f);
        colors.disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.6f);
        button.colors = colors;
    }

    [MenuItem("Tools/RTS/Validate Level Selection Cards")]
    public static void ValidateCards() {
        var controller = Object.FindObjectsOfType<LevelSelectionCarousel>(true).Single();
        var data = AssetDatabase.LoadAssetAtPath<LevelSettingsDatabase>("Assets/Resources/LevelSettingsDatabase.asset");
        var views = controller.GetComponentsInChildren<LevelSelectionCard>(true);
        void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        Check(views.Length == 6, "Expected six reusable card slots.");
        Check(controller.PageCount == Mathf.CeilToInt(data.LevelCount / 6f), "Incorrect page count.");
        var settings = new LevelSettingsDatabase.LevelSettings { bronzeTimeSeconds = 600, silverTimeSeconds = 300, goldTimeSeconds = 180 };
        Check(LevelSelectionCarousel.StarsForTime(0, settings) == 0, "Unplayed medal result.");
        Check(LevelSelectionCarousel.StarsForTime(180, settings) == 3, "Gold boundary.");
        Check(LevelSelectionCarousel.StarsForTime(300, settings) == 2, "Silver boundary.");
        Check(LevelSelectionCarousel.StarsForTime(600, settings) == 1, "Bronze boundary.");
        Check(LevelSelectionCarousel.StarsForTime(601, settings) == 0, "Outside medal time.");
        var selected = controller.SelectedLevel;
        var oldPage = controller.CurrentPage;
        try {
            controller.SetPage(999);
            Check(controller.CurrentPage == controller.PageCount - 1, "Last page boundary.");
            Check(views.Count(v => v.gameObject.activeSelf) == (data.LevelCount - 1) % 6 + 1, "Partial last page.");
            controller.SetPage(-1);
            Check(controller.CurrentPage == 0 && views[0].numberLabel.text == "1", "First page mapping.");
            controller.NextPage();
            Check(views[0].numberLabel.text == "7", "Second page mapping.");
            controller.SelectLevel(0);
            controller.SelectLevel(data.LevelCount + 1);
            Check(controller.SelectedLevel == selected, "Invalid selection changed the selected level.");
            foreach (var graphic in controller.GetComponentsInChildren<LevelCardGraphic>(true))
                Check(graphic.GetComponent<CanvasRenderer>() != null, "Missing card CanvasRenderer.");
            var start = controller.transform.parent.Find("Page/StartGameBtn").GetComponent<Button>();
            Check(Enumerable.Range(0, start.onClick.GetPersistentEventCount()).Any(i => start.onClick.GetPersistentTarget(i) == controller && start.onClick.GetPersistentMethodName(i) == "StartSelectedLevel"), "Start Game is not wired to the cards.");
            File.WriteAllText("Temp/LevelCardsValidation.txt", "PASS: six slots; pagination including partial last page; level mapping; invalid-selection guard; medal boundaries; renderers; existing Start Game binding.");
            Debug.Log("Level selection card validation passed.");
        } finally {
            controller.SetPage(oldPage);
        }
    }

    [MenuItem("Tools/RTS/Save Level Selection Screenshot")]
    public static void SaveScreenshot() {
        if (!EditorApplication.isPlaying || Object.FindObjectOfType<LevelSelectionCarousel>() == null) {
            Debug.LogWarning("Open level selection in Play Mode before capturing it.");
            return;
        }
        Directory.CreateDirectory("Docs");
        ScreenCapture.CaptureScreenshot(Path.GetFullPath("Docs/LevelSelection.png"));
    }

    [MenuItem("Tools/RTS/Capture Missing Level Card Previews")]
    public static void CapturePreviews() {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var database = AssetDatabase.LoadAssetAtPath<LevelSettingsDatabase>("Assets/Resources/LevelSettingsDatabase.asset");
        Directory.CreateDirectory("Assets/UI/LevelPreviews");
        for (var level = 1; level <= database.LevelCount; level++) {
            var settings = database.GetSettingsAt(level - 1);
            if (settings.previewImage != null && !AssetDatabase.GetAssetPath(settings.previewImage).StartsWith("Assets/UI/LevelPreviews/", StringComparison.Ordinal)) continue;
            var path = "Assets/Scenes/" + settings.sceneName + ".unity";
            if (!File.Exists(path)) continue;
            // Do not disturb a battle scene the designer already has open.
            if (SceneManager.GetSceneByPath(path).isLoaded) continue;
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            var otherRenderers = Object.FindObjectsOfType<Renderer>(true).Where(r => r.enabled && r.gameObject.scene != scene).ToArray();
            var otherCanvases = Object.FindObjectsOfType<Canvas>(true).Where(c => c.enabled && c.gameObject.scene != scene).ToArray();
            RenderTexture target = null;
            Texture2D texture = null;
            var oldActive = RenderTexture.active;
            try {
                var camera = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<Camera>(true)).FirstOrDefault(c => c.CompareTag("MainCamera"));
                if (camera == null) continue;
                foreach (var renderer in otherRenderers) renderer.enabled = false;
                foreach (var canvas in otherCanvases) canvas.enabled = false;
                foreach (var canvas in scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<Canvas>(true))) canvas.enabled = false;
                camera.scene = scene;
                camera.enabled = false;
                camera.orthographic = true;
                camera.orthographicSize = settings.cameraOrthographicSize;
                camera.aspect = 0.65f;
                target = new RenderTexture(390, 600, 24);
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                texture = new Texture2D(390, 600, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, 390, 600), 0, 0);
                texture.Apply();
                var output = "Assets/UI/LevelPreviews/Level" + settings.sceneName + ".png";
                File.WriteAllBytes(output, texture.EncodeToPNG());
                AssetDatabase.ImportAsset(output);
                var importer = (TextureImporter)AssetImporter.GetAtPath(output);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
                Undo.RecordObject(database, "Assign level preview");
                settings.previewImage = AssetDatabase.LoadAssetAtPath<Sprite>(output);
                EditorUtility.SetDirty(database);
            } finally {
                RenderTexture.active = oldActive;
                foreach (var renderer in otherRenderers) if (renderer != null) renderer.enabled = true;
                foreach (var canvas in otherCanvases) if (canvas != null) canvas.enabled = true;
                if (target != null) Object.DestroyImmediate(target);
                if (texture != null) Object.DestroyImmediate(texture);
                EditorSceneManager.CloseScene(scene, true);
            }
        }
        AssetDatabase.SaveAssets();
        foreach (var carousel in Object.FindObjectsOfType<LevelSelectionCarousel>(true)) carousel.Refresh();
        File.WriteAllText("Temp/LevelCardsPreviews.txt", "Captured missing previews. " + DateTime.Now);
    }
}
