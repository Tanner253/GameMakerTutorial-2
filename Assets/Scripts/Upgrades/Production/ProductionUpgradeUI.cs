using UnityEngine;
using UnityEngine.UI;
using TMPro; // Use TextMeshPro if you are using it, otherwise use UnityEngine.UI
using System;

/// <summary>
/// UI Handler for PRODUCTION upgrade buttons.
/// Inherits from UpgradeButtonUIBase and handles production-specific data and interactions.
/// </summary>
public class ProductionUpgradeUI : UpgradeButtonUIBase
{
    [Header("Production Specific Data")]
    [Tooltip("Assign the ProductionUpgradeData ScriptableObject for this upgrade.")]
    public ProductionUpgradeData productionUpgradeData;

    [Header("Production Specific UI")]
    public TextMeshProUGUI productionRateText; // Text to show production per second

    // Caching the state and cost for efficiency
    private UpgradeState _currentUpgradeState;
    private decimal _currentCost;

    // --- Implementation of Abstract Members --- 

    protected override ScriptableObject UpgradeData => productionUpgradeData;

    protected override UpgradeState CurrentUpgradeState
    {
        get
        {
            // Retrieve or update the cached state if needed
            if (_currentUpgradeState == null && ProductionManager.Instance != null && productionUpgradeData != null)
            {
                _currentUpgradeState = ProductionManager.Instance.GetPlayerUpgradeState(productionUpgradeData);
            }
            return _currentUpgradeState;
        }
    }

    protected override decimal CurrentCost
    {
        get
        {
            // Retrieve or update the cached cost if needed
            if (ProductionManager.Instance != null && productionUpgradeData != null && CurrentUpgradeState != null)
            {
                 _currentCost = ProductionManager.Instance.CalculateUpgradeCost(productionUpgradeData, CurrentUpgradeState.level);
            }
            return _currentCost;
        }
    }

    protected override void TryPurchaseUpgrade()
    {
        if (ProductionManager.Instance != null)
        {
            ProductionManager.Instance.TryPurchaseUpgrade(productionUpgradeData);
            // UI updates are handled via the OnProductionUpgradeStateChanged event
        }
    }

    protected override void UpdateSpecificUI()
    {
        // Update the production rate text
        if (productionRateText != null && productionUpgradeData != null)
        {
            if (productionUpgradeData.tickRate > 0)
            {
                // Calculate production per second for the *current* level
                // Use level directly, not Mathf.Max(1, level) unless you want to show next level's rate when at 0
                int currentLevel = CurrentUpgradeState?.level ?? 0;
                decimal productionPerTick = (decimal)productionUpgradeData.baseProductionAmount * currentLevel; 
                decimal productionPerSecond = (productionPerTick / (decimal)productionUpgradeData.tickRate);
                productionRateText.text = $"Prod: {productionPerSecond:F2}/s";
            }
            else
            {
                productionRateText.text = "Prod: N/A";
            }
        }
    }

    protected override string GetUpgradeName() => productionUpgradeData?.upgradeName ?? "Error";
    protected override string GetUpgradeDescription() => productionUpgradeData?.description ?? "Error";

    /// <summary>
    /// Handler for the ProductionManager's event.
    /// </summary>
    protected override void HandleSpecificUpgradePurchased(UpgradeState purchasedUpgradeState)
    {
        // Check if the event is for the upgrade this UI represents
        if (purchasedUpgradeState != null && purchasedUpgradeState.upgradeDataRef == this.productionUpgradeData)
        {
            // Update our cached state and cost
            _currentUpgradeState = purchasedUpgradeState;
            _currentCost = ProductionManager.Instance.CalculateUpgradeCost(productionUpgradeData, _currentUpgradeState.level);
            
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
        // Subscribe to the production-specific event
        if (ProductionManager.Instance != null)
        {
            ProductionManager.Instance.OnProductionUpgradeStateChanged += HandleSpecificUpgradePurchased;
        }
    }

    protected override void UnsubscribeFromEvents()
    {
        base.UnsubscribeFromEvents(); // Unsubscribe from common events
        // Unsubscribe from the production-specific event
        if (ProductionManager.Instance != null)
        {
            ProductionManager.Instance.OnProductionUpgradeStateChanged -= HandleSpecificUpgradePurchased;
        }
    }
} 