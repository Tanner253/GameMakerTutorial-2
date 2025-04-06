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
            // Retrieve state from ClickUpgradeManager
            if (_currentUpgradeState == null && ClickUpgradeManager.Instance != null && clickUpgradeData != null)
            {
                _currentUpgradeState = ClickUpgradeManager.Instance.GetClickUpgradeState(clickUpgradeData);
            }
            return _currentUpgradeState;
        }
    }

    protected override decimal CurrentCost
    {
        get
        {
            // Calculate cost using ClickUpgradeManager
            if (ClickUpgradeManager.Instance != null && clickUpgradeData != null && CurrentUpgradeState != null)
            {
                 _currentCost = ClickUpgradeManager.Instance.CalculateClickUpgradeCost(clickUpgradeData, CurrentUpgradeState.level);
            }
            return _currentCost;
        }
    }

    protected override void TryPurchaseUpgrade()
    {
        if (ClickUpgradeManager.Instance != null && ScoreManager.Instance != null)
        {
            // Attempt purchase via ScoreManager
            if (ScoreManager.Instance.TrySpendScore(CurrentCost))
            {
                // If successful, apply the upgrade effect via ClickUpgradeManager
                ClickUpgradeManager.Instance.ApplyClickUpgrade(clickUpgradeData);
                // UI updates are handled via the OnClickUpgradeStateChanged event subscription
            }
            else
            {
                // Handle purchase failure feedback if needed (e.g., button shake)
                // Re-check interactability immediately after fail
                 UpdatePurchaseButtonInteractability(); 
            }
        }
    }

    protected override void UpdateSpecificUI()
    {
        // Update the click bonus text to show the *total* bonus from current level
        if (clickBonusText != null && clickUpgradeData != null)
        {
            int currentLevel = CurrentUpgradeState?.level ?? 0;
            if (currentLevel > 0)
            {
                // Calculate total bonus from this upgrade at its current level
                decimal totalBonusFromThis = (decimal)clickUpgradeData.clickBonusPerLevel * currentLevel;
                clickBonusText.text = $"Bonus: +{totalBonusFromThis:F1}/click"; // Changed text format
            }
            else
            {
                // Show the base bonus per level if level is 0
                clickBonusText.text = $"Bonus: +{clickUpgradeData.clickBonusPerLevel:F1}/click (per lvl)";
            }

        }
    }

    protected override string GetUpgradeName() => clickUpgradeData?.upgradeName ?? "Error";
    protected override string GetUpgradeDescription() => clickUpgradeData?.description ?? "Error";

    /// <summary>
    /// Handler for the ClickUpgradeManager's upgrade change event.
    /// </summary>
    protected override void HandleSpecificUpgradePurchased(UpgradeState purchasedUpgradeState)
    {
        // Check if the event is for the upgrade this UI represents
        if (purchasedUpgradeState != null && purchasedUpgradeState.upgradeDataRef == this.clickUpgradeData)
        {
            // Update cached state and recalculate cost using ClickUpgradeManager
            _currentUpgradeState = purchasedUpgradeState;
            if (ClickUpgradeManager.Instance != null)
            {
                 _currentCost = ClickUpgradeManager.Instance.CalculateClickUpgradeCost(clickUpgradeData, _currentUpgradeState.level);
            }
            
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
        base.SubscribeToEvents(); // Subscribe to common events (button click, score changed)
        // Subscribe to the click-specific event from ClickUpgradeManager
        if (ClickUpgradeManager.Instance != null)
        {
            ClickUpgradeManager.Instance.OnClickUpgradeStateChanged += HandleSpecificUpgradePurchased;
        }
    }

    protected override void UnsubscribeFromEvents()
    {
        base.UnsubscribeFromEvents(); // Unsubscribe from common events
        // Unsubscribe from the click-specific event
        if (ClickUpgradeManager.Instance != null)
        {
            ClickUpgradeManager.Instance.OnClickUpgradeStateChanged -= HandleSpecificUpgradePurchased;
        }
    }
} 