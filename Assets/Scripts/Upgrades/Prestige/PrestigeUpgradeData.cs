using UnityEngine;

[CreateAssetMenu(fileName = "NewPrestigeUpgrade", menuName = "Clicker/Prestige Upgrade Data")]
public class PrestigeUpgradeData : ScriptableObject
{
    [Header("Display Info")]
    public string upgradeName = "New Prestige Upgrade";
    [TextArea(3, 5)]
    public string description = "Description of the prestige upgrade.";

    [Header("Cost")]
    [Tooltip("The base cost in Gold Bars to purchase level 1.")]
    public double baseCostGoldBars = 1.0; // Using double for potentially large costs, cast later if needed
    [Tooltip("The factor by which the cost increases each level.")]
    public float costScaleFactor = 1.5f;

    [Header("Effect - Example: Click Bonus")]
    [Tooltip("Permanent percentage increase to base click value per level.")]
    public float clickBonusPercentPerLevel = 0.0f; // Example effect

    // Add other potential effect types here (e.g., production bonus, cost reduction, Meme Mint specific)
    // public float productionBonusPercentPerLevel = 0.0f;
    // public float mintEfficiencyBonusPerLevel = 0.0f;
    // public float mintCapacityBonusPerLevel = 0.0f;

    // Special flag for unique upgrades like Meme Mint Access
    public bool isUniqueUnlock = false;
    public int requiredPrestigeLevel = 0; // Minimum prestige level required to purchase
} 