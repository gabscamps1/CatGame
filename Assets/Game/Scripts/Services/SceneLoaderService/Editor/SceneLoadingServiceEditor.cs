using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CatGame.Services.SceneManagement
{
    [CustomPropertyDrawer(typeof(SceneLoadingService.SceneSettings))]
    public class SceneLoadingServiceEditor : PropertyDrawer
    {
        private readonly string scenesLabel = "Scene";
        private string[] sceneOptionValues;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            RefreshMapOptions();

            EditorGUI.BeginProperty(position, label, property);

            float line = EditorGUIUtility.singleLineHeight;
            float spacing = 2f;

            property.isExpanded = EditorGUI.Foldout
            (
                new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight),
                property.isExpanded,
                label,
                true
            );

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;

                SerializedProperty scenes = property.FindPropertyRelative("scene");
                SerializedProperty isToReloadScene = property.FindPropertyRelative("isToReloadScene");

                Rect r1 = new(position.x, position.y + line + spacing, position.width, line);
                Rect r2 = new(position.x, position.y + (line + spacing) * 2, position.width, line);    

                int currentIndex = Array.IndexOf(sceneOptionValues, scenes.stringValue);

                if (currentIndex < 0)
                    currentIndex = 0;

                // Cria um enum no inspetor.
                int selected = EditorGUI.Popup(r1, scenesLabel, currentIndex, sceneOptionValues);
                scenes.stringValue = sceneOptionValues[selected];

                EditorGUI.PropertyField(r2, isToReloadScene);

                EditorGUI.indentLevel--;
            }
               
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
                return EditorGUIUtility.singleLineHeight;

            return (EditorGUIUtility.singleLineHeight * 3) + 6;
        }

        private void RefreshMapOptions()
        {
            int sceneCount = EditorBuildSettings.scenes.Length;

            sceneOptionValues = new string[sceneCount];

            for (int i = 0; i < sceneCount; i++)
            {
                string sceneName = Path.GetFileNameWithoutExtension(EditorBuildSettings.scenes[i].path);
                sceneOptionValues[i] = sceneName;
            }
        }
    }
}