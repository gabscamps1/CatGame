using System;
using UnityEngine;

public class ColliderInformer : MonoBehaviour
{
    public bool WasCombinedIn { get; set; }

    private bool hasCollided;

    public event EventHandler OnCollidedWithBase;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // PROBLEMA ATUAL - ao colidir na parede buga a lógica.
        if (!hasCollided && !WasCombinedIn)
        {
            hasCollided = true;
            FruitSelector.Instance.PickNextFruit();
            OnCollidedWithBase?.Invoke(this, EventArgs.Empty);
            Destroy(this);
        }
        
    }
    
}
