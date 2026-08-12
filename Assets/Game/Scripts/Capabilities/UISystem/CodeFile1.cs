using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Logger = CatGame.Core.Logger;

namespace CatGame.Capabilities.UISystem
{
    public class HightlightPool<T> where T : Component
    {
        private readonly Queue<T> pool = new();
        private readonly T prefab;
        private readonly Transform generalParent;

        /// <summary>
        /// Número de objetos na pool.
        /// </summary>
        public int Count => pool.Count;

        public HightlightPool(T prefab, Transform poolTransform)
        {
            this.prefab = prefab;
            this.generalParent = poolTransform;
        }

        /// <summary>
        /// Registra um <paramref name="obj"/> na fila para serem reutilizados.
        /// </summary>
        public void Enqueue(T obj)
        {
            obj.gameObject.SetActive(false);
            obj.transform.SetParent(generalParent, false);
            pool.Enqueue(obj);
        }

        /// <summary>
        /// Remove e retorna um objeto da fila.
        /// </summary>
        public T Dequeue(Transform parent)
        {
            if (parent == null)
                Logger.LogWarning($"Parent da pool {prefab.name} é null. Dependendo da Main Scene que esteja ativada, as intâncias do prefab desta pool podem persistir ATIVADO entre cenas.");

            if (pool.Count > 0)
            {
                T obj = pool.Dequeue();
                obj.transform.SetParent(parent, false);
                obj.gameObject.SetActive(true);

                return obj;
            }
            else
            {
                return Object.Instantiate(prefab, parent);
            }
        }

        /// <summary>
        /// Retorna um objeto da fila sem removê-lo.
        /// </summary>
        public T Get(int value)
        {
            if (pool.Count > 0 && pool.Count > value)
            {
                return pool.ElementAt(value);
            }
            else
            {
                T obj = Object.Instantiate(prefab, generalParent);
                pool.Enqueue(obj);
                return obj;
            }
        }
    }
}