namespace CatGame.Core.Enums
{
    /// <summary>
    /// Fase do carregamento de um grupo de cenas online.
    /// </summary>
    public enum SceneGroupPhase : byte
    {
        None = 0,
        Loading = 1,
        Finished = 2,
    }
}
