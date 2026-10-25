using System;
using Unity.Netcode;

namespace CatGame.Core.Data
{
    /// <summary>
    /// Enum de ID de cada player do jogo.
    /// </summary>
    public struct PlayerId : INetworkSerializable, IEquatable<PlayerId>
    {
        public static PlayerId None => new(-1);
        public readonly bool IsValid => id >= 0;
        public readonly int Id => id;

        private int id;

        public PlayerId(int id)
        {
            this.id = id;
        }     

        public void NetworkSerialize<T>(BufferSerializer<T> serializer)
            where T : IReaderWriter
        {
            serializer.SerializeValue(ref id);
        }

        public bool Equals(PlayerId other)
        {
            return id == other.id;
        }

        public override bool Equals(object obj)
        {
            return obj is PlayerId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return id;
        }

        public static bool operator ==(PlayerId left, PlayerId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PlayerId left, PlayerId right)
        {
            return !left.Equals(right);
        }
    }
}
