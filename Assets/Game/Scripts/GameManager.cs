using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using CatGame.Core.Data;
using CatGame.Services.Register;
using Unity.Netcode;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI _scoreText;
    [SerializeField] private Image _gameOverPanel;
    [SerializeField] private float _fadeTime = 2f;

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

        _scoreText.text = currentScore.ToString("D8");
    }

    private void Start()
    {
        foreach (PlayerId playerId in PlayerIdRegister.PlayerRegistereds.Keys)
        {
            AssignPlayerInGameArea(playerId);
        }
    }

    #region Gameplay

    public void AssignPlayerInGameArea(PlayerId playerId)
    {
        if (!IsServer)
            return;
    }

    #endregion

    public void IncreaseScore(int amount)
    {
           //CRIA PREFAB DE PONTO
        currentScore += amount;
        _scoreText.text = currentScore.ToString("D8");
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
