namespace CatGame.Core.Enums
{
    /// <summary>
    /// Enum de todos os grupos de cena do jogo.
    /// Um Grupo é formado por várias cenas (ex: o grupo Level1 é formado pelas cenas (Bootstrap + MainScene + Level1))
    /// </summary>
    public enum Scenes
    {
        Bootstrap = 0,
        Splashs = 1,
        Cutscene = 2,
        MainMenu = 3,
        Playing = 4,
    }
}