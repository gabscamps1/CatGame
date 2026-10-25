using CatGame.Core.Data;
using CatGame.Services.Register;
using System;
using System.Collections.Generic;

namespace CatGame.Core.Interfaces
{
    public interface IPlayerRegistry
    {
        public IReadOnlyCollection<PlayerInfo> Players { get; }

        public event Action<PlayerInfo> PlayerRegistered;
        public event Action<PlayerInfo> PlayerUnregistered;

        public bool Register(PlayerInfo info);
        public bool Unregister(PlayerId playerId);
        public void Sync(IReadOnlyList<PlayerInfo> snapshot);   // aplica a lista replicada, disparando eventos só do que mudou
        public void Clear();

        public bool TryGetPlayerInfo(PlayerId playerId, out PlayerInfo info);
        public bool TryGetPlayerId(ulong clientId, byte localIndex, out PlayerId playerId);
        public IEnumerable<PlayerInfo> GetPlayersOfClient(ulong clientId);   // local: vários por client

        // Controllers (local por peer)
        public void RegisterController(PlayerId playerId, PlayerController controller);
        public void UnregisterController(PlayerId playerId, PlayerController controller);
        public bool TryGetController(PlayerId playerId, out PlayerController controller);
    }
}