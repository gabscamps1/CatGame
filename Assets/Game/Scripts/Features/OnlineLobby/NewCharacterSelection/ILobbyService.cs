using System;

namespace CatGame.Features.CharacterSelection
{
    public interface ILobbyService
    {
        public event Action Changed; // UI se atualiza quando dispara
        public event Action OnNetworkSpawned; // Quando um jogador entra em rede.
        public event Action OnOwnerDisconected;
        public int SlotCount { get; }
        public LobbySlot GetSlot(int index);
        public bool CanControl(int slotIndex);             // esta máquina pode mexer neste slot?

        public void CycleCharacter(int slotIndex, int direction);   // -1 ou +1
        public void SetReady(int slotIndex, bool ready);
        public void RequestStart();

        public void RequestCloseLobby();

        public string Socket {  get; }
    }
}