using Unity.Netcode;
using UnityEngine;

public class ConnectionManager : MonoBehaviour
{
    public static ConnectionManager Instance;

    private bool canJoin = true;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this) 
        {
            Destroy(gameObject);
            return;
        }

    }

    private void Start()
    {
        NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton) NetworkManager.Singleton.ConnectionApprovalCallback -= ApprovalCheck;
    }

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        response.Approved = canJoin;
        response.CreatePlayerObject = false;
        response.Pending = false;

        if (!canJoin)
            response.Reason = "The match has started.";
    }

    /// <summary>
    /// Fecha o server para os clients que tentarem entrar.
    /// </summary>
    public void CloseConnections()
    {
        canJoin = false;
    }

    /// <summary>
    /// Abre o server para outros clients entrarem.
    /// </summary>
    public void OpenConnections()
    {
        canJoin = true;
    }
}