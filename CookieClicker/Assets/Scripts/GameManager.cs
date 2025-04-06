using UnityEngine;
using System;
using System.Collections.Generic; // Needed for Dictionary
using System.Linq;

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

    // Event for score changes (now uses decimal)
    public event Action<decimal> OnScoreChanged;
    // Event for click value changes (uses decimal)
    public event Action<decimal> OnClickValueChanged;

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

        InitializeClickUpgradeStates(); // Initialize click upgrade tracking
        CalculateClickValue(); // Initial calculation potentially including saved/loaded bonuses
    }

    void Start()
    {
        if (floatingTextManager == null)
        {
            floatingTextManager = FindFirstObjectByType<FloatingTextManager>();
            if (floatingTextManager == null)
            {
                Debug.LogError("GameManager could not find a FloatingTextManager in the scene!");
            }
        }

        OnScoreChanged?.Invoke(currentScore);
        OnClickValueChanged?.Invoke(CalculatedClickValue);
    }

    /// <summary>
    /// Initializes the dictionary tracking click upgrade levels.
    /// TODO: Load saved levels here in the future.
    /// </summary>
    void InitializeClickUpgradeStates()
    {
        playerClickUpgradesState = new Dictionary<ClickUpgradeData, UpgradeState>();
        foreach (var clickData in availableClickUpgradesData)
        {
            if (clickData != null && !playerClickUpgradesState.ContainsKey(clickData))
            {
                playerClickUpgradesState.Add(clickData, new UpgradeState(clickData));
            }
        }
        // After initializing/loading, recalculate the total bonus
        RecalculateTotalClickBonus();
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

    /// <summary>
    /// Processes a score addition from an automated source (e.g., auto-clicker)
    /// and displays floating text at the specified position.
    /// </summary>
    /// <param name="amount">The amount of score generated.</param>
    /// <param name="position">The Canvas position where the text should originate.</param>
    public void ProcessAutoClickerTick(decimal amount, Vector2 position)
    {
        if (amount <= 0) return;

        currentScore += amount;
        OnScoreChanged?.Invoke(currentScore); // Update UI

        // Show floating text using the manager
        if (floatingTextManager != null)
        {
            // You might want a different color for auto-clicks vs manual clicks
            // Example: Color.yellow
            // Reverting temporary change - use 3 arguments again
            floatingTextManager.ShowFloatingText(amount, position, Color.yellow); 
        }
        // Optionally add a less verbose Debug log compared to manual clicks
        // Debug.Log($"Auto-clicker added {amount:F1}. Score: {currentScore:F1}");
    }

    // Return decimal score
    public decimal GetCurrentScore()
    {
        return currentScore;
    }

    // Method for adding score generated passively (e.g., by production upgrades)
    public void AddPassiveScore(decimal amount)
    {
        if (amount <= 0)
        {
            return; // No need to process zero or negative amounts
        }
        currentScore += amount;
        // No floating text for passive income (can get spammy)
        // No debug log spam by default for passive income
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

    void Update()
    {

    }
} 