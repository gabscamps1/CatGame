using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using System;
using CatGame.Core;
using CatGame.Core.Interfaces;
using CatGame.Core.Data;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState : byte { NotInit, Playing, Finished }
    private readonly NetworkVariable<GameState> serverGameState = new(GameState.NotInit);
    private GameState localGameState = GameState.NotInit;
    public event Action OnGameStateChanged;


    [SerializeField] private Image _gameOverPanel;
    [SerializeField] private float _fadeTime = 2f;

    [Header("ToPlayer")]
    public BoxCollider2D[] Boundaries => boundaries;
    [SerializeField] private BoxCollider2D[] boundaries; // Na ordem. Boundary Player 1, Player2, ...

    public GameObject ScoreWhenCombined;

    private int currentScore;
    public float TimeTillGameOver = 1.5f;

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

        // Inicializa o estado local.
        GameManager_OnGameStateChanged(GameState.NotInit, serverGameState.Value);
    }

    public override void OnNetworkDespawn()
    {
        serverGameState.OnValueChanged -= GameManager_OnGameStateChanged;
    }

    private void GameManager_OnGameStateChanged(GameState previous, GameState current)
    {
        localGameState = current;
        OnGameStateChanged?.Invoke();
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

    public void GameOver()
    {
        StartCoroutine(ResetGame());
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
