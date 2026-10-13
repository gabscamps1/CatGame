using CatGame.Capabilities.UISystem;
using CatGame.Core.Data;
using UnityEngine.UI;
using System;
using UnityEngine;

namespace CatGame.Features.CharacterSelection
{
    public class CharacterView : MonoBehaviour
    {
        [SerializeField] private ButtonElement selectCharacterButton;
        [SerializeField] private Image characterImage;

        public event Action<PlayerId> OnSelectButtonClicked;
        public event Action<PlayerId> OnSelectButtonFocused;

        private void Awake()
        {
            selectCharacterButton.OnSubmittedEvent += SelectCharacterButton_OnSubmitted;
            selectCharacterButton.OnFocusedEvent += SelectCharacterButton_OnFocussed;
        }     

        private void OnDestroy()
        {
            selectCharacterButton.OnSubmittedEvent -= SelectCharacterButton_OnSubmitted;
            selectCharacterButton.OnFocusedEvent -= SelectCharacterButton_OnFocussed;
        }

        private void SelectCharacterButton_OnSubmitted(object sender, ButtonElement.SubmittedEvent e)
        {
            OnSelectButtonClicked?.Invoke(e.PlayerId);
        }

        private void SelectCharacterButton_OnFocussed(object sender, ButtonElement.FocussedEvent e)
        {
            OnSelectButtonFocused?.Invoke(e.PlayerId);
        }

        public void SetCharacterSprite(Sprite characterSprite)
        {
            characterImage.sprite = characterSprite;
            characterImage.enabled = characterSprite != null;
        }
    }
}