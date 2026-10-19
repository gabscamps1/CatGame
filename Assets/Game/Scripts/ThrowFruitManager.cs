using CatGame.Core.Data;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace CatGame.Features.Gameplay
{
    public static class ThrowFruitManager
    {
        private static readonly Dictionary<PlayerId, List<FruitCombiner>> clientSpawnedFruits = new();

        private static readonly Dictionary<PlayerId, GameObject> noPhysicalSpawnedFruit = new();
        private static readonly HashSet<PlayerId> clientCurrentFruit = new();
        
        
        /// <summary>
        /// Jogadores que estão segurando frutas na mão
        /// </summary>
        private static readonly HashSet<PlayerId> playerHoldingFruit = new();


        public static bool CanPlayerHoldFruit(PlayerId playerId)
        {
            return !playerHoldingFruit.Contains(playerId);
        }

        public static void PreventPlayerHoldFruit(PlayerId playerId)
        {
            playerHoldingFruit.Add(playerId);
        }

        public static void AllowPlayerHoldFruit(PlayerId playerId)
        {
            playerHoldingFruit.Remove(playerId);
        }


        public static bool CanPlayerSpawnedFruit(PlayerId playerId)
        {
            return !clientCurrentFruit.Contains(playerId);
        }

        public static void PreventPlayerSpawnFruit(PlayerId playerId)
        {
            clientCurrentFruit.Add(playerId);
        }

        public static void AllowPlayerSpawnFruit(PlayerId playerId)
        {
            clientCurrentFruit.Remove(playerId);
        }

        public static void SetPlayerNoPhysicalFruit(PlayerId playerId, GameObject fruit)
        {
            if (!noPhysicalSpawnedFruit.TryAdd(playerId, fruit))
                noPhysicalSpawnedFruit[playerId] = fruit;
        }

        public static GameObject GetPlayerNoPhysicalFruit(PlayerId playerId)
        {
            return noPhysicalSpawnedFruit.TryGetValue(playerId, out var fruit) ? fruit : null;
        }

        public static void RemovePlayerNoPhysicalFruit(PlayerId playerId)
        {
            noPhysicalSpawnedFruit.Remove(playerId);
        }



    }
}