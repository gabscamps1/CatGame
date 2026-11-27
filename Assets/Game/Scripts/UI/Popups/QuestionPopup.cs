using CatGame.Capabilities.UISystem;
using CatGame.Core;
using CatGame.Core.Data;
using CatGame.Core.Enums;
using CatGame.Core.Interfaces;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace CatGame.UI
{
    /// <summary>
    /// Popup com uma mensagem e os botões de confirmar e cancelar. Somente o jogador local 0 navega.
    /// Cada popup define a mensagem e o que acontece ao confirmar ou cancelar.
    /// </summary>
    public abstract class QuestionPopup : BasePanel
    {
        [Header("Buttons")]
        [SerializeField] private ButtonElement confirmButton;
        [SerializeField] private ButtonElement cancelButton;

        [Header("Message")]
        [SerializeField] private TextMeshProUGUI messageText;

        [Header("Sounds")]
        [FormerlySerializedAs("UI_POPUP")]
        [SerializeField] private AudioData popupSound;

        public event Action OnConfirmed;
        public event Action OnCancelled;
        public event Action OnHidedPopup;

        protected readonly PlayerId playerId = new PlayerId(0);

        private INavigationGroup navigationGroup;
        private IAudioService audioService;
        private ILocalizationService localizationService;

        private bool firstTime = true;
        private int shownFrame = -1;

        #region Unity

        protected virtual void Awake()
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

            audioService.PlaySFXClip(popupSound);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (localizationService != null)
                localizationService.OnLocalizationChanged -= RefreshMessage;

            confirmButton.OnSubmittedEvent -= ConfirmButton_OnSubmittedEvent;
            cancelButton.OnSubmittedEvent -= CancelButton_OnSubmittedEvent;
            navigationGroup.OnCancelRequested -= NavigationGroup_OnCancelRequested;
        }

        #endregion

        #region Message

        /// <summary>
        /// ID do texto que aparece na popup.
        /// </summary>
        protected abstract string GetMessageID();

        protected void RefreshMessage()
        {
            if (messageText != null)
                messageText.text = localizationService.GetTextByID(GetMessageID());
        }

        #endregion

        #region Buttons

        private void ConfirmButton_OnSubmittedEvent(object sender, ButtonElement.SubmittedEvent e)
        {
            if (CanReceiveInput(e.PlayerId))
                OnConfirmed?.Invoke();
        }

        private void CancelButton_OnSubmittedEvent(object sender, ButtonElement.SubmittedEvent e)
        {
            if (CanReceiveInput(e.PlayerId))
                OnCancelled?.Invoke();
        }

        private void NavigationGroup_OnCancelRequested(object sender, PlayerId e)
        {
            if (CanReceiveInput(e))
                OnCancelled?.Invoke();
        }

        /// <summary>
        /// Somente o jogador local 0 responde a popup.
        /// O toque que abriu a popup não pode respondê-la no mesmo frame.
        /// </summary>
        private bool CanReceiveInput(PlayerId sender)
        {
            return sender.Id == playerId.Id && Time.frameCount != shownFrame;
        }

        #endregion

        #region Show And Hide

        protected override void OnBeforeShow()
        {
            RefreshMessage();
        }

        protected override void OnAfterShow()
        {
            shownFrame = Time.frameCount;

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

        #endregion
    }
}
