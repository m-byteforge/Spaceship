using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartMenuController : MonoBehaviour
{
    public Button startButton;
    public Button rulesButton;
    public Button closeRulesButton;
    public GameObject rulesPanel;
    public GameObject buttonGroup;

    void Start()
    {
        if (startButton == null || rulesButton == null || closeRulesButton == null || rulesPanel == null || buttonGroup == null)
        {
            Debug.LogError("One or more UI elements not assigned in StartMenuController!");
            return;
        }

        startButton.onClick.AddListener(StartGame);
        rulesButton.onClick.AddListener(ShowRules);
        closeRulesButton.onClick.AddListener(HideRules);

        rulesPanel.SetActive(false);

        // Setup navigation for start menu buttons
        SetupButtonNavigation(new Button[] { startButton, rulesButton }, startButton);
        
        // close rules button
        SetupButtonNavigation(new Button[] { closeRulesButton }, closeRulesButton);
    }

    void Update()
    {
        // Handle Enter key for button activation
        if (Input.GetKeyDown(KeyCode.Return))
        {
            Button selectedButton = UnityEngine.EventSystems.EventSystem.current?.currentSelectedGameObject?.GetComponent<Button>();
            if (selectedButton != null && selectedButton.interactable)
            {
                selectedButton.onClick.Invoke();
                Debug.Log($"Enter pressed: {selectedButton.name} activated");
            }
        }

        // Placeholder for Space key to handle toggles (for future use)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Toggle selectedToggle = UnityEngine.EventSystems.EventSystem.current?.currentSelectedGameObject?.GetComponent<Toggle>();
            if (selectedToggle != null && selectedToggle.interactable)
            {
                selectedToggle.isOn = !selectedToggle.isOn;
                Debug.Log($"Space pressed: {selectedToggle.name} toggled to {selectedToggle.isOn}");
            }
        }
    }

    void SetupButtonNavigation(Button[] buttons, Button firstSelected)
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            Navigation nav = buttons[i].navigation;
            nav.mode = Navigation.Mode.Explicit;
            nav.selectOnUp = buttons[i == 0 ? buttons.Length - 1 : i - 1];
            nav.selectOnDown = buttons[(i + 1) % buttons.Length];
            buttons[i].navigation = nav;
        }
        UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(firstSelected.gameObject);
    }

    void StartGame()
    {
        SceneManager.LoadScene("MainScene");
        Debug.Log("Starting game...");
    }

    void ShowRules()
    {
        buttonGroup.SetActive(false);
        rulesPanel.SetActive(true);
        Debug.Log("Showing rules panel");
        UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(closeRulesButton.gameObject); // Set focus to close button
    }

    void HideRules()
    {
        rulesPanel.SetActive(false);
        buttonGroup.SetActive(true);
        Debug.Log("Hiding rules panel");
        UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(startButton.gameObject); //  focus to start button
    }
}