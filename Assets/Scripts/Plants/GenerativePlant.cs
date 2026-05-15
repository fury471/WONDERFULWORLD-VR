using UnityEngine;
using ButterflyHouse.Core;

namespace ButterflyHouse.Plants
{
    /// <summary>
    /// Interactive generative plant that responds to touch with visual and audio feedback.
    /// Acts as an audio-visual "instrument" in the space.
    /// </summary>
    public class GenerativePlant : MonoBehaviour
    {
        [Header("Audio")]
        [SerializeField] private AudioClip[] touchClips;
        [SerializeField] private AudioClip arpeggioClip;
        [SerializeField] private AudioSource audioSource;
        [Range(0f, 1f)]
        [SerializeField] private float audioVolume = 0.8f;
        
        [Header("Visual")]
        [SerializeField] private PlantVisualController visualController;
        [Range(0.05f, 0.5f)]
        [SerializeField] private float maxOscillationAmplitude = 0.1f;
        [Range(0.5f, 3f)]
        [SerializeField] private float oscillationSpeed = 1f;
        
        [Header("Interaction")]
        [SerializeField] private Collider[] touchableColliders;
        [SerializeField] private float touchCooldown = 0.5f;
        [SerializeField] private bool allowMultipleTouches = false;
        
        private float _lastTouchTime;
        private int _concurrentTouches;
        
        private void Awake()
        {
            if (visualController == null)
                visualController = GetComponent<PlantVisualController>();
            
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
            
            if (touchableColliders == null || touchableColliders.Length == 0)
            {
                // Auto-find colliders if not assigned
                touchableColliders = GetComponentsInChildren<Collider>();
            }
            
            // Set up audio source
            if (audioSource != null)
            {
                audioSource.playOnAwake = false;
                audioSource.loop = false;
                audioSource.spatialBlend = 1f; // 3D sound
            }
        }
        
        private void Start()
        {
            // Register with PlantManager
            if (PlantManager.Instance != null)
            {
                PlantManager.Instance.RegisterPlant(this);
            }
            
            // Apply settings
            if (Settings.Instance != null && audioSource != null)
            {
                audioSource.volume = audioVolume * Settings.Instance.plantVolume;
            }
        }
        
        private void OnDestroy()
        {
            // Unregister from PlantManager
            if (PlantManager.Instance != null)
            {
                PlantManager.Instance.UnregisterPlant(this);
            }
        }
        
        private void Update()
        {
            // Update visual oscillation
            if (visualController != null)
            {
                float oscillation = Mathf.Sin(Time.time * oscillationSpeed) * maxOscillationAmplitude;
                visualController.SetOscillation(oscillation);
            }
        }
        
        /// <summary>
        /// Called when the plant is touched by a hand or interaction.
        /// </summary>
        public void OnTouched(Vector3 touchPoint)
        {
            // Check cooldown
            if (Time.time - _lastTouchTime < touchCooldown && !allowMultipleTouches)
                return;
            
            _lastTouchTime = Time.time;
            _concurrentTouches++;
            
            // Visual feedback
            if (visualController != null)
            {
                visualController.PulseAtPoint(touchPoint);
            }
            
            // Audio feedback
            PlayRandomSound();
            
            // Notify ecosystem orchestrator
            if (Core.EcosystemOrchestrator.Instance != null)
            {
                Core.EcosystemOrchestrator.Instance.RegisterPlantTouch(this);
            }
            
            // Also notify ecosystem state controller for compatibility
            if (Core.EcosystemStateController.Instance != null)
            {
                Core.EcosystemStateController.Instance.OnPlayerPlantInteraction();
            }
            
            // Notify plant growth system
            PlantGrowthSystem growthSystem = GetComponent<PlantGrowthSystem>();
            if (growthSystem != null)
            {
                growthSystem.OnTouched();
            }
            
            // Reset concurrent touches after delay
            Invoke(nameof(DecrementConcurrentTouches), 0.3f);
        }
        
        private void DecrementConcurrentTouches()
        {
            _concurrentTouches = Mathf.Max(0, _concurrentTouches - 1);
        }
        
        /// <summary>
        /// Play a random sound from the touch clips.
        /// </summary>
        private void PlayRandomSound()
        {
            if (audioSource == null || touchClips == null || touchClips.Length == 0)
                return;
            
            AudioClip clip = touchClips[Random.Range(0, touchClips.Length)];
            if (clip != null)
            {
                float volume = audioVolume;
                if (Settings.Instance != null)
                {
                    volume *= Settings.Instance.plantVolume;
                }
                
                audioSource.PlayOneShot(clip, volume);
            }
        }
        
        /// <summary>
        /// Play an arpeggio sequence (if assigned).
        /// </summary>
        public void PlayArpeggio()
        {
            if (audioSource == null || arpeggioClip == null)
                return;
            
            float volume = audioVolume;
            if (Settings.Instance != null)
            {
                volume *= Settings.Instance.plantVolume;
            }
            
            audioSource.PlayOneShot(arpeggioClip, volume);
        }
        
        /// <summary>
        /// Check if any touchable collider contains the given point.
        /// </summary>
        public bool IsPointOnPlant(Vector3 point)
        {
            foreach (var collider in touchableColliders)
            {
                if (collider != null && collider.bounds.Contains(point))
                {
                    return true;
                }
            }
            return false;
        }
        
        private void OnTriggerEnter(Collider other)
        {
            // Check if this is a hand proxy or interaction
            var handProxy = other.GetComponent<Interaction.HandProxy>();
            if (handProxy != null)
            {
                Vector3 touchPoint = other.ClosestPoint(transform.position);
                OnTouched(touchPoint);
            }
        }
        
        private void OnCollisionEnter(Collision collision)
        {
            var handProxy = collision.gameObject.GetComponent<Interaction.HandProxy>();
            if (handProxy != null)
            {
                Vector3 touchPoint = collision.contacts[0].point;
                OnTouched(touchPoint);
            }
        }
    }
}

