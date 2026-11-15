using CatGame.Features.CharacterSelection;
using CatGame.Services.Register;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using CatGame.Core.Data;
using CatGame.Core.Enums;
using CatGame.Core.Interfaces;

namespace CatGame.Features.Gameplay
{
    public class MatchSpawner : NetworkBehaviour
    {
        [SerializeField] private CharacterCatalog catalog;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private PlayerRosterSync roaster;

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;
            NetworkManager.SceneManager.OnLoadEventCompleted += OnSceneLoaded;
        }

        public override void OnNetworkDespawn()
        {
            if (!IsServer) return;

            NetworkManager.SceneManager.OnLoadEventCompleted -= OnSceneLoaded;
        }

        private void OnSceneLoaded(string scene, LoadSceneMode mode, List<ulong> completed, List<ulong> timedOut)
        {
            NetworkManager.SceneManager.OnLoadEventCompleted -= OnSceneLoaded;
            
            for (int i = 0; i < MatchSetup.Instance.Players.Count; i++)
            {
                LobbySlot slot = MatchSetup.Instance.Players[i];
                if (!catalog.TryGet(slot.CharacterId, out CharacterCatalog.Entry entry)) continue;

                PlayerId playerId = new PlayerId(i);              
                byte localIndex = slot.LocalId;

                // Registra o player e suas informações no servidor.
                roaster.ServerAdd(playerId, slot.OwnerId, localIndex, PlayerControlType.Human, slot.CharacterId);

                // Spawna o jogador no server.
                Transform point = spawnPoints[i];
                NetworkObject playerObj = Instantiate(entry.Prefab, point.position, point.rotation);  
                
                // Configura o jogador com o seu ID.
                if (playerObj.TryGetComponent(out IPlayerActor actor))
                    actor.SetPlayerId(playerId);

                // Instancia os jogadores para todos os clients.
                if (localIndex == 0)
                    playerObj.SpawnAsPlayerObject(slot.OwnerId);
                else
                    playerObj.SpawnWithOwnership(slot.OwnerId);
            }
        }
    }
}