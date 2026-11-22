using CatGame.Core;
using CatGame.Core.Enums;
using CatGame.Core.Interfaces;
using CatGame.Features.OnlineLobby;
using System;
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
    /// <summary>
    /// Estado do lobby (local e online).
    /// </summary>
    public class OnlineLobbyModel : NetworkBehaviour, ILobbyService
    {
        private const float MIN_COMMAND_INTERVAL = 0.05f; // Throttle por jogador (server)
        private const byte MAIN_LOCAL_ID = 0; // Jogador principal de cada client.

        [SerializeField] private CharacterCatalog catalog;
        [SerializeField] private bool allowDuplicateCharacters = true;

        private readonly NetworkList<LobbySlot> slots = new();
        private readonly NetworkVariable<LobbyPhase> phase = new(LobbyPhase.Selecting);
        private readonly Dictionary<(ulong clientId, byte localId), float> lastCommandTime = new(); // Só no server

        // Events.
        public event Action Changed;
        public event Action OnNetworkSpawned;
        public event Action OnOwnerDisconected;
        public event Action<byte, SessionCallbackReason> OnJoinRejected;

        public int SlotCount => slots.Count;
        public string Port { get; private set; } = null; // Porta da sessão

        // Server Configs.
        private int maxPlayers;

        private IUIService uiService;
        private CloseQuitLobbyPopUp popUp;

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
                CreateEmptySlots();
                phase.Value = LobbyPhase.Selecting;
                lastCommandTime.Clear();
                ConnectionManager.Instance.OpenConnections();
            }

            slots.OnListChanged += OnSlotsChanged;
            phase.OnValueChanged += OnPhaseChanged;
            NetworkManager.OnClientDisconnectCallback += Singleton_OnClientDisconnectCallback;

            if (IsServer)
                NetworkManager.OnClientDisconnectCallback += RemoveSlotsOf;

            Changed?.Invoke();
            OnNetworkSpawned?.Invoke();
        }

        public override void OnNetworkDespawn()
        {
            slots.OnListChanged -= OnSlotsChanged;
            phase.OnValueChanged -= OnPhaseChanged;

            if (NetworkManager != null)
            {
                NetworkManager.OnClientDisconnectCallback -= RemoveSlotsOf;

                // Por algum motivo o OnNetworkDespawn está sendo chamado antes deste evento, então não me desinscrevo por aqui.
                // Atualmente se desincreve quando o próprio método é chamado.
                //NetworkManager.OnClientDisconnectCallback -= Singleton_OnClientDisconnectCallback;
            }
        }

        private void OnSlotsChanged(NetworkListEvent<LobbySlot> change) => Changed?.Invoke();
        private void OnPhaseChanged(LobbyPhase previous, LobbyPhase current) => Changed?.Invoke();

        #endregion

        #region ServerSlots

        private void CreateEmptySlots()
        {
            if (maxPlayers <= 0)
                Core.Logger.LogError($"[{nameof(OnlineLobbyModel)}] {nameof(Init)} não foi chamado antes do spawn: o lobby não tem vagas.");

            slots.Clear();
            for (int i = 0; i < maxPlayers; i++)
                slots.Add(LobbySlot.Empty);
        }

        /// <summary>
        /// Se o Client desconectar, todas as vagas dele ficam livres.
        /// </summary>
        private void RemoveSlotsOf(ulong clientId)
        {
            foreach (var key in new List<(ulong, byte)>(lastCommandTime.Keys))
                if (key.Item1 == clientId) lastCommandTime.Remove(key);

            if (phase.Value != LobbyPhase.Selecting) return;

            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].Occupied && !slots[i].IsAI && slots[i].OwnerId == clientId)
                    slots[i] = LobbySlot.Empty;
            }
        }

        private bool IsFullLobby()
        {
            return slots.Count > 0 && LobbyRules.IsFull(Snapshot());
        }

        private int FindSlot(ulong clientId, byte localId)
        {
            return LobbyRules.FindSlot(Snapshot(), clientId, localId);
        }

        private List<LobbySlot> Snapshot()
        {
            var list = new List<LobbySlot>(slots.Count);
            foreach (var s in slots) list.Add(s);
            return list;
        }

        private bool PassesThrottle(ulong sender, byte localId)
        {
            float now = Time.unscaledTime;
            var key = (sender, localId);

            if (lastCommandTime.TryGetValue(key, out float last) && now - last < MIN_COMMAND_INTERVAL)
                return false;

            lastCommandTime[key] = now;
            return true;
        }

        private bool IsUnlocked(ulong clientId, ushort characterId) => true;

        private void OnlineLobbyModel_Changed()
        {
            if (IsServer)
                ConnectionManager.Instance.FullLobby(IsFullLobby());
        }

        #endregion

        #region ClientValidations

        public LobbySlot GetSlot(SlotId slot)
        {
            return IsValidSlot(slot) ? slots[slot.Value] : LobbySlot.Empty;
        }

        public bool CanControl(SlotId slot)
        {
            if (!IsValidSlot(slot)) return false;

            LobbySlot lobbySlot = slots[slot.Value];
            return lobbySlot.Occupied && !lobbySlot.IsAI && lobbySlot.OwnerId == NetworkManager.LocalClientId;
        }

        public bool TryGetLocalSlot(byte localId, out SlotId slot)
        {
            int index = FindSlot(NetworkManager.LocalClientId, localId);
            slot = index >= 0 ? new SlotId(index) : SlotId.None;
            return index >= 0;
        }

        private bool IsValidSlot(SlotId slot)
        {
            return slot.IsValid && slot.Value < slots.Count;
        }

        #endregion

        #region ClientRequests

        public void RequestJoin(byte localId, DeviceKind deviceKind)
        {
            RequestJoinRpc(localId, deviceKind);
        }

        public void LeaveSlot(SlotId slot)
        {
            if (!CanControl(slot)) return;

            byte localId = slots[slot.Value].LocalId;
            if (localId == MAIN_LOCAL_ID) return; // O jogador principal só sai pelo popup.

            LeaveSlotRpc(localId);
        }

        public void SetWaitingDevice(SlotId slot)
        {
            if (!CanControl(slot)) return;
            SetWaitingDeviceRpc(slots[slot.Value].LocalId, true, default);
        }

        public void ResumeWithDevice(SlotId slot, DeviceKind deviceKind)
        {
            if (!CanControl(slot)) return;
            SetWaitingDeviceRpc(slots[slot.Value].LocalId, false, deviceKind);
        }

        public void CycleCharacter(SlotId slot, int direction)
        {
            if (!CanControl(slot)) return;
            if (direction != -1 && direction != 1) return;
            CycleCharacterRpc(slots[slot.Value].LocalId, (sbyte)direction);
        }

        public void SetReady(SlotId slot, bool ready)
        {
            if (!CanControl(slot)) return;
            SetReadyRpc(slots[slot.Value].LocalId, ready);
        }

        public void RequestStart(SlotId slot)
        {
            if (!IsServer || !CanControl(slot)) return;
            if (slots[slot.Value].LocalId != MAIN_LOCAL_ID) return; // Só o jogador local 0 do host inicia.

            RequestStartRpc(MAIN_LOCAL_ID);
        }

        #endregion

        #region ServerValidations

        [Rpc(SendTo.Server)]
        private void RequestJoinRpc(byte localId, DeviceKind deviceKind, RpcParams rpc = default)
        {
            ulong sender = rpc.Receive.SenderClientId;
            if (!PassesThrottle(sender, localId)) return;
            if (phase.Value != LobbyPhase.Selecting) return;
            if (FindSlot(sender, localId) >= 0) return; // Pedido repetido: o jogador já tem vaga.

            List<LobbySlot> snapshot = Snapshot();
            int freeSlot = LobbyRules.FirstFreeSlot(snapshot);

            if (freeSlot < 0)
            {
                RejectJoin(sender, localId, SessionCallbackReason.FullLobby);
                return;
            }

            slots[freeSlot] = new LobbySlot
            {
                Occupied = true,
                OwnerId = sender,
                LocalId = localId,
                CharacterId = LobbyRules.FirstFreeCharacter(snapshot, catalog),
                DeviceKind = deviceKind,
                PlayerName = new FixedString32Bytes($"Jogador {freeSlot + 1}"),
                ControlType = PlayerControlType.Human,
            };
        }

        /// <summary>
        /// O jogador principal de um client recusado é desconectado com o motivo.
        /// Um convidado recusado só recebe o aviso.
        /// </summary>
        private void RejectJoin(ulong sender, byte localId, SessionCallbackReason reason)
        {
            if (localId == MAIN_LOCAL_ID && sender != NetworkManager.ServerClientId)
            {
                NetworkManager.DisconnectClient(sender, reason.ToString());
                return;
            }

            JoinRejectedRpc(localId, reason, RpcTarget.Single(sender, RpcTargetUse.Temp));
        }

        [Rpc(SendTo.Server)]
        private void LeaveSlotRpc(byte localId, RpcParams rpc = default)
        {
            ulong sender = rpc.Receive.SenderClientId;
            if (!PassesThrottle(sender, localId)) return;
            if (phase.Value != LobbyPhase.Selecting) return;
            if (localId == MAIN_LOCAL_ID) return; // O jogador principal nunca deixa a vaga sozinho.

            int slot = FindSlot(sender, localId);
            if (slot < 0 || slots[slot].Ready) return;

            slots[slot] = LobbySlot.Empty;
        }

        [Rpc(SendTo.Server)]
        private void SetWaitingDeviceRpc(byte localId, bool waiting, DeviceKind deviceKind, RpcParams rpc = default)
        {
            ulong sender = rpc.Receive.SenderClientId;
            if (phase.Value != LobbyPhase.Selecting) return; // Sem throttle: desconexão e reconexão não podem ser perdidas.

            int slot = FindSlot(sender, localId);
            if (slot < 0) return;

            LobbySlot lobbySlot = slots[slot];
            lobbySlot.WaitingDevice = waiting;

            if (waiting)
                lobbySlot.Ready = false; // Quem está sem dispositivo não está pronto.
            else
                lobbySlot.DeviceKind = deviceKind;

            slots[slot] = lobbySlot;
        }

        [Rpc(SendTo.Server)]
        private void CycleCharacterRpc(byte localId, sbyte direction, RpcParams rpc = default)
        {
            ulong sender = rpc.Receive.SenderClientId;
            if (!PassesThrottle(sender, localId)) return;
            if (phase.Value != LobbyPhase.Selecting) return;
            if (direction != -1 && direction != 1) return; // Não confia no payload

            int slotIndex = FindSlot(sender, localId);
            if (slotIndex < 0) return;

            var snapshot = Snapshot();
            ushort next = LobbyRules.NextAvailable(snapshot, slotIndex, direction, catalog, allowDuplicateCharacters, id => IsUnlocked(sender, id));
            if (next == 0 || next == snapshot[slotIndex].CharacterId) return;

            LobbySlot slot = slots[slotIndex];
            slot.CharacterId = next;
            slots[slotIndex] = slot; // Replica para todos os clients
        }

        [Rpc(SendTo.Server)]
        private void SetReadyRpc(byte localId, bool ready, RpcParams rpc = default)
        {
            ulong sender = rpc.Receive.SenderClientId;
            if (!PassesThrottle(sender, localId)) return;
            if (phase.Value != LobbyPhase.Selecting) return;

            int slot = FindSlot(sender, localId);
            if (slot < 0) return;
            if (!LobbyRules.CanSetReady(Snapshot(), slot, ready)) return;

            var s = slots[slot];
            s.Ready = ready;
            slots[slot] = s;
        }

        [Rpc(SendTo.Server)]
        private void RequestStartRpc(byte localId, RpcParams rpc = default)
        {
            // Só o jogador local 0 do host inicia.
            if (rpc.Receive.SenderClientId != NetworkManager.ServerClientId || localId != MAIN_LOCAL_ID) return;
            if (phase.Value != LobbyPhase.Selecting) return;
            if (!LobbyRules.CanStart(Snapshot())) return;

            ConnectionManager.Instance.CloseConnections();

            // As IAs ocupam as vagas livres só na hora de iniciar.
            List<LobbySlot> participants = Snapshot();
            int freeSlots = LobbyRules.CountFreeSlots(participants);
            int addedAIs = LobbyRules.FillWithAI(participants, catalog, NetworkManager.ServerClientId);

            if (addedAIs < freeSlots)
                Core.Logger.LogWarning($"Só {addedAIs} de {freeSlots} IA(s) foram adicionadas: faltam personagens livres com {nameof(CharacterCatalog.Entry.AIPrefab)} no {nameof(CharacterCatalog)}.");

            MatchSetup.Instance.Capture(participants);
            phase.Value = LobbyPhase.Starting;

            // O serviço carrega o grupo para todos os clients.
            ServiceLocator.Get<ISceneLoadingService>().LoadOnlineAsyncScene(Scenes.Playing);
        }

        #endregion

        #region ClientResponses

        [Rpc(SendTo.SpecifiedInParams)]
        private void JoinRejectedRpc(byte localId, SessionCallbackReason reason, RpcParams rpc = default)
        {
            OnJoinRejected?.Invoke(localId, reason);
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

        /// <summary>
        /// Abre o popup de saída. Quem chama garante que é o jogador local 0 e que ele não está pronto.
        /// </summary>
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

            popUp.SetMode(IsServer); // Texto diferente no host (fecha para todos) e no client (sai com todos os jogadores da máquina).

            // Quando a popup é aberta, se inscreve nos eventos da popup
            popUp.OnConfirmed += CloseQuitLobbyPopUp_OnConfirmed;
            popUp.OnCancelled += CloseQuitLobbyPopUp_OnCancelled;
            popUp.OnHidedPopup += CloseQuitLobbyPopUp_OnHidedPopup;
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
        }

        private void DisposePopupEvents()
        {
            if (popUp == null)
                return;

            popUp.OnConfirmed -= CloseQuitLobbyPopUp_OnConfirmed;
            popUp.OnCancelled -= CloseQuitLobbyPopUp_OnCancelled;
            popUp.OnHidedPopup -= CloseQuitLobbyPopUp_OnHidedPopup;
        }

        /// <summary>
        /// Fechar/Sair do lobby. No client, todos os jogadores desta máquina saem juntos (o server libera as vagas na desconexão).
        /// </summary>
        private void CloseLobby()
        {
            NetworkManager.Singleton.Shutdown(true);
        }

        #endregion

        #region Session

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

        #endregion
    }
}
