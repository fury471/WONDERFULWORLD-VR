using UnityEngine;
using ButterflyHouse.Core;
using ButterflyHouse.Interaction;

namespace ButterflyHouse.Plants
{
    /// <summary>
    /// Interactive fruit that butterflies can land on and feed from.
    /// Provides a landing target and optional visual/audio feedback when butterflies land.
    /// </summary>
    public class GenerativeFruit : MonoBehaviour
    {
        [Header("Landing Target")]
        [SerializeField] private LandingTarget landingTarget;
        [SerializeField] private bool createLandingTarget = true;
        [Range(0.1f, 2f)]
        [SerializeField] private float landingZoneRadius = 0.3f;
        
        [Header("Visual")]
        [SerializeField] private Renderer fruitRenderer;
        [Range(0f, 1f)]
        [SerializeField] private float glowIntensity = 0.5f;
        [SerializeField] private bool animateGlow = true;
        [Range(0.5f, 3f)]
        [SerializeField] private float glowSpeed = 1f;
        
        [Header("Audio")]
        [SerializeField] private AudioClip[] landingClips;
        [SerializeField] private AudioClip[] melodicClips; // Stage-based melodic clips
        [SerializeField] private AudioSource audioSource;
        [Range(0f, 1f)]
        [SerializeField] private float audioVolume = 0.6f;
        [SerializeField] private bool playOnButterflyLand = true;
        [SerializeField] private bool playMelodyContinuously = false;
        [SerializeField] private float melodyPlayInterval = 5f;
        
        [Header("Feeding")]
        [SerializeField] private bool canBeConsumed = false;
        [Range(0.5f, 10f)]
        [SerializeField] private float consumptionTime = 5f;
        [SerializeField] private GameObject consumableVisual;
        
        [Header("Energy Output")]
        [SerializeField] private float energyOutput = 0.5f; // Energy given per second to butterflies
        [SerializeField] private float resonanceFieldRadius = 5f; // Radius of energy field
        
        [Header("Touch Interaction")]
        [SerializeField] private float touchCooldown = 0.5f; // Cooldown between touches
        [SerializeField] private bool allowMultipleTouches = true;
        
        [Header("Stage")]
        [SerializeField] private FruitGrowthSystem fruitGrowthSystem;
        
        private MaterialPropertyBlock _mpb;
        private float _glowPhase = 0f;
        private float _currentGlow = 0f;
        private bool _isBeingConsumed = false;
        private float _consumptionTimer = 0f;
        private float _melodyTimer = 0f;
        private FruitGrowthSystem.FruitStage _currentStage = FruitGrowthSystem.FruitStage.Seed;
        private float _lastTouchTime = 0f;

        // Cached shader property IDs and presence flags — avoid per-frame HasProperty(string) calls.
        private static readonly int _emissionStrengthId = Shader.PropertyToID("_EmissionStrength");
        private static readonly int _emissionId = Shader.PropertyToID("_Emission");
        private static readonly int _baseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int _colorId = Shader.PropertyToID("_Color");
        private bool _hasEmissionStrength;
        private bool _hasEmission;
        private bool _hasBaseColor;
        private bool _hasColor;

        // Throttle glow / energy updates — these don't need to fire every frame.
        private const float ENERGY_UPDATE_INTERVAL = 0.5f;
        private float _energyUpdateTimer;
        
        private void Awake()
        {
            if (fruitRenderer == null)
                fruitRenderer = GetComponent<Renderer>();

            if (fruitRenderer != null)
            {
                _mpb = new MaterialPropertyBlock();
                if (fruitRenderer.sharedMaterial != null)
                {
                    var mat = fruitRenderer.sharedMaterial;
                    _hasEmissionStrength = mat.HasProperty(_emissionStrengthId);
                    _hasEmission = mat.HasProperty(_emissionId);
                    _hasBaseColor = mat.HasProperty(_baseColorId);
                    _hasColor = mat.HasProperty(_colorId);
                }
            }
            
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
            
            // Set up audio source
            if (audioSource != null)
            {
                audioSource.playOnAwake = false;
                audioSource.loop = false;
                audioSource.spatialBlend = 1f; // 3D sound
            }
            
            // Create landing target if needed
            if (createLandingTarget && landingTarget == null)
            {
                CreateLandingTarget();
            }
            
            // Get fruit growth system
            if (fruitGrowthSystem == null)
                fruitGrowthSystem = GetComponent<FruitGrowthSystem>();
            
            // Subscribe to stage changes
            if (fruitGrowthSystem != null)
            {
                fruitGrowthSystem.OnStageChanged += OnFruitStageChanged;
                _currentStage = fruitGrowthSystem.CurrentStage;
            }
            
            // Register with FruitManager
            if (FruitManager.Instance != null)
            {
                FruitManager.Instance.RegisterFruit(this);
            }
        }
        
        private void OnDestroy()
        {
            // Unregister from FruitManager
            if (FruitManager.Instance != null)
            {
                FruitManager.Instance.UnregisterFruit(this);
            }
            
            // Unsubscribe from stage changes
            if (fruitGrowthSystem != null)
            {
                fruitGrowthSystem.OnStageChanged -= OnFruitStageChanged;
            }
        }
        
        private void Update()
        {
            // Animate glow if enabled — uses cached property IDs/flags rather than string lookups per frame.
            if (animateGlow && fruitRenderer != null)
            {
                _glowPhase += Time.deltaTime * glowSpeed;
                _currentGlow = 0.5f + Mathf.Sin(_glowPhase) * glowIntensity * 0.5f;

                if (_hasEmissionStrength || _hasEmission)
                {
                    fruitRenderer.GetPropertyBlock(_mpb);
                    if (_hasEmissionStrength)
                        _mpb.SetFloat(_emissionStrengthId, _currentGlow);
                    else
                        _mpb.SetFloat(_emissionId, _currentGlow);
                    fruitRenderer.SetPropertyBlock(_mpb);
                }
            }

            // Handle consumption
            if (_isBeingConsumed && canBeConsumed)
            {
                _consumptionTimer += Time.deltaTime;

                if (_consumptionTimer >= consumptionTime)
                {
                    OnConsumed();
                }
                else
                {
                    // Visual feedback during consumption
                    float consumptionProgress = _consumptionTimer / consumptionTime;
                    UpdateConsumptionVisual(consumptionProgress);
                }
            }

            // Play continuous melody based on stage
            if (playMelodyContinuously && _currentStage != FruitGrowthSystem.FruitStage.Seed)
            {
                _melodyTimer += Time.deltaTime;

                if (_melodyTimer >= melodyPlayInterval && melodicClips != null && melodicClips.Length > 0 && audioSource != null && !audioSource.isPlaying)
                {
                    PlayMelody();
                    _melodyTimer = 0f;
                }
            }

            // Energy output is driven by stage, which changes rarely — throttle to ~2x/sec.
            _energyUpdateTimer += Time.deltaTime;
            if (_energyUpdateTimer >= ENERGY_UPDATE_INTERVAL)
            {
                _energyUpdateTimer = 0f;
                UpdateEnergyOutput();
            }
        }
        
        private void UpdateEnergyOutput()
        {
            // Energy output increases with stage
            float baseEnergy = 0.5f;
            switch (_currentStage)
            {
                case FruitGrowthSystem.FruitStage.Seed:
                    energyOutput = 0.2f; // Low - butterflies can detect but cannot feed
                    break;
                    
                case FruitGrowthSystem.FruitStage.Harmonic:
                    energyOutput = baseEnergy; // Normal
                    break;
                    
                case FruitGrowthSystem.FruitStage.Resonant:
                    energyOutput = baseEnergy * 1.5f; // Higher
                    break;
                    
                case FruitGrowthSystem.FruitStage.Celestial:
                    energyOutput = baseEnergy * 2f; // Maximum
                    break;
            }
        }
        
        /// <summary>
        /// Play a melodic clip based on stage.
        /// </summary>
        public void PlayMelody()
        {
            if (audioSource == null || melodicClips == null || melodicClips.Length == 0) return;
            
            AudioClip clip = melodicClips[Random.Range(0, melodicClips.Length)];
            if (clip != null)
            {
                float volume = audioVolume;
                if (Core.Settings.Instance != null)
                {
                    volume *= Core.Settings.Instance.plantVolume;
                }
                audioSource.PlayOneShot(clip, volume);
            }
        }
        
        private void OnFruitStageChanged(FruitGrowthSystem.FruitStage stage)
        {
            _currentStage = stage;
            
            // Update visual controller
            FruitVisualController visualController = GetComponent<FruitVisualController>();
            if (visualController != null)
            {
                visualController.OnStageChanged(stage);
            }
            
            // Update audio properties based on stage
            UpdateAudioForStage(stage);
        }
        
        private void UpdateAudioForStage(FruitGrowthSystem.FruitStage stage)
        {
            // Update melody play interval based on stage
            switch (stage)
            {
                case FruitGrowthSystem.FruitStage.Seed:
                    melodyPlayInterval = 10f; // Slow, single tones
                    break;
                    
                case FruitGrowthSystem.FruitStage.Harmonic:
                    melodyPlayInterval = 5f; // Arpeggios
                    break;
                    
                case FruitGrowthSystem.FruitStage.Resonant:
                    melodyPlayInterval = 3f; // Chords and pads
                    break;
                    
                case FruitGrowthSystem.FruitStage.Celestial:
                    melodyPlayInterval = 2f; // Full-spectrum sequences
                    playMelodyContinuously = true;
                    break;
            }
        }
        
        private void CreateLandingTarget()
        {
            GameObject landingObj = new GameObject("LandingTarget");
            landingObj.transform.SetParent(transform);
            landingObj.transform.localPosition = Vector3.zero;
            
            // Add sphere collider for landing detection
            SphereCollider collider = landingObj.AddComponent<SphereCollider>();
            collider.radius = landingZoneRadius;
            collider.isTrigger = true;
            
            // Add LandingTarget component
            landingTarget = landingObj.AddComponent<LandingTarget>();
            
            // Set target type to Fruit
            var field = typeof(LandingTarget).GetField("targetType", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(landingTarget, LandingTarget.TargetType.Fruit);
            }
        }
        
        /// <summary>
        /// Called when a butterfly lands on this fruit.
        /// </summary>
        public void OnButterflyLanded(Butterflies.Butterfly butterfly)
        {
            if (butterfly == null) return;
            
            // Visual feedback
            PulseGlow();
            
            // Audio feedback
            if (playOnButterflyLand && audioSource != null && landingClips != null && landingClips.Length > 0)
            {
                AudioClip clip = landingClips[Random.Range(0, landingClips.Length)];
                if (clip != null)
                {
                    float volume = audioVolume;
                    if (Core.Settings.Instance != null)
                    {
                        volume *= Core.Settings.Instance.plantVolume;
                    }
                    audioSource.PlayOneShot(clip, volume);
                }
            }
            
            // Notify fruit growth system of butterfly feed
            if (fruitGrowthSystem != null)
            {
                fruitGrowthSystem.OnButterflyFeed();
            }
            
            // Notify ecosystem manager of butterfly-plant interaction
            if (Core.EcosystemStateController.Instance != null)
            {
                Core.EcosystemStateController.Instance.OnButterflyPlantInteraction();
            }
            
            // Start consumption if enabled
            if (canBeConsumed && !_isBeingConsumed)
            {
                _isBeingConsumed = true;
                _consumptionTimer = 0f;
            }
        }
        
        /// <summary>
        /// Called when butterfly starts feeding from this fruit (continuous).
        /// </summary>
        public void OnButterflyFeeding(Butterflies.Butterfly butterfly)
        {
            if (butterfly == null) return;
            
            // Trigger visual feedback (subtle pulse)
            FruitVisualController visualController = GetComponent<FruitVisualController>();
            if (visualController != null)
            {
                visualController.Pulse(0.3f, 0.2f);
            }
        }
        
        /// <summary>
        /// Called when a butterfly leaves this fruit.
        /// </summary>
        public void OnButterflyLeft(Butterflies.Butterfly butterfly)
        {
            if (canBeConsumed && _isBeingConsumed)
            {
                _isBeingConsumed = false;
                _consumptionTimer = 0f;
                UpdateConsumptionVisual(0f);
            }
        }
        
        private void PulseGlow()
        {
            if (fruitRenderer == null) return;

            _currentGlow = 1f;
            fruitRenderer.GetPropertyBlock(_mpb);
            if (_hasEmissionStrength)
                _mpb.SetFloat(_emissionStrengthId, _currentGlow);
            else if (_hasEmission)
                _mpb.SetFloat(_emissionId, _currentGlow);
            fruitRenderer.SetPropertyBlock(_mpb);

            // Fade back
            StartCoroutine(FadeGlowCoroutine());
        }
        
        private System.Collections.IEnumerator FadeGlowCoroutine()
        {
            float startGlow = _currentGlow;
            float elapsed = 0f;
            float duration = 0.5f;

            while (elapsed < duration && fruitRenderer != null)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                _currentGlow = Mathf.Lerp(startGlow, 0.5f + glowIntensity * 0.5f, t);

                if (_hasEmissionStrength || _hasEmission)
                {
                    fruitRenderer.GetPropertyBlock(_mpb);
                    if (_hasEmissionStrength)
                        _mpb.SetFloat(_emissionStrengthId, _currentGlow);
                    else
                        _mpb.SetFloat(_emissionId, _currentGlow);
                    fruitRenderer.SetPropertyBlock(_mpb);
                }

                yield return null;
            }
        }
        
        private void UpdateConsumptionVisual(float progress)
        {
            if (consumableVisual != null)
            {
                // Scale down as consumed
                float scale = 1f - progress * 0.5f; // Reduce to 50% size
                consumableVisual.transform.localScale = Vector3.one * scale;
            }

            // Update material color/alpha
            if (fruitRenderer != null && (_hasBaseColor || _hasColor))
            {
                fruitRenderer.GetPropertyBlock(_mpb);
                Color baseColor;
                if (_hasBaseColor)
                    baseColor = _mpb.GetColor(_baseColorId);
                else
                    baseColor = _mpb.GetColor(_colorId);

                baseColor.a = 1f - progress * 0.3f; // Fade slightly

                if (_hasBaseColor)
                    _mpb.SetColor(_baseColorId, baseColor);
                else
                    _mpb.SetColor(_colorId, baseColor);

                fruitRenderer.SetPropertyBlock(_mpb);
            }
        }
        
        private void OnConsumed()
        {
            _isBeingConsumed = false;
            _consumptionTimer = 0f;

            // Disable or destroy the fruit
            if (consumableVisual != null)
            {
                consumableVisual.SetActive(false);
            }

            // Disable landing target
            if (landingTarget != null)
            {
                landingTarget.gameObject.SetActive(false);
            }

            // Disable glow
            _currentGlow = 0f;
            if (fruitRenderer != null && (_hasEmissionStrength || _hasEmission))
            {
                fruitRenderer.GetPropertyBlock(_mpb);
                if (_hasEmissionStrength)
                    _mpb.SetFloat(_emissionStrengthId, 0f);
                else
                    _mpb.SetFloat(_emissionId, 0f);
                fruitRenderer.SetPropertyBlock(_mpb);
            }
        }
        
        public LandingTarget LandingTarget => landingTarget;
        public bool IsAvailable => landingTarget != null && landingTarget.IsAvailable;
        public float EnergyOutput => energyOutput;
        public float ResonanceFieldRadius => resonanceFieldRadius;
        public FruitGrowthSystem.FruitStage CurrentStage => _currentStage;
        
        /// <summary>
        /// Called when fruit stage changes externally.
        /// </summary>
        public void OnStageChanged(FruitGrowthSystem.FruitStage stage)
        {
            OnFruitStageChanged(stage);
        }
        
        /// <summary>
        /// Called when player touches the fruit.
        /// </summary>
        public void OnTouched(Vector3 touchPoint)
        {
            // Check cooldown
            if (Time.time - _lastTouchTime < touchCooldown && !allowMultipleTouches)
                return;
            
            _lastTouchTime = Time.time;
            
            // Visual feedback - pulse glow
            PulseGlow();
            
            // Audio feedback - play stage-based melody
            PlayMelody();
            
            // Notify ecosystem orchestrator (if exists) - for curiosity level
            if (Core.EcosystemOrchestrator.Instance != null)
            {
                // Register fruit touch (could add RegisterFruitTouch method to orchestrator)
                // For now, we'll just trigger visual/audio feedback
            }
            
            // Notify ecosystem state controller for compatibility
            if (Core.EcosystemStateController.Instance != null)
            {
                Core.EcosystemStateController.Instance.OnPlayerExploration(); // Increments curiosity
            }
        }
        
        private void OnTriggerEnter(Collider other)
        {
            // Check if this is a hand proxy or interaction
            var handProxy = other.GetComponent<Interaction.HandProxy>();
            if (handProxy != null)
            {
                Vector3 touchPoint = other.ClosestPoint(transform.position);
                OnTouched(touchPoint);
            }
        }
        
        private void OnCollisionEnter(Collision collision)
        {
            var handProxy = collision.gameObject.GetComponent<Interaction.HandProxy>();
            if (handProxy != null)
            {
                Vector3 touchPoint = collision.contacts[0].point;
                OnTouched(touchPoint);
            }
        }
    }
}

