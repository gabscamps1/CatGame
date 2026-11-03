using CatGame.Core;
using CatGame.Core.Data;
using CatGame.Core.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private Image _gameOverPanel;
    [SerializeField] private float _fadeTime = 2f;

    public GameObject ScoreWhenCombined;

    public float TimeTillGameOver = 1.5f;

    // Player Settings
    [Header("ToPlayer")]
    public BoxCollider2D[] Boundaries => boundaries;
    [SerializeField] private BoxCollider2D[] boundaries; // Na ordem. Boundary Player 1, Player2, ...
    private readonly NetworkList<PlayerId> playerLost = new();

    // Player
    public List<PlayerId> PlayerLost { get; private set; } = new();
    public event Action<PlayerId> OnPlayerLost;

    // Game State Settings
    public enum GameState : byte { NotInit, Playing, GameOver }
    private readonly NetworkVariable<GameState> serverGameState = new(GameState.NotInit);
    private GameState localGameState = GameState.NotInit;
    public event Action<GameState> OnGameStateChanged;


    

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            CatGame.Core.Logger.LogWarning("[GameManager] Objeto duplicado.");
            Destroy(this);
            return;
        }
    }

    private void OnEnable()
    {
        //SceneManager.sceneLoaded += FadeGame;
    }

    private void OnDisable()
    {
        //SceneManager.sceneLoaded -= FadeGame;
    }

    public override void OnNetworkSpawn()
    {
        serverGameState.OnValueChanged += GameManager_OnGameStateChanged;
        playerLost.OnListChanged += PlayerLostList_OnListChanged;

        // Inicializa o estado local.
        GameManager_OnGameStateChanged(GameState.NotInit, serverGameState.Value);
    }

    public override void OnNetworkDespawn()
    {
        serverGameState.OnValueChanged -= GameManager_OnGameStateChanged;
        playerLost.OnListChanged -= PlayerLostList_OnListChanged;
    }

    #region Player

    private void PlayerLostList_OnListChanged(NetworkListEvent<PlayerId> changeEvent)
    {
        PlayerLost.Add(changeEvent.Value);           
        OnPlayerLost?.Invoke(changeEvent.Value);
    }

    #endregion

    #region Game State

    private void GameManager_OnGameStateChanged(GameState previous, GameState current)
    {
        localGameState = current;
        OnGameStateChanged?.Invoke(current);
    }

    public void SetGameState(GameState gameState)
    {
        if (!IsSpawned || !NetworkManager.IsServer || serverGameState.Value == gameState) return;
        serverGameState.Value = gameState;
    }

    public GameState GetGameState()
    {
        return localGameState;
    }

    #endregion

    #region Game

    public void StartGame()
    {
        if (!IsSpawned || !NetworkManager.IsServer || serverGameState.Value == GameState.Playing) return;
        serverGameState.Value = GameState.Playing;
    }

    public void PlayerGameOver(PlayerId player)
    {
        if (!IsSpawned || !IsServer || playerLost.Contains(player) || player == PlayerId.None) return;
        playerLost.Add(player);
    }

    public void GameOver()
    {
        if (!IsSpawned || !NetworkManager.IsServer || serverGameState.Value == GameState.GameOver) return;        
        serverGameState.Value = GameState.GameOver;
    }

    #endregion





    

    public void ShowScore(Vector3 position, int score)
    {
        Debug.Log(position + " " + score);

        GameObject scoreText = Instantiate(
            ScoreWhenCombined,
            position,
            Quaternion.identity
        );

        TextMeshPro text = scoreText.GetComponent<TextMeshPro>();
        text.text = score.ToString();
    }

    private IEnumerator ResetGame()
    {
        if (_gameOverPanel)
            yield break;

        _gameOverPanel.gameObject.SetActive(true);

        Color startColor = _gameOverPanel.color;
        startColor.a = 0f;
        _gameOverPanel.color = startColor;

        float elapsedTime = 0f;
        while(elapsedTime < _fadeTime)
        {
            elapsedTime += Time.deltaTime;

            float newAlpha = Mathf.Lerp(0f, 1f, (elapsedTime / _fadeTime));
            startColor.a = newAlpha;
            _gameOverPanel.color = startColor;

            yield return null;
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void FadeGame(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(FadeGameIn());
    }

    private IEnumerator FadeGameIn()
    {
        _gameOverPanel.gameObject.SetActive(true);
        Color startColor = _gameOverPanel.color;
        startColor.a = 1f;
        _gameOverPanel.color = startColor;

        float elapsedTime = 0f;
        while(elapsedTime < _fadeTime)
        {
            elapsedTime += Time.deltaTime;

            float newAlpha = Mathf.Lerp(1f, 0f, (elapsedTime / _fadeTime));
            startColor.a = newAlpha;
            _gameOverPanel.color = startColor;

            yield return null;
        }

        _gameOverPanel.gameObject.SetActive(false);
    }


   
}
