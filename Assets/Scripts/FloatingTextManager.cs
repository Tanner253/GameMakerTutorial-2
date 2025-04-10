using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.Pool; // Added for Object Pooling
using System.Collections.Generic; // Added for tracking active coroutines

public class FloatingTextManager : MonoBehaviour
{
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

    [Header("Pooling Settings")]
    public int defaultCapacity = 10;
    public int maxPoolSize = 50;

    // Object Pool for the floating text instances
    private IObjectPool<GameObject> _floatingTextPool;

    // Track active coroutines to stop them if the object is released prematurely
    private Dictionary<GameObject, Coroutine> _activeCoroutines = new Dictionary<GameObject, Coroutine>();

    void Awake()
    {
        // Initialize the pool
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

    // Overload 1: Default color
    public void ShowFloatingText(decimal value, Vector2 sourceAnchoredPosition)
    {
        ShowFloatingText(value, sourceAnchoredPosition, null);
    }

    // Overload 2: Specific color
    public void ShowFloatingText(decimal value, Vector2 sourceAnchoredPosition, Color? textColor)
    {
        if (floatingTextPrefab == null || textSpawnParent == null)
        {
            Debug.LogWarning("Floating Text Prefab or Text Spawn Parent not assigned!");
            return;
        }

        // Get an instance from the pool instead of instantiating
        GameObject textInstance = _floatingTextPool.Get();
        if (textInstance == null) return; // Pool failed to provide an instance

        RectTransform textRectTransform = textInstance.GetComponent<RectTransform>();
        TextMeshProUGUI textMesh = textInstance.GetComponentInChildren<TextMeshProUGUI>(); // Assume prefab structure is correct

        if (textRectTransform == null || textMesh == null)
        {
            Debug.LogError("Pooled Floating Text Prefab is missing RectTransform or TextMeshProUGUI! Returning to pool.");
            _floatingTextPool.Release(textInstance);
            return;
        }

        // --- Calculate Initial Position (Simplified back to original) ---
        // Calculate random offset
        float randomX = Random.Range(-randomXRange / 2f, randomXRange / 2f);
        float randomY = Random.Range(-randomYRange / 2f, randomYRange / 2f);
        Vector2 randomOffset = new Vector2(randomX, randomY);

        // Set initial position
        textRectTransform.anchoredPosition = sourceAnchoredPosition + new Vector2(baseSpawnOffset.x, baseSpawnOffset.y) + randomOffset;
        // --- End Position Calculation ---

        // Set text content and appearance
        textMesh.text = NumberFormatter.FormatNumber(value, true);
        textMesh.fontSize = feedbackFontSize;

        // Determine initial color
        Color startColor;
        if (textColor.HasValue)
        {
            // Use the provided color
            startColor = textColor.Value;
        }
        else
        {
            // No color provided (e.g., manual click), explicitly use white
            startColor = Color.white;
        }
        // Apply the determined color immediately (alpha will be set in coroutine)
        textMesh.color = startColor;

        // Start the animation coroutine
        Coroutine animationCoroutine = StartCoroutine(FloatAndFadeText(textInstance, textRectTransform, textMesh, startColor));
        _activeCoroutines[textInstance] = animationCoroutine; // Track the coroutine

    }

    // --- Coroutine (Modified for Pooling) ---

    IEnumerator FloatAndFadeText(GameObject instance, RectTransform rectTransform, TextMeshProUGUI textMesh, Color startColor)
    {
        float timer = 0f;
        // Ensure starting alpha is correct (might have been faded out when returned to pool)
        textMesh.color = new Color(startColor.r, startColor.g, startColor.b, 1f);

        while (timer < floatDuration)
        {
            // Check instance validity (might be released prematurely)
            if (instance == null || !instance.activeInHierarchy)
            {
                 _activeCoroutines.Remove(instance);
                 yield break; // Stop if object was returned to pool early
            }

            // Move up using anchoredPosition
            rectTransform.anchoredPosition += Vector2.up * floatSpeed * Time.deltaTime;

            // Fade out
            float alpha = Mathf.Lerp(1f, 0f, timer / floatDuration);
            textMesh.color = new Color(startColor.r, startColor.g, startColor.b, alpha);

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