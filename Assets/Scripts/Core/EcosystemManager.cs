using UnityEngine;
using System.Collections.Generic;
using ButterflyHouse.Butterflies;
using ButterflyHouse.Plants;

namespace ButterflyHouse.Core
{
    /// <summary>
    /// Manages ecosystem evolution and phase progression.
    /// Tracks player interaction metrics and triggers ecosystem transformations.
    /// </summary>
    public class EcosystemManager : MonoBehaviour
    {
        public static EcosystemManager Instance { get; private set; }
        
        public enum EcosystemPhase
        {
            Emergence,           // Phase 1: Simple butterflies, soft soundscape
            TerritorialPatterns, // Phase 2: Flocks, new archetypes, longer trails
            SymbioticRelations,  // Phase 3: Butterfly-plant interactions
            EmergentEcosystem    // Phase 4: Plant spread, queen chrysalis, wind currents
        }
        
        [Header("Current Phase")]
        [SerializeField] private EcosystemPhase currentPhase = EcosystemPhase.Emergence;
        
        [Header("Phase Progression")]
        [SerializeField] private float timeInCurrentPhase = 0f;
        [SerializeField] private float phase1Duration = 60f; // 1 minute to phase 2
        [SerializeField] private float phase2Duration = 120f; // 2 minutes to phase 3
        [SerializeField] private float phase3Duration = 180f; // 3 minutes to phase 4
        
        [Header("Progression Meters")]
        [Range(0f, 100f)]
        [SerializeField] private float harmonyLevel = 0f; // Butterfly-plant interactions
        [Range(0f, 100f)]
        [SerializeField] private float serenityMeter = 0f; // Player stillness
        [Range(0f, 100f)]
        [SerializeField] private float curiosityMeter = 0f; // Exploration, touching, gesturing
        [Range(0f, 100f)]
        [SerializeField] private float affinityLevel = 0f; // Butterflies landed on player
        
        [Header("Hand Aura")]
        [SerializeField] private HandAuraSystem handAuraSystem;
        
        [Header("Event System")]
        [SerializeField] private EventSystem eventSystem;
        
        [Header("Light Cycle")]
        [SerializeField] private LightCycle lightCycle;
        
        // Tracking
        private float _totalTimeInExperience = 0f;
        private int _butterflyLandingsOnPlayer = 0;
        private int _plantInteractions = 0;
        private float _playerStillnessTimer = 0f;
        private Vector3 _lastPlayerPosition;
        
        // Events
        public System.Action<EcosystemPhase> OnPhaseChanged;
        public System.Action<float> OnHarmonyLevelChanged;
        public System.Action<float> OnAffinityLevelChanged;
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            
            if (handAuraSystem == null)
                handAuraSystem = GetComponent<HandAuraSystem>();
            
            if (eventSystem == null)
                eventSystem = GetComponent<EventSystem>();
            
            if (lightCycle == null)
                lightCycle = GetComponent<LightCycle>();
        }
        
        private void Start()
        {
            _lastPlayerPosition = GetPlayerPosition();
            currentPhase = EcosystemPhase.Emergence;
            InitializePhase(currentPhase);
        }
        
        private void Update()
        {
            _totalTimeInExperience += Time.deltaTime;
            timeInCurrentPhase += Time.deltaTime;
            
            UpdateProgressionMeters();
            CheckPhaseProgression();
            
            // Update light cycle
            if (lightCycle != null)
            {
                lightCycle.UpdateCycle(_totalTimeInExperience);
            }
        }
        
        private Vector3 GetPlayerPosition()
        {
            if (Camera.main != null)
                return Camera.main.transform.position;
            return Vector3.zero;
        }
        
        private void UpdateProgressionMeters()
        {
            // Update Serenity Meter (player stillness)
            Vector3 currentPlayerPos = GetPlayerPosition();
            float movement = Vector3.Distance(currentPlayerPos, _lastPlayerPosition);
            
            if (movement < 0.01f) // Very still
            {
                _playerStillnessTimer += Time.deltaTime;
                serenityMeter = Mathf.Clamp(serenityMeter + Time.deltaTime * 0.5f, 0f, 100f);
            }
            else
            {
                _playerStillnessTimer = 0f;
                serenityMeter = Mathf.Clamp(serenityMeter - Time.deltaTime * 0.2f, 0f, 100f);
            }
            
            _lastPlayerPosition = currentPlayerPos;
            
            // Update Affinity Level (butterflies on player)
            // This is updated when butterflies land on player hands
            
            // Update Curiosity Meter
            // This is updated by InteractionManager when player touches/interacts
        }
        
        private void CheckPhaseProgression()
        {
            EcosystemPhase nextPhase = currentPhase;
            bool shouldAdvance = false;
            
            switch (currentPhase)
            {
                case EcosystemPhase.Emergence:
                    if (timeInCurrentPhase >= phase1Duration)
                    {
                        nextPhase = EcosystemPhase.TerritorialPatterns;
                        shouldAdvance = true;
                    }
                    break;
                    
                case EcosystemPhase.TerritorialPatterns:
                    if (timeInCurrentPhase >= phase2Duration && harmonyLevel > 30f)
                    {
                        nextPhase = EcosystemPhase.SymbioticRelations;
                        shouldAdvance = true;
                    }
                    break;
                    
                case EcosystemPhase.SymbioticRelations:
                    if (timeInCurrentPhase >= phase3Duration && harmonyLevel > 60f && affinityLevel > 40f)
                    {
                        nextPhase = EcosystemPhase.EmergentEcosystem;
                        shouldAdvance = true;
                    }
                    break;
                    
                case EcosystemPhase.EmergentEcosystem:
                    // Stay in final phase
                    break;
            }
            
            if (shouldAdvance)
            {
                AdvanceToPhase(nextPhase);
            }
        }
        
        private void InitializePhase(EcosystemPhase phase)
        {
            timeInCurrentPhase = 0f;
            
            switch (phase)
            {
                case EcosystemPhase.Emergence:
                    // Simple butterflies, soft soundscape
                    break;
                    
                case EcosystemPhase.TerritorialPatterns:
                    // Enable flocking, longer trails, new archetypes
                    EnableTerritorialPatterns();
                    break;
                    
                case EcosystemPhase.SymbioticRelations:
                    // Enable butterfly-plant interactions
                    EnableSymbioticRelations();
                    break;
                    
                case EcosystemPhase.EmergentEcosystem:
                    // Enable plant spread, queen chrysalis, wind currents
                    EnableEmergentEcosystem();
                    break;
            }
            
            OnPhaseChanged?.Invoke(phase);
            Debug.Log($"Ecosystem Phase: {phase}");
        }
        
        private void AdvanceToPhase(EcosystemPhase newPhase)
        {
            currentPhase = newPhase;
            InitializePhase(newPhase);
        }
        
        private void EnableTerritorialPatterns()
        {
            // Enable flocking behavior
            // Spawn new butterfly archetypes
            // Increase trail length
            Debug.Log("Territorial Patterns activated!");
        }
        
        private void EnableSymbioticRelations()
        {
            // Enable pollination
            // Enable plant charging
            // Enable plant growth from butterfly interactions
            Debug.Log("Symbiotic Relations activated!");
        }
        
        private void EnableEmergentEcosystem()
        {
            // Enable plant spreading
            // Spawn queen chrysalis
            // Enable wind currents
            Debug.Log("Emergent Ecosystem activated!");
        }
        
        /// <summary>
        /// Called when a butterfly lands on the player.
        /// </summary>
        public void OnButterflyLandOnPlayer()
        {
            _butterflyLandingsOnPlayer++;
            affinityLevel = Mathf.Clamp(affinityLevel + 2f, 0f, 100f);
            OnAffinityLevelChanged?.Invoke(affinityLevel);
            
            // Check for hand aura level-ups
            if (handAuraSystem != null)
            {
                handAuraSystem.OnButterflyLanding();
            }
        }
        
        /// <summary>
        /// Called when a butterfly interacts with a plant.
        /// </summary>
        public void OnButterflyPlantInteraction()
        {
            _plantInteractions++;
            harmonyLevel = Mathf.Clamp(harmonyLevel + 1f, 0f, 100f);
            OnHarmonyLevelChanged?.Invoke(harmonyLevel);
        }
        
        /// <summary>
        /// Called when player interacts with something.
        /// </summary>
        public void OnPlayerInteraction()
        {
            curiosityMeter = Mathf.Clamp(curiosityMeter + 1f, 0f, 100f);
        }
        
        // Public getters
        public EcosystemPhase CurrentPhase => currentPhase;
        public float HarmonyLevel => harmonyLevel;
        public float SerenityMeter => serenityMeter;
        public float CuriosityMeter => curiosityMeter;
        public float AffinityLevel => affinityLevel;
        public float TotalTimeInExperience => _totalTimeInExperience;
    }
}

