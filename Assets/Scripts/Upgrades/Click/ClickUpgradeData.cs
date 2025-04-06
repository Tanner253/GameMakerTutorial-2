using UnityEngine;

/// <summary>
/// ScriptableObject defining the static data for a type of Click Upgrade.
/// </summary>
[CreateAssetMenu(fileName = "NewClickUpgrade", menuName = "Clicker/Click Upgrade Data")]
public class ClickUpgradeData : ScriptableObject
{
    [Header("Display Info")]
    public string upgradeName = "New Click Upgrade";
    [TextArea(3, 5)]
    public string description = "Description of the click upgrade.";

    [Header("Upgrade Stats")]
    [Tooltip("The amount added to the base click value per level.")]
    public float clickBonusPerLevel = 1.0f; // Changed to float for Inspector visibility

    [Header("Cost Scaling")]
    [Tooltip("The initial cost to purchase level 1.")]
    public float baseCost = 10f; // Changed to float for Inspector visibility
    [Tooltip("The factor by which the cost increases each level (e.g., 1.07 for 7% increase).")]
    public float costScaleFactor = 1.07f;

    // Note: Internal calculations will cast these floats back to decimal where precision is needed.
} 