using UnityEngine;

public class AimLineController : MonoBehaviour
{
    [SerializeField] private Transform fruitThrowTransform;
    [SerializeField] private Transform bottomTransform;

    private LineRenderer lineRenderer;

    private float topPos;
    private float bottomPos;
    private float x;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    private void Start()
    {
        GameManager.Instance.OnGameStateChanged += GameManager_OnGameStateChanged;
        GameManager_OnGameStateChanged(GameManager.Instance.GetGameState());
    }
  
    private void OnDestroy()
    {
        GameManager.Instance.OnGameStateChanged -= GameManager_OnGameStateChanged;
    }

    private void GameManager_OnGameStateChanged(GameManager.GameState currentState)
    {
        switch (currentState)
        {
            case GameManager.GameState.NotInit:
            case GameManager.GameState.GameOver:
                enabled = false;
                break;

            case GameManager.GameState.Playing:
                enabled = true; 
                break;
        }
    }

    private void Update()
    {
        x = fruitThrowTransform.position.x;
        topPos = fruitThrowTransform.position.y;
        bottomPos = bottomTransform.position.y;

        lineRenderer.SetPosition(0, new Vector3(x, topPos));
        lineRenderer.SetPosition(1, new Vector3(x, bottomPos));
    }
}
