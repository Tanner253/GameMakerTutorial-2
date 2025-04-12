using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Globalization;

public class PrestigeUpgradeButtonUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI levelText;
    public Button purchaseButton;
    public TextMeshProUGUI requirementText; // Optional: To show min prestige level
    public TextMeshProUGUI currentStatsText; // NEW: For displaying current stats
    public TextMeshProUGUI nextLevelStatsText; // NEW: For displaying next level stats

    private PrestigeUpgradeData _upgradeData;
    private PrestigeManager _prestigeManager;
    private UpgradeState _currentState;

    // Method called by PrestigeUIController to set up the button
    public void Setup(PrestigeUpgradeData data, PrestigeManager manager)
    {
        _upgradeData = data;
        _prestigeManager = manager;

        if (_upgradeData == null || _prestigeManager == null)
        {
            Debug.LogError("PrestigeUpgradeButtonUI: Setup failed due to null data or manager.");
            gameObject.SetActive(false);
            return;
        }

        // Get current state
        _currentState = _prestigeManager.GetPrestigeUpgradeState(_upgradeData);
        if (_currentState == null)
        {
            // This might happen if the SO wasn't in the manager's list during init
            Debug.LogError($"Could not find state for Prestige Upgrade: {_upgradeData.name}");
            gameObject.SetActive(false);
            return;
        }

        // Set static text
        if (nameText != null) nameText.text = _upgradeData.upgradeName;
        if (descriptionText != null) descriptionText.text = _upgradeData.description;

        // Add listener to purchase button
        purchaseButton?.onClick.RemoveAllListeners(); // Clear previous listeners
        purchaseButton?.onClick.AddListener(HandlePurchaseClick);

        // Subscribe to relevant events to keep UI updated
        SubscribeToEvents();

        // Initial UI update
        UpdateDisplay();
    }

    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    void SubscribeToEvents()
    {
        if (_prestigeManager != null)
        {
            _prestigeManager.OnGoldBarsChanged += HandleGoldBarsChanged;
            _prestigeManager.OnPrestigeCountChanged += HandlePrestigeCountChanged;
            // Optionally subscribe to specific upgrade changes if needed for more complex updates
             _prestigeManager.OnPrestigeUpgradePurchased += HandleThisUpgradePurchased;
        }
    }

    void UnsubscribeFromEvents()
    {
         if (_prestigeManager != null)
        {
            _prestigeManager.OnGoldBarsChanged -= HandleGoldBarsChanged;
            _prestigeManager.OnPrestigeCountChanged -= HandlePrestigeCountChanged;
            _prestigeManager.OnPrestigeUpgradePurchased -= HandleThisUpgradePurchased;
        }
    }

    void UpdateDisplay()
    {
        if (_upgradeData == null || _prestigeManager == null || _currentState == null) return;

        // Update Level Text
        if (levelText != null)
        {
            if (_upgradeData.isUniqueUnlock && _currentState.level > 0)
            {
                levelText.text = "Unlocked"; // Show Unlocked for unique purchases
            }
            else
            {
                 levelText.text = $"Level: {_currentState.level}";
            }
        }

        // Update Cost Text
        if (costText != null)
        {
            if (_upgradeData.isUniqueUnlock && _currentState.level > 0)
            {
                 costText.text = "-"; // No cost after unique purchase
            }
            else
            {
                decimal cost = _prestigeManager.CalculatePrestigeUpgradeCost(_upgradeData, _currentState.level);
                costText.text = $"Cost: {NumberFormatter.FormatNumber(cost)} GB";
            }
        }

        // Update Description - Now just the basic description without stats
        if (descriptionText != null)
        {
            // Only use the basic description from the ScriptableObject
            descriptionText.text = _upgradeData.description;
        }

        // Update Current Stats Text
        if (currentStatsText != null)
        {
            string currentStats = "";
            switch (_upgradeData.effectType)
            {
                case PrestigeEffectType.LemonLifespan:
                    float currentLifespanBonus = _prestigeManager.GetTotalLemonLifespanBonusSeconds();
                    float baseLifespan = _upgradeData.baseValueOverride > 0 ? _upgradeData.baseValueOverride : 15f;
                    float currentTotalLifespan = baseLifespan + currentLifespanBonus;
                    currentStats = $"Current: {currentTotalLifespan:F0} seconds";
                    break;
                    
                case PrestigeEffectType.LemonSpawnRate:
                    float spawnRateBonus = _prestigeManager.GetTotalLemonSpawnTimeReduction();
                    
                    // Get the estimated spawn times directly from PrestigeManager
                    var (minSpawnTime, maxSpawnTime) = _prestigeManager.GetEstimatedLemonSpawnTimes();
                    
                    // Show the current spawn time range and reduction on a single line
                    currentStats = $"Spawn time: {minSpawnTime:F0}-{maxSpawnTime:F0} seconds (Reduced by {-spawnRateBonus:F0}s)";
                    break;
                    
                case PrestigeEffectType.LemonValue:
                    float valueBonus = _prestigeManager.GetTotalLemonValueBonusMinutes();
                    float baseValue = _upgradeData.baseValueOverride > 0 ? _upgradeData.baseValueOverride : 5f;
                    float currentTotalValue = baseValue + valueBonus;
                    currentStats = $"Current: {currentTotalValue:F0} minutes of CPS";
                    break;
                    
                case PrestigeEffectType.ClickMultiplier:
                    float multiplierBonus = _prestigeManager.GetTotalClickMultiplierBonus();
                    currentStats = $"Current bonus: +{multiplierBonus * 100:F0}%";
                    break;
                    
                case PrestigeEffectType.UnlockFeature:
                default:
                    currentStatsText.gameObject.SetActive(false);
                    break;
            }
            
            currentStatsText.text = currentStats;
            currentStatsText.gameObject.SetActive(!string.IsNullOrEmpty(currentStats));
        }
        
        // Update Next Level Stats Text
        if (nextLevelStatsText != null)
        {
            string nextLevelStats = "";
            // Only show next level stats if not a unique upgrade that's already purchased
            if (!(_upgradeData.isUniqueUnlock && _currentState.level > 0))
            {
                switch (_upgradeData.effectType)
                {
                    case PrestigeEffectType.LemonLifespan:
                        float nextLifespanBonus = _upgradeData.effectValuePerLevel;
                        nextLevelStats = $"Next level: +{nextLifespanBonus:F0} seconds";
                        break;
                        
                    case PrestigeEffectType.LemonSpawnRate:
                        float nextSpawnRateBonus = _upgradeData.effectValuePerLevel;
                        nextLevelStats = $"Next level: {nextSpawnRateBonus:F0} seconds faster";
                        break;
                        
                    case PrestigeEffectType.LemonValue:
                        float nextValueBonus = _upgradeData.effectValuePerLevel;
                        nextLevelStats = $"Next level: +{nextValueBonus:F0} minute";
                        break;
                        
                    case PrestigeEffectType.ClickMultiplier:
                        float nextMultiplierBonus = _upgradeData.effectValuePerLevel;
                        nextLevelStats = $"Next level: +{nextMultiplierBonus * 100:F0}%";
                        break;
                        
                    case PrestigeEffectType.UnlockFeature:
                    default:
                        nextLevelStatsText.gameObject.SetActive(false);
                        break;
                }
            }
            
            nextLevelStatsText.text = nextLevelStats;
            nextLevelStatsText.gameObject.SetActive(!string.IsNullOrEmpty(nextLevelStats));
        }

        // Update Requirement Text
        if (requirementText != null)
        {
            if (_upgradeData.requiredPrestigeLevel > 0)
            {
                 requirementText.text = $"Req Prestige: {_upgradeData.requiredPrestigeLevel}";
                 requirementText.gameObject.SetActive(true);
            }
            else
            {
                 requirementText.gameObject.SetActive(false);
            }
        }

        // Update Button Interactability
        if (purchaseButton != null)
        {
            purchaseButton.interactable = _prestigeManager.CanAffordPrestigeUpgrade(_upgradeData);
        }
    }

    void HandlePurchaseClick()
    {
        // Play sound first
        AudioManager.Instance?.PlayClickSound();

        _prestigeManager?.PurchasePrestigeUpgrade(_upgradeData);
        // The UI should update via the HandleThisUpgradePurchased event handler
    }

    // Event Handlers
    private void HandleGoldBarsChanged(decimal newGoldBars)
    {
        UpdateDisplay(); // Re-check affordability
    }

    private void HandlePrestigeCountChanged(int newPrestigeCount)
    {
        UpdateDisplay(); // Re-check requirement and affordability
    }

    // Called when ANY prestige upgrade is purchased.
    // We only care if *this* specific upgrade was purchased.
    private void HandleThisUpgradePurchased(PrestigeUpgradeData purchasedData, int newLevel)
    {
        if (purchasedData == _upgradeData)
        {
            // Update our state reference (level should already be updated in manager)
            _currentState = _prestigeManager.GetPrestigeUpgradeState(_upgradeData);
            UpdateDisplay(); // Refresh this button's UI
        }
        else
        {
             // Another upgrade was purchased, maybe our affordability changed indirectly?
             // UpdateDisplay(); // Uncomment if costs depend on other upgrades (unlikely here)
        }
    }
} 