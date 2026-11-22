using System;
using Unity.Netcode;

namespace CatGame.Features.CharacterSelection
{
    /// <summary>
    /// Uma das vagas fixas do lobby.
    /// Só existe no lobby: no início da partida vira o PlayerId.
    /// </summary>
    public struct SlotId : INetworkSerializable, IEquatable<SlotId>
    {
        public static SlotId None => new(-1);
        public readonly bool IsValid => value >= 0;
        public readonly int Value => value;

        private int value;

        public SlotId(int value)
        {
            this.value = value;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer)
            where T : IReaderWriter
        {
            serializer.SerializeValue(ref value);
        }

        public bool Equals(SlotId other)
        {
            return value == other.value;
        }

        public override bool Equals(object obj)
        {
            return obj is SlotId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return value;
        }

        public override string ToString()
        {
            return $"Slot {value}";
        }

        public static bool operator ==(SlotId left, SlotId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SlotId left, SlotId right)
        {
            return !left.Equals(right);
        }
    }
}
