using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.Pool; // Added for Object Pooling
using System.Collections.Generic; // Added for tracking active coroutines

public class FloatingTextManager : MonoBehaviour
{
    public static FloatingTextManager Instance { get; private set; } // ADDED SINGLETON INSTANCE

    [Header("Floating Text Settings")]
    public GameObject floatingTextPrefab; // Assign your TextMeshPro prefab here
    public Transform textSpawnParent;     // Assign the Canvas Transform here
    public Vector3 baseSpawnOffset = new Vector3(0, 75, 0); // Base offset from the source
    public float floatDuration = 0.75f; // How long the text floats (seconds)
    public float floatSpeed = 100f;    // How fast the text floats up
    public float randomXRange = 100f;   // Max random horizontal offset (+/-)
    public float randomYRange = 50f;    // Max random vertical offset (+/-)
    [Tooltip("Set the font size for the feedback text.")]
    public float feedbackFontSize = 36f; // Added font size control

    [Header("Manual Click Floating Text Settings")] // NEW HEADER
    [Tooltip("Base offset from the click position for manual click text.")]
    public Vector3 manualClickBaseSpawnOffset = new Vector3(0, 50, 0); // Different default
    [Tooltip("How long the manual click text floats (seconds).")]
    public float manualClickFloatDuration = 0.6f; // Different default
    [Tooltip("How fast the manual click text floats up.")]
    public float manualClickFloatSpeed = 120f; // Different default
    [Tooltip("Max random horizontal offset (+/-) for manual click text.")]
    public float manualClickRandomXRange = 80f; // Different default
    [Tooltip("Max random vertical offset (+/-) for manual click text.")]
    public float manualClickRandomYRange = 40f; // Different default
    // Note: Font size is kept global for now unless explicitly requested otherwise.

    [Header("Pooling Settings")]
    public int defaultCapacity = 10;
    public int maxPoolSize = 50;

    // Object Pool for the floating text instances
    private IObjectPool<GameObject> _floatingTextPool;

    // Track active coroutines to stop them if the object is released prematurely
    private Dictionary<GameObject, Coroutine> _activeCoroutines = new Dictionary<GameObject, Coroutine>();

    void Awake()
    {
        // --- ADDED Singleton Logic Start ---
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Make it persistent if needed
        }
        else
        {
            Debug.LogWarning("[FloatingTextManager] Duplicate instance detected. Destroying self.");
            Destroy(gameObject);
            return;
        }
        // --- ADDED Singleton Logic End ---

        InitializePool();
    }

    // NEW: Method to find scene-specific references after scene load
    public void FindSceneReferences()
    {
        // Clear the pool first - objects belong to the previous scene instance
        if (_floatingTextPool != null)
        {
            Debug.Log("[FloatingTextManager] Clearing object pool due to scene change.");
            _floatingTextPool.Clear(); // This should call OnDestroyPoolObject for active/pooled items
            // We might need to re-initialize the pool instance itself if Clear disposes it, 
            // but ObjectPool<T> typically just clears internal collections.
            // Re-initializing just in case:
            InitializePool();
        }
        else
        {
             // Pool didn't exist, initialize it
             InitializePool();
        }
        
        // Find the parent for text spawning - assuming it's under a Canvas named "Canvas"
        // Adjust "Canvas/FloatingText" if your hierarchy is different
        GameObject canvasGO = GameObject.Find("Canvas");
        if (canvasGO != null)
        {
            Transform foundParent = canvasGO.transform.Find("FloatingText"); 
            if (foundParent != null)
            {
                textSpawnParent = foundParent;
                Debug.Log("[FloatingTextManager] Found scene reference for textSpawnParent.", textSpawnParent);
            }
            else
            {
                 Debug.LogError("[FloatingTextManager] Could not find child 'FloatingText' under 'Canvas' for textSpawnParent!");
                 textSpawnParent = null; // Ensure it's null if not found
            }
        }
        else
        {
             Debug.LogError("[FloatingTextManager] Could not find GameObject named 'Canvas' to search for textSpawnParent!");
             textSpawnParent = null; // Ensure it's null if not found
        }

        // Re-initialize pool if it was somehow lost (less likely, but for safety)
        // REMOVED - Now handled at the start of the method
        // if (_floatingTextPool == null)
        // {
        //     Debug.LogWarning("[FloatingTextManager] Pool was null in FindSceneReferences. Re-initializing.");
        //     InitializePool(); // Ensure pool exists
        // }
    }

    // Extracted pool initialization to its own method
    private void InitializePool()
    {
        _floatingTextPool = new ObjectPool<GameObject>(
           CreatePooledItem,
           OnTakeFromPool,
           OnReturnedToPool,
           OnDestroyPoolObject,
           true, // Collection check (optional, for safety)
           defaultCapacity,
           maxPoolSize
       );
    }

    // --- Object Pool Methods ---

    private GameObject CreatePooledItem()
    {
        if (floatingTextPrefab == null)
        {
            Debug.LogError("Floating Text Prefab is not assigned! Cannot create pooled item.");
            return null;
        }
        GameObject instance = Instantiate(floatingTextPrefab, textSpawnParent);
        instance.SetActive(false); // Start inactive
        return instance;
    }

    // Called when an item is taken from the pool
    private void OnTakeFromPool(GameObject instance)
    {
        instance.SetActive(true);
        // Reset any state if needed (e.g., position is set in ShowFloatingText, color/alpha reset in coroutine)
    }

    // Called when an item is returned to the pool
    private void OnReturnedToPool(GameObject instance)
    {
        // Stop any associated coroutine if it's still running
        if (_activeCoroutines.TryGetValue(instance, out Coroutine coroutine))
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }
            _activeCoroutines.Remove(instance);
        }
        instance.SetActive(false);
    }

    // Called when an item is destroyed (e.g., if pool exceeds max size)
    private void OnDestroyPoolObject(GameObject instance)
    {
        Destroy(instance);
        // No need to remove from _activeCoroutines here, as Destroy handles it
    }

    // --- Show Floating Text Logic (Modified for Pooling) ---

    // Overload 1: Default color (NOW USES MANUAL CLICK SETTINGS)
    public void ShowFloatingText(decimal value, Vector2 sourceAnchoredPosition)
    {
        // Explicitly call the other overload, passing the manual click color and null for other settings
        // This ensures we use the manual click parameters within that overload's logic.
        ShowFloatingTextInternal(value, sourceAnchoredPosition, null, true); // Pass null for color, internal method generates random
    }

    // Overload 2: Specific color (USED BY PRODUCTION/OTHER SYSTEMS)
    public void ShowFloatingText(decimal value, Vector2 sourceAnchoredPosition, Color? textColor)
    {
        // Pass false to indicate non-manual click
        ShowFloatingTextInternal(value, sourceAnchoredPosition, textColor, false);
    }

    // Internal method to handle common logic and parameter differences
    private void ShowFloatingTextInternal(decimal value, Vector2 sourceAnchoredPosition, Color? textColor, bool isManualClick)
    {
        if (floatingTextPrefab == null || textSpawnParent == null)
        {
            Debug.LogWarning("Floating Text Prefab or Text Spawn Parent not assigned!");
            return;
        }

        // Get an instance from the pool
        GameObject textInstance = _floatingTextPool.Get();
        if (textInstance == null) return;

        RectTransform textRectTransform = textInstance.GetComponent<RectTransform>();
        TextMeshProUGUI textMesh = textInstance.GetComponentInChildren<TextMeshProUGUI>();

        if (textRectTransform == null || textMesh == null)
        {
            Debug.LogError("Pooled Floating Text Prefab is missing RectTransform or TextMeshProUGUI! Returning to pool.");
            _floatingTextPool.Release(textInstance);
            return;
        }

        // --- Determine Settings Based on Source ---
        Vector3 actualBaseOffset = isManualClick ? manualClickBaseSpawnOffset : baseSpawnOffset;
        float actualRandomX = isManualClick ? manualClickRandomXRange : randomXRange;
        float actualRandomY = isManualClick ? manualClickRandomYRange : randomYRange;
        float actualDuration = isManualClick ? manualClickFloatDuration : floatDuration;
        float actualSpeed = isManualClick ? manualClickFloatSpeed : floatSpeed;
        Color actualColor;
        // float outlineThickness = 0f; // REMOVED - Using Underlay now
        // Color outlineColor = Color.black; // REMOVED

        // Get the material instance. Accessing .fontMaterial creates an instance if needed.
        Material materialInstance = textMesh.fontMaterial;

        if (isManualClick)
        {
            // Generate a random bright color for manual clicks
            actualColor = Random.ColorHSV(0f, 1f, 0.8f, 1f, 0.8f, 1f); // Hue, Saturation Min/Max, Value Min/Max
            // outlineThickness = 0.15f; // REMOVED

            // Enable and configure Underlay for manual clicks
            materialInstance.EnableKeyword("UNDERLAY_ON");
            materialInstance.SetColor("_UnderlayColor", new Color(0.1f, 0.1f, 0.1f, 0.85f)); // Dark grey glow, good opacity
            materialInstance.SetFloat("_UnderlayOffsetX", 0.5f); // Keep offset minimal for centered glow
            materialInstance.SetFloat("_UnderlayOffsetY", -0.5f); // Keep offset minimal for centered glow
            materialInstance.SetFloat("_UnderlayDilate", 0.5f); // Significantly Increased Spread/thickness
            materialInstance.SetFloat("_UnderlaySoftness", 0.6f); // Significantly Increased Blurring/glow
        }
        else
        {
            // Use provided color or default white for production/other text
            actualColor = textColor ?? Color.white;
            // outlineThickness = 0f; // REMOVED

            // Ensure Underlay is disabled for production text
            materialInstance.DisableKeyword("UNDERLAY_ON");
        }

        // Ensure outline is explicitly off for both cases
        textMesh.outlineWidth = 0f;

        // --- Calculate Initial Position ---
        float randomX = Random.Range(-actualRandomX / 2f, actualRandomX / 2f);
        float randomY = Random.Range(-actualRandomY / 2f, actualRandomY / 2f);
        Vector2 randomOffset = new Vector2(randomX, randomY);
        textRectTransform.anchoredPosition = sourceAnchoredPosition + new Vector2(actualBaseOffset.x, actualBaseOffset.y) + randomOffset;
        // --- End Position Calculation ---

        // Set text content and appearance
        textMesh.text = NumberFormatter.FormatNumber(value, true);
        textMesh.fontSize = feedbackFontSize; // Font size remains global for now
        textMesh.color = actualColor; // Apply vertex color (main text color)

        // Apply outline settings (REMOVED as we apply via material now)
        // textMesh.outlineWidth = outlineThickness;
        // textMesh.outlineColor = outlineColor;

        // Start the animation coroutine with appropriate duration and speed
        // Pass the main text color (startColor) to the coroutine
        Coroutine animationCoroutine = StartCoroutine(FloatAndFadeText(textInstance, textRectTransform, textMesh, actualColor, actualDuration, actualSpeed));
        _activeCoroutines[textInstance] = animationCoroutine; // Track the coroutine
    }

    // --- Coroutine (Modified for Pooling & Parameters) ---

    IEnumerator FloatAndFadeText(GameObject instance, RectTransform rectTransform, TextMeshProUGUI textMesh, Color startColor, float duration, float speed) // Added duration and speed parameters
    {
        float timer = 0f;
        // Ensure starting vertex color alpha is correct
        textMesh.color = new Color(startColor.r, startColor.g, startColor.b, 1f);
        // We will fade the vertex color alpha. The underlay color alpha is fixed.

        while (timer < duration)
        {
            // Check instance validity (might be released prematurely)
            if (instance == null || !instance.activeInHierarchy)
            {
                 _activeCoroutines.Remove(instance);
                 yield break; // Stop if object was returned to pool early
            }

            // Move up using anchoredPosition with parameterized speed
            rectTransform.anchoredPosition += Vector2.up * speed * Time.deltaTime;

            // Fade out vertex color alpha
            float alpha = Mathf.Lerp(1f, 0f, timer / duration);
            textMesh.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            // Note: The underlay color alpha remains constant from what we set in ShowFloatingTextInternal

            timer += Time.deltaTime;
            yield return null; // Wait for the next frame
        }

        // Release the instance back to the pool instead of destroying
        _activeCoroutines.Remove(instance); // Stop tracking before release
        if (instance.activeInHierarchy) // Only release if not already released
        {
             _floatingTextPool.Release(instance);
        }
    }

    // --- Cleanup ---
    void OnDestroy()
    {
        // Dispose of the pool and destroy any remaining active instances
         _floatingTextPool?.Clear(); // Destroys pooled objects using OnDestroyPoolObject
    }
} 