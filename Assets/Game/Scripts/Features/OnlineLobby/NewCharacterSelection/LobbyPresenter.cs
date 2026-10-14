using System;
using System.Collections.Generic;

namespace CatGame.Features.CharacterSelection
{
    public class LobbyPresenter : IDisposable
    {
        private readonly ILobbyService lobbyModel;
        private readonly ILobbyView lobbyView;

        public LobbyPresenter(ILobbyService lobbyModel, ILobbyView lobbyView)
        {
            this.lobbyModel = lobbyModel;
            this.lobbyView = lobbyView;

            lobbyModel.Changed += LobbyModel_Changed;
            lobbyModel.OnOwnerDisconected += LobbyModel_OnOwnerDisconected;

            lobbyView.StartPressed += LobbyView_StartPressed;
            lobbyView.CancelPressed += LobbyView_CancelPressed;
            lobbyView.ConfirmPressed += LobbyView_ConfirmPressed;
            lobbyView.CyclePressed += LobbyView_CyclePressed;
            lobbyView.OnCloseLobbyPressed += LobbyView_OnTriedHideMenu;

            LobbyModel_Changed();
        }

        public void Dispose()
        {
            lobbyModel.Changed -= LobbyModel_Changed;
            lobbyModel.OnOwnerDisconected -= LobbyModel_OnOwnerDisconected;

            lobbyView.StartPressed -= LobbyView_StartPressed;
            lobbyView.CancelPressed -= LobbyView_CancelPressed;
            lobbyView.ConfirmPressed -= LobbyView_ConfirmPressed;
            lobbyView.CyclePressed -= LobbyView_CyclePressed;
            lobbyView.OnCloseLobbyPressed -= LobbyView_OnTriedHideMenu;
        }

        private void LobbyModel_OnOwnerDisconected()
        {
            lobbyView.CloseMenu();
        }

        private void LobbyView_OnTriedHideMenu()
        {
            for (int i = 0; i < lobbyModel.SlotCount; i++)
            {
                if (lobbyModel.CanControl(i) && !lobbyModel.GetSlot(i).Ready)           
                    lobbyModel.RequestCloseLobby();            
            }             
        }

        private void LobbyModel_Changed()
        {
            var slots = new List<LobbySlot>(lobbyModel.SlotCount);

            for (int i = 0; i < lobbyView.SlotCount; i++)
            {
                if (i < lobbyModel.SlotCount)
                {
                    var slot = lobbyModel.GetSlot(i);
                    slots.Add(slot);
                    lobbyView.ShowSlot(i, slot, lobbyModel.CanControl(i));
                }
                else
                {
                    lobbyView.ShowEmptySlot(i); // Se houver mais views que slots em uso, deixa as views extras como empty.
                }
            }

            lobbyView.ShotSocket(lobbyModel.Socket);
            lobbyView.SetStartInteractable(LobbyRules.CanStart(slots));
        }

        private void LobbyView_CyclePressed(int slotIndex, int direction)
        {
            Core.Logger.Log("TENTA DAR O CYCLE");
            lobbyModel.CycleCharacter(slotIndex, direction);
        }

        private void LobbyView_ConfirmPressed(int slotIndex)
        {
            if (!lobbyModel.GetSlot(slotIndex).Ready)
                lobbyModel.SetReady(slotIndex, true);
            else
                lobbyModel.RequestStart();

                Core.Logger.Log("PRONTOOOOOOO");
        }

        private void LobbyView_CancelPressed(int slotIndex)
        {
            if (lobbyModel.GetSlot(slotIndex).Ready) 
                lobbyModel.SetReady(slotIndex, false);
        }

        private void LobbyView_StartPressed()
        {
            //lobbyModel.RequestStart();
        }

        
    }
}