using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Globalization;

public class PrestigeUIController : MonoBehaviour
{
    [Header("Navigation/Hub Elements")]
    public GameObject prestigeNavBar; // Assign the GameObject containing the Prestige/PrestigeUpgrades buttons

    [Header("Prestige Trigger UI")]
    public GameObject prestigePanel; // The panel containing the confirmation
    public Button openPrestigePanelButton; // Button to show the panel
    public Button confirmPrestigeButton; // Button inside the panel to confirm
    public Button cancelPrestigeButton; // Button inside the panel to cancel
    public TextMeshProUGUI potentialGainText; // Text showing potential GB gain
    public TextMeshProUGUI requirementsText; // Text showing requirements (e.g., min score)

    [Header("Persistent Displays")]
    public TextMeshProUGUI mainScoreDisplayText; // Text displaying CURRENT MAIN SCORE
    public TextMeshProUGUI goldBarBalanceText; // NEW: Text displaying current Gold Bars
    public TextMeshProUGUI prestigeLevelText; // Text displaying prestige count

    [Header("Prestige Shop UI - Basic Placeholder")]
    public GameObject prestigeShopPanel; // Panel for the shop
    public Button openPrestigeShopButton; // Button to open the shop
    public Button closePrestigeShopButton; // Button to close the shop
    public Transform upgradeListContainer; // Parent transform for upgrade buttons
    public GameObject prestigeUpgradeButtonPrefab; // Prefab for a single upgrade button

    private PrestigeManager _prestigeManager;
    private ScoreManager _scoreManager; // NEW: Reference to ScoreManager
    private bool _isInitialized = false;

    void Start()
    {
        // Initialize might run here if the object starts active
        // but OnEnable is generally more reliable for setup when
        // dealing with scenes loading/objects activating.
        // Initialize(); 
    }

    void OnEnable()
    {
        // This is called when the scene loads or the GameObject becomes active.
        // It's a good place to ensure initialization and UI refresh happens.
        Initialize(); // Ensures manager ref is found and events subscribed
        
        // Explicitly update displays after initializing/subscribing
        // This helps catch the state even if events were missed or Start order was off.
        if (_isInitialized) // Ensure Initialize() didn't fail
        {
             UpdateUIDisplays(); 
        }
    }

    void OnDisable()
    {
        UnsubscribeFromEvents();
    }

    void Initialize()
    {
        _prestigeManager = PrestigeManager.Instance;
        _scoreManager = ScoreManager.Instance; // NEW: Get ScoreManager instance

        if (_prestigeManager == null)
        {
            Debug.LogError("PrestigeUIController: PrestigeManager instance not found!");
            // Keep the UI active but potentially show an error state?
            // gameObject.SetActive(false);
            // return; // Don't return entirely, might still show score
        }
         if (_scoreManager == null) // NEW: Check for ScoreManager
        {
             Debug.LogError("PrestigeUIController: ScoreManager instance not found!");
             // Decide how to handle - maybe disable score display?
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
        if (_prestigeManager != null) 
        {
            // Keep these PrestigeManager subscriptions
            _prestigeManager.OnGoldBarsChanged += UpdateGoldBarDisplay; // NEW
            _prestigeManager.OnPrestigeCountChanged += UpdatePrestigeLevelDisplay;
            _prestigeManager.OnPotentialPrestigeGainCalculated += UpdatePotentialGainDisplay;
            _prestigeManager.OnPrestigeUpgradePurchased += HandleUpgradePurchased; // Refresh shop on purchase
        }
        
        // Subscribe to ScoreManager for main score updates - NEW
        if (_scoreManager != null)
        {
            _scoreManager.OnScoreChanged += UpdateMainScoreDisplay; 
        }
    }

    void UnsubscribeFromEvents()
    {
         if (_prestigeManager != null) 
         {
            // Keep these
            _prestigeManager.OnGoldBarsChanged -= UpdateGoldBarDisplay; // NEW
            _prestigeManager.OnPrestigeCountChanged -= UpdatePrestigeLevelDisplay;
            _prestigeManager.OnPotentialPrestigeGainCalculated -= UpdatePotentialGainDisplay;
            _prestigeManager.OnPrestigeUpgradePurchased -= HandleUpgradePurchased;
         }

        // Unsubscribe from ScoreManager - NEW
         if (_scoreManager != null)
        {
            _scoreManager.OnScoreChanged -= UpdateMainScoreDisplay;
        }
    }

    void UpdateUIDisplays()
    {
        // Update Main Score display
        if (_scoreManager != null)
        {
             UpdateMainScoreDisplay(_scoreManager.GetCurrentScore());
        }
        else if (mainScoreDisplayText != null)
        {
             // Show error or default if ScoreManager is missing
             mainScoreDisplayText.text = "Score N/A";
        }

        // Keep updating Prestige Level and Potential Gain from PrestigeManager
        if (_prestigeManager != null)
        {
            UpdateGoldBarDisplay(_prestigeManager.GetCurrentGoldBars()); // NEW
            UpdatePrestigeLevelDisplay(_prestigeManager.GetCurrentPrestigeCount());
            UpdatePotentialGainDisplay(_prestigeManager.CalculatePotentialGoldBarGain());
            UpdatePrestigeButtonState();
        }
        else 
        {   
            // Handle missing PrestigeManager for other elements if needed
             if (prestigeLevelText != null) prestigeLevelText.text = "Level N/A";
             if (potentialGainText != null) potentialGainText.text = "Gain N/A";
             if (confirmPrestigeButton != null) confirmPrestigeButton.interactable = false;
        }
    }

    // Renamed method
    void UpdateMainScoreDisplay(decimal newScore)
    {
        if (mainScoreDisplayText != null)
            mainScoreDisplayText.text = $"{NumberFormatter.FormatNumber(newScore)}";
    }

    // NEW: Method to update Gold Bar display
    void UpdateGoldBarDisplay(decimal newBalance)
    {
        if (goldBarBalanceText != null)
            goldBarBalanceText.text = $"Gold Bars: {NumberFormatter.FormatNumber(newBalance)}"; // Added prefix
    }

    void UpdatePrestigeLevelDisplay(int count)
    {
        if (prestigeLevelText != null)
            prestigeLevelText.text = $"{count}";
    }

    void UpdatePotentialGainDisplay(decimal potentialGain)
    {
        if (potentialGainText != null)
        {
            // Always format and show the text, even if gain is 0
            potentialGainText.text = $"Potential Gain: +{NumberFormatter.FormatNumber(potentialGain)} GB";
            potentialGainText.gameObject.SetActive(true); // Ensure it's active
        }

        UpdatePrestigeButtonState(); // Update button state when potential gain changes
    }

    void UpdatePrestigeButtonState()
    {
        // Update the interactability of the CONFIRM button INSIDE the panel
        if (confirmPrestigeButton != null && _prestigeManager != null)
        {
             bool canAfford = _prestigeManager.CanAffordPrestige();
             confirmPrestigeButton.interactable = canAfford;
        }

        // Update requirements text inside the panel
         if (requirementsText != null && _prestigeManager != null)
        {
            // Get the dynamically calculated required score
            decimal reqScore = _prestigeManager.GetRequiredScoreForNextPrestige(); 
            string reqScoreFormatted = NumberFormatter.FormatNumber(reqScore);
            requirementsText.text = $"Requires {reqScoreFormatted} Lifetime Score";
        }
    }

    void ShowPrestigePanel()
    {
        prestigeShopPanel?.SetActive(false);
        prestigeNavBar?.SetActive(false);

        UpdatePotentialGainDisplay(_prestigeManager.CalculatePotentialGoldBarGain());
        prestigePanel?.SetActive(true);
    }

    void HidePrestigePanel()
    {
        prestigePanel?.SetActive(false);
        prestigeNavBar?.SetActive(true);
    }

    void HandleConfirmPrestige()
    {
        _prestigeManager?.PerformPrestige();
        HidePrestigePanel(); // Hide the panel after confirming
        // UI should update automatically via events
    }

    void ShowPrestigeShop()
    {
        prestigePanel?.SetActive(false);
        prestigeNavBar?.SetActive(false);

        PopulatePrestigeShop(); 
        prestigeShopPanel?.SetActive(true);
    }

    void HidePrestigeShop()
    {
        prestigeShopPanel?.SetActive(false);
        prestigeNavBar?.SetActive(true);
    }

    void PopulatePrestigeShop()
    {
        if (_prestigeManager == null) Debug.LogError("[PopulatePrestigeShop] PrestigeManager is NULL!");
        if (upgradeListContainer == null) Debug.LogError("[PopulatePrestigeShop] UpgradeListContainer is NULL!");
        if (prestigeUpgradeButtonPrefab == null) Debug.LogError("[PopulatePrestigeShop] PrestigeUpgradeButtonPrefab is NULL!");

        if (_prestigeManager == null || upgradeListContainer == null || prestigeUpgradeButtonPrefab == null) 
        {
             Debug.LogWarning("[PopulatePrestigeShop] Exiting early due to null reference.");
             return;
        }

        foreach (Transform child in upgradeListContainer)
        {
            Destroy(child.gameObject);
        }

        if (_prestigeManager.availablePrestigeUpgradesData == null)
        {
             Debug.LogError("[PopulatePrestigeShop] PrestigeManager.availablePrestigeUpgradesData is NULL!");
             return;
        }

        foreach (var upgradeData in _prestigeManager.availablePrestigeUpgradesData)
        {
            if (upgradeData == null) 
            {
                Debug.LogWarning("[PopulatePrestigeShop] Found NULL entry in upgrade data list. Skipping.");
                continue;
            }

            GameObject buttonGO = Instantiate(prestigeUpgradeButtonPrefab, upgradeListContainer);
            PrestigeUpgradeButtonUI buttonUI = buttonGO.GetComponent<PrestigeUpgradeButtonUI>();
            if (buttonUI != null)
            {
                buttonUI.Setup(upgradeData, _prestigeManager);
            }
            else
            {
                Debug.LogError($"[PopulatePrestigeShop] Instantiated PrestigeUpgradeButtonPrefab is MISSING PrestigeUpgradeButtonUI component! Prefab: {prestigeUpgradeButtonPrefab.name}", buttonGO);
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