using CatGame.Capabilities.UISystem;
using CatGame.Core.Enums;
using TMPro;
using UnityEngine;

namespace CatGame.UI
{
    public class OptionsVisual : HighlightVisual
    {
        [SerializeField] private Color selectedColor;
        [SerializeField] private Color unselectedColor;

        private TextMeshProUGUI currentTmp;

        public override void AttachTo(NavigableElement element, PlayerId playerId)
        {
            currentTmp = element.GetComponentInChildren<TextMeshProUGUI>();
            currentTmp.color = selectedColor;

            base.AttachTo(element, playerId);
        }

        public override void Disattach(PlayerId playerId)
        {
            if (currentTmp != null)
            {
                TextMeshProUGUI text = currentTmp.GetComponentInChildren<TextMeshProUGUI>();
                text.color = unselectedColor;
                currentTmp = null;
            }
           
            base.Disattach(playerId);
        }
    }
}
