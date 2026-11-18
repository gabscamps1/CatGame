using CatGame.Core;
using CatGame.Core.Enums;
using CatGame.Core.Interfaces;
using CatGame.Features.OnlineLobby;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CatGame.Features.CharacterSelection
{
    public class OnlineLobbyModel : NetworkBehaviour, ILobbyService
    {
        const float MinCommandInterval = 0.05f;   // throttle por jogador (server)

        [SerializeField] private CharacterCatalog catalog;
        [SerializeField] private bool allowDuplicateCharacters;

        private readonly NetworkList<LobbySlot> slots = new();
        private readonly NetworkVariable<LobbyPhase> phase = new(LobbyPhase.Selecting);
        private readonly Dictionary<ulong, float> lastCommandTime = new(); // Só no server

        // Events.
        public event Action Changed;
        public event Action OnNetworkSpawned;
        public event Action OnOwnerDisconected;

        public int SlotCount => slots.Count;
        public LobbySlot GetSlot(int i) => slots[i];


        public string Port { get; private set; } = null; // Porta da sessão

        // Server Configs.
        private int maxPlayers;


        private IUIService uiService;
        private CloseQuitLobbyPopUp popUp;
        private LobbySlotInput slotInput;

        public void Init(int maxPlayers)
        {
            this.maxPlayers = maxPlayers;
        }

        #region Unity

        private void Awake()
        {
            uiService = ServiceLocator.Get<IUIService>();
            uiService.OnCurrentUIChanged += UiService_OnCurrentUIChanged;

            Changed += OnlineLobbyModel_Changed;
        }

        public override void OnDestroy()
        {
            uiService.OnCurrentUIChanged -= UiService_OnCurrentUIChanged;
            Changed -= OnlineLobbyModel_Changed;
            base.OnDestroy();
        }

        public override void OnNetworkSpawn()
        {
            // Sessão nova reseta informações anteriores que podem ter sido mantidas.
            if (IsServer)
            {             
                slots.Clear();
                phase.Value = LobbyPhase.Selecting;
                lastCommandTime.Clear();
                ConnectionManager.Instance.OpenConnections();
            }

            slots.OnListChanged += OnSlotsChanged;
            phase.OnValueChanged += OnPhaseChanged;
            NetworkManager.OnClientDisconnectCallback += Singleton_OnClientDisconnectCallback;

            if (IsServer)
            {
                NetworkManager.OnClientConnectedCallback += AddSlotFor;
                NetworkManager.OnClientDisconnectCallback += RemoveSlotFor;              

                foreach (var id in NetworkManager.ConnectedClientsIds) AddSlotFor(id);
            }

            Changed?.Invoke();
            OnNetworkSpawned?.Invoke();
        }

        public override void OnNetworkDespawn()
        {
            slots.OnListChanged -= OnSlotsChanged;
            phase.OnValueChanged -= OnPhaseChanged;

            if (NetworkManager != null)
            {
                NetworkManager.OnClientConnectedCallback -= AddSlotFor;
                NetworkManager.OnClientDisconnectCallback -= RemoveSlotFor;

                // Por algum motivo o OnNetworkDespawn está sendo chamado antes deste evento, então não me desinscrevo por aqui.
                // Atualmente se desincreve quando o próprio método é chamado.
                //NetworkManager.OnClientDisconnectCallback -= Singleton_OnClientDisconnectCallback; 
            }
        }

        private void OnSlotsChanged(NetworkListEvent<LobbySlot> change) => Changed?.Invoke();
        private void OnPhaseChanged(LobbyPhase previous, LobbyPhase current) => Changed?.Invoke();

        #endregion   

        #region ServerSlots

        private void AddSlotFor(ulong clientId)
        {
            if (FindSlot(clientId) >= 0) return;

            ushort currentSlotNumber = (ushort)(slots.Count + 1); // Pega o slotInput que está sendo adicionado. Exemplo: Vai entrar um segundo player agora, ou seja, o slotInput ainda é igual a 1. slotInput[1] 
            ushort characterId = catalog.TryGet(currentSlotNumber, out var entry) ? entry.Id : (ushort)0;

            slots.Add(new LobbySlot
            {
                OwnerId = clientId,
                PlayerName = new FixedString32Bytes($"Jogador {clientId}"),
                CharacterId = characterId,
            });
        }

        private void RemoveSlotFor(ulong clientId)
        {
            lastCommandTime.Remove(clientId);
            if (phase.Value != LobbyPhase.Selecting) return;
            int i = FindSlot(clientId);
            if (i >= 0) slots.RemoveAt(i);
        }

        private bool IsFullLobby()
        {
            return slots.Count >= maxPlayers;
        }

        private int FindSlot(ulong clientId)
        {
            for (int i = 0; i < slots.Count; i++)
                if (slots[i].OwnerId == clientId) return i;
            return -1;
        }

        private List<LobbySlot> Snapshot()
        {
            var list = new List<LobbySlot>(slots.Count);
            foreach (var s in slots) list.Add(s);
            return list;
        }

        private bool PassesThrottle(ulong sender)
        {
            float now = Time.unscaledTime;
            if (lastCommandTime.TryGetValue(sender, out float last) && now - last < MinCommandInterval)
                return false;
            lastCommandTime[sender] = now;
            return true;
        }

        private bool IsUnlocked(ulong clientId, ushort characterId) => true;

        private void OnlineLobbyModel_Changed()
        {
            ConnectionManager.Instance.FullLobby(IsFullLobby());
        }

        #endregion

        #region ClientValidations

        public bool CanControl(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < slots.Count &&
                   slots[slotIndex].OwnerId == NetworkManager.LocalClientId;
        }

        #endregion

        #region ClientRequests     

        public void CycleCharacter(int slotIndex, int direction)
        {
            if (!CanControl(slotIndex)) return;
            if (direction != -1 && direction != 1) return;
            CycleCharacterRpc((sbyte)direction);
        }

        public void SetReady(int slotIndex, bool ready)
        {
            if (!CanControl(slotIndex)) return;
            SetReadyRpc(ready);
        }

        public void RequestStart() => RequestStartRpc();

        #endregion

        #region ServerValidations

        [Rpc(SendTo.Server)]
        private void CycleCharacterRpc(sbyte direction, RpcParams rpc = default)
        {
            ulong sender = rpc.Receive.SenderClientId;
            if (!PassesThrottle(sender)) return;
            if (phase.Value != LobbyPhase.Selecting) return;
            if (direction != -1 && direction != 1) return; // Não confia no payload

            int slotIndex = FindSlot(sender); // Slot ligado ao client
            if (slotIndex < 0) return;

            var slots = Snapshot();
            ushort next = LobbyRules.NextAvailable(slots, slotIndex, direction, catalog, allowDuplicateCharacters, id => IsUnlocked(sender, id));
            if (next == 0 || next == slots[slotIndex].CharacterId) return;

            LobbySlot slot = this.slots[slotIndex];
            slot.CharacterId = next;
            this.slots[slotIndex] = slot; // Replica para todos os clients
        }

        [Rpc(SendTo.Server)]
        private void SetReadyRpc(bool ready, RpcParams rpc = default)
        {
            ulong sender = rpc.Receive.SenderClientId;
            if (!PassesThrottle(sender)) return;
            if (phase.Value != LobbyPhase.Selecting) return;

            int slot = FindSlot(sender);
            if (slot < 0) return;
            if (!LobbyRules.CanSetReady(Snapshot(), slot, ready)) return;

            var s = slots[slot];
            s.Ready = ready;
            slots[slot] = s;
        }

        [Rpc(SendTo.Server)]
        private void RequestStartRpc(RpcParams rpc = default)
        {
            // Só o host inicia. Com dedicated server: iniciar automático quando todos prontos.
            if (rpc.Receive.SenderClientId != NetworkManager.ServerClientId) return;
            if (phase.Value != LobbyPhase.Selecting) return;
            if (!LobbyRules.CanStart(Snapshot())) return;

            ConnectionManager.Instance.CloseConnections();

            // As IAs completam as vagas restantes só na hora de iniciar (não entram na lista do lobby).
            List<LobbySlot> participants = Snapshot();
            int freeSlots = maxPlayers - participants.Count;
            int addedAIs = LobbyRules.FillWithAI(participants, maxPlayers, catalog, NetworkManager.ServerClientId);

            if (addedAIs < freeSlots)
                Core.Logger.LogWarning($"Só {addedAIs} de {freeSlots} IA(s) foram adicionadas: faltam personagens livres com {nameof(CharacterCatalog.Entry.AIPrefab)} no {nameof(CharacterCatalog)}.");

            MatchSetup.Instance.Capture(participants);
            phase.Value = LobbyPhase.Starting;

            // O serviço carrega o grupo para todos os clients.
            ServiceLocator.Get<ISceneLoadingService>().LoadOnlineAsyncScene(Scenes.Playing);
        }

        #endregion

        #region LobbyCreation

        public void CreateLobby()
        {
            if (NetworkManager.Singleton.IsListening)
            {
                Core.Logger.LogWarning("Server já estava criado");
                return;
            }

            int port = 5058;

            while (!IsPortAvaliable(port))
                port++;

            Port = port.ToString();

            if (!NetworkManager.Singleton.TryGetComponent(out UnityTransport unityTransport))
            {
                Core.Logger.LogError($"Não foi encontrado o {nameof(NetworkTransport)} do {nameof(NetworkManager)}");
                return;
            }

            string localIPAddress = "127.0.0.1";
            string genericIp = "0.0.0.0"; // Aceita conexões IPv4 e IPv6.
            unityTransport.SetConnectionData(localIPAddress, (ushort)port, genericIp);

            if (!NetworkManager.Singleton.StartHost())
                return;

            // Clientes mantêm as cenas que já abriram localmente (ex: lobby) ao sincronizar com o servidor.
            NetworkManager.Singleton.SceneManager.SetClientSynchronizationMode(LoadSceneMode.Additive);
        }

        private bool IsPortAvaliable(int port)
        {
            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();

            // Lista todas as portas UDP que estão ativas no PC neste momento
            IPEndPoint[] udpListeners = ipGlobalProperties.GetActiveUdpListeners();

            // Procura se a nossa porta está nessa lista do Windows
            foreach (IPEndPoint endPoint in udpListeners)
            {
                if (endPoint.Port == port)
                {
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region CloseLobby

        public void RequestCloseLobby()
        {
            // Popup já está aberta, então ignora o código.
            if (uiService.CurrentPanelType.HasValue && uiService.CurrentPanelType.Value == PanelType.QuitLobbyPopup)
                return;

            uiService.Push(PanelType.QuitLobbyPopup);
            IBasePanel panel = uiService.CurrentPanel;
            popUp = panel as CloseQuitLobbyPopUp;

            if (popUp == null)
            {
                Core.Logger.LogError($"Não foi encontrado o panel {nameof(CloseQuitLobbyPopUp)}");
                return;
            }

            // Quando a popup é aberta, se inscreve nos eventos da popup
            popUp.OnConfirmed += CloseQuitLobbyPopUp_OnConfirmed;
            popUp.OnCancelled += CloseQuitLobbyPopUp_OnCancelled;
            popUp.OnHidedPopup += CloseQuitLobbyPopUp_OnHidedPopup;
            slotInput?.DisableInputsToThisSlot(); // Desabilita o input do lobby.
        }

        private void CloseQuitLobbyPopUp_OnConfirmed()
        {
            if (!NetworkManager.Singleton.IsListening)
            {
                Core.Logger.LogWarning("Server já estava fechado");
                return;
            }

            // Popup não for o foco atual, então ignora o código.
            if (uiService.CurrentPanelType.Value != PanelType.QuitLobbyPopup)
                return;

            uiService.Pop();
            CloseLobby();
            DisposePopupEvents();
        }

        private void CloseQuitLobbyPopUp_OnCancelled()
        {
            // Popup não for o foco atual, então ignora o código.
            if (uiService.CurrentPanelType.Value != PanelType.QuitLobbyPopup)
                return;

            uiService.Pop();          
        }

         
        private void CloseQuitLobbyPopUp_OnHidedPopup()
        {
            DisposePopupEvents();
            StartCoroutine(RestoreInput());
        }

        private IEnumerator RestoreInput()
        {
            yield return null; // Espera 1 frame para recuperar o input. Garante que duas ações não sejam enviadas ao mesmo tempo.
            slotInput?.EnableInputToThisSlot();          
        }

        private void DisposePopupEvents()
        {
            popUp.OnConfirmed -= CloseQuitLobbyPopUp_OnConfirmed;
            popUp.OnCancelled -= CloseQuitLobbyPopUp_OnCancelled;
            popUp.OnHidedPopup -= CloseQuitLobbyPopUp_OnHidedPopup;
        }

        /// <summary>
        /// Fechar/Sair do lobby
        /// </summary>
        private void CloseLobby()
        {
            NetworkManager.Singleton.Shutdown(true);
        }

        #endregion

        public void SetSlotInput(LobbySlotInput slotInput)
        {
            this.slotInput = slotInput;
        }

        private void UiService_OnCurrentUIChanged(PanelType? obj)
        {
            // Garante que quando a popup for fechada, os eventos são desinscrevidos.
            // Não faz isso diretamente no proprio método para evitar problemas caso a popup for fechada de outras maneiras.
            if (!uiService.IsVisible(PanelType.QuitLobbyPopup) && popUp != null)
            {
                popUp.OnConfirmed -= CloseQuitLobbyPopUp_OnConfirmed;
                popUp.OnCancelled -= CloseQuitLobbyPopUp_OnCancelled;
            }
        }

        private void Singleton_OnClientDisconnectCallback(ulong clientID)
        {
            if (clientID != NetworkManager.ServerClientId && clientID != NetworkManager.LocalClientId)
                return;

            OnOwnerDisconected?.Invoke();
            NetworkManager.OnClientDisconnectCallback -= Singleton_OnClientDisconnectCallback;
            return;
        }
    }
}