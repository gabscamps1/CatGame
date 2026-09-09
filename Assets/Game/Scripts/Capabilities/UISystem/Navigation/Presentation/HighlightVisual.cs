using CatGame.Core.Enums;
using UnityEngine;

namespace CatGame.Capabilities.UISystem
{
    public class HighlightVisual : MonoBehaviour, IHighlightVisual
    {
        public virtual void AttachTo(NavigableElement element, PlayerId playerId)
        {
            RectTransform elementRectTransform = (RectTransform)element.transform;

            if (elementRectTransform == null)
                return;

            RectTransform rectTransform = (RectTransform)transform;
            rectTransform.SetParent(elementRectTransform, false);
            rectTransform.anchoredPosition = Vector2.zero;         
            transform.SetSiblingIndex(0);
            
            gameObject.SetActive(true);
        }

        public virtual void Disattach(PlayerId playerId)
        {
            gameObject.SetActive(false);
        }

        public virtual void Refresh() { }
    }
}
