using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CatGame.Features.Gameplay
{
    public class GameInit : MonoBehaviour
    {
        [Header("Configuração")]
        [SerializeField] private string targetSceneName = "MainScene";
        [SerializeField] private float timeToInit;

        private bool sceneReady;

        private void Awake()
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += NetworkManager_OnLoadEventCompleted;
        }

        private void OnDestroy()
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= NetworkManager_OnLoadEventCompleted;
        }

        private void NetworkManager_OnLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
        {
            if (!NetworkManager.Singleton.IsServer || sceneName != targetSceneName)
                return;

            // Não libera a partida se algum cliente não terminou.
            if (clientsTimedOut.Count > 0)
            {
                sceneReady = false;
                Core.Logger.LogWarning("Nem todos os clientes terminaram de carregar a cena.");
                return;
            }

            sceneReady = true;
            Core.Logger.Log("Todos os clientes terminaram o carregamento da cena.");
            GameManager.Instance.SetGameState(GameManager.GameState.Playing);
        }
    }
}
