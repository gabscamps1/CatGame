using CatGame.Core;
using CatGame.Core.Enums;
using CatGame.Core.Events;
using CatGame.Core.Interfaces;
using Unity.Netcode;
using UnityEngine;

namespace CatGame.Services.Session
{
    /// <summary>
    /// Responsável pelo fim da sessão online. Independente do motivo (saída, queda do host, kick, falha no loading),
    /// a resposta é sempre a mesma: se a sessão saiu do lobby, volta ao menu principal.
    /// </summary>
    [DisallowMultipleComponent]
    public class SessionEndHandler : MonoBehaviour
    {
        private ISceneLoadingService sceneLoadingService;
        private NetworkManager networkManager;

        private bool isSessionActive;
        private bool hasSessionChangedScenes; // A sessão carregou algum grupo, ou seja, saiu do lobby.

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

            networkManager.OnServerStarted += NetworkManager_OnSessionStarted;
            networkManager.OnClientStarted += NetworkManager_OnSessionStarted;
            networkManager.OnServerStopped += NetworkManager_OnServerStopped;
            networkManager.OnClientStopped += NetworkManager_OnClientStopped;
        }

        private void OnDestroy()
        {
            if (sceneLoadingService != null)
                sceneLoadingService.OnLoadingChanged -= SceneLoadingService_OnLoadingChanged;

            if (networkManager != null)
            {
                networkManager.OnServerStarted -= NetworkManager_OnSessionStarted;
                networkManager.OnClientStarted -= NetworkManager_OnSessionStarted;
                networkManager.OnServerStopped -= NetworkManager_OnServerStopped;
                networkManager.OnClientStopped -= NetworkManager_OnClientStopped;
            }
        }

        #endregion

        #region Session

        private void NetworkManager_OnSessionStarted()
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

        private void NetworkManager_OnServerStopped(bool wasClient)
        {
            EndSession(true);
        }

        private void SceneLoadingService_OnLoadingChanged(object sender, LoadingChangedEvent e)
        {
            if (isSessionActive && e.IsLoading)
                hasSessionChangedScenes = true;
        }

        private void EndSession(bool wasServer)
        {
            // No host os dois eventos de parada disparam: trata uma vez só.
            if (!isSessionActive)
                return;

            isSessionActive = false;

            string reason = networkManager.DisconnectReason;
            bool isReturningToMenu = hasSessionChangedScenes;

            // Terminou no lobby: o próprio lobby trata a saída. Fora dele, retorna ao menu principal recarregando a cena.
            if (isReturningToMenu)
                sceneLoadingService.LoadAsyncScene(Scenes.MainMenu, true);

            EventBus.Publish(new SessionEndedEvent(wasServer, isReturningToMenu, reason));
        }

        #endregion
    }
}
