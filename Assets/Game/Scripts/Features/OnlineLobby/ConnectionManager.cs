using Unity.Netcode;
using UnityEngine;

public class ConnectionManager : MonoBehaviour
{
    public static ConnectionManager Instance;

    private bool isMatchStarted = false;
    private bool isFullLobby = false;

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
        if (isFullLobby)
        {
            response.Approved = false;
            response.Reason = "Full Lobby";
            return;
        }

        if (isMatchStarted)
        {
            response.Approved = false;
            response.Reason = "The match has started.";
            return;
        }

        response.Approved = true;
        response.CreatePlayerObject = false;
        response.Pending = false;
    }

    /// <summary>
    /// Fecha o server para os clients que tentarem entrar.
    /// </summary>
    public void CloseConnections()
    {
        isMatchStarted = true;
    }

    /// <summary>
    /// Abre o server para outros clients entrarem.
    /// </summary>
    public void OpenConnections()
    {
        isMatchStarted = false;
    }

    public void FullLobby(bool isFullLobby)
    {
        this.isFullLobby = isFullLobby;
    }
}