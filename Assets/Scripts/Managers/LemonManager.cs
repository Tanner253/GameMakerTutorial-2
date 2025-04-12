using System;
using System.Collections; // Required for Coroutines
using UnityEngine;
using UnityEngine.UI; // Needed for RectTransformUtility
using UnityEngine.SceneManagement; // Needed for scene management

public class LemonManager : MonoBehaviour
{
    public static LemonManager Instance { get; private set; }

    [Header("Configuration - Base Values")]
    [SerializeField] private GameObject lemonPrefab; // Assign your Lemon Prefab
    [SerializeField] private float baseMinSpawnTime = 60f; // Base 60 seconds (1 minute)
    [SerializeField] private float baseMaxSpawnTime = 180f; // Base 180 seconds (3 minutes)
    [SerializeField] private float baseLifespan = 15f;     // Base seconds lemon stays (fallback if not specified in upgrade data)
    [SerializeField] private float baseRewardMinutes = 5f; // Base reward = N minutes of CPS (Adjusted default)
    // Removed SerializeField for Spawn Area - we'll find it dynamically
    private RectTransform spawnArea;    // Will be found dynamically
    [SerializeField] private string spawnAreaObjectName = "LemonSpawnArea"; // Name of the GO with the RectTransform
    [SerializeField] private string mainGameSceneName = "Main"; // Name of your main game scene

    [Header("Runtime Debug")]
    [SerializeField] private float timeToNextSpawn = 0f;
    [SerializeField] private bool isSpawnTimerRunning = false;
    [SerializeField] private bool lemonsUnlocked = false; // Track if feature is unlocked

    // Manager References
    private ProductionManager _productionManager;
    private ScoreManager _scoreManager;
    private FloatingTextManager _floatingTextManager;
    private PrestigeManager _prestigeManager; // Reference to get bonuses

    // Cached values modified by prestige
    private float _currentMinSpawnTime;
    private float _currentMaxSpawnTime;
    private float _currentLifespan;
    private float _currentRewardMinutes;

    [Header("Debug Stats - Read Only")] // Add a header for clarity
    [SerializeField] private float DEBUG_currentMinSpawnTime;
    [SerializeField] private float DEBUG_currentMaxSpawnTime;
    [SerializeField] private float DEBUG_currentLifespan;
    [SerializeField] private float DEBUG_currentRewardMinutes;
    [SerializeField] private bool DEBUG_lemonsUnlocked;

    void Awake()
    {
        // --- Singleton Pattern ---
        if (Instance == null)
        {
            Instance = this;
            // Removed DontDestroyOnLoad - Assuming LemonManager lives with GameManager
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // Find required managers
        _productionManager = ProductionManager.Instance;
        _scoreManager = ScoreManager.Instance;
        _floatingTextManager = FloatingTextManager.Instance;
        _prestigeManager = PrestigeManager.Instance; // Get Prestige Manager

        // Validate dependencies early
        if (!ValidateDependencies()) return;

        // Subscribe to prestige events (upgrade purchase and data loaded)
        if (_prestigeManager != null) {
            _prestigeManager.OnPrestigeUpgradePurchased += HandlePrestigeUpgradePurchased;
            _prestigeManager.OnPrestigeDataLoaded += HandlePrestigeDataLoaded; // Subscribe to the new event
        }

        // ADDED: Check immediately if lemons should be unlocked, don't just wait for the event
        // This handles the case where PrestigeManager already loaded data before we subscribed
        UpdateLemonStatsFromPrestige();
        
        // Check if we should start the timer now based on the current state
        if (lemonsUnlocked && !isSpawnTimerRunning && spawnArea != null)
        {
            Debug.Log("[LemonManager] Lemons are unlocked during Start. Starting spawn timer.");
            StartSpawnTimer();
        }
        else if (lemonsUnlocked && spawnArea == null)
        {
            Debug.Log("[LemonManager] Lemons are unlocked but spawn area not found yet. Will attempt to start timer after scene load.");
        }
    }

    bool ValidateDependencies()
    {
        bool valid = true;
        if (_productionManager == null) { Debug.LogError("[LemonManager] ProductionManager missing!"); valid = false; }
        if (_scoreManager == null) { Debug.LogError("[LemonManager] ScoreManager missing!"); valid = false; }
        if (_floatingTextManager == null) { Debug.LogError("[LemonManager] FloatingTextManager missing!"); valid = false; }
        if (_prestigeManager == null) { Debug.LogError("[LemonManager] PrestigeManager missing!"); valid = false; }
        if (lemonPrefab == null) { Debug.LogError("[LemonManager] Lemon Prefab missing!"); valid = false; }
        
        // Don't fail validation if spawnArea is null - it will be found in the scene later
        if (spawnArea == null) { 
            Debug.LogWarning("[LemonManager] Spawn Area not found yet. Will attempt to find after scene load."); 
        }

        if (!valid) this.enabled = false;
        return valid;
    }

    void OnEnable() // Subscribe when the object becomes enabled
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        // Attempt initial find in case already in correct scene when enabled
        FindAndAssignSpawnArea(SceneManager.GetActiveScene());
    }

    void OnDisable() // Unsubscribe when the object becomes disabled
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        // Unsubscribe from PrestigeManager events
        if (_prestigeManager != null) {
            _prestigeManager.OnPrestigeUpgradePurchased -= HandlePrestigeUpgradePurchased;
            _prestigeManager.OnPrestigeDataLoaded -= HandlePrestigeDataLoaded; // Unsubscribe from the new event
        }
    }

    // Called by SceneManager when a scene finishes loading
    void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindAndAssignSpawnArea(scene);

        // After finding spawn area, check if lemons are unlocked and start timer if needed
        if (lemonsUnlocked && !isSpawnTimerRunning && spawnArea != null)
        {
            Debug.Log("[LemonManager] Scene loaded with lemons unlocked. Starting spawn timer.");
            StartSpawnTimer();
        }
        else if (lemonsUnlocked && spawnArea == null && scene.name == mainGameSceneName)
        {
            Debug.LogWarning("[LemonManager] Lemons are unlocked but spawn area not found in main scene. Check that GameObject named '" + spawnAreaObjectName + "' exists.");
        }
    }

    // Helper method to find and assign the spawn area
    void FindAndAssignSpawnArea(Scene scene)
    {
         // Only try to find it in the main game scene
        if (scene.name == mainGameSceneName)
        {
            GameObject spawnAreaGO = GameObject.Find(spawnAreaObjectName);
            if (spawnAreaGO != null)
            {
                spawnArea = spawnAreaGO.GetComponent<RectTransform>();
                if (spawnArea == null)
                {
                    Debug.LogError($"[LemonManager] Found '{spawnAreaObjectName}' but it has no RectTransform component!");
                }
                else
                {
                    Debug.Log($"[LemonManager] Successfully found and assigned Spawn Area '{spawnAreaObjectName}'.");
                }
            }
            else
            {
                 // Clear reference if object not found in this scene
                 spawnArea = null;
                 Debug.LogWarning($"[LemonManager] Could not find GameObject named '{spawnAreaObjectName}' in scene '{scene.name}'. Lemons cannot spawn without it.");
            }
        }
        else
        {
            // If not in the main game scene, clear the reference
            spawnArea = null;
        }
    }

    // Handler for when PrestigeManager finishes loading its data
    void HandlePrestigeDataLoaded()
    {
        Debug.Log("[LemonManager] Received OnPrestigeDataLoaded event. Updating stats...");
        
        // Store previous state for debugging
        bool wasUnlocked = lemonsUnlocked;
        
        // Update lemon stats based on prestige data
        UpdateLemonStatsFromPrestige();

        // Log detailed information for debugging
        Debug.Log($"[LemonManager] HandlePrestigeDataLoaded - Before: {wasUnlocked}, After: {lemonsUnlocked}, " +
                  $"IsFeatureUnlocked check: {(_prestigeManager != null ? _prestigeManager.IsFeatureUnlocked(_prestigeManager.UnlockLemonsUpgradeData) : false)}, " +
                  $"UnlockLemonsUpgradeData: {(_prestigeManager?.UnlockLemonsUpgradeData != null ? _prestigeManager.UnlockLemonsUpgradeData.name : "null")}");

        // If state changed from locked to unlocked, announce it
        if (!wasUnlocked && lemonsUnlocked)
        {
            Debug.Log("[LemonManager] Lemons have been unlocked after loading prestige data!");
        }

        // Check if we should start the timer now that data is loaded
        if (lemonsUnlocked && !isSpawnTimerRunning && spawnArea != null)
        {
            Debug.Log("[LemonManager] Starting lemon spawn timer after prestige data loaded.");
            StartSpawnTimer();
        }
        else if (lemonsUnlocked && spawnArea == null)
        {
            Debug.Log("[LemonManager] Lemons are unlocked but spawn area not found yet. Will attempt to start timer after scene load.");
        }
    }

    // Method to update lemon stats based on prestige bonuses
    public void UpdateLemonStatsFromPrestige()
    {
        if (_prestigeManager == null) return;

        // Check unlock status first
        // IMPORTANT: Assumes the 'unlockLemonsUpgradeData' field is assigned in PrestigeManager Inspector
        lemonsUnlocked = _prestigeManager.IsFeatureUnlocked(_prestigeManager.UnlockLemonsUpgradeData);

        if (!lemonsUnlocked) {
             isSpawnTimerRunning = false; // Ensure timer stops if feature becomes locked somehow
             return; // No need to calculate stats if locked
        }

        // Calculate modified values
        float spawnTimeReduction = _prestigeManager.GetTotalLemonSpawnTimeReduction(); // Expected negative value
        _currentMinSpawnTime = Mathf.Max(10f, baseMinSpawnTime + spawnTimeReduction); // Ensure min spawn time doesn't go below 10s
        _currentMaxSpawnTime = Mathf.Max(30f, baseMaxSpawnTime + spawnTimeReduction); // Ensure max spawn time doesn't go below 30s
        // Ensure min is never greater than max
        if (_currentMinSpawnTime > _currentMaxSpawnTime) _currentMinSpawnTime = _currentMaxSpawnTime - 1f;

        // Get base lifespan from prestige data if available, otherwise use the default value
        var (baseLifespanValue, maxLifespanValue) = _prestigeManager.GetLemonLifespanRange();
        float actualBaseLifespan = baseLifespanValue > 0 ? baseLifespanValue : baseLifespan;
        
        // Apply bonuses to the base lifespan
        _currentLifespan = actualBaseLifespan + _prestigeManager.GetTotalLemonLifespanBonusSeconds();

        // Make sure we don't exceed the maximum lifespan if one is defined
        if (maxLifespanValue < float.MaxValue)
        {
            _currentLifespan = Mathf.Min(_currentLifespan, maxLifespanValue);
            Debug.Log($"[LemonManager] Limiting lifespan to max value: {maxLifespanValue:F1}s");
        }

        _currentRewardMinutes = baseRewardMinutes + _prestigeManager.GetTotalLemonValueBonusMinutes();

        // Update debug fields
        DEBUG_lemonsUnlocked = lemonsUnlocked;
        DEBUG_currentMinSpawnTime = _currentMinSpawnTime;
        DEBUG_currentMaxSpawnTime = _currentMaxSpawnTime;
        DEBUG_currentLifespan = _currentLifespan;
        DEBUG_currentRewardMinutes = _currentRewardMinutes;

        Debug.Log($"[LemonManager] Stats Updated: Unlock={lemonsUnlocked}, SpawnTime=[{_currentMinSpawnTime:F1}-{_currentMaxSpawnTime:F1}], Lifespan={_currentLifespan:F1} (Base={actualBaseLifespan:F1}), RewardMins={_currentRewardMinutes:F1}");
    }

    void Update()
    {
        if (!lemonsUnlocked || !isSpawnTimerRunning) return; // Don't run timer if locked or not running

        timeToNextSpawn -= Time.deltaTime;

        if (timeToNextSpawn <= 0)
        {
            SpawnLemon();
        }
    }

    void StartSpawnTimer()
    {
        if (!lemonsUnlocked) return; // Safety check

        // Use current (potentially modified) spawn times
        timeToNextSpawn = UnityEngine.Random.Range(_currentMinSpawnTime, _currentMaxSpawnTime);
        isSpawnTimerRunning = true;
        Debug.Log($"[LemonManager] Next lemon in {timeToNextSpawn:F1} seconds (Using range [{_currentMinSpawnTime:F1}-{_currentMaxSpawnTime:F1}]).");
    }

    void SpawnLemon()
    {
        isSpawnTimerRunning = false; // Stop timer until reset

        if (lemonPrefab == null || spawnArea == null) // Keep basic null checks
        {
            Debug.LogError("[LemonManager] Cannot spawn lemon - prefab or spawn area is null.");
            StartSpawnTimer(); // Try again later
            return;
        }

        Rect spawnRect = spawnArea.rect;
        float spawnX = UnityEngine.Random.Range(spawnRect.xMin, spawnRect.xMax);
        float spawnY = spawnRect.yMax + 50f;
        Vector2 spawnPosition = new Vector2(spawnX, spawnY);

        GameObject lemonGO = Instantiate(lemonPrefab, spawnArea);

        if (lemonGO.transform is RectTransform lemonRect)
        {
            lemonRect.anchoredPosition = spawnPosition;
            // Removed spawn position log, redundant with timer log

            LemonBehavior lemonBehavior = lemonGO.GetComponent<LemonBehavior>();
            if (lemonBehavior != null)
            {
                // Initialize with CURRENT lifespan
                lemonBehavior.Initialize(this, _currentLifespan);

                Rigidbody2D rb = lemonGO.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    float torqueAmount = 10f;
                    rb.AddTorque(UnityEngine.Random.Range(-torqueAmount, torqueAmount), ForceMode2D.Impulse);
                }
                else
                {
                    Debug.LogWarning("[LemonManager] Spawned Lemon Prefab missing Rigidbody2D.", lemonGO);
                }
            }
            else
            {
                Debug.LogError("[LemonManager] Spawned Lemon Prefab missing LemonBehavior script!", lemonGO);
                Destroy(lemonGO);
                StartSpawnTimer(); // Restart timer after failed spawn
                return;
            }
        }
        else
        {
            Debug.LogError("[LemonManager] Lemon Prefab needs a RectTransform!", lemonGO);
            Destroy(lemonGO);
            StartSpawnTimer(); // Restart timer after failed spawn
            return;
        }

        StartSpawnTimer(); // Reset timer for the next spawn
    }

    public void LemonClicked(LemonBehavior lemon)
    {
        if (_productionManager == null || _scoreManager == null || _floatingTextManager == null) return;

        // Calculate Reward using CURRENT reward minutes
        decimal currentCPS = _productionManager.GetTotalProductionRatePerSecond();
        decimal reward = currentCPS * (decimal)(_currentRewardMinutes * 60f); // Use modified value

        _scoreManager.AddScore(reward);
        AudioManager.Instance?.PlayCollectSound();

        Debug.Log(
            $"[LemonManager] Lemon clicked! Awarded {NumberFormatter.FormatNumber(reward)} nuggets ({_currentRewardMinutes} mins of CPS: {NumberFormatter.FormatNumber(currentCPS)}/s)" // Log current reward mins
        );

        Vector2 lemonScreenPos = RectTransformUtility.WorldToScreenPoint(null, lemon.transform.position);
        _floatingTextManager.ShowFloatingText(reward, lemonScreenPos, Color.yellow);
    }

    public void LemonExpired(LemonBehavior lemon)
    {
        Debug.Log("[LemonManager] A lemon expired.");
    }

    // Need to call UpdateLemonStatsFromPrestige() when prestige upgrades are loaded or purchased.
    // This might require an event from PrestigeManager or GameManager.

    // NEW: Event handler to update stats when ANY prestige upgrade is bought
    private void HandlePrestigeUpgradePurchased(PrestigeUpgradeData purchasedData, int newLevel)
    {
        Debug.Log("[LemonManager] Received OnPrestigeUpgradePurchased event. Updating stats...");
        UpdateLemonStatsFromPrestige();

        // Start the timer immediately if this purchase unlocked lemons and the timer isn't already running
        if (lemonsUnlocked && !isSpawnTimerRunning && purchasedData == _prestigeManager.UnlockLemonsUpgradeData && newLevel > 0)
        {
             Debug.Log("[LemonManager] Lemons just unlocked by purchase. Starting spawn timer.");
             StartSpawnTimer();
        }
    }

    void OnDestroy()
    {
        if (_prestigeManager != null)
        {
            _prestigeManager.OnPrestigeUpgradePurchased -= HandlePrestigeUpgradePurchased;
            _prestigeManager.OnPrestigeDataLoaded -= HandlePrestigeDataLoaded;
        }
        
        // Also unsubscribe from scene loaded event if we haven't already
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    // Public accessor methods for UI display
    public float GetCurrentMinSpawnTime()
    {
        return _currentMinSpawnTime;
    }

    public float GetCurrentMaxSpawnTime()
    {
        return _currentMaxSpawnTime;
    }

    // Get the base reduction value (useful for UI)
    public float GetBaseSpawnTimeReduction()
    {
        return baseMinSpawnTime - _currentMinSpawnTime;
    }
}
