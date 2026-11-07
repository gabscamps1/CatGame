using CatGame.Core;
using CatGame.Core.Enums;
using CatGame.Core.Data;
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
        public float MaxValue => maxValue;
        public float MinValue => minValue;
        public float Value => value;

        [Header("Slider Settings")]
        [SerializeField] [Min(0)] private float minValue;
        [SerializeField] [Min(0)] private float maxValue;
        [SerializeField] [Min(0)] private float value;
        [SerializeField] private float changeValue;
        
        [Header("Slider UI")]
        [SerializeField] private TextMeshProUGUI valueText;

        [Header("Repeat OnHold Settings")]
        [SerializeField] private float initialDelay = 0.4f;
        [SerializeField] private float repeatInterval = 0.12f;

        [Header("Slider Sounds")]
        [SerializeField] private AudioData onValueChangedSound;

        private IAudioService audioService;

        private NavigationDirection? heldDirection;
        private float nextRepeatTime;

        private IInputService inputService;

        protected override void Awake()
        {
            base.Awake();
            UpdateUI();
        }

        private void Start()
        {
            audioService = ServiceLocator.Get<IAudioService>();
            inputService = ServiceLocator.Get<IInputService>();
        }

        private void Update()
        {
            HandleDirectionalInput();           
        }

        private void HandleDirectionalInput()
        {
            foreach (PlayerId playerId in FocusingPlayerList)
            {
                if (!inputService.GetInputFromPlayer(playerId).Navigation.IsPressed())
                    continue;

                NavigationDirection? direction = ReadDirection(playerId);

                if (direction.HasValue && (direction.Value == NavigationDirection.Up || direction.Value == NavigationDirection.Down))
                    return;

                if (direction.HasValue && direction != heldDirection)
                {
                    heldDirection = direction;
                    nextRepeatTime = Time.unscaledTime + initialDelay;
                    Navigation(direction.Value);
                    return;
                }

                if (!direction.HasValue)
                {
                    heldDirection = null;
                    return;
                }

                if (Time.unscaledTime >= nextRepeatTime)
                {
                    nextRepeatTime = Time.unscaledTime + repeatInterval;
                    Navigation(direction.Value);
                }

                return; // Somente um player mexe por frame.
            }

            heldDirection = null;
        }

        private NavigationDirection? ReadDirection(PlayerId playerId)
        {
            Vector2 navigateDirection = inputService.GetInputFromPlayer(playerId).Navigation.ReadValue<Vector2>();

            float horizontal = navigateDirection.x;
            float vertical = navigateDirection.y;

            if (vertical > 0.5f) return NavigationDirection.Up;
            if (vertical < -0.5f) return NavigationDirection.Down;
            if (horizontal < -0.5f) return NavigationDirection.Left;
            if (horizontal > 0.5f) return NavigationDirection.Right;

            return null;
        }

        private void IncressValue()
        {
            if (value == maxValue) return;
            value += changeValue;
            value = Mathf.Clamp(value, minValue, maxValue);
            OnValueChanged?.Invoke(this, new ValueChangedEvent(value));
            UpdateUI();

            audioService?.PlaySFXClip(onValueChangedSound, false);
        }

        private void DecreaseValue()
        {
            if (value == minValue) return;
            value -= changeValue;
            value = Mathf.Clamp(value, minValue, maxValue);
            OnValueChanged?.Invoke(this, new ValueChangedEvent(value));
            UpdateUI();

            audioService?.PlaySFXClip(onValueChangedSound, false);
        }

        public void SetValue(float amount)
        {
            value = amount;
            value = Mathf.Clamp(value, minValue, maxValue);
            OnValueChanged?.Invoke(this, new ValueChangedEvent(value));
            UpdateUI();
        }

        private void Navigation(NavigationDirection direction)
        {
            switch (direction)
            {
                case NavigationDirection.Right: IncressValue(); break;
                case NavigationDirection.Left: DecreaseValue(); break;
            }
        }

        private void UpdateUI()
        {
            valueText.text = $"<{value}>";
        }
    }
}