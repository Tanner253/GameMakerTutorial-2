using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Creates and manages audio settings controls in the settings panel.
/// This script should be attached to the settings panel GameObject.
/// </summary>
public class AudioSettingsPanel : MonoBehaviour
{
    [Header("Prefabs")]
    [Tooltip("Slider prefab to use for volume controls. If not set, will try to create basic sliders.")]
    [SerializeField] private Slider sliderPrefab;
    
    [Header("Layout Settings")]
    [Tooltip("Parent transform for the audio control elements")]
    [SerializeField] private RectTransform contentParent;
    [Tooltip("Spacing between control elements")]
    [SerializeField] private float spacing = 40f;
    [Tooltip("Starting Y position for the first control")]
    [SerializeField] private float startY = -30f;
    [Tooltip("Width of the sliders")]
    [SerializeField] private float sliderWidth = 160f;
    [Tooltip("Height of the sliders")]
    [SerializeField] private float sliderHeight = 20f;
    
    [Header("Label Settings")]
    [Tooltip("Font to use for labels")]
    [SerializeField] private TMP_FontAsset labelFont;
    [Tooltip("Font size for labels")]
    [SerializeField] private float labelFontSize = 14f;
    [Tooltip("Label color")]
    [SerializeField] private Color labelColor = Color.white;
    
    // References to created components
    private Slider _musicSlider;
    private Slider _sfxSlider;
    private TextMeshProUGUI _musicValueText;
    private TextMeshProUGUI _sfxValueText;
    
    private AudioManager _audioManager;
    private AudioSettingsUI _audioSettingsUI;
    
    void Awake()
    {
        // If contentParent is not assigned, use this transform
        if (contentParent == null)
        {
            contentParent = GetComponent<RectTransform>();
        }
    }
    
    void Start()
    {
        SetupAudioControls();
    }
    
    void SetupAudioControls()
    {
        float currentY = startY;
        
        // Create Music Volume Controls
        GameObject musicControlGroup = CreateControlGroup("Music Volume", currentY);
        currentY -= spacing;
        
        // Create SFX Volume Controls
        GameObject sfxControlGroup = CreateControlGroup("SFX Volume", currentY);
        
        // Add AudioSettingsUI component to handle slider logic
        _audioSettingsUI = gameObject.AddComponent<AudioSettingsUI>();
        
        // Connect the sliders to the UI controller
        if (_audioSettingsUI != null)
        {
            _audioSettingsUI.SetSliders(_musicSlider, _sfxSlider);
            _audioSettingsUI.SetValueTexts(_musicValueText, _sfxValueText);
        }
    }
    
    private GameObject CreateControlGroup(string labelText, float yPosition)
    {
        // Create parent group
        GameObject controlGroup = new GameObject(labelText + " Group");
        RectTransform groupRect = controlGroup.AddComponent<RectTransform>();
        groupRect.SetParent(contentParent, false);
        groupRect.anchorMin = new Vector2(0.5f, 1f);
        groupRect.anchorMax = new Vector2(0.5f, 1f);
        groupRect.pivot = new Vector2(0.5f, 1f);
        groupRect.anchoredPosition = new Vector2(0, yPosition);
        groupRect.sizeDelta = new Vector2(sliderWidth + 100, sliderHeight + 30);
        
        // Create label
        GameObject labelObj = new GameObject(labelText + " Label");
        RectTransform labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.SetParent(groupRect, false);
        labelRect.anchorMin = new Vector2(0, 1f);
        labelRect.anchorMax = new Vector2(0, 1f);
        labelRect.pivot = new Vector2(0, 1f);
        labelRect.anchoredPosition = new Vector2(0, 0);
        labelRect.sizeDelta = new Vector2(100, 20);
        
        TextMeshProUGUI labelComponent = labelObj.AddComponent<TextMeshProUGUI>();
        labelComponent.text = labelText;
        labelComponent.fontSize = labelFontSize;
        labelComponent.color = labelColor;
        if (labelFont != null) labelComponent.font = labelFont;
        labelComponent.alignment = TextAlignmentOptions.Left;
        
        // Create slider
        Slider slider = CreateSlider(groupRect, labelText + " Slider", new Vector2(110, -10));
        
        // Create value text
        GameObject valueTextObj = new GameObject(labelText + " Value");
        RectTransform valueRect = valueTextObj.AddComponent<RectTransform>();
        valueRect.SetParent(groupRect, false);
        valueRect.anchorMin = new Vector2(1, 0.5f);
        valueRect.anchorMax = new Vector2(1, 0.5f);
        valueRect.pivot = new Vector2(1, 0.5f);
        valueRect.anchoredPosition = new Vector2(80, 0);
        valueRect.sizeDelta = new Vector2(60, 20);
        
        TextMeshProUGUI valueText = valueTextObj.AddComponent<TextMeshProUGUI>();
        valueText.text = "100%";
        valueText.fontSize = labelFontSize;
        valueText.color = labelColor;
        if (labelFont != null) valueText.font = labelFont;
        valueText.alignment = TextAlignmentOptions.Right;
        
        // Store references based on label
        if (labelText.Contains("Music"))
        {
            _musicSlider = slider;
            _musicValueText = valueText;
        }
        else if (labelText.Contains("SFX"))
        {
            _sfxSlider = slider;
            _sfxValueText = valueText;
        }
        
        return controlGroup;
    }
    
    private Slider CreateSlider(Transform parent, string name, Vector2 position)
    {
        Slider slider;
        
        if (sliderPrefab != null)
        {
            // Instantiate from prefab if available
            GameObject sliderObj = Instantiate(sliderPrefab.gameObject, parent);
            sliderObj.name = name;
            slider = sliderObj.GetComponent<Slider>();
            RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
            sliderRect.anchoredPosition = position;
            sliderRect.sizeDelta = new Vector2(sliderWidth, sliderHeight);
        }
        else
        {
            // Create slider from scratch
            GameObject sliderObj = new GameObject(name);
            RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
            sliderRect.SetParent(parent, false);
            sliderRect.anchorMin = new Vector2(0, 0.5f);
            sliderRect.anchorMax = new Vector2(0, 0.5f);
            sliderRect.pivot = new Vector2(0, 0.5f);
            sliderRect.anchoredPosition = position;
            sliderRect.sizeDelta = new Vector2(sliderWidth, sliderHeight);
            
            slider = sliderObj.AddComponent<Slider>();
            
            // Create background
            GameObject background = new GameObject("Background");
            RectTransform bgRect = background.AddComponent<RectTransform>();
            bgRect.SetParent(sliderRect, false);
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            Image bgImage = background.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            
            // Create fill area
            GameObject fillArea = new GameObject("Fill Area");
            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.SetParent(sliderRect, false);
            fillAreaRect.anchorMin = new Vector2(0, 0.5f);
            fillAreaRect.anchorMax = new Vector2(1, 0.5f);
            fillAreaRect.sizeDelta = new Vector2(-20, 10);
            fillAreaRect.anchoredPosition = new Vector2(-5, 0);
            
            // Create fill
            GameObject fill = new GameObject("Fill");
            RectTransform fillRect = fill.AddComponent<RectTransform>();
            fillRect.SetParent(fillAreaRect, false);
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(1, 1);
            fillRect.sizeDelta = Vector2.zero;
            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.2f, 0.7f, 1f, 1f);
            
            // Create handle area
            GameObject handleArea = new GameObject("Handle Slide Area");
            RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
            handleAreaRect.SetParent(sliderRect, false);
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.sizeDelta = new Vector2(-20, 0);
            handleAreaRect.anchoredPosition = Vector2.zero;
            
            // Create handle
            GameObject handle = new GameObject("Handle");
            RectTransform handleRect = handle.AddComponent<RectTransform>();
            handleRect.SetParent(handleAreaRect, false);
            handleRect.sizeDelta = new Vector2(20, 20);
            Image handleImage = handle.AddComponent<Image>();
            handleImage.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            
            // Configure slider
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
        }
        
        return slider;
    }
} 