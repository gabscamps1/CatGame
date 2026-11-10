using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CatGame.Core;
using CatGame.Core.Data;
using CatGame.Core.Interfaces;

namespace CatGame.Features.CharacterSelection
{
    public class LobbySlotView : MonoBehaviour
    {
        [SerializeField] private CharacterCatalog catalog;
        [SerializeField] private GameObject activeRoot, emptyRoot;
        [SerializeField] private Image portrait, playerBotImage, emptyImage;
        [SerializeField] private Sprite keyboardSprite, gamepadSprite, botSprite, emptySprite;
        [SerializeField] private TextMeshProUGUI playerNameText, characterNameText;
        [SerializeField] private GameObject readyBadge, arrowsHint;

        [Header("Sounds")]
        [SerializeField] private AudioData UI_READY;
        [SerializeField] private AudioData UI_UNREADY;
        [SerializeField] private AudioData UI_SELECT_PLAYER;

        private IAudioService audioService;



        public event Action<int> Cycled;
        public event Action Confirmed;
        public event Action Cancelled;

        private bool editable;
        private bool isReady;

        private void Start()
        {
            audioService = ServiceLocator.Get<IAudioService>();
        }

        public void Show(LobbySlot slot, bool editable)
        {
            this.editable = editable && !slot.Ready;

            //playerNameText.text = slotInput.PlayerName.ToString();
            playerBotImage.sprite = gamepadSprite;
            emptyImage.color = new Color(emptyImage.color.r, emptyImage.color.g, emptyImage.color.b, 0);
            readyBadge.SetActive(slot.Ready);
            //emptyImage.SetActive(slot.Ready);
            arrowsHint.SetActive(editable && !slot.Ready);
            emptyImage.sprite = emptySprite;

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
            emptyImage.sprite = emptySprite;
            readyBadge.SetActive(false); // Se não tiver jogador, deixa como pronto.
        }

        // Chamados por LobbySlotInput
        public void Cycle(int direction)
        {
            if (!editable)
                return;

            Cycled?.Invoke(direction);
            if (direction != 0)
                audioService.PlaySFXClip(UI_SELECT_PLAYER, false);
        }

        public void Confirm()
        {
            if (editable)
            {
                audioService.PlaySFXClip(UI_READY, false);
            }

            Confirmed?.Invoke();
        }
        public void Cancel()
        {
            if (editable)
                return;

            Cancelled?.Invoke();
            audioService.PlaySFXClip(UI_UNREADY, false);
        }
    }
}