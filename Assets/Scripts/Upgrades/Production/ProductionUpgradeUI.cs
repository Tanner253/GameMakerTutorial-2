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
        // This method is called by the base class's HandlePurchaseButtonClick
        if (productionUpgradeData == null) 
        {
             Debug.LogError("TryPurchaseUpgrade failed: productionUpgradeData is null.");
             return;
         }
        if (ScoreManager.Instance == null) 
        {
             Debug.LogError("TryPurchaseUpgrade failed: ScoreManager instance is null.");
             return;
         }
        if (ProductionManager.Instance == null)
        {
             Debug.LogError("TryPurchaseUpgrade failed: ProductionManager instance is null.");
             return;
        }

        // Get current state (use the property which might already have it cached)
        UpgradeState currentState = CurrentUpgradeState;
        if (currentState == null)
        {
            Debug.LogError($"TryPurchaseUpgrade failed: Could not get state for {productionUpgradeData.name}");
            return;
        }

        // Get current cost (use the property which might already have it cached)
        decimal cost = CurrentCost; 

        // Attempt to spend the score
        if (ScoreManager.Instance.TrySpendScore(cost))
        {
            // If successful, apply the upgrade in the ProductionManager
            ProductionManager.Instance.ApplyUpgrade(productionUpgradeData);
            // The ProductionManager's ApplyUpgrade will trigger OnProductionUpgradeStateChanged,
            // which should cause this UI element to update via HandleSpecificUpgradePurchased.
            Debug.Log($"Successfully purchased {productionUpgradeData.upgradeName} level {currentState.level + 1} for {cost:F0}");
            // Play purchase sound or animation if desired
        }
        else
        {
            // Handle insufficient funds (e.g., show a message, play a sound)
            Debug.Log($"Insufficient funds to purchase {productionUpgradeData.upgradeName}. Need {cost:F0}, have {ScoreManager.Instance.GetCurrentScore():F0}");
            // Optional: Trigger UI feedback (shake button, change color briefly)
            // Re-enable button interactability slightly later if purchase fails, 
            // otherwise HandleScoreChanged will re-enable it if score is sufficient.
            // We might not need this if HandleScoreChanged covers it.
             // Invoke("UpdatePurchaseButtonInteractability", 0.1f); // Example delay
             UpdatePurchaseButtonInteractability(); // Try immediately first
        }
        // Note: Base class HandlePurchaseButtonClick already disables the button temporarily.
        // The button state will be refreshed by HandleScoreChanged or HandleSpecificUpgradePurchased.
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