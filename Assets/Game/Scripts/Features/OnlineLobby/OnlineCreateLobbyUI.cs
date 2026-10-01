using CatGame.Capabilities.UISystem;
using System;
using UnityEngine;
using UnityEngine.VFX;

namespace CatGame.Features.OnlineLobby
{
    public class OnlineCreateLobbyUI : BaseUIScreen
    {
        [SerializeField] private ButtonElement buttonElement;

        public event Action OnEnabledMenu;
        public event Action OnDisabledMenu;
        public event Action OnTriedHideMenu;

        protected override void OnAfterShow()
        {   
            OnEnabledMenu?.Invoke();
        }

        protected override void OnBeforeHide()
        {
            OnDisabledMenu?.Invoke();
        }

        public override bool CanHide()
        {
            OnTriedHideMenu?.Invoke();
            return false;
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