using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
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

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Optional: DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializePlayerUpgradesList();
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

                    // Tell ScoreManager to add the score AND show colored feedback
                    if (ScoreManager.Instance != null)
                    {
                        ScoreManager.Instance.AddScoreAndShowFeedback(scoreGeneratedThisTick, prodData.feedbackColor);
                    }

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

        Debug.Log("ProductionManager: Loading production upgrade levels...");
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
            Debug.Log("ProductionManager: Levels loaded.");
            UpdateTotalRateAndNotify(); // Recalculate total rate if levels changed
        }
        else
        {
             Debug.Log("ProductionManager: No changes in loaded production upgrade levels detected.");
        }
    }

    /// <summary>
    /// Provides production upgrade data for saving.
    /// </summary>
    public List<UpgradeSaveData> GetData()
    {
        List<UpgradeSaveData> dataToSave = new List<UpgradeSaveData>();
        foreach (var upgradeState in playerProductionUpgrades)
        {
            // Check if the reference is valid and level is > 0
            if (upgradeState.upgradeDataRef != null && upgradeState.level > 0)
            {
                dataToSave.Add(new UpgradeSaveData
                {
                    upgradeName = upgradeState.upgradeDataRef.name, // Use SO name as ID
                    level = upgradeState.level
                });
            }
        }
        return dataToSave;
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
} 