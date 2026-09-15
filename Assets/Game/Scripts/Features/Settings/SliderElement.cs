using CatGame.Core;
using CatGame.Core.Enums;
using CatGame.Core.Interfaces;
using System;
using TMPro;
using UnityEngine;

namespace CatGame.Capabilities.UISystem
{
    public class SliderElement : NavigableElement
    {
        public class ValueChangedEvent : EventArgs
        {
            public readonly float Value;

            public ValueChangedEvent(float value)
            {
                Value = value;
            }
        }

        public event EventHandler<ValueChangedEvent> OnValueChanged;
        public float Value => value;

        [Header("Slider Settings")]
        [SerializeField] [Min(0)] private float minValue;
        [SerializeField] [Min(0)] private float maxValue;
        [SerializeField] [Min(0)] private float value;
        [SerializeField] private float changeValue;
        
        [Header("Slider UI")]
        [SerializeField] private TextMeshProUGUI valueText;

        private void IncressValue()
        {
            value += changeValue;
            value = Mathf.Clamp(minValue, maxValue, value);
            OnValueChanged?.Invoke(this, new ValueChangedEvent(value));
        }

        private void DecreaseValue()
        {
            value -= changeValue;
            value = Mathf.Clamp(minValue, maxValue, value);
            OnValueChanged?.Invoke(this, new ValueChangedEvent(value));
        }

        protected override void OnFocusedByPlayer(PlayerId player, NavigationMode navigationMode)
        {
            base.OnFocusedByPlayer(player, navigationMode);

            IPlayerInputController inputController = ServiceLocator.Get<IInputService>().GetInputFromPlayer(player);
            inputController.Navigation.performed += Navigation_performed;
        }

        protected override void OnUnfocusedByPlayer(PlayerId player, NavigationMode navigationMode)
        {
            base.OnUnfocusedByPlayer(player, navigationMode);

            IPlayerInputController inputController = ServiceLocator.Get<IInputService>().GetInputFromPlayer(player);
            inputController.Navigation.performed -= Navigation_performed;
        }

        private void Navigation_performed(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            Vector2 navigationValue = context.ReadValue<Vector2>();

            switch (navigationValue.x)
            {
                case 1: IncressValue(); break;
                case -1: DecreaseValue(); break;
            }
        }

        private void UpdateUI()
        {
            valueText.text = value.ToString();
        }
    }
}