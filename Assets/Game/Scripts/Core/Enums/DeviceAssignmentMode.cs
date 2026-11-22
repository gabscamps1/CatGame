namespace CatGame.Core.Enums
{
    /// <summary>
    /// Como o InputActionsService distribui os dispositivos entre os jogadores locais.
    /// </summary>
    public enum DeviceAssignmentMode : byte
    {
        Shared, // Menu: o jogador local 0 usa teclado, mouse e qualquer controle. Controles novos são distribuídos sozinhos.
        Exclusive // Lobby e partida: cada dispositivo pertence a um jogador só, e nada é distribuído sozinho.
    }
}
