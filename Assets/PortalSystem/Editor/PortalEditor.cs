using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Portal))]
public class PortalEditor : Editor
{
    SerializedProperty otherPortal;

    private void OnEnable() =>
        otherPortal = serializedObject.FindProperty("otherPortal");

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        otherPortal.objectReferenceValue = EditorGUILayout.ObjectField("other portal", otherPortal.objectReferenceValue, typeof(GameObject), true);
        serializedObject.ApplyModifiedProperties();
    }
}
