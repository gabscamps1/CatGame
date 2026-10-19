using CatGame.Core.Interfaces;

namespace CatGame.Core.Events
{
    public struct ClientPingChangedEvent : IGlobalEvent
    {
        public int PlayerIndex { get; set; }
        public int Ping { get; set; }
    }
}