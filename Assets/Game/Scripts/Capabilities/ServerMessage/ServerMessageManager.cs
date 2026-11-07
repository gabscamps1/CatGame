using CatGame.Core;
using CatGame.Core.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public enum MessageID : byte
{
    StartGame,
    TimeOver,
    PlayersOver
}

[Serializable]
public class MessageInfo
{
    public string textID;
    [Min(0)] public float timeToShow;
    [Min(0)] public float timeToHide;
}

[Serializable]
public class Message
{
    public MessageID messageID;
    public MessageInfo[] messageInfos;
}

public class ServerMessageManager : NetworkBehaviour
{
    public static ServerMessageManager Instance;

    [Header("Components")]
    [SerializeField] private GameObject serverMessageGameObject;
    [SerializeField] private TextMeshProUGUI serverMessageText;

    [Header("Message IDs")]
    [SerializeField] private Message[] messages;

    private ILocalizationService localizationService;

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

        localizationService = ServiceLocator.Get<ILocalizationService>();
    }

    #region Client

    private async void ShowMessage(MessageID messageID)
    {
        Message message = GetMessage(messageID);

        if (message == null)
        {
            CatGame.Core.Logger.LogWarning($"[{nameof(ServerMessageManager)}] Mensagem não foi adicionada ao array.");
            return;
        }

        int messageCount = message.messageInfos.Length;

        for (int i = 0; i < messageCount; i++)
        {
            MessageInfo messageInfo = message.messageInfos[i];
            await ShowText(messageInfo);
        }
    }

    private async Task ShowText(MessageInfo messageInfo)
    {
        string textId = messageInfo.textID;
        float timeToShow = messageInfo.timeToShow;
        float timeToHide = messageInfo.timeToHide;

        int millisecondsDelayToShow = (int)(timeToShow * 1000);
        int millisecondsDelayToHide = (int)(timeToHide * 1000);

        await Task.Delay(millisecondsDelayToShow);

        string text = localizationService.GetTextByID(textId);
        serverMessageText.text = text;
        serverMessageGameObject.SetActive(true);

        await Task.Delay(millisecondsDelayToHide);
        serverMessageGameObject.SetActive(false);
    }

    private Message GetMessage(MessageID messageID)
    {
        return messages.Where(x => x.messageID == messageID).FirstOrDefault();
    }

    #endregion

    #region Server

    [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
    public void ShowClientMessageRpc(MessageID messageID)
    {
        ShowMessage(messageID);
    }

    #endregion

    #region Not-Implemented

    /*public void HideMessage()
    {
        serverMessageGameObject.SetActive(false);
    }*/

    /*[Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
    public void HideClientMessageRpc()
    {
        HideMessage();
    }*/

    #endregion
}
