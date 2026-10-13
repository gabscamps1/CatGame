using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using CatGame.Features.CharacterSelection;

namespace CatGame.Features.Gameplay
{
    public class MatchSpawner : NetworkBehaviour
    {
        [SerializeField] CharacterCatalog catalog;
        [SerializeField] Transform[] spawnPoints;
        [SerializeField] private BoxCollider2D[] glassBoudaries;

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;
            NetworkManager.SceneManager.OnLoadEventCompleted += OnSceneLoaded;
        }

        private void OnSceneLoaded(string scene, LoadSceneMode mode, List<ulong> completed, List<ulong> timedOut)
        {
            NetworkManager.SceneManager.OnLoadEventCompleted -= OnSceneLoaded;

            for (int i = 0; i < MatchSetup.Players.Count; i++)
            {
                LobbySlot slot = MatchSetup.Players[i];
                if (!catalog.TryGet(slot.CharacterId, out CharacterCatalog.Entry entry)) continue;

                Transform point = spawnPoints[i];
                NetworkObject obj = Instantiate(entry.Prefab, point.position, point.rotation);             
                
                obj.SpawnAsPlayerObject(slot.OwnerId);   // o dono é quem escolheu o personagem.

                BindPlayerInBoundary(obj, i);
            }
        }

        private void BindPlayerInBoundary(NetworkObject obj, int playerId)
        {
            Core.Logger.Log("Deu Init 1: " + playerId);
            if (!obj.TryGetComponent(out PlayerController player))
                return;

            Core.Logger.Log("Deu Init 2");
            if (glassBoudaries.Length == 0 || playerId >= glassBoudaries.Length)
                return;

            Core.Logger.Log("Deu Init 3");
            player.Init(glassBoudaries[playerId]);
        }
    }
}