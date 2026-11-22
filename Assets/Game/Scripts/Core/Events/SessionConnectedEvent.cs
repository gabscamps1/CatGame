using CatGame.Core.Interfaces;

namespace CatGame.Core.Events
{
    /// <summary>
    /// Disparado quando esta máquina (client, não o host) termina de conectar na sessão.
    /// </summary>
    public struct SessionConnectedEvent : IGlobalEvent
    {
    }
}
