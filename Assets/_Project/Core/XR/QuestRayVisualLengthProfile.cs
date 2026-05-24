using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;

/// <summary>
/// Globally re-tunes every <see cref="CurveVisualController"/> in the scene so controller rays
/// stay short by default and extend only when an interactable is hovered.
///
/// Defaults are chosen for a Quest-style "feels alive" experience:
///   - Idle line length: 0.18 m.
///   - Max extension: 6 m.
///   - Smooth extend and retract animation around 0.12 s.
///
/// Feature scripts can call <see cref="ReportHover"/> with the hit distance. The profile clamps
/// the line to that distance plus padding so the visual lands on the object.
/// </summary>
[DefaultExecutionOrder(9000)]
public sealed class QuestRayVisualLengthProfile : MonoBehaviour
{
    private const float MinimumRescanInterval = 0.5f;

    [Header("Length")]
    [Tooltip("Hard ceiling on how far the ray can ever extend, even when hovering something far away. Keep this short to avoid a 'laser sword' look.")]
    [SerializeField] private float maxInteractionRayDistance = 6f;
    [Tooltip("Resting length when no interactable is being hovered. Keep this small but visible (~0.15m) so the player can still see a pointer hint.")]
    [SerializeField] private float idleRayLength = 0.18f;
    [Tooltip("Extra distance added past the hovered hit point so the visual lands on top of the target instead of stopping just short.")]
    [SerializeField] private float interactableLengthPadding = 0.06f;

    [Header("Animation")]
    [Tooltip("Time (seconds) the line takes to retract when it loses its hover. 0 = snap. ~0.12 feels organic on Quest.")]
    [SerializeField] private float retractDuration = 0.12f;
    [Tooltip("Delay before retraction kicks in after the hover is lost.")]
    [SerializeField] private float retractDelay = 0.05f;
    [Tooltip("Extension rate (0..30). 0 = instant snap to hit length, higher = quicker animated growth.")]
    [SerializeField] private float extensionRate = 16f;

    [Header("Scan")]
    [Tooltip("Re-scan the scene for CurveVisualControllers this often. Scene loads still force an immediate scan.")]
    [SerializeField] private float rescanInterval = 1.25f;
    [SerializeField] private bool includeInactive = true;
    [SerializeField] private bool logScanOnce = false;

    private static int rightHoverFrame = -1;
    private static int leftHoverFrame = -1;
    private static float rightHoverDistance;
    private static float leftHoverDistance;

    private readonly List<CurveVisualController> cachedCurveVisuals = new List<CurveVisualController>(16);
    private float nextRefreshTime;
    private bool didLogScan;

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
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        nextRefreshTime = 0f;
        RefreshCache();
        ApplyProfile();
    }

    private void LateUpdate()
    {
        if (Time.unscaledTime >= nextRefreshTime)
        {
            nextRefreshTime = Time.unscaledTime + Mathf.Max(MinimumRescanInterval, rescanInterval);
            RefreshCache();
        }

        ApplyProfile();
    }

    private void RefreshCache()
    {
        cachedCurveVisuals.Clear();
        CurveVisualController[] found = FindObjectsByType<CurveVisualController>(
            includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < found.Length; i++)
        {
            CurveVisualController visual = found[i];
            if (visual == null || ShouldSkip(visual.gameObject))
            {
                continue;
            }

            cachedCurveVisuals.Add(visual);
        }

        if (logScanOnce && !didLogScan)
        {
            didLogScan = true;
            Debug.Log($"[QuestRayVisualLengthProfile] Tracking {cachedCurveVisuals.Count} CurveVisualController(s).");
        }
    }

    private void ApplyProfile()
    {
        float idle = ResolveIdleLength();
        float maxLength = Mathf.Max(1f, maxInteractionRayDistance);
        float padding = Mathf.Max(0f, interactableLengthPadding);

        for (int i = 0; i < cachedCurveVisuals.Count; i++)
        {
            CurveVisualController visual = cachedCurveVisuals[i];
            if (visual == null)
            {
                continue;
            }

            ResolveTargetState(visual, idle, maxLength, padding, out float targetLength, out bool reportedCustomHover, out bool providerHasInteractiveHit);

            visual.restingVisualLineLength = idle;
            visual.maxVisualCurveDistance = targetLength;
            visual.lineDynamicsMode = LineDynamicsMode.RetractOnHitLoss;
            visual.retractDelay = Mathf.Max(0f, retractDelay);
            visual.retractDuration = Mathf.Max(0f, retractDuration);
            visual.extensionRate = Mathf.Clamp(extensionRate, 0f, 30f);
            visual.extendLineToEmptyHit = reportedCustomHover && !providerHasInteractiveHit;
        }
    }

    private static void ResolveTargetState(
        CurveVisualController visual,
        float idleLength,
        float maxLength,
        float padding,
        out float targetLength,
        out bool reportedCustomHover,
        out bool providerHasInteractiveHit)
    {
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

        bool rightFresh = rightHand && rightHoverFrame >= 0 && (frame - rightHoverFrame) <= 1;
        bool leftFresh = leftHand && leftHoverFrame >= 0 && (frame - leftHoverFrame) <= 1;

        if (rightFresh)
        {
            reportedCustomHover = true;
            targetLength = Mathf.Clamp(rightHoverDistance + padding, idleLength, maxLength);
        }
        else if (leftFresh)
        {
            reportedCustomHover = true;
            targetLength = Mathf.Clamp(leftHoverDistance + padding, idleLength, maxLength);
        }

        if (TryResolveProviderInteractiveDistance(visual, out float providerDistance))
        {
            providerHasInteractiveHit = true;
            targetLength = Mathf.Clamp(Mathf.Max(targetLength, providerDistance + padding), idleLength, maxLength);
        }
    }

    private float ResolveIdleLength()
    {
        return Mathf.Clamp(idleRayLength, 0.05f, Mathf.Max(0.1f, maxInteractionRayDistance));
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
            if (ContainsOrdinalIgnoreCase(current.name, token))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private static bool ShouldSkip(GameObject candidate)
    {
        Transform current = candidate != null ? candidate.transform : null;
        while (current != null)
        {
            if (ContainsOrdinalIgnoreCase(current.name, "teleport") ||
                ContainsOrdinalIgnoreCase(current.name, "gaze"))
            {
                return true;
            }

            current = current.parent;
        }

        return candidate == null;
    }

    private static bool ContainsOrdinalIgnoreCase(string value, string token)
    {
        return !string.IsNullOrEmpty(value) &&
               !string.IsNullOrEmpty(token) &&
               value.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
