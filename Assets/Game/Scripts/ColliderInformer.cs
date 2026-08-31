using UnityEngine;

public class ColliderInformer : MonoBehaviour
{
    public bool WasCombinedIn { get; set; }

    private bool _hasCollided;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!_hasCollided && !WasCombinedIn)
        {
            _hasCollided = true;
            ThrowFruitController.Instance.AllowThrowFruit();
            ThrowFruitController.Instance.PickFruit(FruitSelector.Instance.NextFruit);
            FruitSelector.Instance.PickNextFruit();
            Destroy(this);
        }
        
    }
    
}
