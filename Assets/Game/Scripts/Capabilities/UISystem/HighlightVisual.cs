using CatGame.Capabilities.UISystem;
using CatGame.Core.Enums;
using UnityEngine;

namespace QBProj.Capabilities.UISystem
{
    public class HighlightVisual : MonoBehaviour, IHighlighVisual
    {
        public virtual void AttachTo(NavigableElement element, PlayerId playerId)
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
            
            gameObject.SetActive(true);
        }
    }
}
