using CatGame.UI;
using UnityEngine;

namespace CatGame.Features.OnlineLobby
{
    public class CloseQuitLobbyPopUp : QuestionPopup
    {
        [Header("Message IDs")]
        [SerializeField] private string hostMessageID = "LOBBY_QUIT_HOST";
        [SerializeField] private string clientMessageID = "LOBBY_QUIT_CLIENT";

        private bool isHostMode;

        /// <summary>
        /// Host: o lobby fecha para todos. Client: todos os jogadores desta máquina saem juntos.
        /// </summary>
        public void SetMode(bool isHost)
        {
            isHostMode = isHost;
            RefreshMessage();
        }

        protected override string GetMessageID()
        {
            return isHostMode ? hostMessageID : clientMessageID;
        }
    }
}
