using CatGame.Core.Data;
using CatGame.Core.Interfaces;
using System;
using System.Collections.Generic;

namespace CatGame.Services.Register
{
    public class PlayerRegistry : IPlayerRegistry
    {
        private readonly Dictionary<PlayerId, PlayerInfo> playersById = new();
        private readonly Dictionary<(ulong clientId, byte localIndex), PlayerId> idsByConnection = new();
        private readonly Dictionary<PlayerId, PlayerController> controllers = new();

        public IReadOnlyCollection<PlayerInfo> Players => playersById.Values;

        public event Action<PlayerInfo> PlayerRegistered;
        public event Action<PlayerInfo> PlayerUnregistered;

        public bool Register(PlayerInfo info)
        {
            if (!info.playerId.IsValid) return false;
            if (playersById.ContainsKey(info.playerId)) return false;

            var connectionKey = (info.clientId, info.localIndex);
            if (idsByConnection.ContainsKey(connectionKey)) return false;

            playersById.Add(info.playerId, info);
            idsByConnection.Add(connectionKey, info.playerId);

            PlayerRegistered?.Invoke(info);
            return true;
        }

        public bool Unregister(PlayerId playerId)
        {
            if (!playersById.Remove(playerId, out PlayerInfo info)) return false;

            idsByConnection.Remove((info.clientId, info.localIndex));
            controllers.Remove(playerId);

            PlayerUnregistered?.Invoke(info);
            return true;
        }

        /// <summary>
        /// Sincroniza os players conectados 
        /// </summary>
        public void Sync(IReadOnlyList<PlayerInfo> snapshot)
        {
            HashSet<PlayerId> incoming = new();
            
            // Pega todos os players conectados no momento.
            foreach (PlayerInfo info in snapshot) 
                incoming.Add(info.playerId);

            // Cria uma lista com os players que foram desconectados.
            List<PlayerId> toRemove = new();
            foreach (PlayerId id in playersById.Keys)
            {
                if (!incoming.Contains(id)) 
                    toRemove.Add(id);
            }

            // Remove os players que foram desconectados
            foreach (PlayerId id in toRemove) 
                Unregister(id);

            // Add os players novos.
            foreach (PlayerInfo info in snapshot)
            {
                if (!playersById.ContainsKey(info.playerId)) 
                    Register(info);
            }
        }

        public void Clear()
        {
            playersById.Clear();
            idsByConnection.Clear();
            controllers.Clear();
        }

        public bool TryGetPlayerInfo(PlayerId playerId, out PlayerInfo info)
        {
            return playersById.TryGetValue(playerId, out info);
        }

        public bool TryGetPlayerId(ulong clientId, byte localIndex, out PlayerId playerId)
        {
            return idsByConnection.TryGetValue((clientId, localIndex), out playerId);
        }

        public IEnumerable<PlayerInfo> GetPlayersOfClient(ulong clientId)
        {
            foreach (PlayerInfo info in playersById.Values)
                if (info.clientId == clientId) yield return info;
        }

        #region PlayerController

        public void RegisterController(PlayerId playerId, PlayerController controller)
        {
            controllers[playerId] = controller;
        }

        public void UnregisterController(PlayerId playerId, PlayerController controller)
        {
            // Só remove se ainda for o mesmo (evita apagar um controller novo registrado depois).
            if (controllers.TryGetValue(playerId, out PlayerController current) && current == controller)
                controllers.Remove(playerId);
        }

        public bool TryGetController(PlayerId playerId, out PlayerController controller)
        {
            return controllers.TryGetValue(playerId, out controller);
        }

        #endregion
    }
}
