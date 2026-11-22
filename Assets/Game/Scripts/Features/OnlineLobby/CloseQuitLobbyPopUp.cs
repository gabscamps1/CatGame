using CatGame.Capabilities.UISystem;
using CatGame.Core;
using CatGame.Core.Interfaces;
using CatGame.Core.Data;
using CatGame.Core.Enums;
using System;
using TMPro;
using UnityEngine;

namespace CatGame.Features.OnlineLobby
{
    public class CloseQuitLobbyPopUp : BasePanel
    {
        [SerializeField] private ButtonElement confirmButton;
        [SerializeField] private ButtonElement cancelButton;

        [Header("Message")]
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private string hostMessageID = "LOBBY_QUIT_HOST";
        [SerializeField] private string clientMessageID = "LOBBY_QUIT_CLIENT";

        public event Action OnConfirmed;
        public event Action OnCancelled;
        public event Action OnHidedPopup;

        [Header("Sounds")]
        [SerializeField] private AudioData UI_POPUP;

        private IAudioService audioService;

        private readonly PlayerId playerId = new PlayerId(0);
        INavigationGroup navigationGroup;

        private bool firstTime = true;
        private bool isHostMode;
        private ILocalizationService localizationService;

        private void Awake()
        {
            audioService = ServiceLocator.Get<IAudioService>();
            localizationService = ServiceLocator.Get<ILocalizationService>();
            localizationService.OnLocalizationChanged += RefreshMessage;

            navigationGroup = new NavigationGroup(NavigationMode.Shared);
            navigationGroup.Register(confirmButton, true);
            navigationGroup.Register(cancelButton);

            confirmButton.OnSubmittedEvent += ConfirmButton_OnSubmittedEvent;
            cancelButton.OnSubmittedEvent += CancelButton_OnSubmittedEvent;
            navigationGroup.OnCancelRequested += NavigationGroup_OnCancelRequested;
        }

        private void OnEnable()
        {
            // Interrompe na primeira vez que é chamado, pois a primeira vez é apenas para rodar o Start da class BasePanel.
            if (firstTime)
            {
                firstTime = false;
                return;
            }

            audioService.PlaySFXClip(UI_POPUP);
        }

        protected override void OnDestroy()
        {
            if (localizationService != null)
                localizationService.OnLocalizationChanged -= RefreshMessage;

            confirmButton.OnSubmittedEvent -= ConfirmButton_OnSubmittedEvent;
            cancelButton.OnSubmittedEvent -= CancelButton_OnSubmittedEvent;
            navigationGroup.OnCancelRequested -= NavigationGroup_OnCancelRequested;
        }

        /// <summary>
        /// Host: o lobby fecha para todos. Client: todos os jogadores desta máquina saem juntos.
        /// </summary>
        public void SetMode(bool isHost)
        {
            isHostMode = isHost;
            RefreshMessage();
        }

        private void RefreshMessage()
        {
            if (messageText != null)
                messageText.text = localizationService.GetTextByID(isHostMode ? hostMessageID : clientMessageID);
        }

        private void ConfirmButton_OnSubmittedEvent(object sender, ButtonElement.SubmittedEvent e)
        {
            // Se não for o player 1, não navega.
            if (e.PlayerId.Id != 0)
                return;

            OnConfirmed?.Invoke();
        }

        private void CancelButton_OnSubmittedEvent(object sender, ButtonElement.SubmittedEvent e)
        {
            // Se não for o player 1, não navega.
            if (e.PlayerId.Id != 0)
                return;

            OnCancelled?.Invoke();
        }

        private void NavigationGroup_OnCancelRequested(object sender, PlayerId e)
        {
            // Se não for o player 1, não navega.
            if (e.Id != 0)
                return;

            OnCancelled?.Invoke();
        }

        protected override void OnAfterShow()
        {
            IUINavigationService uiNavigationService = ServiceLocator.Get<IUINavigationService>();
            uiNavigationService.PushGroup(playerId, navigationGroup, false); // Somente o jogador 1 navega.
        }

        protected override void OnAfterHide()
        {
            IUINavigationService uiNavigationService = ServiceLocator.Get<IUINavigationService>();
            INavigationGroup activeGroup = uiNavigationService.GetActiveGroup(playerId);

            // Garante que somente esse grupo seja fechado.
            if (activeGroup != null && activeGroup == navigationGroup)
            {
                uiNavigationService.PopGroup(playerId, navigationGroup);
                activeGroup.ResetFocusToDefault(playerId);

                OnHidedPopup?.Invoke();
            }            
        }
    }
}
