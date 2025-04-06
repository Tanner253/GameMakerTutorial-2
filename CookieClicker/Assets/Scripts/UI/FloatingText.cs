using UnityEngine;
using TMPro;

public class FloatingText : MonoBehaviour
{
    public float moveSpeed = 1.0f;
    public float fadeSpeed = 1.0f;
    public float lifetime = 1.0f;

    private TextMeshProUGUI textMesh;
    private RectTransform rectTransform;
    private Color startColor;
    private float timeElapsed = 0f;

    void Awake()
    {
        textMesh = GetComponentInChildren<TextMeshProUGUI>(); // Get component from child if text is nested
        if (textMesh == null)
        {
             textMesh = GetComponent<TextMeshProUGUI>(); // Get component if directly attached
        }
        rectTransform = GetComponent<RectTransform>();

        if (textMesh == null)
        {
            Debug.LogError("FloatingText requires a TextMeshProUGUI component!", this);
            Destroy(gameObject); // Destroy if component is missing
            return;
        }
        if (rectTransform == null)
        {
             Debug.LogError("FloatingText requires a RectTransform component!", this);
             Destroy(gameObject);
             return;
        }

        startColor = textMesh.color;
    }

    /// <summary>
    /// Initializes the floating text properties. Call this after instantiating.
    /// </summary>
    /// <param name="text">The text to display (e.g., "+1").</param>
    /// <param name="initialPosition">The initial screen position (likely Canvas space).</param>
    /// <param name="color">The starting color.</param>
     public void Initialize(string text, Vector2 initialPosition, Color color)
    {
        if (textMesh == null || rectTransform == null) return; // Guard against Awake failing

        textMesh.text = text;
        rectTransform.anchoredPosition = initialPosition;
        textMesh.color = color;
        startColor = color; // Store the initial color for fading
        timeElapsed = 0f; // Reset lifetime timer
    }


    void Update()
    {
        if (textMesh == null || rectTransform == null) return;

        timeElapsed += Time.deltaTime;

        // Move upwards
        rectTransform.anchoredPosition += Vector2.up * moveSpeed * Time.deltaTime;

        // Fade out
        float alpha = Mathf.Clamp01(1.0f - (timeElapsed * fadeSpeed / lifetime));
        textMesh.color = new Color(startColor.r, startColor.g, startColor.b, alpha);

        // Destroy after lifetime
        if (timeElapsed >= lifetime)
        {
            Destroy(gameObject);
        }
    }
} 