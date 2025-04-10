using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // Required for Button

/// <summary>
/// Simple script to attach to a UI Button (or an Image with a Button component)
/// that navigates to the Prestige scene defined in PrestigeManager.
/// </summary>
[RequireComponent(typeof(Button))] // Ensure there's a Button component
public class NavigateToPrestigeScene : MonoBehaviour
{
    private Button _navigationButton;

    void Awake()
    {
        // Get the Button component attached to this GameObject
        _navigationButton = GetComponent<Button>();
        if (_navigationButton == null)
        {
            Debug.LogError(
                "NavigateToPrestigeScene requires a Button component on the same GameObject.",
                this
            );
        }
    }

    void Start()
    {
        // Add listener if the button was found
        if (_navigationButton != null)
        {
            _navigationButton.onClick.AddListener(GoToPrestigeScene);
        }
    }

    void OnDestroy()
    {
        // Remove listener to prevent memory leaks if the button existed
        if (_navigationButton != null)
        {
            _navigationButton.onClick.RemoveListener(GoToPrestigeScene);
        }
    }

    /// <summary>
    /// Loads the prestige scene defined in PrestigeManager.
    /// This method is typically called by the Button's onClick event.
    /// </summary>
    public void GoToPrestigeScene()
    {
        if (PrestigeManager.Instance == null)
        {
            Debug.LogError("NavigateToPrestigeScene: Cannot find PrestigeManager.Instance!");
            // Optionally provide feedback to the player here
            return;
        }

        string sceneName = PrestigeManager.Instance.PrestigeSceneName;

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError(
                "NavigateToPrestigeScene: Prestige Scene Name is not set in PrestigeManager!"
            );
            // Optionally provide feedback to the player here
            return;
        }

        // Optional: Add a check if the scene actually exists in Build Settings
        // Note: SceneUtility requires using UnityEditor, so it won't work in a build.
        // A better runtime check involves trying to load additively and checking for success,
        // but for simplicity, ensure the scene is in Build Settings.

        Debug.Log($"NavigateToPrestigeScene: Loading scene '{sceneName}'...");
        SceneManager.LoadScene(sceneName);
    }
}
