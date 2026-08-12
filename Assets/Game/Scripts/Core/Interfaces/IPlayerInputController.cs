using System;
using UnityEngine.InputSystem;
using UnityEngine;

namespace CatGame.Core.Interfaces
{
    public interface IPlayerInputController
    {
        public event Action OnAttacked;

        // Game Inputs.
        public InputAction Move { get; }
        public InputAction Acceleration { get; }


        // UI Inputs.
        public InputAction Navigation { get;}
        public event Action OnSubmitted;

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