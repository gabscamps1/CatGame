using UnityEngine;

public class AimLineController : MonoBehaviour
{
    [SerializeField] private Transform fruitThrowTransform;
    [SerializeField] private Transform bottomTransform;

    private LineRenderer lineRenderer;

    private float topPos;
    private float bottomPos;
    private float x;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();    
    }

    private void Update()
    {
        x = fruitThrowTransform.position.x;
        topPos = fruitThrowTransform.position.y;
        bottomPos = bottomTransform.position.y;

        lineRenderer.SetPosition(0, new Vector3(x, topPos));
        lineRenderer.SetPosition(1, new Vector3(x, bottomPos));

    }
}
