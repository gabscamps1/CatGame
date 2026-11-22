using CatGame.Core.Enums;
using CatGame.Core.Interfaces;

namespace CatGame.Core.Events
{
    /// <summary>
    /// Disparado nesta máquina quando o server recusa a entrada de um jogador no lobby (ex: lobby cheio).
    /// </summary>
    public struct LobbyJoinRejectedEvent : IGlobalEvent
    {
        public SessionCallbackReason Reason { get; private set; }

        public LobbyJoinRejectedEvent(SessionCallbackReason reason)
        {
            Reason = reason;
        }
    }
}
