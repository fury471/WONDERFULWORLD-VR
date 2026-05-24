using UnityEngine;

namespace ButterflyHouse.Butterflies
{
    /// <summary>
    /// Energy system for butterflies. Tracks energy level and determines when butterflies need to feed.
    /// </summary>
    public class ButterflyEnergy : MonoBehaviour
    {
        [Header("Energy Settings")]
        [Range(0f, 5f)]
        [SerializeField] private float maxEnergy = 3f;
        [Range(0f, 1f)]
        [SerializeField] private float lowEnergyThreshold = 0.5f;
        [SerializeField] private float energyDecayRate = 0.1f; // Energy lost per second
        [SerializeField] private float energyDecayStartDelay = 10f; // Wait before starting decay
        
        [Header("Current State")]
        [Range(0f, 5f)]
        [SerializeField] private float currentEnergy = 3f;
        [SerializeField] private float timeSinceSpawn = 0f;
        
        private Butterfly _butterfly;

        // Energy decay is a slow continuous process — accumulate deltaTime and run the actual decay
        // at a lower frequency. Saves N butterflies × per-frame branch / event-fire overhead.
        private const float DECAY_TICK_INTERVAL = 0.25f;
        private float _decayTickTimer;

        // Events
        public System.Action<float> OnEnergyChanged;
        public System.Action OnEnergyDepleted;
        public System.Action OnEnergyFull;
        
        public float CurrentEnergy => currentEnergy;
        public float MaxEnergy => maxEnergy;
        public bool NeedsEnergy => currentEnergy < lowEnergyThreshold;
        public float EnergyPercentage => maxEnergy > 0 ? (currentEnergy / maxEnergy) : 0f;
        
        private void Awake()
        {
            _butterfly = GetComponent<Butterfly>();
            currentEnergy = maxEnergy; // Start with full energy
        }
        
        private void Update()
        {
            timeSinceSpawn += Time.deltaTime;

            // Only tick decay periodically — the delta is still equivalent because we multiply by elapsed time.
            if (timeSinceSpawn <= energyDecayStartDelay) return;
            if (_butterfly == null || _butterfly.CurrentState != Butterfly.State.Flying) return;

            _decayTickTimer += Time.deltaTime;
            if (_decayTickTimer < DECAY_TICK_INTERVAL) return;
            float elapsed = _decayTickTimer;
            _decayTickTimer = 0f;

            float oldEnergy = currentEnergy;
            currentEnergy = Mathf.Max(0f, currentEnergy - energyDecayRate * elapsed);

            if (Mathf.Abs(currentEnergy - oldEnergy) > 0.01f)
            {
                OnEnergyChanged?.Invoke(currentEnergy);
            }

            // Check for energy depletion
            if (oldEnergy > 0f && currentEnergy <= 0f)
            {
                OnEnergyDepleted?.Invoke();
            }
        }
        
        /// <summary>
        /// Feed energy from a fruit.
        /// </summary>
        public void FeedFromFruit(Plants.GenerativeFruit fruit)
        {
            if (fruit == null) return;
            
            float energyGain = fruit.EnergyOutput * Time.deltaTime;
            float oldEnergy = currentEnergy;
            
            currentEnergy = Mathf.Clamp(currentEnergy + energyGain, 0f, maxEnergy);
            
            if (Mathf.Abs(currentEnergy - oldEnergy) > 0.01f)
            {
                OnEnergyChanged?.Invoke(currentEnergy);
                
                // Check if energy is now full
                if (oldEnergy < maxEnergy && currentEnergy >= maxEnergy)
                {
                    OnEnergyFull?.Invoke();
                }
            }
        }
        
        /// <summary>
        /// Add energy directly (for testing or special events).
        /// </summary>
        public void AddEnergy(float amount)
        {
            float oldEnergy = currentEnergy;
            currentEnergy = Mathf.Clamp(currentEnergy + amount, 0f, maxEnergy);
            
            if (Mathf.Abs(currentEnergy - oldEnergy) > 0.01f)
            {
                OnEnergyChanged?.Invoke(currentEnergy);
                
                if (oldEnergy < maxEnergy && currentEnergy >= maxEnergy)
                {
                    OnEnergyFull?.Invoke();
                }
            }
        }
        
        /// <summary>
        /// Set energy to a specific value.
        /// </summary>
        public void SetEnergy(float energy)
        {
            currentEnergy = Mathf.Clamp(energy, 0f, maxEnergy);
            OnEnergyChanged?.Invoke(currentEnergy);
        }
        
        /// <summary>
        /// Reset energy to max.
        /// </summary>
        public void ResetEnergy()
        {
            currentEnergy = maxEnergy;
            OnEnergyChanged?.Invoke(currentEnergy);
        }
    }
}

