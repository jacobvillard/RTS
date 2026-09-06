using System;
using System.IO;
using _Scripts.Units;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RTS.LevelAuthoring {
    public static class EnemyUnitPrefabSetup {
        public static readonly string[] Roles = { "Officer", "Pikemen", "Skirmisher", "Dragoon", "Bannermen" };

        [InitializeOnLoadMethod]
        private static void ScheduleInstall() {
            EditorApplication.delayCall += () => {
                if (!EditorApplication.isPlayingOrWillChangePlaymode) EnsureMissingPrefabs();
            };
        }

        public static void EnsureMissingPrefabs() {
            foreach (var role in Roles) {
                var destination = $"Assets/Prefabs/Units/AI_{role}.prefab";
                if (File.Exists(destination)) continue;
                var source = $"Assets/Prefabs/Units/PL_{role}.prefab";
                if (!File.Exists(source)) throw new InvalidOperationException("Missing source unit: " + source);
                var root = PrefabUtility.LoadPrefabContents(source);
                try {
                    root.name = "AI_" + role;
                    var init = root.GetComponent<UnitInit>();
                    if (init == null) throw new InvalidOperationException(source + " has no UnitInit");
                    var serialized = new SerializedObject(init);
                    serialized.FindProperty("team").enumValueIndex = (int)Team.AI;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    root.layer = LayerMask.NameToLayer("AI");
                    foreach (var child in root.GetComponentsInChildren<Transform>(true)) {
                        if (child.gameObject.layer == LayerMask.NameToLayer("Player")) child.gameObject.layer = root.layer;
                    }
                    foreach (var emblem in root.GetComponentsInChildren<TeamEmblemDisplay>(true)) {
                        var data = new SerializedObject(emblem);
                        data.FindProperty("team").enumValueIndex = (int)Team.AI;
                        data.ApplyModifiedPropertiesWithoutUndo();
                    }
                    root.transform.position = Vector3.zero;
                    foreach (var selection in root.GetComponentsInChildren<SelectedUnit>(true))
                        UnityEngine.Object.DestroyImmediate(selection);
                    foreach (var button in root.GetComponentsInChildren<Button>(true))
                        button.gameObject.SetActive(false);
                    if (role == "Officer" && root.GetComponent<EnemyOfficerCommander>() == null)
                        root.AddComponent<EnemyOfficerCommander>();
                    if (role == "Bannermen" && root.GetComponent<MoraleAura>() == null)
                        root.AddComponent<MoraleAura>();
                    PrefabUtility.SaveAsPrefabAsset(root, destination);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
        }
    }
}
