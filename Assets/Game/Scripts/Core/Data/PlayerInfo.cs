using CatGame.Core.Enums;

namespace CatGame.Core.Data
{
    /// <summary>
    /// Informações sobre um participante da partida (Jogador ou IA).
    /// </summary>
    public readonly struct PlayerInfo
    {
        public readonly PlayerId playerId;
        public readonly ulong clientId; // Jogador: client dono do input. IA: o server.
        public readonly byte localIndex; // Jogador: índice do jogador dentro do client. IA: sem significado.
        public readonly PlayerControlType controlType;
        public readonly ushort characterId; // Personagem escolhido (CharacterCatalog).

        public bool IsAI => controlType == PlayerControlType.AI;

        public PlayerInfo(PlayerId playerId, ulong clientId, byte localIndex, PlayerControlType controlType, ushort characterId)
        {
            this.playerId = playerId;
            this.clientId = clientId;
            this.localIndex = localIndex;
            this.controlType = controlType;
            this.characterId = characterId;
        }
    }
}
