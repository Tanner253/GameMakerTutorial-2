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

    // --- Runtime State ---
    private decimal goldBars = 0M;
    private int prestigeCount = 0;
    private Dictionary<PrestigeUpgradeData, UpgradeState> playerPrestigeUpgradesState;

    // --- Events ---
    public event Action<decimal> OnGoldBarsChanged; // Fired when GB balance changes
    public event Action<int> OnPrestigeCountChanged; // Fired when prestige count changes
    public event Action<PrestigeUpgradeData, int> OnPrestigeUpgradePurchased; // Fired when a prestige upgrade level changes
    public event Action<decimal> OnPotentialPrestigeGainCalculated; // Fired with potential GB gain

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
        if (data == null)
            return decimal.MaxValue;
        if (currentLevel < 0)
            currentLevel = 0;
        double cost = data.baseCostGoldBars * Math.Pow(data.costScaleFactor, currentLevel);
        return (decimal)Math.Ceiling(cost);
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

        ApplyPrestigeUpgradeEffect(data, state.level);

        _saveLoadManager?.SaveGameData();
    }

    private void ApplyPrestigeUpgradeEffect(PrestigeUpgradeData data, int newLevel)
    {
        Debug.Log($"Applying effect for {data.upgradeName} Level {newLevel}");
        if (data.clickBonusPercentPerLevel > 0 && _clickUpgradeManager != null)
        {
            List<UpgradeState> allPrestigeStates = playerPrestigeUpgradesState.Values.ToList();
            _clickUpgradeManager.UpdatePermanentClickBonus(allPrestigeStates);
        }
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

        // Load Prestige Upgrade Levels (from List<PrestigeUpgradeSaveData>)
        if (saveData.prestigeUpgradeLevels != null)
        {
            var availableDataLookup = availablePrestigeUpgradesData
                .Where(d => d != null)
                .ToDictionary(d => d.name);

            // Iterate through the saved list
            foreach (var savedUpgradeData in saveData.prestigeUpgradeLevels)
            {
                // Find the corresponding ScriptableObject
                if (
                    availableDataLookup.TryGetValue(
                        savedUpgradeData.upgradeName, // Use name from saved struct
                        out PrestigeUpgradeData dataSO
                    )
                )
                {
                    // Find the state in the manager's dictionary
                    if (playerPrestigeUpgradesState.TryGetValue(dataSO, out UpgradeState state))
                    {
                        state.level = savedUpgradeData.level; // Set the level from saved struct
                    }
                    // else: The state wasn't initialized properly, warning already given if dictionary was null.
                }
                else
                {
                    Debug.LogWarning(
                        $"PrestigeManager: ScriptableObject named '{savedUpgradeData.upgradeName}' not found in available data for saved prestige upgrade. Skipping."
                    );
                }
            }
            // Apply effects after loading all levels
            ApplyAllLoadedPrestigeEffects();
        }
        // else: No saved prestige upgrades.
        OnGoldBarsChanged?.Invoke(goldBars);
        OnPrestigeCountChanged?.Invoke(prestigeCount);
        Debug.Log("PrestigeManager: Prestige data loaded.");
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

        ApplyAllLoadedPrestigeEffects();

        CalculateAndNotifyPotentialGain();
        // Debug.Log("PrestigeManager: Runtime data reset.");
    }

    public void UpdateSaveData(SaveData saveData)
    {
        if (saveData == null) return;

        // Convert goldBars (decimal) to string for saving
        saveData.goldBars = this.goldBars.ToString(CultureInfo.InvariantCulture);
        saveData.prestigeCount = this.prestigeCount;

        // Initialize the list in SaveData
        saveData.prestigeUpgradeLevels = new List<PrestigeUpgradeSaveData>();

        // Populate the list from the manager's state dictionary
        foreach (var kvp in playerPrestigeUpgradesState)
        {
            if (kvp.Value.level > 0)
            {
                // Create a PrestigeUpgradeSaveData struct and add it to the list
                saveData.prestigeUpgradeLevels.Add(new PrestigeUpgradeSaveData
                {
                    upgradeName = kvp.Key.name, // Use the ScriptableObject's asset name
                    level = kvp.Value.level
                });
            }
        }
    }
}
