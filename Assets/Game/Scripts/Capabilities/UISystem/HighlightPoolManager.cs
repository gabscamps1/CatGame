using CatGame.Core.Enums;
using System.Collections.Generic;
using UnityEngine;
using Logger = CatGame.Core.Logger;

namespace CatGame.Capabilities.UISystem
{
    public class HighlightPoolManager : MonoBehaviour
    {
        public HighlightPoolManager Instance => highlightObjectPool;
        private HighlightPoolManager highlightObjectPool;
        
        private readonly Dictionary<(PlayerId, IHighlighVisual), HightlightPool<Component>> hightlightObjectPools = new();

        private void Awake()
        {
            
        }

        public HightlightPool<Component> GetOrCreatePool(PlayerId player, IHighlighVisual prefab)
        {
            if (prefab is not Component highlighVisual)
            {
                Logger.LogError($"HightlightVisual não é um prefab");
                return null;
            }


            (PlayerId, IHighlighVisual) pair = (player, prefab);

            if (hightlightObjectPools.TryGetValue(pair, out HightlightPool<Component> item))
                return item;

            HightlightPool<Component> pool = new HightlightPool<Component>(highlighVisual, transform);
            return hightlightObjectPools[pair] = pool;
        }
    }
}

