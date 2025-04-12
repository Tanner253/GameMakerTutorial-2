using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LemonBehavior : MonoBehaviour
{
    [Header("State References")]
    [SerializeField]
    private Button clickButton;

    [SerializeField]
    private CanvasGroup canvasGroup; // Optional: For fading

    [SerializeField]
    private Animator animator; // Optional: For animations

    // Runtime state
    private LemonManager _manager;
    private float _lifespan;
    private float _timeAlive = 0f;
    private bool _isClicked = false;
    private bool _isExpired = false;

    private RectTransform _rectTransform;

    void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        if (clickButton == null)
            clickButton = GetComponent<Button>();
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        if (animator == null)
            animator = GetComponent<Animator>();

        if (clickButton == null)
            Debug.LogError("[LemonBehavior] Button component not found!", this);
        if (_rectTransform == null)
            Debug.LogError("[LemonBehavior] RectTransform component not found!", this);

        clickButton?.onClick.AddListener(OnClick);
    }

    // Initialize is called by LemonManager after instantiation
    public void Initialize(LemonManager manager, float lifespan)
    {
        _manager = manager;
        _lifespan = lifespan;
        _timeAlive = 0f;
        _isClicked = false;
        _isExpired = false;

        if (clickButton != null)
            clickButton.interactable = true;
        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        // Optional: Trigger fall animation
        // animator?.SetTrigger("StartFalling");
        Debug.Log($"[LemonBehavior] Initialized. Lifespan: {_lifespan}s");
    }

    void Update()
    {
        if (_isClicked || _isExpired)
            return; // Don't update if already clicked or expired

        _timeAlive += Time.deltaTime;

        // --- Lifespan Check ---
        if (_timeAlive >= _lifespan)
        {
            Expire();
        }
    }

    void OnClick()
    {
        if (_isClicked || _isExpired || _manager == null || animator == null)
        {
            Debug.LogWarning(
                "[LemonBehavior] Click ignored (already clicked/expired or manager is null)"
            );
            return; // Prevent multiple clicks or clicking after expiry
        }

        Debug.Log("[LemonBehavior] Clicked! Triggering animation.");
        _isClicked = true;

        // Disable button immediately
        if (clickButton != null)
            clickButton.interactable = false;

        // Notify the manager
        _manager.LemonClicked(this);

        // Optional: Trigger click animation/effect
        // animator?.SetTrigger("Clicked");

        // Start destruction sequence (e.g., wait for animation)
        StartCoroutine(DestroyAfterDelay(0.5f)); // Adjust delay as needed
    }

    void Expire()
    {
        if (_isClicked || _isExpired || _manager == null)
            return;

        Debug.Log("[LemonBehavior] Expired.");
        _isExpired = true;

        // Disable button
        if (clickButton != null)
            clickButton.interactable = false;

        // Notify the manager
        _manager.LemonExpired(this);

        // Optional: Trigger expire animation/effect (e.g., fade out)
        // animator?.SetTrigger("Expired");
        StartCoroutine(FadeOutAndDestroy(0.5f)); // Example fade out
    }

    // Example Coroutine for destroying after a delay (e.g., for animation)
    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        // TODO: Implement object pooling here instead of Destroy
        Destroy(gameObject);
    }

    // Example Coroutine for fading out then destroying
    private IEnumerator FadeOutAndDestroy(float duration)
    {
        if (canvasGroup != null)
        {
            float startAlpha = canvasGroup.alpha;
            float timer = 0f;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, timer / duration);
                yield return null;
            }
            canvasGroup.alpha = 0f;
        }
        else
        {
            // Fallback if no canvas group - just wait
            yield return new WaitForSeconds(duration);
        }

        // TODO: Implement object pooling here instead of Destroy
        Destroy(gameObject);
    }
}
