using System.Threading.Tasks;
using UnityEngine;

namespace CatGame.Capabilities.UISystem
{
    public interface IPanelAnimation
    {
        public void PrepareElementToShow(RectTransform rectTransform);
        public Task PlayShow(RectTransform rectTransform);
        public Task PlayHide(RectTransform rectTransform);
    }
}