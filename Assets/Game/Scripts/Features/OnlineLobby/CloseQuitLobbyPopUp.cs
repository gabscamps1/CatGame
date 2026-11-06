using CatGame.Capabilities.UISystem;
using CatGame.Core;
using CatGame.Core.Interfaces;
using CatGame.Core.Data;
using CatGame.Core.Enums;
using System;
using UnityEngine;

namespace CatGame.Features.OnlineLobby
{
    public class CloseQuitLobbyPopUp : BasePanel
    {
        [SerializeField] private ButtonElement confirmButton;
        [SerializeField] private ButtonElement cancelButton;

        public event Action OnConfirmed;
        public event Action OnCancelled;
        public event Action OnHidedPopup;

        [Header("Sounds")]
        [SerializeField] private AudioData UI_POPUP;

        private IAudioService audioService;

        private readonly PlayerId playerId = new PlayerId(0);
        INavigationGroup navigationGroup;

        private void Awake()
        {
            audioService = ServiceLocator.Get<IAudioService>();

            navigationGroup = new NavigationGroup(NavigationMode.Shared);
            navigationGroup.Register(confirmButton, true);
            navigationGroup.Register(cancelButton);

            confirmButton.OnSubmittedEvent += ConfirmButton_OnSubmittedEvent;
            cancelButton.OnSubmittedEvent += CancelButton_OnSubmittedEvent;
            navigationGroup.OnCancelRequested += NavigationGroup_OnCancelRequested;
        }

        private void OnEnable()
        {
            audioService.PlaySFXClip(UI_POPUP);
        }

        protected override void OnDestroy()
        {
            confirmButton.OnSubmittedEvent -= ConfirmButton_OnSubmittedEvent;
            cancelButton.OnSubmittedEvent -= CancelButton_OnSubmittedEvent;
            navigationGroup.OnCancelRequested -= NavigationGroup_OnCancelRequested;
        }

        private void ConfirmButton_OnSubmittedEvent(object sender, NavigableElement.SubmittedEvent e)
        {
            // Se não for o player 1, não navega.
            if (e.PlayerId.Id != 0)
                return;

            OnConfirmed?.Invoke();
        }

        private void CancelButton_OnSubmittedEvent(object sender, NavigableElement.SubmittedEvent e)
        {
            // Se não for o player 1, não navega.
            if (e.PlayerId.Id != 0)
                return;

            OnCancelled?.Invoke();
        }

        private void NavigationGroup_OnCancelRequested(object sender, PlayerId e)
        {
            // Se não for o player 1, não navega.
            if (e.Id != 0)
                return;

            OnCancelled?.Invoke();
        }

        protected override void OnAfterShow()
        {
            IUINavigationService uiNavigationService = ServiceLocator.Get<IUINavigationService>();
            uiNavigationService.PushGroup(playerId, navigationGroup, false); // Somente o jogador 1 navega.
        }

        protected override void OnAfterHide()
        {
            IUINavigationService uiNavigationService = ServiceLocator.Get<IUINavigationService>();
            INavigationGroup activeGroup = uiNavigationService.GetActiveGroup(playerId);

            // Garante que somente esse grupo seja fechado.
            if (activeGroup != null && activeGroup == navigationGroup)
            {
                uiNavigationService.PopGroup(playerId, navigationGroup);
                activeGroup.ResetFocusToDefault(playerId);

                OnHidedPopup?.Invoke();
            }            
        }
    }
}
