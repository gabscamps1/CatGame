
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraAspectRatio : MonoBehaviour
{
    [Header("Configuração base da câmera")]
    [SerializeField] private float referenceSize = 5f;

    [Header("Proporção de referência")]
    [SerializeField] private float referenceWidth = 16f;
    [SerializeField] private float referenceHeight = 9f;

    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        UpdateCameraSize();
    }

    private void Update()
    {
        // Atualiza caso a resolução ou proporção da tela mude.
        UpdateCameraSize();
    }

    private void UpdateCameraSize()
    {
        if (cam == null || cam.orthographic == false)
            return;

        float currentAspect = (float)Screen.width / Screen.height;
        float referenceAspect = referenceWidth / referenceHeight;

        // Mantém a referência de altura e adapta o tamanho
        // para evitar cortes em telas mais estreitas.
        cam.orthographicSize = referenceSize *
            Mathf.Max(1f, referenceAspect / currentAspect);
    }
}
