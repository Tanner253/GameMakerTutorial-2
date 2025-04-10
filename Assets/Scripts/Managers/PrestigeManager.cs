using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using UnityEngine.SceneManagement;

public class PrestigeManager : MonoBehaviour
{
    public static PrestigeManager Instance { get; private set; }

    [Header("Configuration")]
    [Tooltip("The scaling factor used in the prestige calculation (Score / ScaleFactor)^(1/3)")]
    public double prestigeScaleFactor = 1_000_000_000.0; // 1 Billion default
    [Tooltip("Minimum lifetime score required to be eligible for prestige.")]
    public decimal minScoreToPrestige = 1_000_000_000.0M; // 1 Billion default

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
    private decimal totalLifetimeScoreEarned = 0M;
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

        InitializePrestigeUpgradeStates();
    }

    public void InitializeManagers(ScoreManager sm, ClickUpgradeManager cum, ProductionManager pm, SaveLoadManager slm)
    {
        _scoreManager = sm;
        _clickUpgradeManager = cum;
        _productionManager = pm;
        _saveLoadManager = slm;

        // Subscribe to score changes to update lifetime score
        if (_scoreManager != null)
        {
            _scoreManager.OnScoreChanged += UpdateLifetimeScore;
        }
    }

    void OnDestroy()
    {
        if (_scoreManager != null)
        {
            _scoreManager.OnScoreChanged -= UpdateLifetimeScore;
        }
    }

    void UpdateLifetimeScore(decimal currentScore)
    {
        // This logic assumes score only goes up or resets to 0.
        // If score could decrease in other ways, this needs adjustment.
        if (currentScore > totalLifetimeScoreEarned)
        {
            totalLifetimeScoreEarned = currentScore;
        }
        // Periodically calculate and notify potential gain
        if (Time.frameCount % 60 == 0) // Check roughly once per second
        {
            CalculateAndNotifyPotentialGain();
        }
    }

    void InitializePrestigeUpgradeStates()
    {
        playerPrestigeUpgradesState = new Dictionary<PrestigeUpgradeData, UpgradeState>();
        if (availablePrestigeUpgradesData == null) availablePrestigeUpgradesData = new List<PrestigeUpgradeData>();

        foreach (var prestigeData in availablePrestigeUpgradesData)
        {
            if (prestigeData != null && !playerPrestigeUpgradesState.ContainsKey(prestigeData))
            {
                UpgradeState newState = new UpgradeState(prestigeData) { level = 0 };
                playerPrestigeUpgradesState.Add(prestigeData, newState);
            }
        }
    }

    public bool CanAffordPrestige()
    {
        // Use current score or lifetime score depending on design - using lifetime here
        return totalLifetimeScoreEarned >= minScoreToPrestige;
    }

    public decimal CalculatePotentialGoldBarGain()
    {
        if (!CanAffordPrestige()) return 0M;

        // Using double for intermediate calculation due to Math.Pow
        double scoreDouble = (double)totalLifetimeScoreEarned;
        double calculatedGain = Math.Pow(scoreDouble / prestigeScaleFactor, 1.0 / 3.0);

        // Floor the result and convert back to decimal
        decimal gain = (decimal)Math.Floor(calculatedGain);
        return Math.Max(0M, gain); // Ensure non-negative
    }

    void CalculateAndNotifyPotentialGain()
    {
        decimal potentialGain = CalculatePotentialGoldBarGain();
        OnPotentialPrestigeGainCalculated?.Invoke(potentialGain);
    }

    public void PerformPrestige()
    {
        if (!CanAffordPrestige()) {
             Debug.LogWarning("Attempted to prestige but conditions not met.");
             return;
        }

        Debug.Log("--- Performing Prestige --- ");

        // 1. Calculate and Award Gold Bars
        decimal earnedGB = CalculatePotentialGoldBarGain();
        goldBars += earnedGB;
        Debug.Log($"Earned {earnedGB} Gold Bars. Total GB: {goldBars}");
        OnGoldBarsChanged?.Invoke(goldBars);

        // 2. Increment Prestige Count
        prestigeCount++;
        Debug.Log($"Prestige Count: {prestigeCount}");
        OnPrestigeCountChanged?.Invoke(prestigeCount);

        // 3. Reset Relevant Game State (Delegate to Managers)
        _scoreManager?.ResetData(); // Resets current score, lifetime score remains
        _clickUpgradeManager?.ResetData();
        _productionManager?.ResetData();
        // Reset lifetime score *after* calculating prestige
        totalLifetimeScoreEarned = 0M;

        // 4. Immediately Save Game State after Prestige
        _saveLoadManager?.SaveGameData();

        Debug.Log("--- Prestige Complete --- ");

        // 5. Load the Prestige Scene (REMOVED - Navigation handled by UI)
        // if (!string.IsNullOrEmpty(prestigeSceneName))
        // {
        //     Debug.Log($"Loading prestige scene: {prestigeSceneName}");
        //     SceneManager.LoadScene(prestigeSceneName);
        // }
        // else
        // {
        //     Debug.LogWarning("Prestige scene name is not set in PrestigeManager. Cannot load scene.");
        //      // Re-calculate potential gain if not changing scene
        //      CalculateAndNotifyPotentialGain(); // Call this if staying in scene
        // }
        
        // Re-calculate potential gain (should be 0 now)
        // This should happen automatically if UI is subscribed to score/prestige events
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
        if (data == null) return decimal.MaxValue;
        if (currentLevel < 0) currentLevel = 0;
        // Using double for intermediate Math.Pow, converting base cost
        double cost = data.baseCostGoldBars * Math.Pow(data.costScaleFactor, currentLevel);
        return (decimal)Math.Ceiling(cost);
    }

    public bool CanAffordPrestigeUpgrade(PrestigeUpgradeData data)
    {
        UpgradeState state = GetPrestigeUpgradeState(data);
        if (state == null) return false;
        if (data.isUniqueUnlock && state.level > 0) return false; // Cannot buy unique upgrades more than once
        if (prestigeCount < data.requiredPrestigeLevel) return false; // Check prestige level requirement

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

        // Deduct cost
        goldBars -= cost;
        OnGoldBarsChanged?.Invoke(goldBars);

        // Increment level
        state.level++;
        OnPrestigeUpgradePurchased?.Invoke(data, state.level);
        Debug.Log($"Purchased {data.upgradeName} Level {state.level} for {cost} GB. Remaining GB: {goldBars}");

        // Apply the effect immediately (more complex effects might need separate manager)
        ApplyPrestigeUpgradeEffect(data, state.level);

        // Save after purchase
        _saveLoadManager?.SaveGameData();
    }

    // Placeholder for applying effects - This will need expansion!
    private void ApplyPrestigeUpgradeEffect(PrestigeUpgradeData data, int newLevel)
    {
        Debug.Log($"Applying effect for {data.upgradeName} Level {newLevel}");
        // Example: If it's a click bonus upgrade, notify ClickUpgradeManager
        if (data.clickBonusPercentPerLevel > 0 && _clickUpgradeManager != null)
        {
            // Get all current prestige states to calculate the total bonus
            List<UpgradeState> allPrestigeStates = playerPrestigeUpgradesState.Values.ToList();
            _clickUpgradeManager.UpdatePermanentClickBonus(allPrestigeStates);
        }

        // Add logic for other effect types (production, mint, etc.)
        // Example for Production:
        // if (data.productionBonusPercentPerLevel > 0 && _productionManager != null)
        // {
        //     List<UpgradeState> allPrestigeStates = playerPrestigeUpgradesState.Values.ToList();
        //     _productionManager.UpdatePermanentProductionBonus(allPrestigeStates); // Needs implementing in ProductionManager
        // }
    }


    // --- Save/Load --- 

    public void LoadData(SaveData saveData)
    {
        if (saveData == null) {
             Debug.Log("PrestigeManager: No save data found, using defaults.");
            goldBars = 0M;
            prestigeCount = 0;
            totalLifetimeScoreEarned = 0M; // Also reset lifetime score if no save data
             // Ensure default state is level 0 (already done in InitializePrestigeUpgradeStates)
             foreach(var state in playerPrestigeUpgradesState.Values) state.level = 0;
             OnGoldBarsChanged?.Invoke(goldBars);
             OnPrestigeCountChanged?.Invoke(prestigeCount);
            return;
        }

        Debug.Log("PrestigeManager: Loading prestige data...");
        // Load Gold Bars
        if (decimal.TryParse(saveData.goldBars, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal loadedGB))
        {
            goldBars = loadedGB;
        }
        else
        {
            Debug.LogWarning($"Could not parse Gold Bars value '{saveData.goldBars}'. Defaulting to 0.");
            goldBars = 0M;
        }

        // Load Prestige Count
        prestigeCount = saveData.prestigeCount;

        // Load Total Lifetime Score
        if (decimal.TryParse(saveData.totalLifetimeScoreEarned, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal loadedLifetimeScore))
        {
            totalLifetimeScoreEarned = loadedLifetimeScore;
        }
        else
        {
             Debug.LogWarning($"Could not parse Total Lifetime Score '{saveData.totalLifetimeScoreEarned}'. Defaulting to 0.");
             totalLifetimeScoreEarned = 0M;
        }

        // Load Prestige Upgrade Levels
        if (saveData.prestigeUpgradeLevels != null)
        {
             // Create a lookup from the available data list for efficiency
            var availableDataLookup = availablePrestigeUpgradesData.Where(d => d != null).ToDictionary(d => d.name);

            foreach (var savedUpgrade in saveData.prestigeUpgradeLevels)
            {
                 if (availableDataLookup.TryGetValue(savedUpgrade.upgradeName, out PrestigeUpgradeData dataSO))
                 {
                     if (playerPrestigeUpgradesState.TryGetValue(dataSO, out UpgradeState state))
                     {
                         state.level = savedUpgrade.level;
                         // DO NOT apply effects here one by one anymore
                         // ApplyPrestigeUpgradeEffect(dataSO, state.level); // REMOVED
                     }
                 }
                 else {
                     Debug.LogWarning($"PrestigeManager: ScriptableObject named '{savedUpgrade.upgradeName}' not found for saved prestige upgrade. Skipping.");
                 }
            }
            // Apply ALL cumulative prestige effects *after* loading all levels
            ApplyAllLoadedPrestigeEffects(); // NEW CALL
        }
        OnGoldBarsChanged?.Invoke(goldBars);
        OnPrestigeCountChanged?.Invoke(prestigeCount);
        Debug.Log("PrestigeManager: Prestige data loaded.");
    }

    // NEW: Helper to apply cumulative effects after loading
    private void ApplyAllLoadedPrestigeEffects()
    {
        // NEW: Add null check for the dictionary
        if (playerPrestigeUpgradesState == null)
        {
             Debug.LogError("ApplyAllLoadedPrestigeEffects called before playerPrestigeUpgradesState was initialized. This might indicate an issue with Awake execution order or duplicate managers.");
             return;
        }

        Debug.Log("Applying all loaded prestige effects...");
        List<UpgradeState> allPrestigeStates = playerPrestigeUpgradesState.Values.ToList();

        // Apply Click Bonus
        _clickUpgradeManager?.UpdatePermanentClickBonus(allPrestigeStates);

        // Apply Production Bonus (placeholder call)
        // _productionManager?.UpdatePermanentProductionBonus(allPrestigeStates);

        // Apply other bonuses...
    }

    // NEW: Resets runtime prestige data to defaults
    public void ResetData()
    {
        Debug.Log("PrestigeManager: Resetting runtime data...");
        goldBars = 0M;
        prestigeCount = 0;
        totalLifetimeScoreEarned = 0M; // Reset lifetime score as well for a hard reset

        foreach(var state in playerPrestigeUpgradesState.Values)
        {
            state.level = 0;
        }

        // Notify listeners about the reset state
        OnGoldBarsChanged?.Invoke(goldBars);
        OnPrestigeCountChanged?.Invoke(prestigeCount);

        // Apply effects based on the reset state (likely resulting in no bonuses)
        ApplyAllLoadedPrestigeEffects();

         // Recalculate potential gain (should be 0)
        CalculateAndNotifyPotentialGain();
        Debug.Log("PrestigeManager: Runtime data reset.");
    }

    public void UpdateSaveData(SaveData saveData)
    {
        if (saveData == null) return;

        saveData.goldBars = goldBars.ToString(CultureInfo.InvariantCulture);
        saveData.prestigeCount = prestigeCount;
        saveData.totalLifetimeScoreEarned = totalLifetimeScoreEarned.ToString(CultureInfo.InvariantCulture);

        saveData.prestigeUpgradeLevels = new List<PrestigeUpgradeSaveData>();
        foreach (var kvp in playerPrestigeUpgradesState)
        {
            if (kvp.Value.level > 0)
            {
                saveData.prestigeUpgradeLevels.Add(new PrestigeUpgradeSaveData
                {
                    upgradeName = kvp.Key.name,
                    level = kvp.Value.level
                });
            }
        }
    }
} 