using UnityEngine;

[CreateAssetMenu(fileName = "NewProductionUpgrade", menuName = "Clicker/Production Upgrade Data")]
public class ProductionUpgradeData : ScriptableObject
{
    [Header("Display Info")]
    public string upgradeName = "New Production Upgrade";
    [TextArea(3, 5)]
    public string description = "Description of the upgrade.";

    [Header("Production Stats")]
    [Tooltip("Base amount of score generated per tick.")]
    public float baseProductionAmount = 1.0f; // Can be fractional
    [Tooltip("Time in seconds between each production tick.")]
    public float tickRate = 1.0f;

    [Header("Cost Scaling")]
    [Tooltip("The initial cost to purchase level 1.")]
    public float baseCost = 10f;
    [Tooltip("The factor by which the cost increases each level (e.g., 1.15 for 15% increase).")]
    public float costScaleFactor = 1.15f;

    [Tooltip("The color used for the floating feedback text generated by this upgrade.")]
    public Color feedbackColor = Color.white; // Default color

    // Maybe add icon later: public Sprite icon;
} 