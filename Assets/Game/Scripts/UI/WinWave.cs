
using UnityEngine;
using UnityEngine.UI;

public class WinWave : MonoBehaviour
{
    [Header("Images")]
    [SerializeField] private Image[] images = new Image[10];

    [Header("Wave Settings")]
    [SerializeField] private float jumpHeight = 15f;
    [SerializeField] private float speed = 0.8f;
    [SerializeField] private float delayBetweenImages = 0.12f;

    [Header("General")]
    [SerializeField] private bool useUnscaledTime = true;

    private Vector2[] originalPositions;

    private void Start()
    {
        originalPositions = new Vector2[images.Length];

        // Guarda as posições originais de cada imagem
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] != null)
            {
                originalPositions[i] = images[i].rectTransform.anchoredPosition;
            }
        }
    }

    private void Update()
    {
        float time = useUnscaledTime
            ? Time.unscaledTime
            : Time.time;

        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] == null)
                continue;

            // Cada imagem começa sua animação com um pequeno atraso
            float wave = Mathf.Sin(
                (time - i * delayBetweenImages) * speed * Mathf.PI * 2f
            );

            // Movimento vertical
            float offsetY = Mathf.Max(0f, wave) * jumpHeight;

            images[i].rectTransform.anchoredPosition =
                originalPositions[i] + Vector2.up * offsetY;
        }
    }
}
