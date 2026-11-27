using CatGame.Core;
using CatGame.Core.Data;
using CatGame.Core.Enums;
using CatGame.Core.Events;
using CatGame.Core.Interfaces;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace CatGame.Services.Session
{
    /// <summary>
    /// Responsável pelo fim da sessão online. 
    /// Independente do motivo, a resposta é sempre a mesma: se a sessão saiu do lobby, volta ao menu principal.
    /// </summary>
    [DisallowMultipleComponent]
    public class SessionEndHandler : MonoBehaviour
    {
        [SerializeField] private string conectionErrorMessageID;

        private ISceneLoadingService sceneLoadingService;
        private NetworkManager networkManager;

        private bool isSessionActive;
        private bool hasSessionChangedScenes; // A sessão está fora do lobby (carregou um grupo que não é o menu principal).

        #region Unity

        private void Start()
        {
            sceneLoadingService = ServiceLocator.Get<ISceneLoadingService>();
            sceneLoadingService.OnLoadingChanged += SceneLoadingService_OnLoadingChanged;

            networkManager = NetworkManager.Singleton;

            if (networkManager == null)
            {
                Core.Logger.LogError($"{nameof(SessionEndHandler)} não encontrou o {nameof(NetworkManager)}");
                return;
            }

            networkManager.OnServerStarted += NetworkManager_OnServerStarted;
            networkManager.OnClientStarted += NetworkManager_OnClientStarted;
            networkManager.OnServerStopped += NetworkManager_OnServerStopped;
            networkManager.OnClientStopped += NetworkManager_OnClientStopped;
            networkManager.OnClientDisconnectCallback += NetworkManager_OnClientDisconnectCallback;
            networkManager.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;
        }

        

        private void OnDestroy()
        {
            if (sceneLoadingService != null)
                sceneLoadingService.OnLoadingChanged -= SceneLoadingService_OnLoadingChanged;

            if (networkManager != null)
            {
                networkManager.OnServerStarted -= NetworkManager_OnServerStarted;
                networkManager.OnClientStarted -= NetworkManager_OnClientStarted;
                networkManager.OnServerStopped -= NetworkManager_OnServerStopped;
                networkManager.OnClientStopped -= NetworkManager_OnClientStopped;
                networkManager.OnClientDisconnectCallback -= NetworkManager_OnClientDisconnectCallback;
                networkManager.OnClientConnectedCallback -= NetworkManager_OnClientConnectedCallback;
            }
        }

        #endregion

        #region Session

        private void NetworkManager_OnClientStarted()
        {
            OnSessionStarted();

            if (networkManager.IsClient && !networkManager.IsHost)
                EventBus.Publish(new SessionEndedEvent(false, false, "", SessionCallbackReason.TryingConnnecting));
        }

        private void NetworkManager_OnClientConnectedCallback(ulong clientId)
        {
            // Só o client local que acabou de conectar (o host não passa pela tela de "conectando").
            if (networkManager.IsHost || clientId != networkManager.LocalClientId)
                return;

            EventBus.Publish(new SessionConnectedEvent());
        }

        private void NetworkManager_OnServerStarted()
        {
            OnSessionStarted();
        }

        private void OnSessionStarted()
        {
            // No host os dois eventos disparam.
            if (isSessionActive)
                return;
            
            isSessionActive = true;
            hasSessionChangedScenes = false;
        }

        private void NetworkManager_OnClientStopped(bool wasHost)
        {
            EndSession(wasHost);
        }

        private void NetworkManager_OnServerStopped(bool wasHost)
        {
            EndSession(true);
        }

        private void NetworkManager_OnClientDisconnectCallback(ulong client)
        {
            if (!NetworkManager.Singleton.IsServer)
                return;

            if (ServiceLocator.Get<IGameStateService>().CurrentState != GameState.Playing)
                return;

            IEnumerable<PlayerInfo> playersInfo = ServiceLocator.Get<IPlayerRegistry>().GetPlayersOfClient(client);

            // Caso um client seja desconectado, todos os players desse client perdem automaticamente.
            foreach (PlayerInfo playerInfo in playersInfo)
            {
                if (!GameManager.Instance.IsPlayerGameOver(playerInfo.playerId))
                    GameManager.Instance.PlayerGameOver(playerInfo.playerId);
            }
        }

        private void SceneLoadingService_OnLoadingChanged(object sender, LoadingChangedEvent e)
        {
            if (!isSessionActive)
                return;

            if (e.IsLoading)
                hasSessionChangedScenes = true;
            else if (e.SceneGroup == Scenes.MainMenu)
                hasSessionChangedScenes = false; // Voltou ao lobby pela partida, então a saída é tratada pelo próprio lobby.
        }

        private void EndSession(bool wasServer)
        {
            // No host os dois eventos de parada disparam: trata uma vez só.
            if (!isSessionActive)
                return;

            isSessionActive = false;

            string reason = networkManager.DisconnectReason;
            bool isReturningToMenu = hasSessionChangedScenes;

            // Os dispositivos voltam ao padrão do menu (o jogador local 0 com o dispositivo em uso, teclado e mouse).
            ServiceLocator.Get<IInputService>().RestoreMenuDevices();
            
            // Terminou no lobby: o próprio lobby trata a saída. Fora dele, retorna ao menu principal recarregando a cena.
            if (isReturningToMenu)
                sceneLoadingService.LoadAsyncScene(Scenes.MainMenu, true);
                        
            EventBus.Publish(new SessionEndedEvent(wasServer, isReturningToMenu, reason, ParseReason(reason)));
        }

        /// <summary>
        /// O server envia o motivo como o nome do enum. Sem motivo conhecido, é erro de conexão.
        /// </summary>
        private static SessionCallbackReason ParseReason(string reason)
        {
            if (!string.IsNullOrEmpty(reason) && Enum.TryParse(reason, out SessionCallbackReason parsed) && Enum.IsDefined(typeof(SessionCallbackReason), parsed))
                return parsed;

            return SessionCallbackReason.ConnectionError;
        }

        #endregion
    }
}
