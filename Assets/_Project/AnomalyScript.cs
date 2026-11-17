using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public enum AnomalyType
{
    ObjectChange,
    Disappearance,
    ExtraObject,
    Displacement
}

public class AnomalyScript : MonoBehaviour
{
    [System.Serializable]
    public class AnomalyVariant
    {
        public string variantName;
        public AnomalyType anomalyType;
        public GameObject anomalyObject;
    }

    [System.Serializable]
    public class ItemData
    {
        public string itemName;
        public GameObject normalState;
        public List<AnomalyVariant> anomalyVariants =
            new List<AnomalyVariant>();
    }

    // ==========================================
    // ITEM CONFIGURATION
    // ==========================================

    [Header("Item Configuration")]
    [SerializeField] private List<ItemData> items =
        new List<ItemData>();

    [Header("UI Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject gameplayHUD;
    [Header("Report UI")]
    [SerializeField] private GameObject reportButton; // Drag your main Report button here
    [SerializeField] private GameObject typeSelectionPanel;
    [SerializeField] private GameObject resultPanel;
    [Header("Quit Confirmation UI")]
    [SerializeField] private GameObject quitConfirmationPanel;

    [Header("UI Text")]
    [SerializeField] private TMP_Text resultTitle;
    [SerializeField] private TMP_Text resultDescription;
    [SerializeField] private TMP_Text gameTimeText;
    [SerializeField] private TMP_Text anomalyCountText;
    [SerializeField] private TMP_Text warningText;

    // ==========================================
    // SURVIVAL SETTINGS
    // ==========================================

    [Header("Survival Settings")]
    [SerializeField] private float secondsPerGameMinute = 1f;
    [SerializeField] private int gameDurationMinutes = 60;
    [SerializeField] private int anomalyIntervalMinutes = 10;

    [Header("Warning Settings")]
    [SerializeField] private int firstWarningThreshold = 3;
    [SerializeField] private int gameOverThreshold = 4;

    // ==========================================
    // ACTIVE ANOMALIES
    // ==========================================

    private class ActiveAnomaly
    {
        public int itemIndex;
        public AnomalyVariant variant;
    }

    private List<ActiveAnomaly> activeAnomalies =
        new List<ActiveAnomaly>();

    private string selectedItemName = "";

    // ==========================================
    // GAME STATE
    // ==========================================

    private float elapsedGameMinutes = 0f;
    private float nextAnomalyMinute = 0f;

    private bool isGameRunning = false;
    private bool firstWarningShown = false;

    // ==========================================
    // INITIALIZATION
    // ==========================================

    private void Start()
    {
        Debug.Log("[AnomalyScript] Start() initialized.");
        Time.timeScale = 0f;

        ResetAllToNormal();
        CloseAllPanels();

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);

        if (gameplayHUD != null)
            gameplayHUD.SetActive(false);

        if (resultPanel != null)
            resultPanel.SetActive(false);

        UpdateHUD();
    }

    private void Update()
    {
        if (!isGameRunning)
            return;

        // Update survival timer
        elapsedGameMinutes +=
            Time.deltaTime / Mathf.Max(0.01f, secondsPerGameMinute);

        UpdateHUD();

        // Victory condition
        if (elapsedGameMinutes >= gameDurationMinutes)
        {
            WinGame();
            return;
        }

        // Spawn anomalies at fixed game-time intervals
        if (elapsedGameMinutes >= nextAnomalyMinute)
        {
            SpawnRandomAnomaly();

            nextAnomalyMinute += anomalyIntervalMinutes;

            CheckWarningStatus();

            if (activeAnomalies.Count >= gameOverThreshold)
            {
                GameOver();
                return;
            }
        }

        // Testing shortcut: Space forces an anomaly
        if (Keyboard.current != null &&
            Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            SpawnRandomAnomaly();
            CheckWarningStatus();

            if (activeAnomalies.Count >= gameOverThreshold)
                GameOver();
        }
    }

    // ==========================================
    // MAIN MENU
    // ==========================================

    public void StartGame()
    {
        Debug.Log("[AnomalyScript] StartGame() button clicked.");

        ResetAllToNormal();

        elapsedGameMinutes = 0f;
        nextAnomalyMinute = anomalyIntervalMinutes;

        firstWarningShown = false;
        isGameRunning = true;

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        if (resultPanel != null)
            resultPanel.SetActive(false);

        if (gameplayHUD != null)
        {
            gameplayHUD.SetActive(true);
        }
        else
        {
            Debug.LogError("[AnomalyScript] gameplayHUD reference is missing in the Inspector!");
        }

        CloseAllPanels();

        if (warningText != null)
            warningText.text = "";

        Time.timeScale = 1f;
        UpdateHUD();
    }

    public void ReturnToMainMenu()
    {
        Debug.Log("[AnomalyScript] ReturnToMainMenu() called.");
        isGameRunning = false;
        Time.timeScale = 0f;

        ResetAllToNormal();
        CloseAllPanels();
        if (reportButton != null)
            reportButton.SetActive(true);

        if (gameplayHUD != null) gameplayHUD.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }

    public void QuitGame()
    {
        Debug.Log("[AnomalyScript] QuitGame() called.");
        Application.Quit();
    }

    // ==========================================
    // SURVIVAL TIMER & HUD
    // ==========================================

    private void UpdateHUD()
    {
        if (gameTimeText != null)
        {
            int totalMinutes =
                Mathf.FloorToInt(elapsedGameMinutes);

            DateTime gameTime =
                DateTime.Today.AddMinutes(totalMinutes);

            gameTimeText.text =
                gameTime.ToString("hh:mm tt");
        }

        if (anomalyCountText != null)
        {
            anomalyCountText.text =
                "Active Anomalies: " + activeAnomalies.Count;
        }
    }

    // Called when the user clicks the initial "Quit" button
    public void PromptQuitGame()
    {
        if (quitConfirmationPanel != null)
        {
            quitConfirmationPanel.SetActive(true);
        }
    }

    // Called when the user clicks "No" on the confirmation prompt
    public void CancelQuit()
    {
        if (quitConfirmationPanel != null)
        {
            quitConfirmationPanel.SetActive(false);
        }
    }

    // Called when the user clicks "Yes" on the confirmation prompt
    public void ConfirmQuitGame()
    {
        Debug.Log("[AnomalyScript] Quitting game...");
        Application.Quit();
        
        // Note: Application.Quit() does not close the editor during Play mode, 
        // so we can also stop play mode if testing in the Unity Editor:
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

    // ==========================================
    // WARNING SYSTEM
    // ==========================================

    private void CheckWarningStatus()
    {
        int count = activeAnomalies.Count;

        if (count >= firstWarningThreshold &&
            !firstWarningShown)
        {
            firstWarningShown = true;

            if (warningText != null)
            {
                warningText.text =
                    "Too many anomalies detected!\n Please report them immediately!";
            }

            Debug.LogWarning(
                "WARNING 1: Three active anomalies!");
        }
    }

    // ==========================================
    // RESULT PANELS
    // ==========================================

    private void ShowResult(string title, string description)
    {
        Debug.Log("[AnomalyScript] ShowResult: " + title);
        isGameRunning = false;
        Time.timeScale = 0f;

        CloseAllPanels();

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        if (resultPanel != null)
            resultPanel.SetActive(true);

        if (resultTitle != null)
            resultTitle.text = title;

        if (resultDescription != null)
            resultDescription.text = description;
    }

    private void WinGame()
    {
        ShowResult(
            "YOU SURVIVED",
            "Thank you for testing!"
        );
    }

    private void GameOver()
    {
        ShowResult(
            "GAME OVER",
            "WARNING 2: Four anomalies are active. " +
            "You failed to contain the situation."
        );
    }

    // ==========================================
    // UI NAVIGATION / REPORTING
    // ==========================================

    public void OpenReportMenu()
    {
        if (!isGameRunning)
        {
            Debug.LogWarning("[AnomalyScript] Cannot open report menu - game is not running!");
            return;
        }

        CloseAllPanels();
        selectedItemName = "";

        if (typeSelectionPanel != null)
            typeSelectionPanel.SetActive(true);
        else
            Debug.LogError("[AnomalyScript] typeSelectionPanel reference is missing in the Inspector!");

        // Hide the main report button while reporting
        if (reportButton != null)
            reportButton.SetActive(false);
    }

    public void CloseAllPanels()
    {
        if (typeSelectionPanel != null)
            typeSelectionPanel.SetActive(false);

        selectedItemName = "";

        if (reportButton != null)
        reportButton.SetActive(true);
    }

    public void SelectItemToReport(string itemName)
    {
        selectedItemName = itemName;
        Debug.Log("[AnomalyScript] Item selected for reporting: '" + selectedItemName + "'");

        if (typeSelectionPanel != null)
            typeSelectionPanel.SetActive(true);
    }

    public void SubmitAnomalyType(string anomalyTypeString)
    {
        if (!Enum.TryParse(anomalyTypeString, true, out AnomalyType reportedType))
        {
            Debug.LogError("[AnomalyScript] Invalid AnomalyType string: " + anomalyTypeString);
            CloseAllPanels();
            return;
        }

        Debug.Log("[AnomalyScript] Submitting report for anomaly type: " + reportedType);
        EvaluateAndFixByType(reportedType);
        CloseAllPanels();
    }

    // ==========================================
    // REPORT VERIFICATION & FIXING
    // ==========================================

    private void EvaluateAndFixByType(AnomalyType reportedType)
    {
        ActiveAnomaly targetAnomaly = null;
        int targetIndex = -1;

        // Find the first active anomaly that matches the reported type
        foreach (ActiveAnomaly anomaly in activeAnomalies)
        {
            if (anomaly != null && anomaly.variant != null && anomaly.variant.anomalyType == reportedType)
            {
                targetAnomaly = anomaly;
                targetIndex = anomaly.itemIndex;
                break; // Fixes the first found matching anomaly type
            }
        }

        if (targetAnomaly == null)
        {
            Debug.LogWarning("[AnomalyScript] Wrong report: No active anomaly of type " + reportedType + " found!");
            return;
        }

        Debug.Log("[AnomalyScript] Correct! Fixed anomaly on item index: " + targetIndex);

        // Remove from active tracking list
        activeAnomalies.Remove(targetAnomaly);

        // Reset the item back to normal state
        ResetItemToNormal(targetIndex);

        // Clear warning text if back under threshold
        if (activeAnomalies.Count < firstWarningThreshold && warningText != null)
        {
            warningText.text = "";
        }

        UpdateHUD();
    }

    // ==========================================
    // ANOMALY LIFECYCLE
    // ==========================================

    public void ResetAllToNormal()
    {
        activeAnomalies.Clear();

        if (items == null) return;

        for (int i = 0; i < items.Count; i++)
        {
            ResetItemToNormal(i);
        }
    }

    private void ResetItemToNormal(int index)
    {
        if (items == null || index < 0 || index >= items.Count) return;

        ItemData item = items[index];
        if (item == null) return;

        if (item.normalState != null)
            item.normalState.SetActive(true);

        if (item.anomalyVariants != null)
        {
            foreach (AnomalyVariant variant in item.anomalyVariants)
            {
                if (variant != null && variant.anomalyObject != null)
                {
                    variant.anomalyObject.SetActive(false);
                }
            }
        }
    }

    // ==========================================
    // RANDOM ANOMALY GENERATION
    // ==========================================

    [ContextMenu("Force Random Anomaly Now")]
    public void TriggerRandomAnomaly()
    {
        SpawnRandomAnomaly();
    }

    private void SpawnRandomAnomaly()
    {
        if (items == null || items.Count == 0) return;

        List<int> validIndices = new List<int>();

        for (int i = 0; i < items.Count; i++)
        {
            ItemData item = items[i];
            if (item == null) continue;

            if (item.anomalyVariants == null ||
                item.anomalyVariants.Count == 0)
                continue;

            bool alreadyActive = activeAnomalies.Exists(
                a => a != null && a.itemIndex == i);

            if (!alreadyActive)
                validIndices.Add(i);
        }

        if (validIndices.Count == 0)
        {
            Debug.LogWarning(
                "No available items for another anomaly.");
            return;
        }

        int chosenIndex = validIndices[
            UnityEngine.Random.Range(0, validIndices.Count)];

        ItemData chosenItem = items[chosenIndex];
        if (chosenItem == null || chosenItem.anomalyVariants == null || chosenItem.anomalyVariants.Count == 0)
            return;

        AnomalyVariant chosenVariant =
            chosenItem.anomalyVariants[
                UnityEngine.Random.Range(
                    0, chosenItem.anomalyVariants.Count)];

        if (chosenVariant == null) return;

        // Apply anomaly state safely
        if (chosenVariant.anomalyType != AnomalyType.ExtraObject)
        {
            if (chosenItem.normalState != null)
                chosenItem.normalState.SetActive(false);
        }

        if (chosenVariant.anomalyObject != null)
        {
            chosenVariant.anomalyObject.SetActive(true);
        }

        ActiveAnomaly newAnomaly = new ActiveAnomaly
        {
            itemIndex = chosenIndex,
            variant = chosenVariant
        };

        activeAnomalies.Add(newAnomaly);

        Debug.Log(
            "Anomaly Triggered: " +
            chosenItem.itemName + " - " +
            chosenVariant.anomalyType);

        UpdateHUD();
    }
}