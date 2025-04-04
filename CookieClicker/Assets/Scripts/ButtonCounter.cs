using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections; // Needed for Coroutines

public class ButtonCounter : MonoBehaviour, IPointerClickHandler
{
    private int count = 0;
    public TextMeshProUGUI counterText;
    public Button clickButton;
    public Animator coinAnimator;

    [Header("Floating Text Settings")]
    public GameObject floatingTextPrefab; // Assign your TextMeshPro prefab here
    public Transform textSpawnParent;     // Assign the Canvas Transform here
    public Vector3 textSpawnOffset = new Vector3(0, 75, 0); // Offset from the coin
    public float floatDuration = 0.75f; // How long the text floats (seconds)
    public float floatSpeed = 100f;    // How fast the text floats up

    void Start()
    {
        // Initialize the counter text
        UpdateCounterText();
        
        // Add click listener to the button
        if (clickButton != null)
        {
            clickButton.onClick.AddListener(IncrementCounter);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        IncrementCounter();
    }

    void IncrementCounter()
    {
        count++;
        UpdateCounterText();
        Debug.Log($"Count increased to: {count}");

        // Trigger the animation ONLY if the animator is currently in the Idle state
        if (coinAnimator != null)
        {
            // Get the current state information for the base layer (layer 0)
            AnimatorStateInfo stateInfo = coinAnimator.GetCurrentAnimatorStateInfo(0);
            
            // Check if the current state's name is "Idle"
            if (stateInfo.IsName("Idle"))
            {
                 coinAnimator.SetTrigger("Spin");
            }
        }
        else
        {
            Debug.LogWarning("Coin Animator is not assigned in the Button Counter script in the Inspector!");
        }
        
        // Show floating text feedback
        ShowFloatingText();
    }

    void ShowFloatingText()
    {
        if (floatingTextPrefab == null || textSpawnParent == null)
        {
            Debug.LogWarning("Floating Text Prefab or Text Spawn Parent not assigned in ButtonCounter!");
            return;
        }

        // Instantiate the prefab under the specified parent
        GameObject textInstance = Instantiate(floatingTextPrefab, textSpawnParent);
        
        // Set initial position relative to the coin + offset
        // Note: For UI elements, setting position directly might need adjustment based on Canvas settings.
        // Setting anchoredPosition might be more reliable if anchors aren't centered.
        // Let's start with world position assuming the coin's pivot is reasonable.
        textInstance.transform.position = transform.position + textSpawnOffset;

        // Get the TextMeshPro component
        TextMeshProUGUI textMesh = textInstance.GetComponentInChildren<TextMeshProUGUI>();
        if (textMesh != null)
        {
            textMesh.text = "+1"; // Set the text
            // Start the animation coroutine
            StartCoroutine(FloatAndFadeText(textInstance, textMesh));
        }
        else
        {
             Debug.LogError("Floating Text Prefab needs a TextMeshProUGUI component!");
             Destroy(textInstance); // Clean up useless instance
        }
    }

    IEnumerator FloatAndFadeText(GameObject instance, TextMeshProUGUI textMesh)
    {
        Color startColor = textMesh.color;
        float timer = 0f;

        while (timer < floatDuration)
        {
            if (instance == null) yield break; // Stop if object was destroyed early

            // Move up
            instance.transform.position += Vector3.up * floatSpeed * Time.deltaTime;

            // Fade out (calculate alpha based on remaining time)
            float alpha = Mathf.Lerp(1f, 0f, timer / floatDuration);
            textMesh.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            
            timer += Time.deltaTime;
            yield return null; // Wait for the next frame
        }

        // Ensure it's fully destroyed after the loop
        if(instance != null) Destroy(instance); 
    }

    void UpdateCounterText()
    {
        if (counterText != null)
        {
            counterText.text = $"count: {count}";
            Debug.Log($"Updated text to: {counterText.text}");
        }
        else
        {
            Debug.LogWarning("CounterText is null!");
        }
    }
} 