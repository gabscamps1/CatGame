using CatGame.Capabilities.UISystem;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace CatGame.Features.CharacterSelection
{
    public class LobbyRoot : MonoBehaviour
    {
        [SerializeField] private LobbyView lobbyView;
        [SerializeField] private OnlineLobbyModel onlineLobbyModel;
        [SerializeField] private ButtonElement createOnlineRoomBtn;
        [SerializeField] private int maxPlayers;

        [Header("Hold Times")]
        [SerializeField, Min(0), Tooltip("Tempo segurando um determinado botão para um convidado sair da vaga.")]
        private float leaveSlotHoldTime = 0f;
        [SerializeField, Min(0), Tooltip("Tempo segurando um determinado botão para trocar de dispositivo.")]
        private float swapDeviceHoldTime = 0f;

        private ILobbyService lobbyService;
        private LobbyPresenter lobbyPresenter;
        private LobbyLocalPlayers localPlayers;

        #region Unity

        private void Awake()
        {
            createOnlineRoomBtn.OnSubmittedEvent += CreateOnlineRoomBtn_OnSubmittedEvent;
        }

        private void Update()
        {
            localPlayers?.Update();
        }

        private void OnDestroy()
        {
            ReleaseLobby();

            createOnlineRoomBtn.OnSubmittedEvent -= CreateOnlineRoomBtn_OnSubmittedEvent;
            onlineLobbyModel.OnOwnerDisconected -= OnlineLobbyModel_OnOwnerDisconected;
        }

        #endregion

        #region Online

        private void CreateOnlineRoomBtn_OnSubmittedEvent(object sender, ButtonElement.SubmittedEvent e)
        {
            OpenOnlineHost();
        }

        private void OpenOnlineHost()
        {
            if (NetworkManager.Singleton.ShutdownInProgress)
            {
                Core.Logger.LogWarning("A sessão anterior ainda está sendo encerrada");
                return;
            }

            if (NetworkManager.Singleton.IsListening)
            {
                Core.Logger.LogWarning("Server já estava criado");
                return;
            }

            BeginOnline();

            if (onlineLobbyModel == null)
                return;

            onlineLobbyModel.CreateLobby();
        }

        public void OpenOnlineClient()
        {
            BeginOnline();
        }

        private void BeginOnline()
        {
            onlineLobbyModel.Init(maxPlayers);
            StartCoroutine(BindWhenOnline());
        }

        private IEnumerator BindWhenOnline()
        {
            yield return new WaitUntil(() => onlineLobbyModel.IsSpawned);

            CreatePresenter(onlineLobbyModel);
            onlineLobbyModel.OnOwnerDisconected += OnlineLobbyModel_OnOwnerDisconected;
        }

        private void OnlineLobbyModel_OnOwnerDisconected()
        {
            ReleaseLobby();

            onlineLobbyModel.OnOwnerDisconected -= OnlineLobbyModel_OnOwnerDisconected;
        }

        #endregion

        #region Lobby

        private void CreatePresenter(ILobbyService lobbyModel)
        {
            ReleaseLobby();

            lobbyService = lobbyModel;
            lobbyPresenter = new LobbyPresenter(lobbyService, lobbyView);

            localPlayers = new LobbyLocalPlayers(lobbyService, lobbyView, leaveSlotHoldTime, swapDeviceHoldTime);
            localPlayers.PendingChanged += LocalPlayers_PendingChanged;
            localPlayers.Begin();
        }

        private void LocalPlayers_PendingChanged()
        {
            lobbyPresenter?.SetPendingJoins(localPlayers.PendingCount);
        }

        /// <summary>
        /// Chamar ao sair do lobby para bloquear os inputs do lobby.
        /// </summary>
        public void ReleaseLobby()
        {
            if (localPlayers != null)
            {
                localPlayers.PendingChanged -= LocalPlayers_PendingChanged;
                localPlayers.Dispose();
                localPlayers = null;
            }

            lobbyPresenter?.Dispose();
            lobbyPresenter = null;
            lobbyService = null;
        }

        #endregion
    }
}
