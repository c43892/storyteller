using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.UI;

[CustomEditor(typeof(LangSelButton))]
public class SystemLangInspector : ButtonEditor
{
    SerializedProperty lang;
    SerializedProperty selFrame;

    void OnEnable()
    {
        base.OnEnable();
        lang = serializedObject.FindProperty("Language");
        selFrame = serializedObject.FindProperty("SelFrame");
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        serializedObject.Update();
        EditorGUILayout.PropertyField(lang);
        EditorGUILayout.PropertyField(selFrame);
        serializedObject.ApplyModifiedProperties();
    }
}
