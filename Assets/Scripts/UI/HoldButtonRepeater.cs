using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Add this component to a UI Button GameObject to make it repeatedly
/// invoke its onClick event when held down after an initial delay.
/// </summary>
[RequireComponent(typeof(Button))]
public class HoldButtonRepeater : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Tooltip("The delay in seconds before repeating starts after the initial click.")]
    [SerializeField] private float initialDelay = 0.5f;

    [Tooltip("The interval in seconds between repeated clicks while holding.")]
    [SerializeField] private float repeatInterval = 0.1f;

    private Button _button;
    private Coroutine _repeatCoroutine;
    private bool _isPointerDown = false;

    void Awake()
    {
        _button = GetComponent<Button>();
        if (_button == null)
        {
            Debug.LogError("HoldButtonRepeater requires a Button component on the same GameObject.", this);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_button == null || !_button.interactable) return;

        _isPointerDown = true;
        // Debug.Log("Pointer Down");

        // Stop any previous repeating coroutine
        if (_repeatCoroutine != null)
        {
            StopCoroutine(_repeatCoroutine);
        }

        // Start the repeating process
        _repeatCoroutine = StartCoroutine(RepeatClick());
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _isPointerDown = false;
        // Debug.Log("Pointer Up");

        // Stop the repeating coroutine
        if (_repeatCoroutine != null)
        {
            StopCoroutine(_repeatCoroutine);
            _repeatCoroutine = null;
        }
    }

    private IEnumerator RepeatClick()
    {
        // Wait for the initial delay
        yield return new WaitForSeconds(initialDelay);

        // Repeat while the pointer is still down
        while (_isPointerDown)
        {
            if (_button != null && _button.interactable)
            {
                 // Debug.Log("Repeating Click");
                _button.onClick.Invoke();
            }
            else
            {
                // If button becomes non-interactable while holding, stop repeating
                 // Debug.Log("Button became non-interactable, stopping repeat.");
                 _isPointerDown = false; // Ensure loop condition breaks
                 break;
            }
            // Wait for the repeat interval
            yield return new WaitForSeconds(repeatInterval);
        }
         // Debug.Log("Repeat Coroutine Ended");
         _repeatCoroutine = null; // Ensure coroutine reference is cleared
    }

    // Optional: If the button is disabled/destroyed while holding, clean up
    void OnDisable()
    {
        _isPointerDown = false;
        if (_repeatCoroutine != null)
        {
            StopCoroutine(_repeatCoroutine);
            _repeatCoroutine = null;
             // Debug.Log("Button Disabled, stopping repeat coroutine.");
        }
    }
} 