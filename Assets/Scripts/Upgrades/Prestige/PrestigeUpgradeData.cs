using UnityEngine;

// Enum to define the specific effect of a prestige upgrade
public enum PrestigeEffectType
{
    None, // Default/Error
    UnlockFeature, // For one-time unlocks like enabling lemons
    LemonSpawnRate, // Reduces min/max spawn time
    LemonValue,     // Increases reward minutes
    LemonLifespan,  // Increases how long lemons stay
    ClickMultiplier // Increases base click value percentage
    // Add more types as needed (e.g., ProductionBonus, CostReduction)
}

[CreateAssetMenu(fileName = "NewPrestigeUpgrade", menuName = "Clicker/Prestige Upgrade Data")]
public class PrestigeUpgradeData : ScriptableObject
{
    [Header("Display Info")]
    public string upgradeName = "New Prestige Upgrade";
    [TextArea(3, 5)]
    public string description = "Description of the prestige upgrade.";

    [Header("Cost")]
    [Tooltip("The base cost in Gold Bars to purchase level 1. Use double for Inspector visibility.")]
    [SerializeField]
    public double baseCostGoldBars = 1.0;
    [Tooltip("The factor by which the cost increases each level (e.g., 1.5 = 50% increase per level).")]
    public float costScaleFactor = 1.5f;

    [Header("Effect Configuration")]
    [Tooltip("What kind of effect does this upgrade provide?")]
    public PrestigeEffectType effectType = PrestigeEffectType.None;
    [Tooltip("The amount the effect changes per level (e.g., -5 for spawn time, 0.5 for reward minutes, 1 for lifespan, 0.05 for 5% click bonus).")]
    public float effectValuePerLevel = 0.0f;

    // REMOVED: Click Bonus - Replaced by generic effect system
    // [Header("Effect - Example: Click Bonus")]
    // [Tooltip("Permanent percentage increase to base click value per level.")]
    // public float clickBonusPercentPerLevel = 0.0f; // Example effect

    // // Add other potential effect types here (e.g., production bonus, cost reduction, Meme Mint specific)
    // public float productionBonusPercentPerLevel = 0.0f;
    // public float mintEfficiencyBonusPerLevel = 0.0f;
    // public float mintCapacityBonusPerLevel = 0.0f;

    [Header("Purchase Rules")]
    [Tooltip("Is this a one-time purchase unlock? If true, cost/scaling might be ignored after level 1.")]
    public bool isUniqueUnlock = false;
    [Tooltip("Minimum prestige level required to purchase this upgrade at all.")]
    public int requiredPrestigeLevel = 0;

    // Note: Max level could be added if needed, otherwise upgrades are infinite.
    // public int maxLevel = 0; // 0 means infinite
} 