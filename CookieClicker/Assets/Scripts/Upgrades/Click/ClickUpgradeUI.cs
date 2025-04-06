using UnityEngine;
using TMPro; // Use TextMeshPro if you are using it, otherwise use UnityEngine.UI

/// <summary>
/// UI Handler for CLICK upgrade buttons.
/// Inherits from UpgradeButtonUIBase and handles click-specific data and interactions.
/// </summary>
public class ClickUpgradeUI : UpgradeButtonUIBase
{
    [Header("Click Specific Data")]
    [Tooltip("Assign the ClickUpgradeData ScriptableObject for this upgrade.")]
    public ClickUpgradeData clickUpgradeData;

    [Header("Click Specific UI")]
    public TextMeshProUGUI clickBonusText; // Text to show the bonus per click this upgrade gives

    // Caching the state and cost for efficiency
    private UpgradeState _currentUpgradeState;
    private decimal _currentCost;

    // --- Implementation of Abstract Members ---

    protected override ScriptableObject UpgradeData => clickUpgradeData;

    protected override UpgradeState CurrentUpgradeState
    {
        get
        {
            // Retrieve state from GameManager
            if (_currentUpgradeState == null && GameManager.Instance != null && clickUpgradeData != null)
            {
                _currentUpgradeState = GameManager.Instance.GetClickUpgradeState(clickUpgradeData);
            }
            return _currentUpgradeState;
        }
    }

    protected override decimal CurrentCost
    {
        get
        {
            // Calculate cost using GameManager
            if (GameManager.Instance != null && clickUpgradeData != null && CurrentUpgradeState != null)
            {
                 _currentCost = GameManager.Instance.CalculateClickUpgradeCost(clickUpgradeData, CurrentUpgradeState.level);
            }
            return _currentCost;
        }
    }

    protected override void TryPurchaseUpgrade()
    {
        if (GameManager.Instance != null)
        {
            // Attempt purchase via GameManager
            if (GameManager.Instance.TryPurchaseUpgrade(CurrentCost))
            {
                // If successful, apply the upgrade effect
                GameManager.Instance.ApplyClickUpgrade(clickUpgradeData);
                // UI updates are handled via the OnClickUpgradeStateChanged event
            }
            else
            {
                // Handle purchase failure feedback if needed (e.g., button shake)
                // Since button is disabled momentarily on click, explicit re-enable might be needed on fail
                 UpdatePurchaseButtonInteractability(); // Re-check interactability immediately after fail
            }
        }
    }

    protected override void UpdateSpecificUI()
    {
        // Update the click bonus text
        if (clickBonusText != null && clickUpgradeData != null)
        {
            // Show the bonus this upgrade provides per level
            clickBonusText.text = $"+{clickUpgradeData.clickBonusPerLevel:F1}/click";
        }
    }

    protected override string GetUpgradeName() => clickUpgradeData?.upgradeName ?? "Error";
    protected override string GetUpgradeDescription() => clickUpgradeData?.description ?? "Error";

    /// <summary>
    /// Handler for the GameManager's click upgrade change event.
    /// </summary>
    protected override void HandleSpecificUpgradePurchased(UpgradeState purchasedUpgradeState)
    {
        // Check if the event is for the upgrade this UI represents
        if (purchasedUpgradeState != null && purchasedUpgradeState.upgradeDataRef == this.clickUpgradeData)
        {
            // Update cached state and recalculate cost
            _currentUpgradeState = purchasedUpgradeState;
            _currentCost = GameManager.Instance.CalculateClickUpgradeCost(clickUpgradeData, _currentUpgradeState.level);
            
            // Update the entire UI display
            UpdateUIDisplay();
        }
        else
        {
            // Even if a different upgrade was purchased, our affordability might have changed
            UpdatePurchaseButtonInteractability();
        }
    }

    // --- Event Subscription Overrides ---

    protected override void SubscribeToEvents()
    {
        base.SubscribeToEvents(); // Subscribe to common events
        // Subscribe to the click-specific event from GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnClickUpgradeStateChanged += HandleSpecificUpgradePurchased;
        }
    }

    protected override void UnsubscribeFromEvents()
    {
        base.UnsubscribeFromEvents(); // Unsubscribe from common events
        // Unsubscribe from the click-specific event
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnClickUpgradeStateChanged -= HandleSpecificUpgradePurchased;
        }
    }
} 