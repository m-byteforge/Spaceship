using UnityEngine;
using System.Linq;

public class PlayerController : MonoBehaviour
{
    public float speed = 10f;
    [SerializeField] private float xBound = 10f, zMinBound = -3f, zMaxBound = 2f;

    public GameObject fireEffectPrefab;
    public ParticleSystem rocketFlame;
    public ParticleSystem miniRocketFlamePrefab;
    private GameManager gameManager;
    private GameObject[] miniRockets;
    private Vector3[] miniRocketStartOffsets;
    private Transform[] connectors;
    private Transform connectorRotator;
    private bool[] isMiniRocketFiring;
    private float[] rocketCooldowns;
    private ParticleSystem[] miniRocketFlames;
    private float rocketCooldownDuration = 0.5f, shakeDuration = 0.5f, shakeMagnitude = 0.2f;
    private GameObject lastTargetedBoulder;
    private int rocketCount, currentMiniRocketIndex;
    private float baseFlameSize, rotationSpeed = 60f, quickRotationSpeed = 360f, quickRotationDuration = 0.5f, quickRotationTimer;
    private bool isQuickRotating;
    private float[] bankingAngles = { -15f, -10f, -5f, 5f, 10f, 15f };

    public Transform GetConnector(int i) => connectors[i];
    public Vector3 GetStartOffset(int i) => miniRocketStartOffsets[i];

    void Start()
    {
        transform.position = new Vector3(0, 0.72f, -5);
        gameManager = FindFirstObjectByType<GameManager>();
        if (!gameManager) Debug.LogError("GameManager not found!");

        if (rocketFlame)
        {
            var main = rocketFlame.main;
            baseFlameSize = main.startSize.constant;
            UpdateFlameSize();
            rocketFlame.Play();
        }
        else Debug.LogWarning("RocketFlame particle system not assigned!");

        InitializeMiniRockets();
    }

    public void InitializeMiniRockets()
    {
        rocketCount = gameManager.CurrentLevel == 1 ? 3 : 2;
        miniRockets = new GameObject[rocketCount];
        miniRocketStartOffsets = new Vector3[rocketCount];
        connectors = new Transform[rocketCount];
        isMiniRocketFiring = new bool[rocketCount];
        rocketCooldowns = new float[rocketCount];
        miniRocketFlames = new ParticleSystem[rocketCount];

        connectorRotator = transform.Find("ConnectorRotator") ?? transform.Find("connectorrotator") ?? transform.Find("Connector Rotator");
        if (!connectorRotator)
        {
            connectorRotator = new GameObject("ConnectorRotator") { transform = { parent = transform, localPosition = Vector3.zero } }.transform;
            Debug.LogError("ConnectorRotator not found! Created placeholder.");
        }

        for (int i = 0; i < rocketCount; i++)
        {
            string cName = gameManager.CurrentLevel == 1 ? "connecter" + (i + 1) :
                          gameManager.CurrentLevel == 2 ? (i == 0 ? "leftconnecter" : "rightconnecter") :
                          (i == 0 ? "atomconnecter" : "massivconnecter");
            string rName = gameManager.CurrentLevel == 1 ? "minirocket" + (i + 1) :
                          gameManager.CurrentLevel == 2 ? (i == 0 ? "leftminirocket" : "rightminirocket") :
                          (i == 0 ? "atomrocket" : "massivrocket");

            connectors[i] = connectorRotator.Find(cName) ?? connectorRotator.Find(cName.ToLower());
            if (!connectors[i]) { Debug.LogWarning($"Connector '{cName}' not found, skipping rocket {i + 1}."); continue; }

            Transform rT = connectors[i].Find(rName) ?? connectors[i].Find(rName.ToLower());
            if (!rT) { Debug.LogError($"Rocket '{rName}' missing under '{cName}'."); continue; }

            miniRockets[i] = rT.gameObject;
            miniRocketStartOffsets[i] = rT.localPosition;
            miniRockets[i].SetActive(true);
        }
    }

    public void ShakePlayer()
    {
        SoundManager.Instance.PlaySound("Shake");
        StartCoroutine(Shake(transform.position));
    }

    private System.Collections.IEnumerator Shake(Vector3 originalPos)
    {
        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            transform.position = originalPos + new Vector3(
                Random.Range(-1f, 1f) * shakeMagnitude,
                Random.Range(-1f, 1f) * shakeMagnitude,
                0);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = originalPos;
        SoundManager.Instance.StopSound("Shake");
    }

    void Update()
    {
        if (!gameManager || gameManager.isGameOver) return;

        float h = Input.GetAxisRaw("Horizontal"), v = Input.GetAxisRaw("Vertical");
        transform.Translate(new Vector3(h, 0f, v).normalized * speed * Time.deltaTime, Space.World);

        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, -xBound, xBound);
        pos.z = Mathf.Clamp(pos.z, zMinBound, zMaxBound);
        pos.y = 0.72f;
        transform.position = pos;

        for (int i = 0; i < rocketCount; i++)
        {
            if (miniRockets[i])
            {
                if (!isMiniRocketFiring[i]) miniRockets[i].transform.localPosition = miniRocketStartOffsets[i];
                if (rocketCooldowns[i] > 0) rocketCooldowns[i] -= Time.deltaTime;
            }
        }

        if (connectorRotator && gameManager.CurrentLevel < 3)
            connectorRotator.Rotate(Vector3.forward, (isQuickRotating ? quickRotationSpeed : rotationSpeed) * Time.deltaTime, Space.Self);

        if (gameManager.CurrentLevel == 3 && h != 0f)
        {
            transform.localRotation = Quaternion.identity;
            float bankAngle = h < 0 ? bankingAngles.Where(a => a < 0).OrderBy(a => Random.value).First() :
                              bankingAngles.Where(a => a > 0).OrderBy(a => Random.value).First();
            transform.Rotate(Vector3.up, bankAngle, Space.Self);
        }

        if (isQuickRotating && (quickRotationTimer -= Time.deltaTime) <= 0f) isQuickRotating = false;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            FireMiniRocket(false);
            SoundManager.Instance.PlaySound("FireSingle");
            if (gameManager.CurrentLevel <= 2) { isQuickRotating = true; quickRotationTimer = quickRotationDuration; }
        }
        if (Input.GetKeyDown(KeyCode.T))
        {
            FireMiniRocket(true);
            SoundManager.Instance.PlaySound("FireAll");
            if (gameManager.CurrentLevel <= 2) { isQuickRotating = true; quickRotationTimer = quickRotationDuration; }
        }
        if (Input.GetKeyDown(KeyCode.C))
            foreach (var c in FindObjectsByType<DetectCollisions>(FindObjectsSortMode.None))
                if (c.gameObject.CompareTag("Energy")) c.Collect();

        UpdateFlameSize();
    }

    void FireMiniRocket(bool fireAll)
    {
        var boulders = GameObject.FindGameObjectsWithTag("Boulder").Where(b => b.transform.position.z > -5)
                        .OrderBy(b => b.transform.position.z).ToArray();
        bool anyFired = false;
        int rocketsToFire = fireAll ? rocketCount : 1;

        for (int i = 0; i < rocketsToFire; i++)
        {
            int idx = fireAll ? i : (currentMiniRocketIndex + i) % rocketCount;
            if (miniRockets[idx] == null || isMiniRocketFiring[idx] || rocketCooldowns[idx] > 0) continue;

            var miniRocketScript = miniRockets[idx].GetComponent<MiniRocket>();
            if (!miniRocketScript) continue;

            Vector3 rocketPos = miniRockets[idx].transform.position;
            Vector3 fireDirection = (fireAll && i < boulders.Length) ? (boulders[i].transform.position - rocketPos).normalized : Vector3.forward;
            GameObject target = null;

            if ((fireAll && i < boulders.Length) || (!fireAll && boulders.Length > 0))
            {
                target = fireAll ? boulders[i] : boulders.FirstOrDefault();
                fireDirection = (target.transform.position - rocketPos).normalized;
                fireDirection.y = 0;
                float xDiff = target.transform.position.x - rocketPos.x;
                float angle = xDiff < 0 ? Random.Range(-3f, -1f) : Random.Range(1f, 3f);
                fireDirection = Quaternion.Euler(0, angle, 0) * fireDirection;
                lastTargetedBoulder = target;

                if (gameManager.CurrentLevel == 3)
                {
                    transform.localRotation = Quaternion.identity;
                    transform.Rotate(Vector3.up, bankingAngles[Random.Range(0, bankingAngles.Length)], Space.Self);
                }
            }
            else lastTargetedBoulder = null;

            if (miniRocketFlamePrefab)
            {
                miniRocketFlames[idx] = Instantiate(miniRocketFlamePrefab, miniRockets[idx].transform.position, Quaternion.identity, miniRockets[idx].transform);
                miniRocketFlames[idx].Play();
            }

            miniRocketScript.StartFiring(fireDirection, target);
            isMiniRocketFiring[idx] = true;
            rocketCooldowns[idx] = rocketCooldownDuration;
            anyFired = true;
            if (!fireAll) currentMiniRocketIndex = (idx + 1) % rocketCount;
        }

        if (!anyFired) Debug.Log("No rockets available to fire!");
    }

    public void MiniRocketFinished(int index, bool wasHit)
    {
        if (index < 0 || index >= rocketCount || !miniRockets[index]) return;

        if (miniRocketFlames[index]) Destroy(miniRocketFlames[index].gameObject);
        isMiniRocketFiring[index] = false;
        rocketCooldowns[index] = rocketCooldownDuration;
        miniRockets[index].transform.SetParent(connectors[index]);
        miniRockets[index].transform.localPosition = miniRocketStartOffsets[index];
        if (!wasHit && lastTargetedBoulder) Debug.Log($"Rocket {index} missed boulder");
    }

    public void HitByBoulder(bool isLargeBoulder) => gameManager?.AddEnergy(-40);

    public void TriggerFireEffect()
    {
        if (!fireEffectPrefab) return;
        var effect = Instantiate(fireEffectPrefab, transform.position + Vector3.down * 0.5f, Quaternion.identity);
        effect.transform.SetParent(transform);
        Destroy(effect, 1f);
    }

    public void OnEnergyChanged(float newEnergy)
    {
        if (newEnergy > gameManager.Energy)
        {
            StartCoroutine(FlameRefreshEffect());
            SoundManager.Instance.PlaySound("Flame");
        }
        UpdateFlameSize();
    }

    private void UpdateFlameSize()
    {
        if (!rocketFlame) return;
        float energyPercentage = gameManager.Energy / 100f;
        var main = rocketFlame.main;
        main.startSize = energyPercentage < 0.3f ? baseFlameSize * 0.5f : baseFlameSize * Mathf.Lerp(0.8f, 1f, energyPercentage);
    }

    private System.Collections.IEnumerator FlameRefreshEffect()
    {
        if (!rocketFlame) yield break;

        var main = rocketFlame.main;
        float refreshSize = baseFlameSize * 1.5f, duration = 0.5f, elapsed = 0f;
        while (elapsed < duration)
        {
            main.startSize = Mathf.Lerp(refreshSize, baseFlameSize, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        main.startSize = baseFlameSize;
        UpdateFlameSize();
        SoundManager.Instance.StopSound("Flame");
    }

    private void OnDrawGizmos()
    {
        if (lastTargetedBoulder && miniRockets?.Any(r => r) == true)
        {
            Gizmos.color = Color.red;
            foreach (var rocket in miniRockets) if (rocket) Gizmos.DrawLine(rocket.transform.position, lastTargetedBoulder.transform.position);
        }
    }
}