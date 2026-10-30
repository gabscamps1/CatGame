using CatGame.Core;
using CatGame.Core.Data;
using CatGame.Core.Interfaces;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CatGame.Features.CharacterSelection
{
    public sealed class LobbySlotInput : IDisposable
    {
        private LobbySlotView view;
        private IPlayerInputController playerInput;

        private bool inputEnabled = true;

        public void Bind(LobbySlotView view)
        {
            Unbind();
            this.view = view;

            PlayerId playerOne = new PlayerId(0);
            playerInput = ServiceLocator.Get<IInputService>().GetInputFromPlayer(playerOne);           

            playerInput.Navigation.performed += PlayerInput_OnCycle;
            playerInput.OnSubmitted += PlayerInput_OnSubmitted;
            playerInput.OnCancelled += PlayerInput_OnCancelled;
        }

        public void Unbind()
        {
            if (playerInput != null)
            {
                playerInput.Navigation.performed -= PlayerInput_OnCycle;
                playerInput.OnSubmitted -= PlayerInput_OnSubmitted;
                playerInput.OnCancelled -= PlayerInput_OnCancelled;
            }          

            playerInput = null;
            view = null;
        }

        private void PlayerInput_OnCycle(InputAction.CallbackContext ctx) 
        {
            if (view == null || !inputEnabled) return;

            int direction = (int)ctx.ReadValue<Vector2>().x;
            view.Cycle(direction);
        }

        public void DisableInputsToThisSlot()
        {
            inputEnabled = false;
        }

        public void EnableInputToThisSlot()
        {
            inputEnabled = true;
        }

        private void PlayerInput_OnSubmitted() { if (view != null && inputEnabled) view.Confirm(); }
        private void PlayerInput_OnCancelled() { if (view != null && inputEnabled) view.Cancel(); }

        public void Dispose()
        {
            Unbind();
        }
    }

}
