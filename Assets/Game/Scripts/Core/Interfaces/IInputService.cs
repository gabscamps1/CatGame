using CatGame.Core.Enums;
using System;
using UnityEngine.InputSystem;

namespace CatGame.Core.Interfaces
{
    public interface IInputService
    {
        #region Players

        public int LocalPlayerCount { get; }
        public IPlayerInputController GetInputFromLocalPlayer(int localIndex);

        #endregion

        #region Activation

        public void SwitchToGame();
        public void SwitchToUI();
        public void DisableAllActions();

        #endregion

        #region Devices

        public DeviceAssignmentMode AssignmentMode { get; }

        /// <summary>
        /// Disparado quando um dispositivo atribuído a um jogador local desconecta. (índice local, dispositivo)
        /// </summary>
        public event Action<int, InputDevice> OnAssignedDeviceLost;

        /// <summary>
        /// Só no modo Exclusive: um dispositivo sem dono apertou o Submit.
        /// </summary>
        public event Action<InputDevice> OnFreeDeviceSubmitted;

        public void EnterExclusiveMode();
        public void RestoreMenuDevices();
        public bool AssignDeviceExclusive(int localIndex, InputDevice device);
        public void UnassignDevices(int localIndex);
        public bool TryGetLocalPlayerOfDevice(InputDevice device, out int localIndex);

        #endregion
    }
}
