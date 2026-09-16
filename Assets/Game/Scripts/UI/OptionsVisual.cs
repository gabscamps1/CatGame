using CatGame.Capabilities.UISystem;
using CatGame.Core.Enums;
using TMPro;
using UnityEngine;
using System.Collections.Generic;

namespace CatGame.UI
{
    public class OptionsVisual : HighlightVisual
    {
        [SerializeField] private Color selectedColor;
        [SerializeField] private Color unselectedColor;

        private List<TextMeshProUGUI> tmpList = new();

        public override void AttachTo(NavigableElement element, PlayerId playerId)
        {
            foreach (TextMeshProUGUI tmp in element.GetComponentsInChildren<TextMeshProUGUI>())
            {
                tmp.color = selectedColor;
                tmpList.Add(tmp);
            }

            base.AttachTo(element, playerId);
        }

        public override void Disattach(PlayerId playerId)
        {
            if (tmpList != null && tmpList.Count > 0)
            {
                foreach (TextMeshProUGUI tmp in tmpList)              
                    tmp.color = unselectedColor;
                
                tmpList.Clear();
            }
           
            base.Disattach(playerId);
        }
    }
}
