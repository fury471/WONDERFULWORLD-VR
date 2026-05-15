using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ButterflyHouse.Core;
using ButterflyHouse.Interaction;
using ButterflyHouse.Flowers;

namespace ButterflyHouse.Butterflies
{
    /// <summary>
    /// Main butterfly behavior controller.
    /// Manages lifecycle states: Emerging, Flying, Landing, Dissipating.
    /// Handles procedural flight paths and interaction with landing targets.
    /// </summary>
    public class Butterfly : MonoBehaviour
    {
        public enum State
        {
            Emerging,
            Flying,
            Landing,
            Dissipating
        }
        
        [Header("Components")]
        [SerializeField] private ButterflyVisualController visualController;
        [SerializeField] private ButterflyAudio audioController;
        [SerializeField] private ButterflyFormEvolution formEvolution;
        [SerializeField] private ButterflyEnergy energySystem;
        [SerializeField] private ButterflyPollination pollinationSystem;
        [SerializeField] private TrailRenderer trailRenderer;
        [SerializeField] private Collider butterflyCollider;
        
        [Header("Emerging")]
        [SerializeField] private float emergingDuration = 2f;
        [SerializeField] private AnimationCurve emergingScaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        
        [Header("Landing")]
        [SerializeField] private float landingCheckInterval = 2f;
        [SerializeField] private float landingRadius = 3f;
        [SerializeField] private LayerMask landingTargetLayer = -1;
        [Range(1f, 30f)]
        [SerializeField] private float minLandingDuration = 2f;
        [Range(1f, 30f)]
        [SerializeField] private float maxLandingDuration = 8f;
        [Range(5f, 60f)]
        [SerializeField] private float minLandingCooldown = 10f;
        [Range(5f, 60f)]
        [SerializeField] private float maxLandingCooldown = 30f;
        
        [Header("Flocking")]
        [SerializeField] private bool enableFlocking = true;
        [SerializeField] private float flockDetectionRadius = 1f; // Very close proximity required - butterflies must be within 1 meter to flock
        [SerializeField] private int minButterfliesForFlock = 2; // Minimum number of nearby butterflies required to form a flock
        [SerializeField] private float flockCohesionWeight = 1f;
        [SerializeField] private float flockAlignmentWeight = 1f;
        [SerializeField] private float flockSeparationWeight = 1.5f;
        [SerializeField] private float flockSeparationDistance = 2f;
        [SerializeField] private float flockBlendSpeed = 2f;
        [Range(0f, 1f)]
        [SerializeField] private float breakOutChance = 0.1f;
        [SerializeField] private float breakOutCheckInterval = 3f;
        [SerializeField] private float maxFlockTime = 30f;
        [Header("Flocking Cooldown")]
        [SerializeField] private bool enableFlockingCooldown = true;
        [Range(5f, 60f)]
        [SerializeField] private float minFlockingCooldown = 10f; // Minimum cooldown after leaving flock
        [Range(10f, 120f)]
        [SerializeField] private float maxFlockingCooldown = 30f; // Maximum cooldown after leaving flock
        
        [Header("Territory Exploration")]
        [SerializeField] private bool enableTerritoryExploration = true;
        [SerializeField] private float territoryCheckRadius = 5f; // Radius to consider "same territory"
        [SerializeField] private float maxTimeInTerritory = 60f; // Max seconds in same area before seeking new territory
        [SerializeField] private float territoryReachedDistance = 10f; // Distance to consider new territory "reached"
        [SerializeField] private float newTerritoryMinDistance = 15f; // Minimum distance to travel for new territory
        [SerializeField] private float territorySeekSpeed = 1.5f; // Speed multiplier when seeking new territory
        
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = false; // Disabled by default - enable for debugging
        [SerializeField] private float debugLogInterval = 2f; // Minimum interval between debug logs (in seconds)
        
        [Header("Lifetime")]
        [SerializeField] private float minLifetime = 300f; // 5 minutes (300 seconds)
        [SerializeField] private float maxLifetime = 1800f; // 30 minutes (1800 seconds)
        [SerializeField] private float fruitFeedingLifetimeBonus = 60f; // 1 minute per fruit feeding
        [SerializeField] private float flowerPollinationLifetimeBonus = 45f; // 45 seconds per pollination
        [SerializeField] private float landingSeekTimeThreshold = 30f; // Start seeking landing 30 seconds before death
        
        private ButterflyArchetype _archetype;
        private State _currentState = State.Emerging;
        private Vector3 _velocity;
        private float _age;
        private float _landingTimer;
        private float _landingDuration;
        private float _landingStartTime;
        private float _landingCooldownEndTime;
        private Interaction.LandingTarget _currentLandingTarget;
        private Interaction.LandingTarget _lastLandingTarget;
        private Vector3 _landingOffset;
        private float _actualLifetime = -1f; // -1 means immortal
        private float _normalizedAge => _actualLifetime > 0 ? (_age / _actualLifetime) : 0f;
        private bool _seekingFinalLanding = false; // True when butterfly needs to land before dying
        private int _fruitFeedingCount = 0; // Track fruit feedings for lifetime extension
        private int _pollinationCount = 0; // Track pollinations for lifetime extension
        private bool _hasExtendedLifetimeThisLanding = false; // Prevent multiple extensions per landing
        
        // Flight parameters
        private Vector3 _noiseOffset;
        private Vector3 _focalPoint;
        
        // Flocking parameters
        private bool _isInFlock = false;
        private float _flockBlendFactor = 0f;
        private float _flockStartTime = 0f;
        private float _breakOutCheckTimer = 0f;
        private float _flockingCooldownEndTime = 0f; // When the butterfly can re-enter a flock
        private Vector3 _flockVelocity = Vector3.zero;
        private readonly List<Butterfly> _nearbyButterflies = new List<Butterfly>();
        
        // Territory exploration parameters
        private Vector3 _currentTerritoryPosition;
        private float _timeInCurrentTerritory = 0f;
        private bool _seekingNewTerritory = false;
        private Vector3 _newTerritoryTarget;
        private float _territoryCheckTimer = 0f;
        private const float TERRITORY_CHECK_INTERVAL = 1f; // Check territory every second
        
        // Debug tracking
        private float _debugLogTimer = 0f;
        private Vector3 _lastPosition;
        private float _stuckTimer = 0f;
        private const float STUCK_THRESHOLD = 0.01f; // Movement less than this in 1 second = stuck
        
        public State CurrentState => _currentState;
        public ButterflyArchetype Archetype => _archetype;
        public float CurrentSpeed => _velocity.magnitude;
        public bool IsDissipated => _currentState == State.Dissipating && !gameObject.activeSelf;
        public Vector3 Velocity => _velocity;
        
        private void Awake()
        {
            if (visualController == null)
                visualController = GetComponent<ButterflyVisualController>();
            
            if (audioController == null)
                audioController = GetComponent<ButterflyAudio>();
            
            if (formEvolution == null)
                formEvolution = GetComponent<ButterflyFormEvolution>();
            
            if (energySystem == null)
                energySystem = GetComponent<ButterflyEnergy>();
            
            if (pollinationSystem == null)
                pollinationSystem = GetComponent<ButterflyPollination>();
            
            if (trailRenderer == null)
                trailRenderer = GetComponentInChildren<TrailRenderer>();
            
            if (butterflyCollider == null)
                butterflyCollider = GetComponent<Collider>();
            
            // Fix trail material early if it's invalid
            if (trailRenderer != null)
            {
                FixTrailMaterialIfNeeded();
            }
            
            _noiseOffset = Random.insideUnitSphere * 100f;
            // Initialize focal point - use spawn position (will be updated during Initialize if needed)
            _focalPoint = transform.position;
            
            // Ensure focal point is not zero (could cause issues)
            if (_focalPoint.sqrMagnitude < 0.01f)
            {
                _focalPoint = Vector3.zero;
            }
        }
        
        /// <summary>
        /// Check and fix trail material if it's missing or using an invalid shader.
        /// </summary>
        private void FixTrailMaterialIfNeeded()
        {
            if (trailRenderer == null) return;
            
            bool needsFix = false;
            
            // Check if material is null
            if (trailRenderer.sharedMaterial == null)
            {
                needsFix = true;
            }
            // Check if material uses an error shader
            else if (trailRenderer.sharedMaterial != null)
            {
                string shaderName = trailRenderer.sharedMaterial.shader.name;
                if (shaderName.Contains("Error") || 
                    shaderName == "Hidden/InternalErrorShader" ||
                    shaderName == "Hidden/InternalErrorShader (UnityEngine.Shader)")
                {
                    needsFix = true;
                }
            }
            
            if (needsFix)
            {
                Material trailMat = CreateTrailMaterial();
                if (trailMat != null)
                {
                    trailRenderer.sharedMaterial = trailMat;
                    Debug.Log($"Butterfly: Fixed trail material with shader '{trailMat.shader.name}'");
                }
                else
                {
                    Debug.LogWarning("Butterfly: Could not create trail material. Trail will appear magenta.");
                }
            }
        }
        
        /// <summary>
        /// Initialize the butterfly with an archetype.
        /// </summary>
        public void Initialize(ButterflyArchetype archetype)
        {
            if (archetype == null)
            {
                Debug.LogError("Cannot initialize butterfly: archetype is null");
                Destroy(gameObject);
                return;
            }
            
            _archetype = archetype;
            _currentState = State.Emerging;
            _age = 0f;
            _landingTimer = 0f;
            
            // Calculate random lifetime between 5 and 30 minutes
            _actualLifetime = Random.Range(minLifetime, maxLifetime);
            _seekingFinalLanding = false;
            _fruitFeedingCount = 0;
            _pollinationCount = 0;
            
            if (enableDebugLogs)
                Debug.Log($"[Butterfly] {gameObject.name}: Spawned with lifetime {_actualLifetime:F1}s ({_actualLifetime / 60f:F1} minutes)");
            
            // Initialize scale to 0 for emerging
            transform.localScale = Vector3.zero;
            
            // Initialize components
            if (visualController != null)
            {
                visualController.Initialize(archetype);
            }
            
            if (audioController != null)
            {
                audioController.Initialize(this, archetype);
            }
            
            if (trailRenderer != null)
            {
                // Ensure trail material is valid (should be fixed in Awake, but double-check)
                FixTrailMaterialIfNeeded();
                
                if (Settings.Instance != null)
                {
                    trailRenderer.enabled = Settings.Instance.enableTrails;
                }
                else
                {
                    trailRenderer.enabled = true;
                }
                
                // Set trail color from archetype gradient
                var color = archetype.wingColorGradient.Evaluate(0f);
                trailRenderer.startColor = color;
                trailRenderer.endColor = new Color(color.r, color.g, color.b, 0f);
            }
            
            // Set initial velocity
            _velocity = Random.insideUnitSphere.normalized * archetype.flightSpeedCurve.Evaluate(0f);
            // Ensure velocity is not zero
            if (_velocity.sqrMagnitude < 0.01f)
            {
                _velocity = Vector3.forward * archetype.flightSpeedCurve.Evaluate(0f);
            }
            
            // Set focal point to spawn position (butterflies orbit around where they spawn)
            _focalPoint = transform.position;
            if (_focalPoint.sqrMagnitude < 0.01f)
            {
                _focalPoint = Vector3.zero;
            }
            
            // Initialize territory tracking
            _currentTerritoryPosition = transform.position;
            _timeInCurrentTerritory = 0f;
            _seekingNewTerritory = false;
            _territoryCheckTimer = 0f;
            
            _lastPosition = transform.position; // Initialize for debug tracking
            
            if (enableDebugLogs)
                Debug.Log($"[Butterfly] {gameObject.name}: Initialized at {transform.position:F2}, velocity={_velocity:F3}, speed={_velocity.magnitude:F3}, " +
                         $"focalPoint={_focalPoint:F2}, lifetime={(_actualLifetime < 0 ? "IMMORTAL" : _actualLifetime.ToString("F1") + "s")}");
            
            StartCoroutine(LifecycleCoroutine());
        }
        
        private IEnumerator LifecycleCoroutine()
        {
            // Emerging phase
            yield return StartCoroutine(EmergingCoroutine());
            
            // Main flying loop
            _currentState = State.Flying;
            
            // Loop while flying and not expired
            while (_currentState == State.Flying && _age < _actualLifetime)
            {
                yield return null;
            }
            
            // When lifetime expires, butterfly must land before dying
            if (_actualLifetime > 0 && _age >= _actualLifetime && _currentState == State.Flying)
            {
                // Butterfly cannot die mid-flight - must seek landing
                if (enableDebugLogs)
                    Debug.Log($"[Butterfly] {gameObject.name}: Lifetime expired but in flight - seeking final landing (age={_age:F1}s, lifetime={_actualLifetime:F1}s)");
                _seekingFinalLanding = true;
                
                // Continue flying and seeking landing until landed
                while (_currentState == State.Flying && _seekingFinalLanding)
                {
                    yield return null;
                }
                
                // Now check if we landed - if so, we can die
                if (_currentState == State.Landing || _currentLandingTarget != null)
                {
                    // Wait for landing to complete, then dissipate
                    yield return new WaitForSeconds(1f); // Brief pause after landing
                    if (enableDebugLogs)
                        Debug.Log($"[Butterfly] {gameObject.name}: Lifetime expired after landing - beginning dissipation (age={_age:F1}s, lifetime={_actualLifetime:F1}s)");
                    _currentState = State.Dissipating;
                    yield return StartCoroutine(DissipatingCoroutine());
                }
            }
            else if (_currentState == State.Flying && _actualLifetime > 0 && _age >= _actualLifetime)
            {
                // Fallback - if still flying somehow, start dissipating
                if (enableDebugLogs)
                    Debug.Log($"[Butterfly] {gameObject.name}: Lifetime expired in coroutine (age={_age:F1}s, lifetime={_actualLifetime:F1}s)");
                _currentState = State.Dissipating;
                yield return StartCoroutine(DissipatingCoroutine());
            }
        }
        
        private IEnumerator EmergingCoroutine()
        {
            float elapsed = 0f;
            
            while (elapsed < emergingDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / emergingDuration;
                float scale = emergingScaleCurve.Evaluate(t) * _archetype.baseScale;
                transform.localScale = Vector3.one * scale;
                
                yield return null;
            }
            
            transform.localScale = Vector3.one * _archetype.baseScale;
        }
        
        private IEnumerator DissipatingCoroutine()
        {
            if (enableDebugLogs)
                Debug.Log($"[Butterfly] {gameObject.name}: Starting dissipation (age={_age:F1}s, lifetime={_archetype.lifetime:F1}s)");
            
            // Fade out visual
            if (visualController != null)
            {
                StartCoroutine(visualController.FadeOut(2f));
            }
            
            // Fade out audio
            if (audioController != null)
            {
                StartCoroutine(audioController.FadeOut(2f));
            }
            
            yield return new WaitForSeconds(2f);
            
            if (enableDebugLogs)
                Debug.Log($"[Butterfly] {gameObject.name}: Destroying butterfly");
            
            // Cleanup
            ButterflyManager.Instance?.DespawnButterfly(this);
            Destroy(gameObject);
        }
        
        private void Update()
        {
            if (_archetype == null) return;
            
            _age += Time.deltaTime;
            
            // Update debug log timer for throttling
            if (enableDebugLogs)
            {
                _debugLogTimer += Time.deltaTime;
                if (_debugLogTimer >= debugLogInterval)
                {
                    _debugLogTimer = 0f; // Reset timer
                }
            }
            
            UpdateState();
            
            switch (_currentState)
            {
                case State.Emerging:
                    // Scale handled in coroutine
                    break;
                
                case State.Flying:
                    UpdateFlying();
                    CheckForLandingTargets();
                    break;
                
                case State.Landing:
                    UpdateLanding();
                    break;
                
                case State.Dissipating:
                    // Handled in coroutine
                    break;
            }
            
            // Update visual parameters based on age
            UpdateVisualsFromAge();
        }
        
        private void UpdateState()
        {
            // Check if lifetime expired - if in flight, start seeking final landing
            if (_actualLifetime > 0 && _age >= _actualLifetime && _currentState == State.Flying && !_seekingFinalLanding)
            {
                if (enableDebugLogs)
                    Debug.Log($"[Butterfly] {gameObject.name}: Lifetime expired in UpdateState - must land before dying (age={_age:F1}s, lifetime={_actualLifetime:F1}s)");
                _seekingFinalLanding = true;
            }
            
            // If near death (within landing seek threshold), prioritize landing
            if (_actualLifetime > 0 && _age >= (_actualLifetime - landingSeekTimeThreshold) && _currentState == State.Flying)
            {
                _seekingFinalLanding = true;
            }
        }
        
        private void UpdateFlying()
        {
            if (_archetype == null)
            {
                if (enableDebugLogs)
                    Debug.LogError($"[Butterfly] {gameObject.name}: Cannot update flying - archetype is null!");
                return;
            }
            
            float t = Time.time;
            float speed = _archetype.flightSpeedCurve.Evaluate(_normalizedAge);
            
            // Calculate individual flight path
            // If seeking new territory, override with territory-seeking direction
            Vector3 individualDir;
            if (_seekingNewTerritory && enableTerritoryExploration)
            {
                // Seek new territory - move directly toward target
                Vector3 toTarget = (_newTerritoryTarget - transform.position);
                float distanceToTarget = toTarget.magnitude;
                
                if (distanceToTarget < territoryReachedDistance)
                {
                    // Reached new territory - reset territory tracking
                    _currentTerritoryPosition = transform.position;
                    _timeInCurrentTerritory = 0f;
                    _seekingNewTerritory = false;
                    _newTerritoryTarget = Vector3.zero;
                    
                    if (enableDebugLogs)
                        Debug.Log($"[Butterfly] {gameObject.name}: Reached new territory! Position: {transform.position}");
                    
                    // Fall back to normal flight path
                    individualDir = CalculateIndividualFlightPath(t, speed);
                }
                else
                {
                    // Still seeking - move toward target with some noise for natural movement
                    individualDir = toTarget.normalized;
                    
                    // Add slight noise to make movement more natural (not perfectly straight)
                    Vector3 noiseOffset = CalculateIndividualFlightPath(t, speed) * 0.3f;
                    individualDir = (individualDir + noiseOffset).normalized;
                    
                    // Increase speed when seeking new territory
                    speed *= territorySeekSpeed;
                }
            }
            else
            {
                // Normal flight path (Perlin noise-based wandering)
                individualDir = CalculateIndividualFlightPath(t, speed);
            }
            
            if (enableDebugLogs && _debugLogTimer < debugLogInterval) // Log only occasionally
            {
                if (individualDir.sqrMagnitude < 0.01f)
                {
                    Debug.LogWarning($"[Butterfly] {gameObject.name}: Individual direction is zero!");
                    _debugLogTimer = debugLogInterval; // Prevent multiple logs this frame
                }
            }
            
            // Check for flocking behavior
            Vector3 flockDir = Vector3.zero;
            bool shouldFlock = false;
            bool hasValidFlockDir = false;
            
            // Update territory tracking
            if (enableTerritoryExploration && _currentState == State.Flying)
            {
                UpdateTerritoryTracking();
            }
            
            if (enableFlocking && _currentState == State.Flying)
            {
                // Cannot flock while seeking new territory
                bool canFlock = (!enableFlockingCooldown || Time.time >= _flockingCooldownEndTime) 
                                && !_seekingNewTerritory;
                
                if (canFlock)
                {
                    shouldFlock = CheckForFlocking();
                    
                    // Check for breaking out of flock first
                    if (_isInFlock)
                    {
                        CheckForBreakOut();
                        
                        // Re-check if we should still flock after breakout check
                        shouldFlock = CheckForFlocking() && _isInFlock;
                    }
                    
                    if (shouldFlock || _isInFlock)
                    {
                        flockDir = CalculateFlockingDirection();
                        
                        // Only use flock direction if it's valid (not zero)
                        if (flockDir.sqrMagnitude > 0.01f)
                        {
                            hasValidFlockDir = true;
                            
                            // Blend into flock
                            if (!_isInFlock)
                            {
                                _isInFlock = true;
                                _flockStartTime = Time.time;
                                if (enableDebugLogs)
                                    Debug.Log($"[Butterfly] {gameObject.name}: JOINED FLOCK (nearby: {_nearbyButterflies.Count})");
                            }
                            
                            _flockBlendFactor = Mathf.Lerp(_flockBlendFactor, 1f, Time.deltaTime * flockBlendSpeed);
                        }
                        else
                        {
                            // Invalid flock direction, break out
                            hasValidFlockDir = false;
                            if (_isInFlock)
                            {
                                ExitFlock("invalid direction");
                            }
                            _flockBlendFactor = Mathf.Lerp(_flockBlendFactor, 0f, Time.deltaTime * flockBlendSpeed * 2f);
                        }
                    }
                    else
                    {
                        // Blend out of flock
                        hasValidFlockDir = false;
                        _flockBlendFactor = Mathf.Lerp(_flockBlendFactor, 0f, Time.deltaTime * flockBlendSpeed * 2f);
                        
                        if (_flockBlendFactor < 0.1f && _isInFlock)
                        {
                            ExitFlock("no nearby butterflies");
                        }
                    }
                }
                else
                {
                    // In cooldown period - blend out if currently in flock (shouldn't happen, but safety check)
                    if (_isInFlock)
                    {
                        ExitFlock("cooldown active");
                    }
                    hasValidFlockDir = false;
                    _flockBlendFactor = Mathf.Lerp(_flockBlendFactor, 0f, Time.deltaTime * flockBlendSpeed * 2f);
                }
            }
            else
            {
                // Flocking disabled or not flying, ensure we're not in flock
                if (_isInFlock)
                {
                    ExitFlock("flocking disabled or not flying");
                }
                _flockBlendFactor = 0f;
            }
            
            // Blend individual and flock directions
            // If no valid flock direction, always use individual
            Vector3 finalDir;
            if (hasValidFlockDir && _flockBlendFactor > 0.01f)
            {
                finalDir = Vector3.Lerp(individualDir, flockDir, _flockBlendFactor);
            }
            else
            {
                // Use individual path if no valid flock or blend factor is low
                finalDir = individualDir;
                _flockBlendFactor = 0f;
            }
            
            // Constrain to flight radius
            Vector3 toFocal = transform.position - _focalPoint;
            float distance = toFocal.magnitude;
            if (distance > _archetype.maxFlightRadius)
            {
                Vector3 pullBack = -toFocal.normalized * (distance - _archetype.maxFlightRadius) * 0.5f;
                finalDir += pullBack;
                if (enableDebugLogs && _debugLogTimer < debugLogInterval)
                {
                    Debug.Log($"[Butterfly] {gameObject.name}: Pulled back (distance={distance:F2} > max={_archetype.maxFlightRadius:F2})");
                    _debugLogTimer = debugLogInterval; // Prevent multiple logs this frame
                }
            }
            else if (distance < _archetype.minFlightRadius)
            {
                Vector3 pushOut = toFocal.normalized * (_archetype.minFlightRadius - distance) * 0.5f;
                finalDir += pushOut;
                if (enableDebugLogs && _debugLogTimer < debugLogInterval)
                {
                    Debug.Log($"[Butterfly] {gameObject.name}: Pushed out (distance={distance:F2} < min={_archetype.minFlightRadius:F2})");
                    _debugLogTimer = debugLogInterval; // Prevent multiple logs this frame
                }
            }
            
            // Constrain to bounding box with surface avoidance
            if (ButterflyManager.Instance != null && ButterflyManager.Instance.UseBoundingBox)
            {
                Vector3 position = transform.position;
                float steerStrength;
                Vector3 boundarySteer = ButterflyManager.Instance.GetBoundarySteerDirection(position, out steerStrength);
                
                if (boundarySteer.sqrMagnitude > 0.01f)
                {
                    // Butterfly is outside or near boundary - steer away from it
                    finalDir += boundarySteer * steerStrength;
                    
                    // Check if velocity is parallel to a wall/floor/ceiling (dragging along surface)
                    if (_velocity.sqrMagnitude > 0.01f)
                    {
                        Vector3 velNormalized = _velocity.normalized;
                        float alignment = Vector3.Dot(velNormalized, boundarySteer);
                        
                        // If velocity is perpendicular to the steering direction (moving along surface),
                        // add extra steering perpendicular to the surface
                        if (Mathf.Abs(alignment) < 0.3f && steerStrength > ButterflyManager.Instance.BoundarySteerStrength * 1.5f)
                        {
                            // Butterfly is dragging along a surface - add perpendicular push
                            Vector3 perpendicularPush = boundarySteer * 2f;
                            
                            // If near ground, ensure upward component
                            float distToGround = position.y - ButterflyManager.Instance.BoundingBoxMin.y;
                            if (distToGround < ButterflyManager.Instance.BoundaryBufferZone * 1.5f && boundarySteer.y > 0.1f)
                            {
                                perpendicularPush.y += ButterflyManager.Instance.GroundUpwardBias;
                            }
                            
                            finalDir += perpendicularPush;
                            
                            if (enableDebugLogs && _debugLogTimer < debugLogInterval)
                            {
                                Debug.Log($"[Butterfly] {gameObject.name}: Dragging along surface - applying perpendicular push (pos={position:F2}, vel={velNormalized:F2}, boundary={boundarySteer:F2})");
                                _debugLogTimer = debugLogInterval; // Prevent multiple logs this frame
                            }
                        }
                    }
                    
                    if (enableDebugLogs && _debugLogTimer < debugLogInterval)
                    {
                        Debug.Log($"[Butterfly] {gameObject.name}: Steering from boundary (pos={position:F2}, steer={boundarySteer:F2}, strength={steerStrength:F2})");
                        _debugLogTimer = debugLogInterval; // Prevent multiple logs this frame
                    }
                }
            }
            
            // Ensure we always have a valid direction (fallback to current velocity if somehow zero)
            if (finalDir.sqrMagnitude < 0.01f)
            {
                if (enableDebugLogs)
                    Debug.LogWarning($"[Butterfly] {gameObject.name}: Final direction is zero! Using fallback. " +
                                   $"Individual={individualDir:F3}, Flock={flockDir:F3}, Blend={_flockBlendFactor:F2}");
                finalDir = _velocity.normalized;
                if (finalDir.sqrMagnitude < 0.01f)
                {
                    finalDir = transform.forward;
                    if (finalDir.sqrMagnitude < 0.01f)
                        finalDir = Vector3.forward; // Last resort
                }
            }
            
            // Update velocity
            Vector3 targetDir = finalDir.normalized;
            _velocity = Vector3.Lerp(_velocity, targetDir * speed, Time.deltaTime * _archetype.turnSpeed);
            
            // Check for zero velocity after update
            if (_velocity.sqrMagnitude < 0.001f)
            {
                if (enableDebugLogs && _stuckTimer > 0.5f)
                    Debug.LogWarning($"[Butterfly] {gameObject.name}: Velocity is zero! Speed={speed:F3}, TargetDir={targetDir:F3}, TurnSpeed={_archetype.turnSpeed:F2}");
                _velocity = targetDir * speed * 0.1f; // Force some movement
            }
            
            // Apply movement
            Vector3 newPosition = transform.position + _velocity * Time.deltaTime;
            
            // Clamp to bounding box if enabled
            if (ButterflyManager.Instance != null && ButterflyManager.Instance.UseBoundingBox)
            {
                newPosition = ButterflyManager.Instance.ClampToBounds(newPosition);
            }
            
            transform.position = newPosition;
            
            // Update rotation to face movement direction
            if (_velocity.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(_velocity);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * _archetype.turnSpeed);
            }
        }
        
        /// <summary>
        /// Calculate individual flight path using Perlin noise.
        /// </summary>
        private Vector3 CalculateIndividualFlightPath(float t, float speed)
        {
            if (_archetype == null)
            {
                if (enableDebugLogs && _debugLogTimer < debugLogInterval)
                {
                    Debug.LogWarning($"[Butterfly] {gameObject.name}: Cannot calculate flight path - archetype is null!");
                    _debugLogTimer = debugLogInterval; // Prevent multiple logs this frame
                }
                return Vector3.forward; // Fallback
            }
            
            Vector3 noiseDir = new Vector3(
                Mathf.PerlinNoise(t * _archetype.noiseScale + _noiseOffset.x, _noiseOffset.y) - 0.5f,
                Mathf.PerlinNoise(_noiseOffset.z, t * _archetype.noiseScale + _noiseOffset.x) - 0.5f,
                Mathf.PerlinNoise(t * _archetype.noiseScale + _noiseOffset.y, _noiseOffset.z) - 0.5f
            );
            
            // Audio-linked vertical oscillation
            float audioFrequency = ButterflyAudio.GetCurrentFrequency(this);
            float bob = Mathf.Sin(t * audioFrequency * 0.01f) * 0.01f;
            noiseDir.y += bob;
            
            if (noiseDir.sqrMagnitude < 0.01f)
            {
                if (enableDebugLogs && _debugLogTimer < debugLogInterval)
                {
                    Debug.LogWarning($"[Butterfly] {gameObject.name}: Calculated noiseDir is zero! Returning forward.");
                    _debugLogTimer = debugLogInterval; // Prevent multiple logs this frame
                }
                return Vector3.forward; // Fallback
            }
            
            return noiseDir.normalized;
        }
        
        /// <summary>
        /// Update territory tracking and trigger exploration if stuck too long.
        /// </summary>
        private void UpdateTerritoryTracking()
        {
            _territoryCheckTimer += Time.deltaTime;
            
            // Only check territory every TERRITORY_CHECK_INTERVAL seconds
            if (_territoryCheckTimer < TERRITORY_CHECK_INTERVAL)
                return;
            
            _territoryCheckTimer = 0f;
            
            // Check if still in current territory
            float distanceToTerritory = Vector3.Distance(transform.position, _currentTerritoryPosition);
            
            if (distanceToTerritory <= territoryCheckRadius)
            {
                // Still in same territory - increment timer
                _timeInCurrentTerritory += TERRITORY_CHECK_INTERVAL;
                
                // Check if we've been here too long
                if (_timeInCurrentTerritory >= maxTimeInTerritory && !_seekingNewTerritory)
                {
                    // Start seeking new territory
                    _seekingNewTerritory = true;
                    PickNewTerritoryTarget();
                    
                    // Exit flock if in one (can't flock while seeking new territory)
                    if (_isInFlock)
                    {
                        ExitFlock("seeking new territory");
                    }
                    
                    if (enableDebugLogs)
                        Debug.Log($"[Butterfly] {gameObject.name}: Stuck in territory for {_timeInCurrentTerritory:F1}s. Seeking new territory at {_newTerritoryTarget}");
                }
            }
            else
            {
                // Moved to different area - reset territory tracking
                _currentTerritoryPosition = transform.position;
                _timeInCurrentTerritory = 0f;
                
                // If we were seeking new territory and have moved far enough, we've reached it
                if (_seekingNewTerritory && distanceToTerritory >= territoryReachedDistance)
                {
                    _seekingNewTerritory = false;
                    _newTerritoryTarget = Vector3.zero;
                    
                    if (enableDebugLogs)
                        Debug.Log($"[Butterfly] {gameObject.name}: Reached new territory! Distance traveled: {distanceToTerritory:F1}m");
                }
            }
        }
        
        /// <summary>
        /// Pick a new distant territory target to seek.
        /// </summary>
        private void PickNewTerritoryTarget()
        {
            if (ButterflyManager.Instance == null || !ButterflyManager.Instance.UseBoundingBox)
            {
                // No bounding box - pick random point in a sphere around current position
                _newTerritoryTarget = transform.position + Random.onUnitSphere * newTerritoryMinDistance;
                return;
            }
            
            // Pick a random point within the bounding box, but ensure it's far enough away
            Vector3 boxMin = ButterflyManager.Instance.BoundingBoxMin;
            Vector3 boxMax = ButterflyManager.Instance.BoundingBoxMax;
            
            Vector3 candidateTarget;
            int attempts = 0;
            const int maxAttempts = 20;
            
            do
            {
                candidateTarget = new Vector3(
                    Random.Range(boxMin.x, boxMax.x),
                    Random.Range(boxMin.y, boxMax.y),
                    Random.Range(boxMin.z, boxMax.z)
                );
                attempts++;
            }
            while (Vector3.Distance(candidateTarget, transform.position) < newTerritoryMinDistance && attempts < maxAttempts);
            
            // If we couldn't find a point far enough away, use a point on the sphere
            if (attempts >= maxAttempts)
            {
                Vector3 direction = Random.onUnitSphere;
                if (direction.sqrMagnitude < 0.01f)
                    direction = Vector3.forward;
                
                candidateTarget = transform.position + direction * newTerritoryMinDistance;
                
                // Clamp to bounding box
                candidateTarget = ButterflyManager.Instance.ClampToBounds(candidateTarget);
            }
            
            _newTerritoryTarget = candidateTarget;
            
            if (enableDebugLogs)
                Debug.Log($"[Butterfly] {gameObject.name}: Picked new territory target at {_newTerritoryTarget}, distance: {Vector3.Distance(transform.position, _newTerritoryTarget):F1}m");
        }
        
        /// <summary>
        /// Check if there are nearby butterflies to form a flock with.
        /// </summary>
        private bool CheckForFlocking()
        {
            if (ButterflyManager.Instance == null)
            {
                if (enableDebugLogs && _debugLogTimer < debugLogInterval)
                {
                    Debug.LogWarning($"[Butterfly] {gameObject.name}: Cannot check for flocking - ButterflyManager.Instance is null!");
                    _debugLogTimer = debugLogInterval; // Prevent multiple logs this frame
                }
                return false;
            }
            
            _nearbyButterflies.Clear();
            
            // Find nearby butterflies
            var allButterflies = ButterflyManager.Instance.GetActiveButterflies();
            if (allButterflies == null)
            {
                if (enableDebugLogs && _debugLogTimer < debugLogInterval)
                {
                    Debug.LogWarning($"[Butterfly] {gameObject.name}: GetActiveButterflies() returned null!");
                    _debugLogTimer = debugLogInterval; // Prevent multiple logs this frame
                }
                return false;
            }
            
            foreach (var other in allButterflies)
            {
                if (other == this || other == null)
                    continue;
                
                // Only consider butterflies that are flying (not emerging, landing, or dissipating)
                if (other._currentState != State.Flying)
                    continue;
                
                float distance = Vector3.Distance(transform.position, other.transform.position);
                if (distance <= flockDetectionRadius)
                {
                    _nearbyButterflies.Add(other);
                }
            }
            
            if (enableDebugLogs && _nearbyButterflies.Count > 0 && _debugLogTimer < debugLogInterval)
            {
                Debug.Log($"[Butterfly] {gameObject.name}: Found {_nearbyButterflies.Count} nearby butterflies within {flockDetectionRadius}m (need {minButterfliesForFlock} to flock)");
                _debugLogTimer = debugLogInterval; // Prevent multiple logs this frame
            }
            
            // Need at least minButterfliesForFlock nearby butterflies to form a flock
            return _nearbyButterflies.Count >= minButterfliesForFlock;
        }
        
        /// <summary>
        /// Calculate flocking direction using cohesion, alignment, and separation.
        /// </summary>
        private Vector3 CalculateFlockingDirection()
        {
            if (_nearbyButterflies.Count == 0)
                return Vector3.zero;
            
            Vector3 cohesion = Vector3.zero;
            Vector3 alignment = Vector3.zero;
            Vector3 separation = Vector3.zero;
            int separationCount = 0;
            
            foreach (var neighbor in _nearbyButterflies)
            {
                if (neighbor == null || neighbor == this) continue;
                
                Vector3 toNeighbor = neighbor.transform.position - transform.position;
                float distance = toNeighbor.magnitude;
                
                // Cohesion: Move towards center of nearby butterflies
                cohesion += neighbor.transform.position;
                
                // Alignment: Align with velocity of nearby butterflies
                alignment += neighbor._velocity;
                
                // Separation: Avoid getting too close to neighbors
                if (distance < flockSeparationDistance && distance > 0.01f)
                {
                    separation -= toNeighbor.normalized / distance;
                    separationCount++;
                }
            }
            
            // Average cohesion
            if (_nearbyButterflies.Count > 0)
            {
                cohesion = (cohesion / _nearbyButterflies.Count) - transform.position;
                cohesion = cohesion.normalized;
            }
            
            // Average alignment
            if (_nearbyButterflies.Count > 0)
            {
                alignment = alignment.normalized;
            }
            
            // Average separation
            if (separationCount > 0)
            {
                separation = separation.normalized;
            }
            
            // Combine flocking forces
            Vector3 flockDirection = (cohesion * flockCohesionWeight + 
                                     alignment * flockAlignmentWeight + 
                                     separation * flockSeparationWeight).normalized;
            
            return flockDirection;
        }
        
        /// <summary>
        /// Exit flock and set cooldown timer to prevent immediate re-entry.
        /// </summary>
        private void ExitFlock(string reason)
        {
            if (!_isInFlock) return; // Already out of flock
            
            _isInFlock = false;
            _flockStartTime = 0f;
            _flockBlendFactor = Mathf.Max(0f, _flockBlendFactor * 0.5f); // Quick drop
            
            // Set cooldown timer to prevent immediate re-entry
            if (enableFlockingCooldown)
            {
                float cooldownDuration = Random.Range(minFlockingCooldown, maxFlockingCooldown);
                _flockingCooldownEndTime = Time.time + cooldownDuration;
                
                if (enableDebugLogs)
                    Debug.Log($"[Butterfly] {gameObject.name}: LEFT FLOCK ({reason}). Cooldown: {cooldownDuration:F1}s (ends at {_flockingCooldownEndTime:F1})");
            }
            else
            {
                if (enableDebugLogs)
                    Debug.Log($"[Butterfly] {gameObject.name}: LEFT FLOCK ({reason})");
            }
        }
        
        /// <summary>
        /// Check if butterfly should break out of the flock.
        /// </summary>
        private void CheckForBreakOut()
        {
            if (!_isInFlock) return;
            
            _breakOutCheckTimer += Time.deltaTime;
            
            // Check distance immediately (every frame)
            bool distanceBreakOut = _nearbyButterflies.Count == 0;
            
            if (distanceBreakOut)
            {
                // Immediately break out if no nearby butterflies
                ExitFlock("distance - no nearby");
                _breakOutCheckTimer = 0f;
                return;
            }
            
            // Periodic checks for other breakout conditions
            if (_breakOutCheckTimer >= breakOutCheckInterval)
            {
                _breakOutCheckTimer = 0f;
                
                // Break out conditions:
                // 1. Random chance
                bool randomBreakOut = Random.value < breakOutChance;
                
                // 2. Too long in flock
                float timeInFlock = Time.time - _flockStartTime;
                bool timeBreakOut = timeInFlock > maxFlockTime;
                
                if (randomBreakOut || timeBreakOut)
                {
                    // Break out of flock
                    string reason = $"random={randomBreakOut}, time={timeBreakOut}, timeInFlock={timeInFlock:F1}s";
                    ExitFlock(reason);
                }
            }
        }
        
        private void CheckForLandingTargets()
        {
            _landingTimer += Time.deltaTime;
            if (_landingTimer < landingCheckInterval) return;
            
            _landingTimer = 0f;
            
            // Only check if we're not already landing and it's time to land
            if (_currentLandingTarget != null) return;
            
            // Check if we're in landing cooldown period (ignore if seeking final landing)
            if (!_seekingFinalLanding && Time.time < _landingCooldownEndTime) return;
            
            // Check if butterfly needs energy (prioritize fruits when low energy)
            bool needsEnergy = energySystem != null && energySystem.NeedsEnergy;
            bool isCarryingPollen = pollinationSystem != null && pollinationSystem.IsCarryingPollen;
            
            // Determine seek chance based on state
            float seekChance = 0.3f;
            if (needsEnergy) seekChance = 0.7f; // Higher chance if low energy
            if (isCarryingPollen) seekChance = 0.6f; // Higher chance if carrying pollen
            if (_seekingFinalLanding) seekChance = 1.0f; // Always seek if must land before dying
            
            // Random chance to seek landing target (unless seeking final landing)
            if (!_seekingFinalLanding && Random.value > seekChance) return;
            
            Collider[] nearbyTargets = Physics.OverlapSphere(transform.position, landingRadius, landingTargetLayer);
            
            // Prioritize targets based on state
            // - Seeking final landing: prefer flowers, fruits, or plants (must land before dying)
            // - Low energy: prefer flowers or fruits
            // - Carrying pollen: prefer fruit (to deposit) or flowers (to pollinate)
            // - Otherwise: prefer fruits, then flowers, then other targets
            Interaction.LandingTarget fruitTarget = null;
            Interaction.LandingTarget flowerTarget = null;
            Interaction.LandingTarget plantTarget = null;
            Interaction.LandingTarget otherTarget = null;
            
            foreach (var target in nearbyTargets)
            {
                var landingTarget = target.GetComponent<Interaction.LandingTarget>();
                if (landingTarget == null || !landingTarget.IsAvailable) continue;
                
                // Skip if this is the last place we landed (unless we've landed somewhere else since)
                if (landingTarget == _lastLandingTarget) continue;
                
                // Check target type
                if (landingTarget.Type == Interaction.LandingTarget.TargetType.Fruit)
                {
                    fruitTarget = landingTarget;
                    // If seeking final landing, prioritize fruits (can extend life by feeding)
                    if (_seekingFinalLanding) break;
                    // Don't break - continue to check for flowers if we're carrying pollen
                    if (!isCarryingPollen) break; // Otherwise prioritize fruits
                }
                else if (landingTarget.Type == Interaction.LandingTarget.TargetType.Plant)
                {
                    // Check if it's actually a flower
                    Flower flower = target.GetComponent<Flower>();
                    if (flower == null && target.transform.parent != null)
                        flower = target.transform.parent.GetComponent<Flower>();
                    
                    if (flower != null)
                    {
                        flowerTarget = landingTarget;
                        // If seeking final landing and carrying pollen, prioritize flowers (can extend life by pollinating)
                        if (_seekingFinalLanding && isCarryingPollen) break;
                        // Prefer flowers when low energy or not carrying pollen
                        if (needsEnergy || !isCarryingPollen) break;
                    }
                    else
                    {
                        // Regular plant (not a flower) - acceptable for final landing
                        if (_seekingFinalLanding)
                        {
                            plantTarget = landingTarget;
                        }
                        else if (otherTarget == null)
                        {
                            otherTarget = landingTarget;
                        }
                    }
                }
                else if (otherTarget == null)
                {
                    otherTarget = landingTarget;
                }
            }
            
            // Choose target based on state
            Interaction.LandingTarget chosenTarget = null;
            
            if (_seekingFinalLanding)
            {
                // When seeking final landing, prioritize targets that can extend life or at least allow death
                if (isCarryingPollen && flowerTarget != null)
                {
                    chosenTarget = flowerTarget; // Pollinate to extend life
                }
                else if (fruitTarget != null)
                {
                    chosenTarget = fruitTarget; // Feed to extend life
                }
                else if (flowerTarget != null)
                {
                    chosenTarget = flowerTarget; // Acceptable landing spot
                }
                else if (plantTarget != null)
                {
                    chosenTarget = plantTarget; // Acceptable landing spot (not ideal, but allows death)
                }
            }
            else if (needsEnergy)
            {
                // Low energy: prefer flowers (collect pollen + energy) or fruits
                chosenTarget = flowerTarget ?? fruitTarget;
            }
            else if (isCarryingPollen)
            {
                // Carrying pollen: prefer fruit (deposit pollen) or flowers (pollinate)
                chosenTarget = fruitTarget ?? flowerTarget;
            }
            else
            {
                // Normal state: prefer fruit, then flower, then other
                chosenTarget = fruitTarget ?? flowerTarget ?? otherTarget;
            }
            
            if (chosenTarget != null)
            {
                AttemptLanding(chosenTarget);
            }
        }
        
        private void AttemptLanding(Interaction.LandingTarget target)
        {
            _currentLandingTarget = target;
            _currentLandingTarget.Reserve(this);
            _currentState = State.Landing;
            
            // Set random landing duration
            _landingDuration = Random.Range(minLandingDuration, maxLandingDuration);
            _landingStartTime = Time.time;
            _hasExtendedLifetimeThisLanding = false; // Reset extension tracking for new landing
            
            // Calculate landing offset
            Vector3 toButterfly = transform.position - target.transform.position;
            _landingOffset = target.transform.InverseTransformVector(toButterfly);
            _landingOffset = Vector3.ClampMagnitude(_landingOffset, 0.3f);
            
            // Note: Fruit and flower notifications happen in UpdateLanding() after landing completes
            // to handle energy feeding, pollen collection/deposition correctly
        }
        
        private void UpdateLanding()
        {
            if (_currentLandingTarget == null)
            {
                _currentState = State.Flying;
                return;
            }
            
            Vector3 targetPos = _currentLandingTarget.transform.position + _currentLandingTarget.transform.TransformVector(_landingOffset);
            Vector3 toTarget = targetPos - transform.position;
            
            // Smoothly move to landing position
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 2f);
            
            // Orient towards landing target
            if (toTarget.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(toTarget);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 3f);
            }
            
            // Check if landed (close enough)
            if (toTarget.sqrMagnitude < 0.01f)
            {
                // Reduce audio while landed
                if (audioController != null)
                {
                    audioController.SetIntensity(0.3f);
                }
                
                // Check if landing on fruit and feed energy / deposit pollen
                Plants.GenerativeFruit fruit = null;
                if (_currentLandingTarget.Type == Interaction.LandingTarget.TargetType.Fruit)
                {
                    fruit = _currentLandingTarget.GetComponent<Plants.GenerativeFruit>();
                    if (fruit == null && _currentLandingTarget.transform.parent != null)
                    {
                        fruit = _currentLandingTarget.transform.parent.GetComponent<Plants.GenerativeFruit>();
                    }
                    
                    if (fruit != null)
                    {
                        // Feed energy from fruit
                        if (energySystem != null)
                        {
                            energySystem.FeedFromFruit(fruit);
                            fruit.OnButterflyFeeding(this);
                            
                            // Extend lifetime when feeding from fruit (once per landing)
                            if (_actualLifetime > 0 && !_hasExtendedLifetimeThisLanding)
                            {
                                _fruitFeedingCount++;
                                _actualLifetime += fruitFeedingLifetimeBonus;
                                _seekingFinalLanding = false; // Reset seeking flag since we're extending life
                                _hasExtendedLifetimeThisLanding = true; // Mark that we've extended this landing
                                if (enableDebugLogs)
                                    Debug.Log($"[Butterfly] {gameObject.name}: Fed from fruit - extended lifetime by {fruitFeedingLifetimeBonus:F1}s (new lifetime: {_actualLifetime:F1}s, feed count: {_fruitFeedingCount})");
                            }
                        }
                        
                        // Deposit pollen to fruit (accelerates fruit evolution)
                        if (pollinationSystem != null && pollinationSystem.IsCarryingPollen)
                        {
                            pollinationSystem.DepositPollenToFruit(fruit);
                        }
                    }
                }
                
                // Check if landing on flower and feed energy / collect pollen / deposit pollen
                Flower flower = null;
                if (_currentLandingTarget.Type == Interaction.LandingTarget.TargetType.Plant)
                {
                    flower = _currentLandingTarget.GetComponent<Flower>();
                    if (flower == null && _currentLandingTarget.transform.parent != null)
                    {
                        flower = _currentLandingTarget.transform.parent.GetComponent<Flower>();
                    }
                    
                    if (flower != null)
                    {
                        // On landing, butterfly feeds and collects pollen OR deposits pollen
                        if (pollinationSystem != null && pollinationSystem.IsCarryingPollen)
                        {
                            // Deposit pollen (cross-pollination)
                            pollinationSystem.DepositPollen(flower);
                            
                            // Extend lifetime when pollinating flowers (once per landing)
                            if (_actualLifetime > 0 && !_hasExtendedLifetimeThisLanding)
                            {
                                _pollinationCount++;
                                _actualLifetime += flowerPollinationLifetimeBonus;
                                _seekingFinalLanding = false; // Reset seeking flag since we're extending life
                                _hasExtendedLifetimeThisLanding = true; // Mark that we've extended this landing
                                if (enableDebugLogs)
                                    Debug.Log($"[Butterfly] {gameObject.name}: Pollinated flower - extended lifetime by {flowerPollinationLifetimeBonus:F1}s (new lifetime: {_actualLifetime:F1}s, pollination count: {_pollinationCount})");
                            }
                        }
                        else
                        {
                            // Feed and collect pollen
                            flower.OnButterflyLanded(this);
                        }
                    }
                }
                
                // Check if landing duration has elapsed
                float landingTimeElapsed = Time.time - _landingStartTime;
                if (landingTimeElapsed >= _landingDuration)
                {
                    // If we were seeking final landing and lifetime has expired, start dissipation
                    if (_seekingFinalLanding && _actualLifetime > 0 && _age >= _actualLifetime)
                    {
                        if (enableDebugLogs)
                            Debug.Log($"[Butterfly] {gameObject.name}: Final landing complete - beginning dissipation (age={_age:F1}s, lifetime={_actualLifetime:F1}s)");
                        _currentState = State.Dissipating;
                        StartCoroutine(DissipatingCoroutine());
                        return;
                    }
                    
                    // Landing duration complete, take off
                    TakeOff();
                }
            }
        }
        
        public void TakeOff()
        {
            Interaction.LandingTarget targetToRelease = _currentLandingTarget;
            
            if (_currentLandingTarget != null)
            {
                // Check if landing on player hand
                HandProxy handProxy = _currentLandingTarget.GetComponent<HandProxy>();
                if (handProxy == null && _currentLandingTarget.transform.parent != null)
                {
                    handProxy = _currentLandingTarget.transform.parent.GetComponent<HandProxy>();
                }
                
                if (handProxy != null)
                {
                    // Notify ecosystem manager that butterfly landed on player
                    // Notify ecosystem orchestrator
                    if (Core.EcosystemOrchestrator.Instance != null)
                    {
                        Core.EcosystemOrchestrator.Instance.RegisterButterflyLandingOnHand(this);
                    }
                    
                    // Also notify ecosystem state controller for compatibility
                    if (Core.EcosystemStateController.Instance != null)
                    {
                        Core.EcosystemStateController.Instance.OnButterflyLandOnPlayer();
                    }
                }
                
                // Store this as the last landing target
                _lastLandingTarget = _currentLandingTarget;
                
                // Notify fruit if it's a fruit target
                Plants.GenerativeFruit fruit = _currentLandingTarget.GetComponent<Plants.GenerativeFruit>();
                if (fruit == null && _currentLandingTarget.transform.parent != null)
                {
                    fruit = _currentLandingTarget.transform.parent.GetComponent<Plants.GenerativeFruit>();
                }
                if (fruit != null)
                {
                    fruit.OnButterflyLeft(this);
                }
                
                // Check if landing on plant
                Plants.GenerativePlant plant = _currentLandingTarget.GetComponent<Plants.GenerativePlant>();
                if (plant == null && _currentLandingTarget.transform.parent != null)
                {
                    plant = _currentLandingTarget.transform.parent.GetComponent<Plants.GenerativePlant>();
                }
                if (plant != null)
                {
                    // Notify ecosystem manager of plant interaction
                    if (Core.EcosystemStateController.Instance != null)
                    {
                        Core.EcosystemStateController.Instance.OnButterflyPlantInteraction();
                    }
                    
                    // Notify plant growth system of butterfly visit
                    Plants.PlantGrowthSystem growthSystem = plant.GetComponent<Plants.PlantGrowthSystem>();
                    if (growthSystem == null && plant.transform.parent != null)
                    {
                        growthSystem = plant.transform.parent.GetComponent<Plants.PlantGrowthSystem>();
                    }
                    if (growthSystem != null)
                    {
                        growthSystem.OnButterflyVisit();
                    }
                }
                
                _currentLandingTarget.Release();
                _currentLandingTarget = null;
            }
            
            _currentState = State.Flying;
            
            // Set random landing cooldown - won't try to land again for this duration
            float cooldownDuration = Random.Range(minLandingCooldown, maxLandingCooldown);
            _landingCooldownEndTime = Time.time + cooldownDuration;
            
            // Reset audio
            if (audioController != null)
            {
                audioController.SetIntensity(1f);
            }
            
            // Give a little boost
            _velocity = Random.insideUnitSphere.normalized * _archetype.flightSpeedCurve.Evaluate(_normalizedAge);
        }
        
        private void UpdateVisualsFromAge()
        {
            if (visualController == null || _archetype == null) return;
            
            // Update color based on gradient
            Color currentColor = _archetype.wingColorGradient.Evaluate(_normalizedAge);
            visualController.SetColor(currentColor);
            
            // Update flap frequency
            float flapFreq = _archetype.flapFrequencyCurve.Evaluate(_normalizedAge);
            visualController.SetFlapFrequency(flapFreq);
            
            // Update wave parameters based on audio
            if (audioController != null)
            {
                float audioIntensity = audioController.GetCurrentIntensity();
                visualController.SetWaveParams(audioIntensity * 0.1f, audioIntensity * 5f);
                visualController.SetEmission(audioIntensity * 0.5f);
            }
        }
        
        public void ForceDissipate()
        {
            if (_currentState == State.Dissipating) return;
            
            if (_currentLandingTarget != null)
            {
                _currentLandingTarget.Release();
                _currentLandingTarget = null;
            }
            
            _currentState = State.Dissipating;
            StopAllCoroutines();
            StartCoroutine(DissipatingCoroutine());
        }
        
        /// <summary>
        /// Create a trail material with URP-compatible shader at runtime.
        /// </summary>
        private Material CreateTrailMaterial()
        {
            // Try URP shaders first
            Shader trailShader = Shader.Find("Universal Render Pipeline/Unlit") ??
                                 Shader.Find("Universal Render Pipeline/Simple Lit") ??
                                 Shader.Find("Unlit/Color") ??
                                 Shader.Find("Sprites/Default") ??
                                 Shader.Find("Legacy Shaders/Particles/Alpha Blended");
            
            if (trailShader == null)
            {
                Debug.LogWarning($"Butterfly: Could not find suitable trail shader. Trail may appear magenta.");
                return null;
            }
            
            Material mat = new Material(trailShader);
            mat.name = "TrailMaterial_Runtime";
            
            // Set color to white (trail colors come from vertex colors via startColor/endColor)
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", Color.white);
            }
            else if (mat.HasProperty("_Color"))
            {
                mat.SetColor("_Color", Color.white);
            }
            
            // Enable vertex colors if supported
            if (mat.HasProperty("_VertexColorMode"))
            {
                mat.SetFloat("_VertexColorMode", 1f);
            }
            
            return mat;
        }
        
        private void OnDrawGizmosSelected()
        {
            if (_archetype == null) return;
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(_focalPoint, _archetype.minFlightRadius);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(_focalPoint, _archetype.maxFlightRadius);
            
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, landingRadius);
        }
    }
}

