using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    // PlayerPrefs keys for saving volume settings
    private const string MUSIC_VOLUME_KEY = "MusicVolume";
    private const string SFX_VOLUME_KEY = "SFXVolume";

    // Master volume levels for categories (0-1)
    private float _musicVolume = 0.5f;
    private float _sfxVolume = 1.0f;

    [Header("Audio Sources")]
    [Tooltip("AudioSource dedicated to playing click sounds.")]
    [SerializeField] private AudioSource clickSoundSource;
    [Tooltip("AudioSource dedicated to playing collect/item sounds (e.g., lemon).")]
    [SerializeField] private AudioSource collectSoundSource;
    [Tooltip("AudioSource dedicated to playing background music.")]
    [SerializeField] private AudioSource musicAudioSource;

    // Lists to categorize audio sources
    private List<AudioSource> _sfxSources = new List<AudioSource>();
    private List<AudioSource> _musicSources = new List<AudioSource>();

    [Header("Audio Clips")]
    [Tooltip("The AudioClip for the standard click sound.")]
    [SerializeField] private AudioClip clickAudioClip;
    [Tooltip("The AudioClip for collecting items (e.g., lemon).")]
    [SerializeField] private AudioClip collectAudioClip;
    [Tooltip("The AudioClip for the background music loop.")]
    [SerializeField] private AudioClip musicAudioClip;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Ensure AudioSource components exist - AddComponent if necessary
        clickSoundSource = EnsureAudioSource(clickSoundSource, "ClickSoundSource");
        collectSoundSource = EnsureAudioSource(collectSoundSource, "CollectSoundSource");
        musicAudioSource = EnsureAudioSource(musicAudioSource, "MusicAudioSource");

        // Configure sources
        ConfigureSource(clickSoundSource, false, false); // No loop, no play on awake
        ConfigureSource(collectSoundSource, false, false); // No loop, no play on awake
        ConfigureSource(musicAudioSource, true, false);    // Loop, no play on awake

        // Categorize sources
        _sfxSources.Clear();
        _sfxSources.Add(clickSoundSource);
        _sfxSources.Add(collectSoundSource);
        
        _musicSources.Clear();
        _musicSources.Add(musicAudioSource);

        // Load saved volume settings
        LoadVolumeSettings();
    }

    // Load volume settings from PlayerPrefs
    private void LoadVolumeSettings()
    {
        _musicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, 0.5f);
        _sfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 1.0f);
        
        // Apply the loaded settings
        ApplyMusicVolume();
        ApplySFXVolume();
        
        Debug.Log($"[AudioManager] Loaded volume settings - Music: {_musicVolume}, SFX: {_sfxVolume}");
    }

    // Save volume settings to PlayerPrefs
    private void SaveVolumeSettings()
    {
        PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, _musicVolume);
        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, _sfxVolume);
        PlayerPrefs.Save();
    }

    // Helper to find or add an AudioSource
    private AudioSource EnsureAudioSource(AudioSource source, string name)
    {
        if (source == null)
        {
            // Try to find an existing one by name if multiple sources exist
            AudioSource[] sources = GetComponents<AudioSource>();
            foreach(var s in sources) {
                if(s.gameObject.name == name) { // Check if a previous run named it
                    source = s;
                    break;
                }
            }
             // If still not found, add a new one
             if (source == null) {
                source = gameObject.AddComponent<AudioSource>();
                source.gameObject.name = name; // Name it for easier identification
                Debug.LogWarning($"[AudioManager] Added missing AudioSource: {name}");
            }
        }
        return source;
    }

     // Helper to configure common AudioSource settings
    private void ConfigureSource(AudioSource source, bool loop, bool playOnAwake) {
        if (source != null) {
            source.loop = loop;
            source.playOnAwake = playOnAwake;
        }
    }

    // --- Volume Control Methods ---

    // Get the current music volume (0-1)
    public float GetMusicVolume()
    {
        return _musicVolume;
    }

    // Get the current SFX volume (0-1)
    public float GetSFXVolume()
    {
        return _sfxVolume;
    }

    // Set music volume and save to PlayerPrefs
    public void SetMusicVolume(float volume)
    {
        _musicVolume = Mathf.Clamp01(volume);
        ApplyMusicVolume();
        SaveVolumeSettings();
        Debug.Log($"[AudioManager] Set music volume to {_musicVolume}");
    }

    // Set SFX volume and save to PlayerPrefs
    public void SetSFXVolume(float volume)
    {
        _sfxVolume = Mathf.Clamp01(volume);
        ApplySFXVolume();
        SaveVolumeSettings();
        Debug.Log($"[AudioManager] Set SFX volume to {_sfxVolume}");
    }

    // Apply current music volume to all music sources
    private void ApplyMusicVolume()
    {
        foreach (var source in _musicSources)
        {
            if (source != null)
            {
                source.volume = _musicVolume;
            }
        }
    }

    // Apply current SFX volume to all SFX sources
    private void ApplySFXVolume()
    {
        // We don't set volume directly for SFX sources as they use PlayOneShot with volume parameter
    }

    // --- Sound Playback Methods ---

    public void PlayClickSound(float volume = 1.0f)
    {
        PlayClipOnSource(clickSoundSource, clickAudioClip, volume * _sfxVolume);
    }

    public void PlayCollectSound(float volume = 1.0f)
    {
         PlayClipOnSource(collectSoundSource, collectAudioClip, volume * _sfxVolume);
    }

    public void PlayMusic(float volume = 0.5f)
    {
        if (musicAudioClip == null)
        {
            Debug.LogWarning("[AudioManager] PlayMusic: musicAudioClip is not assigned.");
            return;
        }
        if (musicAudioSource == null)
        {
             Debug.LogError("[AudioManager] PlayMusic: musicAudioSource is null.");
             return;
        }

        // Avoid restarting if the same music is already playing
        if (musicAudioSource.clip == musicAudioClip && musicAudioSource.isPlaying)
        {
            return;
        }

        musicAudioSource.clip = musicAudioClip;
        musicAudioSource.volume = _musicVolume; // Use the master music volume
        musicAudioSource.Play();
        Debug.Log($"[AudioManager] Started playing music: {musicAudioClip.name}");
    }

    // --- Control Methods ---

    public void StopMusic()
    {
        if (musicAudioSource == null) return;
        musicAudioSource.Stop();
        Debug.Log($"[AudioManager] Stopped music.");
    }

    // Add new audio source to the appropriate category
    public void RegisterAudioSource(AudioSource source, bool isMusic)
    {
        if (source == null) return;
        
        if (isMusic)
        {
            if (!_musicSources.Contains(source))
            {
                _musicSources.Add(source);
                source.volume = _musicVolume; // Apply current music volume
            }
        }
        else
        {
            if (!_sfxSources.Contains(source))
            {
                _sfxSources.Add(source);
            }
        }
    }

     // Helper to play a specific clip on a specific source
    private void PlayClipOnSource(AudioSource source, AudioClip clip, float volume)
    {
        if (clip == null)
        {
            Debug.LogWarning($"[AudioManager] Tried to play a sound on '{source?.name ?? "null source"}' but the AudioClip is null.");
            return;
        }
        if (source == null)
        {
             Debug.LogError($"[AudioManager] Cannot play sound '{clip.name}', the designated AudioSource is null.");
             return;
        }
        source.PlayOneShot(clip, Mathf.Clamp01(volume));
    }
}
