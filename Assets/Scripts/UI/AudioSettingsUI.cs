using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Handles UI controls for audio settings, connecting sliders to the AudioManager.
/// </summary>
public class AudioSettingsUI : MonoBehaviour
{
    [Header("Music Volume")]
    [Tooltip("Slider that controls music volume")]
    [SerializeField] private Slider musicVolumeSlider;
    [Tooltip("Optional text to display music volume percentage")]
    [SerializeField] private TextMeshProUGUI musicVolumeText;

    [Header("SFX Volume")]
    [Tooltip("Slider that controls SFX volume")]
    [SerializeField] private Slider sfxVolumeSlider;
    [Tooltip("Optional text to display SFX volume percentage")]
    [SerializeField] private TextMeshProUGUI sfxVolumeText;

    private AudioManager _audioManager;
    private bool _updatingUI = false; // Flag to prevent feedback loops

    // Public methods to set references externally (used by AudioSettingsPanel)
    public void SetSliders(Slider musicSlider, Slider sfxSlider)
    {
        musicVolumeSlider = musicSlider;
        sfxVolumeSlider = sfxSlider;
        
        // Re-initialize if AudioManager is already found
        if (_audioManager != null)
        {
            InitializeSliders();
        }
    }
    
    public void SetValueTexts(TextMeshProUGUI musicText, TextMeshProUGUI sfxText)
    {
        musicVolumeText = musicText;
        sfxVolumeText = sfxText;
        
        // Update texts if AudioManager is already found
        if (_audioManager != null)
        {
            UpdateMusicVolumeText(_audioManager.GetMusicVolume());
            UpdateSFXVolumeText(_audioManager.GetSFXVolume());
        }
    }

    void Start()
    {
        // Find the AudioManager
        _audioManager = AudioManager.Instance;
        if (_audioManager == null)
        {
            Debug.LogError("[AudioSettingsUI] AudioManager instance not found!");
            enabled = false; // Disable this component
            return;
        }

        InitializeSliders();
    }
    
    private void InitializeSliders()
    {
        // Set up the music volume slider
        if (musicVolumeSlider != null)
        {
            // Configure slider range
            musicVolumeSlider.minValue = 0f;
            musicVolumeSlider.maxValue = 1f;
            
            // Set initial value from AudioManager
            _updatingUI = true;
            musicVolumeSlider.value = _audioManager.GetMusicVolume();
            UpdateMusicVolumeText(musicVolumeSlider.value);
            _updatingUI = false;

            // Add listener for value changes
            musicVolumeSlider.onValueChanged.RemoveAllListeners(); // Clear any existing listeners
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }
        else
        {
            Debug.LogWarning("[AudioSettingsUI] Music volume slider not assigned!");
        }

        // Set up the SFX volume slider
        if (sfxVolumeSlider != null)
        {
            // Configure slider range
            sfxVolumeSlider.minValue = 0f;
            sfxVolumeSlider.maxValue = 1f;
            
            // Set initial value from AudioManager
            _updatingUI = true;
            sfxVolumeSlider.value = _audioManager.GetSFXVolume();
            UpdateSFXVolumeText(sfxVolumeSlider.value);
            _updatingUI = false;

            // Add listener for value changes
            sfxVolumeSlider.onValueChanged.RemoveAllListeners(); // Clear any existing listeners
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }
        else
        {
            Debug.LogWarning("[AudioSettingsUI] SFX volume slider not assigned!");
        }
    }

    private void OnMusicVolumeChanged(float value)
    {
        if (_updatingUI) return; // Skip if we're just updating UI

        // Update the AudioManager
        _audioManager.SetMusicVolume(value);
        
        // Update the display text
        UpdateMusicVolumeText(value);
    }

    private void OnSFXVolumeChanged(float value)
    {
        if (_updatingUI) return; // Skip if we're just updating UI

        // Update the AudioManager
        _audioManager.SetSFXVolume(value);
        
        // Play a sample sound when the slider changes
        if (_audioManager != null)
        {
            _audioManager.PlayClickSound(0.5f); // Play at half volume to not be too loud
        }
        
        // Update the display text
        UpdateSFXVolumeText(value);
    }

    private void UpdateMusicVolumeText(float value)
    {
        if (musicVolumeText != null)
        {
            musicVolumeText.text = $"{Mathf.RoundToInt(value * 100)}%";
        }
    }

    private void UpdateSFXVolumeText(float value)
    {
        if (sfxVolumeText != null)
        {
            sfxVolumeText.text = $"{Mathf.RoundToInt(value * 100)}%";
        }
    }

    private void OnDestroy()
    {
        // Remove listeners to prevent memory leaks
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
        }
        
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.RemoveListener(OnSFXVolumeChanged);
        }
    }
} 