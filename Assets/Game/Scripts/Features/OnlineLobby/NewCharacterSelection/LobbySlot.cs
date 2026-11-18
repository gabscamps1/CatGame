using CatGame.Core.Enums;
using System;
using Unity.Collections;
using Unity.Netcode;

namespace CatGame.Features.CharacterSelection
{
    public enum LobbyPhase : byte { None, Selecting, Starting }

    public struct LobbySlot : INetworkSerializable, IEquatable<LobbySlot>
    {
        public ulong OwnerId; // Id do client
        public byte LocalId; // Índice do jogador dentro do mesmo client.
        public ushort CharacterId; // 0 = nenhum escolhido
        public bool Ready;
        public FixedString32Bytes PlayerName;
        public PlayerControlType ControlType; // Human (padrão) ou AI. IA: OwnerId é o server e LocalId não é usado.

        public bool IsAI => ControlType == PlayerControlType.AI;

        public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter
        {
            s.SerializeValue(ref OwnerId);
            s.SerializeValue(ref LocalId);
            s.SerializeValue(ref CharacterId);
            s.SerializeValue(ref Ready);
            s.SerializeValue(ref PlayerName);
            s.SerializeValue(ref ControlType);
        }

        public bool Equals(LobbySlot slot)
        {
            return OwnerId == slot.OwnerId 
                && LocalId == slot.LocalId
                && CharacterId == slot.CharacterId 
                && Ready == slot.Ready 
                && PlayerName.Equals(slot.PlayerName)
                && ControlType == slot.ControlType;
        }
            
    }
}