using UnityEngine;
using UnityEditor;
using Recognissimo.Components;

namespace Recognissimo.Editor
{
    [CustomEditor(typeof(LanguageModelProvider))]
    public class LanguagesModelProviderEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var script = target as LanguageModelProvider;
            if (script == null) return;
            
            if (script.setupOnAwake)
            {
                script.defaultLanguage =
                    (SystemLanguage) EditorGUILayout.EnumPopup("Default Language", script.defaultLanguage);
            }
        }
    }
}