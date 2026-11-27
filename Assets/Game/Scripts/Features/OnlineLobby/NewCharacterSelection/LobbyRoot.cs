using CatGame.Capabilities.UISystem;
using CatGame.Core;
using CatGame.Core.Enums;
using CatGame.Core.Interfaces;
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

        [Header("Return From Match")]
        [SerializeField] private MenuController menuController;
        [SerializeField, Tooltip("Índice do menu do lobby no MenuController.")]
        private int lobbyMenuIndex;
        [SerializeField, Tooltip("Menus que o voltar percorre a partir do lobby, do primeiro ao último. O último é para onde o voltar leva.")]
        private int[] lobbyMenuHistory;

        private ILobbyService lobbyService;
        private LobbyPresenter lobbyPresenter;
        private LobbyLocalPlayers localPlayers;

        #region Unity

        private void Awake()
        {
            createOnlineRoomBtn.OnSubmittedEvent += CreateOnlineRoomBtn_OnSubmittedEvent;
        }

        private void Start()
        {
            // A cena só abre com a sessão ativa quando o host trouxe todos de volta da partida.
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                OpenRestored();
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
            StartCoroutine(BindWhenOnline(false));
        }

        private IEnumerator BindWhenOnline(bool isRestored)
        {
            yield return new WaitUntil(() => onlineLobbyModel.IsSpawned);

            CreatePresenter(onlineLobbyModel, isRestored);
            onlineLobbyModel.OnOwnerDisconected += OnlineLobbyModel_OnOwnerDisconected;
        }

        /// <summary>
        /// Volta da partida. O lobby abre direto, com as vagas da última partida.
        /// </summary>
        private void OpenRestored()
        {
            onlineLobbyModel.Init(maxPlayers);
            StartCoroutine(OpenRestoredWhenReady());
        }

        private IEnumerator OpenRestoredWhenReady()
        {
            ISceneLoadingService sceneLoadingService = ServiceLocator.Get<ISceneLoadingService>();
            IUIService uiService = ServiceLocator.Get<IUIService>();

            // Espera o loading terminar e o menu principal aparecer, igual ao caminho normal até o lobby.
            yield return new WaitUntil(() => !sceneLoadingService.IsLoading && uiService.IsVisible(PanelType.MainMenu));

            if (menuController != null)
                menuController.OpenMenuWithHistory(lobbyMenuIndex, lobbyMenuHistory);
            else
                Core.Logger.LogWarning($"[{nameof(LobbyRoot)}] {nameof(menuController)} não foi configurado, o lobby restaurado não vai abrir.");

            yield return BindWhenOnline(true);
        }

        private void OnlineLobbyModel_OnOwnerDisconected()
        {
            ReleaseLobby();

            onlineLobbyModel.OnOwnerDisconected -= OnlineLobbyModel_OnOwnerDisconected;
        }

        #endregion

        #region Lobby

        private void CreatePresenter(ILobbyService lobbyModel, bool isRestored)
        {
            ReleaseLobby();

            lobbyService = lobbyModel;
            lobbyPresenter = new LobbyPresenter(lobbyService, lobbyView);

            localPlayers = new LobbyLocalPlayers(lobbyService, lobbyView, leaveSlotHoldTime, swapDeviceHoldTime);
            localPlayers.PendingChanged += LocalPlayers_PendingChanged;
            localPlayers.Begin(isRestored);
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
