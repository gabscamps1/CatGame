using UnityEngine;

namespace CatGame.Capabilities.UISystem
{
    [System.Serializable]
    public class AnimationStep
    {
        [Tooltip("Elemento que vai animar neste passo")]
        [field: SerializeField] public PanelElementAnimator Element { get; private set; }

        [Tooltip("Tocar em paralelo com o elemento anterior. Opção desativada fará com que o último passo seja reproduzido antes de começar este.")]
        [field: SerializeField] public bool PlayInParallel { get; private set; } = true;

        [Tooltip("Espera este tempo antes de iniciar este passo. Somente funciona caso a opção playInParallel esteja ativada")]
        [field: SerializeField] public float Delay { get; private set; }
    }
}