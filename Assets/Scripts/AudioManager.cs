using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

public enum SEType
{
    Click,
    Pop,
    Match,
    Land,
    Attack,
    Magic,
    Heal,
    Shield,
    Exp,
    Key,
    WaveStart,
    GameOver,
    Hit,
    EnemyDie,
    ShieldBlock,
    Select,
    Title
    }

[System.Serializable]
public struct SEClip
{
    public SEType Type;
    public AudioClip Clip;
    public bool UseReverb;
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private SEClip[] clips;
    [SerializeField] private int poolSize = 16;
    [SerializeField] private AudioMixerGroup bypassGroup;
    [SerializeField] private AudioMixerGroup reverbGroup;

    private Dictionary<SEType, AudioClip> clipDictionary = new Dictionary<SEType, AudioClip>();
    private Dictionary<SEType, bool> reverbDictionary = new Dictionary<SEType, bool>();
    private Dictionary<SEType, int> lastPlayFrame = new Dictionary<SEType, int>();
    private List<AudioSource> sourcePool = new List<AudioSource>();
    private int nextSourceIndex = 0;

    private void Awake()
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

        foreach (var sc in clips)
        {
            clipDictionary[sc.Type] = sc.Clip;
            reverbDictionary[sc.Type] = sc.UseReverb;
            lastPlayFrame[sc.Type] = -1;
        }

        for (int i = 0; i < poolSize; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            sourcePool.Add(source);
        }
    }

    public void PlaySE(SEType type, float volume = 1.0f, float pitch = 1.0f)
    {
        if (lastPlayFrame.ContainsKey(type) && lastPlayFrame[type] == Time.frameCount)
        {
            // Already played this SE in this frame
            return;
        }

        if (!clipDictionary.ContainsKey(type) || clipDictionary[type] == null)
        {
            // Debug.LogWarning($"SE {type} not found or clip is null.");
            return;
        }

        lastPlayFrame[type] = Time.frameCount;

        AudioSource source = sourcePool[nextSourceIndex];
        source.pitch = pitch;

        // Routing to mixer group
        if (reverbDictionary.ContainsKey(type) && reverbDictionary[type])
        {
            source.outputAudioMixerGroup = reverbGroup;
        }
        else
        {
            source.outputAudioMixerGroup = bypassGroup;
        }

        source.PlayOneShot(clipDictionary[type], volume);

        nextSourceIndex = (nextSourceIndex + 1) % poolSize;
    }

    public void PlaySEWithRandomPitch(SEType type, float volume = 1.0f, float pitchRange = 0.1f)
    {
        float pitch = 1.0f + Random.Range(-pitchRange, pitchRange);
        PlaySE(type, volume, pitch);
    }
}
