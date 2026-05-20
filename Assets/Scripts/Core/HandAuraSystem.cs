using UnityEngine;
using ButterflyHouse.Interaction;

namespace ButterflyHouse.Core
{
    /// <summary>
    /// Manages hand aura evolution and gesture recognition.
    /// Hand aura levels up as butterflies land on player hands.
    /// </summary>
    public class HandAuraSystem : MonoBehaviour
    {
        public enum AuraLevel
        {
            Neutral,      // Level 0: No aura
            Attractor,    // Level 1: Hands glow, butterflies approach slowly (3 landings)
            Conductor,    // Level 2: Trails align, rotating gestures spawn micro-swarm (7 landings + serenity)
            GestureSpellbook // Level 3: Gesture spells (palm-up, circles, hand-to-hand arc)
        }
        
        [Header("Aura Level")]
        [SerializeField] private AuraLevel currentAuraLevel = AuraLevel.Neutral;
        
        [Header("Level Progression")]
        [SerializeField] private int landingsRequiredForAttractor = 3;
        [SerializeField] private int landingsRequiredForConductor = 7;
        [SerializeField] private float serenityRequiredForConductor = 30f;
        [SerializeField] private int currentLandingCount = 0;
        
        [Header("Visual")]
        [SerializeField] private Material auraMaterial;
        [SerializeField] private ParticleSystem auraParticles;
        [SerializeField] private Gradient auraColorGradient;
        
        [Header("Gesture Recognition")]
        [SerializeField] private float gestureDetectionRadius = 0.5f;
        [SerializeField] private float circleGestureThreshold = 0.8f;
        [SerializeField] private float pulseGestureThreshold = 0.5f;
        [SerializeField] private float palmUpHoldDuration = 2f;
        
        private Interaction.InteractionManager _interactionManager;
        private Interaction.HandProxy _leftHand;
        private Interaction.HandProxy _rightHand;
        private Vector3 _lastLeftHandPos;
        private Vector3 _lastRightHandPos;
        private float _gestureStartTime = 0f;
        private GestureType _currentGesture = GestureType.None;
        
        public enum GestureType
        {
            None,
            Circle,
            Pulse,
            HandToHandArc
        }
        
        // Events
        public System.Action<AuraLevel> OnAuraLevelChanged;
        public System.Action<GestureType> OnGestureDetected;
        
        private void Awake()
        {
            _interactionManager = FindFirstObjectByType<InteractionManager>();
            
            if (_interactionManager != null)
            {
                var hands = _interactionManager.GetTrackedHands();
                foreach (var hand in hands)
                {
                    if (hand.Hand == HandProxy.HandType.Left)
                        _leftHand = hand;
                    else if (hand.Hand == HandProxy.HandType.Right)
                        _rightHand = hand;
                }
            }
        }
        
        private void Update()
        {
            UpdateAuraVisuals();
            
            if (currentAuraLevel >= AuraLevel.Conductor)
            {
                DetectConductorGestures();
            }
            
            if (currentAuraLevel >= AuraLevel.GestureSpellbook)
            {
                DetectGestureSpells();
            }
        }
        
        /// <summary>
        /// Called when a butterfly lands on the player.
        /// </summary>
        public void OnButterflyLanding()
        {
            currentLandingCount++;
            
            // Check for Attractor level (3 landings)
            if (currentLandingCount >= landingsRequiredForAttractor && currentAuraLevel < AuraLevel.Attractor)
            {
                SetAuraLevel(AuraLevel.Attractor);
            }
            
            // Check for Conductor level (7 landings + serenity threshold)
            if (currentLandingCount >= landingsRequiredForConductor && currentAuraLevel < AuraLevel.Conductor)
            {
                Core.EcosystemStateController stateController = Core.EcosystemStateController.Instance;
                if (stateController != null && stateController.SerenityLevel >= serenityRequiredForConductor)
                {
                    SetAuraLevel(AuraLevel.Conductor);
                }
            }
            
            // Gesture Spellbook unlocks based on gesture performance (handled separately)
        }
        
        private void SetAuraLevel(AuraLevel newLevel)
        {
            currentAuraLevel = newLevel;
            OnAuraLevelChanged?.Invoke(newLevel);
            
            Debug.Log($"Hand Aura Level: {newLevel}");
            
            switch (newLevel)
            {
                case AuraLevel.Attractor:
                    EnableAttractorAura();
                    break;
                    
                case AuraLevel.Conductor:
                    EnableConductorAura();
                    break;
                    
                case AuraLevel.GestureSpellbook:
                    EnableGestureSpellbook();
                    break;
            }
        }
        
        private void EnableAttractorAura()
        {
            // Enable soft glow on hands
            // Make butterflies approach slowly
            if (auraParticles != null)
            {
                auraParticles.Play();
            }
            Debug.Log("Attractor Aura: Hands emit soft glow, butterflies approach slowly");
        }
        
        private void EnableConductorAura()
        {
            // Enable trail alignment with hand movement
            // Rotating gestures spawn micro-swarm
            Debug.Log("Conductor Aura: Trails align with hand movement, gestures spawn swarms");
        }
        
        private void EnableGestureSpellbook()
        {
            // Enable gesture spell detection
            Debug.Log("Gesture Spellbook: Gesture spells unlocked!");
        }
        
        private void UpdateAuraVisuals()
        {
            if (currentAuraLevel == AuraLevel.Neutral) return;
            
            // Update aura color based on audio frequencies
            // Update particle intensity based on harmony level
            Core.EcosystemStateController stateController = Core.EcosystemStateController.Instance;
            if (stateController != null)
            {
                float harmony = stateController.HarmonyLevel / 100f;
                
                if (auraMaterial != null)
                {
                    Color auraColor = auraColorGradient.Evaluate(harmony);
                    auraMaterial.SetColor("_EmissionColor", auraColor * harmony);
                }
            }
        }
        
        private void DetectConductorGestures()
        {
            // Detect rotating gestures for micro-swarm
            if (_leftHand != null && _rightHand != null)
            {
                Vector3 leftPos = _leftHand.transform.position;
                Vector3 rightPos = _rightHand.transform.position;
                
                // Detect rotation gesture
                Vector3 leftVel = (leftPos - _lastLeftHandPos) / Time.deltaTime;
                Vector3 rightVel = (rightPos - _lastRightHandPos) / Time.deltaTime;
                
                // If hands are rotating around a point
                if (leftVel.magnitude > pulseGestureThreshold && rightVel.magnitude > pulseGestureThreshold)
                {
                    _currentGesture = GestureType.Circle;
                    OnGestureDetected?.Invoke(_currentGesture);
                    TriggerMicroSwarm();
                }
                
                // Update last positions for next frame
                _lastLeftHandPos = leftPos;
                _lastRightHandPos = rightPos;
            }
        }
        
        private void DetectGestureSpells()
        {
            // Detect spell gestures: palm-up, circles, hand-to-hand arc
            if (_leftHand != null && _rightHand != null)
            {
                Vector3 leftPos = _leftHand.transform.position;
                Vector3 rightPos = _rightHand.transform.position;
                
                // Palm-up gesture (hands held upward for duration)
                DetectPalmUpGesture(leftPos, rightPos);
                
                // Circle gesture for spells
                DetectCircleGestureSpell(leftPos, rightPos);
                
                // Hand-to-hand arc for spells
                DetectHandToHandArcSpell(leftPos, rightPos);
                
                // Update last positions for next frame
                _lastLeftHandPos = leftPos;
                _lastRightHandPos = rightPos;
            }
        }
        
        private void DetectPalmUpGesture(Vector3 leftPos, Vector3 rightPos)
        {
            // Detect if palms are facing up and held steady
            // Simple check: hands above head, minimal movement
            float avgHeight = (leftPos.y + rightPos.y) / 2f;
            if (avgHeight > 1.8f) // Above head
            {
                float movement = Vector3.Distance(leftPos, _lastLeftHandPos) + 
                               Vector3.Distance(rightPos, _lastRightHandPos);
                
                if (movement < 0.05f) // Very still
                {
                    _gestureStartTime += Time.deltaTime;
                    
                    if (_gestureStartTime > palmUpHoldDuration)
                    {
                        _currentGesture = GestureType.Pulse; // Palm-up = pulse
                        OnGestureDetected?.Invoke(_currentGesture);
                        TriggerGestureSpell(_currentGesture);
                        _gestureStartTime = 0f;
                    }
                }
                else
                {
                    _gestureStartTime = 0f;
                }
            }
        }
        
        private void DetectCircleGestureSpell(Vector3 leftPos, Vector3 rightPos)
        {
            // Enhanced circle detection for gesture spells
            Vector3 leftVel = (leftPos - _lastLeftHandPos) / Time.deltaTime;
            if (leftVel.magnitude > circleGestureThreshold && leftVel.magnitude < circleGestureThreshold * 10f)
            {
                _currentGesture = GestureType.Circle;
                OnGestureDetected?.Invoke(_currentGesture);
                TriggerGestureSpell(_currentGesture);
            }
        }
        
        private void DetectHandToHandArcSpell(Vector3 leftPos, Vector3 rightPos)
        {
            // Detect arc gesture between hands
            float handDistance = Vector3.Distance(leftPos, rightPos);
            float midPointY = (leftPos.y + rightPos.y) / 2f;
            
            if (handDistance > gestureDetectionRadius * 0.6f &&
                handDistance < gestureDetectionRadius * 2f &&
                midPointY > 1f)
            {
                _currentGesture = GestureType.HandToHandArc;
                OnGestureDetected?.Invoke(_currentGesture);
                TriggerGestureSpell(_currentGesture);
            }
        }
        
        /// <summary>
        /// Trigger a micro-swarm (Conductor level).
        /// </summary>
        private void TriggerMicroSwarm()
        {
            Debug.Log("Micro-swarm triggered by rotating gesture!");
            // Spawn small burst of butterflies around hands
        }
        
        /// <summary>
        /// Trigger a gesture spell (Gesture Spellbook level).
        /// </summary>
        public void TriggerGestureSpell(GestureType gesture)
        {
            if (currentAuraLevel < AuraLevel.GestureSpellbook) return;
            
            switch (gesture)
            {
                case GestureType.Circle:
                    TriggerSwarmBurst(); // Butterfly vortex
                    break;
                    
                case GestureType.Pulse:
                    TriggerHarmonicChord(); // Palm-up = harmonic burst
                    break;
                    
                case GestureType.HandToHandArc:
                    TriggerSpiralWaveform(); // Ribbon of light connecting butterflies
                    break;
            }
        }
        
        private void TriggerSwarmBurst()
        {
            Debug.Log("Swarm Burst (Butterfly Vortex) triggered!");
            // Spawn burst of butterflies in vortex pattern
        }
        
        private void TriggerHarmonicChord()
        {
            Debug.Log("Harmonic Chord (Palm-up burst) triggered!");
            // Play harmonic chord
            // Could trigger environment morph
        }
        
        private void TriggerSpiralWaveform()
        {
            Debug.Log("Spiral Waveform (Ribbon of light) triggered!");
            // Create spiral of waveform butterflies
            // Connect butterflies with ribbon of light
        }
        
        /// <summary>
        /// Update from serenity level (called by EcosystemOrchestrator).
        /// </summary>
        public void UpdateFromSerenity(float serenity)
        {
            // Gesture Spellbook unlocks at high serenity (serenity > 50)
            if (serenity > 50f && currentAuraLevel < AuraLevel.GestureSpellbook)
            {
                SetAuraLevel(AuraLevel.GestureSpellbook);
            }
        }
        
        /// <summary>
        /// Called when progression stage changes.
        /// </summary>
        public void OnProgressionStageChanged(int newStage)
        {
            Debug.Log($"HandAuraSystem: Progression stage changed to {newStage}");
            
            // Stage-specific aura behaviors can be added here
            // For example: enhance aura visuals, unlock gestures, etc.
        }
        
        public AuraLevel CurrentAuraLevel => currentAuraLevel;
    }
}

