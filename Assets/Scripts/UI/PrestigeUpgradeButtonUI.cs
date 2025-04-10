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
            levelText.text = $"Level: {_currentState.level}";

        // Update Cost Text (handle max level if applicable)
        if (costText != null)
        {
            if (_upgradeData.isUniqueUnlock && _currentState.level > 0)
            {
                 costText.text = "Purchased";
            }
            else
            {
                decimal cost = _prestigeManager.CalculatePrestigeUpgradeCost(_upgradeData, _currentState.level);
                costText.text = $"Cost: {NumberFormatter.FormatNumber(cost)} GB";
            }
        }

        // Update Requirement Text (optional)
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