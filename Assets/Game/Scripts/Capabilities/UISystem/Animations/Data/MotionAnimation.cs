using System.Threading.Tasks;
using UnityEngine;
using Logger = CatGame.Core.Logger;

namespace CatGame.Capabilities.UISystem
{
    public class MotionAnimation : IPanelAnimation
    {
        private readonly Vector2 animationShowFrom;
        private readonly Vector2 animationShowTo;
        private readonly float motionInDuration;

        private readonly Vector2 animationHideFrom;
        private readonly Vector2 animationHideTo;
        private readonly float motionOutDuration;

        public MotionAnimation(
            Vector2 animationShowFrom, 
            Vector2 animationShowTo, 
            float motionInDuration,
            Vector2 animationHideFrom, 
            Vector2 animationHideTo, 
            float motionOutDuration)
        {
            this.animationShowFrom = animationShowFrom; 
            this.animationShowTo = animationShowTo;
            this.motionInDuration = motionInDuration;

            this.animationHideFrom = animationHideFrom;
            this.animationHideTo = animationHideTo; 
            this.motionOutDuration = motionOutDuration; 
        }

        public void PrepareElementToShow(RectTransform rectTransform)
        {
            rectTransform.gameObject.SetActive(false);
            rectTransform.anchoredPosition = animationShowFrom;
        }

        public async Task PlayShow(RectTransform rectTransform)
        {
            if (rectTransform == null)
            {
                throw new System.Exception();
            }

            rectTransform.gameObject.SetActive(true);
            await PlayMotionAnimation(animationShowFrom, animationShowTo, motionInDuration, rectTransform);
        }

        public async Task PlayHide(RectTransform rectTransform)
        {
            if (rectTransform == null)
            {
                throw new System.Exception();
            }

            await PlayMotionAnimation(animationHideFrom, animationHideTo, motionOutDuration, rectTransform);
        }

        private async Task PlayMotionAnimation(Vector2 from, Vector2 to, float motionDuration, RectTransform rectTransform)
        {
            if (!rectTransform.TryGetComponent(out CanvasGroup canvasGroup))
            {
                canvasGroup = rectTransform.gameObject.AddComponent<CanvasGroup>();
                Logger.LogWarning($"Adicionar o CanvasGroup ao GameObject {rectTransform.gameObject.name}");
            }

            rectTransform.anchoredPosition = from;

            float elapsed = 0;
            while (elapsed < motionDuration)
            {
                elapsed += Time.unscaledDeltaTime; // Funciona pausado.
                rectTransform.anchoredPosition = Vector2.Lerp(from, to, elapsed / motionDuration);
                await Task.Yield();
            }

            rectTransform.anchoredPosition = to;
        }
    }
}