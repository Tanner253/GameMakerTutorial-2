using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// Abstract base class for upgrade button UI.
/// Handles common UI elements (texts, button), updating cost/level,
/// button interactability, and the purchase interaction.
/// </summary>
public abstract class UpgradeButtonUIBase : MonoBehaviour
{
    [Header("Base UI References")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI levelText;
    public Button purchaseButton;

    // Abstract properties/methods to be implemented by derived classes
    protected abstract ScriptableObject UpgradeData { get; }
    protected abstract UpgradeState CurrentUpgradeState { get; }
    protected abstract decimal CurrentCost { get; }
    protected abstract void TryPurchaseUpgrade();
    protected abstract void UpdateSpecificUI(); // For elements unique to derived classes (like production rate)

    void Start()
    {
        if (!ValidateReferences())
        {
            gameObject.SetActive(false);
            return;
        }

        InitializeUI();
        SubscribeToEvents();
    }

    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    /// <summary>
    /// Validates that essential references (Data, Button) are set.
    /// </summary>
    protected virtual bool ValidateReferences()
    {
        if (UpgradeData == null)
        {
            Debug.LogError($"UpgradeButtonUI ({this.GetType().Name}) is missing its Upgrade Data!", gameObject);
            return false;
        }
        if (purchaseButton == null)
        {
            Debug.LogError($"UpgradeButtonUI ({this.GetType().Name}) is missing its Purchase Button reference!", gameObject);
            return false;
        }
        // Check for managers needed by derived classes
        if (this is ProductionUpgradeUI && ProductionManager.Instance == null)
        {
             Debug.LogError("ProductionManager instance not found for ProductionUpgradeUI!", gameObject);
             return false;
        }
        // Check ClickUpgradeManager for ClickUpgradeUI
        if (this is ClickUpgradeUI && ClickUpgradeManager.Instance == null)
        {
            Debug.LogError("ClickUpgradeManager instance not found for ClickUpgradeUI!", gameObject);
            return false;
        }
        // Check ScoreManager (needed for checking affordability)
        if (ScoreManager.Instance == null)
        {
            Debug.LogError("ScoreManager instance not found! Cannot check purchase affordability.", gameObject);
            return false;
        }
        return true;
    }

    /// <summary>
    /// Sets initial static text values.
    /// </summary>
    protected virtual void InitializeUI()
    {
        Debug.Log($"[{gameObject.name}] Initializing UI. UpgradeData is {(UpgradeData == null ? "NULL" : UpgradeData.name)}", gameObject);

        // Name and Description are usually static based on the data
        string upgradeName = GetUpgradeName();
        Debug.Log($"[{gameObject.name}] GetUpgradeName() returned: '{upgradeName}'", gameObject);
        if (nameText != null)
        {
             nameText.text = upgradeName;
             Debug.Log($"[{gameObject.name}] Set nameText.text to '{upgradeName}'", gameObject);
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] nameText reference is NULL! Cannot set name.", gameObject);
        }

        if (descriptionText != null) descriptionText.text = GetUpgradeDescription();

        // Update dynamic elements
        UpdateUIDisplay();
    }

    /// <summary>
    /// Subscribes to relevant events (Score Changes, Specific Upgrade Changes).
    /// </summary>
    protected virtual void SubscribeToEvents()
    {
        if (purchaseButton != null) purchaseButton.onClick.AddListener(HandlePurchaseButtonClick);
        // Subscribe to ScoreManager for score changes
        if (ScoreManager.Instance != null) ScoreManager.Instance.OnScoreChanged += HandleScoreChanged;

        // Derived classes will subscribe to their specific upgrade change events
    }

    /// <summary>
    /// Unsubscribes from events to prevent memory leaks.
    /// </summary>
    protected virtual void UnsubscribeFromEvents()
    {
        if (purchaseButton != null) purchaseButton.onClick.RemoveListener(HandlePurchaseButtonClick);
        // Unsubscribe from ScoreManager
        if (ScoreManager.Instance != null) ScoreManager.Instance.OnScoreChanged -= HandleScoreChanged;

        // Derived classes will unsubscribe from their specific upgrade change events
    }

    /// <summary>
    /// Updates all dynamic UI elements.
    /// </summary>
    protected void UpdateUIDisplay()
    {
        if (CurrentUpgradeState == null)
        {
            // This might happen briefly during initialization or if data is bad
            // Debug.LogWarning($"CurrentUpgradeState is null for {UpgradeData?.name ?? "Unknown"}");
            SetButtonInteractable(false); // Can't purchase if state is unknown
            return;
        }

        if (levelText != null) levelText.text = $"Level: {CurrentUpgradeState.level}";
        if (costText != null) costText.text = $"Cost: {CurrentCost:F0}"; // Format as whole number

        // Let derived classes update their specific UI parts (like production rate)
        UpdateSpecificUI();

        // Update button interactability based on the calculated cost
        UpdatePurchaseButtonInteractability();
    }

    /// <summary>
    /// Updates only the purchase button's interactable state based on current score and cost.
    /// </summary>
    protected void UpdatePurchaseButtonInteractability()
    {
        if (purchaseButton == null || CurrentUpgradeState == null || ScoreManager.Instance == null) return;

        bool canAfford = ScoreManager.Instance.GetCurrentScore() >= CurrentCost;
        SetButtonInteractable(canAfford);
    }

    protected void SetButtonInteractable(bool interactable)
    {
         if (purchaseButton != null) purchaseButton.interactable = interactable;
    }

    // --- Event Handlers ---

    private void HandlePurchaseButtonClick()
    {
        // Prevent accidental double-clicks if processing takes time
        SetButtonInteractable(false); 
        TryPurchaseUpgrade();
        // Button interactability will be updated properly by HandleScoreChanged or HandleUpgradePurchased events
    }

    /// <summary>
    /// Called by ScoreManager when the score changes. Updates button interactability.
    /// </summary>
    private void HandleScoreChanged(decimal newScore)
    {
        UpdatePurchaseButtonInteractability();
    }

    /// <summary>
    /// Abstract handler for when the specific upgrade this UI represents is purchased/changed.
    /// Derived classes implement this to call UpdateUIDisplay.
    /// </summary>
    protected abstract void HandleSpecificUpgradePurchased(UpgradeState purchasedUpgradeState);

    // --- Abstract Getters for Data --- 
    // These allow base class access to derived data without knowing the specific type
    protected abstract string GetUpgradeName();
    protected abstract string GetUpgradeDescription();

} 