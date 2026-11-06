using CatGame.Capabilities.UISystem;
using CatGame.Core;
using CatGame.Core.Interfaces;
using CatGame.Core.Enums;
using System;
using TMPro;
using UnityEngine;

namespace CatGame.Features.CharacterSelection
{
    public class LobbyView : BaseUIScreen, ILobbyView
    {
        [SerializeField] private LobbySlotView[] slotViews;
        [SerializeField] private ButtonElement startButton;
        [SerializeField] private MenuController menuController;
        [SerializeField] private TextMeshProUGUI socketTMP;

        public int SlotCount => slotViews.Length;

        public event Action<int, int> CyclePressed;
        public event Action<int> ConfirmPressed;
        public event Action<int> CancelPressed;
        public event Action StartPressed;

        public event Action OnCloseLobbyPressed;

        private void Awake()
        {
            for (int i = 0; i < slotViews.Length; i++)
            {
                int index = i;
                slotViews[i].Cycled += direction => CyclePressed?.Invoke(index, direction);
                slotViews[i].Confirmed += () => ConfirmPressed?.Invoke(index);
                slotViews[i].Cancelled += () => CancelPressed?.Invoke(index);
            }

            startButton.OnSubmittedEvent += (_,_) => StartPressed?.Invoke();
        }

        public override bool CanHide()
        {
            OnCloseLobbyPressed?.Invoke();
            return false;
        }

        protected override void OnBeforeHide()
        {
            IUIService uiService = ServiceLocator.Get<IUIService>();

            // Garante que a pop de fechar o lobby esteja fechada quando o lobby for finalizado.
            if (uiService.IsVisible(PanelType.QuitLobbyPopup))
            {
                uiService.Close(PanelType.QuitLobbyPopup);   
            }
        }

        public void SetStartInteractable(bool interactable)
        {
            startButton.SetInteractable(interactable);
        }

        public void ShowEmptySlot(int index)
        {
            slotViews[index].ShowEmpty();
        }

        public void ShowSlot(int index, LobbySlot slot, bool editable)
        {
            slotViews[index].Show(slot, editable);
        }

        public LobbySlotView GetSlotView(int index) => slotViews[index];

        public void CloseMenu()
        {
            menuController.ReturnMenu(true);
        }

        public void ShotSocket(string socket)
        {
            socketTMP.text = socket;
        }
    }
}