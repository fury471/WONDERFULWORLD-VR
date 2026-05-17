using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class GrowthSeedZoneDriver : MonoBehaviour
{
    [System.Serializable]
    private class PlantSlot
    {
        public GrowthPlant plant;
        public bool active;
        public bool retiring;
        public float activatedAt;
    }

    private struct SeedRequest
    {
        public Ray ray;
        public Vector3 magicOrigin;
    }

    private struct PendingInput
    {
        public bool active;
        public float startedAt;
        public SeedRequest request;
    }

    [Header("Zone")]
    [SerializeField] private Collider growthZone;
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float rayDistance = 20f;
    [SerializeField] private float forwardSpawnDistance = 3.5f;
    [SerializeField] private float minSpacingBetweenPlants = 0.75f;
    [SerializeField] private bool requireTerrainColliderForNewMushrooms = true;
    [SerializeField] private bool blockWhenPointingAtInteractable = true;
    [SerializeField] private bool allowForwardFallbackForDebug = false;

    [Header("Interaction")]
    [SerializeField] private Transform interactionOrigin;
    [SerializeField] private InputActionProperty leftTrigger;
    [SerializeField] private InputActionProperty rightTrigger;
    [SerializeField] private bool rightTriggerOnly = true;
    [SerializeField] private bool preferRightControllerOrigin = true;
    [SerializeField] private bool enableMouseClickFallback = false;
    [SerializeField] private bool enableKeyboardFallback = false;
    [SerializeField] private Key keyboardSeedKey = Key.G;
    [SerializeField] private float chargedHoldSeconds = 0.65f;

    [Header("Earth Magic")]
    [SerializeField] private bool enableEarthMagicProjectile = true;
    [SerializeField] private float earthMagicFlightSeconds = 1.55f;
    [SerializeField] private float earthMagicArcHeight = 2.1f;
    [SerializeField] private float earthMagicSideCurve = 0.95f;
    [SerializeField] private float earthMagicSecondarySideCurve = -0.46f;
    [SerializeField] private float earthTrailWidth = 0.018f;
    [SerializeField] private float earthTrailVisibleFraction = 0.38f;
    [SerializeField] private int earthTrailSegments = 34;
    [SerializeField] private float earthHaloWidthMultiplier = 2.35f;
    [SerializeField] private float earthSpiralRadius = 0.12f;
    [SerializeField] private float earthSpiralRadiusVariation = 0.08f;
    [SerializeField] private float earthSpiralTurns = 3.15f;
    [SerializeField] private int earthSpiralStrandCount = 3;
    [SerializeField] private float earthStrandWidthMultiplier = 0.42f;
    [SerializeField] private float earthImpactSparkSeconds = 0.34f;
    [SerializeField] private int earthImpactSparkCount = 14;
    [SerializeField] private Color earthCoreColor = new Color(0.42f, 0.23f, 0.08f, 1f);
    [SerializeField] private Color earthTrailColor = new Color(0.2f, 0.12f, 0.045f, 0.88f);

    [Header("Charge Magic")]
    [SerializeField] private bool enableChargeOrb = true;
    [SerializeField] private Texture2D chargeParticleTexture;
    [SerializeField] private float chargeOrbBaseScale = 0.018f;
    [SerializeField] private float chargeOrbMaxScale = 0.052f;
    [SerializeField] private float chargeOrbForwardOffset = 0.66f;
    [SerializeField] private float chargeOrbJitterRadius = 0.035f;
    [SerializeField] private float chargeOrbJitterSpeed = 22f;
    [SerializeField] private int chargeParticleMaxCount = 18;

    [Header("Pool")]
    [SerializeField] private GrowthPlant[] mushroomPool;
    [SerializeField] private int maxActiveMushrooms = 16;
    [SerializeField] private bool expandPoolToMaxActive = true;
    [SerializeField] private Vector3 spawnOffset = Vector3.zero;
    [SerializeField] private bool snapSpawnToGround = true;
    [SerializeField] private float spawnGroundProbeHeight = 12f;
    [SerializeField] private float spawnGroundProbeDepth = 24f;
    [SerializeField] private float spawnGroundClearance = 0.015f;
    [SerializeField] private bool randomizeYaw = true;
    [SerializeField] private bool retireInstantlyWhenReused = true;
    [SerializeField] private float retireReuseDelay = 0.08f;

    [Header("Seed Burst")]
    [SerializeField] private int tapMushroomsPerSeed = 1;
    [SerializeField] private int chargedMinMushroomsPerSeed = 5;
    [SerializeField] private int chargedMaxMushroomsPerSeed = 8;
    [SerializeField] private float chargedBurstRadius = 4f;

    [Header("Variation")]
    [SerializeField] private Vector2 randomScaleRange = new Vector2(0.16f, 1.05f);
    [SerializeField] private Vector2 randomDurationRange = new Vector2(0.85f, 1.2f);
    [SerializeField] private Vector2 randomWobbleRange = new Vector2(0.8f, 1.25f);

    [Header("Existing Mushroom Growth")]
    [SerializeField] private bool allowCultivateExistingMushrooms = false;
    [SerializeField] private float existingMushroomScaleStep = 0.35f;
    [SerializeField] private int chargedExistingMushroomGrowthSteps = 3;
    [SerializeField] private float existingMushroomMaxScale = 2.4f;
    [SerializeField] private float existingMushroomScaleSeconds = 0.45f;

    [Header("Debug")]
    [SerializeField] private bool logDebugMessages;
    [SerializeField] private bool drawDebugRay = true;

    private readonly List<PlantSlot> slots = new();
    private PendingInput pendingInput;
    private Material runtimeEarthMaterial;
    private Material runtimeEarthParticleMaterial;
    private Texture2D runtimeEarthParticleTexture;
    private GameObject chargeOrbRoot;
    private ParticleSystem chargeOrbParticles;

    private void Awake()
    {
        AutoAssignReferences();
        BuildSlots();
        ExpandPoolIfNeeded();
        ResetPoolToSeed();
    }

    private void OnDestroy()
    {
        if (runtimeEarthMaterial != null)
        {
            Destroy(runtimeEarthMaterial);
        }

        if (runtimeEarthParticleMaterial != null)
        {
            Destroy(runtimeEarthParticleMaterial);
        }

        if (runtimeEarthParticleTexture != null)
        {
            Destroy(runtimeEarthParticleTexture);
        }

        DestroyChargeOrb();
    }

    private void Update()
    {
        AutoAssignReferences();
        UpdateInputChargeState();
    }

    private void AutoAssignReferences()
    {
        if (growthZone == null)
        {
            growthZone = GetComponent<Collider>();
        }

        if (interactionOrigin == null && Camera.main != null)
        {
            interactionOrigin = Camera.main.transform;
        }

        if (preferRightControllerOrigin && !IsRightControllerOrigin(interactionOrigin))
        {
            Transform rightOrigin = FindRightControllerOrigin();
            if (rightOrigin != null)
            {
                interactionOrigin = rightOrigin;
            }
        }

        if (mushroomPool == null || mushroomPool.Length == 0)
        {
            mushroomPool = GetComponentsInChildren<GrowthPlant>(includeInactive: true);
        }
    }

    private void BuildSlots()
    {
        slots.Clear();

        if (mushroomPool == null)
        {
            return;
        }

        HashSet<GrowthPlant> seen = new();
        foreach (GrowthPlant plant in mushroomPool)
        {
            if (plant == null || !seen.Add(plant))
            {
                continue;
            }

            slots.Add(new PlantSlot
            {
                plant = plant,
                active = plant.CurrentGrowthTime > 0.001f,
                retiring = false,
                activatedAt = -1f
            });
        }
    }

    private void ResetPoolToSeed()
    {
        foreach (PlantSlot slot in slots)
        {
            if (slot.plant == null)
            {
                continue;
            }

            slot.plant.SetGrowthTimeImmediate(0f);
            slot.active = false;
            slot.retiring = false;
            slot.activatedAt = -1f;
        }
    }

    private void ExpandPoolIfNeeded()
    {
        if (!expandPoolToMaxActive || slots.Count >= Mathf.Max(1, maxActiveMushrooms))
        {
            return;
        }

        GrowthPlant template = FindPoolTemplate();
        if (template == null)
        {
            return;
        }

        Transform parent = template.transform.parent != null ? template.transform.parent : transform;
        int targetCount = Mathf.Max(1, maxActiveMushrooms);
        while (slots.Count < targetCount)
        {
            GrowthPlant clone = Instantiate(template, parent);
            clone.name = $"{template.name}_RuntimePool_{slots.Count + 1:00}";
            clone.transform.SetPositionAndRotation(template.transform.position, template.transform.rotation);
            clone.transform.localScale = template.transform.localScale;
            clone.RebuildRuntimeGeneratedVisuals();
            clone.SetGrowthTimeImmediate(0f);
            clone.ResetRuntimeVariation();

            slots.Add(new PlantSlot
            {
                plant = clone,
                active = false,
                retiring = false,
                activatedAt = -1f
            });
        }
    }

    private GrowthPlant FindPoolTemplate()
    {
        foreach (PlantSlot slot in slots)
        {
            if (slot.plant != null)
            {
                return slot.plant;
            }
        }

        return null;
    }

    private void UpdateInputChargeState()
    {
        bool pressed = TryReadSeedRequest(out SeedRequest request);
        bool validRequest = pressed && IsSeedRequestEligible(request);

        if (validRequest)
        {
            pendingInput.request = request;
            if (!pendingInput.active)
            {
                pendingInput.active = true;
                pendingInput.startedAt = Time.time;
                EnsureChargeOrb();
            }

            UpdateChargeOrb(request, Time.time - pendingInput.startedAt);
            return;
        }

        if (pressed)
        {
            DestroyChargeOrb();
            pendingInput = default;
            return;
        }

        if (!pendingInput.active)
        {
            return;
        }

        bool charged = Time.time - pendingInput.startedAt >= Mathf.Max(0.05f, chargedHoldSeconds);
        DestroyChargeOrb();
        TryActivateSeedRequest(pendingInput.request, charged);
        pendingInput = default;
    }

    private bool TryReadSeedRequest(out SeedRequest request)
    {
        request = default;

        if (enableMouseClickFallback && Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                return false;
            }

            request.ray = camera.ScreenPointToRay(Mouse.current.position.ReadValue());
            request.magicOrigin = request.ray.origin;
            return true;
        }

        bool pressed = false;
        if (rightTrigger.action != null && rightTrigger.action.IsPressed())
        {
            pressed = true;
        }

        if (!rightTriggerOnly && leftTrigger.action != null && leftTrigger.action.IsPressed())
        {
            pressed = true;
        }

        if (enableKeyboardFallback && Keyboard.current != null && Keyboard.current[keyboardSeedKey].isPressed)
        {
            pressed = true;
        }

        if (!pressed || interactionOrigin == null)
        {
            return pressed;
        }

        request.ray = new Ray(interactionOrigin.position, interactionOrigin.forward);
        request.magicOrigin = interactionOrigin.position + interactionOrigin.forward * 0.35f;
        return true;
    }

    private bool IsSeedRequestEligible(SeedRequest request)
    {
        if (request.ray.direction.sqrMagnitude < 0.0001f)
        {
            return false;
        }

        return TryResolveRequestTarget(request.ray, out _, out _);
    }

    private void EnsureChargeOrb()
    {
        if (!enableChargeOrb || chargeOrbRoot != null)
        {
            return;
        }

        chargeOrbRoot = new GameObject("EarthMagicChargeOrb");
        chargeOrbParticles = chargeOrbRoot.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = chargeOrbParticles.main;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.34f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.01f, 0.055f);
        main.startSize = new ParticleSystem.MinMaxCurve(chargeOrbBaseScale * 0.62f, chargeOrbBaseScale * 1.15f);
        main.startColor = chargeParticleTexture != null
            ? new ParticleSystem.MinMaxGradient(new Color(1f, 1f, 1f, 0.92f), new Color(1f, 1f, 1f, 0.36f))
            : new ParticleSystem.MinMaxGradient(
                new Color(earthCoreColor.r, earthCoreColor.g, earthCoreColor.b, 0.92f),
                new Color(earthTrailColor.r, earthTrailColor.g, earthTrailColor.b, 0.36f));
        main.maxParticles = Mathf.Max(4, chargeParticleMaxCount);
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        ParticleSystem.EmissionModule emission = chargeOrbParticles.emission;
        emission.rateOverTime = 20f;

        ParticleSystem.ShapeModule shape = chargeOrbParticles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.018f;

        ParticleSystem.NoiseModule noise = chargeOrbParticles.noise;
        noise.enabled = true;
        noise.strength = 0.09f;
        noise.frequency = 1.6f;
        noise.scrollSpeed = 0.42f;

        ParticleSystemRenderer renderer = chargeOrbParticles.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = GetRuntimeEarthParticleMaterial();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.minParticleSize = 0.002f;
        renderer.maxParticleSize = 0.08f;

        chargeOrbParticles.Play();
    }

    private void UpdateChargeOrb(SeedRequest request, float heldSeconds)
    {
        if (!enableChargeOrb)
        {
            return;
        }

        EnsureChargeOrb();
        if (chargeOrbRoot == null)
        {
            return;
        }

        float chargeT = Mathf.Clamp01(heldSeconds / Mathf.Max(0.05f, chargedHoldSeconds));
        Vector3 forward = request.ray.direction.sqrMagnitude > 0.0001f ? request.ray.direction.normalized : Vector3.forward;
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        if (right.sqrMagnitude < 0.0001f)
        {
            right = Vector3.right;
        }

        right.Normalize();
        Vector3 up = Vector3.Cross(forward, right).normalized;
        float phase = Time.time * chargeOrbJitterSpeed;
        Vector3 jitter =
            right * (Mathf.PerlinNoise(phase, 0.19f) - 0.5f)
            + up * (Mathf.PerlinNoise(0.37f, phase) - 0.5f);
        jitter *= chargeOrbJitterRadius * Mathf.Lerp(0.45f, 1f, chargeT);
        float hover = Mathf.Sin(Time.time * 8.5f) * 0.012f;
        Vector3 center = request.magicOrigin + forward * chargeOrbForwardOffset + up * hover + jitter;
        chargeOrbRoot.transform.position = center;
        chargeOrbRoot.transform.rotation = Quaternion.LookRotation(forward, up);
        float pulse = 0.9f + Mathf.Sin(Time.time * 13.5f) * 0.055f;
        chargeOrbRoot.transform.localScale = Vector3.one * Mathf.Lerp(0.85f, 1.28f, chargeT) * pulse;

        if (chargeOrbParticles != null)
        {
            ParticleSystem.MainModule main = chargeOrbParticles.main;
            float size = Mathf.Lerp(chargeOrbBaseScale, chargeOrbMaxScale, chargeT);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.58f, size * 1.12f);

            ParticleSystem.EmissionModule emission = chargeOrbParticles.emission;
            emission.rateOverTime = Mathf.Lerp(12f, 28f, chargeT);
        }
    }

    private void DestroyChargeOrb()
    {
        if (chargeOrbRoot == null)
        {
            return;
        }

        Destroy(chargeOrbRoot);
        chargeOrbRoot = null;
        chargeOrbParticles = null;
    }

    private void TryActivateSeedRequest(SeedRequest request, bool charged)
    {
        if (request.ray.direction.sqrMagnitude < 0.0001f)
        {
            if (logDebugMessages)
            {
                Debug.Log("GrowthSeedZoneDriver: seed request has no valid ray.");
            }
            return;
        }

        if (drawDebugRay)
        {
            Debug.DrawRay(request.ray.origin, request.ray.direction.normalized * rayDistance, earthCoreColor, 1.5f);
        }

        if (!TryResolveRequestTarget(request.ray, out Vector3 targetPoint, out GrowthPlant targetPlant))
        {
            if (logDebugMessages)
            {
                Debug.Log("GrowthSeedZoneDriver: no valid mushroom target or planting ground found.");
            }
            return;
        }

        StartCoroutine(ResolveAfterEarthMagic(request.magicOrigin, targetPoint, targetPlant, charged));
    }

    private bool TryResolveRequestTarget(Ray ray, out Vector3 targetPoint, out GrowthPlant targetPlant)
    {
        targetPoint = Vector3.positiveInfinity;
        targetPlant = null;

        RaycastHit[] hits = Physics.RaycastAll(ray, rayDistance, groundMask, QueryTriggerInteraction.Ignore);
        if (hits != null && hits.Length > 0)
        {
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (RaycastHit hit in hits)
            {
                Collider hitCollider = hit.collider;
                if (IsIgnoredRaycastHit(hitCollider))
                {
                    continue;
                }

                if (allowCultivateExistingMushrooms && TryResolveCultivationTarget(hitCollider, hit.point, out targetPoint, out targetPlant))
                {
                    return true;
                }

                if (IsValidPlantingSurface(hitCollider))
                {
                    if (!IsPointInsideZone(hit.point))
                    {
                        return false;
                    }

                    targetPoint = hit.point;
                    return true;
                }

                if (blockWhenPointingAtInteractable && IsBlockingInteractableHit(hitCollider))
                {
                    return false;
                }

                return false;
            }
        }

        if (allowForwardFallbackForDebug)
        {
            Vector3 fallbackPoint = ResolveForwardSpawnPoint();
            if (fallbackPoint != Vector3.positiveInfinity && IsPointInsideZone(fallbackPoint))
            {
                targetPoint = fallbackPoint;
                return true;
            }
        }

        return false;
    }

    private bool TryResolveCultivationTarget(Collider hitCollider, Vector3 hitPoint, out Vector3 targetPoint, out GrowthPlant targetPlant)
    {
        targetPoint = Vector3.positiveInfinity;
        targetPlant = null;

        if (hitCollider == null)
        {
            return false;
        }

        GrowthPlant plant = hitCollider.GetComponentInParent<GrowthPlant>();
        if (plant == null || !TryFindActiveSlot(plant, out PlantSlot slot) || slot.retiring)
        {
            return false;
        }

        targetPlant = plant;
        targetPoint = hitPoint;
        return true;
    }

    private IEnumerator ResolveAfterEarthMagic(Vector3 magicOrigin, Vector3 targetPoint, GrowthPlant targetPlant, bool charged)
    {
        if (enableEarthMagicProjectile)
        {
            yield return FlyEarthMagicProjectile(magicOrigin, targetPoint);
        }

        if (targetPlant != null)
        {
            CultivateExistingMushroom(targetPlant, charged);
            yield break;
        }

        TryPlantSeedAt(targetPoint, charged);
    }

    private void TryPlantSeedAt(Vector3 targetPoint, bool charged)
    {
        if (targetPoint == Vector3.positiveInfinity)
        {
            return;
        }

        if (!IsPointInsideZone(targetPoint))
        {
            if (logDebugMessages)
            {
                Debug.Log($"GrowthSeedZoneDriver: target point {targetPoint} is outside growth zone.");
            }
            return;
        }

        if (IsTooCloseToActivePlant(targetPoint))
        {
            if (logDebugMessages)
            {
                Debug.Log("GrowthSeedZoneDriver: target point is too close to an existing mushroom.");
            }
            return;
        }

        int mushroomsToSpawn = charged
            ? Random.Range(
                Mathf.Max(1, chargedMinMushroomsPerSeed),
                Mathf.Max(Mathf.Max(1, chargedMinMushroomsPerSeed), chargedMaxMushroomsPerSeed) + 1)
            : Mathf.Max(1, tapMushroomsPerSeed);

        List<Vector3> spawnPositions = BuildSpawnPositions(targetPoint, mushroomsToSpawn, charged ? chargedBurstRadius : 0f);
        foreach (Vector3 spawnPosition in spawnPositions)
        {
            TrySpawnSingleMushroom(spawnPosition);
        }
    }

    private void CultivateExistingMushroom(GrowthPlant plant, bool charged)
    {
        if (plant == null)
        {
            return;
        }

        int steps = charged ? Mathf.Max(1, chargedExistingMushroomGrowthSteps) : 1;
        plant.CultivateMatureScale(
            existingMushroomScaleStep * steps,
            existingMushroomMaxScale,
            existingMushroomScaleSeconds * Mathf.Sqrt(steps));
    }

    private bool TryFindActiveSlot(GrowthPlant plant, out PlantSlot slot)
    {
        foreach (PlantSlot candidate in slots)
        {
            if (candidate.plant == plant && candidate.active)
            {
                slot = candidate;
                return true;
            }
        }

        slot = null;
        return false;
    }

    private bool IsPointInsideZone(Vector3 point)
    {
        if (growthZone == null)
        {
            return true;
        }

        Vector3 closest = growthZone.ClosestPoint(point);
        return (closest - point).sqrMagnitude < 0.0001f;
    }

    private Vector3 ResolveForwardSpawnPoint()
    {
        if (interactionOrigin == null)
        {
            return Vector3.positiveInfinity;
        }

        Vector3 origin = interactionOrigin.position;
        Vector3 forward = interactionOrigin.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = interactionOrigin.forward;
        }
        forward.Normalize();

        Vector3 desiredPoint = origin + forward * Mathf.Max(0f, forwardSpawnDistance);
        return ReprojectPointToGround(desiredPoint);
    }

    private bool IsTooCloseToActivePlant(Vector3 point)
    {
        float minSpacingSqr = minSpacingBetweenPlants * minSpacingBetweenPlants;
        foreach (PlantSlot slot in slots)
        {
            if (slot.plant == null || !slot.active || slot.retiring)
            {
                continue;
            }

            Vector3 flatDelta = slot.plant.transform.position - point;
            flatDelta.y = 0f;
            if (flatDelta.sqrMagnitude < minSpacingSqr)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsTooCloseToOtherSpawn(Vector3 point, List<Vector3> existing)
    {
        float minSpacingSqr = minSpacingBetweenPlants * minSpacingBetweenPlants;
        foreach (Vector3 other in existing)
        {
            Vector3 flatDelta = other - point;
            flatDelta.y = 0f;
            if (flatDelta.sqrMagnitude < minSpacingSqr)
            {
                return true;
            }
        }

        return false;
    }

    private PlantSlot FindAvailableSlot()
    {
        foreach (PlantSlot slot in slots)
        {
            if (slot.plant == null)
            {
                continue;
            }

            if (!slot.active && !slot.retiring)
            {
                return slot;
            }
        }

        return null;
    }

    private PlantSlot FindOldestActiveSlot()
    {
        PlantSlot oldest = null;
        float oldestTime = float.MaxValue;
        int activeCount = 0;

        foreach (PlantSlot slot in slots)
        {
            if (slot.plant == null || !slot.active || slot.retiring)
            {
                continue;
            }

            activeCount++;
            if (slot.activatedAt < oldestTime)
            {
                oldest = slot;
                oldestTime = slot.activatedAt;
            }
        }

        return activeCount >= Mathf.Max(1, maxActiveMushrooms) ? oldest : null;
    }

    private List<Vector3> BuildSpawnPositions(Vector3 centerPoint, int desiredCount, float placementRadius)
    {
        List<Vector3> results = new();
        int attempts = Mathf.Max(8, desiredCount * 8);

        if (!IsTooCloseToActivePlant(centerPoint))
        {
            results.Add(centerPoint);
        }

        for (int i = 0; i < attempts && results.Count < desiredCount; i++)
        {
            Vector2 randomOffset = Random.insideUnitCircle * Mathf.Max(0f, placementRadius);
            Vector3 candidateFlat = centerPoint + new Vector3(randomOffset.x, 0f, randomOffset.y);
            Vector3 candidate = ReprojectPointToGround(candidateFlat);
            if (candidate == Vector3.positiveInfinity)
            {
                continue;
            }

            if (!IsPointInsideZone(candidate) ||
                IsTooCloseToActivePlant(candidate) ||
                IsTooCloseToOtherSpawn(candidate, results))
            {
                continue;
            }

            results.Add(candidate);
        }

        return results;
    }

    private void TrySpawnSingleMushroom(Vector3 spawnPosition)
    {
        PlantSlot available = FindAvailableSlot();
        if (available != null)
        {
            ActivateSlotAt(available, spawnPosition);
            return;
        }

        PlantSlot oldest = FindOldestActiveSlot();
        if (oldest != null)
        {
            StartCoroutine(RetireAndReuseSlot(oldest, spawnPosition));
        }
    }

    private void ActivateSlotAt(PlantSlot slot, Vector3 spawnPosition)
    {
        if (slot.plant == null)
        {
            return;
        }

        slot.plant.transform.position = ResolveSpawnRootPosition(spawnPosition, slot.plant);
        if (randomizeYaw)
        {
            Vector3 euler = slot.plant.transform.eulerAngles;
            euler.y = Random.Range(0f, 360f);
            slot.plant.transform.eulerAngles = euler;
        }

        slot.plant.SetGrowthTimeImmediate(0f);
        slot.plant.ConfigureRuntimeVariation(
            Random.Range(randomScaleRange.x, randomScaleRange.y),
            Random.Range(randomDurationRange.x, randomDurationRange.y),
            Random.Range(randomWobbleRange.x, randomWobbleRange.y),
            Random.value);
        slot.plant.GrowToFull();
        slot.active = true;
        slot.retiring = false;
        slot.activatedAt = Time.time;

        if (logDebugMessages)
        {
            Debug.Log($"GrowthSeedZoneDriver: spawned mushroom at {slot.plant.transform.position}.");
        }
    }

    private Vector3 ResolveSpawnRootPosition(Vector3 requestedPosition, GrowthPlant plant)
    {
        if (!snapSpawnToGround || plant == null)
        {
            return requestedPosition;
        }

        Vector3 probeOrigin = requestedPosition + Vector3.up * Mathf.Max(0.1f, spawnGroundProbeHeight);
        RaycastHit[] hits = Physics.RaycastAll(
            probeOrigin,
            Vector3.down,
            Mathf.Max(0.1f, spawnGroundProbeDepth),
            groundMask,
            QueryTriggerInteraction.Ignore);

        if (hits == null || hits.Length == 0)
        {
            return requestedPosition;
        }

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits)
        {
            if (IsIgnoredRaycastHit(hit.collider) || !IsValidPlantingSurface(hit.collider))
            {
                continue;
            }

            Vector3 grounded = requestedPosition;
            grounded.y = hit.point.y + spawnOffset.y + spawnGroundClearance;
            return grounded;
        }

        return requestedPosition;
    }

    private IEnumerator RetireAndReuseSlot(PlantSlot slot, Vector3 spawnPosition)
    {
        slot.retiring = true;
        if (retireInstantlyWhenReused)
        {
            slot.plant.SetGrowthTimeImmediate(0f);
            slot.plant.ResetRuntimeVariation();
            if (retireReuseDelay > 0f)
            {
                yield return new WaitForSeconds(retireReuseDelay);
            }
        }
        else
        {
            slot.plant.ShrinkToSeed();

            while (slot.plant.IsTransitioning() || slot.plant.CurrentGrowthTime > 0.001f)
            {
                yield return null;
            }

            slot.plant.ResetRuntimeVariation();
        }

        slot.active = false;
        slot.retiring = false;
        slot.activatedAt = -1f;

        ActivateSlotAt(slot, spawnPosition);
    }

    private Vector3 ReprojectPointToGround(Vector3 flatPoint)
    {
        Vector3 reprojectionOrigin = flatPoint + Vector3.up * rayDistance;
        RaycastHit[] hits = Physics.RaycastAll(
            reprojectionOrigin,
            Vector3.down,
            rayDistance * 2f,
            groundMask,
            QueryTriggerInteraction.Ignore);

        if (hits != null && hits.Length > 0)
        {
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (RaycastHit hit in hits)
            {
                if (IsIgnoredRaycastHit(hit.collider) || !IsValidPlantingSurface(hit.collider))
                {
                    continue;
                }

                return hit.point;
            }
        }

        return Vector3.positiveInfinity;
    }

    private bool IsIgnoredRaycastHit(Collider candidate)
    {
        if (candidate == null)
        {
            return true;
        }

        if (growthZone != null && candidate == growthZone)
        {
            return true;
        }

        if (candidate.GetComponentInParent<CharacterController>() != null)
        {
            return true;
        }

        return candidate.gameObject.tag == "Player";
    }

    private bool IsValidPlantingSurface(Collider candidate)
    {
        if (candidate == null)
        {
            return false;
        }

        if (!requireTerrainColliderForNewMushrooms)
        {
            return !IsBlockingInteractableHit(candidate);
        }

        return candidate is TerrainCollider || candidate.GetComponent<Terrain>() != null;
    }

    private bool IsBlockingInteractableHit(Collider candidate)
    {
        if (candidate == null)
        {
            return false;
        }

        if (candidate.GetComponentInParent<GrowthPlant>() != null)
        {
            return true;
        }

        if (candidate.GetComponentInParent<XRBaseInteractable>() != null)
        {
            return true;
        }

        Component[] components = candidate.GetComponentsInParent<Component>(true);
        for (int i = 0; i < components.Length; i++)
        {
            Component component = components[i];
            if (component == null)
            {
                continue;
            }

            string typeName = component.GetType().Name;
            if (typeName.Contains("InteractionPrompt") ||
                typeName.Contains("Interactable") ||
                typeName.Contains("Highlight"))
            {
                return true;
            }
        }

        Transform current = candidate.transform;
        while (current != null)
        {
            string objectName = current.name;
            if (objectName.EndsWith("_ToonOutline", System.StringComparison.Ordinal) ||
                objectName.Contains("Highlight") ||
                objectName.Contains("InteractionPrompt"))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private bool IsRightControllerOrigin(Transform candidate)
    {
        return candidate != null && candidate.name.Contains("Right Controller");
    }

    private static Transform FindRightControllerOrigin()
    {
        Transform found = FindInScene("Right Controller Stabilized Attach");
        if (found != null)
        {
            return found;
        }

        found = FindInScene("Right Controller Teleport Stabilized Origin");
        if (found != null)
        {
            return found;
        }

        return FindInScene("Right Controller");
    }

    private static Transform FindInScene(string targetName)
    {
        Scene activeScene = SceneManager.GetActiveScene();
        GameObject[] roots = activeScene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            Transform found = FindChildRecursive(roots[i].transform, targetName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static Transform FindChildRecursive(Transform root, string targetName)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == targetName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildRecursive(root.GetChild(i), targetName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private IEnumerator FlyEarthMagicProjectile(Vector3 start, Vector3 end)
    {
        GameObject magicRoot = new GameObject("EarthMagicRibbon");
        Transform magicTransform = magicRoot.transform;

        LineRenderer halo = CreateMagicLine(magicRoot, "OuterEarthGlow", earthTrailWidth * earthHaloWidthMultiplier, earthTrailColor, 0.015f, 0.24f);
        LineRenderer core = CreateMagicLine(magicRoot, "InnerEarthThread", earthTrailWidth, earthCoreColor, 0.035f, 0.92f);

        int strandCount = Mathf.Clamp(earthSpiralStrandCount, 0, 6);
        LineRenderer[] strands = new LineRenderer[strandCount];
        for (int i = 0; i < strandCount; i++)
        {
            strands[i] = CreateMagicLine(
                magicRoot,
                $"SpiralEarthThread_{i + 1}",
                earthTrailWidth * earthStrandWidthMultiplier,
                Color.Lerp(earthTrailColor, earthCoreColor, 0.35f),
                0f,
                0.72f);
        }

        Light glow = magicRoot.AddComponent<Light>();
        glow.color = earthCoreColor;
        glow.intensity = 1.15f;
        glow.range = 1.55f;

        Vector3 travel = end - start;
        Vector3 travelDirection = travel.sqrMagnitude > 0.0001f ? travel.normalized : transform.forward;
        Vector3 side = Vector3.Cross(Vector3.up, travelDirection);
        if (side.sqrMagnitude < 0.0001f)
        {
            side = Vector3.right;
        }

        side.Normalize();
        Vector3 controlA = Vector3.Lerp(start, end, 0.32f) + Vector3.up * (earthMagicArcHeight * 0.62f) + side * earthMagicSideCurve;
        Vector3 controlB = Vector3.Lerp(start, end, 0.74f) + Vector3.up * earthMagicArcHeight + side * earthMagicSecondarySideCurve;
        float flightSeconds = Mathf.Max(0.05f, earthMagicFlightSeconds);
        float elapsed = 0f;

        while (elapsed < flightSeconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / flightSeconds);
            float eased = t * t * (3f - 2f * t);
            Vector3 current = CubicBezier(start, controlA, controlB, end, eased);

            magicTransform.position = current;
            UpdateRibbonLine(core, start, controlA, controlB, end, eased, 0f, 0f, false);
            UpdateRibbonLine(halo, start, controlA, controlB, end, eased, 0f, 0f, false);

            for (int i = 0; i < strands.Length; i++)
            {
                float phase = i / Mathf.Max(1f, strands.Length) * Mathf.PI * 2f;
                UpdateRibbonLine(strands[i], start, controlA, controlB, end, eased, phase, earthSpiralRadius, true);
            }

            glow.intensity = Mathf.Lerp(0.75f, 1.75f, Mathf.Sin(t * Mathf.PI));
            yield return null;
        }

        magicTransform.position = end;
        UpdateRibbonLine(core, start, controlA, controlB, end, 1f, 0f, 0f, false);
        UpdateRibbonLine(halo, start, controlA, controlB, end, 1f, 0f, 0f, false);
        for (int i = 0; i < strands.Length; i++)
        {
            float phase = i / Mathf.Max(1f, strands.Length) * Mathf.PI * 2f;
            UpdateRibbonLine(strands[i], start, controlA, controlB, end, 1f, phase, earthSpiralRadius, true);
        }

        yield return PlayImpactSparks(magicRoot, end, travelDirection);
        Destroy(magicRoot);
    }

    private LineRenderer CreateMagicLine(GameObject parent, string lineName, float width, Color color, float startAlpha, float endAlpha)
    {
        GameObject lineObject = new GameObject(lineName);
        lineObject.transform.SetParent(parent.transform, false);

        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.sharedMaterial = GetRuntimeEarthMaterial();
        line.positionCount = Mathf.Max(6, earthTrailSegments);
        line.widthMultiplier = Mathf.Max(0.006f, width);
        line.numCapVertices = 4;
        line.alignment = LineAlignment.View;
        line.textureMode = LineTextureMode.Stretch;
        line.widthCurve = CreateTailTaperCurve();
        line.startColor = new Color(color.r, color.g, color.b, startAlpha);
        line.endColor = new Color(color.r, color.g, color.b, endAlpha);
        return line;
    }

    private IEnumerator PlayImpactSparks(GameObject parent, Vector3 impactPoint, Vector3 incomingDirection)
    {
        int sparkCount = Mathf.Clamp(earthImpactSparkCount, 0, 32);
        if (sparkCount == 0 || earthImpactSparkSeconds <= 0f)
        {
            yield break;
        }

        LineRenderer[] sparks = new LineRenderer[sparkCount];
        Vector3[] directions = new Vector3[sparkCount];
        float[] lengths = new float[sparkCount];

        Vector3 baseRight = Vector3.Cross(Vector3.up, incomingDirection);
        if (baseRight.sqrMagnitude < 0.0001f)
        {
            baseRight = Vector3.right;
        }

        baseRight.Normalize();
        Vector3 baseUp = Vector3.Cross(incomingDirection, baseRight).normalized;

        for (int i = 0; i < sparkCount; i++)
        {
            float angle = i / Mathf.Max(1f, sparkCount) * Mathf.PI * 2f;
            float lift = Mathf.Lerp(0.12f, 0.62f, Halton(i + 1, 3));
            directions[i] = (baseRight * Mathf.Cos(angle) + baseUp * Mathf.Sin(angle) + Vector3.up * lift).normalized;
            lengths[i] = Mathf.Lerp(0.14f, 0.38f, Halton(i + 1, 5));
            sparks[i] = CreateMagicLine(parent, $"ImpactEarthSpark_{i + 1}", earthTrailWidth * 0.45f, earthCoreColor, 0.86f, 0f);
            sparks[i].positionCount = 2;
        }

        float elapsed = 0f;
        float sparkSeconds = Mathf.Max(0.05f, earthImpactSparkSeconds);
        while (elapsed < sparkSeconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / sparkSeconds);
            float fade = 1f - t;

            for (int i = 0; i < sparks.Length; i++)
            {
                Vector3 tip = impactPoint + directions[i] * lengths[i] * Mathf.Sin(t * Mathf.PI * 0.85f);
                sparks[i].startColor = new Color(earthCoreColor.r, earthCoreColor.g, earthCoreColor.b, 0.82f * fade);
                sparks[i].endColor = new Color(earthTrailColor.r, earthTrailColor.g, earthTrailColor.b, 0f);
                sparks[i].SetPosition(0, impactPoint);
                sparks[i].SetPosition(1, tip);
            }

            yield return null;
        }
    }

    private Material GetRuntimeEarthMaterial()
    {
        if (runtimeEarthMaterial != null)
        {
            return runtimeEarthMaterial;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        if (shader == null)
        {
            shader = Shader.Find("Hidden/InternalErrorShader");
        }

        runtimeEarthMaterial = new Material(shader);
        runtimeEarthMaterial.name = "Runtime Earth Magic Trail";
        runtimeEarthMaterial.renderQueue = 3000;
        if (runtimeEarthMaterial.HasProperty("_Surface"))
        {
            runtimeEarthMaterial.SetFloat("_Surface", 1f);
        }

        if (runtimeEarthMaterial.HasProperty("_Blend"))
        {
            runtimeEarthMaterial.SetFloat("_Blend", 1f);
        }

        if (runtimeEarthMaterial.HasProperty("_SrcBlend"))
        {
            runtimeEarthMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        }

        if (runtimeEarthMaterial.HasProperty("_DstBlend"))
        {
            runtimeEarthMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
        }

        if (runtimeEarthMaterial.HasProperty("_ZWrite"))
        {
            runtimeEarthMaterial.SetFloat("_ZWrite", 0f);
        }

        if (runtimeEarthMaterial.HasProperty("_BaseColor"))
        {
            runtimeEarthMaterial.SetColor("_BaseColor", earthCoreColor);
        }
        else if (runtimeEarthMaterial.HasProperty("_Color"))
        {
            runtimeEarthMaterial.SetColor("_Color", earthCoreColor);
        }

        runtimeEarthMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        runtimeEarthMaterial.EnableKeyword("_ALPHABLEND_ON");
        return runtimeEarthMaterial;
    }

    private Material GetRuntimeEarthParticleMaterial()
    {
        if (runtimeEarthParticleMaterial != null)
        {
            return runtimeEarthParticleMaterial;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Particles/Standard Unlit");
        }

        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        if (shader == null)
        {
            shader = Shader.Find("Hidden/InternalErrorShader");
        }

        runtimeEarthParticleMaterial = new Material(shader);
        runtimeEarthParticleMaterial.name = "Runtime Earth Charge Particle";
        runtimeEarthParticleMaterial.renderQueue = 3000;

        Texture2D texture = chargeParticleTexture != null ? chargeParticleTexture : CreateSoftParticleTexture();
        if (runtimeEarthParticleMaterial.HasProperty("_BaseMap"))
        {
            runtimeEarthParticleMaterial.SetTexture("_BaseMap", texture);
        }

        if (runtimeEarthParticleMaterial.HasProperty("_MainTex"))
        {
            runtimeEarthParticleMaterial.SetTexture("_MainTex", texture);
        }

        if (runtimeEarthParticleMaterial.HasProperty("_BaseColor"))
        {
            runtimeEarthParticleMaterial.SetColor("_BaseColor", Color.white);
        }
        else if (runtimeEarthParticleMaterial.HasProperty("_Color"))
        {
            runtimeEarthParticleMaterial.SetColor("_Color", Color.white);
        }

        if (runtimeEarthParticleMaterial.HasProperty("_SrcBlend"))
        {
            runtimeEarthParticleMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        }

        if (runtimeEarthParticleMaterial.HasProperty("_DstBlend"))
        {
            runtimeEarthParticleMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
        }

        if (runtimeEarthParticleMaterial.HasProperty("_ZWrite"))
        {
            runtimeEarthParticleMaterial.SetFloat("_ZWrite", 0f);
        }

        runtimeEarthParticleMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        runtimeEarthParticleMaterial.EnableKeyword("_ALPHABLEND_ON");
        return runtimeEarthParticleMaterial;
    }

    private Texture2D CreateSoftParticleTexture()
    {
        if (runtimeEarthParticleTexture != null)
        {
            return runtimeEarthParticleTexture;
        }

        const int size = 32;
        runtimeEarthParticleTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "Runtime Earth Charge Soft Dot",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        Color[] pixels = new Color[size * size];
        Vector2 center = new((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center) / radius;
                float alpha = Mathf.Clamp01(1f - distance);
                alpha = alpha * alpha * (3f - 2f * alpha);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        runtimeEarthParticleTexture.SetPixels(pixels);
        runtimeEarthParticleTexture.Apply(updateMipmaps: false, makeNoLongerReadable: true);
        return runtimeEarthParticleTexture;
    }

    private void UpdateRibbonLine(LineRenderer trail, Vector3 start, Vector3 controlA, Vector3 controlB, Vector3 end, float visibleT, float phase, float radius, bool spiral)
    {
        int count = trail.positionCount;
        Vector3 pathForward = (end - start).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, pathForward);
        if (right.sqrMagnitude < 0.0001f)
        {
            right = Vector3.right;
        }

        right.Normalize();
        Vector3 up = Vector3.Cross(pathForward, right).normalized;
        float tailStart = Mathf.Clamp01(visibleT - Mathf.Clamp01(earthTrailVisibleFraction));
        float visibleSpan = Mathf.Max(0.001f, visibleT - tailStart);

        for (int i = 0; i < count; i++)
        {
            float segmentT = i / Mathf.Max(1f, count - 1);
            float t = Mathf.Clamp01(tailStart + visibleSpan * segmentT);
            Vector3 point = CubicBezier(start, controlA, controlB, end, t);
            if (spiral)
            {
                float taper = Mathf.Sin(segmentT * Mathf.PI);
                float unevenRadius = radius + earthSpiralRadiusVariation * (0.5f + 0.5f * Mathf.Sin(t * Mathf.PI * 5.2f + phase * 1.7f));
                unevenRadius *= Mathf.Lerp(0.62f, 1.08f, Mathf.PerlinNoise(t * 3.1f, phase));
                float angle = phase + t * earthSpiralTurns * Mathf.PI * 2f + Time.time * 4.5f;
                point += (right * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * unevenRadius * taper;
            }

            trail.SetPosition(i, point);
        }
    }

    private static AnimationCurve CreateTailTaperCurve()
    {
        return new AnimationCurve(
            new Keyframe(0f, 0.05f),
            new Keyframe(0.28f, 0.22f),
            new Keyframe(0.78f, 0.72f),
            new Keyframe(1f, 1f));
    }

    private static float Halton(int index, int radix)
    {
        float result = 0f;
        float fraction = 1f / radix;
        while (index > 0)
        {
            result += fraction * (index % radix);
            index = Mathf.FloorToInt(index / (float)radix);
            fraction /= radix;
        }

        return result;
    }

    private static Vector3 CubicBezier(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t)
    {
        float inverse = 1f - t;
        return inverse * inverse * inverse * a
            + 3f * inverse * inverse * t * b
            + 3f * inverse * t * t * c
            + t * t * t * d;
    }
}
