using System;
using UnityEngine.InputSystem;
using CatGame.Core.Interfaces;
using UnityEngine.InputSystem.Users;
using System.Collections.Generic;
using System.Linq;

namespace CatGame.Services.Input
{
    public class PlayerInputController : IPlayerInputController
    {
        private const string PLAYER_MAP = "Player";
        private const string UI_MAP = "UI";

        // Input Activation.
        public bool IsGameInputEnabled { get; private set; }
        public bool IsUIInputEnabled { get; private set; }

        // Devices.
        public event Action<InputDevice> OnCurrentDeviceChanged;     
        public InputDevice CurrentDevice { get; private set; }
        private readonly HashSet<InputDevice> assignedDevices = new();


        // Action Maps.
        private readonly InputActionMap gameMap;
        private readonly InputActionMap uiMap;

        // Game Inputs.
        public InputAction Move => move;
        public event Action OnThrow;

        private readonly InputAction move;
        private readonly InputAction throwFruit;

        // UI Inputs.
        public InputAction Navigation => navigateAction;
        public InputAction SkipCutscene => skipCutsceneAction;
        public event Action<int> OnTabNavigation;
        public event Action OnSubmitted;
        public event Action OnCancelled;

        private readonly InputAction navigateAction;
        private readonly InputAction submitAction;      
        private readonly InputAction cancelAction;      
        private readonly InputAction tabNavigationAction;      
        private readonly InputAction skipCutsceneAction;      

     
        private readonly InputUser inputUser;

        public PlayerInputController(InputActionAsset inputActionsAsset)       
        {
            inputUser = InputUser.CreateUserWithoutPairedDevices();
            inputUser.AssociateActionsWithUser(inputActionsAsset);

            gameMap = inputActionsAsset.FindActionMap(PLAYER_MAP);
            uiMap = inputActionsAsset.FindActionMap(UI_MAP);

            // Game Inputs.
            move = inputActionsAsset.FindActionMap(PLAYER_MAP).FindAction("Move");
            throwFruit = inputActionsAsset.FindActionMap(PLAYER_MAP).FindAction("Throw");

            throwFruit.started += Attack_Started;

            // UI Inputs.
            navigateAction = inputActionsAsset.FindActionMap(UI_MAP).FindAction("Navigation");
            submitAction = inputActionsAsset.FindActionMap(UI_MAP).FindAction("Submit");
            cancelAction = inputActionsAsset.FindActionMap(UI_MAP).FindAction("Cancel");
            tabNavigationAction = inputActionsAsset.FindActionMap(UI_MAP).FindAction("TabNavigation");
            skipCutsceneAction = inputActionsAsset.FindActionMap(UI_MAP).FindAction("SkipCutscene");

            submitAction.started += SubmitAction_Started;
            cancelAction.started += CancelAction_Started;
            tabNavigationAction.started += TabNavigationAction_Started;

            SwitchToGame();
        }

        ~PlayerInputController()
        {
            // Game Inputs.
            throwFruit.started -= Attack_Started;

            // UI Inputs.
            submitAction.started -= SubmitAction_Started;
        }
 
        public void Update()
        {
            if (CheckCurrentDeviceChange(out InputDevice currentDevice))
                ChangeCurrentDevice(currentDevice);
        }

        private bool CheckCurrentDeviceChange(out InputDevice newCurrentDevice)
        {
            newCurrentDevice = CurrentDevice;

            foreach (var device in assignedDevices)
            {
                if (device == CurrentDevice)
                    continue;

                switch (device)
                {
                    case Keyboard keyboard:
                        if (keyboard.anyKey.wasPressedThisFrame)
                            newCurrentDevice = device;
                        break;

                    case Gamepad gamepad:
                        if (WasAnyButtonPressedThisFrame(gamepad))
                            newCurrentDevice = device;
                        break;
                }
            }
            
            return newCurrentDevice != CurrentDevice;
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
                   Math.Abs(gamepad.leftStick.magnitude) > 0.3 ||
                   Math.Abs(gamepad.rightStick.magnitude) > 0.3 ||
                   Math.Abs(gamepad.dpad.magnitude) > 0.3;
        }

        #region Game Inputs

        private void Attack_Started(InputAction.CallbackContext context) => OnThrow?.Invoke();

        #endregion

        #region UI Inputs

        private void SubmitAction_Started(InputAction.CallbackContext context) => OnSubmitted?.Invoke();
        private void CancelAction_Started(InputAction.CallbackContext context) => OnCancelled?.Invoke();
        private void TabNavigationAction_Started(InputAction.CallbackContext context) => OnTabNavigation?.Invoke((int)context.ReadValue<float>());

        #endregion

        #region Devices

        /// <summary>
        /// Altera o currentDevice atual se o currentDevice já estiver conectado.
        /// </summary>
        private void ChangeCurrentDevice(InputDevice device)
        {
            if (!assignedDevices.Contains(device))
                return;

            CurrentDevice = device;
            OnCurrentDeviceChanged?.Invoke(device);
        }

        public void AssignDevice(InputDevice device)
        {
            if (!assignedDevices.Add(device))
                return;

            InputUser.PerformPairingWithDevice(device, inputUser);
            CurrentDevice = device;     
            OnCurrentDeviceChanged?.Invoke(device);
        }

        public void UnassignDevice(InputDevice device)
        {
            if (!assignedDevices.Remove(device))
                return;

            if (assignedDevices.Count == 0)
            {
                CurrentDevice = null;
                OnCurrentDeviceChanged?.Invoke(null);
                return;
            }

            inputUser.UnpairDevice(device);
            CurrentDevice = CurrentDevice == device
                ? assignedDevices.Last() // Se o currentDevice atual foi desconectado, troca o currentDevice atual.
                : CurrentDevice;

            OnCurrentDeviceChanged?.Invoke(CurrentDevice);
        }

        public void UnassignDevices()
        {
            inputUser.UnpairDevices();
            assignedDevices.Clear();
            CurrentDevice = null;
            OnCurrentDeviceChanged?.Invoke(CurrentDevice);
        }

        public bool HasAssignDevice()
        {
            return inputUser.pairedDevices.Count > 0;
        }

        #endregion

        #region Activation

        public void SwitchToGame()
        {
            gameMap.Enable();
            uiMap.Disable();

            IsGameInputEnabled = true;
            IsUIInputEnabled = false;
        }

        public void SwitchToUI()
        {
            uiMap.Enable();
            gameMap.Disable();

            IsGameInputEnabled = false;
            IsUIInputEnabled = true;
        }

        public void DisableAllActions()
        {
            uiMap.Disable();
            gameMap.Disable();

            IsGameInputEnabled = false;
            IsUIInputEnabled = false;
        }

        #endregion
    }
}