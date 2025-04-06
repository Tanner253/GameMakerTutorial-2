using UnityEngine;
using TMPro;
using System.Collections;

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

    // Method to be called by other scripts (like GameManager)
    // Overload 1: Default color (from prefab)
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

        // Instantiate the prefab under the specified parent
        GameObject textInstance = Instantiate(floatingTextPrefab, textSpawnParent);
        RectTransform textRectTransform = textInstance.GetComponent<RectTransform>();

        if (textRectTransform != null)
        {
            // Calculate random offset
            float randomX = Random.Range(-randomXRange / 2f, randomXRange / 2f);
            float randomY = Random.Range(-randomYRange / 2f, randomYRange / 2f);
            Vector2 randomOffset = new Vector2(randomX, randomY);

            // Set initial position relative to the source's anchored position + base offset + random offset
            textRectTransform.anchoredPosition = sourceAnchoredPosition + new Vector2(baseSpawnOffset.x, baseSpawnOffset.y) + randomOffset;
        }
        else
        {
            Debug.LogError("Floating Text Prefab needs a RectTransform!");
            Destroy(textInstance);
            return;
        }

        // Get the TextMeshPro component
        TextMeshProUGUI textMesh = textInstance.GetComponentInChildren<TextMeshProUGUI>();
        if (textMesh != null)
        {
            // Format decimal value
            textMesh.text = $"+{value:F1}";

            // Set the font size
            textMesh.fontSize = feedbackFontSize;

            // Set color if provided, otherwise use prefab's default
            if (textColor.HasValue)
            {
                textMesh.color = textColor.Value;
            }

            // Start the animation coroutine, passing the RectTransform and the *initial* color
            // The coroutine will handle fading from this initial color.
            StartCoroutine(FloatAndFadeText(textInstance, textRectTransform, textMesh, textMesh.color));
        }
        else
        {
            Debug.LogError("Floating Text Prefab needs a TextMeshProUGUI component!");
            Destroy(textInstance); // Clean up useless instance
        }
    }

    // Coroutine to handle floating and fading
    // Now takes the starting color as an argument
    IEnumerator FloatAndFadeText(GameObject instance, RectTransform rectTransform, TextMeshProUGUI textMesh, Color startColor)
    {
        float timer = 0f;

        while (timer < floatDuration)
        {
            if (instance == null || rectTransform == null) yield break; // Stop if object was destroyed early

            // Move up using anchoredPosition
            rectTransform.anchoredPosition += Vector2.up * floatSpeed * Time.deltaTime;

            // Fade out (calculate alpha based on remaining time)
            float alpha = Mathf.Lerp(1f, 0f, timer / floatDuration);
            textMesh.color = new Color(startColor.r, startColor.g, startColor.b, alpha);

            timer += Time.deltaTime;
            yield return null; // Wait for the next frame
        }

        // Ensure it's fully destroyed after the loop
        if (instance != null) Destroy(instance);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
} 