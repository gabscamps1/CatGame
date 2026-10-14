using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CatGame.Features.CharacterSelection
{
    public class LobbySlotView : MonoBehaviour
    {
        [SerializeField] private CharacterCatalog catalog;
        [SerializeField] private GameObject activeRoot, emptyRoot;
        [SerializeField] private Image portrait, playerBotImage;
        [SerializeField] private Sprite keyboardSprite, gamepadSprite, botSprite;
        [SerializeField] private TextMeshProUGUI playerNameText, characterNameText;
        [SerializeField] private GameObject readyBadge, arrowsHint;


        public event Action<int> Cycled;
        public event Action Confirmed;
        public event Action Cancelled;

        private bool editable;

        public void Show(LobbySlot slot, bool editable)
        {
            this.editable = editable;

            //playerNameText.text = slot.PlayerName.ToString();
            playerBotImage.sprite = gamepadSprite;
            readyBadge.SetActive(slot.Ready);
            arrowsHint.SetActive(editable && !slot.Ready);

            if (catalog.TryGet(slot.CharacterId, out var entry))
            {
                portrait.enabled = true;
                portrait.sprite = entry.Portrait;
                characterNameText.text = entry.DisplayName;
            }
            else
            {
                portrait.enabled = false;
                characterNameText.text = "";
            }
        }

        public void ShowEmpty()
        {
            playerBotImage.sprite = botSprite;
            arrowsHint.SetActive(false);
            readyBadge.SetActive(true); // Se não tiver jogador, deixa como pronto.
        }

        // Chamados por LobbySlotInput
        public void Cycle(int direction) { if (editable) Cycled?.Invoke(direction); }
        public void Confirm() { if (editable) Confirmed?.Invoke(); }
        public void Cancel() { if (editable) Cancelled?.Invoke(); }
    }
}