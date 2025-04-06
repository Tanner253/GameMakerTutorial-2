using UnityEngine;
using System.Globalization;

// Handles saving, loading, and resetting game data
public class SaveLoadManager : MonoBehaviour
{
    public static SaveLoadManager Instance { get; private set; }

    // References to managers whose data needs saving/loading
    private ScoreManager _scoreManager;
    private ClickUpgradeManager _clickUpgradeManager;
    private ProductionManager _productionManager; // Optional

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // Optional
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Called by GameManager during Awake to find manager references
    /// and then coordinate the initial loading sequence.
    /// </summary>
    public void InitializeManagers()
    {
        _scoreManager = ScoreManager.Instance;
        _clickUpgradeManager = ClickUpgradeManager.Instance;
        _productionManager = ProductionManager.Instance; // Might be null

        // Load sequence
        LoadGameData();
    }

    /// <summary>
    /// Loads all game data by calling the load methods on respective managers.
    /// </summary>
    void LoadGameData()
    {
        Debug.Log("SaveLoadManager: Starting LoadGameData...");

        // Load Score first (as it doesn't depend on others initially)
        decimal loadedScore = _scoreManager?.LoadScore() ?? 0M;
        _scoreManager?.Initialize(loadedScore); // Initialize score manager with loaded value
        Debug.Log($"SaveLoadManager: Score loaded: {loadedScore}");

        // Load Click Upgrades (initializes levels and recalculates click value)
        _clickUpgradeManager?.LoadClickUpgrades();
        Debug.Log("SaveLoadManager: Click Upgrades load initiated.");

        // Load Production Upgrades (initializes levels and recalculates production rate)
        _productionManager?.LoadProductionUpgrades();
        Debug.Log("SaveLoadManager: Production Upgrades load initiated (if manager exists).");

        Debug.Log("SaveLoadManager: LoadGameData finished.");
        // Managers handle their own UI updates upon loading via events
    }

    /// <summary>
    /// Saves all game data by calling the save methods on respective managers.
    /// </summary>
    public void SaveGameData()
    {
        Debug.Log("SaveLoadManager: Saving game data...");
        _scoreManager?.SaveScore();
        _clickUpgradeManager?.SaveClickUpgrades();
        _productionManager?.SaveProductionUpgrades();

        PlayerPrefs.Save(); // Ensure all changes are written to disk
        Debug.Log("SaveLoadManager: Game data saved.");
    }

    /// <summary>
    /// Resets all game data by calling reset methods on respective managers
    /// and clearing PlayerPrefs.
    /// </summary>
    public void ResetAllGameData()
    {
        Debug.Log("SaveLoadManager: --- Resetting All Game Data --- ");

        // Reset runtime state in managers first
        _scoreManager?.ResetScore();
        _clickUpgradeManager?.ResetClickUpgrades();
        _productionManager?.ResetProductionUpgrades();

        // Clear all saved data
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("SaveLoadManager: All PlayerPrefs deleted.");

        Debug.Log("SaveLoadManager: Reset complete.");

        // Note: Scene reload is NOT handled here. If required, GameManager should do it.
        // GameManager is responsible for updating any UI elements (like production rate)
        // that depend on the now-reset state.
    }
} 