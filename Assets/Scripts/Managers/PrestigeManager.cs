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

    // --- Save/Load ---

    public void LoadData(SaveData saveData)
    {
        if (saveData == null)
        {
            Debug.LogWarning("[PrestigeManager] LoadData called with null saveData.");
            return;
        }

        // Load Gold Bars (Parse from string)
        if (decimal.TryParse(saveData.goldBars, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal loadedGB))
        {
            goldBars = loadedGB;
        }
        else
        {
             Debug.LogWarning($"[PrestigeManager] Could not parse Gold Bars value '{saveData.goldBars}'. Defaulting to 0.");
            goldBars = 0M;
        }

        // Load Prestige Count (already correct type)
        prestigeCount = saveData.prestigeCount;

        // Check and Initialization
        if (playerPrestigeUpgradesState == null) {
            // Debug.LogWarning("[PrestigeManager LoadData] playerPrestigeUpgradesState was null, re-initializing.");
            InitializePrestigeUpgradeStates();
        }

        // *** ADDED: Reset all runtime levels to 0 BEFORE loading saved levels ***
        // This ensures upgrades not present in the save data are reset correctly.
        bool stateChanged = false; // Track if reset actually changed anything
        foreach (var state in playerPrestigeUpgradesState.Values)
        {
            if (state.level != 0)
            {
                state.level = 0;
                stateChanged = true;
                // Potentially notify UI if needed for prestige upgrades on reset
                // OnPrestigeUpgradePurchased?.Invoke(state.upgradeDataRef, state.level); // Example: Careful with nulls
            }
        }
        // **************************************************************************

        // Load Prestige Upgrade Levels (from List<UpgradeSaveData>)
        if (saveData.prestigeUpgradeLevels != null)
        {
            Debug.Log($"[PrestigeManager.LoadData] Found {saveData.prestigeUpgradeLevels.Count} saved prestige upgrades. Processing...");
            foreach (var savedUpgrade in saveData.prestigeUpgradeLevels)
            {
                Debug.Log($"[PrestigeManager.LoadData] ... Loading Saved Name: {savedUpgrade.upgradeName}, Level: {savedUpgrade.level}");
                // Find the corresponding ScriptableObject using the saved name (consistent with other managers)
                PrestigeUpgradeData upgradeDataSO = FindPrestigeUpgradeDataByName(savedUpgrade.upgradeName);

                if (upgradeDataSO != null)
                {
                    // Find the state object in our runtime dictionary
                    if (playerPrestigeUpgradesState.TryGetValue(upgradeDataSO, out UpgradeState state))
                    {
                        state.level = savedUpgrade.level;
                         // Debug.Log($"[PrestigeManager LoadData] Loaded {upgradeDataSO.name} Level {state.level}");
                    }
                    else
                    {
                        Debug.LogWarning(
                            $"[PrestigeManager LoadData] Could not find runtime state for loaded upgrade name '{savedUpgrade.upgradeName}'. Was it added to availablePrestigeUpgradesData?"
                        );
                    }
                }
                else
                {
                    Debug.LogWarning(
                        $"[PrestigeManager LoadData] Could not find PrestigeUpgradeData asset with name '{savedUpgrade.upgradeName}' matching saved data. Skipping."
                    );
                }
            }
            Debug.Log($"[PrestigeManager] Processed {saveData.prestigeUpgradeLevels.Count} loaded prestige upgrade levels.");
        }
         else {
             Debug.Log("[PrestigeManager LoadData] No prestige upgrade levels found in save data.");
         }

        // Consider renaming or replacing with a more general OnLoadComplete event if needed.
        ApplyAllLoadedPrestigeEffects(); // Keep this for now, might need adjustment

        // If reset happened AND no saved data was loaded, we still need to apply effects (of level 0)
        if (stateChanged && (saveData.prestigeUpgradeLevels == null || saveData.prestigeUpgradeLevels.Count == 0))
        {
            ApplyAllLoadedPrestigeEffects(); // Apply effects after resetting to 0
        }

        OnPrestigeDataLoaded?.Invoke(); // NEW: Invoke the new event after LoadData completes
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

    public void UpdateSaveData(SaveData saveData)
    {
        if (saveData == null) return;

        saveData.goldBars = goldBars.ToString(CultureInfo.InvariantCulture);
        saveData.prestigeCount = prestigeCount;

        // Save prestige upgrade levels (Using the standard UpgradeSaveData struct)
        saveData.prestigeUpgradeLevels = new List<UpgradeSaveData>();
        if (playerPrestigeUpgradesState != null)
        {
            foreach (var kvp in playerPrestigeUpgradesState)
            {
                PrestigeUpgradeData data = kvp.Key;
                UpgradeState state = kvp.Value;
                if (data != null && state.level > 0) // Only save if level > 0
                {
                    // Use the ScriptableObject's name as a unique identifier for saving
                    saveData.prestigeUpgradeLevels.Add(new UpgradeSaveData
                    {
                        upgradeName = data.name, // Use SO name as ID (consistent with other managers)
                        level = state.level
                    });
                }
            }
             Debug.Log($"[PrestigeManager] Saved {saveData.prestigeUpgradeLevels.Count} prestige upgrade levels.");
        }
         else {
             Debug.LogWarning("[PrestigeManager] playerPrestigeUpgradesState is null during save.");
         }
    }

    // Helper to find PrestigeUpgradeData by its name (used during load)
    public PrestigeUpgradeData FindPrestigeUpgradeDataByName(string name)
    {
        // Ensure the available upgrades list is initialized
        if (availablePrestigeUpgradesData == null) {
            Debug.LogError("[PrestigeManager] availablePrestigeUpgradesData is null in FindPrestigeUpgradeDataByName!");
            return null;
        }
        return availablePrestigeUpgradesData.FirstOrDefault(data => data != null && data.name == name);
    }
}
