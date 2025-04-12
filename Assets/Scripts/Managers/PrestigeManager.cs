using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PrestigeManager : MonoBehaviour
{
    public static PrestigeManager Instance { get; private set; }

    [Header("Configuration")]
    [Tooltip(
        "The amount of Lifetime Score exceeding the prestige requirement needed to earn 1 Gold Bar."
    )]
    public decimal scorePerGoldBar = 1000M; // Default: 1 Thousand score over requirement = 1 GB

    [Header("Scene Navigation")]
    [SerializeField]
    [Tooltip("The name of the scene to load after performing prestige. Must be in Build Settings.")]
    private string prestigeSceneName = "PrestigeScene";
    public string PrestigeSceneName => prestigeSceneName;

    [Header("Data References")]
    [Tooltip("Assign all available Prestige Upgrade ScriptableObjects here.")]
    public List<PrestigeUpgradeData> availablePrestigeUpgradesData;
    [Tooltip("Assign the specific Prestige Upgrade SO that unlocks the Lemon feature.")]
    [SerializeField] private PrestigeUpgradeData unlockLemonsUpgradeData; // Assign in Inspector
    public PrestigeUpgradeData UnlockLemonsUpgradeData => unlockLemonsUpgradeData; // Public accessor

    // --- Runtime State ---
    private decimal goldBars = 0M;
    private int prestigeCount = 0;
    private Dictionary<PrestigeUpgradeData, UpgradeState> playerPrestigeUpgradesState;

    // --- Events ---
    public event Action<decimal> OnGoldBarsChanged; // Fired when GB balance changes
    public event Action<int> OnPrestigeCountChanged; // Fired when prestige count changes
    public event Action<PrestigeUpgradeData, int> OnPrestigeUpgradePurchased; // Fired when a prestige upgrade level changes
    public event Action<decimal> OnPotentialPrestigeGainCalculated; // Fired with potential GB gain
    public event Action OnPrestigeDataLoaded; // NEW: Fired after LoadData completes

    // --- Manager References ---
    private ScoreManager _scoreManager;
    private ClickUpgradeManager _clickUpgradeManager;
    private ProductionManager _productionManager;
    private SaveLoadManager _saveLoadManager;

    void Awake()
    {
        try
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Debug.LogWarning("[PrestigeManager] Duplicate instance detected. Destroying self.");
                Destroy(gameObject);
                return;
            }

            InitializePrestigeUpgradeStates();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[PrestigeManager] EXCEPTION in Awake: {ex.Message}\n{ex.StackTrace}");
        }
    }

    public void InitializeManagers(
        ScoreManager sm,
        ClickUpgradeManager cum,
        ProductionManager pm,
        SaveLoadManager slm
    )
    {
        try
        {
            _scoreManager = sm;
            _clickUpgradeManager = cum;
            _productionManager = pm;
            _saveLoadManager = slm;

            if (_scoreManager != null)
            {
                _scoreManager.OnScoreChanged += HandleScoreChangeForPotentialGain;
            }
            else
            {
                Debug.LogWarning(
                    "[PrestigeManager] ScoreManager is null during InitializeManagers."
                );
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError(
                $"[PrestigeManager] EXCEPTION in InitializeManagers: {ex.Message}\n{ex.StackTrace}"
            );
        }
    }

    void OnDestroy()
    {
        if (_scoreManager != null)
        {
            _scoreManager.OnScoreChanged -= HandleScoreChangeForPotentialGain;
        }
    }

    // NEW: Method to recalculate potential gain when score changes
    void HandleScoreChangeForPotentialGain(decimal newScore)
    {
         // REMOVED Throttle: Calculate potential gain whenever score changes
         CalculateAndNotifyPotentialGain();
    }

    void InitializePrestigeUpgradeStates()
    {
        playerPrestigeUpgradesState = new Dictionary<PrestigeUpgradeData, UpgradeState>();
        if (availablePrestigeUpgradesData == null)
            availablePrestigeUpgradesData = new List<PrestigeUpgradeData>();

        foreach (var prestigeData in availablePrestigeUpgradesData)
        {
            if (prestigeData != null && !playerPrestigeUpgradesState.ContainsKey(prestigeData))
            {
                UpgradeState newState = new UpgradeState(prestigeData) { level = 0 };
                playerPrestigeUpgradesState.Add(prestigeData, newState);
            }
        }
    }

    // NEW: Calculates the lifetime score required for the *next* prestige level.
    public decimal GetRequiredScoreForNextPrestige()
    {
        int nextPrestigeLevel = prestigeCount + 1;
        switch (nextPrestigeLevel)
        {
            case 1:
                return 10000M; // 10 K
            case 2:
                return 100000M; // 100 K
            case 3:
                return 1000000M; // 1 M
            default:
                return 1000000M; // Requirement stays at 1M for levels 4+ (Adjust if needed)
            // Example scaling beyond level 3:
            // default: return 1000000M * (decimal)Math.Pow(10, nextPrestigeLevel - 3);
        }
    }

    public bool CanAffordPrestige()
    {
        if (_scoreManager == null) {
            Debug.LogError("[PrestigeManager] ScoreManager is null in CanAffordPrestige. Cannot check.");
            return false;
        }
        decimal requiredScore = GetRequiredScoreForNextPrestige();
        return _scoreManager.GetCurrentScore() >= requiredScore;
    }

    public decimal CalculatePotentialGoldBarGain()
    {
        if (_scoreManager == null) {
            Debug.LogError("[PrestigeManager] ScoreManager is null in CalculatePotentialGoldBarGain. Returning 0.");
            return 0M;
        }

        decimal requiredScore = GetRequiredScoreForNextPrestige();
        decimal currentScore = _scoreManager.GetCurrentScore();

        if (currentScore < requiredScore)
        {
            return 0M; // Cannot gain GB if requirement (cost) isn't met
        }

        decimal excessScore = currentScore - requiredScore;

        if (scorePerGoldBar <= 0)
        {
            Debug.LogError("scorePerGoldBar must be positive! Setting gain to 0.");
            return 0M;
        }

        decimal potentialGain = Math.Floor(excessScore / scorePerGoldBar);

        return Math.Max(0M, potentialGain);
    }

    void CalculateAndNotifyPotentialGain()
    {
        decimal potentialGain = CalculatePotentialGoldBarGain();
        // Debug.Log($"[PrestigeManager] CalculateAndNotifyPotentialGain: Calculated Gain = {potentialGain}. Invoking event..."); 
        OnPotentialPrestigeGainCalculated?.Invoke(potentialGain);
    }

    public void PerformPrestige()
    {
        if (!CanAffordPrestige())
        {
            Debug.LogWarning("Attempted to prestige but conditions not met.");
            return;
        }

        Debug.Log("--- Performing Prestige --- ");

        decimal earnedGB = CalculatePotentialGoldBarGain();
        goldBars += earnedGB;
        Debug.Log($"Earned {earnedGB} Gold Bars. Total GB: {goldBars}");
        OnGoldBarsChanged?.Invoke(goldBars);

        prestigeCount++;
        Debug.Log($"Prestige Count: {prestigeCount}");
        OnPrestigeCountChanged?.Invoke(prestigeCount);

        _scoreManager?.ResetData();
        _clickUpgradeManager?.ResetData();
        _productionManager?.ResetData();

        _saveLoadManager?.SaveGameData();

        Debug.Log("--- Prestige Complete --- ");

        CalculateAndNotifyPotentialGain();
    }

    // --- Prestige Upgrade Logic ---

    public UpgradeState GetPrestigeUpgradeState(PrestigeUpgradeData data)
    {
        if (data != null && playerPrestigeUpgradesState.TryGetValue(data, out UpgradeState state))
        {
            return state;
        }
        return null;
    }

    public decimal GetCurrentGoldBars()
    {
        return goldBars;
    }

    public int GetCurrentPrestigeCount()
    {
        return prestigeCount;
    }

    public decimal CalculatePrestigeUpgradeCost(PrestigeUpgradeData data, int currentLevel)
    {
        if (data == null) return 0M;

        // For unique unlocks, cost is 0 after level 0 (i.e., once purchased)
        if (data.isUniqueUnlock && currentLevel > 0)
        {
            return 0M;
        }

        // Calculate cost using exponential scaling
        // Math.Pow returns double, base cost is now double
        double costMultiplier = Math.Pow(data.costScaleFactor, currentLevel);
        // Perform calculation in double, then cast final result to decimal if needed elsewhere,
        // but for cost comparison with Gold Bars (decimal), we need decimal.
        decimal cost = (decimal)(data.baseCostGoldBars * costMultiplier);

        // Apply floor or ceiling if desired, for now return calculated value
        // return Math.Floor(cost);
        return cost;
    }

    public bool CanAffordPrestigeUpgrade(PrestigeUpgradeData data)
    {
        UpgradeState state = GetPrestigeUpgradeState(data);
        if (state == null)
            return false;
        if (data.isUniqueUnlock && state.level > 0)
            return false; // Cannot buy unique upgrades more than once
        if (prestigeCount < data.requiredPrestigeLevel)
            return false; // Check prestige level requirement

        decimal cost = CalculatePrestigeUpgradeCost(data, state.level);
        return goldBars >= cost;
    }

    public void PurchasePrestigeUpgrade(PrestigeUpgradeData data)
    {
        if (!CanAffordPrestigeUpgrade(data))
        {
            Debug.LogWarning($"Cannot afford or purchase prestige upgrade: {data?.upgradeName}");
            return;
        }

        UpgradeState state = GetPrestigeUpgradeState(data);
        decimal cost = CalculatePrestigeUpgradeCost(data, state.level);

        goldBars -= cost;
        OnGoldBarsChanged?.Invoke(goldBars);

        state.level++;
        OnPrestigeUpgradePurchased?.Invoke(data, state.level);
        Debug.Log(
            $"Purchased {data.upgradeName} Level {state.level} for {cost} GB. Remaining GB: {goldBars}"
        );

        _saveLoadManager?.SaveGameData();
    }

    // NEW: Checks if the upgrade corresponding to a specific feature unlock has been purchased (level > 0)
    // Requires the specific ScriptableObject for the unlock feature to be known/assigned.
    // Example usage assumes you have a field like:
    // [SerializeField] private PrestigeUpgradeData unlockLemonsUpgradeData;
    public bool IsFeatureUnlocked(PrestigeUpgradeData featureUnlockData)
    {
        if (featureUnlockData == null || featureUnlockData.effectType != PrestigeEffectType.UnlockFeature)
        {
            Debug.LogWarning("IsFeatureUnlocked called with invalid data.");
            return false; // Or true depending on default game state
        }
        UpgradeState state = GetPrestigeUpgradeState(featureUnlockData);
        return state != null && state.level > 0;
    }

    // NEW: Calculate total bonus value for a given effect type across all purchased upgrades
    public float GetTotalBonusForEffect(PrestigeEffectType effectType)
    {
        float totalBonus = 0f;
        if (playerPrestigeUpgradesState == null) return totalBonus;

        foreach (var kvp in playerPrestigeUpgradesState)
        {
            PrestigeUpgradeData data = kvp.Key;
            UpgradeState state = kvp.Value;

            if (data.effectType == effectType && state.level > 0)
            {
                totalBonus += data.effectValuePerLevel * state.level;
            }
        }
        return totalBonus;
    }

    // --- Specific Bonus Getters (Examples using the generic method) ---
    public float GetTotalLemonSpawnTimeReduction() {
        // Expects negative values from effectValuePerLevel for reduction
        return GetTotalBonusForEffect(PrestigeEffectType.LemonSpawnRate);
    }

     public float GetTotalLemonValueBonusMinutes() {
        return GetTotalBonusForEffect(PrestigeEffectType.LemonValue);
    }

    public float GetTotalLemonLifespanBonusSeconds() {
        return GetTotalBonusForEffect(PrestigeEffectType.LemonLifespan);
    }

     public float GetTotalClickMultiplierBonus() {
        // Returns the total percentage bonus (e.g., 0.15 for 15%)
        return GetTotalBonusForEffect(PrestigeEffectType.ClickMultiplier);
    }

    // Replace the existing GetBaseLemonLifespan method with this version that also returns max value
    public (float baseLifespan, float maxLifespan) GetLemonLifespanRange()
    {
        // Find any prestige upgrade with LemonLifespan effect type that has a baseLifespan field
        var lemonLifespanUpgrade = availablePrestigeUpgradesData?.FirstOrDefault(
            data => data != null && data.effectType == PrestigeEffectType.LemonLifespan);
        
        float baseValue = lemonLifespanUpgrade != null ? lemonLifespanUpgrade.baseValueOverride : 0f;
        float maxValue = lemonLifespanUpgrade != null && lemonLifespanUpgrade.maxValueLimit > 0 
                        ? lemonLifespanUpgrade.maxValueLimit : float.MaxValue;
                    
        return (baseValue, maxValue);
    }

    // Keep a simpler version for backward compatibility
    public float GetBaseLemonLifespan()
    {
        return GetLemonLifespanRange().baseLifespan;
    }

    // --- Save/Load ---

    public void LoadData(SaveData saveData)
    {
        if (saveData == null)
        {
            Debug.LogWarning("[PrestigeManager] LoadData called with null saveData.");
            return;
        }

        Debug.Log("[PrestigeManager] Loading prestige data...");

        try
        {
            // Load Gold Bars (Parse from string)
            if (decimal.TryParse(saveData.goldBars, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal loadedGB))
            {
                goldBars = loadedGB;
                Debug.Log($"[PrestigeManager] Loaded Gold Bars: {goldBars}");
            }
            else
            {
                Debug.LogWarning($"[PrestigeManager] Could not parse Gold Bars value '{saveData.goldBars}'. Defaulting to 0.");
                goldBars = 0M;
            }

            // Load Prestige Count (already correct type)
            prestigeCount = saveData.prestigeCount;
            Debug.Log($"[PrestigeManager] Loaded Prestige Count: {prestigeCount}");

            // Check and Initialize upgrade states
            EnsureUpgradeStatesInitialized();

            // Reset all states to 0 before loading
            ResetAllUpgradeStateLevels();

            // Load Prestige Upgrade Levels (from List<UpgradeSaveData>)
            LoadPrestigeUpgradeLevels(saveData);

            // Apply effects after loading
            ApplyAllLoadedPrestigeEffects();

            // Fire events for loaded data
            OnGoldBarsChanged?.Invoke(goldBars);
            OnPrestigeCountChanged?.Invoke(prestigeCount);
            OnPrestigeDataLoaded?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PrestigeManager] Exception during LoadData: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
        }
        
        Debug.Log("[PrestigeManager] Finished loading prestige data.");
    }

    /// <summary>
    /// Ensures upgrade states dictionary is initialized
    /// </summary>
    private void EnsureUpgradeStatesInitialized()
    {
        if (playerPrestigeUpgradesState == null || availablePrestigeUpgradesData == null) 
        {
            InitializePrestigeUpgradeStates();
        }
        else
        {
            // Check if any new upgrades were added that need to be initialized
            foreach (var data in availablePrestigeUpgradesData)
            {
                if (data != null && !playerPrestigeUpgradesState.ContainsKey(data))
                {
                    Debug.Log($"[PrestigeManager] Adding newly detected upgrade: {data.name}");
                    playerPrestigeUpgradesState.Add(data, new UpgradeState(data) { level = 0 });
                }
            }
        }
    }

    /// <summary>
    /// Resets all upgrade state levels to 0
    /// </summary>
    private void ResetAllUpgradeStateLevels()
    {
        if (playerPrestigeUpgradesState == null) return;
        
        foreach (var state in playerPrestigeUpgradesState.Values)
        {
            if (state.level != 0)
            {
                state.level = 0;
            }
        }
    }

    /// <summary>
    /// Loads prestige upgrade levels from save data
    /// </summary>
    private void LoadPrestigeUpgradeLevels(SaveData saveData)
    {
        if (saveData.prestigeUpgradeLevels == null || saveData.prestigeUpgradeLevels.Count == 0)
        {
            Debug.Log("[PrestigeManager] No prestige upgrade levels found in save data.");
            return;
        }
        
        Debug.Log($"[PrestigeManager] Loading {saveData.prestigeUpgradeLevels.Count} prestige upgrades...");
        
        int loadedCount = 0;
        Dictionary<string, int> missingUpgrades = new Dictionary<string, int>();

        foreach (var savedUpgrade in saveData.prestigeUpgradeLevels)
        {
            if (string.IsNullOrEmpty(savedUpgrade.upgradeName))
            {
                Debug.LogWarning("[PrestigeManager] Skipping upgrade with null or empty name in save data.");
                continue;
            }
            
            // Try primary lookup by exact name
            PrestigeUpgradeData upgradeData = FindPrestigeUpgradeDataByName(savedUpgrade.upgradeName);
            
            // If not found by exact name, try more flexible matching
            if (upgradeData == null)
            {
                upgradeData = FindPrestigeUpgradeDataByFuzzyName(savedUpgrade.upgradeName);
            }

            if (upgradeData != null)
            {
                // Found the upgrade, apply the saved level
                if (playerPrestigeUpgradesState.TryGetValue(upgradeData, out UpgradeState state))
                {
                    state.level = savedUpgrade.level;
                    loadedCount++;
                    Debug.Log($"[PrestigeManager] Loaded upgrade: {upgradeData.name} at level {state.level}");
                }
                else
                {
                    Debug.LogWarning($"[PrestigeManager] Found upgrade data but no state for '{savedUpgrade.upgradeName}'");
                    missingUpgrades[savedUpgrade.upgradeName] = savedUpgrade.level;
                }
            }
            else
            {
                Debug.LogWarning($"[PrestigeManager] Could not find upgrade matching '{savedUpgrade.upgradeName}'");
                missingUpgrades[savedUpgrade.upgradeName] = savedUpgrade.level;
            }
        }
        
        Debug.Log($"[PrestigeManager] Successfully loaded {loadedCount} of {saveData.prestigeUpgradeLevels.Count} prestige upgrades.");
        
        // Report any missing upgrades
        if (missingUpgrades.Count > 0)
        {
            Debug.LogWarning($"[PrestigeManager] Missing {missingUpgrades.Count} prestige upgrades during load!");
        }
    }

    /// <summary>
    /// Tries to find an upgrade by exact name match
    /// </summary>
    public PrestigeUpgradeData FindPrestigeUpgradeDataByName(string name)
    {
        if (string.IsNullOrEmpty(name) || availablePrestigeUpgradesData == null)
        {
            return null;
        }
        
        // First try exact match by name property (standard object name)
        return availablePrestigeUpgradesData.FirstOrDefault(data => 
            data != null && data.name == name);
    }

    /// <summary>
    /// Tries to find an upgrade by more flexible name matching
    /// </summary>
    private PrestigeUpgradeData FindPrestigeUpgradeDataByFuzzyName(string name)
    {
        if (string.IsNullOrEmpty(name) || availablePrestigeUpgradesData == null)
        {
            return null;
        }
        
        // Try matching by upgradeName (display name)
        var match = availablePrestigeUpgradesData.FirstOrDefault(data => 
            data != null && data.upgradeName == name);
        
        if (match != null) return match;
        
        // Try case-insensitive name match (helps if files were renamed)
        return availablePrestigeUpgradesData.FirstOrDefault(data => 
            data != null && string.Equals(data.name, name, StringComparison.OrdinalIgnoreCase));
    }

    public void UpdateSaveData(SaveData saveData)
    {
        if (saveData == null) 
        {
            Debug.LogError("[PrestigeManager] UpdateSaveData called with null saveData!");
            return;
        }

        try
        {
            saveData.goldBars = goldBars.ToString(CultureInfo.InvariantCulture);
            saveData.prestigeCount = prestigeCount;

            // Save prestige upgrade levels (Using the standard UpgradeSaveData struct)
            saveData.prestigeUpgradeLevels = new List<UpgradeSaveData>();
            
            if (playerPrestigeUpgradesState == null)
            {
                Debug.LogError("[PrestigeManager] playerPrestigeUpgradesState is null during save!");
                EnsureUpgradeStatesInitialized(); // Try to recover by initializing
                
                if (playerPrestigeUpgradesState == null)
                {
                    return; // If still null, can't continue
                }
            }

            // Save all upgrades with levels > 0
            foreach (var kvp in playerPrestigeUpgradesState)
            {
                PrestigeUpgradeData data = kvp.Key;
                UpgradeState state = kvp.Value;
                
                if (data != null && state.level > 0)
                {
                    saveData.prestigeUpgradeLevels.Add(new UpgradeSaveData
                    {
                        upgradeName = data.name, // Use object name as ID
                        level = state.level
                    });
                }
            }
            
            Debug.Log($"[PrestigeManager] Saved {saveData.prestigeUpgradeLevels.Count} prestige upgrade levels.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PrestigeManager] Exception during UpdateSaveData: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private void ApplyAllLoadedPrestigeEffects()
    {
        if (playerPrestigeUpgradesState == null)
        {
            Debug.LogError(
                "ApplyAllLoadedPrestigeEffects called before playerPrestigeUpgradesState was initialized. This might indicate an issue with Awake execution order or duplicate managers."
            );
            return;
        }

        Debug.Log("Applying all loaded prestige effects...");
        List<UpgradeState> allPrestigeStates = playerPrestigeUpgradesState.Values.ToList();

        _clickUpgradeManager?.UpdatePermanentClickBonus(allPrestigeStates);
    }

    public void ResetData()
    {
        // Debug.Log("PrestigeManager: Resetting runtime data...");
        goldBars = 0M;
        prestigeCount = 0;

        foreach (var state in playerPrestigeUpgradesState.Values)
        {
            state.level = 0;
        }

        OnGoldBarsChanged?.Invoke(goldBars);
        OnPrestigeCountChanged?.Invoke(prestigeCount);

        CalculateAndNotifyPotentialGain();
        // Debug.Log("PrestigeManager: Runtime data reset.");
    }

    // Add these methods to get spawn time estimates without needing direct LemonManager reference
    public (float minTime, float maxTime) GetEstimatedLemonSpawnTimes()
    {
        // Base values from LemonManager
        float baseMinSpawnTime = 30f; // Default from LemonManager
        float baseMaxSpawnTime = 90f; // Default from LemonManager
        
        // Check if there's a spawn rate upgrade that overrides these values
        var spawnRateUpgrade = availablePrestigeUpgradesData?.FirstOrDefault(
            data => data != null && data.effectType == PrestigeEffectType.LemonSpawnRate);
        
        if (spawnRateUpgrade != null)
        {
            // Use overrides if specified
            if (spawnRateUpgrade.baseValueOverride > 0)
                baseMinSpawnTime = spawnRateUpgrade.baseValueOverride;
                
            // Max value could be specified in maxValueLimit
            if (spawnRateUpgrade.maxValueLimit > 0)
                baseMaxSpawnTime = spawnRateUpgrade.maxValueLimit;
        }
        
        // Get the total reduction from upgrades
        float reduction = GetTotalLemonSpawnTimeReduction();
        
        // Apply reduction and enforce minimum values
        float currentMinTime = Mathf.Max(10f, baseMinSpawnTime + reduction);
        float currentMaxTime = Mathf.Max(30f, baseMaxSpawnTime + reduction);
        
        // Ensure min never exceeds max
        if (currentMinTime > currentMaxTime)
        {
            currentMinTime = currentMaxTime - 1f;
        }
        
        return (currentMinTime, currentMaxTime);
    }
}
