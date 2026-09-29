using CatGame.Capabilities.UISystem;
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

        private void Awake()
        {
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
    }
}
