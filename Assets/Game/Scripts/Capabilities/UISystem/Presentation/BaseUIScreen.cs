using System;
using System.Threading.Tasks;
using UnityEngine;

namespace CatGame.Capabilities.UISystem
{
    public abstract class BaseUIScreen : MonoBehaviour
    {
        [Header("Animation")]
        [SerializeField] AnimationStep[] animationStepsToShow;
        [SerializeField] AnimationStep[] animationStepsToHide;

        public bool IsVisible { get; private set; }

        public async Task Show(Action onComplete = null)
        {
            if (IsVisible)
                return;

            IsVisible = true;

            if (animationStepsToShow != null && animationStepsToShow.Length > 0)
                foreach (var step in animationStepsToShow) step.Element.PrepareElementToShow();

            gameObject.SetActive(true);
            OnBeforeShow();

            if (animationStepsToShow != null && animationStepsToShow.Length > 0)
                await SequenceAnimation(animationStepsToShow, true);

            OnAfterShow();
            onComplete?.Invoke();
        }

        public async Task Hide(Action onComplete = null)
        {
            if (!IsVisible)
                return;

            IsVisible = false;

            OnBeforeHide();

            if (animationStepsToHide != null && animationStepsToHide.Length > 0)
                await SequenceAnimation(animationStepsToHide, false);

            gameObject.SetActive(false);

            OnAfterHide();
            onComplete?.Invoke();
        }

        #region Animation

        private async Task SequenceAnimation(AnimationStep[] animationSteps, bool isOpen)
        {
            AnimationStep previousStep = null;

            foreach (var step in animationSteps)
            {
                if (previousStep == null)
                {
                    previousStep = step;
                    continue;
                }

                if (previousStep.PlayInParallel)
                {
                    int delay = (int)(previousStep.Delay * 1000);
                    await Task.Delay(delay);
                }

                if (step.PlayInParallel)
                    _ = Play(previousStep.Element);
                else
                    await Play(previousStep.Element);

                previousStep = step;
            }

            if (previousStep.PlayInParallel)
            {
                int delay = (int)(previousStep.Delay * 1000);
                await Task.Delay(delay);
            }

            await Play(previousStep.Element);

            async Task Play(PanelElementAnimator element)
            {
                if (isOpen)
                    await element.PlayShowAnimation();
                else
                    await element.PlayHideAnimation();
            }
        }

        #endregion

        #region Subclass Override

        /// <summary> Chamado antes de o painel aparecer. </summary>
        protected virtual void OnBeforeShow() { }

        /// <summary> Chamado depois que a transição de entrada termina. </summary>
        protected virtual void OnAfterShow() { Debug.Log($"Abriu: {gameObject.name}"); }

        /// <summary> Chamado antes de o painel sumir. </summary>
        protected virtual void OnBeforeHide() { }

        /// <summary> Chamado depois que a transição de saída termina. </summary>
        protected virtual void OnAfterHide() { Debug.Log($"Fechou: {name}"); }

        #endregion
    }
}