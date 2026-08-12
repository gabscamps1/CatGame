using UnityEditor;
using UnityEngine;

namespace CatGame.Capabilities.UISystem
{
    [CustomPropertyDrawer(typeof(GroupSelectable))]
    public class MenuController_GroupSelectableEditor : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            Rect fieldRect = new(position.x, position.y + EditorGUIUtility.singleLineHeight - 20, position.width, EditorGUIUtility.singleLineHeight);

            // Opção de expandir para o GroupSelectable.
            property.isExpanded = EditorGUI.Foldout(fieldRect, property.isExpanded, label, true);

            // Caso esteja expandido.
            if (property.isExpanded)
            {
                // Deixa uma margem nos elementos em relação ao label.
                EditorGUI.indentLevel++;

                // Pega cada elemento do GroupSelectable.
                var elementsProp = property.FindPropertyRelative("element");
                var callFunctionProp = property.FindPropertyRelative("function");
                var numberOfMenuProp = property.FindPropertyRelative("numberOfMenu");
                var menuControllerToLoad = property.FindPropertyRelative("newMenuController");

                // Deixa visivel o elemento.
                EditorGUILayout.PropertyField(elementsProp);

                // Torna visivel as funções que serão chamadas, caso o elemento seja um botão.
                if (elementsProp.objectReferenceValue != null && elementsProp.objectReferenceValue is ButtonElement)
                    EditorGUILayout.PropertyField(callFunctionProp);
                else
                    callFunctionProp.enumValueIndex = 0;

                switch ((GroupSelectable.CallFunction)callFunctionProp.enumValueIndex)
                {
                    // Torna visivel a váriavel int, caso a função precisa de um index específico.
                    case GroupSelectable.CallFunction.ChangeMenu:
                        EditorGUILayout.PropertyField(numberOfMenuProp);
                        break;

                    // Torna visivel a váriavel MenuController, caso a função precisa de um MenuController específico.
                    case GroupSelectable.CallFunction.ChangeMenuController:
                        EditorGUILayout.PropertyField(menuControllerToLoad);
                        break;

                    default:
                        break;
                }             

                // Finaliza a margem no último elemento do GroupSelectable.
                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }
    }
}