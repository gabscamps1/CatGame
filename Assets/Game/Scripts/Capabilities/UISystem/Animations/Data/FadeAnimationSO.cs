using UnityEngine;

namespace CatGame.Capabilities.UISystem
{
    [CreateAssetMenu(menuName = "UI System/Animations/FadeAnimationSO", fileName = "NewFadeAnimation_SO", order = 0)]
    public class FadeAnimationSO : PanelAnimationSO
    {
        [Header("Show Animation Settings")]
        [SerializeField] private float showFrom;
        [SerializeField] private float showTo;
        [SerializeField] private float fadeInDuration;

        [Header("Hide Animation Settings")]
        [SerializeField] private float hideFrom;
        [SerializeField] protected float hideTo;
        [SerializeField] private float fadeOutDuration;

        public override IPanelAnimation CreateAnimation()
        {
            return new FadeAnimation(
                showFrom, showTo, fadeInDuration, 
                hideFrom, hideTo, fadeOutDuration);
        }
    }
}