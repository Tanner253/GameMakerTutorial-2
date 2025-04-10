using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Globalization;

public class PrestigeUIController : MonoBehaviour
{
    [Header("Prestige Trigger UI")]
    public GameObject prestigePanel; // The panel containing the confirmation
    public Button openPrestigePanelButton; // Button to show the panel
    public Button confirmPrestigeButton; // Button inside the panel to confirm
    public Button cancelPrestigeButton; // Button inside the panel to cancel
    public TextMeshProUGUI potentialGainText; // Text showing potential GB gain
    public TextMeshProUGUI requirementsText; // Text showing requirements (e.g., min score)

    [Header("Persistent Displays")]
    public TextMeshProUGUI goldBarBalanceText; // Text displaying current GB
    public TextMeshProUGUI prestigeLevelText; // Text displaying prestige count

    [Header("Prestige Shop UI - Basic Placeholder")]
    public GameObject prestigeShopPanel; // Panel for the shop
    public Button openPrestigeShopButton; // Button to open the shop
    public Button closePrestigeShopButton; // Button to close the shop
    public Transform upgradeListContainer; // Parent transform for upgrade buttons
    public GameObject prestigeUpgradeButtonPrefab; // Prefab for a single upgrade button

    private PrestigeManager _prestigeManager;
    private bool _isInitialized = false;

    void Start()
    {
        Initialize();
    }

    void OnEnable()
    {
        // Re-subscribe events if panel was disabled/re-enabled
        if (_isInitialized)
        {
            SubscribeToEvents();
            UpdateUIDisplays(); // Refresh UI
        }
        else
        {
             Initialize(); // Ensure initialization if Start hasn't run yet
        }
    }

    void OnDisable()
    {
        UnsubscribeFromEvents();
    }

    void Initialize()
    {
        _prestigeManager = PrestigeManager.Instance;
        if (_prestigeManager == null)
        {
            Debug.LogError("PrestigeUIController: PrestigeManager instance not found!");
            gameObject.SetActive(false);
            return;
        }

        // Setup button listeners
        openPrestigePanelButton?.onClick.AddListener(ShowPrestigePanel);
        confirmPrestigeButton?.onClick.AddListener(HandleConfirmPrestige);
        cancelPrestigeButton?.onClick.AddListener(HidePrestigePanel);
        openPrestigeShopButton?.onClick.AddListener(ShowPrestigeShop);
        closePrestigeShopButton?.onClick.AddListener(HidePrestigeShop);

        // Initial UI state
        prestigePanel?.SetActive(false);
        prestigeShopPanel?.SetActive(false);

        SubscribeToEvents();
        UpdateUIDisplays();
        PopulatePrestigeShop(); // Initial population

        _isInitialized = true;
    }

    void SubscribeToEvents()
    {
        if (_prestigeManager == null) return;
        _prestigeManager.OnGoldBarsChanged += UpdateGoldBarDisplay;
        _prestigeManager.OnPrestigeCountChanged += UpdatePrestigeLevelDisplay;
        _prestigeManager.OnPotentialPrestigeGainCalculated += UpdatePotentialGainDisplay;
        _prestigeManager.OnPrestigeUpgradePurchased += HandleUpgradePurchased; // Refresh shop on purchase
    }

    void UnsubscribeFromEvents()
    {
         if (_prestigeManager == null) return;
        _prestigeManager.OnGoldBarsChanged -= UpdateGoldBarDisplay;
        _prestigeManager.OnPrestigeCountChanged -= UpdatePrestigeLevelDisplay;
        _prestigeManager.OnPotentialPrestigeGainCalculated -= UpdatePotentialGainDisplay;
        _prestigeManager.OnPrestigeUpgradePurchased -= HandleUpgradePurchased;
    }

    void UpdateUIDisplays()
    {
        if (_prestigeManager == null) return;
        UpdateGoldBarDisplay(_prestigeManager.GetCurrentGoldBars());
        UpdatePrestigeLevelDisplay(_prestigeManager.GetCurrentPrestigeCount());
        UpdatePotentialGainDisplay(_prestigeManager.CalculatePotentialGoldBarGain());
        UpdatePrestigeButtonState();
    }

    void UpdateGoldBarDisplay(decimal newBalance)
    {
        if (goldBarBalanceText != null)
            goldBarBalanceText.text = $"Gold Bars: {NumberFormatter.FormatNumber(newBalance)}";
    }

    void UpdatePrestigeLevelDisplay(int count)
    {
        if (prestigeLevelText != null)
            prestigeLevelText.text = $"Prestige Level: {count}";
    }

    void UpdatePotentialGainDisplay(decimal potentialGain)
    {
        if (potentialGainText != null)
            potentialGainText.text = $"Potential Gain: +{NumberFormatter.FormatNumber(potentialGain)} GB";

        UpdatePrestigeButtonState(); // Update button state when potential gain changes
    }

    void UpdatePrestigeButtonState()
    {
        // Update visibility/interactability of the main prestige trigger button
        if (openPrestigePanelButton != null && _prestigeManager != null)
        {
            bool canPrestige = _prestigeManager.CanAffordPrestige();
            openPrestigePanelButton.interactable = canPrestige;
            // Optionally hide button if cannot prestige: openPrestigePanelButton.gameObject.SetActive(canPrestige);
        }

        // Update requirements text inside the panel
         if (requirementsText != null && _prestigeManager != null)
        {
            string reqScoreFormatted = NumberFormatter.FormatNumber(_prestigeManager.minScoreToPrestige);
            requirementsText.text = $"Requires {reqScoreFormatted} Lifetime Score";
        }
    }

    void ShowPrestigePanel()
    {
        UpdatePotentialGainDisplay(_prestigeManager.CalculatePotentialGoldBarGain()); // Ensure gain text is up-to-date
        prestigePanel?.SetActive(true);
        // Maybe pause game time here if desired
    }

    void HidePrestigePanel()
    {
        prestigePanel?.SetActive(false);
        // Maybe resume game time here
    }

    void HandleConfirmPrestige()
    {
        _prestigeManager?.PerformPrestige();
        HidePrestigePanel();
        // UI should update automatically via events
    }

    void ShowPrestigeShop()
    {
        PopulatePrestigeShop(); // Refresh shop contents
        prestigeShopPanel?.SetActive(true);
    }

    void HidePrestigeShop()
    {
        prestigeShopPanel?.SetActive(false);
    }

    void PopulatePrestigeShop()
    {
        if (_prestigeManager == null || upgradeListContainer == null || prestigeUpgradeButtonPrefab == null) return;

        // Clear existing buttons
        foreach (Transform child in upgradeListContainer)
        {
            Destroy(child.gameObject);
        }

        // Instantiate buttons for each available upgrade
        foreach (var upgradeData in _prestigeManager.availablePrestigeUpgradesData)
        {
            if (upgradeData == null) continue;

            GameObject buttonGO = Instantiate(prestigeUpgradeButtonPrefab, upgradeListContainer);
            PrestigeUpgradeButtonUI buttonUI = buttonGO.GetComponent<PrestigeUpgradeButtonUI>();
            if (buttonUI != null)
            {
                buttonUI.Setup(upgradeData, _prestigeManager);
            }
            else
            {
                Debug.LogError($"PrestigeUpgradeButtonPrefab is missing PrestigeUpgradeButtonUI component!", buttonGO);
            }
        }
    }

    // Called when an upgrade is purchased to refresh the shop UI state
    void HandleUpgradePurchased(PrestigeUpgradeData purchasedData, int newLevel)
    {
        // Simple refresh: Repopulate the whole shop
        // More complex logic could just update the specific button
        if (prestigeShopPanel != null && prestigeShopPanel.activeInHierarchy)
        {
            PopulatePrestigeShop();
        }
    }
} 