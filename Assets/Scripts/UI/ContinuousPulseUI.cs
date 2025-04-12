using UnityEngine;
using System.Collections;

/// <summary>
/// Attach to a UI GameObject with a RectTransform to make it continuously pulse its scale.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class ContinuousPulseUI : MonoBehaviour
{
    [Tooltip("How much larger the object scales at its peak (e.g., 1.05 = 5% larger).")]
    [SerializeField] private float pulseMagnitude = 1.05f;

    [Tooltip("How fast the pulse animation plays (higher value = faster pulse).")]
    [SerializeField] private float pulseSpeed = 1.5f;

    private RectTransform rectTransform;
    private Vector3 originalScale;
    private Coroutine pulseCoroutine;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalScale = rectTransform.localScale;
    }

    void OnEnable()
    {
        // Start the pulse when the component/GameObject becomes active
        if (pulseCoroutine == null)
        {
             // Reset scale in case it was disabled mid-pulse
            rectTransform.localScale = originalScale; 
            pulseCoroutine = StartCoroutine(ContinuousPulseAnimation());
        }
    }

    void OnDisable()
    {
        // Stop the pulse when the component/GameObject becomes inactive
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
            // Reset scale back to original when disabled
            rectTransform.localScale = originalScale;
        }
    }

    private IEnumerator ContinuousPulseAnimation()
    {
        while (true)
        {
            // Use Mathf.PingPong to create a smooth 0 -> 1 -> 0 oscillation
            float pingPong = Mathf.PingPong(Time.time * pulseSpeed, 1.0f);

            // Map the 0-1 value to a scale factor between 1.0 and pulseMagnitude
            float scaleFactor = Mathf.Lerp(1.0f, pulseMagnitude, pingPong);

            // Apply the scale
            rectTransform.localScale = originalScale * scaleFactor;

            yield return null; // Wait for the next frame
        }
    }
} 