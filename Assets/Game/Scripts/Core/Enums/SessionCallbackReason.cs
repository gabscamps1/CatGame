namespace CatGame.Core.Enums
{
    /// <summary>
    /// Motivos dos erros de conexão para entrar no lobby
    /// </summary>
    public enum SessionCallbackReason
    {
        ConnectionError,
        TryingConnnecting,
        FullLobby,
        IpError,
        PortError,
        MatchStarted,
    }
}
