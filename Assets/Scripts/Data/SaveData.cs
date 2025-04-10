using System;
using System.Collections.Generic;

// Represents the structure of the data to be saved and loaded.
// Needs to be serializable by Unity's JsonUtility or another serializer.
[Serializable]
public class SaveData
{
    // Score Data
    public string currentScore; // Using string to preserve decimal precision with JsonUtility
    public string totalLifetimeScoreEarned; // NEW: Track lifetime score for prestige calculation

    // Upgrade Data - Using lists of simple structs as JsonUtility doesn't directly serialize Dictionaries
    public List<UpgradeSaveData> clickUpgradeLevels;
    public List<UpgradeSaveData> productionUpgradeLevels;

    // Prestige Data - NEW
    public string goldBars; // NEW: Using string for decimal precision
    public int prestigeCount; // NEW: Track number of prestiges
    public List<PrestigeUpgradeSaveData> prestigeUpgradeLevels; // NEW: Store levels of prestige upgrades

    // Offline Progress Tracking
    public long lastSaveTimestampTicks; // NEW: Store DateTime.UtcNow.Ticks

    // Constructor to initialize lists and new fields
    public SaveData()
    {
        currentScore = "0";
        totalLifetimeScoreEarned = "0"; // Initialize lifetime score
        clickUpgradeLevels = new List<UpgradeSaveData>();
        productionUpgradeLevels = new List<UpgradeSaveData>();

        // Initialize Prestige Data
        goldBars = "0";
        prestigeCount = 0;
        prestigeUpgradeLevels = new List<PrestigeUpgradeSaveData>();
        lastSaveTimestampTicks = 0; // Initialize timestamp
    }
}

// Helper struct for serializing upgrade levels with JsonUtility
[Serializable]
public struct UpgradeSaveData
{
    public string upgradeName; // Use the ScriptableObject's name as the identifier
    public int level;
}

// NEW: Helper struct for serializing prestige upgrade levels
[Serializable]
public struct PrestigeUpgradeSaveData
{
    public string upgradeName; // Use the ScriptableObject's name as the identifier
    public int level;
} 