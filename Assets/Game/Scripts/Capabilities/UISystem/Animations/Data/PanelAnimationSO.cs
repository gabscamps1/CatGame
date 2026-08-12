using UnityEngine;

namespace CatGame.Capabilities.UISystem
{
    public abstract class PanelAnimationSO : ScriptableObject
    {
        public abstract IPanelAnimation CreateAnimation();
    }
}

