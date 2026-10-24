using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Opcional. Coloque no mesmo objeto do RawImage que usa o material UI/WaveStripes.
/// - Atualiza _Aspect automaticamente conforme o tamanho do RectTransform.
/// - Alimenta _CustomTime com tempo não escalado (funciona com Time.timeScale = 0, ex.: menu de pausa).
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(RawImage))]
public class UIWaveStripesDriver : MonoBehaviour
{
    static readonly int AspectId = Shader.PropertyToID("_Aspect");
    static readonly int CustomTimeId = Shader.PropertyToID("_CustomTime");
    static readonly int UseCustomTimeId = Shader.PropertyToID("_UseCustomTime");

    [SerializeField] bool autoAspect = true;
    [SerializeField] bool useUnscaledTime = true;

    RawImage image;
    RectTransform rect;
    Material instance;

    void OnEnable()
    {
        image = GetComponent<RawImage>();
        rect = (RectTransform)transform;

        if (Application.isPlaying && image.material != null)
        {
            // Instância própria para não alterar o asset do material
            instance = new Material(image.material);
            image.material = instance;
        }
    }

    void OnDisable()
    {
        if (instance != null)
        {
            Destroy(instance);
            instance = null;
        }
    }

    void Update()
    {
        Material m = instance != null ? instance : image.materialForRendering;
        if (m == null) return;

        if (autoAspect && rect.rect.height > 0f)
            m.SetFloat(AspectId, rect.rect.width / rect.rect.height);

        m.SetFloat(UseCustomTimeId, useUnscaledTime ? 1f : 0f);
        if (useUnscaledTime)
            m.SetFloat(CustomTimeId, Time.realtimeSinceStartup);
    }
}
