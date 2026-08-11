using UnityEngine;

public class ColliderInformation : MonoBehaviour
{
    public bool WasCombinedIn { get; set; }

    private bool _hasCollided;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!_hasCollided && !WasCombinedIn)
        {
            _hasCollided = true;
            ThrowFruitController.instance.CanThrow = true;
            ThrowFruitController.instance.SpawnAFruit(FruitSelector.instance.NextFruit);
            FruitSelector.instance.PickNextFruit();
            Destroy(this);
        }
        
    }
    
}
