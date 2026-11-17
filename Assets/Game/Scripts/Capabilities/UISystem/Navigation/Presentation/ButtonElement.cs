using CatGame.Core;
using CatGame.Core.Data;
using CatGame.Core.Enums;
using CatGame.Core.Interfaces;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace CatGame.Capabilities.UISystem
{
    [RequireComponent(typeof(Image))]
    public class ButtonElement : NavigableElement
    {
        public class SubmittedEvent : EventArgs
        {
            public readonly PlayerId PlayerId;

            public SubmittedEvent(PlayerId playerId)
            {
                PlayerId = playerId;
            }
        }

        public event EventHandler<SubmittedEvent> OnSubmittedEvent;

        [Header("Button Settings")]
        [SerializeField] private Color baseColor = Color.white;
        [SerializeField] private Color selectedColor = Color.white;
        [SerializeField] private Color pressedColor = Color.gray;
        [SerializeField] private Color disableColor = Color.darkGray;

        [Header("Button Sounds")]
        [SerializeField] private AudioData onClickSound;

        private Image image;

        private IAudioService audioService;

        protected override void Awake()
        {
            base.Awake();
            image = GetComponent<Image>();
        }

        private void Start()
        {
            audioService = ServiceLocator.Get<IAudioService>();
        }

        protected override void OnSubmitByPlayer(PlayerId player)
        {
            if (!IsInteractable) return;
            audioService?.PlaySFXClip(onClickSound);
            OnSubmittedEvent?.Invoke(this, new SubmittedEvent(player));
        }

        protected override void OnFocusedByPlayer(PlayerId player, NavigationMode navigationMode)
        {
            if (image) image.color = selectedColor;
            base.OnFocusedByPlayer(player, navigationMode);
        }

        protected override void OnUnfocusedByPlayer(PlayerId player, NavigationMode navigationMode)
        {
            if (image) image.color = baseColor;
            base.OnUnfocusedByPlayer(player, navigationMode);
        }
    }
}