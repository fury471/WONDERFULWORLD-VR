using UnityEngine;
using ButterflyHouse.Butterflies;

namespace ButterflyHouse.Core
{
    /// <summary>
    /// Manages progression stage transitions and stage-specific behaviors.
    /// Provides callbacks for each stage entry.
    /// </summary>
    public class ProgressionStageManager : MonoBehaviour
    {
        [Header("Stage Configuration")]
        [SerializeField] private StageConfiguration[] stageConfigs;
        
        [Header("Current Stage")]
        [SerializeField] private int currentStage = 0;
        
        [Header("Butterfly Manager")]
        [SerializeField] private ButterflyManager butterflyManager;
        
        [Header("Visual Effects")]
        [SerializeField] private bool enableStageVisualEffects = true;
        
        // Events
        public System.Action<int> OnStage0Entered;
        public System.Action<int> OnStage1Entered;
        public System.Action<int> OnStage2Entered;
        public System.Action<int> OnStage3Entered;
        public System.Action<int> OnStage4Entered;
        public System.Action<int> OnStage5Entered;
        
        private void Awake()
        {
            if (butterflyManager == null)
                butterflyManager = FindFirstObjectByType<ButterflyManager>();
            
            // Initialize default stage configs if not assigned
            if (stageConfigs == null || stageConfigs.Length == 0)
            {
                CreateDefaultStageConfigs();
            }
        }
        
        /// <summary>
        /// Called when entering a new stage.
        /// </summary>
        public void OnStageEntered(int stage)
        {
            if (stage == currentStage) return;
            
            int previousStage = currentStage;
            currentStage = stage;
            
            Debug.Log($"Stage {stage} entered (from stage {previousStage})");
            
            // Execute stage-specific callbacks
            switch (stage)
            {
                case 0:
                    OnStage0Entered?.Invoke(stage);
                    ApplyStage0Effects();
                    break;
                    
                case 1:
                    OnStage1Entered?.Invoke(stage);
                    ApplyStage1Effects();
                    break;
                    
                case 2:
                    OnStage2Entered?.Invoke(stage);
                    ApplyStage2Effects();
                    break;
                    
                case 3:
                    OnStage3Entered?.Invoke(stage);
                    ApplyStage3Effects();
                    break;
                    
                case 4:
                    OnStage4Entered?.Invoke(stage);
                    ApplyStage4Effects();
                    break;
                    
                case 5:
                    OnStage5Entered?.Invoke(stage);
                    ApplyStage5Effects();
                    break;
            }
            
            // Apply stage configuration if available
            if (stageConfigs != null && stage < stageConfigs.Length && stageConfigs[stage] != null)
            {
                ApplyStageConfiguration(stageConfigs[stage]);
            }
        }
        
        private void ApplyStage0Effects()
        {
            Debug.Log("Stage 0: Emergence - Basic butterflies, minimal plants, low audio density");
            
            // Enable basic butterflies
            // Thin, short trails
            // Minimal audio density
            UpdateTrailSettings(1f, 0.05f); // Short, thin trails
            UpdateAudioDensity(0.3f); // Low audio density
        }
        
        private void ApplyStage1Effects()
        {
            Debug.Log("Stage 1: Expansion - More archetypes, luminescent trails, plants respond");
            
            // More butterfly archetypes unlocked
            // Trails become more luminescent
            // Plants begin responding to footsteps/head movement
            UpdateTrailSettings(2f, 0.1f); // Longer, slightly thicker trails
            UpdateAudioDensity(0.5f); // Medium audio density
            
            // Upgrade fruits to Harmonic stage
            if (Plants.FruitManager.Instance != null)
            {
                Plants.FruitManager.Instance.UpgradeAllFruit(Plants.FruitGrowthSystem.FruitStage.Harmonic);
            }
        }
        
        private void ApplyStage2Effects()
        {
            Debug.Log("Stage 2: Symbiosis - Butterfly-plant interactions, pollination, glowing plants");
            
            // Enable pollination
            // Plants glow where touched
            // New visual bloom patterns
            // Audio gains subtle harmonics
            UpdateTrailSettings(2.5f, 0.15f); // Longer, thicker trails
            UpdateAudioDensity(0.7f); // Higher audio density with harmonics
            
            // Upgrade some fruits to Resonant stage
            if (Plants.FruitManager.Instance != null)
            {
                Plants.FruitManager.Instance.UpgradeSomeFruit(Plants.FruitGrowthSystem.FruitStage.Resonant, 0.3f);
            }
        }
        
        private void ApplyStage3Effects()
        {
            Debug.Log("Stage 3: Emergent Ecology - New plants, synchronized patterns, wind currents");
            
            // New plant types appear procedurally
            // Butterflies flock and synchronize
            // Noise-based wind currents become visible
            // Trails thicken and persist longer
            UpdateTrailSettings(3f, 0.2f); // Long, thick, persistent trails
            UpdateAudioDensity(0.8f); // High audio density
        }
        
        private void ApplyStage4Effects()
        {
            Debug.Log("Stage 4: Synesthetic Overgrowth - Ambient light cycles, waveform butterflies, chord plants");
            
            // Ambient lighting cycles enabled (Dawn/Noon/Dusk/Midnight)
            // Butterflies shift into waveform-mode more often
            // Plants generate chords instead of single notes
            // Space feels alive and self-transforming
            UpdateTrailSettings(4f, 0.3f); // Very long, very thick trails
            UpdateAudioDensity(1f); // Maximum audio density with chords
            
            // Spawn celestial fruits
            if (Plants.FruitManager.Instance != null)
            {
                Plants.FruitManager.Instance.SpawnCelestialFruit();
            }
        }
        
        private void ApplyStage5Effects()
        {
            Debug.Log("Stage 5: Ascension State - Final form, giant chrysalis, butterfly choir");
            
            // Giant chrysalis event can trigger
            // Butterfly choir can form
            // Environment can temporarily dissolve into waveforms
            UpdateTrailSettings(5f, 0.4f); // Maximum trail length and thickness
            UpdateAudioDensity(1f); // Full harmonic density
        }
        
        private void UpdateTrailSettings(float time, float width)
        {
            if (!enableStageVisualEffects)
            {
                return;
            }

            // Update trail settings for all butterflies
            if (butterflyManager != null)
            {
                var butterflies = butterflyManager.GetActiveButterflies();
                foreach (var butterfly in butterflies)
                {
                    var trailRenderer = butterfly.GetComponentInChildren<TrailRenderer>();
                    if (trailRenderer != null)
                    {
                        trailRenderer.time = time;
                        trailRenderer.startWidth = width;
                    }
                }
            }
        }
        
        private void UpdateAudioDensity(float density)
        {
            // Update audio density globally
            // This affects how many butterflies can play audio simultaneously
            if (butterflyManager != null)
            {
                // Could adjust max butterflies based on density
                // Or adjust audio volume curve
            }
        }
        
        private void ApplyStageConfiguration(StageConfiguration config)
        {
            if (config == null) return;
            
            // Apply stage-specific configurations
            // Trail settings, audio density, etc.
        }
        
        private void CreateDefaultStageConfigs()
        {
            // Create default stage configurations
            // These can be overridden with ScriptableObjects
            stageConfigs = new StageConfiguration[6];
            for (int i = 0; i < 6; i++)
            {
                stageConfigs[i] = ScriptableObject.CreateInstance<StageConfiguration>();
                stageConfigs[i].stageNumber = i;
            }
        }
        
        public int CurrentStage => currentStage;
        
        /// <summary>
        /// Evaluate the current progression stage based on ecosystem state.
        /// This is used by EcosystemOrchestrator to determine stage progression.
        /// </summary>
        public int EvaluateStage(EcosystemStateController eco)
        {
            if (eco == null) return 0;
            
            float t = eco.TimeAlive;
            float serenity = eco.SerenityLevel;
            float curiosity = eco.CuriosityLevel;
            float harmony = eco.HarmonyLevel;
            int totalTouches = eco.TotalPlantTouches;
            bool firstLanding = eco.FirstButterflyLanding;
            
            // Stage 0: Emergence
            if (t < 45f && !firstLanding)
                return 0;
            
            // Stage 1: Expansion
            if (harmony < 20f || serenity < 15f)
                return 1;
            
            // Stage 2: Symbiosis
            if (curiosity < 30f || totalTouches < 5)
                return 2;
            
            // Stage 3: Emergent Ecology
            float serenitySustained = eco.SerenitySustainedTime;
            if (serenitySustained < 60f)
                return 3;
            
            // Stage 4: Synesthetic Overgrowth
            bool allHigh = harmony >= 50f && curiosity >= 50f && serenity >= 50f;
            float minTimeForStage5 = 12f * 60f; // 12 minutes
            if (!allHigh || t < minTimeForStage5)
                return 4;
            
            // Stage 5: Ascension
            return 5;
        }
    }
}

