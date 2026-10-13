using System;

namespace CatGame.Features.CharacterSelection
{
    public interface ILobbyView
    {
        public int SlotCount { get; }

        public void CloseMenu();
        public void ShotSocket(string socket);
        public void ShowSlot(int index, LobbySlot slot, bool editable);
        public void ShowEmptySlot(int index);
        public void SetStartInteractable(bool interactable);
        public LobbySlotView GetSlotView(int slotIndex);
        public event Action<int, int> CyclePressed;
        public event Action<int> ConfirmPressed; // (slot)
        public event Action<int> CancelPressed; // (slot)
        public event Action StartPressed;
        public event Action OnCloseLobbyPressed;
    }
}