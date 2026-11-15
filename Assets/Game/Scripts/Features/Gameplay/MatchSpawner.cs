using CatGame.Features.CharacterSelection;
using CatGame.Services.Register;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using CatGame.Core.Data;
using CatGame.Core.Enums;
using CatGame.Core.Interfaces;
using CatGame.Features.AI;

namespace CatGame.Features.Gameplay
{
    public class MatchSpawner : NetworkBehaviour
    {
        [SerializeField] private CharacterCatalog catalog;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private PlayerRosterSync roaster;

        [Header("Debug")]
        [SerializeField] private NetworkObject debugAIPrefab;
        [SerializeField] private AIProfile debugAIProfile;
        [SerializeField, Range(0, 4)] private int debugAICount;
        [SerializeField] private ushort debugAICharacterId; // Personagem da IA no CharacterCatalog (usado na tela de vitória).

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

            SpawnDebugAIs(MatchSetup.Instance.Players.Count);
        }

        /// <summary>
        /// Cria IAs de teste depois dos jogadores do lobby. Temporário até as IAs virem do lobby.
        /// </summary>
        private void SpawnDebugAIs(int firstPlayerIndex)
        {
            if (debugAIPrefab == null || debugAICount <= 0)
                return;

            int lastPlayerIndex = Mathf.Min(firstPlayerIndex + debugAICount, spawnPoints.Length);

            for (int i = firstPlayerIndex; i < lastPlayerIndex; i++)
            {
                PlayerId playerId = new PlayerId(i);

                // A IA roda no server: o clientId é o do server e o localIndex não é usado.
                roaster.ServerAdd(playerId, NetworkManager.ServerClientId, 0, PlayerControlType.AI, debugAICharacterId);

                Transform point = spawnPoints[i];
                NetworkObject aiObj = Instantiate(debugAIPrefab, point.position, point.rotation);

                if (aiObj.TryGetComponent(out IPlayerActor actor))
                    actor.SetPlayerId(playerId);

                if (debugAIProfile != null && aiObj.TryGetComponent(out AIController aiController))
                    aiController.SetProfile(debugAIProfile);

                // O server é o dono da IA.
                aiObj.SpawnWithOwnership(NetworkManager.ServerClientId);
            }
        }
    }
}