using CatGame.Capabilities.UISystem;
using System;
using UnityEngine;

namespace CatGame.Features.OnlineLobby
{
    public class OnlineCreateLobbyUI : BaseUIScreen
    {
        [SerializeField] private ButtonElement createLobbyButton;
        [SerializeField] private MenuController menuController;
        [SerializeField] private int menuToGoWhenCloseLobby;


        public event Action OnTriedHideMenu;

       
        public override bool CanHide()
        {
            OnTriedHideMenu?.Invoke();
            return false;
        }

        public void CloseLobbyUI()
        {
            menuController.ChangeMenu(menuToGoWhenCloseLobby, true);
            Debug.Log("Mudou");
        }

        public void UpdateUI()
        {

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