using UnityEngine;
using CatGame.Capabilities.UISystem;
using UnityEngine.UI;
using CatGame.Core;
using CatGame.Core.Enums;
using CatGame.Core.Interfaces;

public class MainMenuUI : BasePanel
{
    [Header("Main UI Settings")]
    [SerializeField] private Button versusStartButton;
    [SerializeField] private Button quitGameButton;

    private void Awake()
    {
        versusStartButton.onClick.AddListener(() =>
        {
            ServiceLocator.Get<ISceneLoadingService>().LoadAsyncScene(Scenes.Playing);
        });

        quitGameButton.onClick.AddListener(() =>
        {
            Application.Quit();
        });
    }
}
