using UnityEngine;

public class DetectCollisions : MonoBehaviour
{
    private GameManager gameManager;
    private bool hasCollided = false;
    private PlayerController playerController;
    private static int level3BoulderCollisions = 0;

    void Start()
    {
        gameManager = FindFirstObjectByType<GameManager>();
        playerController = FindFirstObjectByType<PlayerController>();

        if (gameManager == null)
            Debug.LogError("GameManager not found in the scene!");

        if (!gameObject.GetComponent<MoveObject>())
            gameObject.AddComponent<MoveObject>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasCollided) return;

        if (gameObject.CompareTag("Boulder") && other.CompareTag("Player"))
        {
            hasCollided = true;
            gameManager.BoulderCollision(gameObject);

            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                bool isLargeBoulder = transform.localScale.x > 0.3f;
                player.HitByBoulder(isLargeBoulder);
                player.ShakePlayer();
                SoundManager.Instance.PlaySound("Collision");

                if (gameManager.CurrentLevel == 3)
                {
                    float energyLoss = gameManager.Energy * 0.3f;
                    gameManager.AddEnergy(-energyLoss);
                    player.OnEnergyChanged(gameManager.Energy);

                    level3BoulderCollisions++;
                    Debug.Log("Level 3 boulder collision count: " + level3BoulderCollisions);

                    if (level3BoulderCollisions >= 2)
                    {
                        Debug.Log("Reached 2 boulder collisions in Level 3. Game Over incoming...");
                        Invoke(nameof(TriggerGameOver), 0.5f);
                    }
                }
                else
                {
                    float reduction = gameManager.CurrentLevel == 1 ? 0.1f : 0.2f;
                    float energyLoss = gameManager.Energy * reduction;
                    gameManager.AddEnergy(-energyLoss);
                    player.OnEnergyChanged(gameManager.Energy);
                }
            }

            Destroy(gameObject);
        }
        else if (gameObject.CompareTag("Energy") && other.CompareTag("Player"))
        {
            hasCollided = true;
            float previousEnergy = gameManager.Energy;
            gameManager.AddEnergy(20f);
            if (playerController != null)
            {
                playerController.OnEnergyChanged(previousEnergy);
            }
            Destroy(gameObject);
        }
    }

    private void TriggerGameOver()
    {
        Debug.Log("Triggering Game Over after 2nd boulder hit in Level 3.");
        gameManager.GameOver("Hit by boulders 2 times in Level 3!");
    }

    public void Collect()
    {
        if (gameObject.CompareTag("Energy") && !hasCollided)
        {
            hasCollided = true;
            float previousEnergy = gameManager.Energy;
            gameManager.AddEnergy(20f);
            if (playerController != null)
            {
                playerController.OnEnergyChanged(previousEnergy);
            }
            Destroy(gameObject);
        }
    }
}

/******************************************************done*/
