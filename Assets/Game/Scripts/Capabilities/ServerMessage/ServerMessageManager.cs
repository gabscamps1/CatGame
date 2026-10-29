using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class ServerMessageManager : NetworkBehaviour
{
    public static ServerMessageManager Instance;

    [SerializeField] private GameObject serverMessageGameObject;
    [SerializeField] private TextMeshProUGUI serverMessageText;

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

    public void ShowMessage(string text)
    {
        serverMessageText.text = text;
        serverMessageGameObject.SetActive(true);
    }

    public void HideMessage()
    {
        serverMessageGameObject.SetActive(false);
    }

    [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
    public void ShowClientMessageRpc(FixedString32Bytes text)
    {
        ShowMessage(text.ToString());
    }

    [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
    public void HideClientMessageRpc()
    {
        HideMessage();
    }
}
