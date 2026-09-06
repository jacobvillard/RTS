using System;
using System.IO;
using System.Linq;
using _Scripts.Units;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace RTS.LevelAuthoring {
    public static class LevelBuilderChecks {
        [InitializeOnLoadMethod]
        private static void RunRequestedCheck() {
            EditorApplication.delayCall += () => {
                if (EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists("Temp/RunLevelBuilderChecks")) return;
                File.Delete("Temp/RunLevelBuilderChecks");
                Run();
            };
        }

        [MenuItem("Tools/RTS/Validate Level Builder Tools")]
        public static void Run() {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                var original = SceneManager.GetActiveScene();
            var scratch = default(Scene);
            Tile tile = null;
            var checks = 0;
            void Check(bool value, string message) {
                if (!value) throw new InvalidOperationException(message);
                checks++;
            }
            try {
                EnemyUnitPrefabSetup.EnsureMissingPrefabs();
                foreach (var role in EnemyUnitPrefabSetup.Roles) {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/Units/AI_{role}.prefab");
                    Check(prefab != null, "Missing enemy " + role);
                    var data = new SerializedObject(prefab.GetComponent<UnitInit>());
                    var stats = data.FindProperty("unit").objectReferenceValue as UnitSO;
                    Check(stats != null && stats.name == role, role + " wrong stats");
                    Check(data.FindProperty("team").enumValueIndex == (int)Team.AI && prefab.layer == LayerMask.NameToLayer("AI"), role + " wrong team/layer");
                    Check(prefab.GetComponentsInChildren<SelectedUnit>(true).Length == 0, role + " retains player selection");
                    if (role == "Officer") Check(prefab.GetComponent<EnemyOfficerCommander>() != null && prefab.GetComponent<OfficerCommandController>() != null, "Officer commands missing");
                    if (role == "Bannermen") Check(prefab.GetComponent<MoraleAura>() != null, "Banner aura missing");
                    if (role == "Dragoon") {
                        Check(stats.aiDismountedPrefab != null, "Dragoon AI dismount missing");
                        var dismount = new SerializedObject(stats.aiDismountedPrefab.GetComponent<UnitInit>());
                        Check(dismount.FindProperty("team").enumValueIndex == (int)Team.AI, "Dragoon dismount uses player team");
                    }
                }
                scratch = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                SceneManager.SetActiveScene(scratch);
                var grid = new GameObject("Test Grid", typeof(Grid));
                SceneManager.MoveGameObjectToScene(grid, scratch);
                var water = new GameObject("Rivers_3", typeof(Tilemap), typeof(TilemapRenderer)).GetComponent<Tilemap>();
                water.transform.SetParent(grid.transform, false);
                var bridge = new GameObject("Bridge", typeof(Tilemap)).GetComponent<Tilemap>();
                bridge.transform.SetParent(grid.transform, false);
                tile = ScriptableObject.CreateInstance<Tile>();
                for (var x = -2; x <= 2; x++) water.SetTile(new Vector3Int(x, 0, 0), tile);
                bridge.SetTile(Vector3Int.zero, tile);
                var runs = LevelAuthoringOperations.GetRuns(water, bridge);
                Check(runs.Count == 2 && runs.Sum(r => r.width) == 4, "Bridge or negative cells lost");
                Check(LevelAuthoringOperations.SyncOverlays(water, LevelAuthoringOperations.Surface.Water, bridge) == 2, "Wrong water overlay count");
                var root = water.transform.Find(LevelAuthoringOperations.OverlayRoot);
                Check(root.GetComponentsInChildren<Collider2D>().Length == 0, "Water blocks vision with collider");
                Check(root.GetComponentsInChildren<NavMeshPlus.Components.NavMeshModifier>().Length > 0, "Water missing navigation modifier");
                LevelAuthoringOperations.SyncOverlays(water, LevelAuthoringOperations.Surface.Water, bridge);
                Check(water.transform.childCount == 1 && water.transform.GetChild(0).childCount == 2, "Sync duplicates overlays");
                Undo.IncrementCurrentGroup();
                var group = Undo.GetCurrentGroup();
                Undo.RegisterCompleteObjectUndo(water, "Test terrain edit");
                water.SetTile(new Vector3Int(-2, 0, 0), null);
                LevelAuthoringOperations.SyncOverlays(water, LevelAuthoringOperations.Surface.Solid, bridge);
                Check(water.transform.GetChild(0).GetComponentsInChildren<Collider2D>().Length == 2, "Solid footprints missing");
                Undo.FlushUndoRecordObjects();
                Undo.RevertAllDownToGroup(group);
                Check(water.HasTile(new Vector3Int(-2, 0, 0)), "Undo did not restore tile");
                Check(water.transform.GetChild(0).GetComponentsInChildren<Collider2D>().Length == 0, "Undo did not restore water overlays");
                var formation = LevelAuthoringOperations.Formation(new Vector3(4, 6, 0), 3, 2, Vector2.one, 90);
                Check(formation.Length == 6 && Vector3.Distance(formation.Aggregate(Vector3.zero, (a, b) => a + b) / 6, new Vector3(4, 6, 0)) < .001f, "Formation centre/rotation wrong");
                var building = new GameObject("Test building");
                LevelAuthoringOperations.AddBuildingObstacle(building, new Vector2(3, 2));
                var collider = building.GetComponentInChildren<BoxCollider2D>();
                Check(collider != null && !collider.isTrigger, "Building collider missing");
                Directory.CreateDirectory("Docs");
                File.WriteAllText("Docs/LevelBuilderChecks.txt", $"PASS: {checks} editor checks. Enemy prefab roles/teams/components, dragoon dismount, bridge exceptions, negative cells, idempotent overlays, water/solid behaviour, Undo, formation centre and building footprint.\nGameplay AI and navigation paths require user playtesting.\n");
                Debug.Log($"Level Builder checks PASS: {checks}");
            }
            catch (Exception exception) {
                File.WriteAllText("Docs/LevelBuilderChecks.txt", "FAIL after " + checks + " checks: " + exception);
                Debug.LogException(exception);
            }
            finally {
                if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
                if (scratch.IsValid()) EditorSceneManager.CloseScene(scratch, true);
                if (tile != null) UnityEngine.Object.DestroyImmediate(tile);
            }
        }
    }
}
