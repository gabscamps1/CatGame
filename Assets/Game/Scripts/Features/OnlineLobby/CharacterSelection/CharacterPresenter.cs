using CatGame.Core;
using CatGame.Core.Data;
using CatGame.Core.Interfaces;
using System;

namespace CatGame.Features.CharacterSelection
{
    public class CharacterPresenter
    {
        private readonly CharacterModel characterModel;
        private readonly CharacterView characterView;

        public event Action<PlayerId, CharacterModel> OnCharacterSelected;
        public event Action<PlayerId, CharacterModel> OnCharacterFocused;

        public CharacterPresenter(CharacterModel characterModel, CharacterView characterView)
        {
            this.characterModel = characterModel;
            this.characterView = characterView;

            characterView.SetCharacterSprite(characterModel.CharacterSprite);

            characterView.OnSelectButtonClicked += CharacterView_OnSelectButtonClicked;
            characterView.OnSelectButtonFocused += CharacterView_OnSelectButtonFocussed;
        }   

        ~CharacterPresenter()
        {
            characterView.OnSelectButtonClicked -= CharacterView_OnSelectButtonClicked;
        }

        private void CharacterView_OnSelectButtonClicked(PlayerId playerId)
        {
            // Prende a navegação do jogador.
            ServiceLocator.Get<IUINavigationService>().SetNavigationLocked(playerId, true); 
            ServiceLocator.Get<IUINavigationService>().SetCancelLocked(playerId, true);

            OnCharacterSelected?.Invoke(playerId, characterModel);
        }

        private void CharacterView_OnSelectButtonFocussed(PlayerId playerId)
        {
            OnCharacterFocused?.Invoke(playerId, characterModel);
        }
    }
}