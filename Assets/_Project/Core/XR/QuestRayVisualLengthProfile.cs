using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;

[DefaultExecutionOrder(9000)]
public sealed class QuestRayVisualLengthProfile : MonoBehaviour
{
    [SerializeField] private float maxInteractionRayDistance = 7f;
    [SerializeField] private float idleRayLength = 0.32f;
    [SerializeField] private float interactableLengthPadding = 0.08f;
    [SerializeField] private bool includeInactive = true;

    private static int rightHoverFrame = -1;
    private static int leftHoverFrame = -1;
    private static float rightHoverDistance;
    private static float leftHoverDistance;

    private CurveVisualController[] cachedCurveVisuals = new CurveVisualController[0];
    private float nextRefreshTime;

    public static void ReportHover(bool rightHand, bool hovering, float distance)
    {
        if (!hovering)
        {
            return;
        }

        float safeDistance = Mathf.Max(0.1f, distance);
        if (rightHand)
        {
            rightHoverFrame = Time.frameCount;
            rightHoverDistance = safeDistance;
        }
        else
        {
            leftHoverFrame = Time.frameCount;
            leftHoverDistance = safeDistance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureRuntimeInstance()
    {
        if (FindFirstObjectByType<QuestRayVisualLengthProfile>() != null)
        {
            return;
        }

        GameObject profileObject = new GameObject("QuestRayVisualLengthProfile");
        DontDestroyOnLoad(profileObject);
        profileObject.AddComponent<QuestRayVisualLengthProfile>();
    }

    private void Awake()
    {
        RefreshCache();
        ApplyProfile();
    }

    private void OnEnable()
    {
        RefreshCache();
        ApplyProfile();
    }

    private void LateUpdate()
    {
        if (Time.unscaledTime >= nextRefreshTime)
        {
            nextRefreshTime = Time.unscaledTime + 1f;
            RefreshCache();
        }

        ApplyProfile();
    }

    private void RefreshCache()
    {
        cachedCurveVisuals = FindObjectsByType<CurveVisualController>(
            includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);
    }

    private void ApplyProfile()
    {
        for (int i = 0; i < cachedCurveVisuals.Length; i++)
        {
            CurveVisualController visual = cachedCurveVisuals[i];
            if (visual == null || ShouldSkip(visual.gameObject))
            {
                continue;
            }

            ResolveTargetState(visual, out float targetLength, out bool reportedCustomHover, out bool providerHasInteractiveHit);

            visual.restingVisualLineLength = ResolveIdleLength();
            visual.maxVisualCurveDistance = targetLength;
            visual.lineDynamicsMode = LineDynamicsMode.RetractOnHitLoss;
            visual.retractDelay = 0f;
            visual.retractDuration = 0f;
            visual.extensionRate = 0f;
            visual.extendLineToEmptyHit = reportedCustomHover && !providerHasInteractiveHit;
        }
    }

    private void ResolveTargetState(
        CurveVisualController visual,
        out float targetLength,
        out bool reportedCustomHover,
        out bool providerHasInteractiveHit)
    {
        float maxLength = Mathf.Max(1f, maxInteractionRayDistance);
        float idleLength = ResolveIdleLength();
        targetLength = idleLength;
        reportedCustomHover = false;
        providerHasInteractiveHit = false;

        if (visual == null)
        {
            return;
        }

        GameObject lineVisualObject = visual.gameObject;
        bool rightHand = IsRightHand(lineVisualObject);
        bool leftHand = IsLeftHand(lineVisualObject);
        int frame = Time.frameCount;

        if (rightHand && rightHoverFrame == frame)
        {
            reportedCustomHover = true;
            targetLength = Mathf.Clamp(rightHoverDistance + Mathf.Max(0f, interactableLengthPadding), idleLength, maxLength);
        }
        else if (leftHand && leftHoverFrame == frame)
        {
            reportedCustomHover = true;
            targetLength = Mathf.Clamp(leftHoverDistance + Mathf.Max(0f, interactableLengthPadding), idleLength, maxLength);
        }

        if (TryResolveProviderInteractiveDistance(visual, out float providerDistance))
        {
            providerHasInteractiveHit = true;
            targetLength = Mathf.Clamp(
                Mathf.Max(targetLength, providerDistance + Mathf.Max(0f, interactableLengthPadding)),
                idleLength,
                maxLength);
        }
    }

    private float ResolveIdleLength()
    {
        return Mathf.Clamp(idleRayLength, 0.08f, Mathf.Max(0.1f, maxInteractionRayDistance));
    }

    private static bool TryResolveProviderInteractiveDistance(CurveVisualController visual, out float distance)
    {
        distance = 0f;
        if (visual == null)
        {
            return false;
        }

        ICurveInteractionDataProvider provider = visual.curveInteractionDataProvider;
        if (provider == null || !provider.isActive)
        {
            return false;
        }

        EndPointType endpointType = provider.TryGetCurveEndPoint(
            out Vector3 endPoint,
            visual.snapToSelectedAttachIfAvailable,
            visual.snapToSnapVolumeIfAvailable);

        bool interactiveEndpoint = endpointType == EndPointType.ValidCastHit
            || endpointType == EndPointType.AttachPoint
            || endpointType == EndPointType.UI;

        if (!interactiveEndpoint)
        {
            return false;
        }

        Transform origin = visual.lineOriginTransform != null ? visual.lineOriginTransform : provider.curveOrigin;
        if (origin == null)
        {
            return false;
        }

        distance = Vector3.Distance(origin.position, endPoint);
        return distance > 0.001f;
    }

    private static bool IsRightHand(GameObject candidate)
    {
        return HierarchyNameContains(candidate, "right");
    }

    private static bool IsLeftHand(GameObject candidate)
    {
        return HierarchyNameContains(candidate, "left");
    }

    private static bool HierarchyNameContains(GameObject candidate, string token)
    {
        Transform current = candidate != null ? candidate.transform : null;
        while (current != null)
        {
            if (current.name.ToLowerInvariant().Contains(token))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private static bool ShouldSkip(GameObject candidate)
    {
        if (candidate == null)
        {
            return true;
        }

        Transform current = candidate.transform;
        while (current != null)
        {
            string lowerName = current.name.ToLowerInvariant();
            if (lowerName.Contains("teleport") || lowerName.Contains("gaze"))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }
}
