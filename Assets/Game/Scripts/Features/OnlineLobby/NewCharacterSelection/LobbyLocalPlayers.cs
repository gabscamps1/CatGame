using CatGame.Core;
using CatGame.Core.Enums;
using CatGame.Core.Events;
using CatGame.Core.Interfaces;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CatGame.Features.CharacterSelection
{
    /// <summary>
    /// Jogadores locais desta máquina no lobby.
    /// </summary>
    public sealed class LobbyLocalPlayers : IDisposable
    {
        private sealed class LocalPlayer
        {
            public byte LocalId;
            public bool IsPending; // Pediu a vaga e ainda não recebeu resposta ("Entrando...").
            public bool IsWaitingDevice; // Está na vaga, sem dispositivo.
            public InputDevice LastDevice; // Último dispositivo deste jogador (volta para ele ao reconectar).
            public SlotId Slot = SlotId.None;
            public LobbySlotInput Input;
        }

        private readonly ILobbyService lobby;
        private readonly ILobbyView lobbyView;
        private readonly IInputService inputService;
        private readonly IUIService uiService;
        private readonly float leaveHoldTime;
        private readonly float swapHoldTime;

        private readonly Dictionary<byte, LocalPlayer> players = new();

        private bool isLocked;
        private bool unlockNextTick;
        private bool isRejectionPopupOpen;
        private bool wasRejectionPopupShown;
        private InputDevice rejectedDevice; // Quem foi recusado também pode fechar a popup.

        private bool isRunning;

        public event Action PendingChanged;

        public int PendingCount
        {
            get
            {
                int count = 0;
                foreach (LocalPlayer player in players.Values)
                    if (player.IsPending) count++;

                return count;
            }
        }

        public LobbyLocalPlayers(ILobbyService lobby, ILobbyView lobbyView, float leaveHoldTime, float swapHoldTime)
        {
            this.lobby = lobby;
            this.lobbyView = lobbyView;
            this.leaveHoldTime = leaveHoldTime;
            this.swapHoldTime = swapHoldTime;

            inputService = ServiceLocator.Get<IInputService>();
            uiService = ServiceLocator.Get<IUIService>();
        }

        #region Lifecycle

        /// <summary>
        /// Chamar quando o lobby nasceu na rede. O jogador local 0 entra sozinho, com o dispositivo que navegou o menu.
        /// </summary>
        public void Begin()
        {
            if (isRunning)
                return;

            isRunning = true;

            inputService.EnterExclusiveMode();

            inputService.OnFreeDeviceSubmitted += InputService_OnFreeDeviceSubmitted;
            inputService.OnAssignedDeviceLost += InputService_OnAssignedDeviceLost;
            lobby.Changed += Lobby_Changed;
            lobby.OnJoinRejected += Lobby_OnJoinRejected;
            uiService.OnCurrentUIChanged += UiService_OnCurrentUIChanged;

            InputDevice mainDevice = inputService.GetInputFromLocalPlayer(LobbyLocalPlayerRules.MAIN_LOCAL_ID).CurrentDevice;
            RequestJoin(LobbyLocalPlayerRules.MAIN_LOCAL_ID, mainDevice);
        }

        /// <summary>
        /// Os dispositivos voltam ao padrão do menu no fim da sessão.
        /// </summary>
        public void Dispose()
        {
            if (!isRunning)
                return;

            isRunning = false;

            inputService.OnFreeDeviceSubmitted -= InputService_OnFreeDeviceSubmitted;
            inputService.OnAssignedDeviceLost -= InputService_OnAssignedDeviceLost;
            lobby.Changed -= Lobby_Changed;
            lobby.OnJoinRejected -= Lobby_OnJoinRejected;
            uiService.OnCurrentUIChanged -= UiService_OnCurrentUIChanged;

            foreach (LocalPlayer player in players.Values)
                DisposeInput(player);

            players.Clear();
        }

        public void Update()
        {
            if (!isRunning)
                return;

            if (unlockNextTick)
            {
                unlockNextTick = false;
                SetLocked(false);
            }

            float now = Time.unscaledTime;

            // Evita erro ao liberar a vaga e remover o jogador no meio do loop.
            foreach (LocalPlayer player in new List<LocalPlayer>(players.Values))
                player.Input?.Update(now);
        }

        #endregion

        #region Join

        private void RequestJoin(byte localId, InputDevice device)
        {
            if (device != null)
                inputService.AssignDeviceExclusive(localId, device); // Usa o dispositivo usado para entrar no lobby.

            players[localId] = new LocalPlayer
            {
                LocalId = localId,
                IsPending = true,
                LastDevice = device,
                IsWaitingDevice = device == null, // Caso o despositivo desconecte enquanto está entrando, fica aguardando o dispositivo.
            };

            lobby.RequestJoin(localId, GetDeviceKind(device));
            PendingChanged?.Invoke();
        }

        private void InputService_OnFreeDeviceSubmitted(InputDevice device)
        {
            if (isRejectionPopupOpen)
            {
                if (device == rejectedDevice)
                    CloseRejectionPopup();
                return;
            }

            if (isLocked)
                return;

            // Prioridade: uma vaga em espera deste client é preenchida antes de criar um jogador novo.
            int waitingLocalId = LobbyLocalPlayerRules.PickWaitingLocalId(device, GetWaitingLastDevices());
            if (waitingLocalId >= 0)
            {
                ResumeWithDevice(players[(byte)waitingLocalId], device);
                return;
            }

            int guestLocalId = LobbyLocalPlayerRules.NextGuestLocalId(players.Keys, inputService.LocalPlayerCount);
            if (guestLocalId < 0)
                return; // Sem controllers livres nesta máquina.

            RequestJoin((byte)guestLocalId, device);
        }

        private void Lobby_OnJoinRejected(byte localId, SessionCallbackReason reason)
        {
            if (!players.TryGetValue(localId, out LocalPlayer player) || !player.IsPending)
                return;

            rejectedDevice = player.LastDevice;
            inputService.UnassignDevices(localId); // O dispositivo continua livre.
            players.Remove(localId);
            PendingChanged?.Invoke();

            isRejectionPopupOpen = true;
            wasRejectionPopupShown = false;
            SetLocked(true);
            EventBus.Publish(new LobbyJoinRejectedEvent(reason));
        }

        private void CloseRejectionPopup()
        {
            if (uiService.IsVisible(PanelType.SessionMessagePopup))
                uiService.Close(PanelType.SessionMessagePopup);
        }

        #endregion

        #region Server State

        /// <summary>
        /// Cria o input da vaga e libera quem saiu da vaga.
        /// </summary>
        private void Lobby_Changed()
        {
            bool pendingChanged = false;
            List<byte> removed = null;

            // Faz uma cópia para evitar erros, porque no host o RPC roda na hora e pode disparar este método de novo no meio do loop.
            foreach (LocalPlayer player in new List<LocalPlayer>(players.Values))
            {
                bool hasSlot = lobby.TryGetLocalSlot(player.LocalId, out SlotId slot);

                if (player.IsPending)
                {
                    if (!hasSlot) continue;

                    player.IsPending = false;
                    player.Slot = slot;
                    pendingChanged = true;
                    CreateInput(player);

                    // O dispositivo caiu enquanto o pedido estava em andamento.
                    if (player.IsWaitingDevice)
                        lobby.SetWaitingDevice(slot);

                    continue;
                }

                if (!hasSlot)
                {
                    // A vaga foi liberada, o dispositivo fica livre para entrar de novo.
                    (removed ??= new List<byte>()).Add(player.LocalId);
                    continue;
                }

                player.Slot = slot;
            }

            if (removed != null)
            {
                foreach (byte localId in removed)
                {
                    if (!players.ContainsKey(localId)) continue;

                    DisposeInput(players[localId]);
                    inputService.UnassignDevices(localId);
                    players.Remove(localId);
                }
            }

            if (pendingChanged)
                PendingChanged?.Invoke();
        }

        #endregion

        #region Slot Input

        private void CreateInput(LocalPlayer player)
        {
            DisposeInput(player);

            LobbySlotInput slotInput = new LobbySlotInput();
            IPlayerInputController controller = inputService.GetInputFromLocalPlayer(player.LocalId);
            SlotId slot = player.Slot;

            // O jogador principal de cada client não usa tempo de segurar, porque ele sai pela popup.
            float holdToLeave = player.LocalId == LobbyLocalPlayerRules.MAIN_LOCAL_ID ? 0f : leaveHoldTime;

            slotInput.Bind(controller, lobbyView.GetSlotView(slot.Value), () => lobby.GetSlot(slot).Ready, holdToLeave, swapHoldTime);
            slotInput.CancelHeld += () => OnCancelHeld(player);
            slotInput.SwapHeld += () => OnSwapHeld(player);

            if (isLocked)
                slotInput.DisableInputsToThisSlot();

            player.Input = slotInput;
        }

        private void DisposeInput(LocalPlayer player)
        {
            player.Input?.Dispose();
            player.Input = null;
        }

        private void OnCancelHeld(LocalPlayer player)
        {
            if (player.IsWaitingDevice)
                return;

            // O jogador principal de cada client abre o popup de saída.
            // O Convidado somente deixa a vaga livre.
            if (player.LocalId == LobbyLocalPlayerRules.MAIN_LOCAL_ID)
                lobby.RequestCloseLobby();
            else
                lobby.LeaveSlot(player.Slot);
        }

        #endregion

        #region Devices

        private void OnSwapHeld(LocalPlayer player)
        {
            if (player.IsWaitingDevice || lobby.GetSlot(player.Slot).Ready)
                return;

            // O dispositivo antigo fica livre (e pode ser o mesmo que volta).
            inputService.UnassignDevices(player.LocalId);
            EnterWaiting(player);
        }

        private void InputService_OnAssignedDeviceLost(int localIndex, InputDevice device)
        {
            if (device is Mouse)
                return; // O mouse acompanha o teclado.

            if (!players.TryGetValue((byte)localIndex, out LocalPlayer player))
                return;

            if (player.IsPending)
            {
                player.IsWaitingDevice = true; // Ao ser aceito, entra aguardando dispositivo.
                return;
            }

            EnterWaiting(player);
        }

        private void EnterWaiting(LocalPlayer player)
        {
            player.IsWaitingDevice = true;
            player.Input?.DisableInputsToThisSlot();
            lobby.SetWaitingDevice(player.Slot);
        }

        private void ResumeWithDevice(LocalPlayer player, InputDevice device)
        {
            inputService.AssignDeviceExclusive(player.LocalId, device);

            player.IsWaitingDevice = false;
            player.LastDevice = device;

            if (player.IsPending)
                return; // Está decidindo se este client poderá entrar.

            lobby.ResumeWithDevice(player.Slot, GetDeviceKind(device));

            if (!isLocked)
                player.Input?.EnableInputToThisSlot();
        }

        private Dictionary<byte, InputDevice> GetWaitingLastDevices()
        {
            var waiting = new Dictionary<byte, InputDevice>();

            foreach (LocalPlayer player in players.Values)
                if (player.IsWaitingDevice) waiting[player.LocalId] = player.LastDevice;

            return waiting;
        }

        private static DeviceKind GetDeviceKind(InputDevice device)
        {
            return device is Gamepad ? DeviceKind.Gamepad : DeviceKind.Keyboard;
        }

        #endregion

        #region Popups

        private void UiService_OnCurrentUIChanged(PanelType? current)
        {
            if (isRejectionPopupOpen)
            {
                if (uiService.IsVisible(PanelType.SessionMessagePopup))
                {
                    wasRejectionPopupShown = true;
                }
                else if (wasRejectionPopupShown)
                {
                    isRejectionPopupOpen = false;
                    rejectedDevice = null;
                }
            }

            bool shouldLock = isRejectionPopupOpen || uiService.IsVisible(PanelType.QuitLobbyPopup);

            if (shouldLock)
            {
                unlockNextTick = false;
                SetLocked(true);
            }
            else if (isLocked)
            {
                unlockNextTick = true; // Espera 1 frame: o toque que fechou a popup não chega ao lobby.
            }
        }

        private void SetLocked(bool locked)
        {
            isLocked = locked;

            foreach (LocalPlayer player in players.Values)
            {
                if (player.Input == null) continue;

                if (locked || player.IsWaitingDevice)
                    player.Input.DisableInputsToThisSlot();
                else
                    player.Input.EnableInputToThisSlot();
            }
        }

        #endregion
    }
}
