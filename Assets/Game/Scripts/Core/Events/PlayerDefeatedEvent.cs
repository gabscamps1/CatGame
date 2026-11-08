using CatGame.Core.Data;
using CatGame.Core.Interfaces;

namespace CatGame.Core.Events
{
    public struct PlayerDefeatedEvent : IGlobalEvent
    {
        public PlayerId PlayerId { get; private set; }

        public PlayerDefeatedEvent(PlayerId playerId)
        {
            PlayerId = playerId;
        }
    }
}