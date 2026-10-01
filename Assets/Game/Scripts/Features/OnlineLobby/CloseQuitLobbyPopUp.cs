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

        INavigationGroup navigationGroup;

        private void Awake()
        {
            navigationGroup = new NavigationGroup(NavigationMode.Shared);
            navigationGroup.Register(confirmButton);
            navigationGroup.Register(cancelButton);

            confirmButton.OnSubmittedEvent += ConfirmButton_OnSubmittedEvent;
            cancelButton.OnSubmittedEvent += CancelButton_OnSubmittedEvent;
        }

        protected override void OnDestroy()
        {
            confirmButton.OnSubmittedEvent -= ConfirmButton_OnSubmittedEvent;
            cancelButton.OnSubmittedEvent -= CancelButton_OnSubmittedEvent;
        }

        private void ConfirmButton_OnSubmittedEvent(object sender, NavigableElement.SubmittedEvent e)
        {
            OnConfirmed?.Invoke();
        }

        private void CancelButton_OnSubmittedEvent(object sender, NavigableElement.SubmittedEvent e)
        {
            OnCancelled?.Invoke();
        }

        protected override void OnAfterShow()
        {
            IUINavigationService uiNavigationService = ServiceLocator.Get<IUINavigationService>();
            uiNavigationService.PushGroup(new PlayerId(0), navigationGroup, false); // Somente o jogador 1 navega.
        }
    }
}
