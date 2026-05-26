using System;
using UnityEngine;
using System.Collections.Generic;
using Random = UnityEngine.Random;

[System.Serializable]
public class Sound
{
    public string name;           // The name you call in code (e.g., "Splat")
    public AudioClip[] clips;     // Drag your 3 variations here
    
    [Range(0f, 1f)]
    public float volume = 0.7f;
    
    [Range(0.1f, 3f)]
    public float pitch = 1f;

    [Header("Randomization")]
    [Range(0f, 0.5f)]
    public float pitchVariance = 0.1f; // How much the pitch wobbles (0.1 is subtle)
    [Range(0f, 0.5f)]
    public float volumeVariance = 0.1f; // How much volume changes
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Sound List")]
    public Sound[] sounds;

    private AudioSource sfxSource;
    private AudioSource musicSource;
    
    // Fast lookup so we don't loop through the list every time
    private Dictionary<string, Sound> soundDictionary;

    void Awake()
    {
        // Singleton Setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keep audio between scenes
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Initialize Sources
        sfxSource = gameObject.AddComponent<AudioSource>();
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;

        // Build Dictionary
        soundDictionary = new Dictionary<string, Sound>();
        foreach (Sound s in sounds)
        {
            if (!soundDictionary.ContainsKey(s.name))
            {
                soundDictionary.Add(s.name, s);
            }
            else
            {
                Debug.LogWarning("Duplicate sound name found: " + s.name);
            }
        }
    }

    private void Start()
    {
        PlayMusic("BackgroundMusic");
    }

    public void Play(string soundName)
    {
        if (soundDictionary.TryGetValue(soundName, out Sound s))
        {
            // 1. Pick a random clip from the array
            if (s.clips.Length == 0) return;
            AudioClip clip = s.clips[Random.Range(0, s.clips.Length)];

            // 2. Randomize Pitch
            // Example: If Pitch is 1.0 and Variance is 0.1, random between 0.9 and 1.1
            sfxSource.pitch = s.pitch + Random.Range(-s.pitchVariance, s.pitchVariance);

            // 3. Randomize Volume
            sfxSource.volume = s.volume + Random.Range(-s.volumeVariance, s.volumeVariance);

            // 4. Play
            sfxSource.PlayOneShot(clip);
        }
        else
        {
            Debug.LogWarning("Sound not found: " + soundName);
        }
    }
    
    // Optional: Play a loop (Background Noise)
    public void PlayMusic(string soundName)
    {
        if (soundDictionary.TryGetValue(soundName, out Sound s))
        {
            musicSource.clip = s.clips[0];
            musicSource.volume = s.volume;
            musicSource.Play();
        }
    }
}