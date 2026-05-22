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
    [SerializeField] private List<Transform> flightPoints = new List<Transform>();

    [Header("Motion")]
    [SerializeField] private float flightSpeed = 2.5f;
    [SerializeField] private float rotateSpeed = 360f;
    [SerializeField] private float reachDistance = 0.08f;
    [SerializeField] private bool resetToStartOnBegin = true;

    [Header("Return After Cat Approach")]
    [SerializeField] private CatRideControllerV2 catController;
    [SerializeField, Min(0.01f)] private float catApproachDistance = 1.5f;
    [SerializeField, Min(0f)] private float hiddenDurationBeforeReappear = 0.25f;

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
    private float lastWaitLogTime;

    private void Awake()
    {
        CacheInitialPose();
        CacheRenderers();
    }

    private void Update()
    {
        switch (state)
        {
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
        lastWaitLogTime = 0f;
        state = FlightState.Flying;

        if (debugLogs)
        {
            string catRefInfo = catController != null ? catController.name : "<null, will auto-find>";
            Debug.Log($"[ButterflyFlightControllerV2] Flight started. name={name} catRef={catRefInfo}");
        }
    }

    private void UpdateFlight()
    {
        Transform targetPoint = GetCurrentPoint();
        if (targetPoint == null)
        {
            state = FlightState.WaitingForCat;

            if (debugLogs)
            {
                Debug.Log("[ButterflyFlightControllerV2] Reached final point, waiting for cat to approach.");
            }

            return;
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPoint.position,
            flightSpeed * Time.deltaTime);

        Vector3 direction = targetPoint.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotateSpeed * Time.deltaTime);
        }

        if (Vector3.Distance(transform.position, targetPoint.position) <= reachDistance)
        {
            currentPointIndex++;
        }
    }

    private void UpdateWaitForCat()
    {
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

        if (initialPoseCached)
        {
            transform.SetPositionAndRotation(initialPosition, initialRotation);
        }

        SetRenderersVisible(true);
        state = FlightState.Idle;

        if (debugLogs)
        {
            Debug.Log("[ButterflyFlightControllerV2] Returned to spawn, ready for next trigger.");
        }
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

    private Transform GetCurrentPoint()
    {
        while (currentPointIndex < flightPoints.Count)
        {
            Transform point = flightPoints[currentPointIndex];
            if (point != null)
            {
                return point;
            }

            currentPointIndex++;
        }

        return null;
    }

    private void CacheInitialPose()
    {
        if (initialPoseCached)
        {
            return;
        }

        initialPosition = transform.position;
        initialRotation = transform.rotation;
        initialPoseCached = true;
    }
}
