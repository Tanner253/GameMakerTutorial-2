using System;
using System.Globalization; // Needed for robust decimal conversion
using TMPro;
using UnityEngine;

// Manages the player's score (balance)
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("UI References")]
    public TextMeshProUGUI balanceText;
    public FloatingTextManager floatingTextManager; // Optional: For showing deductions/additions directly
    public RectTransform scoreDisplayRectTransform; // Needed for floating text positioning

    private decimal currentScore = 0.0M;
    public event Action<decimal> OnScoreChanged;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // Optional: If needs to persist across scenes independently
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Subscribe to own event to update UI
        OnScoreChanged += UpdateScoreDisplay;
    }

    void OnDestroy()
    {
        OnScoreChanged -= UpdateScoreDisplay;
    }

    /// <summary>
    /// Loads the score from the provided SaveData object.
    /// If SaveData is null or score parsing fails, initializes to 0.
    /// </summary>
    public void LoadData(SaveData saveData)
    {
        decimal loadedScore = 0.0M;
        if (saveData != null)
        {
            // Try parse the score string using InvariantCulture for consistency
            if (decimal.TryParse(saveData.currentScore, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsedScore))
            {
                loadedScore = parsedScore;
                // Debug.Log($"ScoreManager: Loaded score {loadedScore} from SaveData.");
            }
            else
            {
                 Debug.LogWarning($"ScoreManager: Could not parse score '{saveData.currentScore}' from SaveData. Defaulting to 0.");
            }
        }
        else
        {
             Debug.Log("ScoreManager: No SaveData provided, initializing score to 0.");
        }

        currentScore = loadedScore;
        OnScoreChanged?.Invoke(currentScore); // Update UI with initial/loaded value
    }

    public decimal GetCurrentScore()
    {
        return currentScore;
    }

    // Add score without specific feedback
    public void AddScore(decimal amount)
    {
        if (amount <= 0)
            return;
        currentScore += amount;
        OnScoreChanged?.Invoke(currentScore);
    }

    // Add score and show floating text feedback
    public void AddScoreAndShowFeedback(decimal amount, Color feedbackColor)
    {
        if (amount <= 0)
            return;
        currentScore += amount;

        if (floatingTextManager != null && scoreDisplayRectTransform != null)
        {
            floatingTextManager.ShowFloatingText(
                amount,
                scoreDisplayRectTransform.anchoredPosition,
                feedbackColor
            );
        }
        else
        {
            Debug.LogWarning(
                "FloatingTextManager or ScoreDisplayRectTransform not set in ScoreManager."
            );
        }

        OnScoreChanged?.Invoke(currentScore);
    }

    // Try to spend score
    public bool TrySpendScore(decimal cost)
    {
        if (currentScore >= cost)
        {
            currentScore -= cost;
            OnScoreChanged?.Invoke(currentScore);
            return true;
        }
        return false;
    }

    // --- Save/Load (Refactored) ---

    // NEW: Returns the current score as a string for serialization.
    public string GetData()
    {
        return currentScore.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Resets the current score to 0 in memory.
    /// Does not affect saved files directly (SaveLoadManager handles file deletion).
    /// </summary>
    public void ResetData()
    {
        currentScore = 0.0M;
        // REMOVED: PlayerPrefs.DeleteKey(ScoreKey);
        OnScoreChanged?.Invoke(currentScore); // Update UI
        Debug.Log("ScoreManager: Runtime score reset to 0.");
    }

    // --- UI Update ---
    void UpdateScoreDisplay(decimal newScore)
    {
        string formattedScore = NumberFormatter.FormatNumber(newScore);
        if (balanceText != null)
        {
            balanceText.text = $"Balance: {formattedScore}";
        }
    }
}
