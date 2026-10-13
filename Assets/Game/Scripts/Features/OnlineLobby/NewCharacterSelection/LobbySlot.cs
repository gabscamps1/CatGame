using System;
using Unity.Collections;
using Unity.Netcode;

namespace CatGame.Features.CharacterSelection
{
    public enum LobbyPhase : byte { None, Selecting, Starting }

    public struct LobbySlot : INetworkSerializable, IEquatable<LobbySlot>
    {
        public ulong OwnerId; // online: clientId | local: índice do jogador
        public ushort CharacterId; // 0 = nenhum escolhido
        public bool Ready;
        public FixedString32Bytes PlayerName;

        public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter
        {
            s.SerializeValue(ref OwnerId);
            s.SerializeValue(ref CharacterId);
            s.SerializeValue(ref Ready);
            s.SerializeValue(ref PlayerName);
        }

        public bool Equals(LobbySlot slot)
        {
            return OwnerId == slot.OwnerId 
                && CharacterId == slot.CharacterId 
                && Ready == slot.Ready 
                && PlayerName.Equals(slot.PlayerName);
        }
            
    }
}