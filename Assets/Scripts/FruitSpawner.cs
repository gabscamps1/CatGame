using UnityEngine;

/// <summary>
/// Spawns the "next fruit" above the jar, lets the player aim it horizontally
/// with mouse/touch, and drops it on click/tap/space. Put this on an empty
/// GameObject positioned at the top of the play area.
/// </summary>
public class FruitSpawner : MonoBehaviour
{
    public Transform spawnPoint;
    public float minX = -2.5f;
    public float maxX = 2.5f;
    public float dropCooldown = 0.5f;

    private GameObject currentFruit;
    private Fruit currentFruitScript;
    private bool canDrop = true;
    private float cooldownTimer = 0f;

    void Start()
    {
        SpawnNewFruit();
    }

    void Update()
    {
        if (!canDrop)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0f) canDrop = true;
        }

        if (currentFruit == null) return;

        HandleAiming();

        bool triggerPressed = Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space);
        if (canDrop && triggerPressed)
        {
            DropFruit();
        }
    }

    void HandleAiming()
    {
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        float clampedX = Mathf.Clamp(mouseWorld.x, minX, maxX);
        currentFruit.transform.position = new Vector3(clampedX, spawnPoint.position.y, 0f);
    }

    void DropFruit()
    {
        currentFruitScript.MarkDropped();
        currentFruit.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;

        currentFruit = null;
        canDrop = false;
        cooldownTimer = dropCooldown;

        Invoke(nameof(SpawnNewFruit), dropCooldown);
    }

    void SpawnNewFruit()
    {
        int tier = GameManager.Instance.GetRandomSpawnTier();
        FruitData data = GameManager.Instance.GetFruitData(tier);

        currentFruit = Instantiate(data.prefab, spawnPoint.position, Quaternion.identity);
        currentFruitScript = currentFruit.GetComponent<Fruit>();
        currentFruitScript.tier = tier;

        // Kinematic while aiming so it floats and ignores gravity/collisions until dropped.
        Rigidbody2D rb = currentFruit.GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
    }
}
