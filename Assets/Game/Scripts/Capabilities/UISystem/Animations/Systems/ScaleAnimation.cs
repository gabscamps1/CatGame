using System.Threading.Tasks;
using UnityEngine;

namespace CatGame.Capabilities.UISystem
{
    public class ScaleAnimation : IPanelAnimation
    {
        private readonly Vector2 animationShowFrom;
        private readonly Vector2 animationShowTo;
        private readonly float scaleInDuration;

        private readonly Vector2 animationHideFrom;
        private readonly Vector2 animationHideTo;
        private readonly float scaleOutDuration;

        public ScaleAnimation(
            Vector2 animationShowFrom, Vector2 animationShowTo, float scaleInDuration,
            Vector2 animationHideFrom, Vector2 animationHideTo, float scaleOutDuration)
        {
            this.animationShowFrom = animationShowFrom;
            this.animationShowTo = animationShowTo; 
            this.scaleInDuration = scaleInDuration; 

            this.animationHideFrom = animationHideFrom; 
            this.animationHideTo = animationHideTo;
            this.scaleOutDuration = scaleOutDuration;
        }

        public void PrepareElementToShow(RectTransform rectTransform)
        {
            rectTransform.localScale = animationShowFrom;
        }

        public async Task PlayShow(RectTransform rectTransform)
        {
            if (rectTransform == null)
            {
                throw new System.Exception();
            }

            await PlayScaleAnimation(animationShowFrom, animationShowTo, scaleInDuration, rectTransform);
        }

        public async Task PlayHide(RectTransform rectTransform)
        {
            if (rectTransform == null)
            {
                throw new System.Exception();
            }

            await PlayScaleAnimation(animationHideFrom, animationHideTo, scaleOutDuration, rectTransform);
        }
       
        private async Task PlayScaleAnimation(Vector2 from, Vector2 to, float fadeDuration, RectTransform rectTransform)
        {
            rectTransform.localScale = to;


            float elapsed = 0;
            while (elapsed < fadeDuration)
            {
                float scaleX = Mathf.Lerp(from.x, to.y, elapsed / fadeDuration);
                float scaleY = Mathf.Lerp(from.y, to.y, elapsed / fadeDuration);
                rectTransform.localScale = new Vector2(scaleX, scaleY);      

                elapsed += Time.unscaledDeltaTime;
                await Task.Yield();
            }

            rectTransform.localScale = to;
        }
    }
}