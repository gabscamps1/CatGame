namespace CatGame.Core.Enums
{
    /// <summary>
    /// Quem controla um jogador da partida.
    /// </summary>
    public enum PlayerControlType : byte
    {
        Human, // Controlado por um jogador (online ou local).
        AI // Controlado pela IA.
    }
}
