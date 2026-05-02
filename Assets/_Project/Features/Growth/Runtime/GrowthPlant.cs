using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GrowthPlant : MonoBehaviour
{
    [System.Serializable]
    public class PlantPartBinding
    {
        public string partName;
        public Transform target;
    }

    [Header("Config")]
    [SerializeField] private GrowthProfile_SO growthProfile;
    [SerializeField] private PlantPartBinding[] bindings;
    [SerializeField] private float growthDuration = 1.0f;
    [SerializeField] private AnimationCurve growthCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private string stemPartName = "Stem";
    [SerializeField] private string bloomPartName = "Bloom";
    [SerializeField] private string bloomAnchorName = "BloomAnchor";
    [SerializeField] private Transform bloomAnchor;
    [SerializeField] private bool useBloomAnchorPosition = true;
    [SerializeField] private bool hideBloomUntilReveal = true;
    [SerializeField] [Range(0f, 1f)] private float bloomRevealThreshold = 0.22f;
    [SerializeField] [Range(0.01f, 0.5f)] private float bloomRevealWindow = 0.18f;
    [SerializeField] [Range(0f, 0.5f)] private float bloomRevealStartScale = 0.06f;
    [SerializeField] [Range(0f, 0.75f)] private float bloomDelay = 0.2f;
    [SerializeField] [Range(0f, 0.2f)] private float overshootAmount = 0.05f;
    [SerializeField] [Range(0f, 0.5f)] private float overshootWindow = 0.12f;
    [SerializeField] private bool enableStemWobble = true;
    [SerializeField] [Range(0f, 12f)] private float stemWobbleAngle = 4f;
    [SerializeField] [Range(0f, 20f)] private float stemWobbleFrequency = 7f;
    [SerializeField] [Range(0f, 1f)] private float stemWobbleJitter = 0.6f;
    [SerializeField] [Range(0f, 1f)] private float stemWobbleStart = 0.08f;
    [SerializeField] [Range(0f, 1f)] private float stemWobbleEnd = 0.82f;
    [SerializeField] private bool playOnStart;

    [Header("Debug")]
    [SerializeField] [Range(0f, 1f)] private float currentGrowthTime;

    [Header("Traversal Blocking")]
    [SerializeField] private float blockActivationTime = 0.5f;
    [SerializeField] private Collider[] collidersToEnableOnGrowth;
    private bool blockApplied;


    private float targetGrowthTime;
    private bool isTransitioning;
    private float runtimeScaleMultiplier = 1f;
    private float runtimeDurationMultiplier = 1f;
    private float runtimeWobbleMultiplier = 1f;
    private readonly System.Collections.Generic.Dictionary<Transform, Renderer[]> cachedRenderers = new();

    public GrowthProfile_SO Profile => growthProfile;
    public float CurrentGrowthTime => currentGrowthTime;
    public float TargetGrowthTime => targetGrowthTime;

    private void Awake()
    {
        AutoAssignProfile();
        AutoAssignBindings();
        AutoAssignBloomAnchor();
    }

    private void Start()
    {
        targetGrowthTime = currentGrowthTime;
        ApplyGrowth(currentGrowthTime);
        ApplyTraversalBlocking();

        if (playOnStart)
        {
            AdvanceStage();
        }
    }

    private void Update()
    {
        if (!isTransitioning)
        {
            return;
        }

        currentGrowthTime = Mathf.MoveTowards(
            currentGrowthTime,
            targetGrowthTime,
            Time.deltaTime / Mathf.Max(0.0001f, growthDuration * runtimeDurationMultiplier));

        ApplyGrowth(currentGrowthTime);
        ApplyTraversalBlocking();

        if (Mathf.Approximately(currentGrowthTime, targetGrowthTime))
        {
            currentGrowthTime = targetGrowthTime;
            isTransitioning = false;
            ApplyGrowth(currentGrowthTime);
            ResetCompletedStemRotation();
        }
    }

    public void AdvanceStage()
    {
        float[] stageTimes = GetStageTimes();
        if (stageTimes.Length == 0)
        {
            return;
        }

        for (int i = 0; i < stageTimes.Length; i++)
        {
            if (stageTimes[i] > currentGrowthTime + 0.001f)
            {
                targetGrowthTime = stageTimes[i];
                isTransitioning = true;
                return;
            }
        }

        targetGrowthTime = stageTimes[stageTimes.Length - 1];
        isTransitioning = true;
    }

    public void RegressStage()
    {
        float[] stageTimes = GetStageTimes();
        if (stageTimes.Length == 0)
        {
            return;
        }

        for (int i = stageTimes.Length - 1; i >= 0; i--)
        {
            if (stageTimes[i] < currentGrowthTime - 0.001f)
            {
                targetGrowthTime = stageTimes[i];
                isTransitioning = true;
                return;
            }
        }

        targetGrowthTime = stageTimes[0];
        isTransitioning = true;
    }

    public void SetGrowthTimeImmediate(float value)
    {
        currentGrowthTime = Mathf.Clamp01(value);
        targetGrowthTime = currentGrowthTime;
        isTransitioning = false;
        ApplyGrowth(currentGrowthTime);
    }

    public bool IsTransitioning()
    {
        return isTransitioning;
    }

    public void GrowToFull()
    {
        targetGrowthTime = 1f;
        isTransitioning = true;
    }

    public void ShrinkToSeed()
    {
        targetGrowthTime = 0f;
        isTransitioning = true;
    }

    public void ConfigureRuntimeVariation(float scaleMultiplier, float durationMultiplier, float wobbleMultiplier)
    {
        runtimeScaleMultiplier = Mathf.Max(0.05f, scaleMultiplier);
        runtimeDurationMultiplier = Mathf.Max(0.05f, durationMultiplier);
        runtimeWobbleMultiplier = Mathf.Max(0f, wobbleMultiplier);
        ApplyGrowth(currentGrowthTime);
    }

    public void ResetRuntimeVariation()
    {
        ConfigureRuntimeVariation(1f, 1f, 1f);
    }

    private void AutoAssignProfile()
    {
        if (growthProfile != null)
        {
            return;
        }

#if UNITY_EDITOR
        growthProfile = AssetDatabase.LoadAssetAtPath<GrowthProfile_SO>(
            "Assets/_Project/Features/Growth/ScriptableObjects/GrowthProfile_SO.asset");
#endif
    }

    private void AutoAssignBindings()
    {
        if (bindings != null && bindings.Length > 0)
        {
            return;
        }

        Transform stem = FindChildRecursive(transform, "Stem");
        Transform bloom = FindChildRecursive(transform, "Bloom");

        int count = 0;
        if (stem != null) count++;
        if (bloom != null) count++;

        if (count == 0)
        {
            return;
        }

        bindings = new PlantPartBinding[count];
        int index = 0;

        if (stem != null)
        {
            bindings[index++] = new PlantPartBinding
            {
                partName = "Stem",
                target = stem
            };
        }

        if (bloom != null)
        {
            bindings[index] = new PlantPartBinding
            {
                partName = "Bloom",
                target = bloom
            };
        }
    }

    private void AutoAssignBloomAnchor()
    {
        if (bloomAnchor != null || string.IsNullOrWhiteSpace(bloomAnchorName))
        {
            return;
        }

        bloomAnchor = FindChildRecursive(transform, bloomAnchorName);
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

    private float[] GetStageTimes()
    {
        if (growthProfile == null || growthProfile.Parts == null || growthProfile.Parts.Length == 0)
        {
            return System.Array.Empty<float>();
        }

        GrowthProfile_SO.PartProfile referencePart = growthProfile.Parts[0];
        if (referencePart == null || referencePart.states == null || referencePart.states.Length == 0)
        {
            return System.Array.Empty<float>();
        }

        float[] times = new float[referencePart.states.Length];
        for (int i = 0; i < referencePart.states.Length; i++)
        {
            times[i] = referencePart.states[i].time;
        }

        return times;
    }

    private void ApplyGrowth(float growthTime)
    {
        if (growthProfile == null || growthProfile.Parts == null)
        {
            return;
        }

        growthTime = Mathf.Clamp01(growthTime);

        foreach (var part in growthProfile.Parts)
        {
            if (part == null || part.states == null || part.states.Length == 0)
            {
                continue;
            }

            Transform target = FindTarget(part.partName);
            if (target == null)
            {
                continue;
            }

            float partGrowthTime = ResolvePartGrowthTime(part.partName, growthTime);
            ApplyInterpolatedState(target, part.partName, part.states, partGrowthTime, growthTime);
            UpdatePartVisibility(target, part.partName, growthTime);
        }
    }

    private float ResolvePartGrowthTime(string partName, float growthTime)
    {
        if (!string.IsNullOrWhiteSpace(bloomPartName) && partName == bloomPartName)
        {
            float delayedRange = Mathf.Max(0.0001f, 1f - bloomDelay);
            return Mathf.Clamp01((growthTime - bloomDelay) / delayedRange);
        }

        return growthTime;
    }

    private void UpdatePartVisibility(Transform target, string partName, float growthProgress)
    {
        if (!hideBloomUntilReveal ||
            string.IsNullOrWhiteSpace(bloomPartName) ||
            partName != bloomPartName ||
            target == null)
        {
            return;
        }

        float revealT = Mathf.InverseLerp(
            bloomRevealThreshold,
            bloomRevealThreshold + Mathf.Max(0.0001f, bloomRevealWindow),
            growthProgress);
        bool shouldShow = revealT > 0.001f;
        Renderer[] renderers = GetCachedRenderers(target);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].enabled = shouldShow;
            }
        }
    }

    private Renderer[] GetCachedRenderers(Transform target)
    {
        if (target == null)
        {
            return System.Array.Empty<Renderer>();
        }

        if (!cachedRenderers.TryGetValue(target, out Renderer[] renderers) || renderers == null)
        {
            renderers = target.GetComponentsInChildren<Renderer>(includeInactive: true);
            cachedRenderers[target] = renderers;
        }

        return renderers;
    }

    private Vector3 ResolvePartScale(string partName, Vector3 baseScale, float partGrowthTime)
    {
        if (!hideBloomUntilReveal ||
            string.IsNullOrWhiteSpace(bloomPartName) ||
            partName != bloomPartName)
        {
            return baseScale;
        }

        float revealT = Mathf.InverseLerp(
            bloomRevealThreshold,
            bloomRevealThreshold + Mathf.Max(0.0001f, bloomRevealWindow),
            partGrowthTime);
        float revealScale = Mathf.SmoothStep(
            bloomRevealStartScale,
            1f,
            Mathf.Clamp01(revealT));

        return baseScale * revealScale;
    }

    private Transform FindTarget(string partName)
    {
        if (bindings == null)
        {
            return null;
        }

        foreach (var binding in bindings)
        {
            if (binding != null && binding.partName == partName)
            {
                return binding.target;
            }
        }

        return null;
    }

    private void ApplyInterpolatedState(Transform target, string partName, GrowthProfile_SO.PartState[] states, float growthTime, float overallGrowthTime)
    {
        if (states.Length == 1)
        {
            ApplyState(target, partName, states[0], overallGrowthTime);
            return;
        }

        if (growthTime <= states[0].time)
        {
            ApplyState(target, partName, states[0], overallGrowthTime);
            return;
        }

        if (growthTime >= states[states.Length - 1].time)
        {
            ApplyState(target, partName, states[states.Length - 1], overallGrowthTime);
            return;
        }

        GrowthProfile_SO.PartState fromState = states[0];
        GrowthProfile_SO.PartState toState = states[states.Length - 1];

        for (int i = 0; i < states.Length - 1; i++)
        {
            if (growthTime >= states[i].time && growthTime <= states[i + 1].time)
            {
                fromState = states[i];
                toState = states[i + 1];
                break;
            }
        }

        float range = Mathf.Max(0.0001f, toState.time - fromState.time);
        float linearT = Mathf.Clamp01((growthTime - fromState.time) / range);
        float t = growthCurve != null ? growthCurve.Evaluate(linearT) : linearT;
        t = ApplyOvershoot(t);

        Vector3 interpolatedScale = Vector3.Lerp(fromState.localScale, toState.localScale, t) * runtimeScaleMultiplier;
        target.localScale = ResolvePartScale(partName, interpolatedScale, overallGrowthTime);

        Vector3 interpolatedPosition = Vector3.Lerp(fromState.localPosition, toState.localPosition, t);
        target.localPosition = ResolvePartPosition(partName, interpolatedPosition);
        target.localRotation = ResolvePartRotation(partName, growthTime);
    }

    private void ApplyState(Transform target, string partName, GrowthProfile_SO.PartState state, float overallGrowthTime)
    {
        Vector3 stateScale = state.localScale * runtimeScaleMultiplier;
        target.localScale = ResolvePartScale(partName, stateScale, overallGrowthTime);
        target.localPosition = ResolvePartPosition(partName, state.localPosition);
        target.localRotation = ResolvePartRotation(partName, currentGrowthTime);
    }

    private Vector3 ResolvePartPosition(string partName, Vector3 statePosition)
    {
        if (useBloomAnchorPosition &&
            bloomAnchor != null &&
            !string.IsNullOrWhiteSpace(bloomPartName) &&
            partName == bloomPartName)
        {
            Vector3 anchorPositionInPlantSpace = transform.InverseTransformPoint(bloomAnchor.position);
            return anchorPositionInPlantSpace + statePosition;
        }

        return statePosition;
    }

    private float ApplyOvershoot(float t)
    {
        t = Mathf.Clamp01(t);

        if (overshootAmount <= 0.0001f || overshootWindow <= 0.0001f)
        {
            return t;
        }

        float start = 1f - overshootWindow;
        if (t <= start)
        {
            return t;
        }

        float normalized = Mathf.InverseLerp(start, 1f, t);
        float overshoot = Mathf.Sin(normalized * Mathf.PI) * overshootAmount;
        return t + overshoot;
    }

    private Quaternion ResolvePartRotation(string partName, float growthTime)
    {
        if (!enableStemWobble ||
            !isTransitioning ||
            string.IsNullOrWhiteSpace(stemPartName) ||
            partName != stemPartName)
        {
            return Quaternion.identity;
        }

        if (growthTime <= stemWobbleStart || growthTime >= stemWobbleEnd)
        {
            return Quaternion.identity;
        }

        float normalizedWindow = Mathf.InverseLerp(stemWobbleStart, stemWobbleEnd, growthTime);
        float envelope = Mathf.Sin(normalizedWindow * Mathf.PI);
        float timePhase = Time.time * Mathf.Max(0.01f, stemWobbleFrequency);
        float noiseX = Mathf.PerlinNoise(timePhase, 1.37f) * 2f - 1f;
        float noiseZ = Mathf.PerlinNoise(2.91f, timePhase * 0.93f) * 2f - 1f;
        float tremorX = Mathf.Sin(timePhase * 1.73f + 0.4f) * (1f - stemWobbleJitter);
        float tremorZ = Mathf.Sin(timePhase * 2.11f + 1.2f) * (1f - stemWobbleJitter) * 0.7f;
        float wobbleAngle = stemWobbleAngle * runtimeWobbleMultiplier;
        float xAngle = (noiseX * stemWobbleJitter + tremorX) * wobbleAngle * envelope;
        float zAngle = (noiseZ * stemWobbleJitter + tremorZ) * wobbleAngle * 0.7f * envelope;
        return Quaternion.Euler(xAngle, 0f, zAngle);
    }

    private void ResetCompletedStemRotation()
    {
        if (bindings == null || string.IsNullOrWhiteSpace(stemPartName))
        {
            return;
        }

        foreach (var binding in bindings)
        {
            if (binding != null && binding.partName == stemPartName && binding.target != null)
            {
                binding.target.localRotation = Quaternion.identity;
            }
        }
    }

    private void ApplyTraversalBlocking()
{
    if (collidersToEnableOnGrowth == null)
    {
        return;
    }

    bool shouldBlock = currentGrowthTime >= blockActivationTime;
    if (shouldBlock == blockApplied)
    {
        return;
    }

    blockApplied = shouldBlock;
    foreach (var col in collidersToEnableOnGrowth)
    {
        if (col != null)
        {
            col.enabled = shouldBlock;
        }
    }
}

}
