using CatGame.Core.Enums;
using System;
using Unity.Collections;
using Unity.Netcode;

namespace CatGame.Features.CharacterSelection
{
    public enum LobbyPhase : byte { None, Selecting, Starting }

    /// <summary>
    /// Estado replicado de uma vaga do lobby.
    /// </summary>
    public struct LobbySlot : INetworkSerializable, IEquatable<LobbySlot>
    {
        public bool Occupied; // Falso = vaga livre.
        public ulong OwnerId; // Id do client
        public byte LocalId; // Índice do jogador dentro do mesmo client (0 = jogador principal do client).
        public ushort CharacterId; // 0 = nenhum escolhido
        public bool Ready;
        public bool WaitingDevice; // O jogador continua na vaga, mas está sem dispositivo (trocou ou o controle desconectou).
        public DeviceKind DeviceKind; // Só para o ícone na UI.
        public FixedString32Bytes PlayerName;
        public PlayerControlType ControlType; // Human ou AI. IA: OwnerId é o server e LocalId não é usado.

        public static LobbySlot Empty => default;

        public bool IsAI => ControlType == PlayerControlType.AI;

        public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter
        {
            s.SerializeValue(ref Occupied);
            s.SerializeValue(ref OwnerId);
            s.SerializeValue(ref LocalId);
            s.SerializeValue(ref CharacterId);
            s.SerializeValue(ref Ready);
            s.SerializeValue(ref WaitingDevice);
            s.SerializeValue(ref DeviceKind);
            s.SerializeValue(ref PlayerName);
            s.SerializeValue(ref ControlType);
        }

        public bool Equals(LobbySlot slot)
        {
            return Occupied == slot.Occupied
                && OwnerId == slot.OwnerId
                && LocalId == slot.LocalId
                && CharacterId == slot.CharacterId
                && Ready == slot.Ready
                && WaitingDevice == slot.WaitingDevice
                && DeviceKind == slot.DeviceKind
                && PlayerName.Equals(slot.PlayerName)
                && ControlType == slot.ControlType;
        }
    }
}
