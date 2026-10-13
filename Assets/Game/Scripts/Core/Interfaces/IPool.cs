using UnityEngine;

namespace CatGame.Core.Interfaces
{
    public interface IPool<T>
    {
        public void Enqueue(T obj);
        public T Dequeue(Transform parent);
        public T Get(int value);
        public bool ObjectBelongsThisPool(T obj);
        public int Count { get; }
    }
}