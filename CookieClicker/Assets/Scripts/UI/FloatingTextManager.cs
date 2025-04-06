using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class FloatingTextManager : MonoBehaviour
{
    public GameObject floatingTextPrefab; // Assign your TextMeshPro Floating Text prefab in the Inspector
    public Transform textParentCanvas; // Assign the Canvas Transform where text should appear

    // Optional: Object Pooling for performance
    private List<FloatingText> textPool = new List<FloatingText>();
    private int poolSize = 20; // Increased pool size

    void Awake() // Use Awake to ensure pool is ready before Start calls
    {
        if (floatingTextPrefab == null)
        {
            Debug.LogError("FloatingTextManager: Floating Text Prefab is not assigned!", this);
        }
        if (textParentCanvas == null)
        {
            // Try to find a Canvas if not assigned
            Canvas canvas = FindFirstObjectByType<Canvas>(); // Use newer API
            if (canvas != null)
            {
                textParentCanvas = canvas.transform;
                Debug.LogWarning("FloatingTextManager: Text Parent Canvas not assigned. Found and assigned a Canvas.", this);
            }
            else
            {
                 Debug.LogError("FloatingTextManager: Text Parent Canvas is not assigned and no Canvas found in scene!", this);
            }
        }

        // Initialize Object Pool (Optional but recommended)
        InitializePool();
    }

    void InitializePool()
    {
        if (floatingTextPrefab == null || textParentCanvas == null) return;

        for (int i = 0; i < poolSize; i++)
        {
            GameObject textGO = Instantiate(floatingTextPrefab, textParentCanvas);
            FloatingText floatingText = textGO.GetComponent<FloatingText>();
            if (floatingText != null)
            {
                textGO.SetActive(false);
                textPool.Add(floatingText);
            }
            else
            {
                 Debug.LogError("FloatingText prefab does not contain a FloatingText component!", floatingTextPrefab);
                 Destroy(textGO); // Clean up invalid instance
                 break; // Stop pooling if prefab is wrong
            }
        }
    }

    FloatingText GetPooledText()
    {
        // Find inactive text in the pool
        foreach (var text in textPool)
        {
            if (!text.gameObject.activeSelf)
            {
                text.gameObject.SetActive(true); // Activate before returning
                return text;
            }
        }

        // If no inactive text found, expand the pool
        if (floatingTextPrefab != null && textParentCanvas != null)
        {
            Debug.LogWarning("Expanding FloatingText pool."); // Log pool expansion
            GameObject textGO = Instantiate(floatingTextPrefab, textParentCanvas);
             FloatingText floatingText = textGO.GetComponent<FloatingText>();
             if (floatingText != null)
            {
                textPool.Add(floatingText); // Add to pool
                // textGO.SetActive(true); // Already active as it was just instantiated
                return floatingText;
            }
            else
            {
                 Debug.LogError("FloatingText prefab does not contain a FloatingText component!", floatingTextPrefab);
                 Destroy(textGO);
            }
        }

        Debug.LogError("Could not get or create FloatingText instance.");
        return null; // Could not get or create text
    }

    /// <summary>
    /// Shows floating text at a specific canvas position.
    /// </summary>
    /// <param name="amount">The numeric value to display (will be formatted).</param>
    /// <param name="position">The position in Canvas space (e.g., anchoredPosition of a UI element).</param>
    /// <param name="color">Optional color for the text.</param>
    public void ShowFloatingText(decimal amount, Vector2 position, Color? color = null)
    {
        if (floatingTextPrefab == null || textParentCanvas == null)
        {
            Debug.LogError("FloatingTextManager cannot show text: Prefab or Canvas missing.");
            return;
        }

        FloatingText textInstance = GetPooledText(); // Use pooling
        if (textInstance == null) return; // Could not get text

        // Format the text, e.g., "+1.5"
        // Ensure formatting handles potential large/small numbers appropriately
        string textToShow = $"+{amount:N1}"; // Use N1 for number format with 1 decimal place

        // Use default color (e.g., white) if none provided
        Color textColor = color ?? Color.white;

        // Initialize the text properties (like content, position, color)
        textInstance.Initialize(textToShow, position, textColor);

    }
    
    // Overload for manual clicks (using GameManager's click value and default white color)
     public void ShowFloatingText(decimal amount, Vector2 position)
    {
         ShowFloatingText(amount, position, Color.white); // Call the main method with default color
    }
} 