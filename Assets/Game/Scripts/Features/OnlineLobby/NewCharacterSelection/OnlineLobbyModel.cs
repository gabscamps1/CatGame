using CatGame.Core;
using CatGame.Core.Enums;
using CatGame.Core.Interfaces;
using CatGame.Features.OnlineLobby;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
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

        [SerializeField] CharacterCatalog catalog;
        [SerializeField] bool allowDuplicateCharacters;
        [SerializeField] string gameScene = "MainScene";

        readonly NetworkList<LobbySlot> slots = new();
        readonly NetworkVariable<LobbyPhase> phase = new(LobbyPhase.Selecting);
        private LobbyPhase clientPhase = LobbyPhase.Selecting;
        readonly Dictionary<ulong, float> lastCommandTime = new();   // só no server

        public event Action Changed;
        public event Action OnNetworkSpawned;
        public event Action OnOwnerDisconected;

        public int SlotCount => slots.Count;
        public LobbySlot GetSlot(int i) => slots[i];

        private IUIService uiService;
        private CloseQuitLobbyPopUp popUp;

        public string Socket { get; private set; }

        private void Awake()
        {
            uiService = ServiceLocator.Get<IUIService>();
            uiService.OnCurrentUIChanged += UiService_OnCurrentUIChanged;
        }

        public override void OnDestroy()
        {
            uiService.OnCurrentUIChanged -= UiService_OnCurrentUIChanged;
            base.OnDestroy();
        }

        public bool CanControl(int slotIndex) 
        {
            return slotIndex >= 0 && slotIndex < slots.Count &&
                   slots[slotIndex].OwnerId == NetworkManager.LocalClientId;
        }


        public override void OnNetworkSpawn()
        {
            slots.OnListChanged += _ => Changed?.Invoke();
            phase.OnValueChanged += (_, _) => Changed?.Invoke();

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
            if (IsServer && NetworkManager != null)
            {
                NetworkManager.OnClientConnectedCallback -= AddSlotFor;
                NetworkManager.OnClientDisconnectCallback -= RemoveSlotFor;
            }
        }

        #region ServerSlots
       
        private void AddSlotFor(ulong clientId)
        {
            if (FindSlot(clientId) >= 0) return;

            ushort currentSlotNumber = (ushort)(slots.Count + 1); // Pega o slot que está sendo adicionado. Exemplo: Vai entrar um segundo player agora, ou seja, o slot ainda é igual a 1. slot[1] 
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

        #endregion

        #region ClientRequests     

        public void CycleCharacter(int slotIndex, int direction)
        {
            if (clientPhase != LobbyPhase.Selecting) return;
            if (!CanControl(slotIndex)) return;
            if (direction != -1 && direction != 1) return;
        }

        public void SetReady(int slotIndex, bool ready)
        {
            var networkManager = NetworkManager.Singleton;
            Debug.Log($"[Lobby] id={GetInstanceID()} spawned={IsSpawned} " +
                      $"listening={(networkManager != null && networkManager.IsListening)} " +
                      $"scene={gameObject.scene.name}", this);

            if (clientPhase != LobbyPhase.Selecting) return;
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
            Core.Logger.Log("TENTA DAR O CYCLE 5");

            var slots = Snapshot();
            ushort next = LobbyRules.NextAvailable(slots, slotIndex, direction, catalog, allowDuplicateCharacters, id => IsUnlocked(sender, id));
            if (next == 0 || next == slots[slotIndex].CharacterId) return;
            Core.Logger.Log("TENTA DAR O CYCLE 6: " + next);

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

            MatchSetup.Capture(Snapshot());
            phase.Value = LobbyPhase.Starting;
            NetworkManager.SceneManager.LoadScene(gameScene, LoadSceneMode.Single);
            ServiceLocator.Get<IGameStateService>().SetState(GameState.Playing);
        }

        #endregion

        #region LobbyCreation

        public void CreateLobby()
        {
            if (NetworkManager.Singleton.IsListening)
            {
                Debug.Log("Server já estava criado");
                return;
            }

            string ip = GetLocalIpAddress();
            int port = 1000;

            while (!IsPortAvaliable(port))
                port++;

            if (!NetworkManager.Singleton.TryGetComponent(out UnityTransport unityTransport))
            {
                Core.Logger.LogError($"Não foi encontrado o {nameof(NetworkTransport)} do {nameof(NetworkManager)}");
                return;
            }

            unityTransport.SetConnectionData(ip, (ushort)port);
            Socket = ip + ":" + port;

            NetworkManager.Singleton.StartHost();

            NetworkManager.Singleton.OnClientDisconnectCallback += Singleton_OnClientDisconnectCallback;
        }


        private string GetLocalIpAddress()
        {
            string localIPAddress = "127.0.0.1";

            // Pega todos os endereços de IPs presentes no PC.
            IPAddress[] ipsAddress = Dns.GetHostAddresses(Dns.GetHostName());

            if (ipsAddress == null || ipsAddress.Length == 0)
                return localIPAddress;

            foreach (IPAddress ip in ipsAddress)
            {
                // Procura pelo IPV4 dentre esses endereços.
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIPAddress = ip.ToString();
                    break;
                }
            }

            return localIPAddress;
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
            if (uiService.CurrentPanelType.Value == PanelType.QuitLobbyPopup)
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
            clientPhase = LobbyPhase.None;
        }

        private void CloseQuitLobbyPopUp_OnConfirmed()
        {
            if (!NetworkManager.Singleton.IsListening)
            {
                Core.Logger.LogWarning("Server já estava fechado");
                return;
            }

            CloseLobby();
            uiService.Pop();
            //clientPhase = LobbyPhase.Selecting;
        }

        private void CloseQuitLobbyPopUp_OnCancelled()
        {
            uiService.Pop();
            //clientPhase = LobbyPhase.Selecting;
        }

        /// <summary>
        /// Fechar/Sair do lobby
        /// </summary>
        private void CloseLobby()
        {
            NetworkManager.Singleton.Shutdown(true);
        }

        #endregion

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
            // Volta pro menu anterior quando o próprio Owner é desconectado.
            if (clientID == NetworkManager.Singleton.LocalClientId)
            {
                OnOwnerDisconected?.Invoke();
                NetworkManager.Singleton.OnClientDisconnectCallback -= Singleton_OnClientDisconnectCallback;
                return;
            }
        }
    }
}