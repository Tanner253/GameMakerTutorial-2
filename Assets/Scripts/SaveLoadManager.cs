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

    [Header("Save File Configuration")]
    [SerializeField] private string saveFileName = "gameSave.dat";
    [SerializeField] private string backupSaveFileName = "gameSave_backup.dat";
    [SerializeField] private bool enableBackups = true;
    [SerializeField] private bool createSaveFileOnInit = true;
    
    // --- Configuration ---
    private string saveFilePath;
    private string backupSaveFilePath;
    private long _loadedTimestampTicks = 0; // Store timestamp after load
    
    // For additional security - track last known valid state
    private SaveData _lastKnownGoodSaveData = null;
    private bool _isSaving = false; // Lock to prevent simultaneous save operations
    
    // Event for notifying other systems about successful loads
    public event Action<SaveData> OnSaveDataLoaded;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Debug.LogWarning("[SaveLoadManager] Duplicate instance found. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        // Determine the save file paths
        saveFilePath = Path.Combine(Application.persistentDataPath, saveFileName);
        backupSaveFilePath = Path.Combine(Application.persistentDataPath, backupSaveFileName);
        
        Debug.Log($"[SaveLoadManager] Save files will be at:\n - Primary: {saveFilePath}\n - Backup: {backupSaveFilePath}");
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
        
        // Create initial save if option enabled and no save exists
        if (createSaveFileOnInit && !File.Exists(saveFilePath) && _lastKnownGoodSaveData != null)
        {
            SaveGameData();
        }
    }

    /// <summary>
    /// Loads game data from the encrypted save file with backup support.
    /// If the primary save is corrupted, tries the backup.
    /// </summary>
    public void LoadGameData()
    {
        Debug.Log("[Load] SaveLoadManager: Attempting to load game data...");
        SaveData loadedData = null;
        
        // Try to load from primary save file
        loadedData = TryLoadFromFile(saveFilePath);
        
        // If primary load failed and backups are enabled, try the backup
        if (loadedData == null && enableBackups && File.Exists(backupSaveFilePath))
        {
            Debug.LogWarning("[Load] Primary save file load failed. Attempting to load from backup...");
            loadedData = TryLoadFromFile(backupSaveFilePath);
            
            // If backup was successful but primary failed, restore the backup to primary
            if (loadedData != null)
            {
                Debug.Log("[Load] Backup load successful. Restoring backup to primary save file.");
                try
                {
                    File.Copy(backupSaveFilePath, saveFilePath, true);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Load] Error restoring backup to primary: {ex.Message}");
                }
            }
        }
        
        // Create new SaveData if all loading attempts failed
        if (loadedData == null)
        {
            Debug.LogWarning("[Load] Could not load data from any save file. Creating new save data.");
            loadedData = new SaveData();
        }
        
        // Store a reference to this valid save data for recovery
        _lastKnownGoodSaveData = DeepCopySaveData(loadedData);
        
        // Distribute loaded data to managers
        DistributeLoadedData(loadedData);
        
        // Notify systems that save data was loaded
        OnSaveDataLoaded?.Invoke(loadedData);
        
        Debug.Log("[Load] SaveLoadManager: LoadGameData finished.");
    }
    
    /// <summary>
    /// Attempts to load save data from a specific file path
    /// </summary>
    private SaveData TryLoadFromFile(string filePath)
    {
        SaveData data = null;
        string json = null;
        
        if (!File.Exists(filePath))
        {
            Debug.Log($"[Load] No save file found at {filePath}.");
            return null;
        }
        
        try
        {
            byte[] encryptedData = File.ReadAllBytes(filePath);
            json = EncryptionUtility.Decrypt(encryptedData);

            if (!string.IsNullOrEmpty(json))
            {
                data = JsonUtility.FromJson<SaveData>(json);
                
                // Basic data validation
                if (!IsValidSaveData(data))
                {
                    Debug.LogError("[Load] Save data validation failed. Data may be corrupted.");
                    return null;
                }
                
                // Store loaded timestamp
                _loadedTimestampTicks = data.lastSaveTimestampTicks;
                
                Debug.Log($"[Load] Successfully loaded save data from {filePath}");
            }
            else
            {
                Debug.LogError("[Load] Decryption returned null or empty string.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Load] Error loading save from {filePath}: {ex.GetType().Name}: {ex.Message}");
            data = null;
        }
        
        return data;
    }
    
    /// <summary>
    /// Validates that the loaded save data contains expected fields
    /// </summary>
    private bool IsValidSaveData(SaveData data)
    {
        if (data == null) return false;
        
        // Check for basic required fields
        if (data.clickUpgradeLevels == null || 
            data.productionUpgradeLevels == null || 
            data.prestigeUpgradeLevels == null)
        {
            return false;
        }
        
        // Could add more specific validation if needed
        
        return true;
    }
    
    /// <summary>
    /// Makes a deep copy of SaveData for backup purposes
    /// </summary>
    private SaveData DeepCopySaveData(SaveData original)
    {
        if (original == null) return null;
        
        // Use JSON serialization to perform deep copy
        string json = JsonUtility.ToJson(original);
        return JsonUtility.FromJson<SaveData>(json);
    }
    
    /// <summary>
    /// Distributes the loaded data to all managers
    /// </summary>
    private void DistributeLoadedData(SaveData loadedData)
    {
        _scoreManager?.LoadData(loadedData);
        _clickUpgradeManager?.LoadData(loadedData);
        _productionManager?.LoadData(loadedData);
        _prestigeManager?.LoadData(loadedData);
    }

    // NEW: Getter for the loaded timestamp
    public long GetLastLoadedTimestampTicks()
    {
        return _loadedTimestampTicks;
    }

    /// <summary>
    /// Gathers data from managers, serializes, encrypts, and saves it to a file.
    /// Also creates a backup if enabled.
    /// </summary>
    public void SaveGameData()
    {
        // Prevent multiple simultaneous save operations
        if (_isSaving)
        {
            Debug.LogWarning("[SaveLoadManager] Save operation already in progress. Skipping.");
            return;
        }
        
        _isSaving = true;
        Debug.Log("SaveLoadManager: Saving game data...");
        
        try
        {
            // 1. Gather data from managers
            SaveData dataToSave = new SaveData();
            
            // Call UpdateSaveData on managers that need to populate the SaveData object
            _scoreManager?.UpdateSaveData(dataToSave);
            _clickUpgradeManager?.UpdateSaveData(dataToSave);
            _productionManager?.UpdateSaveData(dataToSave);
            _prestigeManager?.UpdateSaveData(dataToSave);
            
            // Record the current time before saving
            dataToSave.lastSaveTimestampTicks = DateTime.UtcNow.Ticks;
            
            // 2. Serialize to JSON
            string json = JsonUtility.ToJson(dataToSave, true); // use pretty print for debug
            
            // 3. Encrypt and save
            if (WriteEncryptedSaveFile(json, saveFilePath))
            {
                // Update our last known good save data
                _lastKnownGoodSaveData = DeepCopySaveData(dataToSave);
                
                // Create backup if enabled
                if (enableBackups)
                {
                    WriteEncryptedSaveFile(json, backupSaveFilePath);
                }
            }
            else
            {
                // If primary save failed, try to recover using last known good data
                if (_lastKnownGoodSaveData != null)
                {
                    Debug.LogWarning("[SaveLoadManager] Primary save failed. Attempting emergency save with last known good data.");
                    string recoveryJson = JsonUtility.ToJson(_lastKnownGoodSaveData, true);
                    WriteEncryptedSaveFile(recoveryJson, saveFilePath);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveLoadManager] Exception during save: {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            _isSaving = false;
        }
    }
    
    /// <summary>
    /// Encrypts and writes save data to a file
    /// </summary>
    private bool WriteEncryptedSaveFile(string json, string filePath)
    {
        try
        {
            byte[] encryptedData = EncryptionUtility.Encrypt(json);
            
            if (encryptedData != null)
            {
                // Write to a temporary file first to avoid corruption during write
                string tempPath = filePath + ".tmp";
                File.WriteAllBytes(tempPath, encryptedData);
                
                // If temp file was written successfully, move it to the real location
                if (File.Exists(filePath))
                    File.Delete(filePath);
                    
                File.Move(tempPath, filePath);
                
                Debug.Log($"[SaveLoadManager] Successfully saved to {filePath}");
                return true;
            }
            else
            {
                Debug.LogError("[SaveLoadManager] Encryption failed. Game data not saved.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveLoadManager] Error saving to {filePath}: {ex.GetType().Name}: {ex.Message}");
        }
        
        return false;
    }

    /// <summary>
    /// Deletes the save files. Called by GameManager during Hard Reset.
    /// </summary>
    public void DeleteSaveFile()
    {
        Debug.LogWarning("[SaveLoadManager] Deleting all save files.");
        
        DeleteFileIfExists(saveFilePath);
        DeleteFileIfExists(backupSaveFilePath);
        
        // Clear last known good data
        _lastKnownGoodSaveData = null;
    }
    
    private void DeleteFileIfExists(string filePath)
    {
        if (File.Exists(filePath))
        {
            try
            {
                File.Delete(filePath);
                Debug.Log($"[SaveLoadManager] Deleted file: {filePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveLoadManager] Failed to delete {filePath}: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// Performs a forced reload of save data from disk
    /// </summary>
    public void ForceReloadFromDisk()
    {
        Debug.Log("[SaveLoadManager] Forcing reload from disk.");
        LoadGameData();
    }
} 