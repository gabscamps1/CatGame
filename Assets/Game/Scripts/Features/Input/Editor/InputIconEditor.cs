using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CatGame.Features.InputManagement
{
    [CustomEditor(typeof(InputIcon))]
    public class InputIconEditor : Editor
    {
        private SerializedProperty actionReference;
        private SerializedProperty keyboardBindingID;
        private SerializedProperty gamepadBindingID;


        private GUIContent keyboardBindingLabel = new("Keyboard Binding");
        private GUIContent gamepadBindingLabel = new("Gamepad Binding");
        private GUIContent[] bindingOptions;
        private string[] bindingOptionValues;
        private int keyboardSelectedBindingOption;
        private int gamepadSelectedBindingOption;

        protected void OnEnable()
        {
            actionReference = serializedObject.FindProperty("actionReference");
            keyboardBindingID = serializedObject.FindProperty("keyboardBindingID");
            gamepadBindingID = serializedObject.FindProperty("gamepadBindingID");

            RefreshBindingOptions(keyboardBindingID, ref keyboardSelectedBindingOption);
            RefreshBindingOptions(gamepadBindingID, ref gamepadSelectedBindingOption);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();

            // Desenha todos os campos serializados, menos os IDs de bind (que viram dropdown abaixo).
            DrawPropertiesExcluding(serializedObject, "m_Script", "keyboardBindingID", "gamepadBindingID");

            var newKeyboardSelectedBinding = EditorGUILayout.Popup(keyboardBindingLabel, keyboardSelectedBindingOption, bindingOptions);
            if (newKeyboardSelectedBinding != keyboardSelectedBindingOption)
            {
                var bindingId = bindingOptionValues[newKeyboardSelectedBinding];
                keyboardBindingID.stringValue = bindingId;
                keyboardSelectedBindingOption = newKeyboardSelectedBinding;
            }

            var newGamepadSelectedBinding = EditorGUILayout.Popup(gamepadBindingLabel, gamepadSelectedBindingOption, bindingOptions);
            if (newGamepadSelectedBinding != gamepadSelectedBindingOption)
            {
                var bindingId = bindingOptionValues[newGamepadSelectedBinding];
                gamepadBindingID.stringValue = bindingId;
                gamepadSelectedBindingOption = newGamepadSelectedBinding;
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                RefreshBindingOptions(keyboardBindingID, ref keyboardSelectedBindingOption);
                RefreshBindingOptions(gamepadBindingID, ref gamepadSelectedBindingOption);
            }
        }

        protected void RefreshBindingOptions(SerializedProperty bindingID, ref int selectedBindingOption)
        {
            var actionReference = (InputActionReference)this.actionReference.objectReferenceValue;
            var action = actionReference?.action;

            if (action == null)
            {
                bindingOptions = new GUIContent[0];
                bindingOptionValues = new string[0];
                selectedBindingOption = -1;
                return;
            }

            var bindings = action.bindings;
            var bindingCount = bindings.Count;

            bindingOptions = new GUIContent[bindingCount];
            bindingOptionValues = new string[bindingCount];
            selectedBindingOption = -1;

            var currentBindingId = bindingID.stringValue;
            for (var i = 0; i < bindingCount; ++i)
            {
                var binding = bindings[i];
                var bindingId = binding.id.ToString();
                var haveBindingGroups = !string.IsNullOrEmpty(binding.groups);

                // If we don't have a binding groups (control schemes), show the device that if there are, for example,
                // there are two bindings with the display string "A", the user can see that one is for the keyboard
                // and the other for the gamepad.
                var displayOptions =
                    InputBinding.DisplayStringOptions.DontUseShortDisplayNames | InputBinding.DisplayStringOptions.IgnoreBindingOverrides;
                if (!haveBindingGroups)
                    displayOptions |= InputBinding.DisplayStringOptions.DontOmitDevice;

                // Create display string.
                var displayString = action.GetBindingDisplayString(i, displayOptions);

                // If binding is part of a composite, include the part name.
                if (binding.isPartOfComposite)
                    displayString = $"{ObjectNames.NicifyVariableName(binding.name)}: {displayString}";

                // Some composites use '/' as a separator. When used in popup, this will lead to to submenus. Prevent
                // by instead using a backlash.
                displayString = displayString.Replace('/', '\\');

                // If the binding is part of control schemes, mention them.
                if (haveBindingGroups)
                {
                    var asset = action.actionMap?.asset;
                    if (asset != null)
                    {
                        var controlSchemes = string.Join(", ",
                            binding.groups.Split(InputBinding.Separator)
                                .Select(x => asset.controlSchemes.FirstOrDefault(c => c.bindingGroup == x).name));

                        displayString = $"{displayString} ({controlSchemes.Replace(", ","")})";
                    }
                }

                bindingOptions[i] = new GUIContent(displayString);
                bindingOptionValues[i] = bindingId;

                if (currentBindingId == bindingId)
                    selectedBindingOption = i;
            }
        }
    }
}

