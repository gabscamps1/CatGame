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
        Settings = 2,
        Loading = 3,

        // Menu principal
        MainMenu = 4,
        Credits = 5,

        // Play
        HUD = 6,
    }
}
