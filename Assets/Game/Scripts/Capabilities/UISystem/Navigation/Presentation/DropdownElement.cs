using CatGame.Core;
using CatGame.Core.Enums;
using CatGame.Core.Interfaces;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace CatGame.Capabilities.UISystem
{
    public class DropdownElement : NavigableElement
    {
        public class ValueChangedEvent : EventArgs
        {
            public readonly int Index;

            public ValueChangedEvent(int index)
            {
                Index = index;
            }
        }

        public int Value => selectedValue;
        public event EventHandler<ValueChangedEvent> OnValueChanged;

        [Header("Dropdown Settings")]
        [SerializeField] private TextMeshProUGUI valueText;

        private readonly List<string> valueList = new();
        private int selectedValue;

        private void Awake()
        {
            TryChangeValue(selectedValue);
        }

        public void AddOptions(params string[] value)
        {
            foreach (var item in value)
                valueList.Add(item);
        }

        public void RemoveOptions(string value)
        {
            valueList.Remove(value);
        }

        public void ClearOptions()
        {
            valueList.Clear();
        }

        public void NextOption()
        {
            RemoveNullValues();

            int goingValue = (selectedValue + 1) % valueList.Count;

            if (!TryChangeValue(goingValue))
                return;

            OnValueChanged?.Invoke(this, new ValueChangedEvent(goingValue));
        }

        public void PreviousOption()
        {
            RemoveNullValues();

            int goingValue = --selectedValue == -1
                ? valueList.Count - 1
                : selectedValue;

            if (!TryChangeValue(goingValue))
                return;

            OnValueChanged?.Invoke(this, new ValueChangedEvent(goingValue));
        }

        public void SetValue(int newValuevalue)
        {
            TryChangeValue(newValuevalue);
        }

        private bool TryChangeValue(int newValue)
        {
            if (valueList == null || valueList.Count == 0)
                return false;

            selectedValue = newValue;
            valueText.text = valueList[selectedValue];
            return true;
        }

        private void RemoveNullValues()
        {
            if (valueList == null || valueList.Count == 0)
                return;

            valueList.RemoveAll(x => x == null);
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
                case 1: NextOption(); break;
                case -1: PreviousOption(); break;
            }
        }
    }
}