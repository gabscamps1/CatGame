using System;
using UnityEngine.InputSystem;
using UnityEngine;

namespace CatGame.Core.Interfaces
{
    public interface IPlayerInputController
    {
        public event Action OnThrow;

        // Game Inputs.
        public InputAction Move { get; }


        // UI Inputs.
        public InputAction Navigation { get;}
        public event Action OnSubmitted;
        public event Action OnCancelled;

        public InputDevice CurrentDevice { get; }
        public void Update();
        public void AssignDevice(InputDevice device);
        public void UnassignDevice();
        public bool HasAssignDevice();
        public void SwitchToGame();
        public void SwitchToUI();
        public void DisableAllActions();
    }
}