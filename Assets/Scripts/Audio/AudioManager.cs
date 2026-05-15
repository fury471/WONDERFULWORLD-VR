using UnityEngine;
using ButterflyHouse.Core;

namespace ButterflyHouse.Audio
{
    /// <summary>
    /// Central audio manager that orchestrates all audio in the butterfly house.
    /// Manages global mixing, reverb, and density-based audio adjustments.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }
        
        [Header("Audio Mixer")]
        [SerializeField] private UnityEngine.Audio.AudioMixerGroup masterMixerGroup;
        [SerializeField] private UnityEngine.Audio.AudioMixerGroup butterflyMixerGroup;
        [SerializeField] private UnityEngine.Audio.AudioMixerGroup plantMixerGroup;
        [SerializeField] private UnityEngine.Audio.AudioMixerGroup ambienceMixerGroup;
        
        [Header("Global Settings")]
        [Range(0f, 1f)]
        [SerializeField] private float masterVolume = 0.7f;
        [Range(0f, 1f)]
        [SerializeField] private float reverbLevel = 0.3f;
        
        [Header("Density Response")]
        [SerializeField] private AnimationCurve densityToReverbCurve = AnimationCurve.Linear(0f, 0.2f, 20f, 0.8f);
        [SerializeField] private AnimationCurve densityToMasterVolumeCurve = AnimationCurve.Linear(0f, 0.5f, 20f, 1f);
        
        [Header("Ambience")]
        [SerializeField] private AudioSource ambienceSource;
        [SerializeField] private AudioClip ambientClip;
        [Range(0f, 1f)]
        [SerializeField] private float ambienceVolume = 0.5f;
        [SerializeField] private bool playAmbienceOnStart = true;
        
        private int _lastButterflyCount = 0;
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Multiple AudioManager instances detected. Destroying duplicate.");
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            
            // Setup ambience
            if (ambienceSource == null)
            {
                GameObject ambienceObj = new GameObject("AmbienceSource");
                ambienceObj.transform.SetParent(transform);
                ambienceSource = ambienceObj.AddComponent<AudioSource>();
            }
            
            if (ambienceSource != null)
            {
                ambienceSource.loop = true;
                ambienceSource.playOnAwake = false;
                ambienceSource.spatialBlend = 0f; // 2D ambience
                
                if (ambienceMixerGroup != null)
                {
                    ambienceSource.outputAudioMixerGroup = ambienceMixerGroup;
                }
                
                if (ambientClip != null)
                {
                    ambienceSource.clip = ambientClip;
                }
            }
        }
        
        private void Start()
        {
            // Apply settings
            if (Settings.Instance != null)
            {
                masterVolume = Settings.Instance.masterVolume;
                ambienceVolume = Settings.Instance.ambienceVolume;
            }
            
            UpdateMasterVolume(masterVolume);
            UpdateAmbienceVolume(ambienceVolume);
            
            // Start ambience if enabled
            if (playAmbienceOnStart && ambienceSource != null && ambienceSource.clip != null)
            {
                ambienceSource.Play();
            }
        }
        
        private void Update()
        {
            // Update global mix based on butterfly density
            if (Butterflies.ButterflyManager.Instance != null)
            {
                int currentCount = Butterflies.ButterflyManager.Instance.ActiveButterflyCount;
                
                if (currentCount != _lastButterflyCount)
                {
                    UpdateGlobalMix(currentCount);
                    _lastButterflyCount = currentCount;
                }
            }
            
            // Sync with settings if they change
            if (Settings.Instance != null)
            {
                if (Mathf.Abs(masterVolume - Settings.Instance.masterVolume) > 0.01f)
                {
                    UpdateMasterVolume(Settings.Instance.masterVolume);
                }
                
                if (Mathf.Abs(ambienceVolume - Settings.Instance.ambienceVolume) > 0.01f)
                {
                    UpdateAmbienceVolume(Settings.Instance.ambienceVolume);
                }
            }
        }
        
        /// <summary>
        /// Update global audio mix based on butterfly density.
        /// </summary>
        public void UpdateGlobalMix(int butterflyCount)
        {
            // Update reverb based on density
            float newReverb = densityToReverbCurve.Evaluate(butterflyCount);
            UpdateReverb(newReverb);
            
            // Optionally adjust master volume based on density
            float newMasterVolume = densityToMasterVolumeCurve.Evaluate(butterflyCount);
            // UpdateMasterVolume(newMasterVolume * masterVolume);
        }
        
        /// <summary>
        /// Update master volume.
        /// </summary>
        public void UpdateMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            
            // Apply to audio mixer if available
            if (masterMixerGroup != null && masterMixerGroup.audioMixer != null)
            {
                // Convert 0-1 to decibels: dB = 20 * log10(volume)
                float db = volume > 0.0001f ? 20f * Mathf.Log10(volume) : -80f;
                masterMixerGroup.audioMixer.SetFloat("MasterVolume", db);
            }
        }
        
        /// <summary>
        /// Update reverb level.
        /// </summary>
        public void UpdateReverb(float level)
        {
            reverbLevel = Mathf.Clamp01(level);
            
            // Apply to audio mixer reverb if available
            if (masterMixerGroup != null && masterMixerGroup.audioMixer != null)
            {
                float db = level > 0.0001f ? 20f * Mathf.Log10(level) : -80f;
                masterMixerGroup.audioMixer.SetFloat("ReverbLevel", db);
            }
        }
        
        /// <summary>
        /// Update ambience volume.
        /// </summary>
        public void UpdateAmbienceVolume(float volume)
        {
            ambienceVolume = Mathf.Clamp01(volume);
            
            if (ambienceSource != null)
            {
                ambienceSource.volume = ambienceVolume;
            }
        }
        
        /// <summary>
        /// Get current master volume.
        /// </summary>
        public float GetMasterVolume() => masterVolume;
        
        /// <summary>
        /// Play a one-shot sound on the plant mixer group.
        /// </summary>
        public void PlayPlantSound(AudioClip clip, Vector3 position, float volume = 1f)
        {
            if (clip == null) return;
            
            // Create temporary audio source for one-shot
            GameObject tempObj = new GameObject("PlantSound");
            tempObj.transform.position = position;
            AudioSource tempSource = tempObj.AddComponent<AudioSource>();
            tempSource.clip = clip;
            tempSource.volume = volume * (Settings.Instance != null ? Settings.Instance.plantVolume : 1f);
            tempSource.spatialBlend = 1f; // 3D
            tempSource.outputAudioMixerGroup = plantMixerGroup;
            tempSource.Play();
            
            // Destroy after clip finishes
            Destroy(tempObj, clip.length + 0.1f);
        }
        
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}

