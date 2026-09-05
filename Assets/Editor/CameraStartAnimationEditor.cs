using _Scripts.Camera;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CameraStartAnimation))]
public class CameraStartAnimationEditor : Editor {

    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        var startAnimation = (CameraStartAnimation)target;

        EditorGUILayout.Space();
        if (GUILayout.Button("Set Start Position From Current Camera")) {
            Undo.RecordObject(startAnimation, "Set Camera Animation Start Position");
            startAnimation.SetStartPositionToCurrentTransform();
            EditorUtility.SetDirty(startAnimation);
        }

        if (GUILayout.Button("Set End Position From Current Camera")) {
            Undo.RecordObject(startAnimation, "Set Camera Animation End Position");
            startAnimation.SetEndPositionToCurrentTransform();
            EditorUtility.SetDirty(startAnimation);
        }
    }
}
