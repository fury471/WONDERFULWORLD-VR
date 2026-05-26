using System.Collections.Generic;
using UnityEngine;

public class ButterflyFlightControllerV2 : MonoBehaviour
{
    private enum FlightState
    {
        Idle,
        Flying,
        WaitingForCat,
        Hiding,
    }

    [Header("Path")]
    [Tooltip("If true, flight follows the assigned CatRideControllerV2's autoRoutePoints (with height offset) so cat and butterfly share one authored path. If false, uses the local flightPoints list.")]
    [SerializeField] private bool useCatAutoRoutePoints = false;
    [Tooltip("World-space Y offset applied to each cat route point when useCatAutoRoutePoints is enabled, so the butterfly flies above the cat's ground path.")]
    [SerializeField] private float catRoutePointHeightOffset = 1.1f;
    [SerializeField] private List<Transform> flightPoints = new List<Transform>();

    [Header("Cat Chase Formation")]
    [Tooltip("When the cat auto ride is active, keep this butterfly in a living formation ahead of the cat instead of only following fixed route points.")]
    [SerializeField] private bool chaseCatDuringAutoRide = true;
    [Tooltip("0 = left/lower, 1 = center/higher, 2 = right/middle. -1 derives a stable slot from the spawn point.")]
    [SerializeField] private int catChaseFormationSlot = -1;
    [SerializeField, Min(0.2f)] private float catChaseBaseLeadDistance = 2.65f;
    [SerializeField, Min(0f)] private float catChaseLeadPulseAmplitude = 0.75f;
    [SerializeField, Min(0f)] private float catChaseLeadPulseFrequency = 0.42f;
    [SerializeField, Min(0.1f)] private float catChaseNearLeadDistance = 1.45f;
    [SerializeField, Min(0.2f)] private float catChaseFarLeadDistance = 4.25f;
    [SerializeField, Min(0f)] private float catChaseLateralSpacing = 0.62f;
    [SerializeField, Min(0f)] private float catChaseHeightSpacing = 0.28f;
    [SerializeField, Min(0f)] private float catChaseJitterRadius = 0.18f;
    [SerializeField, Min(0.1f)] private float catChaseMinSpeed = 2.35f;
    [SerializeField, Min(0.1f)] private float catChaseMaxSpeed = 5.8f;
    [SerializeField, Min(0f)] private float catChaseRecoverySharpness = 9f;

    [Header("Motion")]
    [SerializeField] private float flightSpeed = 2.5f;
    [SerializeField] private float rotateSpeed = 360f;
    [SerializeField] private float reachDistance = 0.08f;
    [SerializeField] private bool resetToStartOnBegin = true;

    [Header("Return After Cat Approach")]
    [SerializeField] private CatRideControllerV2 catController;
    [SerializeField, Min(0.01f)] private float catApproachDistance = 1.5f;
    [SerializeField, Min(0f)] private float maxWaitForCatBeforeReturn = 10f;
    [SerializeField, Min(0f)] private float hiddenDurationBeforeReappear = 0.25f;

    [Header("Initial Cue Orbit")]
    [SerializeField] private bool orbitAroundInitialPointWhenIdle = true;
    [SerializeField, Min(0f)] private float idleOrbitRadius = 0.45f;
    [SerializeField, Min(0f)] private float idleOrbitHeight = 0.12f;
    [SerializeField, Min(0f)] private float idleOrbitDegreesPerSecond = 65f;
    [SerializeField, Min(0f)] private float idleOrbitBobFrequency = 1.2f;
    [SerializeField] private bool faceIdleOrbitDirection = true;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private bool initialPoseCached;
    private int currentPointIndex;
    private Renderer[] cachedRenderers;
    private bool renderersCached;
    private FlightState state = FlightState.Idle;
    private float hideTimer;
    private float waitForCatTimer;
    private float lastWaitLogTime;
    private float idleOrbitPhaseOffset;
    private bool wasCatChaseFormationActive;

    public bool IsReadyForTrigger => state == FlightState.Idle;

    private void Awake()
    {
        CacheInitialPose();
        CacheRenderers();
    }

    private void Update()
    {
        switch (state)
        {
            case FlightState.Idle:
                UpdateIdleCueOrbit();
                break;
            case FlightState.Flying:
                UpdateFlight();
                break;
            case FlightState.WaitingForCat:
                UpdateWaitForCat();
                break;
            case FlightState.Hiding:
                UpdateHidden();
                break;
        }
    }

    public void BeginFlight()
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        CacheInitialPose();
        CacheRenderers();
        SetRenderersVisible(true);

        if (resetToStartOnBegin && initialPoseCached)
        {
            transform.SetPositionAndRotation(initialPosition, initialRotation);
        }

        currentPointIndex = 0;
        hideTimer = 0f;
        waitForCatTimer = 0f;
        lastWaitLogTime = 0f;
        wasCatChaseFormationActive = false;
        state = FlightState.Flying;

        if (debugLogs)
        {
            string catRefInfo = catController != null ? catController.name : "<null, will auto-find>";
            Debug.Log($"[ButterflyFlightControllerV2] Flight started. name={name} catRef={catRefInfo}");
        }
    }

    private void UpdateFlight()
    {
        if (UpdateCatChaseFormationIfActive())
        {
            return;
        }

        if (!TryGetCurrentTargetPosition(out Vector3 targetPosition))
        {
            waitForCatTimer = 0f;
            state = FlightState.WaitingForCat;

            if (debugLogs)
            {
                Debug.Log("[ButterflyFlightControllerV2] Reached final point, waiting for cat to approach.");
            }

            return;
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            flightSpeed * Time.deltaTime);

        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotateSpeed * Time.deltaTime);
        }

        if (Vector3.Distance(transform.position, targetPosition) <= reachDistance)
        {
            currentPointIndex++;
        }
    }

    private void UpdateWaitForCat()
    {
        waitForCatTimer += Time.deltaTime;
        if (maxWaitForCatBeforeReturn > 0f && waitForCatTimer >= maxWaitForCatBeforeReturn)
        {
            ReturnToInitialPoint();

            if (debugLogs)
            {
                Debug.Log($"[ButterflyFlightControllerV2] Cat did not approach within {maxWaitForCatBeforeReturn:F1}s, returned to spawn.");
            }

            return;
        }

        Transform catTransform = ResolveCatTransform();
        if (catTransform == null)
        {
            if (debugLogs && Time.time - lastWaitLogTime >= 1f)
            {
                lastWaitLogTime = Time.time;
                Debug.LogWarning($"[ButterflyFlightControllerV2] WaitingForCat but no CatRideControllerV2 resolved. {name}");
            }
            return;
        }

        float distance = Vector3.Distance(transform.position, catTransform.position);
        if (debugLogs && Time.time - lastWaitLogTime >= 1f)
        {
            lastWaitLogTime = Time.time;
            Debug.Log(
                $"[ButterflyFlightControllerV2] WaitingForCat | name={name} " +
                $"butterflyPos={transform.position} catPos={catTransform.position} " +
                $"catRef={catController.name} distance={distance:F2}m threshold={catApproachDistance:F2}m");
        }

        if (distance <= catApproachDistance)
        {
            SetRenderersVisible(false);
            hideTimer = 0f;
            waitForCatTimer = 0f;
            state = FlightState.Hiding;

            if (debugLogs)
            {
                Debug.Log($"[ButterflyFlightControllerV2] Cat approached ({distance:F2}m), butterfly vanished.");
            }
        }
    }

    private void UpdateHidden()
    {
        hideTimer += Time.deltaTime;
        if (hideTimer < hiddenDurationBeforeReappear)
        {
            return;
        }

        ReturnToInitialPoint();

        if (debugLogs)
        {
            Debug.Log("[ButterflyFlightControllerV2] Returned to spawn, ready for next trigger.");
        }
    }

    private bool UpdateCatChaseFormationIfActive()
    {
        if (!useCatAutoRoutePoints || !chaseCatDuringAutoRide)
        {
            return false;
        }

        Transform catTransform = ResolveCatTransform();
        if (catTransform == null || catController == null)
        {
            return false;
        }

        if (!catController.IsAutoRideActive)
        {
            if (wasCatChaseFormationActive)
            {
                wasCatChaseFormationActive = false;
                waitForCatTimer = 0f;
                state = FlightState.WaitingForCat;

                if (debugLogs)
                {
                    Debug.Log("[ButterflyFlightControllerV2] Cat auto ride ended, waiting near the cat before returning.");
                }

                return true;
            }

            return false;
        }

        wasCatChaseFormationActive = true;
        int slot = ResolveCatChaseFormationSlot();
        Vector3 targetPosition = ResolveCatChaseTarget(catTransform, slot);
        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
        float speedT = Mathf.InverseLerp(0.2f, Mathf.Max(0.3f, catChaseBaseLeadDistance), distanceToTarget);
        float phase = ResolveCatChasePhase(slot);
        float speedPulse = 0.92f + Mathf.Sin(phase * 0.73f + 0.9f) * 0.12f;
        float chaseSpeed = Mathf.Lerp(catChaseMinSpeed, Mathf.Max(catChaseMinSpeed, catChaseMaxSpeed), speedT) * speedPulse;

        Vector3 previousPosition = transform.position;
        Vector3 nextPosition = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            chaseSpeed * Time.deltaTime);

        Vector3 forward = ResolveHorizontalForward(catTransform);
        Vector3 catToButterfly = nextPosition - catTransform.position;
        catToButterfly.y = 0f;
        float leadDistance = Vector3.Dot(catToButterfly, forward);
        if (leadDistance < Mathf.Max(0.1f, catChaseNearLeadDistance))
        {
            float recoverT = 1f - Mathf.Exp(-catChaseRecoverySharpness * Time.deltaTime);
            nextPosition = Vector3.Lerp(nextPosition, targetPosition, recoverT);
        }

        transform.position = nextPosition;
        RotateTowardMovement(nextPosition - previousPosition, targetPosition - nextPosition);
        return true;
    }

    private Vector3 ResolveCatChaseTarget(Transform catTransform, int slot)
    {
        Vector3 forward = ResolveHorizontalForward(catTransform);
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        if (right.sqrMagnitude < 0.0001f)
        {
            right = Vector3.right;
        }

        right.Normalize();

        float lateralSlot = slot == 0 ? -1f : (slot == 1 ? 0.15f : 1f);
        float heightSlot = slot == 0 ? -0.2f : (slot == 1 ? 0.55f : 0.18f);
        float leadSlot = slot == 0 ? -0.25f : (slot == 1 ? 0.55f : 0.1f);
        float phase = ResolveCatChasePhase(slot);
        float leadPulse =
            Mathf.Sin(phase) * catChaseLeadPulseAmplitude +
            Mathf.Sin(phase * 0.43f + 1.7f) * catChaseLeadPulseAmplitude * 0.35f;
        float leadDistance = Mathf.Clamp(
            catChaseBaseLeadDistance + leadSlot + leadPulse,
            Mathf.Max(0.1f, catChaseNearLeadDistance),
            Mathf.Max(catChaseNearLeadDistance + 0.1f, catChaseFarLeadDistance));
        float lateralDrift = Mathf.Sin(phase * 1.31f + slot) * catChaseJitterRadius;
        float heightDrift = Mathf.Cos(phase * 1.17f + slot * 0.7f) * catChaseJitterRadius * 0.65f;

        return catTransform.position +
               forward * leadDistance +
               right * (lateralSlot * catChaseLateralSpacing + lateralDrift) +
               Vector3.up * (catRoutePointHeightOffset + heightSlot * catChaseHeightSpacing + heightDrift);
    }

    private int ResolveCatChaseFormationSlot()
    {
        if (catChaseFormationSlot >= 0)
        {
            return Mathf.Abs(catChaseFormationSlot) % 3;
        }

        int seed = Mathf.Abs(Mathf.RoundToInt(initialPosition.x * 11.7f + initialPosition.z * 23.3f));
        return seed % 3;
    }

    private float ResolveCatChasePhase(int slot)
    {
        return Time.time * Mathf.Max(0f, catChaseLeadPulseFrequency) * Mathf.PI * 2f +
               slot * 2.13f +
               idleOrbitPhaseOffset * Mathf.Deg2Rad;
    }

    private Vector3 ResolveHorizontalForward(Transform reference)
    {
        Vector3 forward = reference != null ? reference.forward : transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = transform.forward;
            forward.y = 0f;
        }

        if (forward.sqrMagnitude < 0.0001f)
        {
            return Vector3.forward;
        }

        return forward.normalized;
    }

    private void RotateTowardMovement(Vector3 movementDirection, Vector3 fallbackDirection)
    {
        Vector3 direction = movementDirection.sqrMagnitude > 0.0001f ? movementDirection : fallbackDirection;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            rotateSpeed * Time.deltaTime);
    }

    private void ReturnToInitialPoint()
    {
        if (initialPoseCached)
        {
            transform.SetPositionAndRotation(initialPosition, initialRotation);
        }

        currentPointIndex = 0;
        hideTimer = 0f;
        waitForCatTimer = 0f;
        SetRenderersVisible(true);
        state = FlightState.Idle;
    }

    private void UpdateIdleCueOrbit()
    {
        if (!orbitAroundInitialPointWhenIdle || !initialPoseCached)
        {
            return;
        }

        float radius = Mathf.Max(0f, idleOrbitRadius);
        float angle = (Time.time * Mathf.Max(0f, idleOrbitDegreesPerSecond) + idleOrbitPhaseOffset) * Mathf.Deg2Rad;
        float bobPhase = Time.time * Mathf.Max(0f, idleOrbitBobFrequency) * Mathf.PI * 2f + idleOrbitPhaseOffset * Mathf.Deg2Rad;

        Vector3 offset = new Vector3(
            Mathf.Cos(angle) * radius,
            Mathf.Sin(bobPhase) * Mathf.Max(0f, idleOrbitHeight),
            Mathf.Sin(angle) * radius);
        transform.position = initialPosition + offset;

        if (!faceIdleOrbitDirection || radius <= 0.001f)
        {
            return;
        }

        Vector3 tangent = new Vector3(-Mathf.Sin(angle), 0f, Mathf.Cos(angle));
        if (tangent.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(tangent.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            rotateSpeed * Time.deltaTime);
    }

    private Transform ResolveCatTransform()
    {
        if (catController != null)
        {
            return catController.transform;
        }

#if UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
        catController = FindAnyObjectByType<CatRideControllerV2>(FindObjectsInactive.Exclude);
#else
#pragma warning disable CS0618
        catController = FindObjectOfType<CatRideControllerV2>();
#pragma warning restore CS0618
#endif

        return catController != null ? catController.transform : null;
    }

    private void CacheRenderers()
    {
        if (renderersCached)
        {
            return;
        }

        cachedRenderers = GetComponentsInChildren<Renderer>(includeInactive: true);
        renderersCached = true;
    }

    private void SetRenderersVisible(bool visible)
    {
        if (cachedRenderers == null)
        {
            return;
        }

        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            if (cachedRenderers[i] != null)
            {
                cachedRenderers[i].enabled = visible;
            }
        }
    }

    private bool TryGetCurrentTargetPosition(out Vector3 position)
    {
        if (useCatAutoRoutePoints)
        {
            IReadOnlyList<Transform> routePoints = ResolveCatRoutePoints();
            if (routePoints != null)
            {
                while (currentPointIndex < routePoints.Count)
                {
                    Transform point = routePoints[currentPointIndex];
                    if (point != null)
                    {
                        position = point.position + Vector3.up * catRoutePointHeightOffset;
                        return true;
                    }

                    currentPointIndex++;
                }
            }

            position = default;
            return false;
        }

        while (currentPointIndex < flightPoints.Count)
        {
            Transform point = flightPoints[currentPointIndex];
            if (point != null)
            {
                position = point.position;
                return true;
            }

            currentPointIndex++;
        }

        position = default;
        return false;
    }

    private IReadOnlyList<Transform> ResolveCatRoutePoints()
    {
        if (ResolveCatTransform() == null)
        {
            return null;
        }

        return catController.AutoRoutePoints;
    }

    private void CacheInitialPose()
    {
        if (initialPoseCached)
        {
            return;
        }

        initialPosition = transform.position;
        initialRotation = transform.rotation;
        idleOrbitPhaseOffset = Mathf.Repeat((initialPosition.x * 37.1f + initialPosition.z * 19.7f) * 13.37f, 360f);
        initialPoseCached = true;
    }
}
