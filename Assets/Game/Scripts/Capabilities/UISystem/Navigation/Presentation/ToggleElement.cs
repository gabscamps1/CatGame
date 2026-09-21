using CatGame.Core.Data;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace CatGame.Capabilities.UISystem
{
    public class ToggleElement : NavigableElement
    {
        public class ValueChangedEvent : EventArgs
        {
            public readonly bool IsOn;

            public ValueChangedEvent(bool isOn)
            {
                IsOn = isOn;
            }
        }
        public bool IsOn => isOn;
        public event EventHandler<ValueChangedEvent> OnValueChanged;

        [Header("Toggle Settings")]
        [SerializeField] private bool isOn;
        
        [Header("Toggle UI")]
        [SerializeField] private Image graphic;

        private void Awake()
        {
            UpdateUI();
        }

        public void SetActivate(bool active)
        {
            isOn = active;
            OnValueChanged?.Invoke(this, new ValueChangedEvent(isOn));
            
            UpdateUI();
        }

        public void SwitchState()
        {
            isOn = !IsOn;
            OnValueChanged?.Invoke(this, new ValueChangedEvent(isOn));

            UpdateUI();
        }

        private void UpdateUI()
        {
            if (graphic) graphic.enabled = isOn;
        }

        protected override void OnSubmitByPlayer(PlayerId player)
        {
            SwitchState();
        }
    }
}