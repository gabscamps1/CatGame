namespace CatGame.Core.Data
{
    public readonly struct PlayerInfo
    {
        public readonly PlayerId playerId;
        public readonly ulong clientId;
        public readonly byte localIndex;

        public PlayerInfo(PlayerId playerId, ulong clientId, byte localIndex)
        {
            this.playerId = playerId;
            this.clientId = clientId;
            this.localIndex = localIndex;
        }
    }
}