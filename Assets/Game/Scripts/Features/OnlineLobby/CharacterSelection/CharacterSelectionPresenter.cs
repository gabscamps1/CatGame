using CatGame.Core;
using CatGame.Core.Enums;
using CatGame.Core.Data;
using CatGame.Core.Interfaces;
using System.Collections.Generic;

namespace CatGame.Features.CharacterSelection
{
    public class CharacterSelectionPresenter
    {
        private readonly CharacterSelectionView characterSelectionView;
        private readonly CharacterSelectionModel characterSelectionModel;
        
        private readonly List<CharacterPresenter> characterPresenters = new();

        public CharacterSelectionPresenter(
            CharacterSelectionModel characterSelectionModel,
            CharacterSelectionView characterSelectionView,
            CharacterData[] characterDatas,
            CharacterView[] characterViews)
        {
            this.characterSelectionModel = characterSelectionModel;
            this.characterSelectionView = characterSelectionView;
            characterSelectionView.OnActivatedView += CharacterSelectionView_OnActivatedView;
            characterSelectionView.OnDesactivatedView += CharacterSelectionView_OnDesactivatedView;

            if (characterDatas.Length != characterViews.Length)
            {
                Core.Logger.LogError("Quantidade diferente. Garanta que a quantidade de datas e views sejam a mesma");
                return;
            }

            for (int i = 0; i < characterDatas.Length; i++)
            {
                CharacterData characterData = characterDatas[i];

                CharacterModel characterModel = new CharacterModel(
                    characterData.CharacterID,
                    characterData.CharacterName,
                    characterData.CharacterSprite);

                CharacterView characterView = characterViews[i];

                CharacterPresenter characterPresenter = new(characterModel, characterView);
                characterPresenter.OnCharacterSelected += CharacterPresenter_OnCharacterSelected;
                characterPresenter.OnCharacterFocused += CharacterPresenter_OnCharacterFocused;
                characterPresenters.Add(characterPresenter);
            }
        }

        ~CharacterSelectionPresenter()
        {
            foreach (CharacterPresenter characterPresenter in characterPresenters)
            {
                characterPresenter.OnCharacterSelected -= CharacterPresenter_OnCharacterSelected;
                characterPresenter.OnCharacterFocused -= CharacterPresenter_OnCharacterFocused;
            }

            characterSelectionView.OnActivatedView -= CharacterSelectionView_OnActivatedView;
            characterSelectionView.OnDesactivatedView -= CharacterSelectionView_OnDesactivatedView;
        }

        private void CharacterSelectionView_OnActivatedView()
        {
            ServiceLocator.Get<IInputService>().GetInputFromPlayer(new PlayerId(0)).OnCancelled += PlayerInputController_OnCancelled;
            ServiceLocator.Get<IInputService>().GetInputFromPlayer(new PlayerId(1)).OnCancelled += PlayerInputController_OnCancelled;
        }


        private void CharacterSelectionView_OnDesactivatedView()
        {
            ServiceLocator.Get<IInputService>().GetInputFromPlayer(new PlayerId(0)).OnCancelled -= PlayerInputController_OnCancelled;
            ServiceLocator.Get<IInputService>().GetInputFromPlayer(new PlayerId(1)).OnCancelled -= PlayerInputController_OnCancelled;

            ServiceLocator.Get<IUINavigationService>().SetNavigationLocked(new PlayerId(0), false);
            ServiceLocator.Get<IUINavigationService>().SetCancelLocked(new PlayerId(0), false);

            ServiceLocator.Get<IUINavigationService>().SetNavigationLocked(new PlayerId(1), false);
            ServiceLocator.Get<IUINavigationService>().SetCancelLocked(new PlayerId(1), false);

            // Garante que se estiver no loading de cena (ou seja se os personagens já foram selecionados), o UnassignCharacterToPlayer() não vai ser chamado.
            if (!characterSelectionModel.IsAllCharactersSelected())
            {
                characterSelectionModel.UnassignCharacterToPlayer(new PlayerId(0));
                characterSelectionModel.UnassignCharacterToPlayer(new PlayerId(1));
            }
        }

        private void PlayerInputController_OnCancelled()
        {
            /*characterSelectionModel.UnassignCharacterToPlayer(playerId);

            ServiceLocator.Get<IUINavigationService>().SetNavigationLocked(playerId, false);
            ServiceLocator.Get<IUINavigationService>().SetCancelLocked(playerId, false);*/
        }

        private void CharacterPresenter_OnCharacterFocused(PlayerId player, CharacterModel characterModel)
        {
            characterSelectionModel.ShowCharacter(player, characterModel);
            characterSelectionView.ShowCharacterName(player, characterModel.CharacterName);
        }

        private void CharacterPresenter_OnCharacterSelected(PlayerId player, CharacterModel characterModel)
        {     
            characterSelectionModel.AssignCharacterToPlayer(player, characterModel);

            if (characterSelectionModel.IsAllCharactersSelected())
                ServiceLocator.Get<ISceneLoadingService>().LoadAsyncScene(Scenes.Playing);
        }        
    }
}