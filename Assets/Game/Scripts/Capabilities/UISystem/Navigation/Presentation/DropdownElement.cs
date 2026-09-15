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

        public event EventHandler<ValueChangedEvent> ValueChanged;

        [SerializeField] private TextMeshProUGUI valueText;

        private readonly List<string> valueList = new();
        private int selectedValue;

        public void AddElement(string value)
        {
            valueList.Add(value);
        }

        public void RemoveElement(string value)
        {
            valueList.Remove(value);
        }

        public void NextElement()
        {
            RemoveNullValues();

            int goingValue = (selectedValue + 1) % valueList.Count;

            if (!TryChangeValue(goingValue))
                return;

            ValueChanged?.Invoke(this, new ValueChangedEvent(goingValue));
        }

        public void PreviousElement()
        {
            RemoveNullValues();

            int goingValue = --selectedValue == -1
                ? valueList.Count - 1
                : selectedValue;

            if (!TryChangeValue(goingValue))
                return;

            ValueChanged?.Invoke(this, new ValueChangedEvent(goingValue));
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
    }
}