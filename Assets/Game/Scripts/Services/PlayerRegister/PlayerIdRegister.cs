using CatGame.Core.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CatGame.Services.Register
{
    public static class PlayerIdRegister
    {
        public static IReadOnlyDictionary<PlayerId, string> PlayerRegistereds => playerRegistereds;
        private static readonly Dictionary<PlayerId, string> playerRegistereds = new();

        public static event Action<PlayerId> OnPlayerRegister;
        public static event Action<PlayerId> OnPlayerUnregister;

        public static bool Register(string clientId, out PlayerId playerId)
        {
            playerId = default;

            if (playerRegistereds.ContainsValue(clientId))
                return false;

            playerId = GetNewPlayerId();
            playerRegistereds.Add(playerId, clientId);
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
            return playerRegistereds.TryGetValue(id, out string clientId) && string.IsNullOrEmpty(clientId);
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
