using UnityEngine;
using System.Collections;

namespace ButterflyHouse.Core
{
    /// <summary>
    /// Orchestrates rare synesthetic storm events and ascension events.
    /// Manages event timing, triggers, and execution.
    /// </summary>
    public class EventOrchestrator : MonoBehaviour
    {
    public enum EventType
    {
        None,
        ButterflyEclipse,
        HarmonicRain,
        ChromaticStorm,
        FruitBloom,          // Stage 3+ only - all fruits glow and play
        GreatChrysalis,      // Stage 5 only
        ButterflyChoir,      // Stage 5 only
        DissolutionIntoFrequency // Stage 5 only
    }
        
        [Header("Event Timing")]
        [SerializeField] private float timeSinceLastEvent = 0f;
        [SerializeField] private float minEventInterval = 600f; // 10 minutes
        [SerializeField] private float maxEventInterval = 1200f; // 20 minutes
        [SerializeField] private float nextEventTime = 0f;
        
        [Header("Event Requirements")]
        [SerializeField] private float harmonyRequiredForEclipse = 40f;
        [SerializeField] private float harmonyRequiredForStorm = 70f;
        [SerializeField] private float harmonyRequiredForAscension = 90f;
        [SerializeField] private float affinityRequiredForChoir = 70f;
        
        [Header("Event Prefabs")]
        [SerializeField] private GameObject eclipsePrefab;
        [SerializeField] private GameObject harmonicRainPrefab;
        [SerializeField] private GameObject chromaticStormPrefab;
        [SerializeField] private GameObject greatChrysalisPrefab;
        
        private EventType _activeEvent = EventType.None;
        private EcosystemStateController _stateController;
        
        // Events
        public System.Action<EventType> OnEventStarted;
        public System.Action<EventType> OnEventEnded;
        
        private void Awake()
        {
            _stateController = FindObjectOfType<EcosystemStateController>();
            nextEventTime = Random.Range(minEventInterval, maxEventInterval);
        }
        
        private void Update()
        {
            if (_activeEvent == EventType.None)
            {
                timeSinceLastEvent += Time.deltaTime;
                
                // Check for random event triggers
                if (timeSinceLastEvent >= nextEventTime)
                {
                    TriggerRandomEvent();
                }
                
                // Check for special event conditions
                CheckSpecialEventConditions();
            }
        }
        
        private void CheckSpecialEventConditions()
        {
            if (_stateController == null) return;
            
            int currentStage = _stateController.ProgressionStage;
            
            // Stage 5 only events
            if (currentStage >= 5)
            {
                // Great Chrysalis - one-time, very high harmony + harmony
                if (_stateController.HarmonyLevel >= harmonyRequiredForAscension &&
                    _stateController.HarmonyLevel >= 80f &&
                    _activeEvent == EventType.None)
                {
                    TriggerEvent(EventType.GreatChrysalis);
                }
                
                // Butterfly Choir - high harmony (equivalent to affinity)
                if (_stateController.HarmonyLevel >= affinityRequiredForChoir &&
                    _activeEvent == EventType.None)
                {
                    TriggerEvent(EventType.ButterflyChoir);
                }
            }
        }
        
        private void TriggerRandomEvent()
        {
            if (_stateController == null) return;
            
            EventType[] availableEvents = GetAvailableEvents();
            if (availableEvents.Length == 0) return;
            
            EventType chosenEvent = availableEvents[Random.Range(0, availableEvents.Length)];
            TriggerEvent(chosenEvent);
        }
        
        private EventType[] GetAvailableEvents()
        {
            System.Collections.Generic.List<EventType> available = new System.Collections.Generic.List<EventType>();
            
            if (_stateController == null) return available.ToArray();
            
            int currentStage = _stateController.ProgressionStage;
            float harmony = _stateController.HarmonyLevel;
            
            // Butterfly Eclipse - requires moderate harmony, Stage 1+
            if (currentStage >= 1 && harmony >= harmonyRequiredForEclipse)
            {
                available.Add(EventType.ButterflyEclipse);
            }
            
            // Harmonic Rain - Stage 2+, moderate harmony
            if (currentStage >= 2 && harmony >= 30f)
            {
                available.Add(EventType.HarmonicRain);
            }
            
            // Chromatic Storm - requires high harmony, Stage 3+
            if (currentStage >= 3 && harmony >= harmonyRequiredForStorm)
            {
                available.Add(EventType.ChromaticStorm);
            }
            
            // Fruit Bloom - Stage 3+
            if (currentStage >= 3)
            {
                available.Add(EventType.FruitBloom);
            }
            
            return available.ToArray();
        }
        
        public void TriggerEvent(EventType eventType)
        {
            if (_activeEvent != EventType.None) return;
            
            _activeEvent = eventType;
            timeSinceLastEvent = 0f;
            nextEventTime = Random.Range(minEventInterval, maxEventInterval);
            
            Debug.Log($"Event Orchestrator: Triggering {eventType}");
            OnEventStarted?.Invoke(eventType);
            
            StartCoroutine(ExecuteEvent(eventType));
        }
        
        private IEnumerator ExecuteEvent(EventType eventType)
        {
            switch (eventType)
            {
                case EventType.ButterflyEclipse:
                    yield return StartCoroutine(ButterflyEclipseEvent());
                    break;
                    
                case EventType.HarmonicRain:
                    yield return StartCoroutine(HarmonicRainEvent());
                    break;
                    
                case EventType.ChromaticStorm:
                    yield return StartCoroutine(ChromaticStormEvent());
                    break;
                    
                case EventType.FruitBloom:
                    yield return StartCoroutine(FruitBloomEvent());
                    break;
                    
                case EventType.GreatChrysalis:
                    yield return StartCoroutine(GreatChrysalisEvent());
                    break;
                    
                case EventType.ButterflyChoir:
                    yield return StartCoroutine(ButterflyChoirEvent());
                    break;
                    
                case EventType.DissolutionIntoFrequency:
                    yield return StartCoroutine(DissolutionIntoFrequencyEvent());
                    break;
            }
            
            _activeEvent = EventType.None;
            OnEventEnded?.Invoke(eventType);
            Debug.Log($"Event Orchestrator: {eventType} completed");
        }
        
        private IEnumerator ButterflyEclipseEvent()
        {
            Debug.Log("Butterfly Eclipse: All butterflies gather overhead into rotating disk");
            
            // Gather all butterflies overhead
            // Create rotating disk formation
            // Generate low harmonic drone
            // Project shapes on dome
            
            float duration = 30f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                // Animate butterfly formation
                // Update harmonic drone
                yield return null;
            }
        }
        
        private IEnumerator HarmonicRainEvent()
        {
            Debug.Log("Harmonic Rain: Beams of light fall around user, playable on touch");
            
            // Spawn vertical light beams
            // Make them touchable/playable
            // Plants echo melodies
            
            float duration = 45f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                // Animate rain beams
                // Handle touch interactions
                yield return null;
            }
        }
        
        private IEnumerator ChromaticStormEvent()
        {
            Debug.Log("Chromatic Storm: Synchronized spirals, light spores, cosmic chord");
            
            // Increase all shader emission
            // Synchronize butterfly spirals
            // Plant spore release
            // Swell ambient drones with harmony
            // Wave of color passes through environment
            
            float duration = 60f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                // Animate storm effects
                // Synchronize movements
                // Update audio
                yield return null;
            }
        }
        
        private IEnumerator FruitBloomEvent()
        {
            Debug.Log("Fruit Bloom: All fruits begin glowing simultaneously, chord spreads across sanctuary");
            
            // Trigger fruit bloom in FruitManager
            if (Plants.FruitManager.Instance != null)
            {
                Plants.FruitManager.Instance.TriggerFruitBloom();
            }
            
            // Butterflies rush to fruit zones
            // Plants sway in synchronized resonance
            
            float duration = 45f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                
                // Animate fruit pulsing
                // Trigger butterfly attraction to fruits
                // Synchronize plant swaying
                
                yield return null;
            }
            
            Debug.Log("Fruit Bloom event completed");
        }
        
        /// <summary>
        /// Called when progression stage changes.
        /// </summary>
        public void OnProgressionStageChanged(int newStage)
        {
            Debug.Log($"EventOrchestrator: Progression stage changed to {newStage}");
            
            // Stage 5 triggers ascension sequence
            if (newStage == 5)
            {
                TriggerAscensionSequence();
            }
        }
        
        private void TriggerAscensionSequence()
        {
            Debug.Log("Triggering Ascension Sequence: Great Chrysalis + Butterfly Choir + Dissolution");
            
            // Trigger ascension events in sequence
            StartCoroutine(AscensionSequenceCoroutine());
        }
        
        private System.Collections.IEnumerator AscensionSequenceCoroutine()
        {
            // Great Chrysalis
            yield return StartCoroutine(GreatChrysalisEvent());
            
            // Small delay
            yield return new WaitForSeconds(5f);
            
            // Butterfly Choir
            yield return StartCoroutine(ButterflyChoirEvent());
            
            // Small delay
            yield return new WaitForSeconds(5f);
            
            // Dissolution Into Pure Frequency
            yield return StartCoroutine(DissolutionIntoFrequencyEvent());
        }
        
        private IEnumerator GreatChrysalisEvent()
        {
            Debug.Log("Great Chrysalis: Giant chrysalis forms above player with fractal patterns");
            
            // Spawn giant chrysalis
            // Show morphing fractal patterns
            // Emit low frequencies
            // Crack open and release waveform butterflies
            
            float duration = 90f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                // Animate chrysalis
                // Update patterns
                yield return null;
            }
        }
        
        private IEnumerator ButterflyChoirEvent()
        {
            Debug.Log("Butterfly Choir: All butterflies synchronize into multi-layer chord progression");
            
            // Synchronize all butterfly voices
            // Form multi-layer chord progression
            // Create geometric bloom around user
            
            float duration = 60f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                // Animate choir formation
                // Synchronize audio
                yield return null;
            }
        }
        
        private IEnumerator DissolutionIntoFrequencyEvent()
        {
            Debug.Log("Dissolution into Frequency: Space becomes pure waveform");
            
            // Dissolve geometry into waveforms
            // Plants flatten into oscilloscopes
            // Butterflies turn into pure sine ribbons
            // Entire space becomes living audio-visual waveform
            
            float dissolveDuration = 30f;
            float holdDuration = 60f;
            float reformDuration = 30f;
            
            // Dissolve phase
            float elapsed = 0f;
            while (elapsed < dissolveDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / dissolveDuration;
                // Dissolve geometry
                yield return null;
            }
            
            // Hold phase - pure waveform state
            yield return new WaitForSeconds(holdDuration);
            
            // Reform phase
            elapsed = 0f;
            while (elapsed < reformDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / reformDuration;
                // Reform geometry
                yield return null;
            }
            
            Debug.Log("Space has reformed to normal");
        }
        
        public EventType ActiveEvent => _activeEvent;
        public float TimeSinceLastEvent => timeSinceLastEvent;
    }
}

