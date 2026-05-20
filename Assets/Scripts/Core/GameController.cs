using UnityEngine;

namespace ButterflyHouse.Core
{
    /// <summary>
    /// Main game controller that orchestrates the psychedelic butterfly house experience.
    /// Handles initialization and coordination between systems.
    /// </summary>
    public class GameController : MonoBehaviour
    {
        [Header("System References")]
        [SerializeField] private ButterflyHouse.Butterflies.ButterflyManager butterflyManager;
        [SerializeField] private ButterflyHouse.Audio.AudioManager audioManager;
        [SerializeField] private ButterflyHouse.Interaction.InteractionManager interactionManager;
        [SerializeField] private EcosystemStateController ecosystemStateController;
        [SerializeField] private HandAuraSystem handAuraSystem;
        [SerializeField] private EventOrchestrator eventOrchestrator;
        [SerializeField] private ProgressionStageManager stageManager;
        [SerializeField] private LightCycle lightCycle;
        
        [Header("Experience Settings")]
        [SerializeField] private bool autoStart = true;
        [SerializeField] private float introDelay = 2f;
        
        private bool _isActive;
        
        private void Awake()
        {
            // Ensure systems are initialized
            if (butterflyManager == null)
                butterflyManager = FindFirstObjectByType<ButterflyHouse.Butterflies.ButterflyManager>();
            
            if (audioManager == null)
                audioManager = FindFirstObjectByType<ButterflyHouse.Audio.AudioManager>();
            
            if (interactionManager == null)
                interactionManager = FindFirstObjectByType<ButterflyHouse.Interaction.InteractionManager>();
            
            if (ecosystemStateController == null)
                ecosystemStateController = FindFirstObjectByType<EcosystemStateController>();
            
            if (handAuraSystem == null)
                handAuraSystem = FindFirstObjectByType<HandAuraSystem>();
            
            if (eventOrchestrator == null)
                eventOrchestrator = FindFirstObjectByType<EventOrchestrator>();
            
            if (stageManager == null)
                stageManager = FindFirstObjectByType<ProgressionStageManager>();
            
            if (lightCycle == null)
                lightCycle = FindFirstObjectByType<LightCycle>();
        }
        
        private void Start()
        {
            if (autoStart)
            {
                Invoke(nameof(StartExperience), introDelay);
            }
        }
        
        public void StartExperience()
        {
            if (_isActive) return;
            
            _isActive = true;
            
            // Initialize systems
            if (audioManager != null)
            {
                // Audio system should auto-initialize via Awake
            }
            
            if (butterflyManager != null)
            {
                // Butterfly system should auto-initialize
            }
            
            if (interactionManager != null)
            {
                // Interaction system should auto-initialize
            }
            
            Debug.Log("Butterfly House Experience Started");
        }
        
        public void StopExperience()
        {
            if (!_isActive) return;
            
            _isActive = false;
            
            if (butterflyManager != null)
            {
                butterflyManager.ClearAllButterflies();
            }
            
            Debug.Log("Butterfly House Experience Stopped");
        }
        
        public bool IsActive => _isActive;
    }
}

