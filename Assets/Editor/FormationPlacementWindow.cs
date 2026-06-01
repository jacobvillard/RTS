using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor window that converts ASCII formation text into scene unit placement.
/// </summary>
public class FormationPlacementWindow : EditorWindow {

    #region Types

    /// <summary>
    /// Maps one text symbol to a prefab.
    /// </summary>
    [Serializable]
    private class SymbolPrefabMapping {
        public string symbol;      // Single character used in the formation text.
        public GameObject prefab;  // Prefab spawned when the symbol is found.
    }

    #endregion
    #region Variables

    private const string WindowTitle = "Formation Placer";

    private const string DefaultFormation =
        "...A.A.A.A.A.A...\n" +
        "..IIIIIIIIIIII..\n" +
        "....I.I.I.I.....\n" +
        "C..............C\n" +
        ".................\n" +
        "......PPPP.......\n" +
        "......PPPP.......";

    [SerializeField] private string formationText = DefaultFormation; // ASCII formation pasted by the designer.
    [SerializeField] private Vector2 origin = Vector2.zero;           // World position used by the first row/column.
    [SerializeField] private Vector2 spacing = Vector2.one;           // Distance between rows and columns.
    [SerializeField] private bool centerOnOrigin = true;              // Centers the parsed formation around origin.
    [SerializeField] private bool topRowIsPositiveY = true;           // Places the first row at positive Y when centered.
    [SerializeField] private Transform parent;                        // Optional parent for spawned units.
    [SerializeField] private List<SymbolPrefabMapping> mappings = new(); // Symbol-to-prefab mappings.

    private Vector2 _scrollPosition; // Editor scroll state.

    #endregion
    #region Menu

    /// <summary>
    /// Opens the formation placement window.
    /// </summary>
    [MenuItem("Tools/RTS/Formation Placer")]
    private static void OpenWindow() {
        var window = GetWindow<FormationPlacementWindow>(WindowTitle);
        window.minSize = new Vector2(420f, 520f);
        window.EnsureDefaultMappings();
        window.Show();
    }

    #endregion
    #region GUI

    private void OnGUI() {
        EnsureDefaultMappings();

        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        DrawFormationInput();
        DrawPlacementSettings();
        DrawMappings();
        DrawActions();

        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// Draws the multiline formation input.
    /// </summary>
    private void DrawFormationInput() {
        EditorGUILayout.LabelField("Formation Text", EditorStyles.boldLabel);
        formationText = EditorGUILayout.TextArea(formationText, GUILayout.MinHeight(150f));
        EditorGUILayout.Space();
    }

    /// <summary>
    /// Draws placement options.
    /// </summary>
    private void DrawPlacementSettings() {
        EditorGUILayout.LabelField("Placement", EditorStyles.boldLabel);
        origin = EditorGUILayout.Vector2Field("Origin", origin);
        spacing = EditorGUILayout.Vector2Field("Spacing", spacing);
        centerOnOrigin = EditorGUILayout.Toggle("Center On Origin", centerOnOrigin);
        topRowIsPositiveY = EditorGUILayout.Toggle("Top Row Positive Y", topRowIsPositiveY);
        parent = (Transform)EditorGUILayout.ObjectField("Parent", parent, typeof(Transform), true);
        EditorGUILayout.Space();
    }

    /// <summary>
    /// Draws symbol-to-prefab mappings.
    /// </summary>
    private void DrawMappings() {
        EditorGUILayout.LabelField("Symbol Mappings", EditorStyles.boldLabel);

        for (var i = 0; i < mappings.Count; i++) {
            var mapping = mappings[i];
            if (mapping == null) continue;

            EditorGUILayout.BeginHorizontal();
            mapping.symbol = EditorGUILayout.TextField(mapping.symbol, GUILayout.Width(40f));
            mapping.prefab = (GameObject)EditorGUILayout.ObjectField(mapping.prefab, typeof(GameObject), false);

            if (GUILayout.Button("-", GUILayout.Width(28f))) {
                mappings.RemoveAt(i);
                i--;
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Symbol")) {
            mappings.Add(new SymbolPrefabMapping { symbol = "?" });
        }

        if (GUILayout.Button("Reset Defaults")) {
            ResetDefaultMappings();
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();
    }

    /// <summary>
    /// Draws action buttons.
    /// </summary>
    private void DrawActions() {
        EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

        using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(formationText))) {
            if (GUILayout.Button("Place Formation")) {
                PlaceFormation();
            }
        }

        if (GUILayout.Button("Clear Text")) {
            formationText = string.Empty;
        }
    }

    #endregion
    #region Placement

    /// <summary>
    /// Converts the formation text into prefab instances.
    /// </summary>
    private void PlaceFormation() {
        var rows = GetRows();
        if (rows.Count == 0) {
            Debug.LogWarning("Formation text is empty.");
            return;
        }

        var spawnedCount = 0;
        var maxColumns = GetMaxColumns(rows);
        var mappingLookup = BuildMappingLookup();

        Undo.IncrementCurrentGroup();
        var undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Place Formation");

        for (var row = 0; row < rows.Count; row++) {
            var line = rows[row];
            for (var column = 0; column < line.Length; column++) {
                var symbol = line[column];
                if (symbol == '.' || char.IsWhiteSpace(symbol)) continue;
                if (!mappingLookup.TryGetValue(symbol, out var prefab) || prefab == null) continue;

                var position = GetWorldPosition(row, column, rows.Count, maxColumns);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                if (instance == null) {
                    instance = Instantiate(prefab);
                }

                Undo.RegisterCreatedObjectUndo(instance, "Place Formation Unit");
                instance.transform.position = position;
                instance.transform.rotation = Quaternion.identity;

                if (parent != null) {
                    Undo.SetTransformParent(instance.transform, parent, "Parent Formation Unit");
                }

                spawnedCount++;
            }
        }

        Undo.CollapseUndoOperations(undoGroup);
        Debug.Log($"Placed {spawnedCount} formation units.");
    }

    /// <summary>
    /// Gets the world position for a formation row and column.
    /// </summary>
    /// <param name="row">The row index.</param>
    /// <param name="column">The column index.</param>
    /// <param name="rowCount">The total row count.</param>
    /// <param name="columnCount">The widest row column count.</param>
    /// <returns>The world position.</returns>
    private Vector3 GetWorldPosition(int row, int column, int rowCount, int columnCount) {
        var x = column * spacing.x;
        var yDirection = topRowIsPositiveY ? -1f : 1f;
        var y = row * spacing.y * yDirection;

        if (centerOnOrigin) {
            x -= (columnCount - 1) * spacing.x * 0.5f;
            y += (rowCount - 1) * spacing.y * 0.5f * (topRowIsPositiveY ? 1f : -1f);
        }

        return new Vector3(origin.x + x, origin.y + y, 0f);
    }

    #endregion
    #region Helpers

    /// <summary>
    /// Ensures the default symbol mappings exist.
    /// </summary>
    private void EnsureDefaultMappings() {
        if (mappings.Count > 0) return;

        ResetDefaultMappings();
    }

    /// <summary>
    /// Restores the default project unit mappings.
    /// </summary>
    private void ResetDefaultMappings() {
        mappings = new List<SymbolPrefabMapping> {
            new() { symbol = "A", prefab = LoadPrefab("Assets/Prefabs/Units/AI_Archers.prefab") },
            new() { symbol = "I", prefab = LoadPrefab("Assets/Prefabs/Units/AI_Infantry.prefab") },
            new() { symbol = "C", prefab = LoadPrefab("Assets/Prefabs/Units/AI_Calvary.prefab") },
            new() { symbol = "P", prefab = LoadPrefab("Assets/Prefabs/Units/PL_Infantry.prefab") }
        };
    }

    /// <summary>
    /// Loads a prefab asset by project path.
    /// </summary>
    /// <param name="path">The project asset path.</param>
    /// <returns>The prefab asset, or null.</returns>
    private static GameObject LoadPrefab(string path) {
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    /// <summary>
    /// Builds a single-character mapping lookup.
    /// </summary>
    /// <returns>A symbol lookup.</returns>
    private Dictionary<char, GameObject> BuildMappingLookup() {
        var lookup = new Dictionary<char, GameObject>();

        foreach (var mapping in mappings) {
            if (mapping == null || string.IsNullOrEmpty(mapping.symbol)) continue;

            lookup[mapping.symbol[0]] = mapping.prefab;
        }

        return lookup;
    }

    /// <summary>
    /// Splits formation text into rows.
    /// </summary>
    /// <returns>Non-empty formation rows.</returns>
    private List<string> GetRows() {
        var rows = new List<string>();
        var splitRows = formationText.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

        foreach (var row in splitRows) {
            if (!string.IsNullOrEmpty(row)) {
                rows.Add(row);
            }
        }

        return rows;
    }

    /// <summary>
    /// Gets the widest row length.
    /// </summary>
    /// <param name="rows">The parsed rows.</param>
    /// <returns>The maximum column count.</returns>
    private static int GetMaxColumns(List<string> rows) {
        var maxColumns = 0;

        foreach (var row in rows) {
            maxColumns = Mathf.Max(maxColumns, row.Length);
        }

        return maxColumns;
    }

    #endregion
}
