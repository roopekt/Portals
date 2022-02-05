using UnityEngine;
using UnityEditor;
using System;
using System.Linq;

[CustomEditor(typeof(Teleporter))]
public class TeleporterEditor : Editor
{
    SerializedProperty ghost_compMask;
    SerializedProperty copyChilds;
    SerializedProperty ghost_compIDs;

    private void OnEnable()
    {
        ghost_compMask = serializedObject.FindProperty("ghost_compMask");
        copyChilds = serializedObject.FindProperty("copyChilds");
        ghost_compIDs = serializedObject.FindProperty("ghost_compIDs");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        Component[] components = (target as Teleporter).transform.GetComponents<Component>();

        //make sure, that ghost_compMask is big enough
        if (ghost_compMask.arraySize < components.Length) { ghost_compMask.arraySize = components.Length; }

        //get previous values
        bool[] previousValues = new bool[components.Length];
        for (int i = 0; i < components.Length; i++)
        {
            if (ghost_compIDs.arraySize == 0)//safe mode
            {
                previousValues[i] = ghost_compMask.GetArrayElementAtIndex(i).boolValue;
            }
            else//normal case
            {
                int index;// Array.IndexOf(teleporter.ghost_compIDs, components[i].GetInstanceID());
                for (index = 0; index < ghost_compIDs.arraySize; index++) {
                    if (ghost_compIDs.GetArrayElementAtIndex(index).intValue == components[i].GetInstanceID()) { break; }
                }

                if (index == ghost_compIDs.arraySize)//if component was not found
                {
                    previousValues[i] = Teleporter.isDefault(components[i].GetType());
                }
                else { previousValues[i] = ghost_compMask.GetArrayElementAtIndex(index).boolValue; }
            }
        }

        EditorGUILayout.LabelField("components to copy to ghost", EditorStyles.boldLabel);
        for (int i = 0; i < components.Length; i++)
        {
            if (new Type[] { typeof(Transform), typeof(Teleporter) }.Contains(components[i].GetType()))//if component is Transform or Teleporter
            {
                ghost_compMask.GetArrayElementAtIndex(i).boolValue = false;
            }
            else
            {
                //get name of component as string
                string label = components[i].GetType().ToString();
                label = label.Remove(0, label.LastIndexOf('.') + 1);

                //display toggle field
                ghost_compMask.GetArrayElementAtIndex(i).boolValue = EditorGUILayout.Toggle(label, previousValues[i]);
            }
        }

        EditorGUILayout.Space();
        copyChilds.boolValue = EditorGUILayout.Toggle("Childs", copyChilds.boolValue);

        //trim ghost_compMask to right length
        if (ghost_compMask.arraySize != components.Length) { ghost_compMask.arraySize = components.Length; }

        //update compIDs
        ghost_compIDs.arraySize = components.Length;
        for (int i = 0; i < components.Length; i++) { ghost_compIDs.GetArrayElementAtIndex(i).intValue = components[i].GetInstanceID(); }

        serializedObject.ApplyModifiedProperties();
    }
}