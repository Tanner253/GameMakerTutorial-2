using System; // For Math.Pow
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Ensure Button and TextMeshPro components are present
[RequireComponent(typeof(Button))]
public class ClickUpgradeButton : MonoBehaviour
{
    [Header("Upgrade Settings - Base Values")]
    [Tooltip("The initial cost of the first upgrade.")]
    public long baseCost = 10;

    [Tooltip("The amount added to the click bonus each time. Can be fractional.")]
    public decimal clickBonusIncrease = 0.1M; // Use M suffix

    [Tooltip("Cost multiplier per purchase (e.g., 1.25 for 25% increase each time).")]
    public float costIncreaseFactor = 1.25f;

    [Header("State (Tracked Internally)")]
    [SerializeField] // Show in inspector for debugging, but don't require manual setting
    private int purchaseCount = 0;
    private long currentCost;

    [Header("UI References")]
    public TextMeshProUGUI costText; // Optional: Text to display the cost
    public TextMeshProUGUI descriptionText; // Optional: Text to describe the upgrade effect

    private Button upgradeButton;

    void Awake()
    {
        upgradeButton = GetComponent<Button>();
        // Ensure cost factor is reasonable
        costIncreaseFactor = Mathf.Max(1.01f, costIncreaseFactor); // Prevent factor <= 1 which breaks scaling
    }

    void Start()
    {
        // Add listener to the button's click event
        upgradeButton.onClick.AddListener(AttemptPurchase);

        // Calculate initial cost based on current state (might be loaded later)
        CalculateCurrentCost();
        UpdateUI();

        // Subscribe to GameManager events to update UI dynamically
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreChanged += OnScoreChanged;
            // Initial check for interactability
            OnScoreChanged(GameManager.Instance.GetCurrentScore());
        }
        else
        {
            Debug.LogError("GameManager not found! Upgrade button cannot function correctly.");
            upgradeButton.interactable = false; // Disable if no GameManager
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from GameManager events
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreChanged -= OnScoreChanged;
        }
    }

    // Calculates the current cost based on base, count, factor, and global scale
    void CalculateCurrentCost()
    {
        float globalScale =
            GameManager.Instance != null ? GameManager.Instance.globalProgressionScale : 1.0f;
        // Clamp global scale just in case it wasn't clamped in GameManager Awake yet
        globalScale = Mathf.Max(0.1f, globalScale);

        // Calculate exponential cost
        double costBeforeScaling = baseCost * Math.Pow(costIncreaseFactor, purchaseCount);

        // Apply global scaling factor
        double scaledCost = costBeforeScaling * globalScale;

        // Convert to long, ensuring it doesn't overflow and is at least 1
        // Use ceiling to make upgrades slightly harder if scaling results in fraction
        currentCost = (long)Math.Ceiling(scaledCost);
        currentCost = Math.Max(1, currentCost); // Ensure cost is at least 1

        // Add overflow check if necessary (very large numbers)
        if (scaledCost > long.MaxValue)
        {
            currentCost = long.MaxValue;
            Debug.LogWarning("Upgrade cost exceeded maximum long value!");
        }
    }

    // Method called when the button is clicked
    void AttemptPurchase()
    {
        if (GameManager.Instance == null)
            return;

        // Recalculate cost just before purchase attempt to ensure it uses the latest global scale
        CalculateCurrentCost();

        if (GameManager.Instance.TryPurchaseUpgrade(currentCost))
        {
            GameManager.Instance.ApplyAdditiveClickBonusUpgrade(clickBonusIncrease);

            // Increment purchase count and calculate cost for the *next* purchase
            purchaseCount++;
            CalculateCurrentCost();
            UpdateUI(); // Update text with the new cost

            Debug.Log(
                $"Successfully purchased click upgrade! Purchases: {purchaseCount}. Next cost: {currentCost}"
            );

            // Update button interactability immediately after successful purchase
            UpdateButtonInteractability();
        }
        else
        {
            Debug.Log($"Upgrade purchase failed (not enough score for cost {currentCost}).");
            // No need to update interactability here, OnScoreChanged handles it if score was the issue
        }
        // --- Button interactability update moved inside successful purchase block or handled by OnScoreChanged ---
        // UpdateButtonInteractability(); // Removed redundant call
    }

    // Update the UI elements (cost, description)
    void UpdateUI()
    {
        if (costText != null)
        {
            // Display the *current* calculated cost
            costText.text = $"Cost: {currentCost}";
        }
        if (descriptionText != null)
        {
            descriptionText.text = $"+{clickBonusIncrease:F1} per click";
        }
    }

    // Called when the GameManager's score changes - now accepts decimal
    void OnScoreChanged(decimal newScore)
    {
        UpdateButtonInteractability();
    }

    void UpdateButtonInteractability()
    {
        CalculateCurrentCost();
        if (GameManager.Instance != null)
        {
            // Compare the decimal score with the long cost
            upgradeButton.interactable = GameManager.Instance.GetCurrentScore() >= currentCost;
        }
        else
        {
            upgradeButton.interactable = false;
        }
    }

    // --- Add methods for Save/Load if persistence is needed later ---
    // public int GetPurchaseCount() { return purchaseCount; }
    // public void SetPurchaseCount(int count) { purchaseCount = count; CalculateCurrentCost(); UpdateUI(); }
}
