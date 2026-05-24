using UnityEngine;

namespace ButterflyHouse.Butterflies
{
    /// <summary>
    /// Manages butterfly visual transformations into waveform forms.
    /// Each stage unlocks different waveform evolution tiers.
    /// </summary>
    public class ButterflyFormEvolution : MonoBehaviour
    {
        public enum WaveformTier
        {
            Standard,      // Stage 0: Standard butterfly, simple wandering
            Sineform,      // Stage 1: Smoother motion, sine wave LFO
            Sawform,       // Stage 2: Sharper turns, saw LFO, resonant peaks
            Squareform,    // Stage 3: Stuttering dashes, stepped filter
            FM_Modulated,  // Stage 4: Chaotic fractals, FM synthesis
            PureWaveform   // Stage 5: No wings, pure waveform body, unified chord
        }
        
        [Header("Current Form")]
        [SerializeField] private WaveformTier currentTier = WaveformTier.Standard;
        
        [Header("Visual Controller")]
        [SerializeField] private ButterflyVisualController visualController;
        
        [Header("Audio Controller")]
        [SerializeField] private ButterflyAudio audioController;
        
        [Header("Evolution Settings")]
        [SerializeField] private bool evolveBasedOnStage = true;
        [SerializeField] private int stageRequiredForSineform = 1;
        [SerializeField] private int stageRequiredForSawform = 2;
        [SerializeField] private int stageRequiredForSquareform = 3;
        [SerializeField] private int stageRequiredForFM = 4;
        [SerializeField] private int stageRequiredForPureWaveform = 5;
        
        [Header("Form Chance")]
        [Range(0f, 1f)]
        [SerializeField] private float sineformChance = 0.3f;
        [Range(0f, 1f)]
        [SerializeField] private float sawformChance = 0.2f;
        [Range(0f, 1f)]
        [SerializeField] private float squareformChance = 0.15f;
        [Range(0f, 1f)]
        [SerializeField] private float fmChance = 0.1f;
        
        private Butterfly _butterfly;
        private Core.EcosystemStateController _stateController;
        private WaveformTier _targetTier;
        private float _evolutionProgress = 0f;

        // Evolution is driven by the ecosystem progression stage which changes very rarely.
        // Re-rolling Random.value every frame is wasted CPU and also caused tier flickering.
        // Re-evaluate target tier on a slow cadence; stage-change callbacks should drive most transitions.
        private const float TIER_REEVAL_INTERVAL = 2.0f;
        private float _tierReevalTimer;

        // Events
        public System.Action<WaveformTier> OnTierChanged;
        
        private void Awake()
        {
            _butterfly = GetComponent<Butterfly>();
            
            if (visualController == null)
                visualController = GetComponent<ButterflyVisualController>();
            
            if (audioController == null)
                audioController = GetComponent<ButterflyAudio>();
            
            _stateController = Core.EcosystemStateController.Instance;
        }
        
        private void Start()
        {
            currentTier = WaveformTier.Standard;
            _targetTier = DetermineTargetTier();
        }
        
        private void Update()
        {
            if (evolveBasedOnStage && _stateController != null)
            {
                _tierReevalTimer += Time.deltaTime;
                if (_tierReevalTimer >= TIER_REEVAL_INTERVAL)
                {
                    _tierReevalTimer = 0f;
                    WaveformTier newTargetTier = DetermineTargetTier();
                    if (newTargetTier != _targetTier)
                    {
                        _targetTier = newTargetTier;
                        EvolveToTier(_targetTier);
                    }
                }
            }

            _evolutionProgress = Mathf.Min(1f, _evolutionProgress + Time.deltaTime);

            // Update visual form based on current tier
            UpdateWaveformVisuals();
        }
        
        private WaveformTier DetermineTargetTier()
        {
            if (_stateController == null) return WaveformTier.Standard;
            
            int currentStage = _stateController.ProgressionStage;
            
            // Random chance for evolution within stage limits
            float random = Random.value;
            
            if (currentStage >= stageRequiredForPureWaveform && random < 0.1f)
            {
                return WaveformTier.PureWaveform;
            }
            else if (currentStage >= stageRequiredForFM && random < fmChance)
            {
                return WaveformTier.FM_Modulated;
            }
            else if (currentStage >= stageRequiredForSquareform && random < squareformChance)
            {
                return WaveformTier.Squareform;
            }
            else if (currentStage >= stageRequiredForSawform && random < sawformChance)
            {
                return WaveformTier.Sawform;
            }
            else if (currentStage >= stageRequiredForSineform && random < sineformChance)
            {
                return WaveformTier.Sineform;
            }
            
            return WaveformTier.Standard;
        }
        
        private void EvolveToTier(WaveformTier tier)
        {
            if (tier == currentTier) return;
            
            currentTier = tier;
            _evolutionProgress = 0f;
            
            OnTierChanged?.Invoke(tier);
            Debug.Log($"Butterfly evolved to {tier}");
            
            ApplyTierEffects(tier);
        }
        
        private void ApplyTierEffects(WaveformTier tier)
        {
            if (_butterfly == null || _butterfly.Archetype == null) return;
            
            switch (tier)
            {
                case WaveformTier.Standard:
                    // Default behavior
                    break;
                    
                case WaveformTier.Sineform:
                    // Smoother motion, sine wave LFO
                    ApplySineformEffects();
                    break;
                    
                case WaveformTier.Sawform:
                    // Sharper turns, saw LFO
                    ApplySawformEffects();
                    break;
                    
                case WaveformTier.Squareform:
                    // Stuttering dashes, stepped filter
                    ApplySquareformEffects();
                    break;
                    
                case WaveformTier.FM_Modulated:
                    // Chaotic fractals, FM synthesis
                    ApplyFMEffects();
                    break;
                    
                case WaveformTier.PureWaveform:
                    // No wings, pure waveform body
                    ApplyPureWaveformEffects();
                    break;
            }
        }
        
        private void ApplySineformEffects()
        {
            // Visual: Smooth sinusoidal motion
            // Audio: Sine wave LFO on pitch
            if (visualController != null)
            {
                visualController.SetWaveParams(0.1f, 2f); // Sine wave deformation
            }
        }
        
        private void ApplySawformEffects()
        {
            // Visual: Sawtooth motion pattern
            // Audio: Saw LFO, resonant peaks
            if (visualController != null)
            {
                visualController.SetWaveParams(0.15f, 4f); // Sawtooth deformation
            }
        }
        
        private void ApplySquareformEffects()
        {
            // Visual: Stepped/square motion
            // Audio: Stepped filter
            if (visualController != null)
            {
                visualController.SetWaveParams(0.2f, 8f); // Square wave deformation
            }
        }
        
        private void ApplyFMEffects()
        {
            // Visual: Complex fractal patterns
            // Audio: FM synthesis patterns
            if (visualController != null)
            {
                visualController.SetWaveParams(0.3f, 16f); // Complex FM deformation
            }
        }
        
        private void ApplyPureWaveformEffects()
        {
            // Visual: Dissolve wings, become pure waveform
            // Audio: Unified chord tone
            if (visualController != null)
            {
                visualController.SetWaveParams(1f, 32f); // Maximum waveform deformation
                visualController.SetEmission(1f); // Full emission
            }
        }
        
        private void UpdateWaveformVisuals()
        {
            if (visualController == null) return;
            
            // Update shader parameters based on tier
            // This is handled by the visual controller based on tier effects
        }
        
        /// <summary>
        /// Force evolution to a specific tier (for testing or special events).
        /// </summary>
        public void ForceEvolution(WaveformTier tier)
        {
            EvolveToTier(tier);
        }
        
        public WaveformTier CurrentTier => currentTier;
        public float EvolutionProgress => _evolutionProgress;
    }
}

