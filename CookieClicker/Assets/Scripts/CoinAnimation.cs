using UnityEngine;

public class CoinAnimation : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    [SerializeField] private Sprite defaultSprite;
    private RuntimeAnimatorController animatorController;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        
        // Store references
        if (animator != null)
        {
            animatorController = animator.runtimeAnimatorController;
        }
        
        // Store the default sprite if not already set
        if (defaultSprite == null && spriteRenderer != null)
        {
            defaultSprite = spriteRenderer.sprite;
        }
    }

    void Start()
    {
        InitializeComponents();
    }

    void OnEnable()
    {
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        // Ensure sprite renderer is set up
        if (spriteRenderer != null)
        {
            if (defaultSprite != null)
            {
                spriteRenderer.sprite = defaultSprite;
            }
            spriteRenderer.enabled = true;
        }

        // Ensure animator is properly set up
        if (animator != null)
        {
            if (animatorController != null)
            {
                animator.runtimeAnimatorController = animatorController;
            }
            animator.enabled = true;
            
            // Force the animation to play
            animator.Play("Coin", 0, 0f);
        }
    }
} 