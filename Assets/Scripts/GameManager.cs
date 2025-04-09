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

    // --- UI References (Only those not specific to other managers) ---
    [Header("UI References")]
    public FloatingTextManager floatingTextManager;
    public RectTransform scoreDisplayRectTransform; // Still needed for floating text position
    public TextMeshProUGUI totalProductionRateText;

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
        // It's generally better to assign these in the Inspector, but this provides fallback.
        if (scoreManager == null) scoreManager = FindFirstObjectByType<ScoreManager>();
        if (clickUpgradeManager == null) clickUpgradeManager = FindFirstObjectByType<ClickUpgradeManager>();
        if (productionManager == null) productionManager = FindFirstObjectByType<ProductionManager>(); // May still be null if not in scene
        if (saveLoadManager == null) saveLoadManager = FindFirstObjectByType<SaveLoadManager>();
        if (floatingTextManager == null) floatingTextManager = FindFirstObjectByType<FloatingTextManager>();

        // Null check managers after attempting to find them
        if (scoreManager == null) Debug.LogError("GameManager could not find ScoreManager!");
        if (clickUpgradeManager == null) Debug.LogError("GameManager could not find ClickUpgradeManager!");
        // ProductionManager can be optional
        if (productionManager == null) Debug.LogWarning("GameManager could not find ProductionManager (This is expected if the scene doesn't use passive production).");
        if (saveLoadManager == null) Debug.LogError("GameManager could not find SaveLoadManager!");
        if (floatingTextManager == null) Debug.LogError("GameManager could not find FloatingTextManager!");

        // --- Initialization and Loading ---
        // Ensure SaveLoadManager exists before trying to initialize
        if (saveLoadManager != null)
        {
             saveLoadManager.InitializeManagers(); // SaveLoadManager now coordinates loading/initialization
        }
        else
        {
             Debug.LogError("CRITICAL: SaveLoadManager not found! Game cannot be loaded or saved.");
             // Handle this critical error appropriately - maybe load defaults manually or stop execution?
        }
       
        // Subscribe to ProductionManager's event (if it exists)
        if (productionManager != null)
        {
            productionManager.OnTotalProductionRateChanged += UpdateProductionRateDisplay;
            // Also update display with initial rate from ProductionManager if it exists
            UpdateProductionRateDisplay(productionManager.GetTotalProductionRatePerSecond());
        }
        else
        {
            // Set display to 0 if no production manager
            UpdateProductionRateDisplay(0);
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from ProductionManager's event
        if (productionManager != null)
        {
            productionManager.OnTotalProductionRateChanged -= UpdateProductionRateDisplay;
        }
    }

    void Start()
    {
        // Get initial production rate and update display (if ProductionManager exists)
        if (productionManager != null)
        {
            UpdateProductionRateDisplay(productionManager.GetTotalProductionRatePerSecond());
        }
        else
        {
            // Optionally hide or set default text if no production manager
            UpdateProductionRateDisplay(0); // Show 0/s if no production
            // if (totalProductionRateText != null) totalProductionRateText.gameObject.SetActive(false);
        }

        // Initial UI updates for score/click value are now handled by their respective managers after LoadGame/InitializeManagers
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
    // Delegate reset to SaveLoadManager
    public void ResetGameData()
    {
        saveLoadManager?.ResetAllGameData();

        // Potentially re-fetch initial state displays after reset if needed
        if (productionManager != null)
        {
            UpdateProductionRateDisplay(productionManager.GetTotalProductionRatePerSecond());
        }
        else
        {
            UpdateProductionRateDisplay(0);
        }
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
}
