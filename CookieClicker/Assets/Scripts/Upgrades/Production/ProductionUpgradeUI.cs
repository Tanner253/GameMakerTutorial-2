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

    private AutoClickerUnit autoClickerUnit; // Reference to the unit logic component

    // Caching the state
    private UpgradeState _currentUpgradeState;
    // Cost is calculated on demand via the property getter

    // --- Implementation of Abstract Members from UpgradeButtonUIBase --- 

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
             // Ensure we return a valid state, even if initialization failed partially
            if (_currentUpgradeState == null && productionUpgradeData != null) 
            {
                // Attempt to create a default state if manager didn't provide one
                // This might happen if the data was added after manager initialization
                Debug.LogWarning($"State for {productionUpgradeData.name} was null, creating default. Ensure ProductionManager initializes it.");
                _currentUpgradeState = new UpgradeState(productionUpgradeData);
                 // We don't add it to the manager here, manager should own the list
            }
            return _currentUpgradeState;
        }
    }

    protected override decimal CurrentCost
    {
        get
        {
            // Calculate cost based on the *current* state
            if (ProductionManager.Instance != null && productionUpgradeData != null && CurrentUpgradeState != null)
            {
                 return ProductionManager.Instance.CalculateUpgradeCost(productionUpgradeData, CurrentUpgradeState.level);
            }
            return decimal.MaxValue; // Indicate unavailability if prerequisites missing
        }
    }

    // --- MonoBehaviour Methods --- 

    // Awake is a standard Unity message, not virtual/abstract in base class
    protected void Awake()
    {
        // base.Awake(); // Removed call to base.Awake as it's not defined in base class
        // Get or add the AutoClickerUnit component
        autoClickerUnit = GetComponent<AutoClickerUnit>();
        if (autoClickerUnit == null)
        {
            autoClickerUnit = gameObject.AddComponent<AutoClickerUnit>();
            Debug.LogWarning("AutoClickerUnit component was missing, added automatically.", this);
        }
        // Initialization logic moved to Start or a dedicated Initialize method called externally
    }
    
    // Use Start or an explicit Initialize method called by the setup script
    // to ensure Managers are available
    protected void Start()
    {
        InitializeUnitAndUI();
    }
    
    /// <summary>
    /// Initializes the AutoClicker unit and the initial UI state.
    /// Assumes ProductionManager is available.
    /// </summary>
    void InitializeUnitAndUI()
    {
        if (productionUpgradeData == null)
        {
            Debug.LogError("ProductionUpgradeData is not assigned!", this);
            gameObject.SetActive(false);
            return;
        }
        
        // Retrieve the state via the property getter which handles caching/creation
        var state = CurrentUpgradeState; 
        if (state == null)
        {   
            // Property getter should have logged an error/warning
            gameObject.SetActive(false); // Disable if state cannot be determined
            return;
        }

        // Initialize the AutoClickerUnit with data and current level
        if (autoClickerUnit != null)
        {
            autoClickerUnit.Initialize(productionUpgradeData, state.level);
        }
        else
        {
            Debug.LogError("AutoClickerUnit component is missing after Awake!", this);
        }

        // Initial UI Update (calls UpdateUIDisplay internally)
        UpdateUIDisplay();
    }
    
    // Remove direct subscription/unsubscription - Base class handles events
    // void OnDestroy() { ... }

    // --- Event Handlers & Purchase Logic (Implement required abstract methods) --- 

    /// <summary>
    /// Called by the base class when an upgrade purchase relevant to this UI might have occurred.
    /// This UI should update its state based on the provided state.
    /// </summary>
    protected override void HandleSpecificUpgradePurchased(UpgradeState purchasedUpgradeState)
    {
        // Check if the event is for the upgrade this UI represents
        if (purchasedUpgradeState != null && purchasedUpgradeState.upgradeDataRef == this.productionUpgradeData)
        {   
             // Update our cached state FIRST
            _currentUpgradeState = purchasedUpgradeState;
            
            // Update the AutoClickerUnit's level
            if(autoClickerUnit != null)
            {
                autoClickerUnit.SetLevel(_currentUpgradeState.level);
            }
            
            // Update the entire UI display using the new state
            UpdateUIDisplay(); 
        }
        else
        {
            // Even if a different upgrade was purchased, our affordability might change
            // Base class's UpdateUI likely calls UpdatePurchaseButtonInteractability
             UpdatePurchaseButtonInteractability();
        }
    }
    
    /// <summary>
    /// Called when the purchase button is clicked (typically by the base class).
    /// Uses ProductionManager to attempt the purchase.
    /// </summary>
    protected override void TryPurchaseUpgrade()
    {
        if (ProductionManager.Instance != null && productionUpgradeData != null)
        {
            // Attempt purchase via ProductionManager
            // ProductionManager handles state update and event firing upon success
            ProductionManager.Instance.TryPurchaseUpgrade(productionUpgradeData);
            // Base class might handle button disabling/feedback, or we might need:
            // UpdatePurchaseButtonInteractability(); // Re-check immediately after trying
        }
         else
        {
            Debug.LogError("Cannot purchase: ProductionManager or ProductionUpgradeData missing.");
        }
    }

    /// <summary>
    /// Updates UI elements specific to production upgrades.
    /// Called by the base class's UpdateUIDisplay method.
    /// </summary>
    protected override void UpdateSpecificUI()
    {
        var state = CurrentUpgradeState; // Use property getter
        if (productionRateText != null && productionUpgradeData != null && state != null && productionUpgradeData.tickRate > 0)
        {
            // Calculate and display production rate per second
            decimal productionPerTick = (decimal)productionUpgradeData.baseProductionAmount;
            decimal ticksPerSecond = 1M / (decimal)productionUpgradeData.tickRate;
            decimal currentRatePerSecond = productionPerTick * ticksPerSecond * state.level;
            // decimal nextLevelRatePerSecond = productionPerTick * ticksPerSecond * (state.level + 1);
            
            productionRateText.text = $"{currentRatePerSecond:F2}/sec";
            // Optionally, update description with next level info
            // upgradeDescriptionText.text = $"{GetUpgradeDescription()} (+{nextLevelRatePerSecond - currentRatePerSecond:F2}/sec)";
        }
        else if (productionRateText != null)
        {
            productionRateText.text = "0.00/sec"; // Default if level 0 or data missing
        }
    }

    // --- Helper Methods (from base or specific) ---
    // These likely come from the base class or are needed by it

    protected override string GetUpgradeName() => productionUpgradeData?.upgradeName ?? "Error";
    protected override string GetUpgradeDescription() => productionUpgradeData?.description ?? "Error";
    
    // Removed HandleUpgradeStateChanged as HandleSpecificUpgradePurchased is used
    // protected override void HandleUpgradeStateChanged(UpgradeState updatedState) { ... }
    
    // Removed GetCurrentCost() method as CurrentCost property is used
    // protected override decimal GetCurrentCost() { ... }
} 