using UnityEngine;

public class FruitCombiner : MonoBehaviour
{
    private FruitInfo info;

    private void Awake()
    {
        info = GetComponent<FruitInfo>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.TryGetComponent(out FruitInfo info))
            return;

        // Somente permite a combinação de duas frutas iguais.
        if (info.FruitIndex != this.info.FruitIndex)
            return;

        int thisID = gameObject.GetInstanceID();
        int otherID = collision.gameObject.GetInstanceID();

        // Garante que somente uma das frutas que sofreu a colisão prossiga com esse método.
        if (thisID < otherID)
            return;

        GameManager.Instance.IncreaseScore(this.info.PointsWhenAnnihilated);

        if (FruitSelector.Instance.IsLastTypeOfFruit(this.info.FruitIndex))
        {
            Destroy(collision.gameObject);
            Destroy(gameObject);
        }
        else
        {
            Vector3 middlePosition = (transform.position + collision.transform.position) / 2f;
            GameObject go = Instantiate(SpawnCombinedFruit(this.info.FruitIndex), GameManager.Instance.transform);
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
        GameObject go = FruitSelector.Instance.GetPhysicalFruit(index + 1);
        return go;
    }
}
