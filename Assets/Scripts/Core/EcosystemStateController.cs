using UnityEngine;
using ButterflyHouse.Butterflies;
using ButterflyHouse.Plants;

namespace ButterflyHouse.Core
{
    /// <summary>
    /// Central progression state controller for the Butterfly House ecosystem.
    /// Tracks all progression meters and manages global state.
    /// </summary>
    public class EcosystemStateController : MonoBehaviour
    {
        public static EcosystemStateController Instance { get; private set; }
        
        [Header("Progression Meters")]
        [Range(0f, 100f)]
        [SerializeField] private float serenityLevel = 0f; // Increases when user is still
        [Range(0f, 100f)]
        [SerializeField] private float curiosityLevel = 0f; // Increases when user explores/touches plants
        [Range(0f, 100f)]
        [SerializeField] private float harmonyLevel = 0f; // Increases when butterflies flock or land
        [SerializeField] private float timeAlive = 0f; // Total runtime in seconds
        
        [Header("Progression Stage")]
        [Range(0, 5)]
        [SerializeField] private int progressionStage = 0; // 0-5 main phases
        
        [Header("Tracking")]
        [SerializeField] private bool firstButterflyLanding = false;
        [SerializeField] private int totalPlantTouches = 0;
        [SerializeField] private int butterflyLandingsOnPlayer = 0;
        [SerializeField] private float serenitySustainedTime = 0f;
        [SerializeField] private float lastSerenityThreshold = 60f;
        
        [Header("Hand Aura")]
        [SerializeField] private HandAuraSystem handAuraSystem;
        
        [Header("Event Orchestrator")]
        [SerializeField] private EventOrchestrator eventOrchestrator;
        
        [Header("Stage Manager")]
        [SerializeField] private ProgressionStageManager stageManager;
        
        [Header("Light Cycle")]
        [SerializeField] private LightCycle lightCycle;
        
        // Tracking
        private Vector3 _lastPlayerPosition;
        private float _lastPlayerPositionUpdate;
        private float _serenitySustainedStartTime = -1f;

        // Camera.main does a FindGameObjectsWithTag("MainCamera") under the hood — cache it.
        private Transform _cachedCameraTransform;
        
        // Events
        public System.Action<int> OnStageChanged;
        public System.Action<float> OnSerenityChanged;
        public System.Action<float> OnCuriosityChanged;
        public System.Action<float> OnHarmonyChanged;
        public System.Action OnFirstButterflyLanding;
        
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
            
            if (eventOrchestrator == null)
                eventOrchestrator = GetComponent<EventOrchestrator>();
            
            if (stageManager == null)
                stageManager = GetComponent<ProgressionStageManager>();
            
            if (lightCycle == null)
                lightCycle = GetComponent<LightCycle>();
        }
        
        private void Start()
        {
            progressionStage = 0;
            InitializeStage(0);
            
            _lastPlayerPosition = GetPlayerPosition();
            _lastPlayerPositionUpdate = Time.time;
        }
        
        private void Update()
        {
            timeAlive += Time.deltaTime;
            
            UpdateProgressionMeters();
            CheckStageProgression();
            
            // Update light cycle
            if (lightCycle != null)
            {
                lightCycle.UpdateCycle(timeAlive);
            }
        }
        
        private Vector3 GetPlayerPosition()
        {
            // Refresh the cached reference if the main camera has been destroyed/swapped.
            if (_cachedCameraTransform == null)
            {
                var cam = Camera.main;
                if (cam != null) _cachedCameraTransform = cam.transform;
            }
            if (_cachedCameraTransform != null)
                return _cachedCameraTransform.position;
            return Vector3.zero;
        }
        
        private void UpdateProgressionMeters()
        {
            // Update Serenity Level (player stillness)
            Vector3 currentPlayerPos = GetPlayerPosition();
            float timeSinceUpdate = Time.time - _lastPlayerPositionUpdate;
            
            if (timeSinceUpdate > 0.1f) // Update every 0.1s
            {
                float movement = Vector3.Distance(currentPlayerPos, _lastPlayerPosition);
                
                if (movement < 0.01f) // Very still
                {
                    // Player is still
                    if (_serenitySustainedStartTime < 0f)
                    {
                        _serenitySustainedStartTime = Time.time;
                    }
                    
                    serenitySustainedTime = Time.time - _serenitySustainedStartTime;
                    serenityLevel = Mathf.Clamp(serenityLevel + Time.deltaTime * 0.5f, 0f, 100f);
                }
                else
                {
                    // Player is moving
                    _serenitySustainedStartTime = -1f;
                    serenitySustainedTime = 0f;
                    serenityLevel = Mathf.Clamp(serenityLevel - Time.deltaTime * 0.2f, 0f, 100f);
                }
                
                _lastPlayerPosition = currentPlayerPos;
                _lastPlayerPositionUpdate = Time.time;
                
                OnSerenityChanged?.Invoke(serenityLevel);
            }
            
            // Check if serenity has been sustained above threshold
            if (serenityLevel > lastSerenityThreshold && serenitySustainedTime > 60f)
            {
                // Sustained serenity achieved - this can trigger stage progression
            }
        }
        
        private void CheckStageProgression()
        {
            int nextStage = progressionStage;
            bool shouldAdvance = false;
            
            switch (progressionStage)
            {
                case 0: // Emergence
                    // Trigger: timeAlive > 45 sec OR firstButterflyLanding == true
                    if (timeAlive > 45f || firstButterflyLanding)
                    {
                        nextStage = 1;
                        shouldAdvance = true;
                    }
                    break;
                    
                case 1: // Expansion
                    // Trigger: harmonyLevel > threshold AND serenityLevel > threshold
                    if (harmonyLevel > 20f && serenityLevel > 15f)
                    {
                        nextStage = 2;
                        shouldAdvance = true;
                    }
                    break;
                    
                case 2: // Symbiosis
                    // Trigger: curiosityLevel > threshold AND totalPlantTouches > X
                    if (curiosityLevel > 30f && totalPlantTouches > 5)
                    {
                        nextStage = 3;
                        shouldAdvance = true;
                    }
                    break;
                    
                case 3: // Emergent Ecology
                    // Trigger: serenityLevel sustained > 60 seconds OR swarmEvent triggered
                    if (serenitySustainedTime > 60f)
                    {
                        nextStage = 4;
                        shouldAdvance = true;
                    }
                    break;
                    
                case 4: // Synesthetic Overgrowth
                    // Trigger: all primary meters above threshold OR rare event timer > 12-18 minutes
                    if ((serenityLevel > 50f && curiosityLevel > 50f && harmonyLevel > 50f) ||
                        (eventOrchestrator != null && eventOrchestrator.TimeSinceLastEvent > 720f)) // 12 minutes
                    {
                        nextStage = 5;
                        shouldAdvance = true;
                    }
                    break;
                    
                case 5: // Ascension State
                    // Stay in ascension state
                    break;
            }
            
            if (shouldAdvance)
            {
                AdvanceToStage(nextStage);
            }
        }
        
        private void InitializeStage(int stage)
        {
            progressionStage = stage;
            
            if (stageManager != null)
            {
                stageManager.OnStageEntered(stage);
            }
            
            OnStageChanged?.Invoke(stage);
            Debug.Log($"Progression Stage: {stage}");
        }
        
        private void AdvanceToStage(int newStage)
        {
            if (newStage <= progressionStage) return;
            
            InitializeStage(newStage);
        }
        
        /// <summary>
        /// Called when a butterfly lands on the player.
        /// </summary>
        public void OnButterflyLandOnPlayer()
        {
            butterflyLandingsOnPlayer++;
            
            if (!firstButterflyLanding)
            {
                firstButterflyLanding = true;
                OnFirstButterflyLanding?.Invoke();
            }
            
            harmonyLevel = Mathf.Clamp(harmonyLevel + 2f, 0f, 100f);
            OnHarmonyChanged?.Invoke(harmonyLevel);
            
            // Update hand aura
            if (handAuraSystem != null)
            {
                handAuraSystem.OnButterflyLanding();
            }
        }
        
        /// <summary>
        /// Called when a butterfly interacts with a plant (pollination).
        /// </summary>
        public void OnButterflyPlantInteraction()
        {
            harmonyLevel = Mathf.Clamp(harmonyLevel + 1f, 0f, 100f);
            OnHarmonyChanged?.Invoke(harmonyLevel);
        }
        
        /// <summary>
        /// Called when a flower is pollinated. Registers pollination and updates harmony.
        /// </summary>
        public void RegisterPollination(Flowers.Flower flower, float pollenAmount)
        {
            if (flower == null) return;
            
            // Increase harmony based on pollen amount
            harmonyLevel = Mathf.Clamp(harmonyLevel + pollenAmount * 0.2f, 0f, 100f);
            OnHarmonyChanged?.Invoke(harmonyLevel);
            
            // Meta-Flowers can spawn fruit seeds
            if (flower.CurrentStage == Flowers.Flower.FlowerStage.Meta)
            {
                if (Plants.FruitManager.Instance != null)
                {
                    Vector3 spawnPos = flower.transform.position + Vector3.up * 0.15f;
                    Plants.FruitManager.Instance.TrySpawnFruitAt(spawnPos);
                }
            }
        }
        
        /// <summary>
        /// Called when player touches/interacts with a plant.
        /// </summary>
        public void OnPlayerPlantInteraction()
        {
            totalPlantTouches++;
            curiosityLevel = Mathf.Clamp(curiosityLevel + 1f, 0f, 100f);
            OnCuriosityChanged?.Invoke(curiosityLevel);
        }
        
        /// <summary>
        /// Called when player performs a gesture or explores.
        /// </summary>
        public void OnPlayerExploration()
        {
            curiosityLevel = Mathf.Clamp(curiosityLevel + 0.5f, 0f, 100f);
            OnCuriosityChanged?.Invoke(curiosityLevel);
        }
        
        /// <summary>
        /// Called when a swarm event is triggered by user gesture.
        /// </summary>
        public void OnSwarmEventTriggered()
        {
            // Can trigger stage progression from 3 to 4
            if (progressionStage == 3)
            {
                AdvanceToStage(4);
            }
        }
        
        // Public getters and setters
        public int ProgressionStage 
        { 
            get => progressionStage; 
            set 
            { 
                progressionStage = value; 
                OnStageChanged?.Invoke(progressionStage);
            }
        }
        
        public float SerenityLevel 
        { 
            get => serenityLevel; 
            set 
            { 
                serenityLevel = Mathf.Clamp(value, 0f, 100f);
                OnSerenityChanged?.Invoke(serenityLevel);
            }
        }
        
        public float CuriosityLevel 
        { 
            get => curiosityLevel; 
            set 
            { 
                curiosityLevel = Mathf.Clamp(value, 0f, 100f);
                OnCuriosityChanged?.Invoke(curiosityLevel);
            }
        }
        
        public float HarmonyLevel 
        { 
            get => harmonyLevel; 
            set 
            { 
                harmonyLevel = Mathf.Clamp(value, 0f, 100f);
                OnHarmonyChanged?.Invoke(harmonyLevel);
            }
        }
        
        public float TimeAlive 
        { 
            get => timeAlive; 
            set => timeAlive = value;
        }
        public int TotalPlantTouches => totalPlantTouches;
        public int ButterflyLandingsOnPlayer => butterflyLandingsOnPlayer;
        public bool FirstButterflyLanding => firstButterflyLanding;
        public float SerenitySustainedTime => serenitySustainedTime;
    }
}

