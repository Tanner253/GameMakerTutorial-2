using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

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

    private decimal totalProductionPerSecond = 0M;
    private float productionTimer = 0f;
    private const float PRODUCTION_INTERVAL = 1.0f; // Calculate and add score every second

    /// <summary>
    /// Fired when a production upgrade's state changes (e.g., purchased).
    /// Passes the updated UpgradeState.
    /// </summary>
    public event Action<UpgradeState> OnProductionUpgradeStateChanged;

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
        CalculateTotalProduction();
    }

    void Update()
    {
        productionTimer += Time.deltaTime;
        if (productionTimer >= PRODUCTION_INTERVAL)
        {
            decimal scoreToAdd = totalProductionPerSecond * (decimal)PRODUCTION_INTERVAL;
            if (scoreToAdd > 0)
            {
                GameManager.Instance.AddPassiveScore(scoreToAdd);
            }
            productionTimer -= PRODUCTION_INTERVAL; // Reset timer, retaining overshoot
        }
    }

    void InitializePlayerUpgrades()
    {
        playerProductionUpgrades = new List<UpgradeState>();
        foreach (var upgradeData in availableUpgradesData)
        {
            if (upgradeData != null)
            {
                // Create an UpgradeState instance specifically for this production upgrade data
                playerProductionUpgrades.Add(new UpgradeState(upgradeData));
            }
            else
            {
                Debug.LogWarning("Null ProductionUpgradeData found in availableUpgradesData list.");
            }
        }
    }

    void CalculateTotalProduction()
    {
        totalProductionPerSecond = 0M;
        foreach (var upgradeState in playerProductionUpgrades)
        {
            // Ensure the dataRef is actually ProductionUpgradeData before accessing its fields
            if (upgradeState.upgradeDataRef is ProductionUpgradeData prodData && upgradeState.level > 0 && prodData.tickRate > 0)
            {
                // Production per second = (amount per tick) * (ticks per second)
                // Cast float baseProductionAmount to decimal before multiplying
                decimal productionPerTick = (decimal)prodData.baseProductionAmount * upgradeState.level; // Linear scaling per level
                decimal ticksPerSecond = 1M / (decimal)prodData.tickRate;
                totalProductionPerSecond += productionPerTick * ticksPerSecond;
            }
        }
        // Debug.Log($"Total Production Per Second: {totalProductionPerSecond:F2}");
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
        // Use GameManager's central purchase logic which now handles decimal costs
        if (GameManager.Instance.TryPurchaseUpgrade(cost)) 
        {
            upgradeState.level++;
            CalculateTotalProduction(); // Recalculate production after purchase
            OnProductionUpgradeStateChanged?.Invoke(upgradeState); // Notify listeners (UI)
            Debug.Log($"Purchased '{dataToPurchase.upgradeName}' level {upgradeState.level} for {cost:F0}. New total PPS: {totalProductionPerSecond:F2}");
            return true;
        }
        else
        {
            Debug.Log($"Not enough score ({GameManager.Instance.GetCurrentScore():F1}) to purchase '{dataToPurchase.upgradeName}' level {upgradeState.level + 1} costing {cost:F0}");
        }

        return false;
    }
} 