using System;
using System.Collections.Generic;

namespace CatGame.Features.CharacterSelection
{
    public class LobbyPresenter : IDisposable
    {
        private readonly ILobbyService lobbyModel;
        private readonly ILobbyView lobbyView;

        private int pendingJoins; // Pedidos de entrada desta máquina ainda sem resposta.

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

        #region Model

        public void SetPendingJoins(int count)
        {
            pendingJoins = count;
            LobbyModel_Changed();
        }

        private void LobbyModel_OnOwnerDisconected()
        {
            lobbyView.CloseMenu();
        }

        private void LobbyModel_Changed()
        {
            var slots = new List<LobbySlot>(lobbyModel.SlotCount);
            int joiningToShow = pendingJoins; // Os pedidos pendentes aparecem nas primeiras vagas livres.

            for (int i = 0; i < lobbyView.SlotCount; i++)
            {
                SlotId slotId = new SlotId(i);
                LobbySlot slot = i < lobbyModel.SlotCount ? lobbyModel.GetSlot(slotId) : LobbySlot.Empty;

                if (i < lobbyModel.SlotCount)
                    slots.Add(slot);

                if (slot.Occupied)
                {
                    lobbyView.ShowSlot(i, slot, lobbyModel.CanControl(slotId));
                    continue;
                }

                // Vaga livre mostra o ícone de IA. Se houver pedido pendente, mostra a mensagem de entrando no lobby.
                bool isJoining = i < lobbyModel.SlotCount && joiningToShow > 0;
                if (isJoining) joiningToShow--;

                lobbyView.ShowEmptySlot(i, isJoining);
            }

            lobbyView.ShotSocket(lobbyModel.Port);
            lobbyView.SetStartInteractable(LobbyRules.CanStart(slots));
        }

        #endregion

        #region View

        private void LobbyView_OnTriedHideMenu()
        {
            // Somente o jogador local 0 abre o popup.
            if (!lobbyModel.TryGetLocalSlot(LobbyLocalPlayerRules.MAIN_LOCAL_ID, out SlotId mainSlot))
                return;

            LobbySlot slot = lobbyModel.GetSlot(mainSlot);
            if (!slot.Ready && !slot.WaitingDevice)
                lobbyModel.RequestCloseLobby();
        }

        private void LobbyView_CyclePressed(int slotIndex, int direction)
        {
            lobbyModel.CycleCharacter(new SlotId(slotIndex), direction);
        }

        private void LobbyView_ConfirmPressed(int slotIndex)
        {
            SlotId slotId = new SlotId(slotIndex);
            LobbySlot slot = lobbyModel.GetSlot(slotId);

            if (!slot.Ready)
            {
                lobbyModel.SetReady(slotId, true);
                return;
            }

            // Se já estiver pronto, o segundo toque do jogador local 0 do host inicia a partida.
            if (lobbyModel.IsHost && slot.LocalId == LobbyLocalPlayerRules.MAIN_LOCAL_ID)
                lobbyModel.RequestStart(slotId);
        }

        private void LobbyView_CancelPressed(int slotIndex)
        {
            SlotId slotId = new SlotId(slotIndex);

            if (lobbyModel.GetSlot(slotId).Ready)
                lobbyModel.SetReady(slotId, false);
        }

        private void LobbyView_StartPressed()
        {
            //lobbyModel.RequestStart();
        }

        #endregion
    }
}
