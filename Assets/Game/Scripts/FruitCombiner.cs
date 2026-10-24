using CatGame.Core;
using CatGame.Core.Events;
using CatGame.Features.CharacterSelection;
using Unity.Netcode;
using UnityEngine;

public class FruitCombiner : NetworkBehaviour
{
    private FruitInfo fruitInfo;

    private void Awake()
    {
        fruitInfo = GetComponent<FruitInfo>();

        if (!IsSpawned)
        {
            CatGame.Core.Logger.Log("Não foi iniciado ainda");
            gameObject.SetActive(false);
        }
    }

    public override void OnNetworkSpawn()
    {
        gameObject.SetActive(true);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!IsServer)
            return;

        if (!collision.gameObject.TryGetComponent(out FruitInfo otherFruitInfo))
            return;
        Debug.Log("Testeeeee");
        // Somente permite a combinação de duas frutas iguais.
        if (otherFruitInfo.FruitIndex != fruitInfo.FruitIndex)
            return;

        int thisID = gameObject.GetInstanceID();
        int otherID = collision.gameObject.GetInstanceID();

        // Garante que somente uma das frutas que sofreu a colisão prossiga com esse método.
        if (thisID < otherID)
            return;
        Debug.Log("Testeeeee2");

        GameManager.Instance.IncreaseScore(fruitInfo.PointsWhenAnnihilated);
        
        Vector3 middlePosition = (transform.position + collision.transform.position) / 2f;

        GameManager.Instance.ShowScore(middlePosition, fruitInfo.PointsWhenAnnihilated);

        /////////Instantiate(TextAlignment, middlePosition);
        Debug.Log(middlePosition);
        Debug.Log(this.fruitInfo.PointsWhenAnnihilated); // <---- SPAWNAR O TEXTO COM OS PONTOS NA COORDENADA (GABRIEL)

        if (FruitSelector.Instance.IsLastTypeOfFruit(fruitInfo.FruitIndex))
        {
            Destroy(collision.gameObject);
            Destroy(gameObject);
        }
        else
        {
            //Vector3 middlePosition = (transform.position + collision.transform.position) / 2f;
            GameObject go = Instantiate(SpawnCombinedFruit(this.fruitInfo.FruitIndex), GameManager.Instance.transform);
            go.transform.position = middlePosition;

            ColliderInformer informer = go.GetComponent<ColliderInformer>();
            if (informer != null)
            {
                informer.WasCombinedIn = true;
            }

            Destroy(collision.gameObject);
            Destroy(gameObject);
        }

        UpdateScoreInClientRpc(this.fruitInfo.PointsWhenAnnihilated);
    }

    [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
    private void UpdateScoreInClientRpc(int score)
    {
        for (int i = 0; i < MatchSetup.Players.Count; i++)
        {
            if (MatchSetup.Players[i].OwnerId != NetworkManager.LocalClientId)
                continue;
            
            int playerIndex = i;

            /*EventBus.Publish(new ScoreChangedEvent() 
            { 
                PlayerIndex = playerIndex, 
                ScoreAmount = score 
            });*/
        }      
    }

    private GameObject SpawnCombinedFruit(int index)
    {
        GameObject go = FruitSelector.Instance.GetPhysicalFruit(index + 1);
        return go;
    }
}
