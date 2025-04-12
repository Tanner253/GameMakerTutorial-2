using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class TogglePanelButton : MonoBehaviour
{
    [Tooltip("The panel GameObject to toggle.")]
    public GameObject panelToToggle;

    [Tooltip("If true, this button GameObject will be hidden when the panel is active.")]
    public bool hideButtonWhenPanelOpens = true; // Default to true for existing behavior

    [Tooltip("(Optional) The button GameObject to reactivate when the panel is closed by this button.")]
    public GameObject buttonToReactivate;

    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();
    }

    void Start()
    {
        // Ensure the panel reference is set
        if (panelToToggle == null)
        {
            Debug.LogError("Panel to toggle is not assigned in the inspector!", this);
            button.interactable = false; // Disable button if panel isn't assigned
            return;
        }

        // Add listener to the button's click event
        button.onClick.AddListener(TogglePanel);

        // Optional: Start with the panel disabled
        // panelToToggle.SetActive(false);

        // Set initial button visibility based on panel's starting state
        // Only hide if the flag is set
        if (hideButtonWhenPanelOpens)
        {
            gameObject.SetActive(!panelToToggle.activeSelf);
        }
    }

    // Public method to be called by the button's onClick event
    public void TogglePanel()
    {
        AudioManager.Instance?.PlayClickSound(); // Play click sound
        if (panelToToggle != null)
        {
            bool isPanelNowActive = !panelToToggle.activeSelf;
            // Toggle the active state of the panel
            panelToToggle.SetActive(isPanelNowActive);

            // Hide this button if the panel is now active, show it if inactive
            // Only hide if the flag is set
            if (hideButtonWhenPanelOpens)
            {
                gameObject.SetActive(!isPanelNowActive);
            }

            // If the panel was just closed, reactivate the specified button
            if (!isPanelNowActive && buttonToReactivate != null)
            {
                buttonToReactivate.SetActive(true);
            }

            Debug.Log($"Toggled panel '{panelToToggle.name}' visibility to: {isPanelNowActive}"); // Log the new state
        }
        else
        {
             Debug.LogError("Cannot toggle panel - reference is missing!", this);
        }
    }

    // Optional: Ensure panel reference isn't lost if object is destroyed/reloaded
    void OnValidate()
    {
        // This helps ensure the reference is valid in the editor,
        // but runtime checks in Start are still crucial.
        if (panelToToggle != null && button == null)
        {
            button = GetComponent<Button>(); // Try to get button reference again if needed
        }
    }
} 