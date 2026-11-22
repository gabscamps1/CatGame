using CatGame.Core;
using CatGame.Core.Enums;
using CatGame.Core.Interfaces;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Logger = CatGame.Core.Logger;

namespace CatGame.Services.Input
{
    [DefaultExecutionOrder(-50)]
    public class InputActionsService : MonoBehaviour, IInputService
    {
        private const string UI_MAP = "UI";
        private const string SUBMIT_ACTION = "Submit";

        public static InputActionsService Instance => inputService;
        private static InputActionsService inputService;

        [SerializeField] private InputActionAsset inputActionsAsset;

        [SerializeField, Min(1), Tooltip("Quantidade de jogadores locais. No lobby e na partida, cada um tem um dispositivo só.")]
        private int localPlayerCount = 4;

        private PlayerInputController[] inputControllers;
        private readonly HashSet<InputDevice> assignedDevices = new HashSet<InputDevice>();

        // Cópia do asset. Somente serve pra ouvir a action de Submit de todos os dispositivos para achar quem quer entrar.
        private InputActionAsset freeDevicesAsset;
        private InputAction freeDeviceSubmitAction;

        private IGameStateService gameStateService;

        public int LocalPlayerCount => inputControllers.Length;
        public DeviceAssignmentMode AssignmentMode { get; private set; } = DeviceAssignmentMode.Shared;

        public event Action<int, InputDevice> OnAssignedDeviceLost;
        public event Action<InputDevice> OnFreeDeviceSubmitted;

        #region Unity

        private void Awake()
        {
            if (inputService == null)
            {
                inputService = this;
            }
            else if (inputService != this)
            {
                Logger.LogWarning($"InputActionsService duplicado: {gameObject.name} & {inputService.name}");
                Destroy(gameObject);
                return;
            }

            CreateControllers();
            CreateFreeDevicesListener();
            AssignMenuDevices(null);

            InputSystem.onDeviceChange += InputSystem_OnDeviceChange;
        }

        private void Start()
        {
            gameStateService = ServiceLocator.Get<IGameStateService>();
            gameStateService.OnStateChanged += GameStateService_OnStateChanged;
        }

        private void Update()
        {
            UpdateInputs();

            if (AssignmentMode == DeviceAssignmentMode.Shared)
                AssignUnusedGamepads();
        }

        private void OnDestroy()
        {
            if (inputService != this)
                return;

            InputSystem.onDeviceChange -= InputSystem_OnDeviceChange;

            if (gameStateService != null)
                gameStateService.OnStateChanged -= GameStateService_OnStateChanged;

            if (freeDeviceSubmitAction != null)
            {
                freeDeviceSubmitAction.performed -= FreeDeviceSubmitAction_Performed;
                freeDeviceSubmitAction.Disable();
            }

            if (freeDevicesAsset != null)
                Destroy(freeDevicesAsset);
        }

        #endregion

        #region Setup

        private void CreateControllers()
        {
            inputControllers = new PlayerInputController[localPlayerCount];

            for (int i = 0; i < localPlayerCount; i++)
            {
                InputActionAsset playerInputAsset = Instantiate(inputActionsAsset);
                inputControllers[i] = new PlayerInputController(playerInputAsset);
            }
        }

        private void CreateFreeDevicesListener()
        {
            freeDevicesAsset = Instantiate(inputActionsAsset);
            freeDeviceSubmitAction = freeDevicesAsset.FindActionMap(UI_MAP).FindAction(SUBMIT_ACTION);
            freeDeviceSubmitAction.performed += FreeDeviceSubmitAction_Performed;
        }

        private void UpdateInputs()
        {
            foreach (IPlayerInputController input in inputControllers)
                input.Update();
        }

        #endregion

        #region GameState

        private void GameStateService_OnStateChanged(GameState beforeState, GameState currentState)
        {
            switch (currentState)
            {
                case GameState.Playing:
                    SwitchToGame();
                    break;

                case GameState.Cutscene:
                case GameState.MainMenu:
                case GameState.Paused:
                    SwitchToUI();
                    break;

                case GameState.Loading:
                    DisableAllActions();
                    break;

                default:
                    Logger.LogWarning("[InputActionsService] Estado não foi configurado.");
                    break;
            }
        }

        #endregion

        #region Devices - Modes

        public void EnterExclusiveMode()
        {
            if (AssignmentMode == DeviceAssignmentMode.Exclusive)
                return;

            // O jogador local 0 fica só com o dispositivo que está usando (o que navegou o menu).
            InputDevice deviceInUse = inputControllers[0].CurrentDevice ?? Keyboard.current;

            UnassignAllDevices();
            AssignDeviceExclusive(0, deviceInUse);

            AssignmentMode = DeviceAssignmentMode.Exclusive;
            freeDeviceSubmitAction.Enable();
        }

        public void RestoreMenuDevices()
        {
            InputDevice deviceInUse = inputControllers[0].CurrentDevice;

            freeDeviceSubmitAction.Disable();
            AssignmentMode = DeviceAssignmentMode.Shared;

            UnassignAllDevices();
            AssignMenuDevices(deviceInUse);
        }

        /// <summary>
        /// Menu: o jogador local 0 fica com mouse, teclado e o controle que estava usando (se houver).
        /// O último atribuído vira o CurrentDevice, por isso o dispositivo em uso é atribuído por último.
        /// </summary>
        private void AssignMenuDevices(InputDevice deviceInUse)
        {
            PlayerInputController playerOne = inputControllers[0];

            if (Mouse.current != null) playerOne.AssignDevice(Mouse.current);
            if (Keyboard.current != null) playerOne.AssignDevice(Keyboard.current);

            if (deviceInUse is Gamepad gamepad && gamepad.added)
                playerOne.AssignDevice(gamepad);
        }

        #endregion

        #region Devices - Assignment

        public bool AssignDeviceExclusive(int localIndex, InputDevice device)
        {
            if (!IsValidLocalIndex(localIndex) || device == null)
                return false;

            // O teclado e o mouse são um jogador só: o mouse vai junto e é atribuído antes, para o teclado virar o CurrentDevice.
            if (device is Keyboard && Mouse.current != null)
                MoveDevice(localIndex, Mouse.current);

            MoveDevice(localIndex, device);
            return true;
        }

        public void UnassignDevices(int localIndex)
        {
            if (IsValidLocalIndex(localIndex))
                inputControllers[localIndex].UnassignDevices();
        }

        public bool TryGetLocalPlayerOfDevice(InputDevice device, out int localIndex)
        {
            for (int i = 0; i < inputControllers.Length; i++)
            {
                if (inputControllers[i].HasDevice(device))
                {
                    localIndex = i;
                    return true;
                }
            }

            localIndex = -1;
            return false;
        }

        /// <summary>
        /// Tira o dispositivo de qualquer outro jogador e atribui a este.
        /// </summary>
        private void MoveDevice(int localIndex, InputDevice device)
        {
            for (int i = 0; i < inputControllers.Length; i++)
            {
                if (i != localIndex)
                    inputControllers[i].UnassignDevice(device);
            }

            inputControllers[localIndex].AssignDevice(device);
        }

        private void UnassignAllDevices()
        {
            foreach (PlayerInputController controller in inputControllers)
                controller.UnassignDevices();
        }

        private bool IsValidLocalIndex(int localIndex)
        {
            return localIndex >= 0 && localIndex < inputControllers.Length;
        }

        #endregion

        #region Devices - Shared Mode

        private void AssignUnusedGamepads()
        {
            CacheAssignedDevices();

            foreach (Gamepad gamepad in Gamepad.all)
            {
                if (assignedDevices.Contains(gamepad))
                    continue;

                if (!WasAnyButtonPressedThisFrame(gamepad))
                    continue;

                AssignDeviceToNextAvailablePlayer(gamepad);
            }
        }

        private void CacheAssignedDevices()
        {
            assignedDevices.Clear();

            foreach (IPlayerInputController input in inputControllers)
            {
                if (input.HasAssignDevice())
                    assignedDevices.Add(input.CurrentDevice);
            }
        }

        private bool WasAnyButtonPressedThisFrame(Gamepad gamepad)
        {
            return gamepad.rightShoulder.wasPressedThisFrame ||
                   gamepad.leftShoulder.wasPressedThisFrame ||
                   gamepad.rightTrigger.wasPressedThisFrame ||
                   gamepad.leftTrigger.wasPressedThisFrame ||
                   gamepad.buttonSouth.wasPressedThisFrame ||
                   gamepad.buttonNorth.wasPressedThisFrame ||
                   gamepad.buttonWest.wasPressedThisFrame ||
                   gamepad.buttonEast.wasPressedThisFrame ||
                   gamepad.leftStickButton.wasPressedThisFrame ||
                   gamepad.rightStickButton.wasPressedThisFrame ||
                   gamepad.dpad.magnitude != 0;
        }

        private void AssignDeviceToNextAvailablePlayer(InputDevice device)
        {
            foreach (IPlayerInputController player in inputControllers)
            {
                if (player.HasAssignDevice() && player.CurrentDevice is Gamepad)
                    continue;

                player.AssignDevice(device);
                assignedDevices.Add(device);
                return;
            }
        }

        #endregion

        #region Devices - Exclusive Mode

        private void FreeDeviceSubmitAction_Performed(InputAction.CallbackContext context)
        {
            if (AssignmentMode != DeviceAssignmentMode.Exclusive)
                return;

            InputDevice device = context.control.device;

            // O mouse acompanha o teclado.
            if (device is Mouse)
                return;

            if (TryGetLocalPlayerOfDevice(device, out _))
                return; // Se já tem dono, o input vai para o jogador dele.

            OnFreeDeviceSubmitted?.Invoke(device);
        }

        private void InputSystem_OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (change != InputDeviceChange.Disconnected && change != InputDeviceChange.Removed)
                return;

            if (!TryGetLocalPlayerOfDevice(device, out int localIndex))
                return;

            inputControllers[localIndex].UnassignDevice(device);
            OnAssignedDeviceLost?.Invoke(localIndex, device);
        }

        #endregion

        #region Public Functions

        public void SwitchToGame()
        {
            foreach (PlayerInputController controller in inputControllers)
                controller.SwitchToGame();
        }

        public void SwitchToUI()
        {
            foreach (PlayerInputController controller in inputControllers)
                controller.SwitchToUI();
        }

        public void DisableAllActions()
        {
            foreach (PlayerInputController controller in inputControllers)
                controller.DisableAllActions();
        }

        public IPlayerInputController GetInputFromLocalPlayer(int localIndex)
        {
            return IsValidLocalIndex(localIndex) ? inputControllers[localIndex] : null;
        }

        #endregion
    }
}
