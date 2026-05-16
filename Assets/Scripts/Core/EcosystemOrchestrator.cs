using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using ButterflyHouse.Butterflies;
using ButterflyHouse.Plants;
using ButterflyHouse.Flowers;

namespace ButterflyHouse.Core
{
    /// <summary>
    /// Central brain of the ecosystem. Tracks global metrics, advances progression stages,
    /// and coordinates all subsystems (butterflies, fruits, plants, flowers, hand aura, events).
    /// </summary>
    public class EcosystemOrchestrator : MonoBehaviour
    {
        public static EcosystemOrchestrator Instance { get; private set; }
        
        [Header("Global State")]
        [SerializeField] private EcosystemStateController ecosystemState;
        [SerializeField] private ProgressionStageManager progressionStageManager;
        
        [Header("Subsystems")]
        [SerializeField] private ButterflyManager butterflyManager;
        [SerializeField] private FruitManager fruitManager;
        [SerializeField] private PlantManager plantManager;
        [SerializeField] private HandAuraSystem handAuraManager;
        [SerializeField] private EventOrchestrator eventOrchestrator;
        
        [Header("Tuning")]
        [Range(0f, 1f)]
        [SerializeField] private float serenityDecayRate = 0.2f;
        [Range(0f, 2f)]
        [SerializeField] private float serenityGainWhenStill = 0.5f;
        [Range(0f, 2f)]
        [SerializeField] private float curiosityGainPerTouch = 0.5f;
        [Range(0f, 1f)]
        [SerializeField] private float harmonyGainPerPollination = 0.3f;
        
        [Header("Player Tracking")]
        [SerializeField] private Transform headTransform;
        [SerializeField] private Transform leftHandTransform;
        [SerializeField] private Transform rightHandTransform;
        
        [Header("Keyboard Controls (Development/Testing)")]
        [SerializeField] private bool enableKeyboardControls = true;
        [SerializeField] private KeyCode stageUpKey = KeyCode.PageUp;
        [SerializeField] private KeyCode stageDownKey = KeyCode.PageDown;
        [SerializeField] private KeyCode[] stageKeys = { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6 };
        
        private Vector3 _lastHeadPos;
        private Vector3 _lastLeftHandPos;
        private Vector3 _lastRightHandPos;
        
        private float _updateInterval = 0.1f; // Update every 0.1 seconds
        private float _nextUpdateTime = 0f;
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            
            // Auto-find components if not assigned
            if (ecosystemState == null)
                ecosystemState = FindObjectOfType<EcosystemStateController>();
            
            if (progressionStageManager == null)
                progressionStageManager = FindObjectOfType<ProgressionStageManager>();
            
            if (butterflyManager == null)
                butterflyManager = FindObjectOfType<ButterflyManager>();
            
            if (fruitManager == null)
                fruitManager = FindObjectOfType<FruitManager>();
            
            if (plantManager == null)
                plantManager = FindObjectOfType<PlantManager>();
            
            if (handAuraManager == null)
                handAuraManager = FindObjectOfType<HandAuraSystem>();
            
            if (eventOrchestrator == null)
                eventOrchestrator = FindObjectOfType<EventOrchestrator>();
        }
        
        private void Start()
        {
            // Initialize tracking positions
            if (headTransform != null)
                _lastHeadPos = headTransform.position;
            
            if (leftHandTransform != null)
                _lastLeftHandPos = leftHandTransform.position;
            
            if (rightHandTransform != null)
                _lastRightHandPos = rightHandTransform.position;
            
            // Initialize progression stage
            if (ecosystemState != null && progressionStageManager != null)
            {
                int initialStage = progressionStageManager.EvaluateStage(ecosystemState);
                if (ecosystemState.ProgressionStage != initialStage)
                {
                    OnProgressionStageChanged(ecosystemState.ProgressionStage, initialStage);
                    ecosystemState.ProgressionStage = initialStage;
                }
            }
        }
        
        private void Update()
        {
            if (ecosystemState == null) return;
            
            // Update time alive
            ecosystemState.TimeAlive += Time.deltaTime;
            
            // Handle keyboard controls for stage cycling
            if (enableKeyboardControls)
            {
                HandleKeyboardInput();
            }
            
            // Throttled updates for performance
            if (Time.time >= _nextUpdateTime)
            {
                _nextUpdateTime = Time.time + _updateInterval;
                
                UpdateSerenity(Time.deltaTime);
                UpdateProgression();
            }
        }
        
        // ----------------------------
        // SERENITY / CURIOSITY / HARMONY
        // ----------------------------
        
        private void UpdateSerenity(float dt)
        {
            if (headTransform == null || leftHandTransform == null || rightHandTransform == null)
                return;
            
            // Calculate velocities
            float headVel = Vector3.Distance(headTransform.position, _lastHeadPos) / dt;
            float leftVel = Vector3.Distance(leftHandTransform.position, _lastLeftHandPos) / dt;
            float rightVel = Vector3.Distance(rightHandTransform.position, _lastRightHandPos) / dt;
            
            float avgVel = (headVel + leftVel + rightVel) / 3f;
            
            bool isStill = avgVel < 0.01f;
            
            // Update serenity based on stillness (setter will clamp automatically)
            if (isStill)
            {
                ecosystemState.SerenityLevel += serenityGainWhenStill * dt;
            }
            else
            {
                ecosystemState.SerenityLevel -= serenityDecayRate * dt;
            }
            
            // Update tracking positions
            _lastHeadPos = headTransform.position;
            _lastLeftHandPos = leftHandTransform.position;
            _lastRightHandPos = rightHandTransform.position;
            
            // Notify HandAuraManager
            if (handAuraManager != null)
            {
                handAuraManager.UpdateFromSerenity(ecosystemState.SerenityLevel);
            }
        }
        
        /// <summary>
        /// Register when player touches a plant.
        /// </summary>
        public void RegisterPlantTouch(GenerativePlant plant)
        {
            if (ecosystemState == null || plant == null) return;
            
            ecosystemState.CuriosityLevel += curiosityGainPerTouch; // Setter will clamp automatically
            
            if (plantManager != null)
            {
                plantManager.OnPlantTouched(plant);
            }
        }
        
        /// <summary>
        /// Register when a flower is pollinated.
        /// </summary>
        public void RegisterPollination(Flower flower, float pollenAmount)
        {
            if (ecosystemState == null || flower == null) return;
            
            float harmonyGain = harmonyGainPerPollination * pollenAmount;
            ecosystemState.HarmonyLevel += harmonyGain; // Setter will clamp automatically
            
            // Also notify EcosystemStateController (for compatibility)
            if (ecosystemState != null)
            {
                ecosystemState.RegisterPollination(flower, pollenAmount);
            }
            
            if (plantManager != null)
            {
                plantManager.OnFlowerPollinated(flower);
            }
            
            if (fruitManager != null)
            {
                fruitManager.OnFlowerPollinated(flower);
            }
        }
        
        /// <summary>
        /// Register when a butterfly lands on player's hand.
        /// </summary>
        public void RegisterButterflyLandingOnHand(Butterfly butterfly)
        {
            if (ecosystemState == null || butterfly == null) return;
            
            ecosystemState.HarmonyLevel += 0.2f; // Setter will clamp automatically
            
            // Also notify EcosystemStateController (for compatibility)
            ecosystemState.OnButterflyLandOnPlayer();
            
            if (handAuraManager != null)
            {
                handAuraManager.OnButterflyLanding();
            }
        }
        
        // ----------------------------
        // PROGRESSION
        // ----------------------------
        
        private void UpdateProgression()
        {
            if (ecosystemState == null || progressionStageManager == null) return;
            
            int previousStage = ecosystemState.ProgressionStage;
            int newStage = progressionStageManager.EvaluateStage(ecosystemState);
            
            if (newStage != previousStage)
            {
                OnProgressionStageChanged(previousStage, newStage);
                ecosystemState.ProgressionStage = newStage;
            }
        }
        
        private void OnProgressionStageChanged(int oldStage, int newStage)
        {
            Debug.Log($"EcosystemOrchestrator: Progression stage changed from {oldStage} → {newStage}");
            
            // Inform all subsystems
            if (butterflyManager != null)
            {
                butterflyManager.OnProgressionStageChanged(newStage);
            }
            
            if (fruitManager != null)
            {
                fruitManager.OnProgressionStageChanged(newStage);
            }
            
            if (plantManager != null)
            {
                plantManager.OnProgressionStageChanged(newStage);
            }
            
            if (handAuraManager != null)
            {
                handAuraManager.OnProgressionStageChanged(newStage);
            }
            
            if (eventOrchestrator != null)
            {
                eventOrchestrator.OnProgressionStageChanged(newStage);
            }
            
            // Also notify progression stage manager for compatibility
            if (progressionStageManager != null)
            {
                progressionStageManager.OnStageEntered(newStage);
            }
        }
        
        /// <summary>
        /// Handle keyboard input for manual stage progression (for testing/development).
        /// Uses the new Unity Input System if available, otherwise falls back to legacy Input.
        /// </summary>
        private void HandleKeyboardInput()
        {
            if (ecosystemState == null || progressionStageManager == null) return;
            
            // Try to use new Input System first
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                // Cycle stages up (Page Up)
                if (keyboard.pageUpKey.wasPressedThisFrame)
                {
                    int currentStage = ecosystemState.ProgressionStage;
                    int nextStage = Mathf.Min(5, currentStage + 1);
                    if (nextStage != currentStage)
                    {
                        SetStageManually(nextStage);
                    }
                }
                
                // Cycle stages down (Page Down)
                if (keyboard.pageDownKey.wasPressedThisFrame)
                {
                    int currentStage = ecosystemState.ProgressionStage;
                    int prevStage = Mathf.Max(0, currentStage - 1);
                    if (prevStage != currentStage)
                    {
                        SetStageManually(prevStage);
                    }
                }
                
                // Direct stage selection with number keys (1-6)
                if (keyboard.digit1Key.wasPressedThisFrame) SetStageManually(0);
                if (keyboard.digit2Key.wasPressedThisFrame) SetStageManually(1);
                if (keyboard.digit3Key.wasPressedThisFrame) SetStageManually(2);
                if (keyboard.digit4Key.wasPressedThisFrame) SetStageManually(3);
                if (keyboard.digit5Key.wasPressedThisFrame) SetStageManually(4);
                if (keyboard.digit6Key.wasPressedThisFrame) SetStageManually(5);
            }
            else
            {
                // Fallback to legacy Input System if new one is not available
                #if ENABLE_LEGACY_INPUT_MANAGER
                // Cycle stages up
                if (Input.GetKeyDown(stageUpKey))
                {
                    int currentStage = ecosystemState.ProgressionStage;
                    int nextStage = Mathf.Min(5, currentStage + 1);
                    if (nextStage != currentStage)
                    {
                        SetStageManually(nextStage);
                    }
                }
                
                // Cycle stages down
                if (Input.GetKeyDown(stageDownKey))
                {
                    int currentStage = ecosystemState.ProgressionStage;
                    int prevStage = Mathf.Max(0, currentStage - 1);
                    if (prevStage != currentStage)
                    {
                        SetStageManually(prevStage);
                    }
                }
                
                // Direct stage selection with number keys (1-6)
                for (int i = 0; i < stageKeys.Length && i <= 5; i++)
                {
                    if (Input.GetKeyDown(stageKeys[i]))
                    {
                        int targetStage = i; // Alpha1 = stage 0, Alpha2 = stage 1, etc.
                        SetStageManually(targetStage);
                    }
                }
                #endif
            }
        }
        
        /// <summary>
        /// Manually set the progression stage (for keyboard controls).
        /// </summary>
        private void SetStageManually(int targetStage)
        {
            if (ecosystemState == null) return;
            
            targetStage = Mathf.Clamp(targetStage, 0, 5);
            int currentStage = ecosystemState.ProgressionStage;
            
            if (targetStage != currentStage)
            {
                Debug.Log($"[EcosystemOrchestrator] Manual stage change: {currentStage} → {targetStage} (via keyboard)");
                
                OnProgressionStageChanged(currentStage, targetStage);
                ecosystemState.ProgressionStage = targetStage;
                
                // Also notify progression stage manager
                if (progressionStageManager != null)
                {
                    progressionStageManager.OnStageEntered(targetStage);
                }
            }
        }
        
        // Public getters
        public EcosystemStateController EcosystemState => ecosystemState;
        public ProgressionStageManager ProgressionStageManager => progressionStageManager;
        public ButterflyManager ButterflyManager => butterflyManager;
        public FruitManager FruitManager => fruitManager;
        public PlantManager PlantManager => plantManager;
        public HandAuraSystem HandAuraManager => handAuraManager;
        public EventOrchestrator EventOrchestrator => eventOrchestrator;
    }
}

