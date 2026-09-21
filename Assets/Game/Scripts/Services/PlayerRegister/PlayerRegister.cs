using CatGame.Core.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CatGame.Services.Register
{
    public static class PlayerRegister
    {
        public static IReadOnlyDictionary<PlayerId, PlayerController> PlayerRegistereds => playerRegistereds;
        private static readonly Dictionary<PlayerId, PlayerController> playerRegistereds = new();

        public static event Action<PlayerId> OnPlayerRegister;
        public static event Action<PlayerId> OnPlayerUnregister;

        public static bool Register(PlayerController playerController, out PlayerId playerId)
        {
            playerId = default;

            if (playerRegistereds.ContainsValue(playerController))
                return false;

            playerId = GetNewPlayerId();
            playerRegistereds.Add(playerId, playerController);
            OnPlayerRegister?.Invoke(playerId);
            return true;
        }

        public static bool Unregister(PlayerId id)
        {
            if (!playerRegistereds.Remove(id))
                return false;

            OnPlayerUnregister?.Invoke(id);
            return true;
        }

        public static PlayerId[] GetAllPlayersConnected()
        {
            return playerRegistereds.Keys.ToArray();
        }

        public static bool IsPlayerConnected(PlayerId id)
        {
            return playerRegistereds.TryGetValue(id, out PlayerController player) && player != null;
        }

        private static PlayerId GetNewPlayerId()
        {
            int id = 0;

            while (playerRegistereds.ContainsKey(new PlayerId(id)))
            {
                id++;
            }

            return new PlayerId(id);
        }
    }
}