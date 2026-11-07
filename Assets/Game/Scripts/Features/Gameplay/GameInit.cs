using CatGame.Core;
using CatGame.Core.Data;
using CatGame.Core.Interfaces;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CatGame.Features.Gameplay
{
    public class GameInit : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private string targetSceneName = "MainScene";
        [SerializeField] private int timeToInit;

        private void Awake()
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += NetworkManager_OnLoadEventCompleted;
        }

        private void OnDestroy()
        {
            if (NetworkManager.Singleton != null) 
                NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= NetworkManager_OnLoadEventCompleted;
        }

        private void NetworkManager_OnLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
        {
            if (!NetworkManager.Singleton.IsServer || sceneName != targetSceneName)
                return;

            // N�o libera a partida se algum cliente n�o terminou.
            if (clientsTimedOut.Count > 0)
            {
                Core.Logger.LogWarning("Nem todos os clientes terminaram de carregar a cena.");
                return;
            }

            Core.Logger.Log("Todos os clientes terminaram o carregamento da cena.");
            StartCoroutine(AllowPlayers());
        }

        private IEnumerator AllowPlayers()
        {
            if (!NetworkManager.Singleton.IsServer) 
                yield break;

            ServerMessageManager.Instance.ShowClientMessageRpc(MessageID.StartGame);
            yield return new WaitForSeconds(timeToInit);

            // Planejamento futuro caso a movimentação do player seja alterada para Server.
            /*switch (GameManager.Instance.GetGameState())
            {
                case GameManager.GameState.NotInit:
                    break;

                case GameManager.GameState.Playing:

                    IPlayerRegistry playerRegistry = ServiceLocator.Get<IPlayerRegistry>();

                    // Muda o Ownership para o client dono do player.
                    foreach (PlayerInfo player in playerRegistry.Players)
                    {
                        if (!playerRegistry.TryGetController(player.playerId, out PlayerController playerController))
                            continue;

                        if (!playerController.TryGetComponent(out NetworkObject obj))
                            continue;

                        obj.ChangeOwnership(player.clientId);
                    }

                    break;
            }*/

            GameManager.Instance.StartGame();
        }
    }
}
