using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // Required for Button

/// <summary>
/// Simple script to attach to a UI Button
/// that navigates back to the main game scene ("Main").
/// </summary>
[RequireComponent(typeof(Button))] // Ensure there's a Button component
public class ReturnToMainScene : MonoBehaviour
{
    private Button _returnButton;

    // --- Configuration ---
    [SerializeField] // Allow setting in Inspector if needed, but keep default
    [Tooltip("The exact name of the main game scene to load.")]
    private string mainSceneName = "Main"; // Default name of your main game scene

    void Awake()
    {
        _returnButton = GetComponent<Button>();
        if (_returnButton == null)
        {
            Debug.LogError(
                "ReturnToMainScene requires a Button component on the same GameObject.",
                this
            );
        }
    }

    void Start()
    {
        if (_returnButton != null)
        {
            _returnButton.onClick.AddListener(GoToMainScene);
        }
    }

    void OnDestroy()
    {
        if (_returnButton != null)
        {
            _returnButton.onClick.RemoveListener(GoToMainScene);
        }
    }

    /// <summary>
    /// Loads the main game scene specified by mainSceneName.
    /// </summary>
    public void GoToMainScene()
    {
        AudioManager.Instance?.PlayClickSound(); // Play click sound
        // Basic check if the scene name is set
        if (string.IsNullOrEmpty(mainSceneName))
        {
            Debug.LogError(
                "ReturnToMainScene: mainSceneName is not set in the script or Inspector!",
                this
            );
            return;
        }

        // Ensure the target scene is in Build Settings before loading
        // Note: This is an editor-only check using UnityEditor namespace.
        // Remove or wrap with #if UNITY_EDITOR for builds.
#if UNITY_EDITOR
        bool sceneExistsInBuild = false;
        foreach (var scene in UnityEditor.EditorBuildSettings.scenes)
        {
            if (
                scene.enabled
                && System.IO.Path.GetFileNameWithoutExtension(scene.path) == mainSceneName
            )
            {
                sceneExistsInBuild = true;
                break;
            }
        }
        if (!sceneExistsInBuild)
        {
            Debug.LogError(
                $"ReturnToMainScene: Scene '{mainSceneName}' cannot be loaded because it has not been added or enabled in the build settings.",
                this
            );
            return;
        }
#endif

        Debug.Log($"ReturnToMainScene: Loading scene '{mainSceneName}'...");
        SceneManager.LoadScene(mainSceneName);
    }
}
