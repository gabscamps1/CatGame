using CatGame.Core.Interfaces;
using CatGame.Core.Enums;

namespace CatGame.Core.Events
{
    /// <summary>
    /// Disparado quando a sessão online termina nesta máquina, por qualquer motivo (saiu, host caiu, kick, falha no loading).
    /// </summary>
    public struct SessionEndedEvent : IGlobalEvent
    {
        /// <summary>
        /// Esta máquina era o servidor.
        /// </summary>
        public bool WasServer { get; private set; }

        /// <summary>
        /// A sessão terminou fora do lobby e o jogador está sendo levado de volta ao menu.
        /// Falso quando terminou no próprio lobby, que já trata a saída sozinho.
        /// </summary>
        public bool IsReturningToMenu { get; private set; }

        /// <summary>
        /// Mensagem do motivo enviado pelo servidor. Vazio quando não há.
        /// </summary>
        public string ReasonMessage { get; private set; }

        /// <summary>
        /// Motivo enviado pelo servidor. Vazio quando não há.
        /// </summary>
        public SessionCallbackReason Reason { get; private set; }

        public SessionEndedEvent(bool wasServer, bool isReturningToMenu, string reasonMessage, SessionCallbackReason reason)
        {
            WasServer = wasServer;
            IsReturningToMenu = isReturningToMenu;
            ReasonMessage = reasonMessage;
            Reason = reason;
        }
    }
}
