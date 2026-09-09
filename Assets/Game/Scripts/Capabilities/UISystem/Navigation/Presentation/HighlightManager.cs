using CatGame.Core.Enums;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Logger = CatGame.Core.Logger;

namespace CatGame.Capabilities.UISystem
{
    public class HighlightManager : MonoBehaviour
    {
        public static HighlightManager Instance => highlightObjectPool;
        private static HighlightManager highlightObjectPool;
        
        private readonly Dictionary<(PlayerId, IHighlightVisual), IHighlightPool> hightlightObjectPools = new();

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

        public IHighlightVisual AssignHighlightToTarget(IHighlightVisual prefab, NavigableElement element, PlayerId player = PlayerId.P1)
        {
            IHighlightPool pool = GetOrCreatePool(player, prefab);

            Component obj = pool.Dequeue(transform);

            if (obj is not IHighlightVisual visual)
                return null;

            visual.AttachTo(element, player);

            return visual;
        }

        public void UnassignHightlightOfTarget(IHighlightVisual prefab, IHighlightVisual instance, PlayerId player = PlayerId.P1)
        {
            if (prefab == null || instance == null)
                return;

            if (instance is not Component highlightComponent)
                return;

            if (highlightComponent == null || highlightComponent.IsDestroyed())
            {
                Logger.LogError("Tá destruido porra");
                return;
            }

            instance.Disattach(player);

            IHighlightPool pool = GetOrCreatePool(player, prefab);
            pool.Enqueue(highlightComponent);
        }

        public IHighlightPool GetOrCreatePool(PlayerId player, IHighlightVisual prefab)
        {
            if (prefab is not Component highlighVisual)
            {
                Logger.LogError($"[HighlightPoolManager] {prefab} não é um prefab");
                return null;
            }

            (PlayerId, IHighlightVisual) pair = (player, prefab);

            if (hightlightObjectPools.TryGetValue(pair, out IHighlightPool item))
                return item;

            HighlightPool pool = new (highlighVisual, transform);
            return hightlightObjectPools[pair] = pool;
        }
    }
}

