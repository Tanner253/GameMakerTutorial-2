using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine.SceneManagement; // NEW: Required for SceneManager
// Add using for TextMeshPro if you were to add a direct reference here

/// <summary>
/// Manages all PRODUCTION related upgrades.
/// Handles passive score generation based on owned production upgrades.
/// </summary>
public class ProductionManager : MonoBehaviour
{
    public static ProductionManager Instance { get; private set; }

    [Tooltip("Assign all available Production Upgrade ScriptableObjects here.")]
    public List<ProductionUpgradeData> availableUpgradesData;

    // Runtime state of PRODUCTION upgrades the player owns or can purchase
    private List<UpgradeState> playerProductionUpgrades;

    /// <summary>
    /// Fired when a production upgrade's state changes (e.g., purchased).
    /// Passes the updated UpgradeState.
    /// </summary>
    public event Action<UpgradeState> OnProductionUpgradeStateChanged;

    /// <summary>
    /// Fired when the calculated total production rate per second changes.
    /// </summary>
    public event Action<decimal> OnTotalProductionRateChanged;

    private PrestigeManager _prestigeManagerInstance; // Cache instance
    private GameManager _gameManagerInstance; // Cache GameManager

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializePlayerUpgradesList();
    }

    void Start() // Use Start for finding other Singletons safely
    {
        _prestigeManagerInstance = PrestigeManager.Instance;
        _gameManagerInstance = GameManager.Instance; 

        if (_prestigeManagerInstance != null)
        {
            _prestigeManagerInstance.OnPrestigeCountChanged += HandlePrestigeCountChanged;
        }
        else
        {
            Debug.LogWarning("ProductionManager could not find PrestigeManager instance during Start.");
        }
        // Ensure initial rate calculation includes potential existing prestige bonus
        UpdateTotalRateAndNotify();
    }

    void OnDestroy() // Unsubscribe
    {
         if (_prestigeManagerInstance != null)
        {
            _prestigeManagerInstance.OnPrestigeCountChanged -= HandlePrestigeCountChanged;
        }
    }

    void Update()
    {
        // Iterate through each owned production upgrade state
        foreach (var upgradeState in playerProductionUpgrades)
        {
            // Skip if level is 0 or if dataRef isn't ProductionUpgradeData
            if (upgradeState.level <= 0 || !(upgradeState.upgradeDataRef is ProductionUpgradeData prodData))
            {
                continue;
            }

            // Ensure tickRate is valid to prevent division by zero or infinite loops
            if (prodData.tickRate <= 0)
            {
                continue;
            }

            // Increment the individual timer for this upgrade
            upgradeState.productionTimer += Time.deltaTime;

            // Check if it's time for this upgrade to produce
            if (upgradeState.productionTimer >= prodData.tickRate)
            {
                // Calculate how many ticks occurred (usually 1, but handles frame rate drops)
                int ticksOccurred = Mathf.FloorToInt(upgradeState.productionTimer / prodData.tickRate);
                decimal scoreGeneratedThisTick = 0;

                if (ticksOccurred > 0)
                {
                    // Calculate score generated *by this upgrade* for the ticks that occurred
                    decimal productionPerTick = (decimal)prodData.baseProductionAmount * upgradeState.level;
                    scoreGeneratedThisTick = productionPerTick * ticksOccurred;

                    // Tell ScoreManager to add the score
                    ScoreManager.Instance?.AddScore(scoreGeneratedThisTick);

                    // --- Start Floating Text Logic ---
                    // REMOVED: bool isMainScene = _gameManagerInstance != null && SceneManager.GetActiveScene().buildIndex == _gameManagerInstance.gameObject.scene.buildIndex;
                    
                    // REMOVED DEBUG LOG
                    // Debug.Log($"[ProdManager Tick Check] Upgrade: {prodData.name}, Ticks: {ticksOccurred}, Score: {scoreGeneratedThisTick}, IsMainScene: {isMainScene}");
                    
                    // Check if the GameManager exists and necessary components are assigned
                    if (_gameManagerInstance != null) // Check GameManager first
                    {                       
                        // Check if the specific components needed are valid IN THE CURRENT CONTEXT
                        FloatingTextManager ftm = _gameManagerInstance.floatingTextManager;
                        RectTransform scoreRect = _gameManagerInstance.scoreDisplayRectTransform; 
                        
                        // Only show text if FTM exists AND the RectTransform target exists (implies we are in the correct scene)
                        if (ftm != null && scoreRect != null) 
                        {
                            ftm.ShowFloatingText(
                                scoreGeneratedThisTick,
                                scoreRect.anchoredPosition,
                                prodData.feedbackColor
                            );
                        }
                    }
                    // Optional: Log if the scene check failed
                    // else if (_gameManagerInstance != null) 
                    // {
                    //      Debug.LogWarning($"[ProdManager] Scene check failed. Active Scene Index: {SceneManager.GetActiveScene().buildIndex}, GameManager Scene Index: {_gameManagerInstance.gameObject.scene.buildIndex}");
                    // }
                    // --- End Floating Text Logic ---

                    // Reset timer, retaining the remainder
                    upgradeState.productionTimer -= ticksOccurred * prodData.tickRate;
                }
            }
        }
    }

    void InitializePlayerUpgradesList()
    {
        playerProductionUpgrades = new List<UpgradeState>();
        if (availableUpgradesData == null) {
             Debug.LogError("Available Production Upgrades Data is not assigned in ProductionManager!");
             availableUpgradesData = new List<ProductionUpgradeData>(); // Prevent null errors
             return;
         }

        foreach (var upgradeData in availableUpgradesData)
        {
            if (upgradeData != null)
            {
                // Check for duplicates based on the ScriptableObject reference itself
                 if (playerProductionUpgrades.Any(state => state.upgradeDataRef == upgradeData))
                 {
                     Debug.LogWarning($"Duplicate ProductionUpgradeData {upgradeData.name} detected in available list. Skipping.");
                     continue;
                 }

                // Create an UpgradeState instance specifically for this production upgrade data
                UpgradeState newState = new UpgradeState(upgradeData)
                {
                    level = 0, // Start at level 0
                    productionTimer = 0f // Initialize timer
                };
                playerProductionUpgrades.Add(newState);
            }
            else
            {
                Debug.LogWarning("Null ProductionUpgradeData found in availableUpgradesData list.");
            }
        }
        // Calculate initial rate (will be 0) and notify listeners
        UpdateTotalRateAndNotify();
    }

    /// <summary>
    /// Calculates the total score generated per second by all active production upgrades.
    /// </summary>
    /// <returns>Total production rate (score per second).</returns>
    public decimal GetTotalProductionRatePerSecond()
    {
        decimal totalRate = 0M;
        foreach (var upgradeState in playerProductionUpgrades)
        {
            if (upgradeState.level > 0 && upgradeState.upgradeDataRef is ProductionUpgradeData prodData)
            {
                // Ensure tickRate is valid to prevent division by zero
                if (prodData.tickRate > 0)
                {
                    // Calculate rate per second for this upgrade: (Amount per tick * Level) / Seconds per tick
                    decimal ratePerSecond = (decimal)prodData.baseProductionAmount * upgradeState.level / (decimal)prodData.tickRate;
                    totalRate += ratePerSecond;
                }
            }
        }

        // Apply Prestige Bonus
        if (_prestigeManagerInstance != null)
        {
            int prestigeLevel = _prestigeManagerInstance.GetCurrentPrestigeCount();
            if (prestigeLevel > 0)
            {
                decimal prestigeMultiplier = 1M + ((decimal)prestigeLevel * 0.01M); // +1% per level
                totalRate *= prestigeMultiplier;
            }
        }

        return totalRate;
    }

    /// <summary>
    /// Helper method to recalculate the total rate and invoke the event.
    /// </summary>
    private void UpdateTotalRateAndNotify()
    {
        decimal newRate = GetTotalProductionRatePerSecond();
        OnTotalProductionRateChanged?.Invoke(newRate);
        // Debug.Log($"Total Production Rate updated: {newRate:F1}/s"); // Optional debug log
    }

    /// <summary>
    /// Loads production upgrade levels from the SaveData object.
    /// </summary>
    public void LoadData(SaveData saveData)
    {
        if (saveData == null || saveData.productionUpgradeLevels == null)
        {
            Debug.Log("ProductionManager: No save data for production upgrades, using initial levels (0).");
            // Ensure default state is level 0 (already done in InitializePlayerUpgradesList)
            UpdateTotalRateAndNotify();
            return;
        }

        // Debug.Log("ProductionManager: Loading production upgrade levels...");
        bool stateChanged = false;

        // Create a lookup from the available data list for efficiency
        var availableDataLookup = availableUpgradesData.Where(d => d != null).ToDictionary(d => d.name);
        // Create a lookup for the runtime states
        var runtimeStateLookup = playerProductionUpgrades.Where(s => s.upgradeDataRef != null).ToDictionary(s => s.upgradeDataRef.name);

        foreach (var savedUpgrade in saveData.productionUpgradeLevels)
        {
             // Find the corresponding runtime state object using the saved name
             if (runtimeStateLookup.TryGetValue(savedUpgrade.upgradeName, out UpgradeState state))
             {
                  if (state.level != savedUpgrade.level)
                 {
                     state.level = savedUpgrade.level;
                     state.productionTimer = 0f; // Reset timer on load
                     OnProductionUpgradeStateChanged?.Invoke(state); // Notify UI
                     stateChanged = true;
                     // Debug.Log($"Loaded production upgrade '{savedUpgrade.upgradeName}' level {savedUpgrade.level}.");
                 }
             }
             else
             {
                 // This implies an upgrade was saved that isn't in the current availableUpgradesData list
                 // or the runtime state wasn't initialized correctly.
                 Debug.LogWarning($"ProductionManager: Runtime state or ScriptableObject for saved upgrade '{savedUpgrade.upgradeName}' not found. Skipping load for this upgrade.");
             }
        }

        if (stateChanged)
        {
            // Debug.Log("ProductionManager: Levels loaded.");
            UpdateTotalRateAndNotify(); // Recalculate total rate if levels changed
        }
        else
        {
             Debug.Log("ProductionManager: No changes in loaded production upgrade levels detected.");
        }
    }

    /// <summary>
    /// Updates the SaveData object with production upgrade data.
    /// </summary>
    public void UpdateSaveData(SaveData saveData)
    {
        if (saveData == null) return;

        saveData.productionUpgradeLevels = new List<UpgradeSaveData>();
        foreach (var upgradeState in playerProductionUpgrades)
        {
            if (upgradeState.upgradeDataRef != null && upgradeState.level > 0)
            {
                saveData.productionUpgradeLevels.Add(new UpgradeSaveData
                {
                    upgradeName = upgradeState.upgradeDataRef.name,
                    level = upgradeState.level
                });
            }
        }
    }

    /// <summary>
    /// Resets all production upgrade levels to 0 in memory and resets timers.
    /// Does not affect saved files directly.
    /// </summary>
    public void ResetData()
    {
        Debug.Log("ProductionManager: Resetting runtime production upgrades...");
        bool stateChanged = false;
        foreach (var upgradeState in playerProductionUpgrades)
        {
            if (upgradeState.level != 0)
            {
                upgradeState.level = 0;
                stateChanged = true;
            }
            // Always reset timer on reset
            upgradeState.productionTimer = 0f;

            // Notify UI about the reset state for this upgrade
            OnProductionUpgradeStateChanged?.Invoke(upgradeState);

            // REMOVED: PlayerPrefs key deletion
        }

        if (stateChanged)
        {
            Debug.Log("ProductionManager: Runtime levels reset.");
        }
        else
        {
            Debug.Log("ProductionManager: Runtime levels were already 0.");
        }

        UpdateTotalRateAndNotify(); // Update total rate (will be 0)
    }

    /// <summary>
    /// Gets the current runtime state (level) of a specific production upgrade.
    /// </summary>
    /// <param name="data">The ProductionUpgradeData asset to query.</param>
    /// <returns>The UpgradeState or null if not found.</returns>
    public UpgradeState GetPlayerUpgradeState(ProductionUpgradeData data)
    {
        // Find the state where the upgradeDataRef matches the passed-in ScriptableObject
        return playerProductionUpgrades.FirstOrDefault(upgState => upgState.upgradeDataRef == data);
    }

    /// <summary>
    /// Calculates the cost for the next level of a given production upgrade.
    /// Assumes GameManager exists for global scale.
    /// </summary>
    public decimal CalculateUpgradeCost(ProductionUpgradeData data, int currentLevel)
    {
        if (data == null) return decimal.MaxValue;
        if (currentLevel < 0) currentLevel = 0;

        float globalScaleFloat = GameManager.Instance != null ? GameManager.Instance.globalProgressionScale : 1.0f;
        decimal globalScale = (decimal)globalScaleFloat;

        decimal cost = (decimal)data.baseCost * (decimal)Mathf.Pow(data.costScaleFactor, currentLevel) * globalScale;
        return Math.Ceiling(cost);
    }

    /// <summary>
    /// Applies a successfully purchased production upgrade.
    /// Increments level, updates total rate, and notifies listeners.
    /// </summary>
    public void ApplyUpgrade(ProductionUpgradeData data)
    {
        UpgradeState state = GetPlayerUpgradeState(data);
        if (state != null)
        {
            state.level++;
            OnProductionUpgradeStateChanged?.Invoke(state); // Notify UI FIRST
            UpdateTotalRateAndNotify(); // THEN update the total rate
            // Debug.Log($"Applied production upgrade '{data.upgradeName}' level {state.level}.");
        }
        else
        {
            Debug.LogError($"Could not find state for ProductionUpgradeData: {data.name} during ApplyUpgrade.");
        }
    }

    // NEW Method: Calculates offline earnings without applying them
    public decimal CalculateOfflineEarnings(float offlineSeconds)
    {
        if (offlineSeconds <= 0) return 0M;

        // Use the already calculated total rate per second
        decimal ratePerSecond = GetTotalProductionRatePerSecond(); 
        decimal totalScoreEarned = ratePerSecond * (decimal)offlineSeconds;

        // Apply any potential caps here if needed (e.g., based on max offline time)

        return totalScoreEarned;
    }

    private void HandlePrestigeCountChanged(int newPrestigeCount) // Method to handle event
    {
        // Recalculate total rate when prestige level changes
        UpdateTotalRateAndNotify();
    }
} 