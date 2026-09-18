using System.Collections;
using TMPro;
using UnityEngine;

public class ShowCombinedScoreText : MonoBehaviour
{
    [SerializeField] private float moveDistance = 1f;
    [SerializeField] private float moveDuration = 1f;
    [SerializeField] private float fadeDuration = 0.5f;

    private TextMeshPro text;

    private void Awake()
    {
        text = GetComponent<TextMeshPro>();
    }

    private void Start()
    {
        StartCoroutine(Animate());
    }

    private IEnumerator Animate()
    {
        Vector3 startPosition = transform.position;
        Vector3 endPosition = startPosition + Vector3.up * moveDistance;

        float elapsed = 0f;

        // Movimento para cima
        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / moveDuration;

            transform.position = Vector3.Lerp(
                startPosition,
                endPosition,
                t
            );

            yield return null;
        }

        // Fade Out
        Color color = text.color;
        elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / fadeDuration;

            color.a = Mathf.Lerp(1f, 0f, t);
            text.color = color;

            yield return null;
        }

        Destroy(gameObject);
    }
}
