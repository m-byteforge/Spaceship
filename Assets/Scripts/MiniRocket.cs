using UnityEngine;

public class MiniRocket : MonoBehaviour
{
    private Vector3 fireDirection;
    private float speed;
    private float maxDistance;
    private float distanceTraveled = 0f;
    private bool isFiring = false;
    private PlayerController playerController;
    private int miniRocketIndex;
    private bool wasHit = false;
    private Collider rocketCollider;
    private Rigidbody rocketRb;
    private GameManager gameManager;
    private GameObject targetBoulder;
    private bool hasTargeting;
    private string rocketType;
    public GameObject smallExplosionEffectPrefab; // Explosion for small boulders
    public GameObject mediumExplosionEffectPrefab; // Explosion for medium boulders
    public GameObject largeExplosionEffectPrefab; // Explosion for large boulders
    private ParticleSystem flameEffect;

    void Start()
    {
        playerController = GetComponentInParent<PlayerController>();
        if (playerController == null)
            Debug.LogError($"PlayerController not found in parent hierarchy for {gameObject.name}!");

        gameManager = FindFirstObjectByType<GameManager>();
        rocketCollider = GetComponent<Collider>();
        rocketRb = GetComponent<Rigidbody>();

        // Find the flame effect for the mini-rocket
        var flameObj = transform.Find("FlameEffect");
        flameEffect = flameObj != null ? flameObj.GetComponent<ParticleSystem>() : null;
        if (flameEffect == null)
            Debug.LogWarning($"FlameEffect not found or missing ParticleSystem on {gameObject.name}!");

        string lower = name.ToLower();
        if (lower.Contains("left")) miniRocketIndex = 0;
        else if (lower.Contains("right")) miniRocketIndex = 1;
        else if (lower.Contains("atomrocket")) miniRocketIndex = 0;
        else if (lower.Contains("massivrocket")) miniRocketIndex = 1;
        else if (lower.Contains("minirocket") && int.TryParse(lower.Replace("minirocket", ""), out int idx))
            miniRocketIndex = idx - 1;
        else
            miniRocketIndex = 0;

        if (lower.Contains("atomrocket"))
        {
            speed = 18; maxDistance = 12; hasTargeting = false; rocketType = "atom";
        }
        else if (lower.Contains("massivrocket"))
        {
            speed = 15; maxDistance = 10; hasTargeting = true; rocketType = "massiv";
        }
        else
        {
            speed = 25; maxDistance = 15;
            hasTargeting = lower.Contains("minirocket") && !lower.Contains("left") && !lower.Contains("right");
            rocketType = (lower.Contains("left") || lower.Contains("right")) ? "level2" : "level1";
        }

        // Only call ReturnToDock if playerController is found
        if (playerController != null)
            ReturnToDock();
        else
            Debug.LogWarning($"Skipping ReturnToDock for {gameObject.name} as playerController is null at Start.");

        Debug.Log($"MiniRocket initialized: idx={miniRocketIndex}, type={rocketType}, speed={speed}, maxDist={maxDistance}, targeting={hasTargeting}");
    }

    void Update()
    {
        if (!isFiring) return;

        if (hasTargeting && rocketType == "massiv" && targetBoulder != null)
        {
            Vector3 directionToTarget = (targetBoulder.transform.position - transform.position).normalized;
            directionToTarget.y = 0;
            fireDirection = Vector3.Lerp(fireDirection, directionToTarget, Time.deltaTime * 5f);
        }

        transform.Translate(fireDirection * speed * Time.deltaTime, Space.World);
        distanceTraveled += speed * Time.deltaTime;

        float z = transform.position.z;
        if (distanceTraveled >= maxDistance || z > 10f || z < -6f)
        {
            Debug.Log($"Rocket[{miniRocketIndex}] end of flight (z={z}, dist={distanceTraveled}). Returning to dock.");
            StopFiring();
        }
    }

    public void StartFiring(Vector3 direction, GameObject target = null)
    {
        transform.SetParent(null, worldPositionStays: true);
        fireDirection = direction.normalized;
        targetBoulder = target;
        isFiring = true;
        distanceTraveled = 0f;
        wasHit = false;
        rocketCollider.enabled = true;
        if (rocketRb != null) rocketRb.isKinematic = false;

        // Enable flame effect for mini-rocket
        if (flameEffect != null)
        {
            var main = flameEffect.main;
            main.startSize = 0.3f; // Adjust size for mini-rocket flame
            main.startSpeed = 2f; // Adjust speed for trail effect
            // Position flame at the back (negative Z offset)
            flameEffect.transform.localPosition = new Vector3(0, 0, -0.2f);
            flameEffect.Play();
        }

        Debug.Log($"Rocket[{miniRocketIndex}] firing dir={fireDirection}, target={target?.name}");
    }

    public void StopFiring()
    {
        if (!isFiring) return;
        isFiring = false;

        // Disable flame effect
        if (flameEffect != null)
            flameEffect.Stop();

        playerController?.MiniRocketFinished(miniRocketIndex, wasHit);
        ReturnToDock();
    }

    private void ReturnToDock()
    {
        if (rocketRb != null)
        {
            rocketRb.linearVelocity = Vector3.zero;
            rocketRb.angularVelocity = Vector3.zero;
            rocketRb.isKinematic = true;
        }

        if (rocketCollider != null)
            rocketCollider.enabled = false;

        if (playerController == null)
        {
            Debug.LogError($"Cannot return to dock: playerController is null for {gameObject.name}!");
            return;
        }

        var conn = playerController.GetConnector(miniRocketIndex);
        if (conn == null)
        {
            Debug.LogError($"Cannot return to dock: Connector for miniRocketIndex {miniRocketIndex} is null in {gameObject.name}!");
            return;
        }

        var offset = playerController.GetStartOffset(miniRocketIndex);
        transform.SetParent(conn, worldPositionStays: false);
        transform.localPosition = offset;
        transform.localRotation = Quaternion.identity;

        // Ensure flame is off when docked
        if (flameEffect != null)
            flameEffect.Stop();

        targetBoulder = null;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isFiring || gameManager == null) return;
        if (!other.CompareTag("Boulder")) return;

        Debug.Log($"Rocket[{miniRocketIndex}] hit {other.name} at {other.transform.position}");
        wasHit = true;
        rocketCollider.enabled = false;

        // Select explosion effect based on boulder size
        float scale = other.transform.localScale.x;
        GameObject explosionEffectToUse;
        if (scale < 0.2f)
        {
            explosionEffectToUse = smallExplosionEffectPrefab;
            Debug.Log($"Small boulder explosion at {transform.position}");
        }
        else if (scale <= 0.3f)
        {
            explosionEffectToUse = mediumExplosionEffectPrefab;
            Debug.Log($"Medium boulder explosion at {transform.position}");
        }
        else
        {
            explosionEffectToUse = largeExplosionEffectPrefab;
            Debug.Log($"Large boulder explosion at {transform.position}");
        }

        // Instantiate the selected explosion effect
        if (explosionEffectToUse != null)
        {
            var ex = Instantiate(explosionEffectToUse, transform.position, Quaternion.identity);
            Destroy(ex, 1f);
        }
        else
        {
            Debug.LogWarning($"Explosion effect prefab not assigned for boulder size (scale={scale}) on {gameObject.name}!");
        }
        SoundManager.Instance.PlaySound("Explosion");

        float pointsToAdd;
        if (scale < 0.2f)
            pointsToAdd = 2.5f;
        else if (scale <= 0.3f)
            pointsToAdd = 5f;
        else
            pointsToAdd = 10f;
        gameManager.AddPoints(Mathf.FloorToInt(pointsToAdd));
        Debug.Log($"+{pointsToAdd} pts (scale={scale}) → total {gameManager.Points}");

        Destroy(other.gameObject);
        StopFiring();
    }
}