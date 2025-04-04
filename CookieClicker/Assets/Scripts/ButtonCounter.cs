using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ButtonCounter : MonoBehaviour
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

    void IncrementCounter()
    {
        count++;
        UpdateCounterText();
    }

    void UpdateCounterText()
    {
        if (counterText != null)
        {
            counterText.text = $"Count: {count}";
        }
    }
} 