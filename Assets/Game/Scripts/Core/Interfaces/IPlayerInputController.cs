using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace CatGame.Core.Interfaces
{
    public interface IPlayerInputController
    {
        public event Action OnThrow;

        // Game Inputs.
        public InputAction Move { get; }


        // UI Inputs.
        public InputAction Navigation { get;}
        public InputAction SkipCutscene { get; }
        public InputAction Cancel { get; }
        public InputAction SwapDevice { get; }
        public event Action<int> OnTabNavigation;
        public event Action OnSubmitted;
        public event Action OnCancelled;

        public void Update();

        // Device.
        public event Action<InputDevice> OnCurrentDeviceChanged;
        public InputDevice CurrentDevice { get; }
        public IReadOnlyCollection<InputDevice> AssignedDevices { get; }
        public void AssignDevice(InputDevice device);
        public void UnassignDevice(InputDevice device);
        public void UnassignDevices();
        public bool HasAssignDevice();
        public bool HasDevice(InputDevice device);

        // Input Activation.
        public void SwitchToGame();
        public void SwitchToUI();
        public void DisableAllActions();
    }
}
