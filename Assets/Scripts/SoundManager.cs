using UnityEngine;

public class SoundManager : MonoBehaviour
{
    // Existing AudioClips for other sounds
    public AudioClip explosionSound, collisionSound, flameSound, shakeSound, gameOverSound;
    public AudioClip backgroundSoundLevel1, backgroundSoundLevel2, backgroundSoundLevel3;

    // New AudioClips for level-specific firing sounds
    public AudioClip fireSingleSoundLevel1, fireSingleSoundLevel2, fireSingleSoundLevel3;
    public AudioClip fireAllSoundLevel1, fireAllSoundLevel2, fireAllSoundLevel3;

    // Keep the existing fireSingleSound and fireAllSound for backward compatibility (optional)
    public AudioClip fireSingleSound, fireAllSound;

    private AudioSource audioSource;
    private AudioSource backgroundAudioSource;
    private bool isBackgroundSoundEnabled = true;
    private bool isExplosionSoundEnabled = true;
    private bool isInteractionSoundEnabled = true;
    private int currentLevel = 1;

    public static SoundManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        backgroundAudioSource = gameObject.AddComponent<AudioSource>();
        backgroundAudioSource.playOnAwake = false;
        UpdateBackgroundSound(1);
    }

    public void UpdateBackgroundSound(int level)
    {
        currentLevel = level;
        AudioClip backgroundClip = level switch
        {
            1 => backgroundSoundLevel1,
            2 => backgroundSoundLevel2,
            3 => backgroundSoundLevel3,
            _ => backgroundSoundLevel1
        };

        if (backgroundClip == null) return;
        backgroundAudioSource.clip = backgroundClip;
        backgroundAudioSource.loop = true;
        backgroundAudioSource.volume = 0.3f;
        if (isBackgroundSoundEnabled && !backgroundAudioSource.isPlaying) backgroundAudioSource.Play();
    }

    public void PlaySound(string soundType)
    {
        if (!isInteractionSoundEnabled && soundType != "Background") return;

        AudioClip clip = soundType switch
        {
            // Level-specific firing sounds
            "FireSingle" => currentLevel switch
            {
                1 => fireSingleSoundLevel1 ?? fireSingleSound,
                2 => fireSingleSoundLevel2 ?? fireSingleSound,
                3 => fireSingleSoundLevel3 ?? fireSingleSound,
                _ => fireSingleSound
            },
            "FireAll" => currentLevel switch
            {
                1 => fireAllSoundLevel1 ?? fireAllSound,
                2 => fireAllSoundLevel2 ?? fireAllSound,
                3 => fireAllSoundLevel3 ?? fireAllSound,
                _ => fireAllSound
            },
            // Other sounds remain unchanged
            "Explosion" => explosionSound,
            "Collision" => collisionSound,
            "Flame" => flameSound,
            "Shake" => shakeSound,
            "GameOver" => gameOverSound,
            _ => null
        };

        if (clip == null) return;
        if (soundType == "Explosion" && !isExplosionSoundEnabled) return;

        float volume = soundType switch
        {
            "FireSingle" => 0.7f,
            "FireAll" => 1.0f,
            "Explosion" => 0.8f,
            "Collision" => 0.6f,
            "Flame" => 0.5f,
            "Shake" => 0.4f,
            "GameOver" => 0.9f,
            _ => 1.0f
        };

        audioSource.Stop();
        audioSource.PlayOneShot(clip, volume);
    }

    public void StopSound(string soundType)
    {
        if (soundType == "Background" && backgroundAudioSource.isPlaying)
            backgroundAudioSource.Stop();
        else if (audioSource.isPlaying && audioSource.clip == GetClip(soundType))
            audioSource.Stop();
    }

    public void StopAllSounds()
    {
        audioSource.Stop();
        backgroundAudioSource.Stop();
    }

    private AudioClip GetClip(string soundType)
    {
        return soundType switch
        {
            // Update GetClip to handle level-specific firing sounds
            "FireSingle" => currentLevel switch
            {
                1 => fireSingleSoundLevel1 ?? fireSingleSound,
                2 => fireSingleSoundLevel2 ?? fireSingleSound,
                3 => fireSingleSoundLevel3 ?? fireSingleSound,
                _ => fireSingleSound
            },
            "FireAll" => currentLevel switch
            {
                1 => fireAllSoundLevel1 ?? fireAllSound,
                2 => fireAllSoundLevel2 ?? fireAllSound,
                3 => fireAllSoundLevel3 ?? fireAllSound,
                _ => fireAllSound
            },
            "Explosion" => explosionSound,
            "Collision" => collisionSound,
            "Flame" => flameSound,
            "Shake" => shakeSound,
            "GameOver" => gameOverSound,
            _ => null
        };
    }

    public void ToggleBackgroundSound(bool enable)
    {
        isBackgroundSoundEnabled = enable;
        if (enable && !backgroundAudioSource.isPlaying) UpdateBackgroundSound(currentLevel);
        else if (!enable && backgroundAudioSource.isPlaying) backgroundAudioSource.Stop();
    }

    public void ToggleExplosionSound(bool enable)
    {
        isExplosionSoundEnabled = enable;
    }

    public void ToggleInteractionSound(bool enable)
    {
        isInteractionSoundEnabled = enable;
        if (!enable) audioSource.Stop();
    }

    public bool IsBackgroundSoundEnabled => isBackgroundSoundEnabled;
    public bool IsExplosionSoundEnabled => isExplosionSoundEnabled;
    public bool IsInteractionSoundEnabled => isInteractionSoundEnabled;
}