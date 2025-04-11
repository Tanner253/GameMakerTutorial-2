using UnityEngine;
using TMPro;
using System.Collections;

public class OfflineProgressAlertUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Assign the parent Panel GameObject that contains the message text.")]
    [SerializeField] private GameObject alertPanel; 
    [Tooltip("Assign the TextMeshProUGUI component that will display the message.")]
    [SerializeField] private TextMeshProUGUI messageText; 

    void Start()
    {
        // Ensure the panel is hidden initially
        if (alertPanel != null)
        {
            Debug.Log($"[OfflineProgressAlertUI.Start] Found alertPanel reference ({alertPanel.name}). Setting inactive.", alertPanel);
            alertPanel.SetActive(false);
        }
        else
        {
            Debug.LogError("OfflineProgressAlertUI: Alert Panel reference not set in Inspector! Panel cannot be hidden on Start.", this);
        }
        if (messageText == null)
        {
             Debug.LogError("OfflineProgressAlertUI: Message Text reference not set in Inspector!", this);
        }
    }

    // Call this method to show the alert
    public void ShowAlert(decimal offlineEarnings)
    {
        if (alertPanel == null || messageText == null)
        {
            Debug.LogError("Cannot show offline alert - UI references missing or not assigned in Inspector.");
            return;
        }

        // Don't show if earning is zero or negative
        if (offlineEarnings <= 0)
        {
            return;
        }

        // Format the message using the NumberFormatter utility
        messageText.text = $"You earned {NumberFormatter.FormatNumber(offlineEarnings)} nuggets while away!";

        // Show the panel 
        Debug.Log($"[OfflineProgressAlertUI] Activating panel object: {alertPanel.name}");
        alertPanel.SetActive(true);
        Debug.Log($"[OfflineProgressAlertUI] Panel activeSelf state after SetActive(true): {alertPanel.activeSelf}");
    }

    // Public method to be called by a UI Button's OnClick event
    public void DismissAlert()
    {
        Debug.Log("[OfflineProgressAlertUI] Dismiss button clicked.");
        if (alertPanel != null)
        {
            alertPanel.SetActive(false);
        }
    }
} 