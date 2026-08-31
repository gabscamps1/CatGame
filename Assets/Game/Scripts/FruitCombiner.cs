using UnityEngine;

public class FruitCombiner : MonoBehaviour
{
    private int _layerIndex;

    private FruitInfo _info;

    private void Awake()
    {
        _info = GetComponent<FruitInfo>();
        _layerIndex = gameObject.layer;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer != _layerIndex)
            return;

        if (collision.gameObject.TryGetComponent(out FruitInfo info))
            return;

        // Somente permite a combinação de duas frutas iguais.
        if (info.FruitIndex != _info.FruitIndex)
            return;

        int thisID = gameObject.GetInstanceID();
        int otherID = collision.gameObject.GetInstanceID();

        // Garante que somente uma das frutas que sofreu a colisão prossiga com esse método.
        if (thisID < otherID)
            return;

        GameManager.Instance.IncreaseScore(_info.PointsWhenAnnihilated);

        if (_info.FruitIndex == FruitSelector.Instance.Fruits.Length - 1)
        {
            Destroy(collision.gameObject);
            Destroy(gameObject);
        }

        else
        {
            Vector3 middlePosition = (transform.position + collision.transform.position) / 2f;
            GameObject go = Instantiate(SpawnCombinedFruit(_info.FruitIndex), GameManager.Instance.transform);
            go.transform.position = middlePosition;

            ColliderInformer informer = go.GetComponent<ColliderInformer>();
            if (informer != null)
            {
                informer.WasCombinedIn = true;
            }

            Destroy(collision.gameObject);
            Destroy(gameObject);
        }
    }

    private GameObject SpawnCombinedFruit(int index)
    {
        GameObject go = FruitSelector.Instance.Fruits[index + 1];
        return go;
    }
}
