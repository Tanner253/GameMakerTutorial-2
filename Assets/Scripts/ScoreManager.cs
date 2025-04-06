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

    private const string ScoreKey = "CurrentScore";

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

    public void Initialize(decimal startingScore)
    {
        currentScore = startingScore;
        OnScoreChanged?.Invoke(currentScore); // Update UI with initial value
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

    // --- Save/Load ---
    public void SaveScore()
    {
        PlayerPrefs.SetString(ScoreKey, currentScore.ToString(CultureInfo.InvariantCulture));
    }

    public decimal LoadScore()
    {
        string savedScoreString = PlayerPrefs.GetString(ScoreKey, "0");
        if (
            decimal.TryParse(
                savedScoreString,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out decimal loadedScore
            )
        )
        {
            return loadedScore;
        }
        return 0.0M;
    }

    public void ResetScore()
    {
        currentScore = 0.0M;
        PlayerPrefs.DeleteKey(ScoreKey);
        OnScoreChanged?.Invoke(currentScore);
    }

    // --- UI Update ---
    void UpdateScoreDisplay(decimal newScore)
    {
        string formattedScore = FormatScore(newScore);
        if (balanceText != null)
        {
            balanceText.text = $"Balance: {formattedScore}";
        }
    }

    string FormatScore(decimal score)
    {
        // Basic formatting, can be expanded (e.g., K, M, B)
        // Consider creating a dedicated NumberFormatter utility class if formatting gets complex
        if (score >= 1000000000)
            return (score / 1000000000M).ToString("0.##B");
        if (score >= 1000000)
            return (score / 1000000M).ToString("0.##M");
        if (score >= 1000)
            return (score / 1000M).ToString("0.##K");
        return score.ToString("F0");
    }
}
