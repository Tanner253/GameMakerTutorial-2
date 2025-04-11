using System;
using System.Collections.Generic; // Needed for Dictionary
using System.Globalization; // Needed for robust decimal conversion
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement; // Needed for scene reloading

public class GameManager : MonoBehaviour
{
    // Singleton pattern instance
    public static GameManager Instance { get; private set; }

    // --- Global Scaling Factor (Remains here for now, could move later) ---
    [Header("Global Settings")]
    [Tooltip(
        "Adjusts the cost scaling globally. > 1.0 makes upgrades more expensive (slower progression), < 1.0 makes them cheaper (faster progression)."
    )]
    public float globalProgressionScale = 1.0f; // Default to no scaling adjustment

    // --- References to Other Managers ---
    [Header("Manager References")]
    public ScoreManager scoreManager; // ADDED
    public ClickUpgradeManager clickUpgradeManager; // ADDED
    public ProductionManager productionManager; // Keep reference
    public SaveLoadManager saveLoadManager; // ADDED
    public PrestigeManager prestigeManager; // NEW
    public FloatingTextManager floatingTextManager; // Make public again
    private OfflineProgressAlertUI offlineAlertUI; // NEW reference

    // --- UI References (Only those not specific to other managers) ---
    [Header("UI References")]
    [Tooltip("ASSIGN IN INSPECTOR! This will be re-found on scene load if needed.")]
    public RectTransform scoreDisplayRectTransform; // Still needed for floating text position
    [Tooltip("ASSIGN IN INSPECTOR! This will be re-found on scene load if needed.")]
    public TextMeshProUGUI totalProductionRateText;

    [Header("Scene Config")]
    [SerializeField] private string mainGameSceneName = "Main"; // Adjust if your main scene name is different

    void Awake()
    {
        // Implement Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Optional: Keep GameManager across scenes
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Clamp global scale
        globalProgressionScale = Mathf.Max(0.1f, globalProgressionScale);

        // --- Find Manager References if not assigned ---
        // Managers are DontDestroyOnLoad, so finding them once in Awake is fine
        if (scoreManager == null) scoreManager = FindFirstObjectByType<ScoreManager>();
        if (clickUpgradeManager == null) clickUpgradeManager = FindFirstObjectByType<ClickUpgradeManager>();
        if (productionManager == null) productionManager = FindFirstObjectByType<ProductionManager>(); 
        if (saveLoadManager == null) saveLoadManager = FindFirstObjectByType<SaveLoadManager>();
        if (floatingTextManager == null) floatingTextManager = FindFirstObjectByType<FloatingTextManager>();
        if (prestigeManager == null) prestigeManager = FindFirstObjectByType<PrestigeManager>(); 

        // NEW: Find the offline alert UI (assuming it's on the same GameObject or easily findable)
        offlineAlertUI = FindFirstObjectByType<OfflineProgressAlertUI>(); 
        if (offlineAlertUI == null)
        {
            Debug.LogWarning("GameManager could not find OfflineProgressAlertUI. Offline alerts will not be shown.");
        }

        // Null check managers after attempting to find them
        if (scoreManager == null) Debug.LogError("GameManager could not find ScoreManager!");
        if (clickUpgradeManager == null) Debug.LogError("GameManager could not find ClickUpgradeManager!");
        if (productionManager == null) Debug.LogWarning("GameManager could not find ProductionManager (This is expected if the scene doesn't use passive production).");
        if (saveLoadManager == null) Debug.LogError("GameManager could not find SaveLoadManager!");
        if (floatingTextManager == null) Debug.LogError("GameManager could not find FloatingTextManager!");
        if (prestigeManager == null) Debug.LogError("GameManager could not find PrestigeManager!"); 

        // --- Initialization and Loading ---
        if (saveLoadManager != null)
        {
            saveLoadManager.InitializeManagers(scoreManager, clickUpgradeManager, productionManager, prestigeManager); 
        }
        else
        {
             Debug.LogError("CRITICAL: SaveLoadManager not found! Game cannot be loaded or saved.");
        }
       
        // Subscribe to ProductionManager's event (if it exists)
        // Note: If ProductionManager is also DontDestroyOnLoad, this subscription is fine here.
        // If ProductionManager is scene-specific, subscription needs to happen after scene load.
        if (productionManager != null)
        {
            productionManager.OnTotalProductionRateChanged += UpdateProductionRateDisplay;
        }
    }

    // --- Scene Load Handling ---
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        // Also unsubscribe from manager events here if they might be destroyed before GameManager
        if (productionManager != null)
        {
            productionManager.OnTotalProductionRateChanged -= UpdateProductionRateDisplay;
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[GameManager] Scene Loaded: {scene.name}");
        // Check if the loaded scene is the main game scene
        if (scene.name == mainGameSceneName)
        {
            Debug.Log("[GameManager] Main game scene loaded. Finding UI references...");
            // Re-find essential UI references that exist IN THIS SCENE
            FindSceneUIReferences();

            // Re-subscribe to events from scene-specific managers if necessary
            // (Assuming ProductionManager is DontDestroyOnLoad based on previous logic)
            // If ProductionManager was scene-specific, you'd find it and subscribe here.
            
            // Update UI immediately after finding references
            if (productionManager != null)
            {
                 // Unsubscribe first to prevent double-subscription if OnEnable also ran
                productionManager.OnTotalProductionRateChanged -= UpdateProductionRateDisplay;
                productionManager.OnTotalProductionRateChanged += UpdateProductionRateDisplay;
                UpdateProductionRateDisplay(productionManager.GetTotalProductionRatePerSecond());
            }
            else
            {
                UpdateProductionRateDisplay(0);
            }
        }
    }

    void FindSceneUIReferences()
    {
        // Find Score Display - Be specific if multiple RectTransforms exist
        // Option 1: Find by name (adjust "Counter" if needed)
        GameObject counterGO = GameObject.Find("Counter"); // Assumes name is unique
        if (counterGO != null)
        {
            scoreDisplayRectTransform = counterGO.GetComponent<RectTransform>();
            if (scoreDisplayRectTransform == null) Debug.LogError("[GameManager] Found 'Counter' GameObject but it has no RectTransform!", counterGO);
            else Debug.Log("[GameManager] Found scoreDisplayRectTransform on 'Counter' GameObject.", scoreDisplayRectTransform);
        }
        else
        {
             Debug.LogError("[GameManager] Could not find GameObject named 'Counter' to get scoreDisplayRectTransform!");
             scoreDisplayRectTransform = null; // Ensure it's null if not found
        }

        // Find Total Production Rate Text - Be specific!
        GameObject prodRateGO = GameObject.Find("ProductionRate"); // UPDATED NAME based on Hierarchy
        if (prodRateGO != null)
        {
            totalProductionRateText = prodRateGO.GetComponent<TextMeshProUGUI>();
            if (totalProductionRateText == null) Debug.LogError("[GameManager] Found 'ProductionRate' GameObject but it has no TextMeshProUGUI component!", prodRateGO);
            else Debug.Log("[GameManager] Found totalProductionRateText on 'ProductionRate' GameObject.", totalProductionRateText);
        }
        else
        {
            Debug.LogError("[GameManager] Could not find GameObject named 'ProductionRate'! Please ensure it exists and the name matches exactly."); // UPDATED log message
            totalProductionRateText = null; // Ensure null if not found
        }

        // IMPORTANT: Check if floatingTextManager also needs its references updated (e.g., its spawn parent canvas)
        if (floatingTextManager != null)
        {
             floatingTextManager.FindSceneReferences(); // Assumes FloatingTextManager has a method to find its scene stuff
        }
    }

    void Start()
    {
        // Apply offline progress calculation *after* all managers are loaded and initialized
        // This should run after Awake and potentially after the first OnSceneLoaded
        ApplyOfflineProduction();

        // Initial UI update relies on OnSceneLoaded finding references first
        // if (productionManager != null)
        // {
        //     UpdateProductionRateDisplay(productionManager.GetTotalProductionRatePerSecond());
        // }
        // else
        // {
        //     UpdateProductionRateDisplay(0); 
        // }
    }

    // Method called when a click occurs
    public void ProcessClick(Vector2 clickPosition)
    {
        if (clickUpgradeManager == null || scoreManager == null || floatingTextManager == null)
        {
            Debug.LogError("Missing manager reference in ProcessClick!");
            return;
        }

        decimal clickValue = clickUpgradeManager.GetCalculatedClickValue();
        scoreManager.AddScore(clickValue);

        // Show floating text using the value obtained from the ClickUpgradeManager
        // Ensure scoreDisplayRectTransform is valid before calling ProcessClick if it relies on it indirectly
        // The current call uses clickPosition, so it's okay
        floatingTextManager.ShowFloatingText(clickValue, clickPosition);
    }

    // --- Application Lifecycle Methods for Saving --- 
    // Delegate saving to SaveLoadManager
    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            saveLoadManager?.SaveGameData();
        }
    }

    void OnApplicationQuit()
    {
        saveLoadManager?.SaveGameData();
    }

    // --- Game Reset --- 
    // This might need adjustment - Does Reset Game Data mean prestige or a full wipe?
    // Assuming ResetGameData performs a PRESTIGE action for now.
    public void RequestPrestige()
    {
        // Delegate the actual prestige logic to PrestigeManager
        prestigeManager?.PerformPrestige();
    }

    // If a full hard reset is needed separate from prestige:
    public void HardResetGameData()
    {
        Debug.LogWarning("Performing HARD RESET - Deleting save file and resetting runtime data.");
        saveLoadManager?.DeleteSaveFile(); // Delete the save file

        // Reset runtime data in all managers
        scoreManager?.ResetData();
        clickUpgradeManager?.ResetData();
        productionManager?.ResetData();
        prestigeManager?.ResetData(); // NEW call

        // Update UI explicitly if needed, as events should handle most of it
        // Example: Ensure production rate display is updated
        if (productionManager != null)
        {
             UpdateProductionRateDisplay(productionManager.GetTotalProductionRatePerSecond());
        }
        else
        {
             UpdateProductionRateDisplay(0);
        }

        // *** NEW STEP: Immediately save the reset state ***
        saveLoadManager?.SaveGameData();

        Debug.LogWarning("Hard Reset Complete. Save file deleted, runtime data reset, and reset state saved.");

        // REMOVED: SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // --- Production Rate Display --- 
    // Kept here as it uses a UI element specific to the overall game view
    void UpdateProductionRateDisplay(decimal newTotalRate)
    {
        if (totalProductionRateText != null)
        {
            // Use the utility formatter
            string formattedRate = NumberFormatter.FormatNumber(newTotalRate);

            totalProductionRateText.text = $"Generating: {formattedRate}/s";
        }
    }

    // NEW: Method to handle offline calculation call
    void ApplyOfflineProduction()
    {
        long lastSaveTicks = saveLoadManager?.GetLastLoadedTimestampTicks() ?? 0;
        if (lastSaveTicks == 0) return; // No previous save time

        TimeSpan offlineTime = DateTime.UtcNow - new DateTime(lastSaveTicks, DateTimeKind.Utc);
        Debug.Log($"[Offline Calc] Offline time: {offlineTime.TotalSeconds:F0} seconds");

        if (offlineTime.TotalSeconds <= 10) return; // Ignore very short offline times

        // Calculate offline earnings (delegate to ProductionManager)
        decimal offlineScoreEarned = productionManager?.CalculateOfflineEarnings((float)offlineTime.TotalSeconds) ?? 0M;

        if (offlineScoreEarned > 0)
        {
            // Add score directly (or maybe show breakdown?)
            scoreManager?.AddScore(offlineScoreEarned);
            Debug.Log($"[Offline Calc] Awarded {offlineScoreEarned:F0} score for offline time.");

            // NEW: Show the alert UI
            Debug.Log($"[Offline Calc] Checking offlineAlertUI reference before calling ShowAlert. Is null? {offlineAlertUI == null}");
            offlineAlertUI?.ShowAlert(offlineScoreEarned);
        }
    }
}
