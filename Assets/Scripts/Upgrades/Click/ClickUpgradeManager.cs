using UnityEngine;
using System;
using System.Collections.Generic;

// Manages click-based upgrades
public class ClickUpgradeManager : MonoBehaviour
{
    public static ClickUpgradeManager Instance { get; private set; }

    // --- Configurable Values ---
    [Header("Click Settings")]
    public decimal baseClickValue = 1.0M; // Initial value per click
    // public decimal multiplicativeClickBonus = 1.0M; // Example for future multipliers

    [Header("Upgrade Data")]
    [Tooltip("Assign all available Click Upgrade ScriptableObjects here for initialization.")]
    public List<ClickUpgradeData> availableClickUpgradesData;

    // --- Runtime State ---
    private Dictionary<ClickUpgradeData, UpgradeState> playerClickUpgradesState;
    private decimal additiveClickBonus = 0.0M; // Total bonus added from upgrades
    private decimal calculatedClickValue = 1.0M; // Cached final click value

    // --- Events ---
    public event Action<decimal> OnClickValueChanged; // Fired when the calculated click value changes
    public event Action<UpgradeState> OnClickUpgradeStateChanged; // Fired when an upgrade level changes

    // --- Constants ---
    private const string ClickUpgradeLevelKeyPrefix = "ClickUpgradeLevel_";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // Optional
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Initialize the dictionary immediately
        InitializeClickUpgradeStates();
        // Note: Loading happens via SaveLoadManager calling LoadClickUpgrades
    }

    /// <summary>
    /// Initializes the dictionary tracking click upgrade levels.
    /// Sets levels to 0 initially. Loading saved levels happens separately.
    /// </summary>
    void InitializeClickUpgradeStates()
    {
        playerClickUpgradesState = new Dictionary<ClickUpgradeData, UpgradeState>();
        if (availableClickUpgradesData == null)
        {
            Debug.LogError("Available Click Upgrades Data list is not assigned in ClickUpgradeManager!");
            availableClickUpgradesData = new List<ClickUpgradeData>(); // Avoid null reference errors
        }

        foreach (var clickData in availableClickUpgradesData)
        {
            if (clickData != null && !playerClickUpgradesState.ContainsKey(clickData))
            {
                // Create the state object with level 0
                UpgradeState newState = new UpgradeState(clickData) { level = 0 };
                playerClickUpgradesState.Add(clickData, newState);
            }
            else if (clickData == null)
            {
                Debug.LogWarning("Null entry found in availableClickUpgradesData list.");
            }
            else
            {
                Debug.LogWarning($"Duplicate ClickUpgradeData {clickData.name} detected in list. Skipping.");
            }
        }
        // Recalculate bonus based on initial (likely level 0) state
        RecalculateTotalClickBonus();
    }

    /// <summary>
    /// Recalculates the total additive click bonus based on current levels of all click upgrades.
    /// </summary>
    void RecalculateTotalClickBonus()
    {
        additiveClickBonus = 0M;
        foreach (var kvp in playerClickUpgradesState)
        {
            ClickUpgradeData data = kvp.Key;
            UpgradeState state = kvp.Value;
            if (data != null && state.level > 0)
            {
                // Cast float clickBonusPerLevel to decimal
                additiveClickBonus += (decimal)data.clickBonusPerLevel * state.level;
            }
        }
        // After recalculating the total bonus, update the final click value
        CalculateAndCacheClickValue();
    }

    /// <summary>
    /// Calculates the final click value based on base value and bonuses.
    /// </summary>
    void CalculateAndCacheClickValue()
    {
        // Start with the base value
        decimal finalValue = baseClickValue + additiveClickBonus;

        // Apply multiplicative bonuses (example)
        // finalValue = finalValue * multiplicativeClickBonus;

        // Ensure click value doesn't go below a minimum (e.g., 0.1 or 1.0 depending on design)
        calculatedClickValue = Math.Max(0.1M, finalValue); // Use M suffix for decimal literal

        OnClickValueChanged?.Invoke(calculatedClickValue);
        // Debug.Log($"Click value recalculated: {calculatedClickValue}");
    }

    /// <summary>
    /// Gets the currently calculated click value.
    /// </summary>
    public decimal GetCalculatedClickValue()
    {
        return calculatedClickValue;
    }

    /// <summary>
    /// Gets the current runtime state (level) of a specific click upgrade.
    /// </summary>
    public UpgradeState GetClickUpgradeState(ClickUpgradeData data)
    {
        if (data == null) return null;

        if (playerClickUpgradesState.TryGetValue(data, out UpgradeState state))
        {
            return state;
        }
        else
        {
            // This case should ideally not happen if initialization is correct
            // and data list hasn't changed at runtime without re-initialization.
            Debug.LogWarning($"State for {data.name} not found in dictionary. Was it added to availableClickUpgradesData?");
            // Optionally create a default state if needed, but might indicate an issue
            // UpgradeState newState = new UpgradeState(data) { level = 0 };
            // playerClickUpgradesState.Add(data, newState);
            // return newState;
            return null;
        }
    }

    /// <summary>
    /// Calculates the cost for the next level of a given click upgrade.
    /// Uses the global progression scale from GameManager.
    /// </summary>
    public decimal CalculateClickUpgradeCost(ClickUpgradeData data, int currentLevel)
    {
        if (data == null) return decimal.MaxValue; // Prevent purchase if data is invalid
        if (currentLevel < 0) currentLevel = 0;

        // Get global scale from GameManager (ensure GameManager instance exists)
        float globalScaleFloat = GameManager.Instance != null ? GameManager.Instance.globalProgressionScale : 1.0f;
        decimal globalScale = (decimal)globalScaleFloat;

        // Cast float baseCost to decimal
        decimal cost = (decimal)data.baseCost * (decimal)Mathf.Pow(data.costScaleFactor, currentLevel) * globalScale;
        return Math.Ceiling(cost);
    }

    /// <summary>
    /// Called when a click upgrade is successfully purchased (after cost is paid).
    /// Increments the level, recalculates bonuses, and notifies listeners.
    /// </summary>
    public void ApplyClickUpgrade(ClickUpgradeData data)
    {
        UpgradeState state = GetClickUpgradeState(data);
        if (state != null)
        {
            state.level++;
            RecalculateTotalClickBonus(); // This recalculates bonus and updates click value
            OnClickUpgradeStateChanged?.Invoke(state); // Notify UI or other listeners
            // Debug.Log($"Applied click upgrade '{data.upgradeName}' level {state.level}.");
        }
        else
        {
            Debug.LogError($"Could not find state for ClickUpgradeData: {data.name} during ApplyClickUpgrade.");
        }
    }

    // --- Save/Load --- 
    public void SaveClickUpgrades()
    {
        foreach (var kvp in playerClickUpgradesState)
        {
            if (kvp.Key != null)
            {
                string key = ClickUpgradeLevelKeyPrefix + kvp.Key.name; // Use name for uniqueness
                PlayerPrefs.SetInt(key, kvp.Value.level);
                // Debug.Log($"Saving Click Upgrade: Key={key}, Level={kvp.Value.level}");
            }
        }
    }

    public void LoadClickUpgrades()
    {
        bool needsRecalculate = false;
        Debug.Log("LoadClickUpgrades: Starting...");
        foreach (var kvp in playerClickUpgradesState)
        {
            if (kvp.Key != null)
            {
                string key = ClickUpgradeLevelKeyPrefix + kvp.Key.name;
                int loadedLevel = PlayerPrefs.GetInt(key, 0); // Default to 0 if not found
                if (kvp.Value.level != loadedLevel)
                {
                    // Debug.Log($"Loading Click Upgrade: Key={key}, Found Level={loadedLevel}, Updating State for {kvp.Key.name}");
                    kvp.Value.level = loadedLevel;
                    needsRecalculate = true;
                    OnClickUpgradeStateChanged?.Invoke(kvp.Value); // Notify UI of loaded level
                }
            }
        }

        if (needsRecalculate)
        {
            Debug.Log("LoadClickUpgrades: Levels loaded, recalculating total bonus...");
            RecalculateTotalClickBonus(); // Recalculate if any levels were loaded
        }
        else
        {
            Debug.Log("LoadClickUpgrades: No changes in loaded levels detected.");
        }
    }

    public void ResetClickUpgrades()
    {
        bool changed = false;
        foreach (var state in playerClickUpgradesState.Values)
        {
            if (state.level != 0)
            {
                state.level = 0;
                OnClickUpgradeStateChanged?.Invoke(state); // Notify UI of reset
                changed = true;
            }
            // Delete the specific key
            string key = ClickUpgradeLevelKeyPrefix + state.upgradeData.name;
            PlayerPrefs.DeleteKey(key);
        }

        if (changed)
        {
            RecalculateTotalClickBonus(); // Recalculate bonus (will likely reset to base)
        }
    }
} 