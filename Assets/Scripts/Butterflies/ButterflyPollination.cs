using UnityEngine;

namespace ButterflyHouse.Butterflies
{
    /// <summary>
    /// Pollination system for butterflies. Tracks pollen collection and deposition.
    /// Butterflies collect pollen from flowers and deposit it to other flowers or fruit.
    /// </summary>
    public class ButterflyPollination : MonoBehaviour
    {
        [Header("Pollen Settings")]
        [Range(0f, 5f)]
        [SerializeField] private float maxPollenCharge = 3f;
        [Range(0f, 1f)]
        [SerializeField] private float pollenDecayRate = 0.1f; // Pollen lost per second
        [SerializeField] private float pollenDecayStartDelay = 30f; // Wait before decay starts
        
        [Header("Current State")]
        [Range(0f, 5f)]
        [SerializeField] private float pollenCharge = 0f;
        [SerializeField] private float timeSincePollenCollection = 0f;
        
        private Butterfly _butterfly;
        
        // Events
        public System.Action<float> OnPollenChanged;
        public System.Action OnPollenDeposited;
        
        public float PollenCharge => pollenCharge;
        public float MaxPollenCharge => maxPollenCharge;
        public bool IsCarryingPollen => pollenCharge > 0.1f;
        public float PollenPercentage => maxPollenCharge > 0 ? (pollenCharge / maxPollenCharge) : 0f;
        
        private void Awake()
        {
            _butterfly = GetComponent<Butterfly>();
        }
        
        private void Update()
        {
            timeSincePollenCollection += Time.deltaTime;
            
            // Start pollen decay after delay
            if (timeSincePollenCollection > pollenDecayStartDelay && pollenCharge > 0f)
            {
                float oldCharge = pollenCharge;
                pollenCharge = Mathf.Max(0f, pollenCharge - pollenDecayRate * Time.deltaTime);
                
                if (Mathf.Abs(pollenCharge - oldCharge) > 0.01f)
                {
                    OnPollenChanged?.Invoke(pollenCharge);
                }
            }
        }
        
        /// <summary>
        /// Collect pollen from a flower.
        /// </summary>
        public void CollectPollen(float amount)
        {
            float oldCharge = pollenCharge;
            pollenCharge = Mathf.Clamp(pollenCharge + amount, 0f, maxPollenCharge);
            timeSincePollenCollection = 0f; // Reset decay timer
            
            if (Mathf.Abs(pollenCharge - oldCharge) > 0.01f)
            {
                OnPollenChanged?.Invoke(pollenCharge);
            }
        }
        
        /// <summary>
        /// Deposit pollen to a flower.
        /// </summary>
        public void DepositPollen(Flowers.Flower flower)
        {
            if (flower == null) return;
            if (!IsCarryingPollen) return;
            
            float depositedAmount = pollenCharge;
            pollenCharge = 0f;
            timeSincePollenCollection = 0f;
            
            flower.OnPollinated(depositedAmount);
            
            OnPollenDeposited?.Invoke();
            OnPollenChanged?.Invoke(pollenCharge);
        }
        
        /// <summary>
        /// Deposit pollen to a fruit (accelerates fruit evolution).
        /// </summary>
        public void DepositPollenToFruit(Plants.GenerativeFruit fruit)
        {
            if (fruit == null) return;
            if (!IsCarryingPollen) return;
            
            float depositedAmount = pollenCharge;
            pollenCharge = 0f;
            timeSincePollenCollection = 0f;
            
            // Accelerate fruit evolution
            Plants.FruitGrowthSystem growthSystem = fruit.GetComponent<Plants.FruitGrowthSystem>();
            if (growthSystem != null)
            {
                // Multiple feeds to advance fruit
                for (int i = 0; i < Mathf.FloorToInt(depositedAmount); i++)
                {
                    growthSystem.OnButterflyFeed();
                }
            }
            
            OnPollenDeposited?.Invoke();
            OnPollenChanged?.Invoke(pollenCharge);
        }
        
        /// <summary>
        /// Clear all pollen (for testing or special events).
        /// </summary>
        public void ClearPollen()
        {
            pollenCharge = 0f;
            timeSincePollenCollection = 0f;
            OnPollenChanged?.Invoke(pollenCharge);
        }
    }
}

