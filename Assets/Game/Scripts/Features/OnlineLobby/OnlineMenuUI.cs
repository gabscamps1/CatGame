using CatGame.Capabilities.UISystem;
using UnityEngine;
using CatGame.Features.OnlineLobby;

public class OnlineMenuUI : BaseUIScreen
{
    [SerializeField] private ButtonElement createLobbyButton;
    [SerializeField] private OnlineCreateLobbyPresenter onlineCreateLobbyPresenter;

    private void Awake()
    {
        createLobbyButton.OnSubmittedEvent += CreateLobbyButton_OnSubmittedEvent;
    }

    private void OnDestroy()
    {
        createLobbyButton.OnSubmittedEvent -= CreateLobbyButton_OnSubmittedEvent;
    }

    private void CreateLobbyButton_OnSubmittedEvent(object sender, NavigableElement.SubmittedEvent e)
    {
        onlineCreateLobbyPresenter.CreateLobby();
    }
}
