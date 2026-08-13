using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Logger = CatGame.Core.Logger;

namespace CatGame.Capabilities.UISystem
{
    public class HighlightPool : IHighlightPool
    {
        private readonly Queue<Component> pool = new(); 
        private readonly Component prefab;
        private readonly Transform generalParent;

        /// <summary>
        /// Número de objetos na pool.
        /// </summary>
        public int Count => pool.Count;

        public HighlightPool(Component prefab, Transform poolTransform)
        {
            this.prefab = prefab;
            this.generalParent = poolTransform;
        }

        /// <summary>
        /// Registra um <paramref name="obj"/> na fila para serem reutilizados.
        /// </summary>
        public void Enqueue(Component obj)
        {
            obj.gameObject.SetActive(false);
            obj.transform.SetParent(generalParent, false);
            pool.Enqueue(obj);
        }

        /// <summary>
        /// Remove e retorna um objeto da fila.
        /// </summary>
        public Component Dequeue(Transform parent)
        {
            if (parent == null)
                Logger.LogWarning($"Parent da pool {prefab.name} é null. Dependendo da Main Scene que esteja ativada, as intâncias do prefab desta pool podem persistir ATIVADO entre cenas.");

            if (pool.Count > 0)
            {
                Component obj = pool.Dequeue();
                obj.transform.SetParent(parent, false);
                obj.gameObject.SetActive(true);

                return obj;
            }
            else
            {
                return Object.Instantiate(prefab, parent);
            }
        }
    }
}