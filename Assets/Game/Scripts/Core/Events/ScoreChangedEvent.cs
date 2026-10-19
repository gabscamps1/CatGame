using CatGame.Core.Interfaces;

namespace CatGame.Core.Events
{
    public struct ScoreChangedEvent : IGlobalEvent
    {
        public int PlayerIndex { get; set; }
        public int ScoreAmount { get; set; }
    }
}