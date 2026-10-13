using CatGame.Capabilities.UISystem;
using CatGame.Core.Data;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace CatGame.Features.CharacterSelection
{
    [RequireComponent(typeof(Image))]
    public class SelectCharacterVisual : HighlightVisual
    {
        [SerializeField] private Sprite highlightP1;
        [SerializeField] private Sprite highlightP2;
        [SerializeField] private Sprite highlightBoth;
        [SerializeField] private Vector2 initialScale;
        [SerializeField] private Vector2 overScale;

        private Image visualImage;

        private NavigableElement attachedElement;
        private RectTransform attachedElementRT;

        private void Awake()
        {
            visualImage = GetComponent<Image>();
        }

        public override void AttachTo(NavigableElement element, PlayerId playerId)
        {
            RectTransform elementRectTransform = (RectTransform)element.transform;

            if (elementRectTransform == null)
                return;

            RectTransform rectTransform = (RectTransform)transform;
            rectTransform.SetParent(elementRectTransform, false);
            rectTransform.anchorMin = Vector3.zero;
            rectTransform.anchorMax = Vector3.one;
            rectTransform.offsetMin = Vector3.zero;
            rectTransform.offsetMax = Vector3.zero;           

            attachedElementRT = elementRectTransform;
            attachedElement = element;

            elementRectTransform.localScale = overScale;

            ConfigVisual();
        }

        public override void Disattach(PlayerId playerId)
        {
            if (!attachedElement.IsFocused)
            {
                attachedElementRT.localScale = initialScale;
                return;
            }

            ConfigVisual();
        }

        public override void Refresh()
        {
            ConfigVisual();
        }

        private void ConfigVisual()
        {
            if (attachedElement.FocusingPlayerList.Count == 1)
            {
                PlayerId playerId = attachedElement.FocusingPlayerList[0];
                switch (playerId.Id)
                {
                    case 0: visualImage.sprite = highlightP1; break;
                    case 1: visualImage.sprite = highlightP2; break;
                }
            }
            else if (attachedElement.FocusingPlayerList.Count > 1)
            {
                if (attachedElement.FocusingPlayerList.Contains(new PlayerId(0)) && attachedElement.FocusingPlayerList.Contains(new PlayerId(1)))
                    visualImage.sprite = highlightBoth;
            }
        }
    }
}