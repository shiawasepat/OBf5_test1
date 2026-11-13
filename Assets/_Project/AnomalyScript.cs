
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
    [SerializeField] private GameObject typeSelectionPanel;
    [SerializeField] private GameObject resultPanel;

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
    [SerializeField] private int gameDurationMinutes = 360;
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
        Time.timeScale = 0f;

        ResetAllToNormal();
        CloseAllPanels();

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);

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
        ResetAllToNormal();

        elapsedGameMinutes = 0f;
        nextAnomalyMinute = anomalyIntervalMinutes;

        firstWarningShown = false;
        isGameRunning = true;

        // Hide other panels
        mainMenuPanel.SetActive(false);
        resultPanel.SetActive(false);

        // Show gameplay interface
        gameplayHUD.SetActive(true);

        CloseAllPanels();

        if (warningText != null)
            warningText.text = "";

        Time.timeScale = 1f;

        UpdateHUD();
    }

    public void ReturnToMainMenu()
    {
        isGameRunning = false;
        Time.timeScale = 0f;

        ResetAllToNormal();
        CloseAllPanels();

        gameplayHUD.SetActive(false);
        resultPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    public void QuitGame()
    {
        Application.Quit();

        Debug.Log("Game Quit");
    }

    // ==========================================
    // SURVIVAL TIMER
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
                    "WARNING 1: Too many anomalies detected!";
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
            "You successfully survived until 06:00 AM!"
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
            return;

        CloseAllPanels();
        selectedItemName = "";

    }

    public void CloseAllPanels()
    {
        if (typeSelectionPanel != null)
            typeSelectionPanel.SetActive(false);

        selectedItemName = "";
    }

    public void SelectItemToReport(string itemName)
    {
        selectedItemName = itemName;

        if (typeSelectionPanel != null)
            typeSelectionPanel.SetActive(true);
    }

    public void SubmitAnomalyType(string anomalyTypeString)
    {
        if (!Enum.TryParse(
            anomalyTypeString,
            true,
            out AnomalyType reportedType))
        {
            Debug.LogError(
                "Invalid AnomalyType: " + anomalyTypeString);

            CloseAllPanels();
            return;
        }

        EvaluateReport(selectedItemName, reportedType);

        CloseAllPanels();
    }

    // ==========================================
    // REPORT VERIFICATION
    // ==========================================

    private void EvaluateReport(
        string reportedItem,
        AnomalyType reportedType)
    {
        int targetIndex = -1;
        ActiveAnomaly targetAnomaly = null;

        // Find the reported item's active anomaly
        foreach (ActiveAnomaly anomaly in activeAnomalies)
        {
            ItemData item = items[anomaly.itemIndex];

            if (string.Equals(
                item.itemName,
                reportedItem,
                StringComparison.OrdinalIgnoreCase))
            {
                targetIndex = anomaly.itemIndex;
                targetAnomaly = anomaly;
                break;
            }
        }

        if (targetAnomaly == null)
        {
            Debug.LogWarning(
                "Wrong report: No active anomaly on " + reportedItem);

            return;
        }

        if (targetAnomaly.variant.anomalyType == reportedType)
        {
            Debug.Log(
                "Correct! Fixed " + reportedItem);

            activeAnomalies.Remove(targetAnomaly);

            ResetItemToNormal(targetIndex);

            // Update warning text if the situation is contained
            if (activeAnomalies.Count < firstWarningThreshold &&
                warningText != null)
            {
                warningText.text = "";
            }

            UpdateHUD();
        }
        else
        {
            Debug.LogWarning(
                "Wrong anomaly type for " + reportedItem);
        }
    }

    // ==========================================
    // ANOMALY LIFECYCLE
    // ==========================================

    public void ResetAllToNormal()
    {
        activeAnomalies.Clear();

        for (int i = 0; i < items.Count; i++)
        {
            ResetItemToNormal(i);
        }
    }

    private void ResetItemToNormal(int index)
    {
        ItemData item = items[index];

        if (item.normalState != null)
            item.normalState.SetActive(true);

        foreach (AnomalyVariant variant in item.anomalyVariants)
        {
            if (variant.anomalyObject != null)
                variant.anomalyObject.SetActive(false);
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
        List<int> validIndices = new List<int>();

        for (int i = 0; i < items.Count; i++)
        {
            ItemData item = items[i];

            if (item.anomalyVariants == null ||
                item.anomalyVariants.Count == 0)
                continue;

            // Don't spawn another anomaly on an already
            // affected item
            bool alreadyActive = activeAnomalies.Exists(
                a => a.itemIndex == i);

            if (!alreadyActive)
                validIndices.Add(i);
        }

        if (validIndices.Count == 0)
        {
            Debug.LogWarning(
                "No available items for another anomaly.");

            return;
        }

        // Select random item
        int chosenIndex = validIndices[
            UnityEngine.Random.Range(0, validIndices.Count)];

        ItemData chosenItem = items[chosenIndex];

        // Select random anomaly variant
        AnomalyVariant chosenVariant =
            chosenItem.anomalyVariants[
                UnityEngine.Random.Range(
                    0, chosenItem.anomalyVariants.Count)];

        // Apply anomaly
        if (chosenVariant.anomalyType != AnomalyType.ExtraObject)
        {
            if (chosenItem.normalState != null)
                chosenItem.normalState.SetActive(false);
        }

        if (chosenVariant.anomalyObject != null)
            chosenVariant.anomalyObject.SetActive(true);

        // Register active anomaly
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
