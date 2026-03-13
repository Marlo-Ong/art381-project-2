using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class RootSoundEffectPlayer : MonoBehaviour
{
    public const int TokenCollectedSoundEffectIndex = 0;
    public const int TokensDepositedSoundEffectIndex = 1;

    public static RootSoundEffectPlayer Instance { get; private set; }

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] soundEffects = new AudioClip[0];
    [SerializeField, Range(0f, 1f)] private float volumeScale = 1f;

    public int SoundEffectCount => soundEffects != null ? soundEffects.Length : 0;

    private void Reset()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Debug.LogWarning("Multiple RootSoundEffectPlayer instances are active. The newest instance will be used.", this);

        Instance = this;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static RootSoundEffectPlayer FindInstance()
    {
        if (Instance == null)
            Instance = FindFirstObjectByType<RootSoundEffectPlayer>(FindObjectsInactive.Include);

        return Instance;
    }

    public void PlaySoundEffect(int soundEffectIndex)
    {
        if (audioSource == null)
        {
            Debug.LogWarning("RootSoundEffectPlayer requires an AudioSource reference.", this);
            return;
        }

        if (soundEffects == null || soundEffectIndex < 0 || soundEffectIndex >= soundEffects.Length)
        {
            Debug.LogWarning($"Sound effect index {soundEffectIndex} is out of range.", this);
            return;
        }

        var clip = soundEffects[soundEffectIndex];
        if (clip == null)
        {
            Debug.LogWarning($"Sound effect slot {soundEffectIndex} does not have an AudioClip assigned.", this);
            return;
        }

        audioSource.PlayOneShot(clip, volumeScale);
    }

    public void StopSoundEffect()
    {
        if (audioSource != null)
            audioSource.Stop();
    }
}
