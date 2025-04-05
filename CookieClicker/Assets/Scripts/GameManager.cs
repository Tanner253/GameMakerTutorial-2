using UnityEngine;
using System;

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

        // Initial calculation
        CalculateClickValue();
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

        OnScoreChanged?.Invoke(currentScore);
        OnClickValueChanged?.Invoke(CalculatedClickValue);
    }

    // Method to calculate the final click value (now returns decimal)
    public void CalculateClickValue()
    {
        // Start with the base value
        decimal finalValue = baseClickValue;

        // Apply additive bonuses
        finalValue += additiveClickBonus;

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

    // --- Upgrade System ---
    public bool TryPurchaseUpgrade(long cost)
    {
        if (currentScore >= cost)
        {
            currentScore -= cost;
            OnScoreChanged?.Invoke(currentScore);
            Debug.Log($"Purchased upgrade for {cost}. Score remaining: {currentScore:F1}");
            return true;
        }
        else
        {
            Debug.Log($"Not enough score ({currentScore:F1}) for upgrade costing {cost}.");
            return false;
        }
    }

    // Method specifically for upgrading the additive click bonus (now accepts decimal)
    public void ApplyAdditiveClickBonusUpgrade(decimal amountToAdd)
    {
        additiveClickBonus += amountToAdd;
        CalculateClickValue(); // Recalculate the click value immediately
        Debug.Log($"Additive Click Bonus increased by {amountToAdd:F1} to: {additiveClickBonus:F1}. New Calculated Click Value: {CalculatedClickValue:F1}");
    }

    // Example function for upgrades (adjust if needed for decimal)
    public void IncreaseBaseClickValue(decimal amount)
    {
        baseClickValue += amount;
        CalculateClickValue(); // Recalculate after upgrade
        Debug.Log($"Base Click Value increased to: {baseClickValue:F1}, Calculated: {CalculatedClickValue:F1}");
    }

    void Update()
    {

    }
} 