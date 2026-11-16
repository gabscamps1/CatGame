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

        private void ConfirmButton_OnSubmittedEvent(object sender, NavigableElement.SubmittedEvent e)
        {       
            uiService.Close(PanelType.SessionMessagePopup);
            uiNavigationService.PopGroup(playerId, navigationGroup);
        }

        protected override void OnDestroy()
        {
            EventBus.Unsubscribe<SessionEndedEvent>(SessionEndedEvent);
            confirmButton.OnSubmittedEvent -= ConfirmButton_OnSubmittedEvent;
            base.OnDestroy();
        }

        private void SessionEndedEvent(SessionEndedEvent e)
        {
            switch (e.Reason)
            {
                case SessionCallbackReason.ConnectionError:
                    ConnectionError();
                    sessionMessage.text = localizationService.GetTextByID(connectionErrorMessageID);
                    break;

                case SessionCallbackReason.TryingConnnecting:
                    TryingConnectingMode();
                    sessionMessage.text = localizationService.GetTextByID(tryingConnectMessageID);
                    break;

                default:
                    sessionMessage.text = null;
                    Core.Logger.LogError($"[{nameof(SessionMessageUI)}] Callback da sessão não foi adicionado a popup");
                    break;
            }
        }

        private void TryingConnectingMode()
        {
            confirmButton.gameObject.SetActive(false);
            sessionMessage.rectTransform.position = withoutInteractionTextPosition.position;
            spinnerObject.SetActive(true);
        }

        private void ConnectionError()
        {
            sessionMessage.rectTransform.position = interactionTextPosition.position;
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