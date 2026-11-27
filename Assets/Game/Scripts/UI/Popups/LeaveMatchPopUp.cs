using CatGame.Core;
using CatGame.Core.Enums;
using CatGame.Core.Interfaces;
using Unity.Netcode;
using UnityEngine;

namespace CatGame.UI
{
    /// <summary>
    /// Client no fim da partida. Confirmar sai da sessão e o SessionEndHandler leva ao menu principal.
    /// </summary>
    public class LeaveMatchPopUp : QuestionPopup
    {
        [Header("Message IDs")]
        [SerializeField] private string messageID = "MATCH_LEAVE_CLIENT";

        private IUIService uiService;

        protected override void Awake()
        {
            base.Awake();

            OnConfirmed += LeaveMatchPopUp_OnConfirmed;
            OnCancelled += LeaveMatchPopUp_OnCancelled;
        }

        protected override void Start()
        {
            base.Start();
            uiService = ServiceLocator.Get<IUIService>();
        }

        protected override void OnDestroy()
        {
            OnConfirmed -= LeaveMatchPopUp_OnConfirmed;
            OnCancelled -= LeaveMatchPopUp_OnCancelled;

            base.OnDestroy();
        }

        protected override string GetMessageID() => messageID;

        private void LeaveMatchPopUp_OnConfirmed()
        {
            if (!IsCurrentPanel())
                return;

            uiService.Pop();

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                NetworkManager.Singleton.Shutdown();
        }

        private void LeaveMatchPopUp_OnCancelled()
        {
            if (IsCurrentPanel())
                uiService.Pop();
        }

        private bool IsCurrentPanel()
        {
            return uiService.CurrentPanelType.HasValue && uiService.CurrentPanelType.Value == PanelType.LeaveMatchPopup;
        }
    }
}
