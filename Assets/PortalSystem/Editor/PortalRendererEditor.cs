using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PortalRenderer))]
[CanEditMultipleObjects]
public class PortalRendererEditor : Editor
{
    SerializedProperty portalCamera;
    SerializedProperty maxRenderCount;
    SerializedProperty portalCamFarClipDist;
    SerializedProperty rendTexMaxRes;
    SerializedProperty portals;
    SerializedProperty autosearchPortals;
    SerializedProperty portalTag;
    SerializedProperty debug_visualizeClipping;

    private void OnEnable()
    {
        portalCamera = serializedObject.FindProperty("portalCamera");
        maxRenderCount = serializedObject.FindProperty("maxRenderCount");
        portalCamFarClipDist = serializedObject.FindProperty("portalCamFarClipDist");
        rendTexMaxRes = serializedObject.FindProperty("rendTexMaxRes");
        portals = serializedObject.FindProperty("portals");
        autosearchPortals = serializedObject.FindProperty("autosearchPortals");
        portalTag = serializedObject.FindProperty("portalTag");
        debug_visualizeClipping = serializedObject.FindProperty("debug_visualizeClipping");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        //camera
        EditorGUILayout.LabelField("Camera", EditorStyles.boldLabel);
        portalCamera.objectReferenceValue = EditorGUILayout.ObjectField("portal camera", portalCamera.objectReferenceValue, typeof(Camera), true);
        portalCamFarClipDist.floatValue = EditorGUILayout.FloatField("far plane", portalCamFarClipDist.floatValue);
        maxRenderCount.intValue = EditorGUILayout.IntField("max render count", maxRenderCount.intValue);

        //textures
        EditorGUILayout.LabelField("Textures", EditorStyles.boldLabel);
        rendTexMaxRes.vector2IntValue = EditorGUILayout.Vector2IntField("max resolution", rendTexMaxRes.vector2IntValue);

        //portals
        EditorGUILayout.LabelField("Portals", EditorStyles.boldLabel);
        autosearchPortals.boolValue = EditorGUILayout.Toggle("auto search portals", autosearchPortals.boolValue);
        if (autosearchPortals.boolValue)
            portalTag.stringValue = EditorGUILayout.TextField("tag", portalTag.stringValue);
        else
            EditorGUILayout.PropertyField(portals, new GUIContent("portals"), true);

        //debug
        EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
        debug_visualizeClipping.boolValue = EditorGUILayout.Toggle("visualize clipping", debug_visualizeClipping.boolValue);

        serializedObject.ApplyModifiedProperties();
    }
}