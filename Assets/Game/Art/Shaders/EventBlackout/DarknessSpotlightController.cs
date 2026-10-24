using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Evento "Escuridão" com o shader UI/DarknessSpotlight.
/// Fases: 1) Entrada: a tela escurece até mostrar o ponto de luz.
///        2) Patrulha: a luz vai e volta (direita ↔ esquerda) até o tempo acabar.
///        3) Saída: o ponto de luz cresce e revela a tela toda.
/// Coloque em um RawImage de tela cheia. Chame Play() para iniciar.
/// </summary>
[RequireComponent(typeof(RawImage))]
public class DarknessSpotlightController : MonoBehaviour
{
    static readonly int ProgressId = Shader.PropertyToID("_Progress");
    static readonly int IntensityId = Shader.PropertyToID("_Intensity");
    static readonly int RevealId = Shader.PropertyToID("_Reveal");
    static readonly int AspectId = Shader.PropertyToID("_Aspect");
    static readonly int CustomTimeId = Shader.PropertyToID("_CustomTime");
    static readonly int UseCustomTimeId = Shader.PropertyToID("_UseCustomTime");

    [Header("Tempos (segundos)")]
    [Tooltip("Entrada: tempo para a tela escurecer até mostrar o ponto de luz.")]
    [Min(0f)] public float entryTime = 1.5f;
    [Tooltip("Tempo em que a luz fica indo e voltando.")]
    [Min(0.1f)] public float patrolDuration = 10f;
    [Tooltip("Saída: tempo para o ponto de luz crescer e revelar a tela toda.")]
    [Min(0f)] public float exitTime = 1.5f;

    [Header("Movimento da luz")]
    [Tooltip("Tempo de uma passada (de um lado até o outro). Menor = mais rápido.")]
    [Min(0.1f)] public float sweepDuration = 3f;
    [Tooltip("Ligado: a luz começa na direita e vai para a esquerda.")]
    public bool startFromRight = true;
    [Tooltip("Curva de cada passada. Linear = velocidade constante; EaseInOut = desacelera nas pontas (mais fluido).")]
    public AnimationCurve movement = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Tooltip("Curva do crescimento do ponto de luz na saída.")]
    public AnimationCurve revealCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Tempo")]
    [Tooltip("Ignora Time.timeScale (útil se o jogo pausar durante o evento).")]
    public bool useUnscaledTime;
    public bool playOnEnable;

    [Header("Eventos")]
    public UnityEvent onStarted;
    public UnityEvent onPatrolFinished;
    public UnityEvent onFinished;

    RawImage image;
    RectTransform rect;
    Material instance;
    Coroutine routine;

    public bool IsPlaying => routine != null;
    public float TotalDuration => entryTime + patrolDuration + exitTime;
    /// <summary>Material em uso, para mudar raio, suavidade etc. por código.</summary>
    public Material Material => instance;

    float Dt => useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

    void Awake()
    {
        image = GetComponent<RawImage>();
        rect = (RectTransform)transform;
        instance = new Material(image.material);
        image.material = instance;
        image.raycastTarget = false;
        image.enabled = false;
    }

    void OnEnable()
    {
        if (playOnEnable) Play();
    }

    void OnDestroy()
    {
        if (instance != null) Destroy(instance);
    }

    [ContextMenu("Play")]
    public void Play()
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Run());
    }

    public void Stop()
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;
        image.enabled = false;
    }

    IEnumerator Run()
    {
        image.enabled = true;
        onStarted.Invoke();

        // 1) Entrada: escurece até mostrar o ponto (luz parada na posição inicial)
        float p = EvaluatePosition(0f);
        float t = 0f;
        Apply(p, 0f, 0f);
        while (t < entryTime)
        {
            t += Dt;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / entryTime));
            Apply(p, k, 0f);
            yield return null;
        }

        // 2) Patrulha: vai e volta até acabar o tempo
        t = 0f;
        while (t < patrolDuration)
        {
            t += Dt;
            p = EvaluatePosition(t);
            Apply(p, 1f, 0f);
            yield return null;
        }
        onPatrolFinished.Invoke();

        // 3) Saída: o ponto cresce a partir de onde a luz parou e revela a tela toda
        t = 0f;
        while (t < exitTime)
        {
            t += Dt;
            float r = revealCurve.Evaluate(Mathf.Clamp01(t / exitTime));
            // nos últimos 20% some o resto da escuridão para não dar "pop"
            float fade = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.8f, 1f, r));
            Apply(p, fade, r);
            yield return null;
        }

        Apply(p, 0f, 1f);
        image.enabled = false;
        routine = null;
        onFinished.Invoke();
    }

    // Posição 0..1 (0 = esquerda, 1 = direita) no tempo t da patrulha
    float EvaluatePosition(float t)
    {
        float pingPong = Mathf.PingPong(t / sweepDuration, 1f);
        float e = movement.Evaluate(pingPong);
        return startFromRight ? 1f - e : e;
    }

    void Apply(float progress, float intensity, float reveal)
    {
        instance.SetFloat(ProgressId, progress);
        instance.SetFloat(IntensityId, intensity);
        instance.SetFloat(RevealId, reveal);

        if (rect.rect.height > 0f)
            instance.SetFloat(AspectId, rect.rect.width / rect.rect.height);

        instance.SetFloat(UseCustomTimeId, useUnscaledTime ? 1f : 0f);
        if (useUnscaledTime)
            instance.SetFloat(CustomTimeId, Time.unscaledTime);
    }
}
