using System.Collections.Generic;
using UnityEngine;
using CatGame.Core.Interfaces;

/// <summary>
/// Responsável por fazer o Object Pooling do jogo.
/// </summary>
public class ObjectPooling : MonoBehaviour, IObjectPoolingService
{
    private readonly Dictionary<Component, object> componentPools = new();

    /// <summary>
    /// Cria uma pool ou retorna uma já existente.
    /// </summary>
    public IPool<T> GetOrCreatePool<T>(T prefab, int preLoad = 0) where T : Component
    {
        if (componentPools.TryGetValue(prefab, out var pool))
            return (IPool<T>) pool;

        Debug.Log($"Foi criada uma pool do prefab {prefab}");

        Pool<T> newPool = new(prefab, preLoad, transform);
        componentPools.Add(prefab, newPool);
        return newPool;
    }
}