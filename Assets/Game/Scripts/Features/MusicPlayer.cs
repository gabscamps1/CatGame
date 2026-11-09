using CatGame.Core;
using CatGame.Core.Data;
using CatGame.Core.Interfaces;
using CatGame.Services.AudioManagement;
using UnityEngine;



public class MusicPlayer : MonoBehaviour
{
    [Header("Sounds")]
    [SerializeField] private AudioData MATCH_MUSIC;
    [SerializeField] private AudioData MATCH_LOSE;

    private IAudioService audioService;

    private void Start()
    {
        audioService = ServiceLocator.Get<IAudioService>();
        GameManager.Instance.OnGameStateChanged += GameManager_OnGameStateChanged;   
    }

    private void OnDestroy()
    {
        if (GameManager.Instance)
            GameManager.Instance.OnGameStateChanged -= GameManager_OnGameStateChanged;   
    }

    private void GameManager_OnGameStateChanged(GameManager.GameState state)
    {
        switch (state)
        {
            case GameManager.GameState.Playing:
                if (!audioService.IsPlayingThisMusic(MATCH_MUSIC))
                {
                    audioService.PlayMusic(MATCH_MUSIC, true);
                }
                break;

            case GameManager.GameState.GameOver:
                audioService.StopMusic();
                audioService.PlaySFXClip(MATCH_LOSE, false);

                break;
        }
    }
}
