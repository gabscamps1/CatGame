using System;
using UnityEngine.InputSystem;
using CatGame.Core.Interfaces;
using UnityEngine.InputSystem.Users;
using UnityEngine;

namespace CatGame.Services.Input
{
    public class PlayerInputController : IPlayerInputController
    {
        public bool IsGameInputEnabled { get; private set; }
        public bool IsUIInputEnabled { get; private set; }

        public InputDevice CurrentDevice { get; private set; }

        private const string PLAYER_MAP = "Player";
        private const string UI_MAP = "UI";

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

        public event Action OnSubmitted;
        public event Action OnCancelled;

        private readonly InputAction navigateAction;
        private readonly InputAction submitAction;      
        private readonly InputAction cancelAction;      

     
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

            submitAction.started += SubmitAction_Started;
            cancelAction.started += CancelAction_started;


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
            
        }

        #region Game Inputs

        private void Attack_Started(InputAction.CallbackContext context) => OnThrow?.Invoke();

        #endregion

        #region UI Inputs

        private void SubmitAction_Started(InputAction.CallbackContext context) => OnSubmitted?.Invoke();
        private void CancelAction_started(InputAction.CallbackContext obj) => OnCancelled?.Invoke();

        #endregion

        #region Devices

        public void AssignDevice(InputDevice device)
        {
            InputUser.PerformPairingWithDevice(device, inputUser);
            CurrentDevice = device;
        }

        public void UnassignDevice()
        {
            inputUser.UnpairDevices();
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