using UnityEngine;
using UnityEngine.UI; // Required for Button

[RequireComponent(typeof(Button))] // Ensures a Button component is attached
public class ResetButtonHelper : MonoBehaviour
{
    private Button resetButton;

    void Start()
    {
        resetButton = GetComponent<Button>();

        // Find the GameManager instance
        GameManager gameManager = GameManager.Instance;

        if (gameManager != null && resetButton != null)
        {
            // Remove any existing persistent listeners added through the inspector
            // to avoid potential duplicate calls if configuration changes.
            resetButton.onClick.RemoveAllListeners();

            // Add the listener purely through code, targeting the current instance
            resetButton.onClick.AddListener(gameManager.ResetGameData);
             Debug.Log($"ResetButtonHelper: Added listener for GameManager.ResetGameData on {gameObject.name}");
        }
        else
        {
            if (resetButton == null)
            {
                 Debug.LogError("ResetButtonHelper: Button component not found!", this);
            }
             if (gameManager == null)
            {
                 Debug.LogError("ResetButtonHelper: GameManager.Instance is null! Make sure GameManager is active.", this);
            }
            // Disable the button if setup failed
             if(resetButton != null) resetButton.interactable = false;
        }
    }

     // Optional: Good practice to remove listeners when the object is destroyed
     void OnDestroy()
     {
         // Check if Instance is still valid before trying to access it
         if (GameManager.Instance != null && resetButton != null)
         {
             resetButton.onClick.RemoveListener(GameManager.Instance.ResetGameData); // Attempt removal
         }
     }
} 