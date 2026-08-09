using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Central controller: holds the fruit tier list, resolves merges, tracks score,
/// and detects the game-over condition (fruit resting above the danger line too long).
/// Put this on an empty GameObject called "GameManager".
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Fruit Setup")]
    [Tooltip("Index 0 = smallest fruit. Must be ordered by tier.")]
    public List<FruitData> fruitTiers;

    [Tooltip("Only tiers 0..maxSpawnTier-1 are chosen randomly when spawning a new fruit")]
    public int maxSpawnTier = 4;

    [Header("Game Over")]
    [Tooltip("An empty Transform placed near the top of the jar as the danger line")]
    public Transform dangerLine;
    public float gameOverDelay = 2f;

    [Header("UI")]
    public TextMeshProUGUI scoreText;
    public GameObject gameOverPanel;

    private int score = 0;
    private bool isGameOver = false;
    private float dangerTimer = 0f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Update()
    {
        if (isGameOver) return;
        CheckGameOverCondition();
    }

    public FruitData GetFruitData(int tier)
    {
        if (tier < 0 || tier >= fruitTiers.Count) return null;
        return fruitTiers[tier];
    }

    public int GetRandomSpawnTier()
    {
        return Random.Range(0, Mathf.Min(maxSpawnTier, fruitTiers.Count));
    }

    public void MergeFruits(Fruit a, Fruit b, Vector2 mergePoint)
    {
        if (isGameOver) return;

        a.hasMerged = true;
        b.hasMerged = true;

        int currentTier = a.tier;
        int nextTier = currentTier + 1;

        AddScore(GetFruitData(currentTier).scoreValue);

        Destroy(a.gameObject);
        Destroy(b.gameObject);

        // Top tier (e.g. watermelon) just disappears with big points - no next tier exists.
        if (nextTier < fruitTiers.Count)
        {
            FruitData data = fruitTiers[nextTier];
            GameObject newFruit = Instantiate(data.prefab, mergePoint, Quaternion.identity);
            Fruit fruitComp = newFruit.GetComponent<Fruit>();
            fruitComp.tier = nextTier;
            fruitComp.MarkDropped(); // it's already "in play", can merge again immediately
        }
    }

    public void AddScore(int amount)
    {
        score += amount;
        if (scoreText != null) scoreText.text = "Score: " + score;
    }

    void CheckGameOverCondition()
    {
        Fruit[] allFruits = FindObjectsOfType<Fruit>();
        bool anyAboveLine = false;

        foreach (var f in allFruits)
        {
            if (!f.isDropped) continue; // ignore the fruit currently being aimed at the top
            if (f.transform.position.y > dangerLine.position.y)
            {
                anyAboveLine = true;
                break;
            }
        }

        if (anyAboveLine)
        {
            dangerTimer += Time.deltaTime;
            if (dangerTimer >= gameOverDelay) TriggerGameOver();
        }
        else
        {
            dangerTimer = 0f;
        }
    }

    void TriggerGameOver()
    {
        isGameOver = true;
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }
}
