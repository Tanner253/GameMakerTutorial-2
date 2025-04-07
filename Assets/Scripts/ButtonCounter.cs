using System;
using System.Collections; // Needed for Coroutines
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonCounter : MonoBehaviour, IPointerClickHandler
{
    // References for UI and Animation
    public TextMeshProUGUI counterText;
    public Button clickButton; // Optional: Can be removed if IPointerClickHandler is sufficient
    public Animator coinAnimator;

    private RectTransform rectTransform; // Store RectTransform for position

    // Define constants for number thresholds (using decimal for precision)
    private static readonly decimal CryptonThreshold = 1_000_000_000_000_000_000_000_000m;
    private static readonly decimal SextillionThreshold = 1_000_000_000_000_000_000_000m;
    private static readonly decimal QuintillionThreshold = 1_000_000_000_000_000_000m;
    private static readonly decimal QuadrillionThreshold = 1_000_000_000_000_000m;
    private static readonly decimal TrillionThreshold = 1_000_000_000_000m;
    private static readonly decimal BillionThreshold = 1_000_000_000m;
    private static readonly decimal MillionThreshold = 1_000_000m;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogError("ButtonCounter requires a RectTransform component!", this);
        }
    }

    void Start()
    {
        // Register for score updates from ScoreManager
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged += UpdateCounterText;
            // Update text with initial score
            UpdateCounterText(ScoreManager.Instance.GetCurrentScore());
        }
        else
        {
            Debug.LogError(
                "ScoreManager not found! ButtonCounter cannot register for score updates or get initial score."
            );
        }

        // Optional: Add click listener to the button component
        // Can be removed if direct IPointerClickHandler on the coin is preferred
        if (clickButton != null)
        {
            clickButton.onClick.AddListener(HandleClick);
        }
    }

    void OnDestroy()
    {
        // Unregister from ScoreManager event when this object is destroyed
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged -= UpdateCounterText;
        }
    }

    // Called by Unity's Event System when the GameObject is clicked
    public void OnPointerClick(PointerEventData eventData)
    {
        // Check if the object clicked is NOT this specific coin GameObject.
        // If the click hit a different UI element (like the upgrade panel background),
        // ignore the click for the coin.
        if (eventData.pointerCurrentRaycast.gameObject != this.gameObject)
        {
            return;
        }

        // If the click hit the coin itself, proceed with the click logic.
        HandleClick();
    }

    // Centralized click handling logic
    void HandleClick()
    {
        // Tell GameManager to process the click, providing our position
        if (GameManager.Instance != null && rectTransform != null)
        {
            GameManager.Instance.ProcessClick(rectTransform.anchoredPosition);
        }
        else
        {
            Debug.LogError(
                "Cannot process click: GameManager instance or RectTransform is missing."
            );
            return; // Don't proceed if we can't notify GameManager
        }

        // Trigger the coin's visual animation (remains local responsibility)
        if (coinAnimator != null)
        {
            AnimatorStateInfo stateInfo = coinAnimator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName("Idle")) // Check if idle before triggering
            {
                coinAnimator.SetTrigger("Spin");
            }
        }
        else
        {
            Debug.LogWarning("Coin Animator not assigned in ButtonCounter!", this);
        }
    }

    // Accept decimal score and format
    void UpdateCounterText(decimal newScore)
    {
        if (counterText != null)
        {
            // Use the new helper method to format the score
            counterText.text = FormatScore(newScore);
        }
        else
        {
            Debug.LogWarning("CounterText is not assigned in ButtonCounter!", this);
        }
    }

    /// <summary>
    /// Formats the score with commas and abbreviations (M, B, T, Qd, Qt, Sx, C).
    /// </summary>
    /// <param name="score">The score to format.</param>
    /// <returns>Formatted score string.</returns>
    public static string FormatScore(decimal score)
    {
        if (score >= CryptonThreshold)
        {
            return $"{(score / SextillionThreshold):F2} C"; // Use Sextillion base for Cryptons
        }
        if (score >= SextillionThreshold)
        {
            return $"{(score / SextillionThreshold):F2} Sx";
        }
        if (score >= QuintillionThreshold)
        {
            return $"{(score / QuintillionThreshold):F2} Qt";
        }
        if (score >= QuadrillionThreshold)
        {
            return $"{(score / QuadrillionThreshold):F2} Qd";
        }
        if (score >= TrillionThreshold)
        {
            return $"{(score / TrillionThreshold):F2} T";
        }
        if (score >= BillionThreshold)
        {
            return $"{(score / BillionThreshold):F2} B";
        }
        if (score >= MillionThreshold)
        {
            return $"{(score / MillionThreshold):F2} M";
        }

        // Format with commas for numbers less than a million
        // Use CultureInfo.InvariantCulture to ensure consistent decimal/group separators
        return score.ToString("N0", CultureInfo.InvariantCulture);
    }
}
