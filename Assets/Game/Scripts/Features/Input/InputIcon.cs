using UnityEngine;
using UnityEngine.InputSystem;
using System;
using UnityEngine.UI;

namespace CatGame.Features.InputManagement
{
    public class InputIcon : MonoBehaviour
    {
        [SerializeField]
        private Image inputIconImage;

        [SerializeField]
        private InputActionReference actionReference;

        [SerializeField, Tooltip("Setas e d-pad: mostra a direção sozinha ou o grupo (vertical, horizontal ou as 4).")]
        private InputIconVariant variant;

        [SerializeField]
        private string keyboardBindingID;
        private int keyboardBindingIndex;

        [SerializeField]
        private string gamepadBindingID;
        private int gamepadBindingIndex;

        private void Start()
        {
            if (actionReference == null) return;

            Guid keyboardBindingId = new(keyboardBindingID);
            keyboardBindingIndex = actionReference.action.bindings.IndexOf(x => x.id == keyboardBindingId);

            Guid gamepadBindingId = new(gamepadBindingID);
            gamepadBindingIndex = actionReference.action.bindings.IndexOf(x => x.id == gamepadBindingId);

            InputIconsManager.Instance.OnIconsChanged += UpdateIcon;
            UpdateIcon(InputIconsManager.Instance.CurrentDevice);
        }

        private void OnDestroy()
        {
            if (InputIconsManager.Instance)
                InputIconsManager.Instance.OnIconsChanged -= UpdateIcon;
        }

        private void UpdateIcon(InputDevice currentDevice)
        {
            switch (currentDevice)
            {
                case Keyboard:
                    inputIconImage.sprite = InputIconsManager.Instance.GetIcon(actionReference.action.bindings[keyboardBindingIndex].effectivePath, variant);
                    break;

                case Gamepad:
                    inputIconImage.sprite = InputIconsManager.Instance.GetIcon(actionReference.action.bindings[gamepadBindingIndex].effectivePath, variant);
                    break;

                default:
                    inputIconImage.sprite = InputIconsManager.Instance.GetIcon(actionReference.action.bindings[keyboardBindingIndex].effectivePath, variant);
                    break;
            }
        }
    }
}
