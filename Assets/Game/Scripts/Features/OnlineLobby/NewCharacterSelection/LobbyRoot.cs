using CatGame.Capabilities.UISystem;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace CatGame.Features.CharacterSelection
{
    public class LobbyRoot : MonoBehaviour
    {
        [SerializeField] private LobbyView lobbyView;
        [SerializeField] private OnlineLobbyModel onlineLobbyModel;
        [SerializeField] private ButtonElement createOnlineRoomBtn;
        [SerializeField] private int maxPlayers;

        private bool isOnline;

        private ILobbyService lobbyService;
        private LobbyPresenter lobbyPresenter;
        private readonly Dictionary<int, LobbySlotInput> bindings = new();

        private int onlineSlot = -1;


        private void Awake()
        {
            createOnlineRoomBtn.OnSubmittedEvent += CreateOnlineRoomBtn_OnSubmittedEvent;
        }


        private void CreateOnlineRoomBtn_OnSubmittedEvent(object sender, NavigableElement.SubmittedEvent e)
        {
            OpenOnlineHost();
        }

        private void OnDestroy()
        {
            lobbyPresenter?.Dispose();
            ReleaseAllInputs();

            createOnlineRoomBtn.OnSubmittedEvent -= CreateOnlineRoomBtn_OnSubmittedEvent;
            onlineLobbyModel.OnOwnerDisconected -= OnlineLobbyModel_OnOwnerDisconected;
            if (lobbyService != null) lobbyService.Changed -= RefreshOnlineBinding;
        }
     

        #region Online

        private void OpenOnlineHost()
        {
            if (NetworkManager.Singleton.ShutdownInProgress)
            {
                Core.Logger.LogWarning("A sessão anterior ainda está sendo encerrada");
                return;
            }

            if (NetworkManager.Singleton.IsListening)
            {
                Core.Logger.LogWarning("Server já estava criado");
                return;
            }

            isOnline = true;
         
            BeginOnline();

            if (onlineLobbyModel == null) 
                return;

            onlineLobbyModel.CreateLobby();
        }

        public void OpenOnlineClient()
        {
            isOnline = true;
            BeginOnline();
        }

        private void BeginOnline()
        {
            isOnline = true;
            StartCoroutine(BindWhenOnline());
        }

        private IEnumerator BindWhenOnline()
        {
            yield return new WaitUntil(() => onlineLobbyModel.IsSpawned);

            CreatePresenter(onlineLobbyModel);
            onlineLobbyModel.OnOwnerDisconected += OnlineLobbyModel_OnOwnerDisconected;
        }

        private void OnlineLobbyModel_OnOwnerDisconected()
        {
            ReleaseAllInputs();

            onlineLobbyModel.OnOwnerDisconected -= OnlineLobbyModel_OnOwnerDisconected;
        }

        private void RefreshOnlineBinding()
        {
           /*lobbySlotInput.Unbind();

            Debug.Log("PlayerJoined");
            if (lobbyService == null)
                return;

            int slotIndex = isOnline ? FindControlledSlot() : -1;
            Debug.Log($"Player Slot Insddex: {slotIndex}");

            if (slotIndex < 0)
                return;

            lobbySlotInput.CreatePresenter(lobbyView.GetSlotView(slotIndex));*/

            int slot = FindControlledSlot();
            if (slot < 0 || slot == onlineSlot) return;

            if (onlineSlot >= 0) UnbindSlot(onlineSlot);
            BindSlot(slot);
            onlineSlot = slot;
        }

        private int FindControlledSlot()
        {
            for (int i = 0; i < lobbyService.SlotCount; i++)
            {
                if (lobbyService.CanControl(i))
                    return i;
            }
            
            return -1;
        }

        #endregion

        private void BindSlot(int slot)
        {
            UnbindSlot(slot);

            LobbySlotInput slotInput = new LobbySlotInput();
            slotInput.Bind(lobbyView.GetSlotView(slot));

            lobbyService?.SetSlotInput(slotInput);
            bindings[slot] = slotInput;
        }

        private void UnbindSlot(int slot)
        {
            if (bindings.TryGetValue(slot, out var binds)) 
            { 
                binds.Dispose();
                bindings.Remove(slot); 
            }
        }

        // Chamar ao sair do lobby (ex: ao iniciar a partida) para bloquear os inputs do lobby.
        public void ReleaseAllInputs()
        {
            foreach (var bind in bindings.Values) 
                bind.Dispose();

            bindings.Clear();
            onlineSlot = -1;
        }

        private void CreatePresenter(ILobbyService lobbyModel)
        {
            if (lobbyService != null) lobbyService.Changed -= RefreshOnlineBinding;
            lobbyPresenter?.Dispose();

            lobbyService = lobbyModel;
            lobbyPresenter = new LobbyPresenter(lobbyService, lobbyView);

            if (isOnline)
            {
                lobbyService.Changed += RefreshOnlineBinding;
                RefreshOnlineBinding();
            }
        }




        #region NotImplemented

        private void OpenLocal()
        {
            isOnline = false;
            // TODO - Abrir local
        }

        #endregion
    }
}