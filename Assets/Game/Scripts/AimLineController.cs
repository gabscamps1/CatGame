using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class AimLineController : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private Transform fruitThrowTransform;
    [SerializeField] private Transform bottomTransform;

    [Header("Tracejado")]
    [SerializeField] private float dashLength = 0.3f;   // tamanho de um traço + espaço (unidades do mundo)
    [SerializeField] private float scrollSpeed = 1f;    // repetições do tracejado por segundo
    [SerializeField] private bool scrollDown = true;

    private LineRenderer lineRenderer;
    private Material lineMaterial;

    private float topPos;
    private float bottomPos;
    private float x;
    private float scrollOffset;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.textureMode = LineTextureMode.Tile;

        // Instância própria do material pra poder animar o offset
        lineMaterial = lineRenderer.material;
    }

    private void Start()
    {
        GameManager.Instance.OnGameStateChanged += GameManager_OnGameStateChanged;
        GameManager_OnGameStateChanged(GameManager.Instance.GetGameState());
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStateChanged -= GameManager_OnGameStateChanged;

        if (lineMaterial != null)
            Destroy(lineMaterial);
    }

    private void GameManager_OnGameStateChanged(GameManager.GameState currentState)
    {
        switch (currentState)
        {
            case GameManager.GameState.NotInit:
            case GameManager.GameState.GameOver:
                gameObject.SetActive(false);
                break;

            case GameManager.GameState.Playing:
                gameObject.SetActive(true);
                break;
        }
    }

    private void Update()
    {
        UpdateLinePositions();
        UpdateDashScroll();
    }

    private void UpdateLinePositions()
    {
        x = fruitThrowTransform.position.x;
        topPos = fruitThrowTransform.position.y;
        bottomPos = bottomTransform.position.y;

        lineRenderer.SetPosition(0, new Vector3(x, topPos));
        lineRenderer.SetPosition(1, new Vector3(x, bottomPos));
    }

    private void UpdateDashScroll()
    {
        // No modo Tile a escala é "repetições por unidade do mundo",
        // então o tamanho do traço fica constante mesmo se a linha mudar de comprimento.
        lineMaterial.mainTextureScale = new Vector2(1f / dashLength, 1f);

        // Mantém entre 0 e 1 pra não perder precisão com o tempo
        scrollOffset = Mathf.Repeat(scrollOffset + scrollSpeed * Time.deltaTime, 1f);

        // Posição 0 = topo, posição 1 = base.
        // Offset negativo faz o padrão descer; positivo, subir.
        float signedOffset = scrollDown ? -scrollOffset : scrollOffset;
        lineMaterial.mainTextureOffset = new Vector2(signedOffset, 0f);
    }
}