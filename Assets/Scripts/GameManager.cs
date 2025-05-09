using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public GameObject level1RocketPrefab, level2RocketPrefab, level3RocketPrefab;
    public Transform spaceport, level1StationPoint, level2StationPoint, level3StationPoint;
    public GameObject upgradeEffectPrefab;
    public Sprite background1, background2, background3;
    public Vector3 level1RocketScale = Vector3.one, level2RocketScale = Vector3.one, level3RocketScale = Vector3.one;
    public Vector3 level1GameplayScale = new Vector3(1f, 1f, 1f), level2GameplayScale = new Vector3(1f, 1f, 1f), level3GameplayScale = new Vector3(1.2f, 1.2f, 1.2f);
    public TextMeshProUGUI pointsText, energyText, levelText, missesText, gameOverText, levelUpText;
    public Slider energySlider;
    public GameObject gameOverPanel, levelUpPanel, settingsPanel, uiPanel;
    public Button restartButton, continueButton, cancelButton, gearButton, closeSettingsButton, backButton;
    public Toggle backgroundSoundToggle, explosionSoundToggle, interactionSoundToggle;
    public GameObject confirmAbortPanel;
    public Button cancelAbortButton, okAbortButton;
    public GameObject shipSelectionPanel;
    public Button level1ShipButton, level2ShipButton, level3ShipButton;

    private SpriteRenderer backgroundRenderer;
    private GameObject currentPlayer;
    private GameObject[] rocketInstances = new GameObject[3];
    private int points, currentLevel = 1, boulderCollisionCount, boulderMissCount;
    private int allowedMisses = 10;
    private float energy = 100f;
    private SpawnManager spawnManager;
    private HashSet<GameObject> collidedBoulders = new HashSet<GameObject>();
    public bool isGameOver, isPausedForLevelUp, isPausedForSettings;
    private CanvasGroup uiCanvasGroup;
    private GameObject previousPanel;

    private Vector3 spawnPoint = new Vector3(0, 0.72f, -2);
    private Quaternion spawnRotation = Quaternion.identity;

    public int Points => points;
    public float Energy => energy;
    public int CurrentLevel => currentLevel;

    void Start()
    {
        spawnManager = FindFirstObjectByType<SpawnManager>();
        backgroundRenderer = GameObject.Find("Background")?.GetComponent<SpriteRenderer>();
        uiCanvasGroup = uiPanel?.GetComponent<CanvasGroup>();
        if (uiCanvasGroup) uiCanvasGroup.alpha = 0.5f;

        // Validate critical components
        if (!spaceport && !(spaceport = GameObject.Find("Spaceport")?.transform) ||
            !level1StationPoint || !level2StationPoint || !level3StationPoint ||
            !level1RocketPrefab || !level2RocketPrefab || !level3RocketPrefab ||
            !backgroundRenderer || !pointsText || !energySlider || !energyText || !levelText ||
            !missesText || !gameOverPanel || !gameOverText || !restartButton || !uiPanel ||
            !levelUpPanel || !levelUpText || !continueButton || !cancelButton ||
            !settingsPanel || !gearButton || !closeSettingsButton || !backgroundSoundToggle ||
            !explosionSoundToggle || !interactionSoundToggle || !backButton || !confirmAbortPanel ||
            !cancelAbortButton || !okAbortButton || !shipSelectionPanel || !level1ShipButton ||
            !level2ShipButton || !level3ShipButton)
        {
            Debug.LogError("Critical components missing!");
            return;
        }

        backgroundRenderer.sprite = background1;

        // UI Setup
        SetupUI();

        // Initialize Rockets
        rocketInstances[0] = Instantiate(level1RocketPrefab, spawnPoint, spawnRotation);
        rocketInstances[0].transform.localScale = level1GameplayScale;
        rocketInstances[1] = Instantiate(level2RocketPrefab, level2StationPoint.position, Quaternion.identity, spaceport);
        rocketInstances[1].transform.localScale = level2RocketScale;
        AddStationPointAnimation(rocketInstances[1], 2);
        rocketInstances[2] = Instantiate(level3RocketPrefab, level3StationPoint.position, Quaternion.identity, spaceport);
        rocketInstances[2].transform.localScale = level3RocketScale;
        AddStationPointAnimation(rocketInstances[2], 3);

        foreach (var i in new[] { 1, 2 })
        {
            var controller = rocketInstances[i].GetComponent<PlayerController>();
            if (controller) controller.enabled = false;
            else Debug.LogError($"PlayerController missing on {rocketInstances[i].name}!");
        }

        currentPlayer = rocketInstances[0];
        allowedMisses = 10;
        SetGameplayFlame(rocketInstances[0], true);
        Debug.Log($"Game Started: Energy {energy}%, Points {points}, Level {currentLevel}, Allowed Misses {allowedMisses}");
    }

    void SetupUI()
    {
        // Game Over
        gameOverPanel.SetActive(false);
        restartButton.onClick.AddListener(RestartGame);
        SetupNavigation(new Selectable[] { restartButton }, restartButton);

        // Level Up
        levelUpPanel.SetActive(false);
        continueButton.onClick.AddListener(OnContinue);
        cancelButton.onClick.AddListener(OnCancel);
        SetupNavigation(new Selectable[] { continueButton, cancelButton }, continueButton);

        // Settings
        settingsPanel.SetActive(false);
        gearButton.onClick.AddListener(OpenSettings);
        closeSettingsButton.onClick.AddListener(CloseSettings);
        backgroundSoundToggle.isOn = SoundManager.Instance.IsBackgroundSoundEnabled;
        explosionSoundToggle.isOn = SoundManager.Instance.IsExplosionSoundEnabled;
        interactionSoundToggle.isOn = SoundManager.Instance.IsInteractionSoundEnabled;
        backgroundSoundToggle.onValueChanged.AddListener(SoundManager.Instance.ToggleBackgroundSound);
        explosionSoundToggle.onValueChanged.AddListener(SoundManager.Instance.ToggleExplosionSound);
        interactionSoundToggle.onValueChanged.AddListener(SoundManager.Instance.ToggleInteractionSound);
        SetupNavigation(new Selectable[] { backgroundSoundToggle, explosionSoundToggle, interactionSoundToggle, closeSettingsButton }, backgroundSoundToggle);

        // Confirm Abort
        confirmAbortPanel.SetActive(false);
        backButton.onClick.AddListener(ShowConfirmAbort);
        cancelAbortButton.onClick.AddListener(HideConfirmAbort);
        okAbortButton.onClick.AddListener(ReturnToStartMenu);
        SetupNavigation(new Selectable[] { okAbortButton, cancelAbortButton }, okAbortButton);

        // Ship Selection
        shipSelectionPanel.SetActive(false);
        level1ShipButton.onClick.AddListener(() => ReplayWithShip(0));
        level2ShipButton.onClick.AddListener(() => ReplayWithShip(1));
        level3ShipButton.onClick.AddListener(() => ReplayWithShip(2));
        SetupNavigation(new Selectable[] { level1ShipButton, level2ShipButton, level3ShipButton }, level1ShipButton);

        UpdateUI();
    }

    void SetupNavigation(Selectable[] elements, Selectable firstSelected)
    {
        for (int i = 0; i < elements.Length; i++)
        {
            Navigation nav = elements[i].navigation;
            nav.mode = Navigation.Mode.Explicit;
            nav.selectOnUp = elements[i == 0 ? elements.Length - 1 : i - 1];
            nav.selectOnDown = elements[(i + 1) % elements.Length];
            elements[i].navigation = nav;
        }
        UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(firstSelected.gameObject);
    }

    void Update()
    {
        if (isGameOver || isPausedForLevelUp || isPausedForSettings)
        {
            var currentSelected = UnityEngine.EventSystems.EventSystem.current?.currentSelectedGameObject;
            if (Input.GetKeyDown(KeyCode.Return) && currentSelected)
            {
                if (currentSelected.GetComponent<Button>() is Button btn && btn.interactable)
                {
                    btn.onClick.Invoke();
                    Debug.Log($"Enter pressed: {btn.name} activated");
                }
            }
            if (settingsPanel.activeInHierarchy && Input.GetKeyDown(KeyCode.Space) && currentSelected)
            {
                if (currentSelected.GetComponent<Toggle>() is Toggle tog && tog.interactable)
                {
                    tog.isOn = !tog.isOn;
                    Debug.Log($"Space pressed: {tog.name} toggled to {tog.isOn}");
                }
            }
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape)) ShowConfirmAbort();
        UpdateUI();
    }

    void UpdateUI()
    {
        pointsText.text = $"Points: {points}";
        energySlider.value = energy;
        energyText.text = $"{Mathf.RoundToInt(energy)}%";
        levelText.text = $"Level: {currentLevel}";
        missesText.text = $"Misses Left: {allowedMisses - boulderMissCount}";
    }

    void CheckForLevelUpgrade()
    {
        int maxPoints = currentLevel == 1 ? 165 : currentLevel == 2 ? 265 : 365;
        if (points < maxPoints) return;

        Debug.Log($"Level upgrade triggered! Points: {points}/{maxPoints}, Current Level: {currentLevel}");
        points = maxPoints;
        if (currentLevel == 3) EndGame();
        else if (currentLevel == 2) UpgradeToLevel3();
        else UpgradeToLevel2();
    }

    void UpgradeToLevel2() => UpgradeRocket(0, 2, 6);
    void UpgradeToLevel3() => UpgradeRocket(1, 3, 4);

    void UpgradeRocket(int rocketIndex, int newLevel, int newAllowedMisses)
    {
        StationRocket(rocketIndex);
        currentLevel = newLevel;
        boulderMissCount = boulderCollisionCount = 0;
        allowedMisses = newAllowedMisses;
        energy = 100f;
        SoundManager.Instance.UpdateBackgroundSound(currentLevel);
        ShowLevelUpMessage();
        Debug.Log($"Upgraded to Level {currentLevel}! Points: {points}, Energy: {energy}%, Allowed Misses: {allowedMisses}");
    }

    void ShowLevelUpMessage()
    {
        isPausedForLevelUp = true;
        Time.timeScale = 0f;
        levelUpText.text = currentLevel == 2 ? "Level 2! 100 points to Level 3" : "Level 3! 365 points to complete the game";
        levelUpPanel.SetActive(true);
        continueButton.gameObject.SetActive(true);
        cancelButton.gameObject.SetActive(true);
        if (uiCanvasGroup) uiCanvasGroup.alpha = 1f;
        Debug.Log($"Level-up message shown: {levelUpText.text}");
    }

    void OnContinue()
    {
        isPausedForLevelUp = false;
        Time.timeScale = 1f;
        levelUpPanel.SetActive(false);
        ActivateRocket(currentLevel - 1);
        spawnManager.ResetAndIncreaseSpawns(currentLevel);
        UpdateBackgroundSprites();
        if (upgradeEffectPrefab) Destroy(Instantiate(upgradeEffectPrefab, spawnPoint, Quaternion.identity), 2f);
        if (uiCanvasGroup) uiCanvasGroup.alpha = 0.5f;
        Debug.Log($"Player continued to Level {currentLevel}. Game state: isGameOver={isGameOver}, isPausedForLevelUp={isPausedForLevelUp}, isPausedForSettings={isPausedForSettings}");
    }

    void OnCancel()
    {
        isPausedForLevelUp = false;
        Time.timeScale = 1f;
        levelUpPanel.SetActive(false);
        if (uiCanvasGroup) uiCanvasGroup.alpha = 0.5f;
        GameOver($"Game Cancelled at Level {currentLevel}!");
        Debug.Log($"Player cancelled the game at Level {currentLevel}");
    }

    void EndGame()
    {
        StationRocket(2);
        GameOver("Final level!\nAll ships unlocked continue playing!");
        ShowShipSelection();
    }

    void ShowShipSelection()
    {
        spawnManager.StopSpawning();
        if (currentPlayer) currentPlayer.GetComponent<PlayerController>().enabled = false;
        SoundManager.Instance.StopAllSounds();
        SoundManager.Instance.PlaySound("GameOver");
        shipSelectionPanel.SetActive(true);
        if (uiCanvasGroup) uiCanvasGroup.alpha = 1f;
        Debug.Log("Showing ship selection panel");
    }

    void ReplayWithShip(int rocketIndex)
    {
        points = 0;
        energy = 50f;
        boulderMissCount = boulderCollisionCount = 0;
        collidedBoulders.Clear();
        isGameOver = isPausedForLevelUp = isPausedForSettings = false;
        Time.timeScale = 1f;

        currentLevel = rocketIndex + 1;
        allowedMisses = currentLevel == 1 ? 10 : currentLevel == 2 ? 6 : 4;

        for (int i = 0; i < rocketInstances.Length; i++)
            if (i != rocketIndex) StationRocket(i);

        ActivateRocket(rocketIndex);
        spawnManager.ResetAndIncreaseSpawns(currentLevel);
        UpdateBackgroundSprites();
        shipSelectionPanel.SetActive(false);
        if (uiCanvasGroup) uiCanvasGroup.alpha = 0.5f;
        UpdateUI();
        SoundManager.Instance.UpdateBackgroundSound(currentLevel);
        Debug.Log($"Replaying with rocket {rocketIndex + 1} (Level {currentLevel}). Energy: {energy}%, Points: {points}, Allowed Misses: {allowedMisses}");
    }

    void UpdateBackgroundSprites()
    {
        Sprite[] levelBackgrounds = currentLevel == 1 ? new[] { background1, background1, background1 } :
                                   currentLevel == 2 ? new[] { background2, background2, background2 } :
                                                       new[] { background3, background3, background3 };
        GameObject[] backgroundObjects = GameObject.FindGameObjectsWithTag("Background");

        if (backgroundObjects.Length == 0)
        {
            Debug.LogWarning("No background objects found with tag 'Background'!");
            return;
        }

        for (int i = 0; i < backgroundObjects.Length && i < levelBackgrounds.Length; i++)
        {
            var sr = backgroundObjects[i].GetComponent<SpriteRenderer>();
            if (sr && levelBackgrounds[i]) sr.sprite = levelBackgrounds[i];
            else Debug.LogWarning($"Failed to swap background layer {i}: SpriteRenderer or sprite missing!");
        }
    }

    void StationRocket(int rocketIndex)
    {
        if (!rocketInstances[rocketIndex]) return;

        Transform targetStationPoint = rocketIndex == 0 ? level1StationPoint : rocketIndex == 1 ? level2StationPoint : level3StationPoint;
        Vector3 targetScale = rocketIndex == 0 ? level1RocketScale : rocketIndex == 1 ? level2RocketScale : level3RocketScale;

        if (!targetStationPoint) { Debug.LogError($"Station point for rocket {rocketIndex} is null!"); return; }

        var controller = rocketInstances[rocketIndex].GetComponent<PlayerController>();
        if (controller) controller.enabled = false;

        var t = rocketInstances[rocketIndex].transform;
        if (spaceport) t.SetParent(spaceport);
        t.position = targetStationPoint.position;
        t.rotation = Quaternion.identity;
        t.localScale = targetScale;
        AddStationPointAnimation(rocketInstances[rocketIndex], rocketIndex + 1);
        SetStationFlame(rocketInstances[rocketIndex], true);
        Debug.Log($"Stationed rocket {rocketInstances[rocketIndex].name} at {targetStationPoint.position}");
    }

    void ActivateRocket(int rocketIndex)
    {
        if (!rocketInstances[rocketIndex]) { Debug.LogError($"Rocket instance at index {rocketIndex} is null!"); return; }

        var t = rocketInstances[rocketIndex].transform;
        t.SetParent(null);
        t.position = spawnPoint;
        t.rotation = spawnRotation;
        t.localScale = rocketIndex == 0 ? level1GameplayScale : rocketIndex == 1 ? level2GameplayScale : level3GameplayScale;

        var animator = rocketInstances[rocketIndex].GetComponent<StationPointAnimator>();
        if (animator) Destroy(animator);

        var connectorRotator = t.Find("ConnectorRotator") ?? t.Find("connectorrotator") ?? t.Find("Connector Rotator");
        if (connectorRotator) connectorRotator.localRotation = Quaternion.identity;

        currentPlayer = rocketInstances[rocketIndex];
        var controller = currentPlayer.GetComponent<PlayerController>();
        if (controller) { controller.enabled = true; StartCoroutine(InitializeRocketController(rocketIndex)); }
        else Debug.LogError($"PlayerController missing on {rocketInstances[rocketIndex].name}!");
        SetGameplayFlame(rocketInstances[rocketIndex], true);
        Debug.Log($"Activated rocket {rocketInstances[rocketIndex].name} at {spawnPoint}");
    }

    private System.Collections.IEnumerator InitializeRocketController(int rocketIndex)
    {
        yield return null;
        var controller = rocketInstances[rocketIndex].GetComponent<PlayerController>();
        if (controller) controller.InitializeMiniRockets();
        else Debug.LogError($"PlayerController missing on {rocketInstances[rocketIndex].name}!");
    }

    void AddStationPointAnimation(GameObject rocket, int level)
    {
        var animator = rocket.GetComponent<StationPointAnimator>() ?? rocket.AddComponent<StationPointAnimator>();
        animator.Initialize(level);
    }

    private void SetStationFlame(GameObject rocket, bool enable)
    {
        var flame = rocket.transform.Find("FlameEffect")?.GetComponent<ParticleSystem>();
        if (flame == null) { Debug.LogWarning($"FlameEffect missing on {rocket.name}!"); return; }

        if (enable)
        {
            var main = flame.main;
            main.startSize = 0.2f;
            main.startSpeed = 0.5f;
            flame.transform.localPosition = new Vector3(0, 0, -rocket.transform.localScale.z * 0.5f);
            if (!flame.isPlaying) flame.Play();
        }
        else flame.Stop();
    }

    private void SetGameplayFlame(GameObject rocket, bool enable)
    {
        var flame = rocket.transform.Find("FlameEffect")?.GetComponent<ParticleSystem>();
        if (flame == null) { Debug.LogWarning($"FlameEffect missing on {rocket.name}!"); return; }

        if (enable)
        {
            var main = flame.main;
            main.startSize = 1.5f;
            main.startSpeed = 3f;
            flame.transform.localPosition = new Vector3(0, 0, rocket.transform.localScale.z * 0.5f);
            if (!flame.isPlaying) flame.Play();
        }
        else flame.Stop();
    }

    public void AddPoints(int value)
    {
        if (isGameOver) return;
        points += value;

        int maxPoints = currentLevel == 1 ? 165 : currentLevel == 2 ? 265 : 365;
        if (points > maxPoints) points = maxPoints;

        UpdateUI();
        CheckForLevelUpgrade();
    }

    public void AddEnergy(float value)
    {
        if (isGameOver) return;
        energy = Mathf.Clamp(energy + value, 0f, 100f);
        if (energy <= 0f) GameOver("Energy depleted!");
        UpdateUI();
    }

    public void BoulderCollision(GameObject boulder)
    {
        if (collidedBoulders.Contains(boulder)) return;
        collidedBoulders.Add(boulder);
        boulderCollisionCount++;
        if (currentLevel == 3 && boulderCollisionCount >= 2 || boulderCollisionCount >= 5)
            GameOver(currentLevel == 3 ? "Hit by boulders 2 times in Level 3!" : "Hit by boulders 5 times!");
    }

    public void BoulderMissed()
    {
        if (isGameOver) return;
        boulderMissCount++;
        UpdateUI();
        if (boulderMissCount >= allowedMisses) GameOver("Too many boulders missed!");
    }

    public void GameOver(string reason = "Game Over!")
    {
        if (isGameOver) return;
        isGameOver = true;
        isPausedForLevelUp = false;
        spawnManager.StopSpawning();
        if (currentPlayer) currentPlayer.GetComponent<PlayerController>().enabled = false;
        SoundManager.Instance.StopAllSounds();
        SoundManager.Instance.PlaySound("GameOver");
        if (levelUpPanel.activeInHierarchy)
        {
            previousPanel = levelUpPanel;
            levelUpPanel.SetActive(false);
        }
        gameOverPanel.SetActive(true);
        gameOverText.text = $"{reason}\nFinal Points: {points}\nFinal Energy: {Mathf.RoundToInt(energy)}%";
        if (uiCanvasGroup) uiCanvasGroup.alpha = 1f;
        Debug.Log($"💀 {reason} Final Points: {points}, Energy: {energy}%");
    }

    public void RestartGame()
    {
        isGameOver = isPausedForLevelUp = isPausedForSettings = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        Debug.Log("Game restarted, all pause states reset.");
    }

    void OpenSettings()
    {
        isPausedForSettings = true;
        Time.timeScale = 0f;
        if (gameOverPanel.activeInHierarchy || levelUpPanel.activeInHierarchy || shipSelectionPanel.activeInHierarchy)
        {
            previousPanel = gameOverPanel.activeInHierarchy ? gameOverPanel : levelUpPanel.activeInHierarchy ? levelUpPanel : shipSelectionPanel;
            if (gameOverPanel.activeInHierarchy) gameOverPanel.SetActive(false);
            else if (levelUpPanel.activeInHierarchy) levelUpPanel.SetActive(false);
            else shipSelectionPanel.SetActive(false);
        }
        settingsPanel.SetActive(true);
        if (uiCanvasGroup) uiCanvasGroup.alpha = 0.9f;
        UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(backgroundSoundToggle.gameObject);
        Debug.Log("Settings opened, game paused.");
    }

    void CloseSettings()
    {
        isPausedForSettings = false;
        if (isPausedForLevelUp && !levelUpPanel.activeInHierarchy) isPausedForLevelUp = false;
        if (!isGameOver && !isPausedForLevelUp) Time.timeScale = 1f;
        settingsPanel.SetActive(false);
        if (previousPanel && !confirmAbortPanel.activeInHierarchy)
        {
            previousPanel.SetActive(true);
            if (uiCanvasGroup) uiCanvasGroup.alpha = previousPanel == gameOverPanel ? 1f : previousPanel == levelUpPanel ? 1f : 1f;
            previousPanel = null;
        }
        else if (uiCanvasGroup) uiCanvasGroup.alpha = 0.5f;
        Debug.Log("Settings closed, game resumed.");
    }

    void ShowConfirmAbort()
    {
        isPausedForSettings = true;
        Time.timeScale = 0f;
        if (gameOverPanel.activeInHierarchy || levelUpPanel.activeInHierarchy || settingsPanel.activeInHierarchy || shipSelectionPanel.activeInHierarchy)
        {
            previousPanel = gameOverPanel.activeInHierarchy ? gameOverPanel : levelUpPanel.activeInHierarchy ? levelUpPanel : settingsPanel.activeInHierarchy ? settingsPanel : shipSelectionPanel;
            if (gameOverPanel.activeInHierarchy) gameOverPanel.SetActive(false);
            else if (levelUpPanel.activeInHierarchy) levelUpPanel.SetActive(false);
            else if (settingsPanel.activeInHierarchy) settingsPanel.SetActive(false);
            else shipSelectionPanel.SetActive(false);
        }
        confirmAbortPanel.SetActive(true);
        if (uiCanvasGroup) uiCanvasGroup.alpha = 1f;
        UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(okAbortButton.gameObject);
        Debug.Log("Showing confirm abort panel");
    }

    void HideConfirmAbort()
    {
        confirmAbortPanel.SetActive(false);
        isPausedForSettings = false;
        Time.timeScale = 1f;
        if (previousPanel)
        {
            previousPanel.SetActive(true);
            if (uiCanvasGroup) uiCanvasGroup.alpha = previousPanel == gameOverPanel ? 1f : previousPanel == levelUpPanel ? 1f : previousPanel == settingsPanel ? 0.9f : 1f;
            previousPanel = null;
        }
        else if (uiCanvasGroup) uiCanvasGroup.alpha = 0.5f;
        Debug.Log("Hiding confirm abort panel");
    }

    void ReturnToStartMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("StartMenu");
        Debug.Log("Returning to Start Menu");
    }
}