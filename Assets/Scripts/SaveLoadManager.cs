using UnityEngine;
using System.Globalization;
using System.IO; // Required for File operations
using System.Collections.Generic; // Required for List
using System;

// Handles saving, loading, and resetting game data using encrypted files.
public class SaveLoadManager : MonoBehaviour
{
    public static SaveLoadManager Instance { get; private set; }

    // References to managers whose data needs saving/loading
    private ScoreManager _scoreManager;
    private ClickUpgradeManager _clickUpgradeManager;
    private ProductionManager _productionManager; // Optional
    private PrestigeManager _prestigeManager; // NEW

    // --- Configuration ---
    private string saveFileName = "gameSave.dat"; // Name of the encrypted save file
    private string saveFilePath;
    private long _loadedTimestampTicks = 0; // NEW: Store timestamp after load

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Determine the save file path
        saveFilePath = Path.Combine(Application.persistentDataPath, saveFileName);
        // Debug.Log($"Save file path: {saveFilePath}");
    }

    /// <summary>
    /// Called by GameManager during Awake to find manager references
    /// and then coordinate the initial loading sequence.
    /// </summary>
    public void InitializeManagers(ScoreManager sm, ClickUpgradeManager cum, ProductionManager pm, PrestigeManager prs)
    {
        _scoreManager = sm;
        _clickUpgradeManager = cum;
        _productionManager = pm;
        _prestigeManager = prs; // NEW

        // Pass manager references needed by PrestigeManager
        _prestigeManager?.InitializeManagers(_scoreManager, _clickUpgradeManager, _productionManager, this);

        // Load sequence
        LoadGameData();
    }

    /// <summary>
    /// Loads game data from the encrypted save file.
    /// If the file doesn't exist or is invalid, loads default state.
    /// </summary>
    void LoadGameData()
    {
        Debug.Log("[Load] SaveLoadManager: Attempting to load game data...");
        SaveData loadedData = null;
        string json = null; // Store json result for logging

        if (File.Exists(saveFilePath))
        {
            Debug.Log($"[Load] Save file found at {saveFilePath}");
            try
            {
                Debug.Log("[Load] Reading encrypted bytes...");
                byte[] encryptedData = File.ReadAllBytes(saveFilePath);
                Debug.Log($"[Load] Read {encryptedData?.Length ?? 0} encrypted bytes. Decrypting...");

                json = EncryptionUtility.Decrypt(encryptedData);

                if (!string.IsNullOrEmpty(json))
                {
                    Debug.Log("[Load] Decryption successful. Deserializing JSON...");
                    // Debug.Log($"[Load] Decrypted JSON: {json}"); // Uncomment cautiously for debugging, might be large
                    loadedData = JsonUtility.FromJson<SaveData>(json);
                     Debug.Log("[Load] JSON Deserialized successfully.");
                }
                else
                {
                     // Error already logged by EncryptionUtility if decryption itself failed
                     Debug.LogError("[Load] Decryption returned null or empty string. Loading default game state.");
                     loadedData = new SaveData(); // Ensure defaults on decryption failure
                }
            }
            catch (ArgumentException argEx) // Specific exception for JsonUtility failures
            {
                Debug.LogError($"[Load] Error deserializing JSON: {argEx.Message}. JSON content was: \n{json ?? "<Decryption Failed>"}\nLoading default game state.");
                loadedData = new SaveData(); // Ensure we load defaults on JSON error
            }
            catch (Exception ex) // Catch other potential errors (IO, etc.)
            {
                 Debug.LogError($"[Load] General error loading/processing save file: {ex.Message}. Loading default game state.");
                 loadedData = null; // Ensure we fall through to default creation
            }
        }
        else
        {
             Debug.Log($"[Load] No save file found at {saveFilePath}. Loading default game state.");
             loadedData = new SaveData(); // Create a default SaveData object for initialization
        }

         // If loadedData is still null after attempts (e.g., general exception), create a default one
         if (loadedData == null) { 
             loadedData = new SaveData();
             Debug.Log("[Load] Instantiated default SaveData because loadedData was null.");
         }

        // Distribute loaded data (or default data) to managers
        Debug.Log("[Load] Initializing ScoreManager...");
        _scoreManager?.LoadData(loadedData);
        Debug.Log("[Load] Initializing ClickUpgradeManager...");
        _clickUpgradeManager?.LoadData(loadedData);
        Debug.Log("[Load] Initializing ProductionManager...");
        _productionManager?.LoadData(loadedData);
        Debug.Log("[Load] Initializing PrestigeManager..."); // NEW
        _prestigeManager?.LoadData(loadedData); // NEW

        // NEW: Store the loaded timestamp
        _loadedTimestampTicks = loadedData?.lastSaveTimestampTicks ?? 0;

        Debug.Log("[Load] SaveLoadManager: LoadGameData finished. Managers initialized with loaded/default data.");
    }

    // NEW: Getter for the loaded timestamp
    public long GetLastLoadedTimestampTicks()
    {
        return _loadedTimestampTicks;
    }

    /// <summary>
    /// Gathers data from managers, serializes, encrypts, and saves it to a file.
    /// </summary>
    public void SaveGameData()
    {
        Debug.Log("SaveLoadManager: Saving game data...");

        // 1. Gather data from managers
        SaveData dataToSave = new SaveData();

        // Call UpdateSaveData on managers that need to populate the SaveData object
        _scoreManager?.UpdateSaveData(dataToSave);             // Needs UpdateSaveData method
        _clickUpgradeManager?.UpdateSaveData(dataToSave);      // Needs UpdateSaveData method
        _productionManager?.UpdateSaveData(dataToSave);        // Needs UpdateSaveData method
        _prestigeManager?.UpdateSaveData(dataToSave); // Call new method

        // NEW: Record the current time before saving
        dataToSave.lastSaveTimestampTicks = DateTime.UtcNow.Ticks;

        // 2. Serialize to JSON
        string json = JsonUtility.ToJson(dataToSave, true); // Use pretty print for debug, false for release

        // 3. Encrypt JSON
        byte[] encryptedData = EncryptionUtility.Encrypt(json);

        // 4. Write encrypted data to file
        if (encryptedData != null)
        {
            try
            {
                File.WriteAllBytes(saveFilePath, encryptedData);
                 Debug.Log($"SaveLoadManager: Game data encrypted and saved successfully to {saveFilePath}");
            }
            catch (Exception ex)
            {
                 Debug.LogError($"SaveLoadManager: Failed to write save file: {ex.Message}");
            }
        }
        else
        {
             Debug.LogError("SaveLoadManager: Encryption failed. Game data not saved.");
        }

         // REMOVED: PlayerPrefs.Save();
    }

    /// <summary>
    /// Deletes the save file. Called by GameManager during Hard Reset.
    /// </summary>
    public void DeleteSaveFile()
    {
        if (File.Exists(saveFilePath))
        {
            try
            {
                File.Delete(saveFilePath);
                Debug.Log($"SaveLoadManager: Encrypted save file deleted: {saveFilePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"SaveLoadManager: Failed to delete save file: {ex.Message}");
            }
        }
        else
        {
            Debug.Log("SaveLoadManager: No save file found to delete.");
        }
    }
} 