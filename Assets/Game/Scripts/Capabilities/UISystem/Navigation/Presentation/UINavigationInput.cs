using CatGame.Core;
using CatGame.Core.Enums;
using CatGame.Core.Interfaces;
using UnityEngine;

namespace CatGame.Capabilities.UISystem
{
    public class UINavigationInput : MonoBehaviour
    {
        [Header("Player")]
        [SerializeField] private PlayerId playerId;

        [Header("Repeat OnHold Settings")]
        [SerializeField] private float initialDelay = 0.4f;
        [SerializeField] private float repeatInterval = 0.12f;

        private NavigationDirection? heldDirection;
        private float nextRepeatTime;

        private IUINavigationService uiNavigationService;
        private IPlayerInputController playerInputController;

        private void Start()
        {
            uiNavigationService = ServiceLocator.Get<IUINavigationService>();

            playerInputController = ServiceLocator.Get<IInputService>().GetInputFromPlayer(playerId);

            playerInputController.OnSubmitted += PlayerInputController_OnSubmitted;
            playerInputController.OnCancelled += PlayerInputController_OnCancelled;
        }

        private void OnDestroy()
        {
            playerInputController.OnSubmitted -= PlayerInputController_OnSubmitted;
            playerInputController.OnCancelled -= PlayerInputController_OnCancelled;
        }

        private void Update()
        {       
            HandleDirectionalInput();                
        }

        private void PlayerInputController_OnSubmitted()
        {
            uiNavigationService.Submit(playerId);
        }

        private void PlayerInputController_OnCancelled()
        {
            uiNavigationService.Cancel(playerId);
            Core.Logger.Log("TryCancel");
        }

        private void HandleDirectionalInput()
        {
            NavigationDirection? pressedNow = ReadDirection();

            if (pressedNow.HasValue && pressedNow != heldDirection)
            {
                heldDirection = pressedNow;
                nextRepeatTime = Time.unscaledTime + initialDelay;
                uiNavigationService.Navigate(playerId, pressedNow.Value);
                return;
            }

            if (!pressedNow.HasValue)
            {
                heldDirection = null;
                return;
            }

            if (Time.unscaledTime >= nextRepeatTime)
            {
                Core.Logger.Log("TryNavigate");
                nextRepeatTime = Time.unscaledTime + repeatInterval;
                uiNavigationService.Navigate(playerId, pressedNow.Value);
            }
        }

        private NavigationDirection? ReadDirection()
        {
            Vector2 navigateDirection = playerInputController.Navigation.ReadValue<Vector2>();

            float horizontal = navigateDirection.x;
            float vertical = navigateDirection.y;

            if (vertical > 0.5f) return NavigationDirection.Up;
            if (vertical < -0.5f) return NavigationDirection.Down;
            if (horizontal < -0.5f) return NavigationDirection.Left;
            if (horizontal > 0.5f) return NavigationDirection.Right;

            return null;
        }
    }
}