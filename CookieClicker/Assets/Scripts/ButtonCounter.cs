using System.Collections; // Needed for Coroutines
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
        // Register for score updates from GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreChanged += UpdateCounterText;
            // Update text with initial score
            UpdateCounterText(GameManager.Instance.GetCurrentScore()); 
        }
        else
        {
            Debug.LogError("GameManager not found! ButtonCounter cannot register for score updates.");
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
        // Unregister from GameManager event when this object is destroyed
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreChanged -= UpdateCounterText;
        }
    }

    // Called by Unity's Event System when the GameObject is clicked
    public void OnPointerClick(PointerEventData eventData)
    {
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
             Debug.LogError("Cannot process click: GameManager instance or RectTransform is missing.");
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
            // Format the decimal score to display with one decimal place
            counterText.text = $"count: {newScore:F1}";
        }
        else
        {
            Debug.LogWarning("CounterText is not assigned in ButtonCounter!", this);
        }
    }
}
