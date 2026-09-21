using CatGame.Core;
using CatGame.Core.Enums;
using CatGame.Core.Data;
using CatGame.Core.Interfaces;
using UnityEngine;
using UnityEngine.UI;


namespace CatGame.Capabilities.UISystem
{
    [RequireComponent(typeof(Image))]
    public class ButtonElement : NavigableElement
    {
        [Header("Button Settings")]
        [SerializeField] private Color baseColor = Color.white;
        [SerializeField] private Color selectedColor = Color.lightGray;
        [SerializeField] private Color pressedColor = Color.gray;
        [SerializeField] private Color disableColor = Color.darkGray;

        [SerializeField] private AudioData onClickSound;
        [SerializeField] private AudioData onSelectSound;

        private Image image;

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
            if (onClickSound != null)
                audioService.PlaySFXClip(onClickSound);
        }

        protected override void OnFocusedByPlayer(PlayerId player, NavigationMode navigationMode)
        {
            image.color = selectedColor;
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