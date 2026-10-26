using CatGame.Features.CharacterSelection;
using CatGame.Services.Register;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using CatGame.Core.Data;
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
            
            for (int i = 0; i < MatchSetup.Players.Count; i++)
            {
                LobbySlot slot = MatchSetup.Players[i];
                if (!catalog.TryGet(slot.CharacterId, out CharacterCatalog.Entry entry)) continue;

                PlayerId playerId = new PlayerId(i);
                byte localIndex = 0; // GABRIEL ME LEMBRA DE CONFIGURAR DEPOIS COM O LOBBY.

                roaster.ServerAdd(playerId, slot.OwnerId, localIndex);

                Transform point = spawnPoints[i];
                NetworkObject playerObj = Instantiate(entry.Prefab, point.position, point.rotation);  
                
                if (playerObj.TryGetComponent(out IPlayerIdConfig playerIdConfig))
                    playerIdConfig.SetPlayerId(playerId);

                // Coloca inicialmente o server como dono de cada objeto.
                playerObj.SpawnAsPlayerObject(NetworkManager.ServerClientId);
            }
        }
    }
}