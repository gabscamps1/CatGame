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

            // A lista tem uma entrada por vaga do lobby.
            for (int i = 0; i < MatchSetup.Instance.Players.Count; i++)
            {
                if (i >= spawnPoints.Length)
                {
                    Core.Logger.LogWarning($"Há mais participantes ({MatchSetup.Instance.Players.Count}) do que spawn points ({spawnPoints.Length}).");
                    break;
                }

                LobbySlot slot = MatchSetup.Instance.Players[i];

                if (!slot.Occupied)
                    continue;

                if (!catalog.TryGet(slot.CharacterId, out CharacterCatalog.Entry entry)) 
                    continue;

                // o PlayerId do participante é o número da vaga.
                SlotId slotId = new SlotId(i);
                PlayerId playerId = new PlayerId(slotId.Value);
                Transform point = spawnPoints[i];

                if (slot.IsAI)
                    SpawnAI(playerId, slot, entry, point);
                else
                    SpawnHuman(playerId, slot, entry, point, timedOut);
            }
        }

        private void SpawnHuman(PlayerId playerId, LobbySlot slot, CharacterCatalog.Entry entry, Transform point, List<ulong> timedOut)
        {
            // Quem caiu ou não carregou a tempo fica de fora (quem estourou o tempo é desconectado pelo SceneLoadingService).
            // O índice continua o do lobby, então o pote dele só fica vazio.
            if (!NetworkManager.ConnectedClients.ContainsKey(slot.OwnerId) || timedOut.Contains(slot.OwnerId))
                return;

            byte localIndex = slot.LocalId;

            // Registra o player e suas informações no servidor.
            roaster.ServerAdd(playerId, slot.OwnerId, localIndex, PlayerControlType.Human, slot.CharacterId);

            // Spawna o jogador no server.
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

        private void SpawnAI(PlayerId playerId, LobbySlot slot, CharacterCatalog.Entry entry, Transform point)
        {
            if (entry.AIPrefab == null)
            {
                Core.Logger.LogWarning($"O personagem {entry.Id} não tem {nameof(CharacterCatalog.Entry.AIPrefab)} no {nameof(CharacterCatalog)}.");
                return;
            }

            // A IA roda no server: o clientId é o do server e o localIndex não é usado.
            roaster.ServerAdd(playerId, NetworkManager.ServerClientId, 0, PlayerControlType.AI, slot.CharacterId);

            // A dificuldade vem do AIProfile configurado no próprio prefab.
            NetworkObject aiObj = Instantiate(entry.AIPrefab, point.position, point.rotation);

            if (aiObj.TryGetComponent(out IPlayerActor actor))
                actor.SetPlayerId(playerId);

            // O server é o dono da IA.
            aiObj.SpawnWithOwnership(NetworkManager.ServerClientId);
        }
    }
}