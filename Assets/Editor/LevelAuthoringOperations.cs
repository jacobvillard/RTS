using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace RTS.LevelAuthoring {
    public static class LevelAuthoringOperations {
        public const string OverlayRoot = "__LevelBuilderOverlays";
        public const string ObstacleFolder = "Assets/Prefabs/Environment/Obstacles/";
        public enum Surface { Decoration, Water, Solid }

        public static Surface GuessSurface(string name) {
            name = name.ToLowerInvariant();
            if (name.Contains("river") || name.Contains("water")) return Surface.Water;
            if (name.Contains("mountain") || name.Contains("objects") || name.Contains("wall")) return Surface.Solid;
            return Surface.Decoration;
        }

        public static GameObject Spawn(GameObject prefab, Scene scene, Transform parent, Vector3 position, Quaternion rotation) {
            if (prefab == null || !PrefabUtility.IsPartOfPrefabAsset(prefab))
                throw new InvalidOperationException("Choose a prefab asset first.");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            Undo.RegisterCreatedObjectUndo(instance, "Place level object");
            if (parent != null) Undo.SetTransformParent(instance.transform, parent, "Parent level object");
            instance.transform.SetPositionAndRotation(position, rotation);
            return instance;
        }

        // Consecutive cells share one rectangle. Gaps and bridge cells remain open.
        public static List<RectInt> GetRuns(Tilemap map, Tilemap passable) {
            var bounds = map.cellBounds;
            if ((long)bounds.size.x * bounds.size.y * bounds.size.z > 262144)
                throw new InvalidOperationException("Tilemap bounds are too large. Compress Bounds or use a smaller map.");
            var runs = new List<RectInt>();
            for (var y = bounds.yMin; y < bounds.yMax; y++) {
                var start = int.MinValue;
                for (var x = bounds.xMin; x <= bounds.xMax; x++) {
                    var cell = new Vector3Int(x, y, 0);
                    var occupied = x < bounds.xMax && map.HasTile(cell) &&
                        (passable == null || !passable.HasTile(passable.WorldToCell(map.GetCellCenterWorld(cell))));
                    if (occupied && start == int.MinValue) start = x;
                    if (!occupied && start != int.MinValue) {
                        runs.Add(new RectInt(start, y, x - start, 1));
                        start = int.MinValue;
                    }
                }
            }
            return runs;
        }

        public static int SyncOverlays(Tilemap map, Surface surface, Tilemap passable = null) {
            if (map == null) return 0;
            if (map.layoutGrid == null || map.layoutGrid.cellLayout != GridLayout.CellLayout.Rectangle ||
                map.orientation != Tilemap.Orientation.XY || map.cellBounds.size.z > 1 ||
                (map.GetUsedTilesCount() > 0 && map.cellBounds.zMin != 0))
                throw new InvalidOperationException("Automatic overlays require a rectangular XY tilemap at Z=0.");
            var runs = surface == Surface.Decoration ? new List<RectInt>() : GetRuns(map, passable);
            var prefab = surface == Surface.Decoration ? null : AssetDatabase.LoadAssetAtPath<GameObject>(
                ObstacleFolder + (surface == Surface.Water ? "SeethroughObstacle.prefab" : "CastleWall.prefab"));
            if (surface != Surface.Decoration && prefab == null) throw new InvalidOperationException("Missing obstacle prefab.");
            var old = map.transform.Find(OverlayRoot);
            if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
            if (surface == Surface.Decoration) return 0;
            var root = new GameObject(OverlayRoot);
            SceneManager.MoveGameObjectToScene(root, map.gameObject.scene);
            Undo.RegisterCreatedObjectUndo(root, "Create tile overlays");
            Undo.SetTransformParent(root.transform, map.transform, "Parent tile overlays");
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;
            foreach (var run in runs) {
                var min = map.CellToLocal(new Vector3Int(run.xMin, run.yMin, 0));
                var max = map.CellToLocal(new Vector3Int(run.xMax, run.yMax, 0));
                var instance = Spawn(prefab, map.gameObject.scene, root.transform, Vector3.zero, map.transform.rotation);
                instance.transform.localPosition = (min + max) * 0.5f;
                FitOverlay(instance, new Vector2(Mathf.Abs(max.x - min.x), Mathf.Abs(max.y - min.y)));
            }
            return runs.Count;
        }

        public static void FitOverlay(GameObject instance, Vector2 size) {
            var sprite = instance.GetComponent<SpriteRenderer>();
            var original = sprite != null && sprite.sprite != null ? sprite.sprite.bounds.size : Vector3.one;
            instance.transform.localScale = new Vector3(size.x / Mathf.Max(.001f, original.x), size.y / Mathf.Max(.001f, original.y), 1);
            // Retain the sprite geometry for NavMeshPlus collection, without covering map artwork.
            if (sprite != null) { var color = sprite.color; color.a = 0; sprite.color = color; }
        }

        public static void AddBuildingObstacle(GameObject building, Vector2 footprint) {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ObstacleFolder + "CastleWall.prefab");
            var obstacle = Spawn(prefab, building.scene, building.transform, building.transform.position, building.transform.rotation);
            obstacle.name = "__LevelBuilderFootprint";
            FitOverlay(obstacle, footprint);
        }

        public static Vector3[] Formation(Vector3 origin, int columns, int rows, Vector2 spacing, float angle) {
            if (columns < 1 || rows < 1 || (long)columns * rows > 500) throw new InvalidOperationException("Use 1-500 troops per formation.");
            var rotation = Quaternion.Euler(0, 0, angle);
            return Enumerable.Range(0, columns * rows).Select(i => origin + rotation * new Vector3(
                (i % columns - (columns - 1) * .5f) * spacing.x,
                (i / columns - (rows - 1) * .5f) * spacing.y, 0)).ToArray();
        }
    }
}
