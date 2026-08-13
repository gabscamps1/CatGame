using UnityEngine;

namespace CatGame.Capabilities.UISystem
{
    public interface IHighlightPool
    {
        public void Enqueue(Component obj);
        public Component Dequeue(Transform parent);
    }
}