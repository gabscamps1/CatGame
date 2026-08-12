using System.Threading.Tasks;
using UnityEngine;
using Logger = CatGame.Core.Logger;

namespace CatGame.Capabilities.UISystem
{
    public class FadeAnimation : IPanelAnimation
    {
        private readonly float animationShowFrom;
        private readonly float animationShowTo;
        private readonly float fadeInDuration;

        private readonly float animationHideFrom;
        private readonly float animationHideTo;
        private readonly float fadeOutDuration;

        public FadeAnimation(
            float animationShowFrom, float animationShowTo, float fadeInDuration,
            float animationHideFrom, float animationHideTo, float fadeOutDuration)
        {
            this.animationShowFrom = animationShowFrom;
            this.animationShowTo = animationShowTo;
            this.fadeInDuration = fadeInDuration;

            this.animationHideFrom = animationHideFrom;
            this.animationHideTo = animationHideTo;
            this.fadeOutDuration = fadeOutDuration;
        }

        public void PrepareElementToShow(RectTransform rectTransform)
        {
            rectTransform.gameObject.SetActive(false);
        }

        public async Task PlayShow(RectTransform rectTransform)
        {
            if (rectTransform == null)
            {
                throw new System.Exception();
            }

            rectTransform.gameObject.SetActive(true);
            await PlayFadeAnimation(animationShowFrom, animationShowTo, fadeInDuration, rectTransform);
        }

        public async Task PlayHide(RectTransform rectTransform)
        {
            if (rectTransform == null)
            {
                throw new System.Exception();
            }

            await PlayFadeAnimation(animationHideFrom, animationHideTo, fadeOutDuration, rectTransform);
        }

        private async Task PlayFadeAnimation(float from, float to, float fadeDuration, RectTransform rectTransform)
        {
            if (!rectTransform.TryGetComponent(out CanvasGroup canvasGroup))
            {
                canvasGroup = rectTransform.gameObject.AddComponent<CanvasGroup>();
                Logger.LogWarning($"Adicionar o CanvasGroup ao GameObject {rectTransform.gameObject.name}");
            }

            canvasGroup.alpha = from;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            float elapsed = 0;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime; // Funciona pausado.
                canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
                await Task.Yield();
            }

            canvasGroup.alpha = to;

            bool visible = to > 0.5f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }        
    }
}
