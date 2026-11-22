using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CatGame.Core;
using CatGame.Core.Data;
using CatGame.Core.Enums;
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

        [Header("Status")]
        [Tooltip("Texto sobre a vaga: dica para entrar, \"Entrando...\" ou \"Aguardando dispositivo\".")]
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private string joinHintTextID = "LOBBY_JOIN_HINT";
        [SerializeField] private string joiningTextID = "LOBBY_JOINING";
        [SerializeField] private string waitingDeviceTextID = "LOBBY_WAITING_DEVICE";

        [Header("Sounds")]
        [SerializeField] private AudioData UI_READY;
        [SerializeField] private AudioData UI_UNREADY;
        [SerializeField] private AudioData UI_SELECT_PLAYER;

        private IAudioService audioService;
        private ILocalizationService localizationService;
        private string currentStatusID; // Para atualizar o texto quando o idioma mudar.

        public event Action<int> Cycled;
        public event Action Confirmed;
        public event Action Cancelled;

        private bool editable;
        private bool isReady;

        #region Unity

        private void Awake()
        {
            localizationService = ServiceLocator.Get<ILocalizationService>();
            localizationService.OnLocalizationChanged += LocalizationService_OnLocalizationChanged;
        }

        private void Start()
        {
            audioService = ServiceLocator.Get<IAudioService>();
        }

        private void OnDestroy()
        {
            if (localizationService != null)
                localizationService.OnLocalizationChanged -= LocalizationService_OnLocalizationChanged;
        }

        #endregion

        #region Show

        public void Show(LobbySlot slot, bool editable)
        {
            this.editable = editable && !slot.Ready;

            //playerNameText.text = slotInput.PlayerName.ToString();
            playerBotImage.sprite = slot.DeviceKind == DeviceKind.Gamepad ? gamepadSprite : keyboardSprite;
            SetStatus(slot.WaitingDevice ? waitingDeviceTextID : null);
            emptyImage.color = new Color(emptyImage.color.r, emptyImage.color.g, emptyImage.color.b, 0);
            readyBadge.SetActive(slot.Ready);
            //emptyImage.SetActive(slot.Ready);
            arrowsHint.SetActive(editable && !slot.Ready && !slot.WaitingDevice);
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

        /// <summary>
        /// Vaga livre: ícone de IA e a dica para entrar. 
        /// isJoining: um jogador desta máquina pediu esta vaga.
        /// </summary>
        public void ShowEmpty(bool isJoining)
        {
            playerBotImage.sprite = botSprite;
            SetStatus(isJoining ? joiningTextID : joinHintTextID);

            emptyImage.color = new Color(emptyImage.color.r, emptyImage.color.g, emptyImage.color.b, 1);
            portrait.enabled = false; // Esconde o personagem de quem estava na vaga.
            characterNameText.text = "";
            arrowsHint.SetActive(false);
            emptyImage.sprite = emptySprite;
            readyBadge.SetActive(false); // Se não tiver jogador, deixa como pronto.
        }

        private void SetStatus(string textID)
        {
            currentStatusID = textID;

            if (statusText == null)
                return;

            bool hasStatus = !string.IsNullOrEmpty(textID);
            statusText.gameObject.SetActive(hasStatus);

            if (hasStatus)
                statusText.text = localizationService.GetTextByID(textID);
        }

        private void LocalizationService_OnLocalizationChanged()
        {
            SetStatus(currentStatusID);
        }

        #endregion

        #region Input

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
        #endregion
    }
}