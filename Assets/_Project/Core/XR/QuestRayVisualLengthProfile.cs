using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;

/// <summary>
/// Globally re-tunes every <see cref="CurveVisualController"/> in the scene so the controller
/// ray is short and subtle by default and only extends out when an interactable is hovered.
///
/// Defaults are chosen for a Quest-style "feels alive" experience:
///   • Idle line length: 0.18 m (just a hint that the controller has a pointer)
///   • Max extension: 6 m (clamped so empty-space hits never look like a tunnel of light)
///   • Smooth extend / retract animation (~0.12 s) so the line breathes in and out
///
/// Features that want to override behaviour for a single frame can call
/// <see cref="ReportHover"/> with the hit distance — the profile clamps the line to that
/// distance + padding so the visual lands right on the object.
///
/// The profile is auto-spawned after the first scene load and lives across scenes.
/// </summary>
[DefaultExecutionOrder(9000)]
public sealed class QuestRayVisualLengthProfile : MonoBehaviour
{
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
    [Tooltip("Re-scan the scene for CurveVisualControllers this often. Short interval catches controllers that spawn later.")]
    [SerializeField] private float rescanInterval = 0.25f;
    [SerializeField] private bool includeInactive = true;
    [SerializeField] private bool logScanOnce = false;

    private static int rightHoverFrame = -1;
    private static int leftHoverFrame = -1;
    private static float rightHoverDistance;
    private static float leftHoverDistance;

    private readonly List<CurveVisualController> cachedCurveVisuals = new List<CurveVisualController>(16);
    private float nextRefreshTime;
    private bool didLogScan;

    /// <summary>
    /// Feature scripts call this each frame they detect a custom interactable hit (something the
    /// XRI provider doesn't know about). The profile uses the reported distance to size the line.
    /// </summary>
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
        // Force a rescan immediately so a freshly-loaded XR rig gets tuned on its first frame
        // instead of waiting up to a rescanInterval.
        nextRefreshTime = 0f;
        RefreshCache();
        ApplyProfile();
    }

    private void LateUpdate()
    {
        if (Time.unscaledTime >= nextRefreshTime)
        {
            nextRefreshTime = Time.unscaledTime + Mathf.Max(0.05f, rescanInterval);
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
        for (int i = 0; i < cachedCurveVisuals.Count; i++)
        {
            CurveVisualController visual = cachedCurveVisuals[i];
            if (visual == null)
            {
                continue;
            }

            ResolveTargetState(visual, out float targetLength, out bool reportedCustomHover, out bool providerHasInteractiveHit);

            float idle = ResolveIdleLength();
            visual.restingVisualLineLength = idle;
            visual.maxVisualCurveDistance = targetLength;
            visual.lineDynamicsMode = LineDynamicsMode.RetractOnHitLoss;
            visual.retractDelay = Mathf.Max(0f, retractDelay);
            visual.retractDuration = Mathf.Max(0f, retractDuration);
            visual.extensionRate = Mathf.Clamp(extensionRate, 0f, 30f);
            // Stop the line from extending into empty space — empty hits should keep it short.
            // The only exception is when a feature explicitly reported a custom hover but the
            // XRI provider hasn't caught up; in that one-frame case we let the line extend so the
            // visual still reaches the custom hit point.
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

        // Allow a one-frame lag tolerance so the line stays extended even if the feature is one
        // frame behind the visual update tick.
        bool rightFresh = rightHand && (frame - rightHoverFrame) <= 1 && rightHoverFrame >= 0;
        bool leftFresh = leftHand && (frame - leftHoverFrame) <= 1 && leftHoverFrame >= 0;

        if (rightFresh)
        {
            reportedCustomHover = true;
            targetLength = Mathf.Clamp(rightHoverDistance + Mathf.Max(0f, interactableLengthPadding), idleLength, maxLength);
        }
        else if (leftFresh)
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

        // Only treat real interactable hits as "extend" triggers — empty raycast hits should
        // leave the line short. That's the entire point of this profile.
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
