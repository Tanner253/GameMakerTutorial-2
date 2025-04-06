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

    private const string ProdUpgradeLevelKeyPrefix = "ProdUpgradeLevel_";

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

        InitializePlayerUpgrades();
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

    void InitializePlayerUpgrades()
    {
        playerProductionUpgrades = new List<UpgradeState>();
        bool anyLoaded = false;
        foreach (var upgradeData in availableUpgradesData)
        {
            if (upgradeData != null)
            {
                // Create an UpgradeState instance specifically for this production upgrade data
                UpgradeState newState = new UpgradeState(upgradeData);

                // Load the saved level for this upgrade
                string key = ProdUpgradeLevelKeyPrefix + upgradeData.name; // Use upgrade name as part of the key
                newState.level = PlayerPrefs.GetInt(key, 0); // Default to 0 if not found
                if (newState.level > 0) anyLoaded = true;

                playerProductionUpgrades.Add(newState);
            }
            else
            {
                Debug.LogWarning("Null ProductionUpgradeData found in availableUpgradesData list.");
            }
        }
        if (anyLoaded) Debug.Log("Production upgrade levels loaded during initialization.");
        // Note: No recalculation needed here as production is calculated per-frame in Update
        // UI updates are handled by OnProductionUpgradeStateChanged event when levels actually change (purchase)
        // Initial UI population should read the loaded state.

        // Calculate initial rate based on loaded levels and notify listeners
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
    /// Loads production upgrade levels from PlayerPrefs.
    /// Called by SaveLoadManager.
    /// </summary>
    public void LoadProductionUpgrades()
    {
        Debug.Log("ProductionManager: Loading production upgrade levels...");
        bool anyLoaded = false;
        bool stateChanged = false;
        foreach (var upgradeState in playerProductionUpgrades)
        {
            if (upgradeState.upgradeDataRef != null && upgradeState.upgradeDataRef is ProductionUpgradeData prodData)
            {
                string key = ProdUpgradeLevelKeyPrefix + prodData.name;
                int loadedLevel = PlayerPrefs.GetInt(key, 0);
                if (upgradeState.level != loadedLevel)
                {
                    upgradeState.level = loadedLevel;
                    upgradeState.productionTimer = 0f; // Reset timer on load
                    OnProductionUpgradeStateChanged?.Invoke(upgradeState); // Notify UI
                    anyLoaded = true;
                    stateChanged = true;
                }
            }
        }

        if (anyLoaded) Debug.Log("ProductionManager: Levels loaded.");
        if (stateChanged) UpdateTotalRateAndNotify(); // Recalculate total rate if levels changed
    }

    /// <summary>
    /// Saves production upgrade levels to PlayerPrefs.
    /// This method itself is called by SaveLoadManager.
    /// </summary>
    public void SaveProductionUpgrades()
    {
        foreach (var upgradeState in playerProductionUpgrades)
        {
            if (upgradeState.upgradeDataRef != null && upgradeState.upgradeDataRef is ProductionUpgradeData prodData)
            {
                string key = ProdUpgradeLevelKeyPrefix + prodData.name;
                PlayerPrefs.SetInt(key, upgradeState.level);
                // Debug.Log($"Saving Prod Upgrade: Key={key}, Level={upgradeState.level}");
            }
        }
        // PlayerPrefs.Save() is called by SaveLoadManager after all saving is done.
    }

    /// <summary>
    /// Resets the runtime state AND saved data for production upgrades.
    /// Called by SaveLoadManager during the reset process.
    /// </summary>
    public void ResetProductionUpgrades()
    {
        Debug.Log("ProductionManager: Resetting production upgrades...");
        bool stateChanged = false;
        foreach (var upgradeState in playerProductionUpgrades)
        {
            if (upgradeState.level != 0)
            {
                upgradeState.level = 0;
                upgradeState.productionTimer = 0f; // Reset timer too
                OnProductionUpgradeStateChanged?.Invoke(upgradeState);
                stateChanged = true;
            }
            // Delete the specific key
            if (upgradeState.upgradeDataRef != null)
            {
                string key = ProdUpgradeLevelKeyPrefix + upgradeState.upgradeDataRef.name;
                PlayerPrefs.DeleteKey(key);
            }
        }
        if (stateChanged) Debug.Log("ProductionManager: Runtime levels reset and PlayerPrefs keys deleted.");

        UpdateTotalRateAndNotify(); // Update total rate (will be 0)
    }

    /// <summary>
    /// Gets the current runtime state (level) of a specific production upgrade.
    /// </summary>
    /// <param name="data">The ProductionUpgradeData asset to query.</param>
    /// <returns>The UpgradeState or null if not found.</returns>
    public UpgradeState GetPlayerUpgradeState(ProductionUpgradeData data)
    {
        return playerProductionUpgrades.FirstOrDefault(upgState => upgState.upgradeDataRef == data);
    }

    /// <summary>
    /// Calculates the cost for the next level of a given production upgrade.
    /// </summary>
    public decimal CalculateUpgradeCost(ProductionUpgradeData data, int currentLevel)
    {
        if (currentLevel < 0) currentLevel = 0;

        // Cost formula: baseCost * (costScaleFactor ^ level) * globalScale
        decimal globalScale = (decimal)GameManager.Instance.globalProgressionScale;
        // Cast float baseCost to decimal for calculation
        decimal cost = (decimal)data.baseCost * (decimal)Mathf.Pow(data.costScaleFactor, currentLevel) * globalScale;

        return Math.Ceiling(cost); // Round up to nearest whole number
    }

    /// <summary>
    /// Attempts to purchase the next level of a production upgrade.
    /// </summary>
    /// <param name="dataToPurchase">The ProductionUpgradeData asset to purchase.</param>
    /// <returns>True if purchase was successful, false otherwise.</returns>
    public bool TryPurchaseUpgrade(ProductionUpgradeData dataToPurchase)
    {
        UpgradeState upgradeState = GetPlayerUpgradeState(dataToPurchase);
        if (upgradeState == null)
        {
            Debug.LogError($"Upgrade data '{dataToPurchase.upgradeName}' not found in player production upgrades list.");
            return false;
        }

        decimal cost = CalculateUpgradeCost(dataToPurchase, upgradeState.level);
        // Use ScoreManager's central purchase logic
        if (ScoreManager.Instance != null && ScoreManager.Instance.TrySpendScore(cost))
        {
            upgradeState.level++;
            upgradeState.productionTimer = 0f; // Reset timer on purchase for fairness
            OnProductionUpgradeStateChanged?.Invoke(upgradeState); // Notify listeners (UI)
            Debug.Log($"Purchased '{dataToPurchase.upgradeName}' level {upgradeState.level} for {cost:F0}.");

            // After successfully changing level, update total rate
            UpdateTotalRateAndNotify();
            return true;
        }
        else
        {
            Debug.Log($"Not enough score ({ScoreManager.Instance.GetCurrentScore():F1}) to purchase '{dataToPurchase.upgradeName}' level {upgradeState.level + 1} costing {cost:F0}");
        }

        return false;
    }
} 