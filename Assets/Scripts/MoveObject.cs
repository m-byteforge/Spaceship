using UnityEngine;

public class MoveObject : MonoBehaviour
{
    private Vector3 direction;
    private float speed = 1f;
    private float speedMultiplier = 1f;
    private GameManager gameManager;
    private float targetY = 0.5f;
    private float wanderStrength = 0.5f;
    private float wanderTimer = 0f;
    private float wanderInterval = 0.1f;
    private float xBound = 6f;
    private float lowerBound = -5f;
    private float upperBound = 10f;
    private bool hasWanderBehavior;

    void Start()
    {
        gameManager = FindFirstObjectByType<GameManager>();
        hasWanderBehavior = gameObject.CompareTag("Boulder") || gameObject.CompareTag("Energy");

        if (hasWanderBehavior)
        {
            // Downward-biased random direction
            direction = new Vector3(Random.Range(-0.5f, 0.5f), 0, -1f).normalized;
            speed = 1f;
        }
        else
        {
            // Straight-down for any other object
            direction = Vector3.back;
            speed = 1.5f;
        }
    }

    void Update()
    {
        // Optional wandering for boulders/energy
        if (hasWanderBehavior)
        {
            wanderTimer += Time.deltaTime;
            if (wanderTimer >= wanderInterval)
            {
                var wanderOffset = new Vector3(
                    Random.Range(-1f, 1f),
                    0,
                    Random.Range(-1f, 0.5f)
                ).normalized * wanderStrength;

                direction = (direction + wanderOffset).normalized;
                direction.y = 0;
                wanderTimer = 0f;
            }

            // Optional Y-adjustment based on Z
            float t = Mathf.InverseLerp(4f, -5f, transform.position.z); // Changed -2f to -5f
            float currentY = Mathf.Lerp(1.5f, targetY, t);
            var pos = transform.position;
            pos.y = currentY;
            transform.position = pos;
        }

        // Move the object
        transform.Translate(direction * speed * speedMultiplier * Time.deltaTime, Space.World);

        // Clamp horizontal bounds only (no Z clamp, so boulders can fall past lowerBound)
        var clamped = transform.position;
        clamped.x = Mathf.Clamp(clamped.x, -xBound, xBound);
        transform.position = clamped;

        // Destroy when off bottom/top and register misses for boulders
        float z = transform.position.z;
        if (z < lowerBound || z > upperBound)
        {
            if (gameObject.CompareTag("Boulder") && z < lowerBound && gameManager != null)
            {
                gameManager.BoulderMissed();
            }
            Destroy(gameObject);
        }
    }

    public void SetSpeedMultiplier(float multiplier)
    {
        speedMultiplier = multiplier;
    }
}
/* works*/
/*good*//******************************************************************/