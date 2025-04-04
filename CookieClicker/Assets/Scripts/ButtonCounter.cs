using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ButtonCounter : MonoBehaviour, IPointerClickHandler
{
    private int count = 0;
    public TextMeshProUGUI counterText;
    public Button clickButton;

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