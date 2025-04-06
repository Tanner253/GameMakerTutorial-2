using UnityEngine;
using System;
using System.Collections.Generic; // Needed for Dictionary
using System.Linq;
using System.Globalization; // Needed for robust decimal conversion
using UnityEngine.SceneManagement; // Needed for scene reloading

public class GameManager : MonoBehaviour
{
    // Singleton pattern instance
    public static GameManager Instance { get; private set; }

    // Score uses decimal for high precision and range
    private decimal currentScore = 0.0M;

    // --- Click Value Calculation --- 
    // Use decimal for click values to handle fractional bonuses
    public decimal baseClickValue = 1.0M;
    public decimal additiveClickBonus = 0.0M; // Total bonus added from upgrades
    // public decimal multiplicativeClickBonus = 1.0M; // Example for future multipliers

    public decimal CalculatedClickValue { get; private set; } = 1.0M;

    // --- Global Scaling Factor ---
    [Header("Global Settings")]
    [Tooltip("Adjusts the cost scaling globally. > 1.0 makes upgrades more expensive (slower progression), < 1.0 makes them cheaper (faster progression).")]
    // This scale factor is intended to influence the cost calculation of *all* upgrades
    // that use a scaling formula (e.g., exponential cost increases).
    // It provides a single point to tune the overall game progression speed.
    // Ensure any new upgrade scripts incorporating cost scaling read this value from the GameManager
    // and include it in their final cost calculation (typically as a multiplier).
    public float globalProgressionScale = 1.0f; // Default to no scaling adjustment

    // --- Upgrade Management ---
    // Store runtime levels of CLICK upgrades (Production upgrades managed by ProductionManager)
    private Dictionary<ClickUpgradeData, UpgradeState> playerClickUpgradesState;
    // Event for when a click upgrade's state changes
    public event Action<UpgradeState> OnClickUpgradeStateChanged;
    // TEMP: Assign Click Upgrade Data assets here for initialization
    [Tooltip("Assign all available Click Upgrade ScriptableObjects here for initialization.")]
    public List<ClickUpgradeData> availableClickUpgradesData;

    // Reference to the FloatingTextManager
    public FloatingTextManager floatingTextManager;
    // Reference to the UI element displaying the score (Assign in Inspector)
    public RectTransform scoreDisplayRectTransform;

    // Event for score changes (now uses decimal)
    public event Action<decimal> OnScoreChanged;
    // Event for click value changes (uses decimal)
    public event Action<decimal> OnClickValueChanged;

    private const string ScoreKey = "CurrentScore";
    private const string ClickUpgradeLevelKeyPrefix = "ClickUpgradeLevel_";

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
            Destroy(gameObject); // Destroy duplicate instances
            return;
        }

        // Clamp global scale to prevent zero or negative values
        globalProgressionScale = Mathf.Max(0.1f, globalProgressionScale);

        InitializeClickUpgradeStates(); // Initialize click upgrade structures FIRST
        LoadGame(); // Load saved progress AFTER initialization but before calculations/UI updates
        // CalculateClickValue(); // Recalculate bonuses based potentially on loaded levels - This is now called within LoadGame or Initialize
    }

    void Start()
    {
        if (floatingTextManager == null)
        {
            floatingTextManager = FindObjectOfType<FloatingTextManager>();
            if (floatingTextManager == null)
            {
                Debug.LogError("GameManager could not find a FloatingTextManager in the scene!");
            }
        }

        // Initial UI updates should happen after loading
        OnScoreChanged?.Invoke(currentScore);
        OnClickValueChanged?.Invoke(CalculatedClickValue);
    }

    /// <summary>
    /// Initializes the dictionary tracking click upgrade levels and loads saved levels.
    /// </summary>
    void InitializeClickUpgradeStates()
    {
        Debug.Log("InitializeClickUpgradeStates: Starting...");
        playerClickUpgradesState = new Dictionary<ClickUpgradeData, UpgradeState>();
        foreach (var clickData in availableClickUpgradesData)
        {
            if (clickData != null && !playerClickUpgradesState.ContainsKey(clickData))
            {
                // Create the state object
                UpgradeState newState = new UpgradeState(clickData);

                // Load the saved level for this upgrade
                string key = ClickUpgradeLevelKeyPrefix + clickData.name; // Use upgrade name as part of the key
                // Use -1 default in log to clearly distinguish from a valid saved 0
                int loadedLevel = PlayerPrefs.GetInt(key, 0); 
                Debug.Log($"InitializeClickUpgradeStates: Loading level for {clickData.name}. Key: {key}, Value Read: {PlayerPrefs.GetInt(key, -1)}, Using Level: {loadedLevel}"); 
                newState.level = loadedLevel;

                playerClickUpgradesState.Add(clickData, newState);
            }
        }
        Debug.Log("InitializeClickUpgradeStates: Finished loading levels. Recalculating bonus...");
        // After initializing AND loading levels, recalculate the total bonus
        RecalculateTotalClickBonus(); // This also calls CalculateClickValue
    }

    /// <summary>
    /// Recalculates the total additive click bonus based on current levels of all click upgrades.
    /// </summary>
    void RecalculateTotalClickBonus()
    {
        additiveClickBonus = 0M;
        foreach(var kvp in playerClickUpgradesState)
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
        CalculateClickValue();
    }

    // Method to calculate the final click value (now returns decimal)
    public void CalculateClickValue()
    {
        // Start with the base value
        decimal finalValue = baseClickValue + additiveClickBonus;

        // Apply multiplicative bonuses (example)
        // finalValue = finalValue * (decimal)multiplicativeClickBonus;

        // Ensure click value doesn't go below a minimum (e.g., 0.1 or 1.0 depending on design)
        CalculatedClickValue = Math.Max(0.1M, finalValue); // Use M suffix for decimal literal

        OnClickValueChanged?.Invoke(CalculatedClickValue);
    }

    // Method called when a click occurs
    public void ProcessClick(Vector2 clickPosition)
    {
        // Increase score - cast the calculated click value to decimal for score addition
        currentScore += CalculatedClickValue;
        Debug.Log($"Score increased by {CalculatedClickValue:F1} to: {currentScore:F1}");

        if (floatingTextManager != null)
        {
            // Pass the precise decimal value to the text manager
            floatingTextManager.ShowFloatingText(CalculatedClickValue, clickPosition);
        }

        OnScoreChanged?.Invoke(currentScore);
    }

    // Return decimal score
    public decimal GetCurrentScore()
    {
        return currentScore;
    }

    // Method for adding score generated passively (e.g., by production upgrades)
    // This version ONLY adds score, no visual feedback.
    public void AddPassiveScore(decimal amount)
    {
        if (amount <= 0) return;
        currentScore += amount;
        /* // Display floating text for passive income near the score display <-- REMOVED FROM HERE
        if (floatingTextManager != null && scoreDisplayRectTransform != null)
        {
            // Use the amount generated this tick and the score display's position
            floatingTextManager.ShowFloatingText(amount, scoreDisplayRectTransform.anchoredPosition);
        }*/
        // No debug log spam by default for passive income
        OnScoreChanged?.Invoke(currentScore); // Update UI
    }

    // New method specifically for adding passive score AND showing colored feedback
    public void AddPassiveScoreAndShowFeedback(decimal amount, Color feedbackColor)
    {
        if (amount <= 0) return; // No need to process zero or negative amounts

        // 1. Add the score
        currentScore += amount;

        // 2. Show the floating text with the specified color
        if (floatingTextManager != null && scoreDisplayRectTransform != null)
        {
            floatingTextManager.ShowFloatingText(amount, scoreDisplayRectTransform.anchoredPosition, feedbackColor);
        }
        else
        {
            if (floatingTextManager == null) Debug.LogWarning("FloatingTextManager not found for passive feedback.");
            if (scoreDisplayRectTransform == null) Debug.LogWarning("ScoreDisplayRectTransform not assigned in GameManager for passive feedback.");
        }

        // 3. Update the score display UI
        OnScoreChanged?.Invoke(currentScore); // Update UI
    }

    /// <summary>
    /// Central method for attempting to purchase anything with score.
    /// Now accepts decimal cost directly.
    /// </summary>
    /// <param name="cost">The cost of the purchase.</param>
    /// <returns>True if purchase is successful, false otherwise.</returns>
    public bool TryPurchaseUpgrade(decimal cost)
    {
        if (currentScore >= cost)
        {
            currentScore -= cost;
            OnScoreChanged?.Invoke(currentScore);
            Debug.Log($"Purchase successful for {cost:F0}. Score remaining: {currentScore:F1}");
            return true;
        }
        else
        {
            // Keep this log less prominent or conditional if spammy
            // Debug.Log($"Not enough score ({currentScore:F1}) for purchase costing {cost:F0}.");
            return false;
        }
    }

    /// <summary>
    /// Gets the current runtime state (level) of a specific click upgrade.
    /// </summary>
    public UpgradeState GetClickUpgradeState(ClickUpgradeData data)
    {
        playerClickUpgradesState.TryGetValue(data, out UpgradeState state);
        // If state is null (e.g., data added after initialization), create it
        if (state == null && data != null)
        {
            Debug.LogWarning($"State for {data.name} not found, creating now. Ensure it's in availableClickUpgradesData list.");
            state = new UpgradeState(data);
            playerClickUpgradesState.Add(data, state);
        }
        return state;
    }

    /// <summary>
    /// Calculates the cost for the next level of a given click upgrade.
    /// </summary>
    public decimal CalculateClickUpgradeCost(ClickUpgradeData data, int currentLevel)
    {
        if (currentLevel < 0) currentLevel = 0;
        decimal globalScale = (decimal)globalProgressionScale;
        // Cast float baseCost to decimal
        decimal cost = (decimal)data.baseCost * (decimal)Mathf.Pow(data.costScaleFactor, currentLevel) * globalScale;
        return Math.Ceiling(cost);
    }

    /// <summary>
    /// Called when a click upgrade is successfully purchased to apply its effect and update state.
    /// </summary>
    public void ApplyClickUpgrade(ClickUpgradeData data)
    {
        UpgradeState state = GetClickUpgradeState(data);
        if (state != null)
        {
            state.level++;
            RecalculateTotalClickBonus(); // Recalculate bonus which calls CalculateClickValue
            OnClickUpgradeStateChanged?.Invoke(state); // Notify UI
            Debug.Log($"Applied click upgrade '{data.upgradeName}' level {state.level}. New total bonus: {additiveClickBonus:F1}, New click value: {CalculatedClickValue:F1}");
        }
        else
        {
             Debug.LogError($"Could not find or create state for ClickUpgradeData: {data.name}");
        }
    }

    /// <summary>
    /// Saves the current game state to PlayerPrefs.
    /// </summary>
    void SaveGame()
    {
        // Save Score (as string for decimal)
        PlayerPrefs.SetString(ScoreKey, currentScore.ToString(CultureInfo.InvariantCulture));

        // Save Click Upgrade Levels
        foreach (var kvp in playerClickUpgradesState)
        {
            if (kvp.Key != null)
            {
                string key = ClickUpgradeLevelKeyPrefix + kvp.Key.name;
                PlayerPrefs.SetInt(key, kvp.Value.level);
            }
        }

        // Save Production Upgrade Levels (delegated)
        if (ProductionManager.Instance != null)
        {
            ProductionManager.Instance.SaveProductionUpgrades();
        }

        PlayerPrefs.Save(); // Explicitly save changes
        Debug.Log("Game state saved.");
    }

    /// <summary>
    /// Loads the game state from PlayerPrefs. Called after initialization.
    /// </summary>
    void LoadGame()
    {
        Debug.Log("LoadGame: Starting...");
        // Load Score
        // Use "NULL" default in log to clearly see if key exists
        string savedScoreString = PlayerPrefs.GetString(ScoreKey, "0");
        Debug.Log($"LoadGame: Loading score. Key: {ScoreKey}, Value Read: {PlayerPrefs.GetString(ScoreKey, "NULL")}"); 
        if (decimal.TryParse(savedScoreString, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal loadedScore))
        {
            currentScore = loadedScore;
        }
        else
        {
            currentScore = 0.0M;
            Debug.LogWarning($"LoadGame: Failed to parse saved score: {savedScoreString}. Resetting score to 0.");
        }

        // Click Upgrade Levels are loaded in InitializeClickUpgradeStates
        // Production Upgrade Levels are loaded in ProductionManager.InitializePlayerUpgrades

        // Important: Recalculate dependent values after loading score and after upgrades have loaded their levels
        RecalculateTotalClickBonus(); // Recalculates additive bonus based on loaded levels
        // CalculateClickValue(); // This is called by RecalculateTotalClickBonus

        Debug.Log($"LoadGame: Finished. Score: {currentScore:F1}. Upgrade levels loaded separately.");
        // UI updates will happen in Start or via OnScoreChanged/OnClickValueChanged events triggered by recalculations.
    }

    // --- Application Lifecycle Methods for Saving ---

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveGame();
        }
    }

    void OnApplicationQuit()
    {
        SaveGame();
    }

    /// <summary>
    /// Deletes all saved PlayerPrefs data and reloads the current scene.
    /// </summary>
    public void ResetGameData()
    {
        Debug.Log("--- ResetGameData method called! ---");
        Debug.Log("ResetGameData: Resetting runtime state...");

        // 1. Reset runtime values
        currentScore = 0M;
        additiveClickBonus = 0M;
        CalculatedClickValue = baseClickValue; // Reset to base

        // Re-initialize click upgrade dictionary with level 0
        playerClickUpgradesState.Clear();
        foreach (var clickData in availableClickUpgradesData)
        {
            if (clickData != null)
            {
                playerClickUpgradesState.Add(clickData, new UpgradeState(clickData) { level = 0 });
            }
        }
        // Trigger immediate updates for any listeners (though scene reload will also do this)
        OnScoreChanged?.Invoke(currentScore);
        OnClickValueChanged?.Invoke(CalculatedClickValue); 
        // Notify UI about all click upgrades being reset (important for UI listening to this)
        foreach(var state in playerClickUpgradesState.Values)
        {
            OnClickUpgradeStateChanged?.Invoke(state);
        }

        // Reset production upgrades (assuming ProductionManager might also persist or needs explicit reset call)
        // If ProductionManager reloads cleanly with the scene, this might be redundant, but safer to include.
        if (ProductionManager.Instance != null)
        {   
            ProductionManager.Instance.ResetProductionUpgrades(); // We'll need to add this method
        }

        Debug.Log("ResetGameData: Deleting PlayerPrefs...");
        PlayerPrefs.DeleteAll(); // 2. Delete saved persistent data
        PlayerPrefs.Save();
        Debug.Log("ResetGameData: PlayerPrefs deleted and saved. Reloading scene...");

        // 3. Reload the scene to reset everything else
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    void Update()
    {

    }
} 