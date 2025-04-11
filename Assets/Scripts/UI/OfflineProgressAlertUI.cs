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

    [Header("Settings")]
    [SerializeField] private float displayDuration = 10.0f; // How long to show the alert - CHANGED DEFAULT TO 10s

    private Coroutine _hideCoroutine;

    void Start()
    {
        // REMOVED: Ensure the panel is hidden initially
        // if (alertPanel != null)
        // {
        //     alertPanel.SetActive(false);
        // }
        // else ... (Keep error check)
        if (alertPanel == null)
        {
            Debug.LogError("OfflineProgressAlertUI: Alert Panel reference not set in Inspector!", this);
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

        // Stop any previous hide coroutine if it was running
        if (_hideCoroutine != null)
        {
            StopCoroutine(_hideCoroutine);
        }

        // Show the panel and start the timer to hide it
        Debug.Log($"[OfflineProgressAlertUI] Activating panel object: {alertPanel.name}");
        alertPanel.SetActive(true);
        Debug.Log($"[OfflineProgressAlertUI] Panel activeSelf state after SetActive(true): {alertPanel.activeSelf}");
        _hideCoroutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        Debug.Log($"[OfflineProgressAlertUI] Coroutine started. Waiting for {displayDuration} seconds."); // ADD THIS LOG
        yield return new WaitForSeconds(displayDuration);
        Debug.Log("[OfflineProgressAlertUI] Wait finished. Deactivating panel."); // ADD THIS LOG
        if (alertPanel != null) // Add null check before deactivating
        {
             alertPanel.SetActive(false);
        }
        _hideCoroutine = null; // Reset coroutine reference
    }
} 