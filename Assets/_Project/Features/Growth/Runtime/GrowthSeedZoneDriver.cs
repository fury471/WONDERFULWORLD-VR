using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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

    [Header("Zone")]
    [SerializeField] private Collider growthZone;
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float rayDistance = 20f;
    [SerializeField] private float forwardSpawnDistance = 3.5f;
    [SerializeField] private float minSpacingBetweenPlants = 0.75f;

    [Header("Interaction")]
    [SerializeField] private Transform interactionOrigin;
    [SerializeField] private InputActionProperty leftTrigger;
    [SerializeField] private InputActionProperty rightTrigger;
    [SerializeField] private bool enableMouseClickFallback = true;
    [SerializeField] private bool enableKeyboardFallback = true;
    [SerializeField] private Key keyboardSeedKey = Key.G;

    [Header("Pool")]
    [SerializeField] private GrowthPlant[] mushroomPool;
    [SerializeField] private int maxActiveMushrooms = 4;
    [SerializeField] private Vector3 spawnOffset = Vector3.zero;
    [SerializeField] private bool randomizeYaw = true;
    [SerializeField] private bool retireInstantlyWhenReused = true;
    [SerializeField] private float retireReuseDelay = 0.08f;

    [Header("Seed Burst")]
    [SerializeField] private int minMushroomsPerSeed = 2;
    [SerializeField] private int maxMushroomsPerSeed = 3;
    [SerializeField] private float burstRadius = 0.9f;

    [Header("Variation")]
    [SerializeField] private Vector2 randomScaleRange = new Vector2(0.85f, 1.2f);
    [SerializeField] private Vector2 randomDurationRange = new Vector2(0.85f, 1.2f);
    [SerializeField] private Vector2 randomWobbleRange = new Vector2(0.8f, 1.25f);

    [Header("Debug")]
    [SerializeField] private bool logDebugMessages;
    [SerializeField] private bool drawDebugRay = true;

    private readonly List<PlantSlot> slots = new();
    private bool triggerPressedLastFrame;

    private void Awake()
    {
        AutoAssignReferences();
        BuildSlots();
        ResetPoolToSeed();
    }

    private void Update()
    {
        AutoAssignReferences();

        bool pressedThisFrame = ReadSeedPressedThisFrame();
        if (pressedThisFrame && !triggerPressedLastFrame)
        {
            TryPlantSeed();
        }

        triggerPressedLastFrame = pressedThisFrame;
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

        foreach (GrowthPlant plant in mushroomPool)
        {
            if (plant == null)
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

    private bool ReadSeedPressedThisFrame()
    {
        bool pressed = false;

        if (rightTrigger.action != null && rightTrigger.action.IsPressed())
        {
            pressed = true;
        }

        if (leftTrigger.action != null && leftTrigger.action.IsPressed())
        {
            pressed = true;
        }

        if (enableMouseClickFallback && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            pressed = true;
        }

        if (enableKeyboardFallback && Keyboard.current != null && Keyboard.current[keyboardSeedKey].isPressed)
        {
            pressed = true;
        }

        return pressed;
    }

    private void TryPlantSeed()
    {
        if (interactionOrigin == null)
        {
            if (logDebugMessages)
            {
                Debug.Log("GrowthSeedZoneDriver: interactionOrigin is missing.");
            }
            return;
        }

        Ray ray = new(interactionOrigin.position, interactionOrigin.forward);
        if (drawDebugRay)
        {
            Debug.DrawRay(ray.origin, ray.direction * rayDistance, Color.green, 1.5f);
        }

        Vector3 targetPoint = ResolveForwardSpawnPoint();
        if (targetPoint == Vector3.positiveInfinity)
        {
            if (logDebugMessages)
            {
                Debug.Log("GrowthSeedZoneDriver: no ground found near forward spawn point.");
            }
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

        int mushroomsToSpawn = Random.Range(
            Mathf.Max(1, minMushroomsPerSeed),
            Mathf.Max(Mathf.Max(1, minMushroomsPerSeed), maxMushroomsPerSeed) + 1);

        List<Vector3> spawnPositions = BuildSpawnPositions(targetPoint, mushroomsToSpawn);
        foreach (Vector3 spawnPosition in spawnPositions)
        {
            TrySpawnSingleMushroom(spawnPosition + spawnOffset);
        }
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
        Vector3 reprojectionOrigin = desiredPoint + Vector3.up * rayDistance;

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
                if (growthZone != null && hit.collider == growthZone)
                {
                    continue;
                }

                return hit.point;
            }
        }

        return Vector3.positiveInfinity;
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

    private List<Vector3> BuildSpawnPositions(Vector3 centerPoint, int desiredCount)
    {
        List<Vector3> results = new();
        int attempts = Mathf.Max(8, desiredCount * 8);

        if (!IsTooCloseToActivePlant(centerPoint))
        {
            results.Add(centerPoint);
        }

        for (int i = 0; i < attempts && results.Count < desiredCount; i++)
        {
            Vector2 randomOffset = Random.insideUnitCircle * burstRadius;
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
        slot.plant.transform.position = spawnPosition;
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
            Random.Range(randomWobbleRange.x, randomWobbleRange.y));
        slot.plant.GrowToFull();
        slot.active = true;
        slot.retiring = false;
        slot.activatedAt = Time.time;

        if (logDebugMessages)
        {
            Debug.Log($"GrowthSeedZoneDriver: spawned mushroom at {spawnPosition}.");
        }
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
                if (growthZone != null && hit.collider == growthZone)
                {
                    continue;
                }

                return hit.point;
            }
        }

        return Vector3.positiveInfinity;
    }
}
