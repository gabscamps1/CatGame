using CatGame.Core;
using CatGame.Core.Data;
using CatGame.Core.Interfaces;
using CatGame.Services.AudioManagement;
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

    [Header("Sounds")]
    public AudioData serverAudio;

    [Header("Animation")]
    [Tooltip("Se marcado, toca a animação de saída ao fim do tempo de exibição.")]
    public bool playExitAnimation;
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

    [Header("Exit Animation")]
    [Tooltip("Animator da mensagem. Se vazio, tenta pegar do serverMessageGameObject.")]
    [SerializeField] private Animator messageAnimator;
    [Tooltip("Nome do Trigger no Animator que dispara a animação de saída.")]
    [SerializeField] private string exitTriggerName = "Exit";
    [Tooltip("Duração da animação de saída em segundos (use o tamanho do clip).")]
    [SerializeField, Min(0)] private float exitAnimationDuration = 0.5f;

    [Header("Message IDs")]
    [SerializeField] private Message[] messages;

    private ILocalizationService localizationService;
    private IAudioService audioService;
    private int exitTriggerHash;

    public bool IsShowing { get; private set; }

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

        if (messageAnimator == null && serverMessageGameObject != null)
        {
            messageAnimator = serverMessageGameObject.GetComponent<Animator>();
        }

        exitTriggerHash = Animator.StringToHash(exitTriggerName);
    }

    private void Start()
    {
        TryGetAudioService();
    }

    private bool TryGetAudioService()
    {
        if (audioService != null) return true;

        try
        {
            audioService = ServiceLocator.Get<IAudioService>();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }

        return audioService != null;
    }

    #region Client

    private async void ShowMessage(MessageID messageID)
    {
        Debug.Log($"[ServerMessageManager] ShowMessage chamado: {messageID}");

        Message message = GetMessage(messageID);

        if (message == null)
        {
            CatGame.Core.Logger.LogWarning($"[{nameof(ServerMessageManager)}] Mensagem {messageID} não foi adicionada ao array.");
            return;
        }

        if (message.messageInfos == null || message.messageInfos.Length == 0)
        {
            Debug.LogWarning($"[ServerMessageManager] Mensagem {messageID} não tem nenhum MessageInfo.");
            return;
        }

        IsShowing = true;

        try
        {
            for (int i = 0; i < message.messageInfos.Length; i++)
            {
                MessageInfo messageInfo = message.messageInfos[i];
                Debug.Log($"[ServerMessageManager] Item {i} de {messageID} (textID: {messageInfo.textID})");

                PlayAudio(messageInfo, messageID, i);

                await ShowText(messageInfo);

                // Se o objeto foi destruído durante o await, para por aqui.
                if (this == null) return;
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
        finally
        {
            // IsShowing só volta a false depois da animação de saída terminar,
            // então quem espera a mensagem (ex.: tela de vitória) também espera a animação.
            IsShowing = false;
            Debug.Log($"[ServerMessageManager] Fim de {messageID}");
        }
    }

    private void PlayAudio(MessageInfo messageInfo, MessageID messageID, int index)
    {
        try
        {
            if (!TryGetAudioService())
            {
                Debug.LogWarning("[ServerMessageManager] audioService indisponível, pulando som.");
                return;
            }

            if (messageInfo.serverAudio == null)
            {
                Debug.LogWarning($"[ServerMessageManager] serverAudio vazio em {messageID}, item {index}.");
                return;
            }

            audioService.PlaySFXClip(messageInfo.serverAudio, false);
        }
        catch (Exception e)
        {
            // Falha no som não pode impedir o texto de aparecer.
            Debug.LogException(e);
        }
    }

    private async Task ShowText(MessageInfo messageInfo)
    {
        int millisecondsDelayToShow = (int)(messageInfo.timeToShow * 1000);
        int millisecondsDelayToHide = (int)(messageInfo.timeToHide * 1000);

        await Task.Delay(millisecondsDelayToShow);
        if (this == null) return;

        serverMessageText.text = localizationService.GetTextByID(messageInfo.textID);

        // Limpa um trigger de saída que tenha sobrado de uma execução anterior.
        if (messageAnimator != null)
        {
            messageAnimator.ResetTrigger(exitTriggerHash);
        }

        serverMessageGameObject.SetActive(true);

        await Task.Delay(millisecondsDelayToHide);
        if (this == null) return;

        if (messageInfo.playExitAnimation)
        {
            await PlayExitAnimation();
            if (this == null) return;
        }

        serverMessageGameObject.SetActive(false);
    }

    private async Task PlayExitAnimation()
    {
        if (messageAnimator == null)
        {
            Debug.LogWarning("[ServerMessageManager] playExitAnimation marcado, mas nenhum Animator foi atribuído.");
            return;
        }

        messageAnimator.SetTrigger(exitTriggerHash);

        int millisecondsExit = (int)(exitAnimationDuration * 1000);
        await Task.Delay(millisecondsExit);
    }

    private Message GetMessage(MessageID messageID)
    {
        return messages.FirstOrDefault(x => x.messageID == messageID);
    }

    #endregion

    #region Server

    [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
    public void ShowClientMessageRpc(MessageID messageID)
    {
        Debug.Log($"[ServerMessageManager] RPC recebido: {messageID}, IsShowing = {IsShowing}");

        if (IsShowing)
        {
            CatGame.Core.Logger.LogWarning("[ServerMessageManager] Tentou reproduzir a mensagem do server enquanto uma mensagem está sendo exibida.");
            return;
        }

        ShowMessage(messageID);
    }

    #endregion
}