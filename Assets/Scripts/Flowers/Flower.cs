using UnityEngine;
using ButterflyHouse.Core;
using ButterflyHouse.Interaction;
using ButterflyHouse.Butterflies;

namespace ButterflyHouse.Flowers
{
    /// <summary>
    /// Interactive flower that butterflies can pollinate.
    /// Flowers sit between plants and fruit in the ecosystem.
    /// Butterflies collect pollen from flowers and deposit it to other flowers/fruit.
    /// </summary>
    public class Flower : MonoBehaviour
    {
        public enum FlowerStage
        {
            Bud,        // Stage FL0: Small, closed, low-emission
            Bloom,      // Stage FL1: Opens petals, emits 2-note motif
            Radiant,    // Stage FL2: Strong bioluminescence, 3-5 note phrases
            Meta        // Stage FL3: Fractal patterns, evolving melodies, spawns fruit
        }
        
        [Header("Flower Stage")]
        [SerializeField] private FlowerStage stage = FlowerStage.Bud;
        
        [Header("Pollination")]
        [SerializeField] private int pollinationCount = 0;
        [SerializeField] private float nectarValue = 0.5f;  // Energy for butterflies
        [SerializeField] private float pollenYield = 0.5f;  // How much pollen butterflies collect
        [Range(0.1f, 2f)]
        [SerializeField] private float influenceRadius = 3f; // Radius of influence for Meta stage
        
        [Header("Visual")]
        [SerializeField] private Renderer flowerRenderer;
        [SerializeField] private FlowerVisualController visualController;
        
        [Header("Audio")]
        [SerializeField] private AudioClip[] nectarMelodies;
        [SerializeField] private AudioSource audioSource;
        [Range(0f, 1f)]
        [SerializeField] private float audioVolume = 0.6f;
        
        [Header("Landing Target")]
        [SerializeField] private LandingTarget landingTarget;
        [SerializeField] private bool createLandingTarget = true;
        [Range(0.1f, 2f)]
        [SerializeField] private float landingZoneRadius = 0.3f;
        
        [Header("Parent Plant")]
        [SerializeField] private Plants.GenerativePlant parentPlant;
        
        [Header("Touch Interaction")]
        [SerializeField] private float touchCooldown = 0.5f; // Cooldown between touches
        [SerializeField] private bool allowMultipleTouches = true;
        
        private void Start()
        {
            // Ensure parent plant reference is set
            if (parentPlant == null)
                parentPlant = GetComponentInParent<Plants.GenerativePlant>();
        }
        
        private int _stageProgressionPollinationCount = 0;
        private float _lastTouchTime = 0f;
        
        // Events
        public System.Action<FlowerStage> OnStageChanged;
        public System.Action<Butterfly> OnButterflyLandedEvent;
        public System.Action<float> OnPollinatedEvent;
        
        private void Awake()
        {
            if (flowerRenderer == null)
                flowerRenderer = GetComponent<Renderer>();
            
            if (visualController == null)
                visualController = GetComponent<FlowerVisualController>();
            
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
            
            // Set up audio source
            if (audioSource != null)
            {
                audioSource.playOnAwake = false;
                audioSource.loop = false;
                audioSource.spatialBlend = 1f; // 3D sound
            }
            
            // Find parent plant
            if (parentPlant == null)
                parentPlant = GetComponentInParent<Plants.GenerativePlant>();
            
            // Create landing target if needed
            if (createLandingTarget && landingTarget == null)
            {
                CreateLandingTarget();
            }
        }
        
        private void Update()
        {
            // Check for stage progression
            TryAdvanceStage();
            
            // Update Meta stage behaviors (mini-orbits, synchronized glow)
            if (stage == FlowerStage.Meta)
            {
                UpdateMetaBehaviors();
            }
        }
        
        private void CreateLandingTarget()
        {
            GameObject landingObj = new GameObject("LandingTarget");
            landingObj.transform.SetParent(transform);
            landingObj.transform.localPosition = Vector3.zero;
            
            // Add sphere collider for landing detection
            SphereCollider collider = landingObj.AddComponent<SphereCollider>();
            collider.radius = landingZoneRadius;
            collider.isTrigger = true;
            
            // Add LandingTarget component
            landingTarget = landingObj.AddComponent<LandingTarget>();
            
            // Set target type to Plant (flowers are part of plants)
            var field = typeof(LandingTarget).GetField("targetType", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(landingTarget, LandingTarget.TargetType.Plant);
            }
        }
        
        /// <summary>
        /// Called when a butterfly lands on this flower.
        /// </summary>
        public void OnButterflyLanded(Butterfly butterfly)
        {
            if (butterfly == null) return;
            
            // Butterfly feeds and collects pollen
            ButterflyEnergy energySystem = butterfly.GetComponent<ButterflyEnergy>();
            if (energySystem != null)
            {
                energySystem.AddEnergy(nectarValue);
            }
            
            ButterflyPollination pollinationSystem = butterfly.GetComponent<ButterflyPollination>();
            if (pollinationSystem != null)
            {
                pollinationSystem.CollectPollen(pollenYield);
            }
            
            // Visual feedback
            if (visualController != null)
            {
                visualController.OnNectarSipped();
            }
            
            // Audio feedback
            PlayNectarMelody();
            
            // Notify ecosystem
            if (EcosystemStateController.Instance != null)
            {
                EcosystemStateController.Instance.OnButterflyPlantInteraction();
            }
            
            // Notify parent plant
            if (parentPlant != null)
            {
                Plants.PlantGrowthSystem growthSystem = parentPlant.GetComponent<Plants.PlantGrowthSystem>();
                if (growthSystem != null)
                {
                    growthSystem.OnButterflyVisit();
                }
            }
            
            OnButterflyLandedEvent?.Invoke(butterfly);
        }
        
        /// <summary>
        /// Called when a butterfly deposits pollen on this flower (cross-pollination).
        /// </summary>
        public void OnPollinated(float pollenAmount)
        {
            pollinationCount++;
            _stageProgressionPollinationCount++;
            
            // Visual burst
            if (visualController != null)
            {
                visualController.OnPollinatedBurst();
            }
            
            // Notify ecosystem orchestrator
            if (Core.EcosystemOrchestrator.Instance != null)
            {
                Core.EcosystemOrchestrator.Instance.RegisterPollination(this, pollenAmount);
            }
            
            // Also notify ecosystem state controller for compatibility
            if (Core.EcosystemStateController.Instance != null)
            {
                Core.EcosystemStateController.Instance.RegisterPollination(this, pollenAmount);
            }
            
            // Try to advance stage
            TryAdvanceStage();
            
            // Meta-Flower: spawn fruit seeds after heavy pollination
            if (stage == FlowerStage.Meta && pollinationCount % 5 == 0)
            {
                TrySpawnFruitSeed();
            }
            
            OnPollinatedEvent?.Invoke(pollenAmount);
        }
        
        private void PlayNectarMelody()
        {
            if (audioSource == null || nectarMelodies == null || nectarMelodies.Length == 0) return;
            
            AudioClip clip = nectarMelodies[Random.Range(0, nectarMelodies.Length)];
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
        
        private void TryAdvanceStage()
        {
            EcosystemStateController stateController = EcosystemStateController.Instance;
            if (stateController == null) return;
            
            FlowerStage nextStage = stage;
            bool shouldAdvance = false;
            
            switch (stage)
            {
                case FlowerStage.Bud:
                    // Trigger: pollinationCount >= 1 OR plantLevel >= 1
                    if (_stageProgressionPollinationCount >= 1)
                    {
                        nextStage = FlowerStage.Bloom;
                        shouldAdvance = true;
                    }
                    break;
                    
                case FlowerStage.Bloom:
                    // Trigger: pollinationCount >= 3
                    if (_stageProgressionPollinationCount >= 3)
                    {
                        nextStage = FlowerStage.Radiant;
                        shouldAdvance = true;
                    }
                    break;
                    
                case FlowerStage.Radiant:
                    // Trigger: pollinationCount >= 7 AND progressionStage >= 4
                    if (_stageProgressionPollinationCount >= 7 && stateController.ProgressionStage >= 4)
                    {
                        nextStage = FlowerStage.Meta;
                        shouldAdvance = true;
                    }
                    break;
                    
                case FlowerStage.Meta:
                    // Stay in Meta stage
                    break;
            }
            
            if (shouldAdvance)
            {
                SetStage(nextStage);
            }
        }
        
        private void SetStage(FlowerStage newStage)
        {
            if (newStage == stage) return;
            
            FlowerStage oldStage = stage;
            stage = newStage;
            
            Debug.Log($"Flower {gameObject.name} advanced from {oldStage} to {newStage}");
            
            OnStageChanged?.Invoke(newStage);
            
            // Update visual controller
            if (visualController != null)
            {
                visualController.OnStageChanged(newStage);
            }
            
            // Update stage-specific properties
            UpdateStageProperties(newStage);
        }
        
        private void UpdateStageProperties(FlowerStage stage)
        {
            // Update nectar value based on stage
            switch (stage)
            {
                case FlowerStage.Bud:
                    nectarValue = 0.2f; // Minimal
                    pollenYield = 0.3f;
                    break;
                    
                case FlowerStage.Bloom:
                    nectarValue = 0.5f; // Normal
                    pollenYield = 0.5f;
                    break;
                    
                case FlowerStage.Radiant:
                    nectarValue = 1f; // High
                    pollenYield = 0.8f;
                    break;
                    
                case FlowerStage.Meta:
                    nectarValue = 1.5f; // Maximum
                    pollenYield = 1f;
                    influenceRadius = 5f; // Larger influence
                    break;
            }
        }
        
        private void UpdateMetaBehaviors()
        {
            // Meta-Flowers: butterflies form mini-orbits around them
            // Fruit and plants in vicinity glow in sync
            
            Collider[] nearbyObjects = Physics.OverlapSphere(transform.position, influenceRadius);
            
            foreach (var obj in nearbyObjects)
            {
                // Influence nearby fruit
                Plants.GenerativeFruit fruit = obj.GetComponent<Plants.GenerativeFruit>();
                if (fruit != null)
                {
                    // Trigger subtle glow sync (could be handled by visual controller)
                }
                
                // Influence nearby plants
                Plants.GenerativePlant plant = obj.GetComponent<Plants.GenerativePlant>();
                if (plant != null && plant != parentPlant)
                {
                    // Trigger synchronized swaying
                }
            }
        }
        
        private void TrySpawnFruitSeed()
        {
            // Meta-Flowers can spawn new Seed Fruit above them
            if (Plants.FruitManager.Instance != null)
            {
                Vector3 spawnPos = transform.position + Vector3.up * 0.15f;
                // Note: This would need FruitManager.SpawnFruitAt() method
                Debug.Log($"Meta-Flower {gameObject.name}: Attempting to spawn fruit seed at {spawnPos}");
            }
        }
        
        public LandingTarget LandingTarget => landingTarget;
        public FlowerStage CurrentStage => stage;
        public int PollinationCount => pollinationCount;
        public float NectarValue => nectarValue;
        public float PollenYield => pollenYield;
        public float InfluenceRadius => influenceRadius;
        public Plants.GenerativePlant ParentPlant => parentPlant;
        
        /// <summary>
        /// Called when player touches the flower.
        /// </summary>
        public void OnTouched(Vector3 touchPoint)
        {
            // Check cooldown
            if (Time.time - _lastTouchTime < touchCooldown && !allowMultipleTouches)
                return;
            
            _lastTouchTime = Time.time;
            
            // Visual feedback - pulse petals
            if (visualController != null)
            {
                visualController.OnNectarSipped(); // Reuse nectar sipped visual
            }
            
            // Audio feedback - play nectar melody
            PlayNectarMelody();
            
            // Notify ecosystem orchestrator (if exists) - for curiosity level
            if (Core.EcosystemOrchestrator.Instance != null)
            {
                // Could add RegisterFlowerTouch method to orchestrator
                // For now, we'll just trigger visual/audio feedback
            }
            
            // Notify ecosystem state controller for compatibility
            if (Core.EcosystemStateController.Instance != null)
            {
                Core.EcosystemStateController.Instance.OnPlayerExploration(); // Increments curiosity
            }
            
            // Make flower more attractive to butterflies (could increase nectar value temporarily)
            // This encourages pollination as mentioned in README
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

