using CatGame.Core.Data;
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
        public void Sync(IReadOnlyList<PlayerInfo> snapshot); // Aplica a lista replicada, disparando eventos só do que mudou.
        public void Clear();

        public bool TryGetPlayerInfo(PlayerId playerId, out PlayerInfo info);
        public bool TryGetPlayerId(ulong clientId, byte localIndex, out PlayerId playerId); // Só jogadores humanos: a IA não tem conexão nem índice local próprios.
        public IEnumerable<PlayerInfo> GetPlayersOfClient(ulong clientId); // Jogadores controlados por este client.

        // Participantes: jogadores ou IA (local por peer)
        public void RegisterActor(PlayerId playerId, IPlayerActor actor);
        public void UnregisterActor(PlayerId playerId, IPlayerActor actor);
        public bool TryGetActor(PlayerId playerId, out IPlayerActor actor);
    }
}