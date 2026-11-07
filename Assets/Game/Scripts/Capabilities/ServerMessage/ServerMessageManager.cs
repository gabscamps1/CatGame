using System;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public enum StateMessage
{
    StartMessage,
    TimeOver,
    PlayersOver
}

[Serializable]
public class MessagesIDs
{
    public StateMessage stateMessage;
    public string[] ids;
}

public class ServerMessageManager : NetworkBehaviour
{
    public static ServerMessageManager Instance;

    public Dictionary<StateMessage, string[]> messages = new()
    {
        { StateMessage.StartMessage, new string [1] { ""} },
        { StateMessage.TimeOver, new string[1] {""} } ,
        { StateMessage.PlayersOver, new string[] {"d"} } ,
    };

    [Header("Components")]
    [SerializeField] private GameObject serverMessageGameObject;
    [SerializeField] private TextMeshProUGUI serverMessageText;

    [Header("Message IDs")]
    [SerializeField] private MessagesIDs[] messagesIDs;

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
