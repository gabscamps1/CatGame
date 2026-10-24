using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Evento "Neblina" com o shader UI/Neblina.
/// Para desfocar e refratar a tela, o shader precisa "ver" o jogo. Por isso, durante o evento,
/// a câmera do jogo renderiza para uma RenderTexture e o RawImage mostra essa imagem já processada.
/// Fora do evento, a câmera volta ao normal e o RawImage é desligado.
/// Fases: fade-in → efeito completo (duration) → fade-out.
/// Coloque em um RawImage de tela cheia, o PRIMEIRO (mais ao fundo) do Canvas Overlay.
/// </summary>
[RequireComponent(typeof(RawImage))]
public class NeblinaController : MonoBehaviour
{
    static readonly int IntensityId = Shader.PropertyToID("_Intensity");
    static readonly int SeedId = Shader.PropertyToID("_Seed");
    static readonly int AspectId = Shader.PropertyToID("_Aspect");
    static readonly int UseBackgroundId = Shader.PropertyToID("_UseBackground");
    static readonly int CustomTimeId = Shader.PropertyToID("_CustomTime");
    static readonly int UseCustomTimeId = Shader.PropertyToID("_UseCustomTime");

    [Header("Tempos (segundos)")]
    [Tooltip("Tempo para a neblina e a chuva surgirem.")]
    [Min(0f)] public float fadeInTime = 2f;
    [Tooltip("Tempo com o efeito no máximo.")]
    [Min(0.1f)] public float duration = 10f;
    [Tooltip("Tempo para tudo sumir.")]
    [Min(0f)] public float fadeOutTime = 2f;

    [Header("Captura da tela (desfoque e refração)")]
    [Tooltip("Desligado: só a neblina e as gotas são desenhadas por cima (sem desfoque nem refração reais).")]
    public bool useCapture = true;
    [Tooltip("Câmera que renderiza o jogo. Vazio = Camera.main.")]
    public Camera sourceCamera;
    [Tooltip("Resolução da captura. Menor = mais leve e mais borrado.")]
    [Range(0.25f, 1f)] public float renderScale = 0.75f;
    [Tooltip("Cria uma câmera vazia que renderiza para a tela enquanto a câmera do jogo está desviada para a RenderTexture. Evita o aviso 'No cameras rendering'. Só é criada se nenhuma outra câmera estiver renderizando para a tela.")]
    public bool createDisplayCamera = true;

    [Header("Opções")]
    [Tooltip("Sorteia uma nova semente a cada Play (nuvens e gotas diferentes).")]
    public bool randomizeSeed = true;
    [Tooltip("Ignora Time.timeScale.")]
    public bool useUnscaledTime;
    public bool playOnEnable;

    [Header("Eventos")]
    public UnityEvent onStarted;
    public UnityEvent onFinished;

    RawImage image;
    RectTransform rect;
    Material instance;
    Coroutine routine;

    Camera captureCam;
    RenderTexture previousTarget;
    RenderTexture rt;
    GameObject displayCamObj;
    bool capturing;

    public bool IsPlaying => routine != null;
    public float TotalDuration => fadeInTime + duration + fadeOutTime;
    /// <summary>Material em uso, para mudar parâmetros por código.</summary>
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

    void OnDisable()
    {
        Stop();
    }

    void OnDestroy()
    {
        if (instance != null) Destroy(instance);
    }

    [ContextMenu("Play")]
    public void Play()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            EndCapture();
        }
        routine = StartCoroutine(Run());
    }

    public void Stop()
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;
        EndCapture();
        if (image != null) image.enabled = false;
    }

    IEnumerator Run()
    {
        BeginCapture();
        if (randomizeSeed) instance.SetFloat(SeedId, Random.Range(0f, 100f));
        Apply(0f);
        image.enabled = true;
        onStarted.Invoke();

        yield return Fade(fadeInTime, 0f, 1f);

        float t = 0f;
        while (t < duration)
        {
            t += Dt;
            Apply(1f);
            yield return null;
        }

        yield return Fade(fadeOutTime, 1f, 0f);

        Apply(0f);
        EndCapture();
        image.enabled = false;
        routine = null;
        onFinished.Invoke();
    }

    IEnumerator Fade(float time, float from, float to)
    {
        float t = 0f;
        while (t < time)
        {
            t += Dt;
            Apply(Mathf.SmoothStep(from, to, Mathf.Clamp01(t / time)));
            yield return null;
        }
    }

    void Apply(float intensity)
    {
        EnsureCapture();

        instance.SetFloat(IntensityId, intensity);

        if (rect.rect.height > 0f)
            instance.SetFloat(AspectId, rect.rect.width / rect.rect.height);

        instance.SetFloat(UseCustomTimeId, useUnscaledTime ? 1f : 0f);
        if (useUnscaledTime)
            instance.SetFloat(CustomTimeId, Time.unscaledTime);
    }

    // ---------- Captura da câmera ----------

    void BeginCapture()
    {
        capturing = false;
        instance.SetFloat(UseBackgroundId, 0f);
        if (!useCapture) return;

        captureCam = sourceCamera != null ? sourceCamera : Camera.main;
        if (captureCam == null) return;

        previousTarget = captureCam.targetTexture;
        CreateRT();
        captureCam.targetTexture = rt;
        image.texture = rt;
        instance.SetFloat(UseBackgroundId, 1f);
        capturing = true;

        if (createDisplayCamera) EnsureDisplayCamera();
    }

    // Se a câmera do jogo está indo para a RenderTexture e nenhuma outra câmera desenha na tela,
    // a Unity avisa "No cameras rendering". Esta câmera vazia (preto, sem objetos) resolve isso.
    void EnsureDisplayCamera()
    {
        foreach (Camera c in Camera.allCameras)
        {
            if (c != captureCam && c.enabled && c.targetTexture == null
                && c.targetDisplay == captureCam.targetDisplay)
                return; // já existe uma câmera renderizando para a tela
        }

        displayCamObj = new GameObject("NeblinaDisplayCamera");
        Camera cam = displayCamObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.cullingMask = 0;
        cam.depth = -100f;
        cam.orthographic = true;
        cam.allowHDR = false;
        cam.allowMSAA = false;
        cam.useOcclusionCulling = false;
        cam.targetDisplay = captureCam.targetDisplay;
    }

    void CreateRT()
    {
        int w = Mathf.Max(16, Mathf.RoundToInt(Screen.width * renderScale));
        int h = Mathf.Max(16, Mathf.RoundToInt(Screen.height * renderScale));
        rt = new RenderTexture(w, h, 24)
        {
            name = "NeblinaCapture",
            antiAliasing = 1,
            filterMode = FilterMode.Bilinear
        };
        rt.Create();
    }

    // Recria a captura se o tamanho da tela mudou durante o evento
    void EnsureCapture()
    {
        if (!capturing || rt == null) return;

        int w = Mathf.Max(16, Mathf.RoundToInt(Screen.width * renderScale));
        int h = Mathf.Max(16, Mathf.RoundToInt(Screen.height * renderScale));
        if (rt.width == w && rt.height == h) return;

        RenderTexture old = rt;
        CreateRT();
        captureCam.targetTexture = rt;
        image.texture = rt;
        old.Release();
        Destroy(old);
    }

    void EndCapture()
    {
        if (!capturing) return;

        if (displayCamObj != null)
        {
            Destroy(displayCamObj);
            displayCamObj = null;
        }

        if (captureCam != null) captureCam.targetTexture = previousTarget;
        if (image != null) image.texture = null;
        if (rt != null)
        {
            rt.Release();
            Destroy(rt);
            rt = null;
        }
        capturing = false;
    }
}
