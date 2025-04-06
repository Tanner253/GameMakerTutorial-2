using UnityEngine;
using System;

/// <summary>
/// Holds the runtime state (primarily level) for any type of upgrade.
/// Uses a reference to the base ScriptableObject for identification.
/// </summary>
[Serializable] // Allows instances of this class to be serialized (e.g., if saved in a list in a MonoBehaviour)
public class UpgradeState
{
    public ScriptableObject upgradeDataRef; // Reference to the specific data asset (ClickUpgradeData or ProductionUpgradeData)
    public int level = 0;

    public UpgradeState(ScriptableObject dataRef)
    {
        if (dataRef == null)
        {
            UnityEngine.Debug.LogError("UpgradeState created with null dataRef!");
        }
        this.upgradeDataRef = dataRef;
        this.level = 0; // Start at level 0 (unpurchased)
    }
} 