using UnityEngine;

namespace CatGame.Capabilities.UISystem
{
    [CreateAssetMenu(menuName = "UI System/Animations/MotionAnimationSO", fileName = "NewMotionAnimation_SO", order = 2)]
    public class MotionAnimationSO : PanelAnimationSO
    {
        [Header("Show Animation Settings")]
        [SerializeField] private Vector2 animationShowFrom;
        [SerializeField] private Vector2 animationShowTo;
        [SerializeField] private float motionInDuration;

        [Header("Hide Animation Settings")]
        [SerializeField] private Vector2 animationHideFrom;
        [SerializeField] private Vector2 animationHideTo;
        [SerializeField] private float motionOutDuration;

        public override IPanelAnimation CreateAnimation()
        {
            return new MotionAnimation(
                animationShowFrom, animationShowTo, motionInDuration,
                animationHideFrom, animationHideTo, motionOutDuration);
        }     
    }
}