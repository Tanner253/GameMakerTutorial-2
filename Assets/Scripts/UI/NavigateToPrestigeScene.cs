using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // Needed for Button

/// <summary>
/// Simple script attached to a Button to load the Prestige Scene when clicked.
/// Now also handles conditionally enabling a pulse effect when prestige is affordable.
/// </summary>
[RequireComponent(typeof(Button))]
public class NavigateToPrestigeScene : MonoBehaviour
{
    private Button _button;
    private ContinuousPulseUI _pulseComponent; // Reference to the pulse script
    private bool _isPulsing = false; // Track current state to avoid unnecessary GetComponent calls

    void Awake()
    {
        _button = GetComponent<Button>();
        _pulseComponent = GetComponent<ContinuousPulseUI>(); // Get the pulse component on the same GameObject

        if (_button != null)
        {
            _button.onClick.AddListener(LoadPrestigeScene);
        }
        else
        {
            Debug.LogError("NavigateToPrestigeScene requires a Button component.", this);
        }

        if (_pulseComponent == null)
        {
            Debug.LogWarning("ContinuousPulseUI component not found on this button. Conditional pulsing will not work.", this);
        }
        else
        {
            // Ensure it starts disabled (assuming it's disabled by default in Inspector too)
            _pulseComponent.enabled = false;
            _isPulsing = false;
        }
    }

    void Update()
    {
        // Check prestige status periodically
        // Could optimize this by only checking when score changes if needed, but Update is simpler for now.
        if (PrestigeManager.Instance != null && _pulseComponent != null)
        {
            bool canAfford = PrestigeManager.Instance.CanAffordPrestige();

            // Only update the component's state if it needs to change
            if (canAfford && !_isPulsing)
            {
                _pulseComponent.enabled = true;
                _isPulsing = true;
                // Debug.Log("Starting Prestige Nav Pulse"); // Optional debug
            }
            else if (!canAfford && _isPulsing)
            {
                _pulseComponent.enabled = false;
                _isPulsing = false;
                // Debug.Log("Stopping Prestige Nav Pulse"); // Optional debug
            }
        }
    }

    void LoadPrestigeScene()
    {
        AudioManager.Instance?.PlayClickSound(); // Play click sound
        // Get the scene name from the PrestigeManager (safe access)
        string sceneName = PrestigeManager.Instance?.PrestigeSceneName;
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogError("Cannot navigate: Prestige Scene Name is not set in PrestigeManager or PrestigeManager instance is null.");
        }
    }

    void OnDestroy()
    {
        // Cleanup listener
        if (_button != null)
        {
            _button.onClick.RemoveListener(LoadPrestigeScene);
        }
    }
}
