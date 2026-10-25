using CatGame.Core;
using CatGame.Core.Data;
using CatGame.Core.Interfaces;
using System;
using System.Collections.Generic;
using Unity.Netcode;

namespace CatGame.Services.Register
{
    public struct PlayerRosterEntry : INetworkSerializable, IEquatable<PlayerRosterEntry>
    {
        public int playerId;
        public ulong clientId;
        public byte localIndex;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref playerId);
            serializer.SerializeValue(ref clientId);
            serializer.SerializeValue(ref localIndex);
        }

        public bool Equals(PlayerRosterEntry other) =>
            playerId == other.playerId && clientId == other.clientId && localIndex == other.localIndex;
    }

    public sealed class PlayerRosterSync : NetworkBehaviour
    {
        private readonly NetworkList<PlayerRosterEntry> roster = new();
        private IPlayerRegistry registry;

        public override void OnNetworkSpawn()
        {
            registry = ServiceLocator.Get<IPlayerRegistry>();
            roster.OnListChanged += OnRosterChanged;
            ApplyToRegistry(); // Quem entra no meio já recebe a lista atual
        }

        public override void OnNetworkDespawn()
        {
            roster.OnListChanged -= OnRosterChanged;
            registry?.Clear();
        }

        
        public void ServerAdd(PlayerId playerId, ulong clientId, byte localIndex)
        {
            if (!IsServer) return;

            for (int i = 0; i < roster.Count; i++)
                if (roster[i].playerId == playerId.Id) return; // Já está registrado

            roster.Add(new PlayerRosterEntry
            {
                playerId = playerId.Id,
                clientId = clientId,
                localIndex = localIndex
            });

            ApplyToRegistry();
        }

        public void ServerRemove(PlayerId playerId)
        {
            if (!IsServer) return;

            for (int i = 0; i < roster.Count; i++)
            {
                if (roster[i].playerId != playerId.Id) continue;
                roster.RemoveAt(i);
                return;
            }

            ApplyToRegistry();
        }

        
        private void OnRosterChanged(NetworkListEvent<PlayerRosterEntry> change)
        {
            ApplyToRegistry();
        }

        private void ApplyToRegistry()
        {
            List<PlayerInfo> snapshot = new(roster.Count);
            foreach (PlayerRosterEntry entry in roster)
                snapshot.Add(new PlayerInfo(new PlayerId(entry.playerId), entry.clientId, entry.localIndex));

            registry.Sync(snapshot);
        }
    }
}