namespace CatGame.Core.Enums
{
    /// <summary>
    /// Enum de todos os painéis principais do jogo.
    /// </summary>
    public enum PanelType
    {
        // Random
        None = 0,

        // Globais    
        Pause = 1,
        Loading = 3,

        // Menu principal
        MainMenu = 4,

        // Partida
        MatchEnd = 8,

        // Popups
        QuitLobbyPopup = 6,
        SessionMessagePopup = 7,
        LeaveMatchPopup = 9,
    }
}
