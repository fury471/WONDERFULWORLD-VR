using UnityEngine;
using ButterflyHouse.Core;

namespace ButterflyHouse.Plants
{
    /// <summary>
    /// Manages fruit growth stages and melodic evolution.
    /// Fruits evolve through 4 stages: Seed, Harmonic, Resonant, Celestial.
    /// </summary>
    public class FruitGrowthSystem : MonoBehaviour
    {
        public enum FruitStage
        {
            Seed,          // Stage F0: Small glowing orb, single pure tone
            Harmonic,      // Stage F1: Grows petals/facets, emits 2-3 note arpeggios
            Resonant,      // Stage F2: Complex geometry, emits chords and harmonic pads
            Celestial      // Stage F3: Levitates, emits full-spectrum melodic sequences
        }
        
        [Header("Growth Stage")]
        [SerializeField] private FruitStage currentStage = FruitStage.Seed;
        
        [Header("Growth Requirements")]
        [SerializeField] private float harmonyRequiredForHarmonic = 20f;
        [SerializeField] private int butterflyFeedsRequiredForResonant = 5;
        [SerializeField] private float curiosityRequiredForResonant = 30f;
        [SerializeField] private int progressionStageRequiredForCelestial = 4;
        
        [Header("Tracking")]
        [SerializeField] private bool firstButterflyFeed = false;
        [SerializeField] private int butterflyFeedCount = 0;
        
        [Header("Visual")]
        [SerializeField] private Transform growthTarget; // Transform to scale/grow
        [SerializeField] private GameObject[] stageVisuals; // Visual objects for each stage
        
        private GenerativeFruit _generativeFruit;
        private FruitVisualController _visualController;
        
        // Events
        public System.Action<FruitStage> OnStageChanged;
        
        private void Awake()
        {
            _generativeFruit = GetComponent<GenerativeFruit>();
            _visualController = GetComponent<FruitVisualController>();
        }
        
        private void Update()
        {
            CheckStageProgression();
            UpdateStageBehaviors();
        }
        
        private void CheckStageProgression()
        {
            EcosystemStateController stateController = EcosystemStateController.Instance;
            if (stateController == null) return;
            
            int nextStage = (int)currentStage;
            bool shouldAdvance = false;
            
            switch (currentStage)
            {
                case FruitStage.Seed:
                    // Trigger: harmonyLevel > threshold OR firstButterflyFeed == true
                    if (stateController.HarmonyLevel >= harmonyRequiredForHarmonic || firstButterflyFeed)
                    {
                        nextStage = 1; // Harmonic
                        shouldAdvance = true;
                    }
                    break;
                    
                case FruitStage.Harmonic:
                    // Trigger: butterflyFeedCount > X OR curiosityLevel > threshold
                    if (butterflyFeedCount >= butterflyFeedsRequiredForResonant || 
                        stateController.CuriosityLevel >= curiosityRequiredForResonant)
                    {
                        nextStage = 2; // Resonant
                        shouldAdvance = true;
                    }
                    break;
                    
                case FruitStage.Resonant:
                    // Trigger: progressionStage >= 4
                    if (stateController.ProgressionStage >= progressionStageRequiredForCelestial)
                    {
                        nextStage = 3; // Celestial
                        shouldAdvance = true;
                    }
                    break;
                    
                case FruitStage.Celestial:
                    // Stay in Celestial stage
                    break;
            }
            
            if (shouldAdvance)
            {
                AdvanceToStage((FruitStage)nextStage);
            }
        }
        
        private void UpdateStageBehaviors()
        {
            switch (currentStage)
            {
                case FruitStage.Seed:
                    // Single pure tone
                    break;
                    
                case FruitStage.Harmonic:
                    // 2-3 note arpeggios
                    break;
                    
                case FruitStage.Resonant:
                    // Chords and harmonic pads
                    break;
                    
                case FruitStage.Celestial:
                    // Full-spectrum melodic sequences
                    // Levitate
                    UpdateCelestialBehaviors();
                    break;
            }
        }
        
        private void UpdateCelestialBehaviors()
        {
            // Levitation effect
            if (growthTarget != null)
            {
                float levitation = Mathf.Sin(Time.time * 2f) * 0.1f;
                Vector3 basePos = transform.position;
                basePos.y += levitation;
                // Apply gentle levitation
            }
        }
        
        /// <summary>
        /// Called when a butterfly feeds from this fruit.
        /// </summary>
        public void OnButterflyFeed()
        {
            if (!firstButterflyFeed)
            {
                firstButterflyFeed = true;
            }
            
            butterflyFeedCount++;
            
            // Notify ecosystem
            if (EcosystemStateController.Instance != null)
            {
                EcosystemStateController.Instance.OnButterflyPlantInteraction();
            }
        }
        
        /// <summary>
        /// Force advance to next stage (for progression system).
        /// </summary>
        public void ForceAdvance()
        {
            if ((int)currentStage < 3)
            {
                FruitStage nextStage = (FruitStage)((int)currentStage + 1);
                AdvanceToStage(nextStage);
            }
        }
        
        private void AdvanceToStage(FruitStage newStage)
        {
            if ((int)newStage <= (int)currentStage) return; // Don't downgrade
            
            currentStage = newStage;
            OnStageChanged?.Invoke(newStage);
            
            Debug.Log($"Fruit {gameObject.name} advanced to stage: {newStage}");
            
            ApplyStageEffects(newStage);
        }
        
        private void ApplyStageEffects(FruitStage stage)
        {
            // Update visuals
            if (stageVisuals != null && stageVisuals.Length > (int)stage)
            {
                for (int i = 0; i < stageVisuals.Length; i++)
                {
                    if (stageVisuals[i] != null)
                        stageVisuals[i].SetActive(i == (int)stage);
                }
            }
            
            // Update visual controller
            if (_visualController != null)
            {
                _visualController.OnStageChanged(stage);
            }
            
            // Update fruit properties
            if (_generativeFruit != null)
            {
                _generativeFruit.OnStageChanged(stage);
            }
            
            // Update scale/growth
            if (growthTarget != null)
            {
                float scale = 1f + ((int)stage * 0.2f); // Grow with each stage
                growthTarget.localScale = Vector3.one * scale;
            }
            
            switch (stage)
            {
                case FruitStage.Harmonic:
                    EnableHarmonicEffects();
                    break;
                    
                case FruitStage.Resonant:
                    EnableResonantEffects();
                    break;
                    
                case FruitStage.Celestial:
                    EnableCelestialEffects();
                    break;
            }
        }
        
        private void EnableHarmonicEffects()
        {
            // Stage F1: Harmonic
            // Fruit grows petals/facets
            // Starts emitting 2-3 note arpeggios
            // Pulse frequency doubles
            Debug.Log($"Fruit {gameObject.name} has reached Harmonic stage!");
        }
        
        private void EnableResonantEffects()
        {
            // Stage F2: Resonant
            // Complex geometry (kaleidoscopic mesh deformation)
            // Emits chords and harmonic pads
            // Visual ripple rings when approached
            Debug.Log($"Fruit {gameObject.name} has reached Resonant stage!");
        }
        
        private void EnableCelestialEffects()
        {
            // Stage F3: Celestial
            // Fruit levitates
            // Emits full-spectrum melodic sequences
            // Dramatic visual pulses, fractal casing, liquid iridescent patterns
            Debug.Log($"Fruit {gameObject.name} has reached CELESTIAL stage!");
        }
        
        public FruitStage CurrentStage => currentStage;
        public int ButterflyFeedCount => butterflyFeedCount;
    }
}

