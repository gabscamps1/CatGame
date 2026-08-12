using UnityEngine;

namespace CatGame.Capabilities.UISystem
{
    [CreateAssetMenu(menuName = "UI System/Animations/ScaleAnimationSO", fileName = "NewScaleAnimation_SO", order = 1)]
    public class ScaleAnimationSO : PanelAnimationSO
    {
        [Header("Show Animation Settings")]
        [SerializeField] private Vector2 animationShowFrom;
        [SerializeField] private Vector2 animationShowTo;
        [SerializeField] private float scaleInDuration;

        [Header("Hide Animation Settings")]
        [SerializeField] private Vector2 animationHideFrom;
        [SerializeField] private Vector2 animationHideTo;
        [SerializeField] private float scaleOutDuration;

        public override IPanelAnimation CreateAnimation()
        {
            return new ScaleAnimation(
                animationShowFrom, animationShowTo, scaleInDuration, 
                animationHideFrom, animationHideTo, scaleOutDuration);
        }
    }
}