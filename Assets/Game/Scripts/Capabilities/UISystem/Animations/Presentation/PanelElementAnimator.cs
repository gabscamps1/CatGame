using System.Threading.Tasks;
using UnityEngine;

namespace CatGame.Capabilities.UISystem
{
    public class PanelElementAnimator : MonoBehaviour
    {
        [Header("Animation Data")]
        [SerializeField] private PanelAnimationSO panelAnimationShowSO;
        [SerializeField] private PanelAnimationSO panelAnimationHideSO;

        public async Task PlayShowAnimation()
        {
            if (panelAnimationShowSO == null)
                await Task.CompletedTask;
            else
                await panelAnimationShowSO.CreateAnimation().PlayShow((RectTransform)transform);
        }

        public async Task PlayHideAnimation()
        {
            if (panelAnimationHideSO == null)
                await Task.CompletedTask;
            else
                await panelAnimationHideSO.CreateAnimation().PlayHide((RectTransform)transform);
        }

        public void PrepareElementToShow()
        {
            panelAnimationShowSO?.CreateAnimation().PrepareElementToShow((RectTransform)transform);
        }
    }
}