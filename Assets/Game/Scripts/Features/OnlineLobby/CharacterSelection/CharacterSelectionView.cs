using CatGame.Capabilities.UISystem;
using CatGame.Core.Data;
using System;
using TMPro;
using UnityEngine;

namespace CatGame.Features.CharacterSelection
{
    public class CharacterSelectionView : BaseUIScreen
    {
        public event Action OnActivatedView;
        public event Action OnDesactivatedView;

        [Tooltip("Elemento default ao abrir o menu para o P1")]
        [SerializeField] private ButtonElement firstElement;

        [Tooltip("Elemento default ao abrir o menu para o P2")]
        [SerializeField] private ButtonElement secondElement;

        [SerializeField] private MenuController menuPresenter;

        [SerializeField] private TextMeshProUGUI firstCharacterName;
        [SerializeField] private TextMeshProUGUI secondCharacterName;

        private void OnDestroy()
        {
            OnDesactivatedView?.Invoke();
        }

        protected override void OnAfterShow()
        {
            menuPresenter.CurrentNavigationGroup.SetDefaultForPlayer(new PlayerId(0), firstElement);
            menuPresenter.CurrentNavigationGroup.SetDefaultForPlayer(new PlayerId(1), secondElement);

            OnActivatedView?.Invoke();
        }

        protected override void OnBeforeHide()
        {
            OnDesactivatedView?.Invoke();
        }

        public void ShowCharacterName(PlayerId player, string name)
        {
            switch (player.Id)
            {
                case 0: firstCharacterName.text = name; break; 
                case 1: secondCharacterName.text = name; break;
                default: Core.Logger.Log($"{player} não foi adicionado"); break;
            }
        }

        /*[SerializeField] private CharacterView characterViewPrefab;

        public void InitViews(string characterName, Sprite characterSprite)
        {
            Instantiate(characterViewPrefab);
        }*/
    }
}