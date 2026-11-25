
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UISparkleGenerator : MonoBehaviour
{
    [Header("Sparkle Settings")]
    [SerializeField] private Image sparklePrefab;
    [SerializeField] private int amount = 20;
    [SerializeField] private Vector2 sparkleSize = new Vector2(20, 20);

    [Header("Opacity")]
    [Range(0f, 1f)]
    [SerializeField] private float minAlpha = 0.2f;

    [Range(0f, 1f)]
    [SerializeField] private float maxAlpha = 1f;

    [Header("Scale Animation")]
    [SerializeField] private float minScale = 0.7f;
    [SerializeField] private float maxScale = 1.3f;
    [SerializeField] private float scaleSpeed = 2f;

    [Header("Rotation Animation")]
    [SerializeField] private float rotationSpeed = 40f;
    [SerializeField] private float rotationAmplitude = 30f;

    [Header("Blink Animation")]
    [SerializeField] private float blinkSpeed = 2f;

    [Header("Spawn Animation")]
    [SerializeField] private float spawnDuration = 0.4f;

    [Header("Lifetime")]
    [SerializeField] private float minLifetime = 2f;
    [SerializeField] private float maxLifetime = 4f;
    [SerializeField] private float fadeOutDuration = 0.8f;

    [Header("General")]
    [SerializeField] private bool useUnscaledTime = true;
    [SerializeField] private bool randomRotation = true;

    private class Sparkle
    {
        public RectTransform rect;
        public Image image;

        public float phase;
        public float rotationOffset;
        public float rotationDirection;
        public float blinkOffset;

        public float lifetime;
        public float elapsed;

        public float spawnElapsed;

        public bool isFading;
        public float fadeElapsed;
    }

    private readonly List<Sparkle> sparkles = new();

    private RectTransform parentRect;

    private void Start()
    {
        parentRect = GetComponent<RectTransform>();

        if (sparklePrefab == null || parentRect == null)
        {
            Debug.LogError("[Sparkles] Configure o Prefab e o RectTransform!");
            return;
        }

        // Cria as sparkles iniciais
        for (int i = 0; i < amount; i++)
        {
            CreateSparkle();
        }
    }

    private void CreateSparkle()
    {
        Image img = Instantiate(sparklePrefab, parentRect);

        RectTransform rect = img.rectTransform;

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        // Posição aleatória dentro da área do Canvas
        rect.anchoredPosition = new Vector2(
            Random.Range(-parentRect.rect.width / 2f, parentRect.rect.width / 2f),
            Random.Range(-parentRect.rect.height / 2f, parentRect.rect.height / 2f)
        );

        rect.sizeDelta = sparkleSize;

        img.raycastTarget = false;
        img.color = Color.white;

        // TOCAR EFEITO SONORO AQUI
        // Exemplo: AudioManager.Instance.PlaySFX(...);

        Sparkle sparkle = new Sparkle();

        sparkle.rect = rect;
        sparkle.image = img;

        sparkle.phase = Random.Range(0f, Mathf.PI * 2f);
        sparkle.rotationOffset = Random.Range(0f, 360f);
        sparkle.rotationDirection = Random.value > 0.5f ? 1f : -1f;
        sparkle.blinkOffset = Random.Range(0f, Mathf.PI * 2f);

        sparkle.lifetime = Random.Range(
            minLifetime,
            maxLifetime
        );

        sparkle.elapsed = 0f;
        sparkle.spawnElapsed = 0f;

        sparkle.isFading = false;
        sparkle.fadeElapsed = 0f;

        // Começa invisível e sem tamanho
        rect.localScale = Vector3.zero;

        Color color = img.color;
        color.a = 0f;
        img.color = color;

        sparkles.Add(sparkle);
    }

    private void Update()
    {
        float deltaTime = useUnscaledTime
            ? Time.unscaledDeltaTime
            : Time.deltaTime;

        float time = useUnscaledTime
            ? Time.unscaledTime
            : Time.time;

        // Percorre de trás para frente para permitir remover elementos
        for (int i = sparkles.Count - 1; i >= 0; i--)
        {
            Sparkle s = sparkles[i];

            if (s.rect == null)
            {
                sparkles.RemoveAt(i);
                continue;
            }

            // SPAWN ANIMATION
            s.spawnElapsed += deltaTime;

            float spawnProgress = Mathf.Clamp01(
                s.spawnElapsed / Mathf.Max(0.01f, spawnDuration)
            );

            float spawnFactor = Mathf.SmoothStep(
                0f,
                1f,
                spawnProgress
            );

            // Lifetime começa após terminar o spawn
            if (spawnProgress >= 1f)
            {
                s.elapsed += deltaTime;
            }

            // Inicia fade out ao terminar o lifetime
            if (s.elapsed >= s.lifetime && spawnProgress >= 1f)
            {
                s.isFading = true;
            }

            // SCALE ANIMATION
            float scaleWave = (
                Mathf.Sin(time * scaleSpeed + s.phase) + 1f
            ) / 2f;

            float scale = Mathf.Lerp(
                minScale,
                maxScale,
                scaleWave
            );

            // Aplica animação de spawn na escala
            s.rect.localScale = Vector3.one * (
                scale * spawnFactor
            );

            // OPACITY ANIMATION
            float alphaWave = (
                Mathf.Sin(time * blinkSpeed + s.blinkOffset) + 1f
            ) / 2f;

            float alpha = Mathf.Lerp(
                minAlpha,
                maxAlpha,
                alphaWave
            );

            // Aplica animação de spawn na opacidade
            alpha *= spawnFactor;

            // FADE OUT
            if (s.isFading)
            {
                s.fadeElapsed += deltaTime;

                float fadeProgress = Mathf.Clamp01(
                    s.fadeElapsed / Mathf.Max(0.01f, fadeOutDuration)
                );

                float fade = 1f - Mathf.SmoothStep(
                    0f,
                    1f,
                    fadeProgress
                );

                alpha *= fade;

                // Destrói somente quando o fade terminar
                if (fadeProgress >= 1f)
                {
                    Destroy(s.rect.gameObject);
                    sparkles.RemoveAt(i);

                    // Cria uma nova sparkle em outra posição
                    CreateSparkle();

                    continue;
                }
            }

            Color color = s.image.color;
            color.a = alpha;
            s.image.color = color;

            // ROTATION ANIMATION
            float rotation = Mathf.Sin(
                time * rotationSpeed * 0.01f + s.phase
            ) * rotationAmplitude;

            float direction = randomRotation
                ? s.rotationDirection
                : 1f;

            s.rect.localRotation = Quaternion.Euler(
                0f,
                0f,
                s.rotationOffset + rotation * direction
            );
        }
    }

    private void OnDestroy()
    {
        sparkles.Clear();
    }
}
