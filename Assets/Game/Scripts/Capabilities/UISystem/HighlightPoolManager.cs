using CatGame.Core.Enums;
using System;
using System.Collections.Generic;
using UnityEngine;
using Logger = CatGame.Core.Logger;

namespace CatGame.Capabilities.UISystem
{
    public class HighlightPoolManager : MonoBehaviour
    {
        public HighlightPoolManager Instance => highlightObjectPool;
        private HighlightPoolManager highlightObjectPool;
        
        private readonly Dictionary<(PlayerId, IHighlighVisual), IHighlightPool> hightlightObjectPools = new();

        private void Awake()
        {
            if (highlightObjectPool == null)
            {
                highlightObjectPool = this;
            }
            else if (highlightObjectPool != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        public void AssignHighlightToTarget(IHighlighVisual prefab, NavigableElement element, PlayerId player = PlayerId.P1)
        {
            IHighlightPool pool = GetOrCreatePool(player, prefab);
            AssignHighlightToTarget(pool, element, player);
        }

        public void AssignHighlightToTarget(IHighlightPool pool, NavigableElement element, PlayerId player = PlayerId.P1)
        {
            Component obj = pool.Dequeue(transform);

            if (obj is not IHighlighVisual visual)
                return;

            visual.AttachTo(element, player);
        }

        private IHighlightPool GetOrCreatePool(PlayerId player, IHighlighVisual prefab)
        {
            if (prefab is not Component highlighVisual)
            {
                Logger.LogError($"[HighlightPoolManager] {prefab} não é um prefab");
                return null;
            }

            (PlayerId, IHighlighVisual) pair = (player, prefab);

            if (hightlightObjectPools.TryGetValue(pair, out IHighlightPool item))
                return item;

            HighlightPool pool = new (highlighVisual, transform);
            return hightlightObjectPools[pair] = pool;
        }
    }
}

