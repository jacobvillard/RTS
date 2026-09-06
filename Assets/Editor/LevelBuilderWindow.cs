using System;
using System.Collections.Generic;
using System.Linq;
using _Scripts.Units;
using _Scripts.GameManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace RTS.LevelAuthoring {
    public class LevelBuilderWindow : EditorWindow {
        [Serializable] private class MapRule {
            public Tilemap map;
            public LevelAuthoringOperations.Surface surface;
            public Tilemap passable;
        }
        [SerializeField] private List<MapRule> rules = new();
        [SerializeField] private SceneAsset sourceScene;
        [SerializeField] private int tab, mapIndex, seed = 1, columns = 3, rows = 2;
        [SerializeField] private Vector2Int regionOrigin = new(-10, -10), regionSize = new(20, 20);
        [SerializeField] private TileBase tile, alternateTile;
        [SerializeField] private float density = .25f, noiseScale = .15f, angle;
        [SerializeField] private Vector2 spacing = new(.8f, .8f), footprint = Vector2.one;
        [SerializeField] private Vector3 origin;
        [SerializeField] private GameObject troopPrefab, buildingPrefab;
        [SerializeField] private Transform parent;
        [SerializeField] private bool autoOverlays = true, buildingObstacle = true, avoidOverlap = true;
        private bool brush, erase;
        private Vector2 scroll;
        private string status = "Choose a battle scene, then scan its tilemaps.";
        private int strokeGroup = -1;
        private readonly HashSet<Vector3Int> strokeCells = new();
        private Scene activeScene;
        private List<GameObject> troopPalette = new(), buildingPalette = new();

        [MenuItem("Tools/RTS/Level Builder")]
        public static void Open() {
            var window = GetWindow<LevelBuilderWindow>("Level Builder");
            window.minSize = new Vector2(440, 560);
        }

        private void OnEnable() {
            SceneView.duringSceneGui += DuringSceneGUI;
            Undo.undoRedoPerformed += Repaint;
            RefreshPalettes();
            activeScene = SceneManager.GetActiveScene();
            if (rules.Count == 0) ScanMaps();
        }
        private void OnDisable() {
            EndStroke();
            SceneView.duringSceneGui -= DuringSceneGUI;
            Undo.undoRedoPerformed -= Repaint;
            brush = false;
        }
        private void RefreshPalettes() {
            troopPalette = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Units" })
                .Select(g => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(p => p != null && p.GetComponent<UnitInit>() != null).OrderBy(p => p.name).ToList();
            buildingPalette = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Environment" })
                .Select(g => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(p => p != null).OrderBy(p => p.name).ToList();
        }
        private void ScanMaps() {
            var scene = SceneManager.GetActiveScene();
            var previous = rules.Where(r => r.map != null).ToDictionary(r => r.map, r => r);
            rules = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Tilemap>(true))
                .Select(m => previous.TryGetValue(m, out var rule) ? rule : new MapRule {
                    map = m, surface = LevelAuthoringOperations.GuessSurface(m.name)
                }).ToList();
            mapIndex = Mathf.Clamp(mapIndex, 0, Mathf.Max(0, rules.Count - 1));
            status = $"Found {rules.Count} tilemaps. Review surface rules before generating overlays.";
        }
        private MapRule Current => mapIndex >= 0 && mapIndex < rules.Count ? rules[mapIndex] : null;

        private void OnGUI() {
            if (activeScene != SceneManager.GetActiveScene()) {
                EndStroke();
                activeScene = SceneManager.GetActiveScene(); brush = false; parent = null; ScanMaps();
            }
            EditorGUILayout.LabelField("Battle Boxes Level Builder", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Active scene: " + activeScene.name);
            var nextTab = GUILayout.Toolbar(tab, new[] { "Scene", "Tilemaps", "Troops", "Buildings" });
            if (nextTab != tab) { EndStroke(); tab = nextTab; brush = false; }
            var wasBrushing = brush;
            scroll = EditorGUILayout.BeginScrollView(scroll);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode)) {
                if (tab == 0) DrawScene();
                if (tab == 1) DrawTiles();
                if (tab == 2 || tab == 3) DrawPlacement();
            }
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(status, MessageType.Info);
            EditorGUILayout.EndScrollView();
            if (wasBrushing && !brush) EndStroke();
        }

        private void DrawScene() {
            EditorGUILayout.HelpBox("Duplicate an existing battle or Template to retain the project's cameras, managers, grid and navigation setup. Source scenes are preserved.", MessageType.None);
            sourceScene = (SceneAsset)EditorGUILayout.ObjectField("Base scene", sourceScene, typeof(SceneAsset), false);
            if (GUILayout.Button("Use active scene as base")) sourceScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(activeScene.path);
            if (GUILayout.Button("Create level copy...")) Run(() => {
                if (sourceScene == null) throw new InvalidOperationException("Choose a saved base scene.");
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                var path = EditorUtility.SaveFilePanelInProject("Create level", "NewBattle", "unity", "Choose a new scene filename.", "Assets/Scenes");
                if (string.IsNullOrEmpty(path)) return;
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null) throw new InvalidOperationException("Choose a new filename; existing levels are not overwritten.");
                if (!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(sourceScene), path)) throw new InvalidOperationException("Could not copy scene.");
                EditorSceneManager.OpenScene(path);
                ScanMaps();
                status = "Created " + path + ". Set its database entry and rebuild navigation after editing.";
            });
            if (GUILayout.Button("Register active scene in Build Settings")) Run(() => {
                if (string.IsNullOrEmpty(activeScene.path)) throw new InvalidOperationException("Save the scene first.");
                var scenes = EditorBuildSettings.scenes.ToList();
                var found = scenes.Find(s => s.path == activeScene.path);
                if (found != null) found.enabled = true;
                else scenes.Add(new EditorBuildSettingsScene(activeScene.path, true));
                EditorBuildSettings.scenes = scenes.ToArray();
                status = "Scene registered; startup order was preserved.";
            });
            if (GUILayout.Button("Select level settings database")) Selection.activeObject = AssetDatabase.LoadMainAssetAtPath("Assets/Resources/LevelSettingsDatabase.asset");
            DrawLevelSettings();
            if (GUILayout.Button("Scan active scene tilemaps")) ScanMaps();
            if (GUILayout.Button("Validate active level")) Run(ValidateLevel);
            if (GUILayout.Button("Build active scene navigation")) Run(() => {
                var surfaces = activeScene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<NavMeshPlus.Components.NavMeshSurface>(true)).ToArray();
                if (surfaces.Length == 0) throw new InvalidOperationException("No NavMeshPlus surface found in this scene.");
                foreach (var surface in surfaces) { Undo.RecordObject(surface, "Build navigation"); surface.BuildNavMesh(); EditorUtility.SetDirty(surface); }
                EditorSceneManager.MarkSceneDirty(activeScene);
                status = "Navigation rebuilt. Save the scene and playtest paths across bridges and around buildings.";
            });
            if (GUILayout.Button("Refresh unit and building palettes")) RefreshPalettes();
            if (GUILayout.Button("Run level builder self-checks")) LevelBuilderChecks.Run();
        }

        private void DrawLevelSettings() {
            var database = LevelSettingsDatabase.Load();
            if (database == null) return;
            var data = new SerializedObject(database);
            var levels = data.FindProperty("levels");
            SerializedProperty entry = null;
            for (var i = 0; i < levels.arraySize; i++) {
                var candidate = levels.GetArrayElementAtIndex(i);
                if (candidate.FindPropertyRelative("sceneName").stringValue == activeScene.name) { entry = candidate; break; }
            }
            if (entry == null) {
                EditorGUILayout.HelpBox("No settings entry matches this scene. Use a numeric scene name for compatibility with campaign progression.", MessageType.Info);
                return;
            }
            EditorGUILayout.PropertyField(entry, new GUIContent("This level's settings"), true);
            if (data.ApplyModifiedProperties()) EditorUtility.SetDirty(database);
        }

        private void DrawTiles() {
            if (GUILayout.Button("Scan tilemaps")) ScanMaps();
            if (rules.Count == 0) { EditorGUILayout.HelpBox("Open a battle scene containing a Grid and Tilemaps.", MessageType.Info); return; }
            var nextMap = EditorGUILayout.Popup("Tilemap", mapIndex, rules.Select(r => r.map != null ? r.map.name : "Missing").ToArray());
            if (nextMap != mapIndex) { EndStroke(); mapIndex = nextMap; }
            var rule = Current;
            if (rule?.map == null) return;
            rule.surface = (LevelAuthoringOperations.Surface)EditorGUILayout.EnumPopup("Surface rule", rule.surface);
            rule.passable = (Tilemap)EditorGUILayout.ObjectField("Passable / bridge tiles", rule.passable, typeof(Tilemap), true);
            EditorGUILayout.HelpBox("Water uses SeethroughObstacle. Solid uses CastleWall. Decoration creates none. Assign a separate bridge tilemap to leave openings; default classifications are based on existing map-layer names.", MessageType.None);
            tile = (TileBase)EditorGUILayout.ObjectField("Paint tile", tile, typeof(TileBase), false);
            alternateTile = (TileBase)EditorGUILayout.ObjectField("Variation tile", alternateTile, typeof(TileBase), false);
            autoOverlays = EditorGUILayout.Toggle("Sync overlays after painting", autoOverlays);
            erase = EditorGUILayout.Toggle("Erase brush", erase);
            brush = GUILayout.Toggle(brush, brush ? "Painting active — Escape to stop" : "Paint in Scene view", "Button");
            regionOrigin = EditorGUILayout.Vector2IntField("Region start cell", regionOrigin);
            regionSize = EditorGUILayout.Vector2IntField("Region size", regionSize);
            seed = EditorGUILayout.IntField("Seed", seed);
            density = EditorGUILayout.Slider("Variation coverage", density, 0, 1);
            noiseScale = EditorGUILayout.Slider("Noise scale", noiseScale, .01f, 1);
            if (GUILayout.Button("Fill region")) Run(() => FillRegion(false));
            if (GUILayout.Button("Generate terrain variation")) Run(() => FillRegion(true));
            if (GUILayout.Button("Erase region")) Run(() => FillRegion(false, true));
            if (GUILayout.Button("Sync this tilemap's overlays")) Run(() => InUndo("Sync tile overlays", () => {
                status = "Generated " + Sync(rule) + " obstacle strips.";
            }));
            if (GUILayout.Button("Sync all tilemap overlays")) Run(() => InUndo("Sync all overlays", () => {
                var count = 0; foreach (var r in rules) count += Sync(r);
                status = $"Generated {count} obstacle strips. Rebuild navigation when ready.";
            }));
        }

        private int Sync(MapRule rule) {
            if (rule.map == null) return 0;
            if (rule.passable == rule.map || (rule.passable != null && rule.passable.gameObject.scene != activeScene))
                throw new InvalidOperationException("Bridge exceptions must use a different tilemap in the active scene.");
            return LevelAuthoringOperations.SyncOverlays(rule.map, rule.surface, rule.passable);
        }
        private void FillRegion(bool varied, bool clear = false) {
            if (Current?.map == null || (!clear && tile == null)) throw new InvalidOperationException("Choose a tilemap and paint tile.");
            if (varied && alternateTile == null) throw new InvalidOperationException("Choose a variation tile.");
            if (regionSize.x < 1 || regionSize.y < 1 || (long)regionSize.x * regionSize.y > 65536)
                throw new InvalidOperationException("Use a region of 1-65,536 cells.");
            InUndo("Generate tile region", () => {
                var map = Current.map;
                Undo.RegisterCompleteObjectUndo(map, "Paint tiles");
                var random = new System.Random(seed);
                var offset = new Vector2(random.Next(10000), random.Next(10000));
                var values = new TileBase[regionSize.x * regionSize.y];
                for (var y = 0; y < regionSize.y; y++) for (var x = 0; x < regionSize.x; x++) {
                    var sample = Mathf.PerlinNoise((regionOrigin.x + x) * noiseScale + offset.x, (regionOrigin.y + y) * noiseScale + offset.y);
                    values[y * regionSize.x + x] = clear ? null : varied && sample < density ? alternateTile : tile;
                }
                map.SetTilesBlock(new BoundsInt(regionOrigin.x, regionOrigin.y, 0, regionSize.x, regionSize.y, 1), values);
                if (autoOverlays) Sync(Current);
                status = "Region updated. Ctrl+Z undoes tiles and generated overlays together.";
            });
        }

        private void DrawPlacement() {
            var troops = tab == 2;
            var palette = troops ? troopPalette : buildingPalette;
            var chosen = troops ? troopPrefab : buildingPrefab;
            chosen = (GameObject)EditorGUILayout.ObjectField("Prefab", chosen, typeof(GameObject), false);
            if (palette.Count > 0) {
                var index = palette.IndexOf(chosen);
                var labels = new[] { "Choose from existing prefabs..." }.Concat(palette.Select(p => p.name)).ToArray();
                var next = EditorGUILayout.Popup("Palette", index + 1, labels);
                if (next > 0) chosen = palette[next - 1];
            }
            if (troops) troopPrefab = chosen; else buildingPrefab = chosen;
            parent = (Transform)EditorGUILayout.ObjectField("Parent (optional)", parent, typeof(Transform), true);
            origin = EditorGUILayout.Vector3Field("Position / formation centre", origin);
            angle = EditorGUILayout.FloatField("Rotation Z", angle);
            if (troops) {
                columns = EditorGUILayout.IntSlider("Columns", columns, 1, 25);
                rows = EditorGUILayout.IntSlider("Rows", rows, 1, 20);
                spacing = EditorGUILayout.Vector2Field("Spacing", spacing);
                avoidOverlap = EditorGUILayout.Toggle("Reject occupied troop positions", avoidOverlap);
            } else {
                buildingObstacle = EditorGUILayout.Toggle("Add CastleWall footprint", buildingObstacle);
                footprint = EditorGUILayout.Vector2Field("Local footprint size", footprint);
                EditorGUILayout.HelpBox("Choose any building prefab, including one saved from an existing level. The footprint adds movement/vision blocking; keep entrances outside it. Disable it for walkable objectives or existing blockers.", MessageType.None);
            }
            brush = GUILayout.Toggle(brush, brush ? "Placement active — Escape to stop" : "Preview and place in Scene view", "Button");
            if (GUILayout.Button(troops ? "Place formation at position" : "Place building at position")) Run(() => Place(origin));
            if (!troops && GUILayout.Button("Add footprint to selected scene object")) Run(() => {
                var target = Selection.activeGameObject;
                if (target == null || target.scene != activeScene) throw new InvalidOperationException("Select an object in the active scene.");
                if (target.transform.Find("__LevelBuilderFootprint") != null) throw new InvalidOperationException("This object already has a generated footprint.");
                CheckFootprint();
                InUndo("Add building footprint", () => LevelAuthoringOperations.AddBuildingObstacle(target, footprint));
            });
        }

        private void CheckFootprint() {
            if (footprint.x <= 0 || footprint.y <= 0) throw new InvalidOperationException("Footprint dimensions must be positive.");
        }
        private void Place(Vector3 position) {
            var troops = tab == 2;
            var prefab = troops ? troopPrefab : buildingPrefab;
            if (prefab == null || !PrefabUtility.IsPartOfPrefabAsset(prefab)) throw new InvalidOperationException("Choose a prefab asset.");
            if (parent != null && parent.gameObject.scene != activeScene) throw new InvalidOperationException("Parent must belong to the active scene.");
            if (troops && prefab.GetComponent<UnitInit>() == null) throw new InvalidOperationException("Troop prefab requires UnitInit.");
            if (troops && (spacing.x <= 0 || spacing.y <= 0)) throw new InvalidOperationException("Spacing must be positive.");
            if (!troops && buildingObstacle) CheckFootprint();
            var points = troops ? LevelAuthoringOperations.Formation(position, columns, rows, spacing, angle) : new[] { position };
            if (troops && avoidOverlap) {
                var units = activeScene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<UnitInit>(true));
                var minimum = Mathf.Min(spacing.x, spacing.y) * .45f;
                if (units.Any(u => points.Any(p => Vector2.Distance(u.transform.position, p) < minimum)))
                    throw new InvalidOperationException("Formation overlaps an existing troop. Move it or disable the overlap check.");
            }
            InUndo("Place level objects", () => {
                foreach (var point in points) {
                    var instance = LevelAuthoringOperations.Spawn(prefab, activeScene, parent, point, Quaternion.Euler(0, 0, angle));
                    if (!troops && buildingObstacle) LevelAuthoringOperations.AddBuildingObstacle(instance, footprint);
                }
                status = $"Placed {points.Length} object(s). Ctrl+Z undoes this batch.";
            });
        }

        private void DuringSceneGUI(SceneView view) {
            if (!brush || EditorApplication.isPlayingOrWillChangePlaymode) return;
            var e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape) { EndStroke(); brush = false; e.Use(); Repaint(); return; }
            if (e.alt) return;
            var ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            var plane = tab == 1 && Current?.map != null ? new Plane(Current.map.transform.forward, Current.map.transform.position) : new Plane(Vector3.forward, origin);
            if (!plane.Raycast(ray, out var distance)) return;
            var point = ray.GetPoint(distance);
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            Handles.color = erase && tab == 1 ? Color.red : Color.green;
            if (tab == 1 && Current?.map != null) {
                var cell = Current.map.WorldToCell(point); cell.z = 0;
                Handles.DrawWireCube(Current.map.GetCellCenterWorld(cell), Current.map.layoutGrid.cellSize);
                if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0 && (erase || tile != null)) {
                    if (strokeGroup < 0) { Undo.IncrementCurrentGroup(); strokeGroup = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Paint tile stroke"); Undo.RegisterCompleteObjectUndo(Current.map, "Paint tiles"); }
                    if (strokeCells.Add(cell)) Current.map.SetTile(cell, erase ? null : tile);
                    e.Use();
                }
                if (e.type == EventType.MouseUp && strokeGroup >= 0) {
                    EndStroke();
                    EditorSceneManager.MarkSceneDirty(activeScene); e.Use();
                }
            } else if (tab == 2 || tab == 3) {
                var points = tab == 2 ? LevelAuthoringOperations.Formation(point, columns, rows, spacing, angle) : new[] { point };
                foreach (var p in points) Handles.DrawWireDisc(p, Vector3.forward, .25f);
                if (tab == 3) using (new Handles.DrawingScope(Matrix4x4.TRS(point, Quaternion.Euler(0, 0, angle), Vector3.one)))
                    Handles.DrawWireCube(Vector3.zero, new Vector3(footprint.x, footprint.y, 0));
                if (e.type == EventType.MouseDown && e.button == 0) { Run(() => Place(point)); e.Use(); }
            }
            view.Repaint();
        }

        private void EndStroke() {
            if (strokeGroup < 0) return;
            try { if (autoOverlays && Current?.map != null) Sync(Current); }
            catch (Exception exception) { status = "Tiles painted; overlay sync needs attention: " + exception.Message; }
            finally { Undo.CollapseUndoOperations(strokeGroup); strokeGroup = -1; strokeCells.Clear(); }
        }

        private void ValidateLevel() {
            var messages = new List<string>();
            if (!EditorBuildSettings.scenes.Any(s => s.enabled && s.path == activeScene.path)) messages.Add("Scene is not enabled in Build Settings.");
            var roots = activeScene.GetRootGameObjects();
            var units = roots.SelectMany(g => g.GetComponentsInChildren<UnitInit>(true)).ToArray();
            if (units.Length == 0) messages.Add("No preplaced troops found.");
            foreach (var unit in units) {
                var data = new SerializedObject(unit);
                if (data.FindProperty("unit").objectReferenceValue == null) messages.Add(unit.name + ": missing unit stats.");
                if (data.FindProperty("team").enumValueIndex == (int)Team.AI && unit.name.Contains("Officer") && unit.GetComponent<EnemyOfficerCommander>() == null)
                    messages.Add(unit.name + ": missing enemy officer commander.");
            }
            foreach (var transform in roots.SelectMany(g => g.GetComponentsInChildren<Transform>(true)))
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) > 0) messages.Add(transform.name + ": missing script.");
            var surfaces = roots.SelectMany(g => g.GetComponentsInChildren<NavMeshPlus.Components.NavMeshSurface>(true)).ToArray();
            if (surfaces.Length == 0 || surfaces.Any(s => s.navMeshData == null)) messages.Add("Navigation surface/data missing; rebuild navigation.");
            status = messages.Count == 0 ? "Basic setup checks passed. Playtest navigation, objectives and budget." : string.Join("\n", messages.Distinct());
            Debug.Log("Level Builder: " + status);
        }
        private void InUndo(string name, Action action) {
            Undo.IncrementCurrentGroup(); var group = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName(name);
            try { action(); EditorSceneManager.MarkSceneDirty(activeScene); }
            catch { Undo.RevertAllDownToGroup(group); throw; }
            finally { Undo.CollapseUndoOperations(group); }
        }
        private void Run(Action action) {
            try { action(); }
            catch (Exception exception) { status = exception.Message; Debug.LogWarning("Level Builder: " + exception.Message); }
            Repaint();
        }
    }
}
