using UnityEngine;

namespace CatGame.Core.Interfaces
{
    public interface IObjectPoolingService
    {
        public IPool<T> GetOrCreatePool<T>(T prefab, int preLoad) where T : Component;
    }
}