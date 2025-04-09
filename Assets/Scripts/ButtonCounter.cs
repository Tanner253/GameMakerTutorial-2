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
    // REMOVED: Unused optional Button reference
    // public Button clickButton;

    private RectTransform rectTransform; // Store RectTransform for position
    private Vector3 originalScale; // Store the original scale
    private Coroutine clickAnimationCoroutine; // To manage the animation coroutine

    // Animation parameters
    private float animationDuration = 0.1f; // Duration of scale down/up
    private float scaleDownFactor = 0.9f; // How much to scale down

    // REMOVED Number formatting constants - Moved to NumberFormatter utility
    // // Define constants for number thresholds (using decimal for precision)
    // private static readonly decimal GoldThreshold = 1_000_000_000_000_000_000_000_000m;
    // private static readonly decimal SextillionThreshold = 1_000_000_000_000_000_000_000m;
    // private static readonly decimal QuintillionThreshold = 1_000_000_000_000_000_000m;
    // private static readonly decimal QuadrillionThreshold = 1_000_000_000_000_000m;
    // private static readonly decimal TrillionThreshold = 1_000_000_000_000m;
    // private static readonly decimal BillionThreshold = 1_000_000_000m;
    // private static readonly decimal MillionThreshold = 1_000_000m;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogError("ButtonCounter requires a RectTransform component!", this);
        }
        else
        {
             originalScale = rectTransform.localScale; // Store the initial scale
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

        // REMOVED: Optional Button listener logic - Not used
        // if (clickButton != null)
        // {
        //     clickButton.onClick.AddListener(HandleClick);
        // }
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
        Debug.Log($"OnPointerClick received at {Time.time} for object {eventData.pointerCurrentRaycast.gameObject?.name ?? "None"}");

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

        // Start the click animation coroutine
        if (clickAnimationCoroutine != null)
        {
            StopCoroutine(clickAnimationCoroutine); // Stop previous animation if running
        }
        clickAnimationCoroutine = StartCoroutine(ClickAnimation());
    }

    // Coroutine for click animation
    private IEnumerator ClickAnimation()
    {
        Vector3 targetScale = originalScale * scaleDownFactor;
        float elapsedTime = 0f;

        // Scale down
        while (elapsedTime < animationDuration / 2)
        {
            rectTransform.localScale = Vector3.Lerp(originalScale, targetScale, (elapsedTime / (animationDuration / 2)));
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        rectTransform.localScale = targetScale; // Ensure final scale down state

        elapsedTime = 0f; // Reset for scale up

        // Scale back up
        while (elapsedTime < animationDuration / 2)
        {
            rectTransform.localScale = Vector3.Lerp(targetScale, originalScale, (elapsedTime / (animationDuration / 2)));
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        rectTransform.localScale = originalScale; // Ensure final original scale state
        clickAnimationCoroutine = null; // Reset coroutine reference
    }

    // Accept decimal score and format
    void UpdateCounterText(decimal newScore)
    {
        if (counterText != null)
        {
            // Use the new utility method to format the score
            counterText.text = NumberFormatter.FormatNumber(newScore);
        }
        else
        {
            Debug.LogWarning("CounterText is not assigned in ButtonCounter!", this);
        }
    }
}
