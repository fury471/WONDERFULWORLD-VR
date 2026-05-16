using UnityEngine;

namespace ButterflyHouse.Plants
{
    /// <summary>
    /// Manages plant growth phases and evolution.
    /// Plants evolve from small bulbs to fractal blooms to psychedelic sentience.
    /// </summary>
    public class PlantGrowthSystem : MonoBehaviour
    {
        public enum GrowthPhase
        {
            Sprout,          // Level 0: Minimal mesh, single note on touch
            Bloom,           // Level 1: New tendrils grow, bioluminescent pulses, multiple notes
            FractalBloom,    // Level 2: Branches subdivide procedurally, responds to butterflies landing
            PsychedelicSentience // Level 3: Fully responsive, emit chords, sway based on audio band energy
        }
        
        [Header("Growth Phase")]
        [SerializeField] private GrowthPhase currentPhase = GrowthPhase.Sprout;
        
        [Header("Growth Requirements")]
        [SerializeField] private int touchesRequiredForBloom = 3;
        [SerializeField] private int butterflyVisitsRequiredForFractalBloom = 5;
        [SerializeField] private float harmonyRequiredForSentience = 50f;
        [SerializeField] private float timeRequiredForSentience = 300f; // 5 minutes
        
        [Header("Tracking")]
        [SerializeField] private int touchCount = 0;
        [SerializeField] private int butterflyVisitCount = 0;
        [SerializeField] private float timeInBloomPhase = 0f;
        
        [Header("Visual")]
        [SerializeField] private Transform growthTarget; // Transform to scale/grow
        [SerializeField] private GameObject[] phaseVisuals; // Visual objects for each phase
        
        [Header("Flower Spawning")]
        [SerializeField] private Flowers.Flower flowerPrefab;
        [SerializeField] private bool spawnFlowers = true;
        [SerializeField] private GrowthPhase minPhaseForFlower = GrowthPhase.Bloom;
        private Flowers.Flower _attachedFlower;
        
        private GenerativePlant _generativePlant;
        private PlantVisualController _visualController;
        
        // Events
        public System.Action<GrowthPhase> OnGrowthPhaseChanged;
        
        private void Awake()
        {
            _generativePlant = GetComponent<GenerativePlant>();
            _visualController = GetComponent<PlantVisualController>();
        }
        
        private void Update()
        {
            if (currentPhase == GrowthPhase.FractalBloom)
            {
                timeInBloomPhase += Time.deltaTime;
                CheckForSentience();
            }
            
            // Update phase-specific behaviors
            UpdatePhaseBehaviors();
        }
        
        private void UpdatePhaseBehaviors()
        {
            switch (currentPhase)
            {
                case GrowthPhase.Bloom:
                    // Bioluminescent pulses
                    UpdateBioluminescentPulses();
                    break;
                    
                case GrowthPhase.FractalBloom:
                    // Responds to butterflies landing
                    // Procedural branch subdivision handled visually
                    break;
                    
                case GrowthPhase.PsychedelicSentience:
                    // Responds to footsteps
                    // Sway based on audio band energy
                    // Emit chords
                    UpdateSentientBehaviors();
                    break;
            }
        }
        
        private void UpdateBioluminescentPulses()
        {
            // Pulse emission based on time
            if (_visualController != null)
            {
                float pulse = Mathf.Sin(Time.time * 2f) * 0.5f + 0.5f;
                // Update visual pulse
            }
        }
        
        private void UpdateSentientBehaviors()
        {
            // Plants respond to footsteps (player position)
            // Plants sway based on audio band energy
            // Generate particle loops
            // Emit chords instead of single notes
        }
        
        /// <summary>
        /// Called when plant is touched.
        /// </summary>
        public void OnTouched()
        {
            touchCount++;
            
            // Level 0 → Level 1: Touches required for Bloom
            if (currentPhase == GrowthPhase.Sprout && touchCount >= touchesRequiredForBloom)
            {
                AdvanceToPhase(GrowthPhase.Bloom);
            }
        }
        
        /// <summary>
        /// Called when a butterfly visits/pollinates the plant.
        /// </summary>
        public void OnButterflyVisit()
        {
            butterflyVisitCount++;
            
            // Level 1 → Level 2: Butterfly visits required for Fractal Bloom
            if (currentPhase == GrowthPhase.Bloom && butterflyVisitCount >= butterflyVisitsRequiredForFractalBloom)
            {
                AdvanceToPhase(GrowthPhase.FractalBloom);
            }
        }
        
        private void CheckForSentience()
        {
            if (currentPhase != GrowthPhase.FractalBloom) return;
            
            Core.EcosystemStateController stateController = Core.EcosystemStateController.Instance;
            if (stateController == null) return;
            
            // Level 2 → Level 3: Requires harmony level + time
            if (stateController.HarmonyLevel >= harmonyRequiredForSentience &&
                timeInBloomPhase >= timeRequiredForSentience)
            {
                AdvanceToPhase(GrowthPhase.PsychedelicSentience);
            }
        }
        
        private void AdvanceToPhase(GrowthPhase newPhase)
        {
            currentPhase = newPhase;
            OnGrowthPhaseChanged?.Invoke(newPhase);
            
            Debug.Log($"Plant {gameObject.name} advanced to phase: {newPhase}");
            
            ApplyPhaseEffects(newPhase);
        }
        
        private void ApplyPhaseEffects(GrowthPhase phase)
        {
            // Update visuals
            if (phaseVisuals != null && phaseVisuals.Length > (int)phase)
            {
                // Hide previous phase visuals
                for (int i = 0; i < phaseVisuals.Length; i++)
                {
                    if (phaseVisuals[i] != null)
                        phaseVisuals[i].SetActive(i == (int)phase);
                }
            }
            
            // Update scale/growth
            if (growthTarget != null)
            {
                float scale = 1f + ((int)phase * 0.3f); // Grow with each phase
                growthTarget.localScale = Vector3.one * scale;
            }
            
            switch (phase)
            {
                case GrowthPhase.FractalBloom:
                    EnableBloomEffects();
                    break;
                    
                case GrowthPhase.PsychedelicSentience:
                    EnableSentienceEffects();
                    break;
            }
            
            // Try to spawn flower when reaching minimum phase
            if ((int)phase >= (int)minPhaseForFlower)
            {
                TrySpawnFlower();
            }
        }
        
        /// <summary>
        /// Try to spawn a flower attached to this plant.
        /// </summary>
        private void TrySpawnFlower()
        {
            if (!spawnFlowers || _attachedFlower != null || flowerPrefab == null) return;
            
            if ((int)currentPhase < (int)minPhaseForFlower) return;
            
            // Spawn flower at top of plant or designated anchor point
            Vector3 flowerPos = GetFlowerAnchorPoint();
            GameObject flowerObj = Instantiate(flowerPrefab.gameObject, flowerPos, Quaternion.identity, transform);
            _attachedFlower = flowerObj.GetComponent<Flowers.Flower>();
            
            if (_attachedFlower != null)
            {
                // Parent plant reference will be set by Flower's Start() method
                // via GetComponentInParent<GenerativePlant>()
                
                Debug.Log($"Plant {gameObject.name} spawned flower at {flowerPos}");
            }
        }
        
        /// <summary>
        /// Get the anchor point for spawning flowers (top of plant).
        /// </summary>
        private Vector3 GetFlowerAnchorPoint()
        {
            if (growthTarget != null)
            {
                // Place at top of growth target
                Bounds bounds = growthTarget.GetComponent<Renderer>()?.bounds ?? new Bounds(transform.position, Vector3.one);
                return bounds.center + Vector3.up * (bounds.size.y * 0.5f + 0.2f);
            }
            
            // Default: above plant center
            return transform.position + Vector3.up * 1f;
        }
        
        /// <summary>
        /// Get the attached flower.
        /// </summary>
        public Flowers.Flower GetAttachedFlower()
        {
            if (_attachedFlower == null)
                _attachedFlower = GetComponentInChildren<Flowers.Flower>();
            
            return _attachedFlower;
        }
        
        private void EnableBloomEffects()
        {
            // Level 1: Bloom
            // New tendrils grow (visual)
            // Bioluminescent pulses
            // Multiple notes per touch (instead of single note)
            if (_generativePlant != null)
            {
                // Plants can now play arpeggios/multiple notes
            }
            Debug.Log($"Plant {gameObject.name} has bloomed!");
        }
        
        private void EnableSentienceEffects()
        {
            // Level 3: Psychedelic Sentience
            // Fully responsive plants
            // Emit chords, not single notes
            // Plants sway based on audio band energy
            // Touch releases spores/light particles
            // Respond to footsteps or hand movement
            Debug.Log($"Plant {gameObject.name} has achieved psychedelic sentience!");
        }
        
        public GrowthPhase CurrentPhase => currentPhase;
    }
}

