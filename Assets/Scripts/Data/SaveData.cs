using System;
using System.Collections.Generic;

// Represents the structure of the data to be saved and loaded.
// Needs to be serializable by Unity's JsonUtility or another serializer.
[Serializable]
public class SaveData
{
    // Score Data
    public string currentScore; // Using string to preserve decimal precision with JsonUtility

    // Upgrade Data - Using lists of simple structs as JsonUtility doesn't directly serialize Dictionaries
    public List<UpgradeSaveData> clickUpgradeLevels;
    public List<UpgradeSaveData> productionUpgradeLevels;

    // Constructor to initialize lists
    public SaveData()
    {
        currentScore = "0";
        clickUpgradeLevels = new List<UpgradeSaveData>();
        productionUpgradeLevels = new List<UpgradeSaveData>();
    }
}

// Helper struct for serializing upgrade levels with JsonUtility
[Serializable]
public struct UpgradeSaveData
{
    public string upgradeName; // Use the ScriptableObject's name as the identifier
    public int level;
} 