using UnityEngine;
using System.Collections;

namespace ButterflyHouse.Core
{
    /// <summary>
    /// Manages synesthetic storm events and meta-level ascension events.
    /// Triggers rare spectacular events based on ecosystem state.
    /// </summary>
    public class EventSystem : MonoBehaviour
    {
        public enum EventType
        {
            None,
            ButterflyEclipse,
            HarmonicRain,
            ChromaticStorm,
            ChrysalisOfConsciousness,
            ButterflyChoir,
            DissolutionIntoFrequency
        }
        
        [Header("Event Settings")]
        [SerializeField] private float timeSinceLastEvent = 0f;
        [SerializeField] private float minEventInterval = 600f; // 10 minutes
        [SerializeField] private float maxEventInterval = 1200f; // 20 minutes
        [SerializeField] private float nextEventTime = 0f;
        
        [Header("Event Requirements")]
        [SerializeField] private float harmonyRequiredForEclipse = 50f;
        [SerializeField] private float affinityRequiredForChoir = 70f;
        
        private EventType _activeEvent = EventType.None;
        private EcosystemManager _ecosystemManager;
        
        // Event objects/prefabs
        [Header("Event Prefabs")]
        [SerializeField] private GameObject eclipsePrefab;
        [SerializeField] private GameObject harmonicRainPrefab;
        [SerializeField] private GameObject chromaticStormPrefab;
        [SerializeField] private GameObject chrysalisPrefab;
        
        private void Awake()
        {
            _ecosystemManager = FindFirstObjectByType<EcosystemManager>();
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
            if (_ecosystemManager == null) return;
            
            // Check for Chrysalis of Consciousness (one-time, high harmony + affinity)
            if (_ecosystemManager.HarmonyLevel >= 90f && 
                _ecosystemManager.AffinityLevel >= 80f &&
                _activeEvent == EventType.None)
            {
                TriggerEvent(EventType.ChrysalisOfConsciousness);
            }
            
            // Check for Butterfly Choir (high affinity)
            if (_ecosystemManager.AffinityLevel >= affinityRequiredForChoir &&
                _activeEvent == EventType.None)
            {
                TriggerEvent(EventType.ButterflyChoir);
            }
        }
        
        private void TriggerRandomEvent()
        {
            if (_ecosystemManager == null) return;
            
            EventType[] availableEvents = GetAvailableEvents();
            if (availableEvents.Length == 0) return;
            
            EventType chosenEvent = availableEvents[Random.Range(0, availableEvents.Length)];
            TriggerEvent(chosenEvent);
        }
        
        private EventType[] GetAvailableEvents()
        {
            System.Collections.Generic.List<EventType> available = new System.Collections.Generic.List<EventType>();
            
            if (_ecosystemManager == null) return available.ToArray();
            
            // Butterfly Eclipse - requires moderate harmony
            if (_ecosystemManager.HarmonyLevel >= harmonyRequiredForEclipse)
            {
                available.Add(EventType.ButterflyEclipse);
            }
            
            // Harmonic Rain - always available after phase 2
            if (_ecosystemManager.CurrentPhase >= EcosystemManager.EcosystemPhase.TerritorialPatterns)
            {
                available.Add(EventType.HarmonicRain);
            }
            
            // Chromatic Storm - requires high harmony
            if (_ecosystemManager.HarmonyLevel >= 70f)
            {
                available.Add(EventType.ChromaticStorm);
            }
            
            return available.ToArray();
        }
        
        public void TriggerEvent(EventType eventType)
        {
            if (_activeEvent != EventType.None) return;
            
            _activeEvent = eventType;
            timeSinceLastEvent = 0f;
            nextEventTime = Random.Range(minEventInterval, maxEventInterval);
            
            Debug.Log($"Event Triggered: {eventType}");
            
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
                    
                case EventType.ChrysalisOfConsciousness:
                    yield return StartCoroutine(ChrysalisOfConsciousnessEvent());
                    break;
                    
                case EventType.ButterflyChoir:
                    yield return StartCoroutine(ButterflyChoirEvent());
                    break;
                    
                case EventType.DissolutionIntoFrequency:
                    yield return StartCoroutine(DissolutionIntoFrequencyEvent());
                    break;
            }
            
            _activeEvent = EventType.None;
        }
        
        private IEnumerator ButterflyEclipseEvent()
        {
            Debug.Log("Butterfly Eclipse: All butterflies gather overhead!");
            // Gather all butterflies overhead
            // Create rotating disk
            // Generate harmonic drone
            yield return new WaitForSeconds(30f);
        }
        
        private IEnumerator HarmonicRainEvent()
        {
            Debug.Log("Harmonic Rain: Light beams fall like rain!");
            // Spawn vertical light beams
            // Make them playable/touchable
            yield return new WaitForSeconds(45f);
        }
        
        private IEnumerator ChromaticStormEvent()
        {
            Debug.Log("Chromatic Storm: Synchronized spirals and light spores!");
            // Thicken trails
            // Synchronize butterfly spirals
            // Plant spore release
            yield return new WaitForSeconds(60f);
        }
        
        private IEnumerator ChrysalisOfConsciousnessEvent()
        {
            Debug.Log("Chrysalis of Consciousness: Giant chrysalis appears!");
            // Spawn giant chrysalis
            // Show morphing patterns
            // Burst into light cathedral
            yield return new WaitForSeconds(90f);
        }
        
        private IEnumerator ButterflyChoirEvent()
        {
            Debug.Log("Butterfly Choir: All butterflies form coherent melody!");
            // Organize butterflies into choir formation
            // Play coherent melody
            yield return new WaitForSeconds(60f);
        }
        
        private IEnumerator DissolutionIntoFrequencyEvent()
        {
            Debug.Log("Dissolution into Frequency: Space becomes pure waveform!");
            // Dissolve geometry
            // Turn butterflies to waveforms
            // Plants become oscilloscopes
            yield return new WaitForSeconds(120f);
            
            // Reforms back to normal
            Debug.Log("Space reforms to normal...");
        }
        
        public EventType ActiveEvent => _activeEvent;
    }
}

