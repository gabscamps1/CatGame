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
        [SerializeField] private Image portrait;
        [SerializeField] private TextMeshProUGUI playerNameText, characterNameText;
        [SerializeField] private GameObject readyBadge, arrowsHint;
        [SerializeField] private Color confirmedTint = Color.white;
        [SerializeField] private Color previewTint = new(1f, 1f, 1f, 0.6f);

        /*public event Action<int> CyclePressed; // Direção para alterar o personagem atual
        public event Action<ushort> Submitted; // id da prévia atual
        public event Action Cancelled;

        private LobbySlot lobbySlot;
        private int characterPreviewIndex; // Index do personagem em preview

        private bool isInitialized, isEditable;

        public void Show(LobbySlot lobbySlot, bool isEditable)
        {
            this.lobbySlot = lobbySlot;
            this.isEditable = isEditable;

            activeRoot.SetActive(true);
            emptyRoot.SetActive(false);

            if (!isInitialized || !isEditable || lobbySlot.Ready)
                characterPreviewIndex = GetIndexInCatalog(lobbySlot.CharacterId);
            isInitialized = true;

            // Informações do jogador
            playerNameText.text = lobbySlot.PlayerName.ToString();

            // Informações da selelção.
            readyBadge.SetActive(lobbySlot.Ready);
            arrowsHint.SetActive(!lobbySlot.Ready);

            RefreshPreview();
        }

        /// <summary>
        /// Exibe a UI sem nenhum personagem selecionado.
        /// </summary>
        public void ShowEmpty()
        {
            isInitialized = false;
            activeRoot.SetActive(false);
            emptyRoot.SetActive(true);
        }

        public void MovePreview(int delta)
        {
            if (!isEditable || lobbySlot.Ready || catalog.Entries.Count == 0)
                return;

            int n = catalog.Entries.Count;
            characterPreviewIndex = (characterPreviewIndex + delta + n) % n;   // dá a volta
            RefreshPreview();
        }

        public void Submit()
        {
            if (!isEditable || catalog.Entries.Count == 0)
                return;

            Submitted?.Invoke(catalog.Entries[characterPreviewIndex].Id);
        }

        public void Cancel()
        {
            if (isEditable) Cancelled?.Invoke();
        }

        /// <summary>
        /// Atualiza a UI com o novo personagem selecionado.
        /// </summary>
        private void RefreshPreview()
        {
            if (catalog.Entries.Count == 0) return;
            CharacterCatalog.Entry entry = catalog.Entries[characterPreviewIndex]; // Busca o personagem selecionado no catalogo.

            // Informações do personagem
            portrait.sprite = entry.Portrait;
            characterNameText.text = entry.DisplayName;

            portrait.color = entry.Id == lobbySlot.CharacterId ? confirmedTint : previewTint;
        }

        /// <summary>
        /// Procura o index do catalogo que armazena o personagem.
        /// </summary>
        private int GetIndexInCatalog(ushort characterId)
        {
            for (int i = 0; i < catalog.Entries.Count; i++)
                if (catalog.Entries[i].Id == characterId) 
                    return i;

            return 0;
        }*/

        public event Action<int> Cycled;
        public event Action Confirmed;
        public event Action Cancelled;

        private bool editable;

        public void Show(LobbySlot slot, bool editable)
        {
            this.editable = editable;
            activeRoot.SetActive(true);
            emptyRoot.SetActive(false);

            //playerNameText.text = slot.PlayerName.ToString();
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
            activeRoot.SetActive(false);
            emptyRoot.SetActive(true);
        }

        // Chamados por LobbySlotInput
        public void Cycle(int direction) { if (editable) Cycled?.Invoke(direction); }
        public void Confirm() { if (editable) Confirmed?.Invoke(); }
        public void Cancel() { if (editable) Cancelled?.Invoke(); }
    }
}