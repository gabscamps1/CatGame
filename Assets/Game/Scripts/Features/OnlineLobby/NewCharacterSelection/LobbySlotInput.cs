using CatGame.Core.Interfaces;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CatGame.Features.CharacterSelection
{
    /// <summary>
    /// Liga o input de um jogador local à view da vaga dele.
    /// </summary>
    public sealed class LobbySlotInput : IDisposable
    {
        private LobbySlotView view;
        private IPlayerInputController playerInput;
        private Func<bool> isReady;

        private float leaveHoldTime;
        private float swapHoldTime;

        private bool inputEnabled = true;

        // Hold.
        private HoldState cancelHold;
        private HoldState swapHold;
        private bool isHoldingCycle;

        public event Action CancelHeld; // Segurou cancelar sem estar pronto.
        public event Action SwapHeld; // Segurou trocar dispositivo sem estar pronto.

        private struct HoldState
        {
            public bool IsHolding;
            public bool Fired;
            public float StartTime;
        }

        #region Bind

        public void Bind(IPlayerInputController playerInput, LobbySlotView view, Func<bool> isReady, float leaveHoldTime, float swapHoldTime)
        {
            Unbind();

            this.playerInput = playerInput;
            this.view = view;
            this.isReady = isReady;
            this.leaveHoldTime = leaveHoldTime;
            this.swapHoldTime = swapHoldTime;

            playerInput.Navigation.performed += PlayerInput_OnCycle;
            playerInput.Navigation.canceled += Navigation_Canceled;
            playerInput.OnSubmitted += PlayerInput_OnSubmitted;
            playerInput.Cancel.started += CancelAction_Started;
            playerInput.Cancel.canceled += CancelAction_Canceled;
            playerInput.SwapDevice.started += SwapAction_Started;
            playerInput.SwapDevice.canceled += SwapAction_Canceled;
        }

        public void Unbind()
        {
            if (playerInput != null)
            {
                playerInput.Navigation.performed -= PlayerInput_OnCycle;
                playerInput.Navigation.canceled -= Navigation_Canceled;
                playerInput.OnSubmitted -= PlayerInput_OnSubmitted;
                playerInput.Cancel.started -= CancelAction_Started;
                playerInput.Cancel.canceled -= CancelAction_Canceled;
                playerInput.SwapDevice.started -= SwapAction_Started;
                playerInput.SwapDevice.canceled -= SwapAction_Canceled;
            }

            playerInput = null;
            view = null;
            isReady = null;
            cancelHold = default;
            swapHold = default;
        }

        public void Dispose()
        {
            Unbind();
        }

        #endregion

        #region Activation

        public void DisableInputsToThisSlot()
        {
            inputEnabled = false;
            cancelHold = default;
            swapHold = default;
        }

        public void EnableInputToThisSlot()
        {
            inputEnabled = true;
        }

        #endregion

        #region Tick

        /// <summary>
        /// Chamado todo frame pelo LobbyLocalPlayers para medir o tempo segurando.
        /// </summary>
        public void Update(float now)
        {
            if (!inputEnabled || view == null)
                return;

            if (TryFire(ref cancelHold, leaveHoldTime, now))
                CancelHeld?.Invoke();

            if (TryFire(ref swapHold, swapHoldTime, now))
                SwapHeld?.Invoke();
        }

        private static bool TryFire(ref HoldState hold, float holdTime, float now)
        {
            if (!hold.IsHolding || hold.Fired)
                return false;

            if (now - hold.StartTime < holdTime)
                return false;

            hold.Fired = true; // Dispara uma vez por toque.
            return true;
        }

        #endregion

        #region Input Callbacks

        private void PlayerInput_OnCycle(InputAction.CallbackContext ctx)
        {
            if (view == null || !inputEnabled || isHoldingCycle) return;

            int direction = (int)ctx.ReadValue<Vector2>().x;
            view.Cycle(direction);

            isHoldingCycle = true;
        }

        private void Navigation_Canceled(InputAction.CallbackContext ctx)
        {
            isHoldingCycle = false;
        }

        private void PlayerInput_OnSubmitted()
        {
            if (view != null && inputEnabled)
                view.Confirm();
        }

        private void CancelAction_Started(InputAction.CallbackContext ctx)
        {
            if (view == null || !inputEnabled) return;

            // Pronto: cancelar só desmarca o ready. Esse mesmo toque não conta para sair da vaga.
            if (IsReady())
            {
                view.Cancel();
                cancelHold = default;
                return;
            }

            cancelHold = new HoldState { IsHolding = true, StartTime = Time.unscaledTime };
            Update(Time.unscaledTime); // Com tempo 0, dispara no mesmo toque.
        }

        private void CancelAction_Canceled(InputAction.CallbackContext ctx)
        {
            cancelHold = default;
        }

        private void SwapAction_Started(InputAction.CallbackContext ctx)
        {
            if (view == null || !inputEnabled) return;
            if (IsReady()) return; // Só troca de dispositivo quem não está pronto.

            swapHold = new HoldState { IsHolding = true, StartTime = Time.unscaledTime };
            Update(Time.unscaledTime);
        }

        private void SwapAction_Canceled(InputAction.CallbackContext ctx)
        {
            swapHold = default;
        }

        private bool IsReady()
        {
            return isReady != null && isReady();
        }

        #endregion
    }
}
