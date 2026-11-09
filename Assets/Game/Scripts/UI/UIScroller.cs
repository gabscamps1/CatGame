
using UnityEngine;
using UnityEngine.UI;

public class InfiniteUIScroll : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RawImage rawImage;

    [Header("Scroll Settings")]
    [SerializeField] private Vector2 scrollSpeed = new Vector2(0.1f, 0f);

    private Vector2 offset;

    private void Update()
    {
        offset += scrollSpeed * Time.deltaTime;

        // Mantém os valores entre 0 e 1
        offset.x = Mathf.Repeat(offset.x, 1f);
        offset.y = Mathf.Repeat(offset.y, 1f);

        // Atualiza o UV da imagem
        rawImage.uvRect = new Rect(
            offset.x,
            offset.y,
            1f,
            1f
        );
    }
}
