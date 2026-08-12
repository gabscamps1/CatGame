using CatGame.Core;
using CatGame.Core.Enums;
using CatGame.Core.Interfaces;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Logger = CatGame.Core.Logger;

namespace CatGame.Services.Input
{
    [DefaultExecutionOrder(-50)]
    public class InputActionsService : MonoBehaviour, IInputService
    {
        public static InputActionsService Instance => inputService;
        private static InputActionsService inputService;

        [SerializeField] private InputActionAsset inputActionsAsset;

        private readonly Dictionary<PlayerId, PlayerInputController> inputControllers = new();
        private readonly HashSet<InputDevice> assignedDevices = new HashSet<InputDevice>();

        private IGameStateService gameStateService;

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

            InputActionAsset playerOneInputAsset = Instantiate(inputActionsAsset);          
            InputActionAsset playerTwoInputAsset = Instantiate(inputActionsAsset);

            PlayerInputController playerInputManager1 = new PlayerInputController(playerOneInputAsset);
            PlayerInputController playerInputManager2 = new PlayerInputController(playerTwoInputAsset);

            inputControllers.Add(PlayerId.P1, playerInputManager1);
            inputControllers.Add(PlayerId.P2, playerInputManager2);

            AssignDeviceToNextAvailablePlayer(Keyboard.current);            
            GetInput(PlayerId.P1).AssignDevice(Mouse.current);            
        }


        private void Update()
        {
            UpdateInputs();
            AssignUnusedGamepads();
        }

        private void UpdateInputs()
        {
            foreach (IPlayerInputController input in inputControllers.Values)
            {
                input.Update();
            }
        }

        private void Start()
        {
            gameStateService = ServiceLocator.Get<IGameStateService>();
            gameStateService.OnStateChanged += GameStateService_OnStateChanged;
        }

        private void OnDestroy()
        {
            gameStateService.OnStateChanged -= GameStateService_OnStateChanged;
        }

        private void GameStateService_OnStateChanged(GameState beforeState, GameState currentState)
        {
            switch (currentState)
            {
                case GameState.Playing:
                    SwitchToGame();
                    break;

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

        #region Devices

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

            foreach (IPlayerInputController input in inputControllers.Values)
            {
                if (input.HasAssignDevice())
                    assignedDevices.Add(input.CurrentDevice);
            }
        }

        private static bool WasAnyButtonPressedThisFrame(Gamepad gamepad)
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
            foreach (IPlayerInputController player in inputControllers.Values)
            {
                if (player.HasAssignDevice() && player.CurrentDevice is Gamepad)
                    continue;

                player.AssignDevice(device);
                assignedDevices.Add(device);
                return;
            }
        }

        #endregion

        #region Public Functions
     
        public void SwitchToGame()
        {
            foreach (var pair in inputControllers)
            {
                pair.Value.SwitchToGame();
            }
        }

        public void SwitchToUI()
        {
            foreach (var pair in inputControllers)
            {
                pair.Value.SwitchToUI();
            }
        }

        public void DisableAllActions()
        {
            foreach (var pair in inputControllers)
            {
                pair.Value.DisableAllActions();
            }
        }
        
        public IPlayerInputController GetInput(PlayerId playerId)
        {
            if (inputControllers.TryGetValue(playerId, out PlayerInputController playerInputController))          
                return playerInputController;

            return null;
        }

        #endregion
    }
}

