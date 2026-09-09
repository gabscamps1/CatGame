using System;
using CatGame.Core;
using CatGame.Core.Enums;
using CatGame.Core.Interfaces;
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

        public class FocussedEvent : EventArgs 
        {
            public readonly PlayerId PlayerId;

            public FocussedEvent(PlayerId playerId)
            {
                PlayerId = playerId;
            }
        }

        [Header("Button Settings")]
        [SerializeField] private Color baseColor = Color.white;
        [SerializeField] private Color selectedColor = Color.lightGray;
        [SerializeField] private Color pressedColor = Color.gray;
        [SerializeField] private Color disableColor = Color.darkGray;

        [SerializeField] private Core.Data.AudioData onClickSound;
        [SerializeField] private Core.Data.AudioData onSelectSound;

        private Image image;

        public event EventHandler<SubmittedEvent> OnSubmittedEvent; 
        public event EventHandler<FocussedEvent> OnFocusedEvent; 

        private IAudioService audioService;

        private void Awake()
        {
            image = GetComponent<Image>();
        }

        private void Start()
        {
            audioService = ServiceLocator.Get<IAudioService>();
        }

        protected override void OnSubmitByPlayer(PlayerId player)
        {
            OnSubmittedEvent?.Invoke(this, new SubmittedEvent(player));

            if (onClickSound != null)
                audioService.PlaySFXClip(onClickSound);
        }

        protected override void OnFocusedByPlayer(PlayerId player, NavigationMode navigationMode)
        {
            image.color = selectedColor;
            OnFocusedEvent?.Invoke(this, new FocussedEvent(player));
            base.OnFocusedByPlayer(player, navigationMode);

            if (onSelectSound != null)
                audioService.PlaySFXClip(onSelectSound);
        }

        protected override void OnUnfocusedByPlayer(PlayerId player, NavigationMode navigationMode)
        {
            image.color = baseColor;
            base.OnUnfocusedByPlayer(player, navigationMode);
        }
    }
}