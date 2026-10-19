using CatGame.Core.Interfaces;

namespace CatGame.Core.Events
{
    public struct LeaderChangedEvent : IGlobalEvent
    {
        public int PlayerIndex { get; set; }
        public bool IsLeader { get; set; }
    }
}