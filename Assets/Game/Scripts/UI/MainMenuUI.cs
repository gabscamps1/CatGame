using UnityEngine;
using CatGame.Capabilities.UISystem;

public class MainMenuUI : BasePanel
{
    [Header("Main UI Settings")]
    [SerializeField] private ButtonElement quitGameButton;

    private void Awake()
    {
        quitGameButton.OnSubmittedEvent += QuitGameButton_OnSubmittedEvent;
    }

    private void QuitGameButton_OnSubmittedEvent(object sender, ButtonElement.SubmittedEvent e)
    {
        Application.Quit();
    }
}
