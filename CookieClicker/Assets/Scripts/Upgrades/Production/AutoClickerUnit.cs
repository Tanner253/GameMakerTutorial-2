using UnityEngine;
using System;

/// <summary>
/// Represents a single instance of an auto-clicking production unit.
/// Manages its own timer and triggers score addition + floating text.
/// </summary>
public class AutoClickerUnit : MonoBehaviour
{
    private ProductionUpgradeData upgradeData; // Data defining this unit's properties
    private int unitLevel = 0; // The current level of this specific unit/upgrade
    private float timer = 0f;
    private RectTransform rectTransform; // To get position for floating text

    void Awake()
    {
        // Get the RectTransform if this is a UI element
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogWarning("AutoClickerUnit is not attached to a UI element with a RectTransform. Floating text position might be incorrect.", this);
        }
    }

    /// <summary>
    /// Initializes the unit with its data and starting level.
    /// Should be called by whatever system manages/creates these units (e.g., ProductionManager or UI setup).
    /// </summary>
    public void Initialize(ProductionUpgradeData data, int level)
    {
        upgradeData = data;
        unitLevel = level;
        timer = 0f; // Reset timer on initialization/level up

        // Initial state update if needed
        UpdateState(); 
    }

    /// <summary>
    /// Called when the unit's level changes.
    /// </summary>
    public void SetLevel(int newLevel)
    {
        unitLevel = Mathf.Max(0, newLevel); // Ensure level is not negative
        timer = 0f; // Reset timer on level change
        UpdateState();
    }

    /// <summary>
    /// Updates the component's state based on current level and data.
    /// For example, enables/disables the component if level is 0.
    /// </summary>
    void UpdateState()
    {
        // Disable the component's update loop if the level is 0 or data is missing
        bool shouldBeEnabled = (unitLevel > 0 && upgradeData != null && upgradeData.tickRate > 0);
        if (this.enabled != shouldBeEnabled)
        {
            this.enabled = shouldBeEnabled;
            Debug.Log($"AutoClickerUnit ({upgradeData?.name ?? "N/A"}) {(this.enabled ? "Enabled" : "Disabled")}. Level: {unitLevel}, TickRate: {upgradeData?.tickRate ?? 0}", this);
        }
        // Optionally update visual representation based on level here
    }

    void Update()
    {
        if (!this.enabled) return; // Component might be disabled by UpdateState

        timer += Time.deltaTime;

        // Check if enough time has passed for a tick
        if (timer >= upgradeData.tickRate)
        {
            // Calculate how many ticks occurred (in case of lag or low framerate)
            int ticksOccurred = Mathf.FloorToInt(timer / upgradeData.tickRate);
            timer -= ticksOccurred * upgradeData.tickRate; // Consume the time used for ticks
            
            Debug.Log($"AutoClickerUnit ({upgradeData.name}) Tick! Ticks occurred: {ticksOccurred}", this);

            // Calculate score generated for the ticks that occurred
            decimal scorePerTick = (decimal)upgradeData.baseProductionAmount * unitLevel;
            decimal totalScoreToAdd = scorePerTick * ticksOccurred;

            if (totalScoreToAdd > 0)
            {
                // Determine position for floating text
                // Use RectTransform position if available, otherwise default to zero
                Vector2 textPosition = rectTransform != null ? rectTransform.anchoredPosition : Vector2.zero;

                // --- Trigger Score Addition and Floating Text --- 
                // We need a new method in GameManager for this
                if (GameManager.Instance != null)
                {   
                    // TODO: Replace AddPassiveScore with a new GameManager method
                    // GameManager.Instance.AddPassiveScore(totalScoreToAdd); 
                    
                    // Hypothetical new method call:
                     GameManager.Instance.ProcessAutoClickerTick(totalScoreToAdd, textPosition);
                     Debug.Log($"AutoClickerUnit ({upgradeData.name}) added {totalScoreToAdd:F1} score.", this);
                }
                else
                {
                    Debug.LogError($"AutoClickerUnit ({upgradeData.name}) could not find GameManager to add score!", this);
                }
            }
            else
            {
                Debug.LogWarning($"AutoClickerUnit ({upgradeData.name}) generated zero score this tick. Amount: {totalScoreToAdd}", this);
            }
        }
    }
} 