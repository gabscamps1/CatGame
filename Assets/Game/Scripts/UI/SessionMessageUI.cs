using CatGame.Capabilities.UISystem;
using CatGame.Core;
using CatGame.Core.Data;
using CatGame.Core.Enums;
using CatGame.Core.Events;
using CatGame.Core.Interfaces;
using TMPro;
using UnityEngine;

namespace CatGame.UI
{
    public class SessionMessageUI : BasePanel
    {
        [SerializeField] private TextMeshProUGUI sessionMessage;

        [Header("Interaction")]
        [SerializeField] private RectTransform interactionTextPosition;
        [SerializeField] private ButtonElement confirmButton;
        
        [Header("Without Interaction")]
        [SerializeField] private RectTransform withoutInteractionTextPosition;
        [SerializeField] private GameObject spinnerObject;

        [Header("Text IDs")]
        [SerializeField] private string connectionErrorMessageID;
        [SerializeField] private string tryingConnectMessageID;
        [SerializeField] private string lobbyFullMessageID;
        [SerializeField] private string ipErrorMessageID;
        [SerializeField] private string portErrorMessageID;

        // Navegação
        private IUIService uiService;
        private IUINavigationService uiNavigationService;
        private INavigationGroup navigationGroup;
        private readonly PlayerId playerId = new PlayerId(0);

        // Mensagem
        private ILocalizationService localizationService;

        private void Awake()
        {
            uiService = ServiceLocator.Get<IUIService>();
            uiNavigationService = ServiceLocator.Get<IUINavigationService>();
            localizationService = ServiceLocator.Get<ILocalizationService>();

            navigationGroup = new NavigationGroup(NavigationMode.Shared);
            navigationGroup.Register(confirmButton, true);

            EventBus.Subscribe<SessionEndedEvent>(SessionEndedEvent);
            confirmButton.OnSubmittedEvent += ConfirmButton_OnSubmittedEvent;
        }

        private void ConfirmButton_OnSubmittedEvent(object sender, ButtonElement.SubmittedEvent e)
        {       
            if (uiService.IsVisible(PanelType.SessionMessagePopup))
                uiService.Close(PanelType.SessionMessagePopup);
        }

        protected override void OnDestroy()
        {
            EventBus.Unsubscribe<SessionEndedEvent>(SessionEndedEvent);
            confirmButton.OnSubmittedEvent -= ConfirmButton_OnSubmittedEvent;
            base.OnDestroy();
        }

        private void SessionEndedEvent(SessionEndedEvent e)
        {
            bool openPopup = true;

            switch (e.Reason)
            {
                case SessionCallbackReason.ConnectionError when e.WasServer == false:
                    MessageWithConfirm();
                    sessionMessage.text = localizationService.GetTextByID(connectionErrorMessageID);
                    break;

                case SessionCallbackReason.TryingConnnecting:
                    TryingConnectingMode();
                    sessionMessage.text = localizationService.GetTextByID(tryingConnectMessageID);
                    break;

                case SessionCallbackReason.FullLobby:
                    MessageWithConfirm();
                    sessionMessage.text = localizationService.GetTextByID(lobbyFullMessageID);
                    break;

                case SessionCallbackReason.IpError:
                    MessageWithConfirm();
                    sessionMessage.text = localizationService.GetTextByID(ipErrorMessageID);
                    break;

                case SessionCallbackReason.PortError:
                    MessageWithConfirm();
                    sessionMessage.text = localizationService.GetTextByID(portErrorMessageID);
                    break;

                default:
                    sessionMessage.text = null;
                    Core.Logger.LogError($"[{nameof(SessionMessageUI)}] Callback da sessão não foi adicionado a popup");
                    openPopup = false;
                    break;
            }

            if (openPopup)
                uiService.Push(PanelType.SessionMessagePopup);
        }

        private void TryingConnectingMode()
        {
            sessionMessage.rectTransform.position = withoutInteractionTextPosition.position;
            confirmButton.gameObject.SetActive(false);
            confirmButton.SetOptionInteractable(false);
            spinnerObject.SetActive(true);
        }

        private void MessageWithConfirm()
        {
            sessionMessage.rectTransform.position = interactionTextPosition.position;
            confirmButton.SetOptionInteractable(true);
            confirmButton.gameObject.SetActive(true);
            spinnerObject.SetActive(false);
        }

        protected override void OnAfterShow()
        {
            uiNavigationService.PushGroup(playerId, navigationGroup, false);
        }

        protected override void OnAfterHide()
        {
            INavigationGroup activeGroup = uiNavigationService.GetActiveGroup(playerId);

            // Garante que somente esse grupo seja fechado.
            if (activeGroup != null && activeGroup == navigationGroup)
            {
                uiNavigationService.PopGroup(playerId, navigationGroup);
            }
        }
    }
}