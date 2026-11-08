using UnityEngine;

public class FadeInLoseTape : MonoBehaviour
{
    private const string PROGRESS = "_Progress";

    [Header("Settings")]
    [SerializeField, Min(0.1f)] private float effectDuration = 1f;

    public bool IsFinished { get; private set; }

    private SpriteRenderer sprite;
    private Material spriteMaterial;
    private float countDuration;
    private bool executingEffect;

    private void Awake()
    {
        sprite = GetComponent<SpriteRenderer>();
        spriteMaterial = sprite.material;

        ResetEffect();
    }

    private void Update()
    {
        if (!executingEffect)
            return;
              
        float currentProgress = Mathf.Clamp01(countDuration / effectDuration);

        countDuration += Time.deltaTime;
        spriteMaterial.SetFloat(PROGRESS, currentProgress);

        if (currentProgress >= 1)
        {
            executingEffect = false;
            IsFinished = true;
            enabled = false;
        }
    }

    private void ResetEffect()
    {
        countDuration = 0;
        spriteMaterial.SetFloat(PROGRESS, countDuration);
    }

    public void PlayEffect()
    {
        ResetEffect();
        executingEffect = true;
        enabled = true;
        gameObject.SetActive(true);
    }
}
