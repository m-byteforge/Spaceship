using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public GameObject[] objectPrefabs;

    [Header("Spawn Position Settings")]
    [SerializeField] private float spawnRangeX = 10f;    // Horizontal range 
    [SerializeField] private float spawnPosY = 1.5f;   // Height at which objects spawn
    [SerializeField] private float spawnPosZ = 6f;     // Maximum Z 

    [Header("Spawn Rate Settings")]
    [SerializeField] private float baseSpawnInterval = 2.0f; // Seconds spawn waves
    [SerializeField] private int baseObjectsPerSpawn = 2;    // How many objects to spawn per wave
    [SerializeField] private int maxBouldersOnScreen = 5;
    private float spawnInterval;
    private int objectsPerSpawn;
    private float boulderSpeedMultiplier = 1f;
    private bool isSpawning = true;
    private GameManager gameManager;

    void Start()
    {
        gameManager = FindFirstObjectByType<GameManager>();
        if (!gameManager) Debug.LogError("GameManager not found!");

        gameManager = FindFirstObjectByType<GameManager>();
        spawnInterval = baseSpawnInterval;
        objectsPerSpawn = baseObjectsPerSpawn;
        InvokeRepeating(nameof(SpawnRandomObject), 1f, spawnInterval);
    }
    void Update()
    {
        if (gameManager != null && gameManager.Energy >= 100f)
        {
            foreach (GameObject energy in GameObject.FindGameObjectsWithTag("Energy"))
            {
                Destroy(energy);
            }
        }

        // Check for boulders that fall onto the screen (reach z <= -3)
        if (gameManager != null && !gameManager.isGameOver)

        {
            foreach (GameObject boulder in GameObject.FindGameObjectsWithTag("Boulder"))
            {
                if (boulder.transform.position.z <= -3f)
                {
                    gameManager.BoulderMissed();
                    Destroy(boulder);
                    Debug.Log($"Boulder missed at z={boulder.transform.position.z}");
                }
            }
        }
    }

    void SpawnRandomObject()
    {
        if (!isSpawning)
            return;

        int currentBoulderCount = GameObject.FindGameObjectsWithTag("Boulder").Length;
        if (currentBoulderCount >= maxBouldersOnScreen)
            return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Vector3 playerPos = player != null
                             ? player.transform.position
                             : new Vector3(0, 0, spawnPosZ);

        // Attempt to spawn 'objectsPerSpawn' items each wave
        int i = 0;
        while (i < objectsPerSpawn)
        {
            currentBoulderCount = GameObject.FindGameObjectsWithTag("Boulder").Length;
            if (currentBoulderCount >= maxBouldersOnScreen)
                break;

            float energySpawnChance = gameManager.CurrentLevel == 1
                                      ? 0.2f
                                      : (gameManager.CurrentLevel == 2 ? 0.1f : 0.05f);
            int objectIndex = (gameManager.Energy >= 100f || Random.value >= energySpawnChance)
                              ? 0  // Boulder
                              : 1; // Energy pickup
            float spawnZ = spawnPosZ;

            float spawnX = Random.Range(
                Mathf.Max(-spawnRangeX, playerPos.x - spawnRangeX),
                Mathf.Min(spawnRangeX, playerPos.x + spawnRangeX)
            );
            Vector3 spawnPos = new Vector3(spawnX, spawnPosY, spawnZ);

            GameObject obj = Instantiate(objectPrefabs[objectIndex], spawnPos, Quaternion.identity);

            if (obj.CompareTag("Boulder"))
            {
                // Randomize boulder size (small / medium / large)
                float rand = Random.value;
                float scale = rand < 0.33f
                              ? Random.Range(0.1f, 0.2f)
                              : (rand < 0.66f
                                 ? Random.Range(0.2f, 0.3f)
                                 : Random.Range(0.3f, 0.4f));
                obj.transform.localScale = new Vector3(scale, scale, scale);


                Collider collider = obj.GetComponent<Collider>();
                if (collider != null)
                {
                    if (collider is BoxCollider boxCollider)
                        boxCollider.size = new Vector3(1f / scale, 1f / scale, 1f / scale);
                    else if (collider is SphereCollider sphereCollider)
                        sphereCollider.radius = 0.5f / scale;
                }
                MoveObject moveObject = obj.GetComponent<MoveObject>();
                if (moveObject != null)
                    moveObject.SetSpeedMultiplier(boulderSpeedMultiplier);
            }
            else
            {
                // Energy pickups always spawn at half scale
                obj.transform.localScale = Vector3.one * 0.5f;
            }
            i++;
        }
    }
    public void IncreaseDifficulty(int level)
    {
        foreach (var lvl in new[] { 2, 3 })
        {
            if (level == lvl)
            {
                spawnInterval = lvl == 2 ? baseSpawnInterval * 0.8f : baseSpawnInterval * 0.6f;
                boulderSpeedMultiplier = lvl == 2 ? 1.3f : 1.6f;
                break;
            }
        }
    }

    public void ResetAndIncreaseSpawns(int level)
    {
        StopSpawning();

        // Destroy all current boulders & energy pickups
        foreach (GameObject boulder in GameObject.FindGameObjectsWithTag("Boulder"))
            Destroy(boulder);
        foreach (GameObject energy in GameObject.FindGameObjectsWithTag("Energy"))
            Destroy(energy);

        objectsPerSpawn = (level == 2)
                         ? 3
                         : (baseObjectsPerSpawn + (level - 1));
        isSpawning = true;
        IncreaseDifficulty(level);
        InvokeRepeating(nameof(SpawnRandomObject), 1f, spawnInterval);
    }
    public void StopSpawning()
    {
        isSpawning = false;
        CancelInvoke(nameof(SpawnRandomObject));
    }
}
/******************************************************don e*/
