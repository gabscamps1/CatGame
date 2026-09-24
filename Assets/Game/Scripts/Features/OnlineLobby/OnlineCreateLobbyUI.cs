using CatGame.Capabilities.UISystem;
using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace CatGame.Features.OnlineLobby
{
    public class OnlineCreateLobbyUI : BaseUIScreen
    {
        [SerializeField] private ButtonElement buttonElement;

        public event Action OnEnabledMenu;
        public event Action OnDisabledMenu;

        protected override void OnAfterShow()
        {   
            OnEnabledMenu?.Invoke();
        }

        protected override void OnBeforeHide()
        {
            OnDisabledMenu?.Invoke();
        }

        #region Join

        private void InputFieldElement_OnValueChanged(object sender, InputFieldElement.ValueChangedEvent e)
        {
            SetJoinButtonInteractable();
        }

        private void SetJoinButtonInteractable()
        {
            // TODO
            // buttonElement.SetInteractable();
        }

        private void ButtonElement_OnSubmittedEvent(object sender, NavigableElement.SubmittedEvent e)
        {
            // TODO
        }

        #endregion
    }
}