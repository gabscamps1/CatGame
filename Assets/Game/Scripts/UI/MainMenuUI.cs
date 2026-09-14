using UnityEngine;
using CatGame.Capabilities.UISystem;

public class MainMenuUI : BasePanel
{
    [Header("Main UI Settings")]
    [SerializeField] private ButtonElement versusStartButton;
    [SerializeField] private ButtonElement quitGameButton;

    private void Awake()
    {
        /*versusStartButton.onClick.AddListener(() =>
        {
            ServiceLocator.Get<ISceneLoadingService>().LoadAsyncScene(Scenes.Playing);
        });*/

        quitGameButton.OnSubmittedEvent += QuitGameButton_OnSubmittedEvent;
    }

    private void QuitGameButton_OnSubmittedEvent(object sender, ButtonElement.SubmittedEvent e)
    {
        Application.Quit();
    }
}
