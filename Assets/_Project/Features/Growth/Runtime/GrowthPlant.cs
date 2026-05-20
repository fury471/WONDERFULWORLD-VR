using System.Collections;
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

    private class RuntimeMeshBend
    {
        public MeshFilter filter;
        public Mesh sourceMesh;
        public Mesh runtimeMesh;
        public Vector3[] sourceVertices;
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
    [SerializeField] private bool autoCreateBlockingCollider = true;
    [SerializeField] private Vector3 autoBlockingColliderCenter = new Vector3(0f, 0.85f, 0f);
    [SerializeField, Min(0.01f)] private float autoBlockingColliderRadius = 0.42f;
    [SerializeField, Min(0.01f)] private float autoBlockingColliderHeight = 1.7f;
    [SerializeField] private bool deferBlockingUntilPlayerClear = true;
    [SerializeField, Min(0f)] private float blockingPlayerClearancePadding = 0.12f;
    private bool blockApplied;

    [Header("Stylized Toon Outline")]
    [SerializeField] private bool enableToonOutline = true;
    [SerializeField] private Color toonOutlineColor = Color.black;
    [SerializeField, Range(0.01f, 0.18f)] private float toonOutlineThickness = 0.075f;
    [SerializeField] private bool outlineStem = true;
    [SerializeField] private bool outlineBloom = true;

    [Header("Organic Shape Variation")]
    [SerializeField] private bool enableOrganicShapeVariation = true;
    [SerializeField, Range(0f, 22f)] private float maxStemLeanDegrees = 13f;
    [SerializeField, Range(0f, 0.4f)] private float maxStemBend = 0.18f;
    [SerializeField] private Vector2 bloomWidthRange = new Vector2(0.78f, 1.38f);
    [SerializeField] private Vector2 bloomDepthRange = new Vector2(0.78f, 1.28f);
    [SerializeField] private Vector2 bloomHeightRange = new Vector2(0.72f, 1.18f);
    [SerializeField, Range(0f, 0.35f)] private float maxBloomOffset = 0.16f;
    [SerializeField, Range(0f, 18f)] private float maxBloomTiltDegrees = 9f;
    [SerializeField, Range(0f, 1f)] private float shapeVariationBlendStart = 0.18f;

    private float targetGrowthTime;
    private bool isTransitioning;
    private float runtimeScaleMultiplier = 1f;
    private float runtimeDurationMultiplier = 1f;
    private float runtimeWobbleMultiplier = 1f;
    private Quaternion runtimeStemLean = Quaternion.identity;
    private Quaternion runtimeBloomTilt = Quaternion.identity;
    private Vector3 runtimeBloomScale = Vector3.one;
    private Vector3 runtimeBloomOffset = Vector3.zero;
    private Vector3 runtimeStemBendDirection = Vector3.right;
    private float runtimeStemBendAmount;
    private Coroutine matureScaleRoutine;
    private Material runtimeToonOutlineMaterial;
    private readonly System.Collections.Generic.List<RuntimeMeshBend> runtimeStemBends = new();
    private readonly System.Collections.Generic.List<GameObject> runtimeToonOutlineObjects = new();
    private readonly System.Collections.Generic.Dictionary<Transform, Renderer[]> cachedRenderers = new();

    public GrowthProfile_SO Profile => growthProfile;
    public float CurrentGrowthTime => currentGrowthTime;
    public float TargetGrowthTime => targetGrowthTime;

    private void Awake()
    {
        AutoAssignProfile();
        AutoAssignBindings();
        AutoAssignBloomAnchor();
        EnsureBlockingColliders();
        EnsureStemMeshBends();
        RebuildToonOutline();
    }

    private void OnDestroy()
    {
        if (runtimeToonOutlineMaterial != null)
        {
            Destroy(runtimeToonOutlineMaterial);
        }

        for (int i = 0; i < runtimeStemBends.Count; i++)
        {
            if (runtimeStemBends[i]?.filter != null && runtimeStemBends[i].sourceMesh != null)
            {
                runtimeStemBends[i].filter.sharedMesh = runtimeStemBends[i].sourceMesh;
            }

            if (runtimeStemBends[i]?.runtimeMesh != null)
            {
                Destroy(runtimeStemBends[i].runtimeMesh);
            }
        }
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
            ApplyTraversalBlocking();
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
            ApplyTraversalBlocking();
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
        ApplyTraversalBlocking();
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
        ConfigureRuntimeVariation(scaleMultiplier, durationMultiplier, wobbleMultiplier, Random.value);
    }

    public void ConfigureRuntimeVariation(float scaleMultiplier, float durationMultiplier, float wobbleMultiplier, float shapeSeed)
    {
        runtimeScaleMultiplier = Mathf.Max(0.05f, scaleMultiplier);
        runtimeDurationMultiplier = Mathf.Max(0.05f, durationMultiplier);
        runtimeWobbleMultiplier = Mathf.Max(0f, wobbleMultiplier);
        ConfigureOrganicShapeVariation(shapeSeed);
        ApplyGrowth(currentGrowthTime);
        ApplyTraversalBlocking();
    }

    public void ResetRuntimeVariation()
    {
        if (matureScaleRoutine != null)
        {
            StopCoroutine(matureScaleRoutine);
            matureScaleRoutine = null;
        }

        runtimeScaleMultiplier = 1f;
        runtimeDurationMultiplier = 1f;
        runtimeWobbleMultiplier = 1f;
        runtimeStemLean = Quaternion.identity;
        runtimeBloomTilt = Quaternion.identity;
        runtimeBloomScale = Vector3.one;
        runtimeBloomOffset = Vector3.zero;
        runtimeStemBendAmount = 0f;
        ApplyStemMeshBend();
        ApplyGrowth(currentGrowthTime);
        ApplyTraversalBlocking();
    }

    public void RebuildRuntimeGeneratedVisuals()
    {
        ClearToonOutline();
        cachedRenderers.Clear();
        RebuildToonOutline();
    }

    public void CultivateMatureScale(float scaleStep, float maxScaleMultiplier, float transitionSeconds)
    {
        float targetScale = Mathf.Min(Mathf.Max(0.05f, maxScaleMultiplier), runtimeScaleMultiplier + Mathf.Max(0f, scaleStep));
        if (targetScale <= runtimeScaleMultiplier + 0.001f)
        {
            return;
        }

        targetGrowthTime = 1f;
        if (currentGrowthTime < 0.999f)
        {
            isTransitioning = true;
        }

        if (matureScaleRoutine != null)
        {
            StopCoroutine(matureScaleRoutine);
        }

        matureScaleRoutine = StartCoroutine(AnimateRuntimeScale(runtimeScaleMultiplier, targetScale, transitionSeconds));
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

    private void ConfigureOrganicShapeVariation(float seed)
    {
        if (!enableOrganicShapeVariation)
        {
            runtimeStemLean = Quaternion.identity;
            runtimeBloomTilt = Quaternion.identity;
            runtimeBloomScale = Vector3.one;
            runtimeBloomOffset = Vector3.zero;
            return;
        }

        float s = Mathf.Repeat(seed, 1f);
        float angle = Mathf.Lerp(-maxStemLeanDegrees, maxStemLeanDegrees, Stable01(s + 0.11f));
        float leanAzimuth = Stable01(s + 0.29f) * Mathf.PI * 2f;
        Vector3 leanAxis = new Vector3(Mathf.Cos(leanAzimuth), 0f, Mathf.Sin(leanAzimuth)).normalized;
        runtimeStemLean = Quaternion.AngleAxis(angle, leanAxis);
        runtimeStemBendDirection = new Vector3(Mathf.Cos(leanAzimuth + Mathf.PI * 0.5f), 0f, Mathf.Sin(leanAzimuth + Mathf.PI * 0.5f)).normalized;
        runtimeStemBendAmount = Mathf.Lerp(-maxStemBend, maxStemBend, Stable01(s + 0.31f));

        runtimeBloomScale = new Vector3(
            Mathf.Lerp(bloomWidthRange.x, bloomWidthRange.y, Stable01(s + 0.43f)),
            Mathf.Lerp(bloomHeightRange.x, bloomHeightRange.y, Stable01(s + 0.61f)),
            Mathf.Lerp(bloomDepthRange.x, bloomDepthRange.y, Stable01(s + 0.79f)));

        float offsetAngle = Stable01(s + 0.37f) * Mathf.PI * 2f;
        float offsetDistance = Mathf.Lerp(0f, maxBloomOffset, Stable01(s + 0.53f));
        runtimeBloomOffset = new Vector3(Mathf.Cos(offsetAngle) * offsetDistance, 0f, Mathf.Sin(offsetAngle) * offsetDistance);

        float tiltX = Mathf.Lerp(-maxBloomTiltDegrees, maxBloomTiltDegrees, Stable01(s + 0.67f));
        float tiltZ = Mathf.Lerp(-maxBloomTiltDegrees, maxBloomTiltDegrees, Stable01(s + 0.83f));
        runtimeBloomTilt = Quaternion.Euler(tiltX, 0f, tiltZ);
        ApplyStemMeshBend();
    }

    private static float Stable01(float value)
    {
        return Mathf.Repeat(Mathf.Sin(value * 127.1f + 19.19f) * 43758.5453f, 1f);
    }

    private void ApplyStemMeshBend()
    {
        EnsureStemMeshBends();
        for (int i = 0; i < runtimeStemBends.Count; i++)
        {
            RuntimeMeshBend bend = runtimeStemBends[i];
            if (bend == null || bend.runtimeMesh == null || bend.sourceVertices == null)
            {
                continue;
            }

            Vector3[] vertices = new Vector3[bend.sourceVertices.Length];
            Bounds bounds = bend.sourceMesh != null ? bend.sourceMesh.bounds : default;
            float minY = bounds.min.y;
            float height = Mathf.Max(0.0001f, bounds.size.y);
            for (int v = 0; v < vertices.Length; v++)
            {
                Vector3 vertex = bend.sourceVertices[v];
                float heightT = Mathf.Clamp01((vertex.y - minY) / height);
                float curve = heightT * heightT;
                vertex += runtimeStemBendDirection * runtimeStemBendAmount * curve;
                vertices[v] = vertex;
            }

            bend.runtimeMesh.vertices = vertices;
            bend.runtimeMesh.RecalculateBounds();
            bend.runtimeMesh.RecalculateNormals();
        }
    }

    private void EnsureStemMeshBends()
    {
        if (runtimeStemBends.Count > 0)
        {
            return;
        }

        Transform stem = FindTarget(stemPartName);
        if (stem == null)
        {
            return;
        }

        MeshFilter[] filters = stem.GetComponentsInChildren<MeshFilter>(includeInactive: true);
        for (int i = 0; i < filters.Length; i++)
        {
            MeshFilter filter = filters[i];
            if (filter == null || filter.sharedMesh == null)
            {
                continue;
            }

            Mesh sourceMesh = filter.sharedMesh;
            if (!sourceMesh.isReadable)
            {
                continue;
            }

            Mesh runtimeMesh = Instantiate(sourceMesh);
            runtimeMesh.name = sourceMesh.name + "_RuntimeStemBend";
            filter.sharedMesh = runtimeMesh;
            runtimeStemBends.Add(new RuntimeMeshBend
            {
                filter = filter,
                sourceMesh = sourceMesh,
                runtimeMesh = runtimeMesh,
                sourceVertices = sourceMesh.vertices
            });
        }
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
            Renderer[] allRenderers = target.GetComponentsInChildren<Renderer>(includeInactive: true);
            System.Collections.Generic.List<Renderer> visibleRenderers = new();
            for (int i = 0; i < allRenderers.Length; i++)
            {
                if (allRenderers[i] == null ||
                    allRenderers[i].gameObject.name.EndsWith("_ToonOutline", System.StringComparison.Ordinal))
                {
                    continue;
                }

                visibleRenderers.Add(allRenderers[i]);
            }

            renderers = visibleRenderers.ToArray();
            cachedRenderers[target] = renderers;
        }

        return renderers;
    }

    private Vector3 ResolvePartScale(string partName, Vector3 baseScale, float partGrowthTime)
    {
        float shapeT = ResolveShapeVariationWeight(partGrowthTime);
        if (!string.IsNullOrWhiteSpace(bloomPartName) && partName == bloomPartName && enableOrganicShapeVariation)
        {
            Vector3 organicScale = new Vector3(
                Mathf.Lerp(1f, runtimeBloomScale.x, shapeT),
                Mathf.Lerp(1f, runtimeBloomScale.y, shapeT),
                Mathf.Lerp(1f, runtimeBloomScale.z, shapeT));
            baseScale = Vector3.Scale(baseScale, organicScale);
        }

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

    private float ResolveShapeVariationWeight(float growthTime)
    {
        return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(shapeVariationBlendStart, 1f, growthTime));
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
        if (enableOrganicShapeVariation && !string.IsNullOrWhiteSpace(bloomPartName) && partName == bloomPartName)
        {
            statePosition += runtimeBloomOffset * runtimeScaleMultiplier * ResolveShapeVariationWeight(currentGrowthTime);
        }

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
        Quaternion organicRotation = Quaternion.identity;
        if (enableOrganicShapeVariation)
        {
            float shapeT = ResolveShapeVariationWeight(growthTime);
            if (!string.IsNullOrWhiteSpace(stemPartName) && partName == stemPartName)
            {
                organicRotation = Quaternion.Slerp(Quaternion.identity, runtimeStemLean, shapeT);
            }
            else if (!string.IsNullOrWhiteSpace(bloomPartName) && partName == bloomPartName)
            {
                organicRotation = Quaternion.Slerp(Quaternion.identity, runtimeBloomTilt, shapeT);
            }
        }

        if (!enableStemWobble ||
            !isTransitioning ||
            string.IsNullOrWhiteSpace(stemPartName) ||
            partName != stemPartName)
        {
            return organicRotation;
        }

        if (growthTime <= stemWobbleStart || growthTime >= stemWobbleEnd)
        {
            return organicRotation;
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
        return organicRotation * Quaternion.Euler(xAngle, 0f, zAngle);
    }

    private void ResetCompletedStemRotation()
    {
        ApplyGrowth(currentGrowthTime);
    }

    private IEnumerator AnimateRuntimeScale(float fromScale, float toScale, float transitionSeconds)
    {
        float duration = Mathf.Max(0.05f, transitionSeconds);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = t * t * (3f - 2f * t);
            runtimeScaleMultiplier = Mathf.Lerp(fromScale, toScale, eased);
            ApplyGrowth(currentGrowthTime);
            ApplyTraversalBlocking();
            yield return null;
        }

        runtimeScaleMultiplier = toScale;
        ApplyGrowth(currentGrowthTime);
        ApplyTraversalBlocking();
        matureScaleRoutine = null;
    }

    private void EnsureBlockingColliders()
    {
        if (collidersToEnableOnGrowth != null && collidersToEnableOnGrowth.Length > 0)
        {
            return;
        }

        Collider[] existing = GetComponentsInChildren<Collider>(includeInactive: true);
        if (existing != null && existing.Length > 0)
        {
            collidersToEnableOnGrowth = existing;
            return;
        }

        if (!autoCreateBlockingCollider)
        {
            return;
        }

        CapsuleCollider capsule = gameObject.AddComponent<CapsuleCollider>();
        capsule.isTrigger = false;
        capsule.direction = 1;
        capsule.center = autoBlockingColliderCenter;
        capsule.radius = autoBlockingColliderRadius;
        capsule.height = autoBlockingColliderHeight;
        capsule.enabled = false;
        collidersToEnableOnGrowth = new Collider[] { capsule };
    }

    private void RebuildToonOutline()
    {
        ClearToonOutline();
        if (!enableToonOutline)
        {
            return;
        }

        if (outlineStem)
        {
            BuildPartToonOutline(FindTarget(stemPartName));
        }

        if (outlineBloom)
        {
            BuildPartToonOutline(FindTarget(bloomPartName));
        }
    }

    private void BuildPartToonOutline(Transform partRoot)
    {
        if (partRoot == null)
        {
            return;
        }

        MeshRenderer[] renderers = partRoot.GetComponentsInChildren<MeshRenderer>(includeInactive: true);
        for (int i = 0; i < renderers.Length; i++)
        {
            MeshRenderer sourceRenderer = renderers[i];
            if (sourceRenderer == null || IsToonOutlineObject(sourceRenderer.gameObject))
            {
                continue;
            }

            MeshFilter sourceFilter = sourceRenderer.GetComponent<MeshFilter>();
            if (sourceFilter == null || sourceFilter.sharedMesh == null)
            {
                continue;
            }

            GameObject outline = new GameObject(sourceRenderer.gameObject.name + "_ToonOutline");
            outline.transform.SetParent(sourceRenderer.transform, false);
            outline.transform.localPosition = Vector3.zero;
            outline.transform.localRotation = Quaternion.identity;
            outline.transform.localScale = Vector3.one * (1f + toonOutlineThickness);

            MeshFilter outlineFilter = outline.AddComponent<MeshFilter>();
            outlineFilter.sharedMesh = sourceFilter.sharedMesh;

            MeshRenderer outlineRenderer = outline.AddComponent<MeshRenderer>();
            outlineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            outlineRenderer.receiveShadows = false;
            outlineRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            outlineRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

            Material outlineMaterial = GetRuntimeToonOutlineMaterial();
            Material[] materials = new Material[Mathf.Max(1, sourceRenderer.sharedMaterials.Length)];
            for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
            {
                materials[materialIndex] = outlineMaterial;
            }

            outlineRenderer.sharedMaterials = materials;
            runtimeToonOutlineObjects.Add(outline);
        }
    }

    private void ClearToonOutline()
    {
        for (int i = runtimeToonOutlineObjects.Count - 1; i >= 0; i--)
        {
            DestroySafe(runtimeToonOutlineObjects[i]);
        }

        runtimeToonOutlineObjects.Clear();

        Transform[] children = GetComponentsInChildren<Transform>(includeInactive: true);
        for (int i = children.Length - 1; i >= 0; i--)
        {
            Transform child = children[i];
            if (child == null || child == transform)
            {
                continue;
            }

            if (IsToonOutlineObject(child.gameObject))
            {
                DestroySafe(child.gameObject);
            }
        }
    }

    private static bool IsToonOutlineObject(GameObject candidate)
    {
        return candidate != null && candidate.name.EndsWith("_ToonOutline", System.StringComparison.Ordinal);
    }

    private static void DestroySafe(Object target)
    {
        if (target == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(target);
        }
        else
        {
            DestroyImmediate(target);
        }
    }

    private Material GetRuntimeToonOutlineMaterial()
    {
        if (runtimeToonOutlineMaterial != null)
        {
            return runtimeToonOutlineMaterial;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        runtimeToonOutlineMaterial = new Material(shader);
        runtimeToonOutlineMaterial.name = "Runtime Mushroom Toon Outline";
        runtimeToonOutlineMaterial.renderQueue = 1990;
        if (runtimeToonOutlineMaterial.HasProperty("_BaseColor"))
        {
            runtimeToonOutlineMaterial.SetColor("_BaseColor", toonOutlineColor);
        }
        if (runtimeToonOutlineMaterial.HasProperty("_Color"))
        {
            runtimeToonOutlineMaterial.SetColor("_Color", toonOutlineColor);
        }
        if (runtimeToonOutlineMaterial.HasProperty("_Cull"))
        {
            runtimeToonOutlineMaterial.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Front);
        }
        if (runtimeToonOutlineMaterial.HasProperty("_ZWrite"))
        {
            runtimeToonOutlineMaterial.SetFloat("_ZWrite", 1f);
        }
        if (runtimeToonOutlineMaterial.HasProperty("_ZTest"))
        {
            runtimeToonOutlineMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
        }

        return runtimeToonOutlineMaterial;
    }

    private void ApplyTraversalBlocking()
    {
        if (collidersToEnableOnGrowth == null)
        {
            return;
        }

        bool shouldBlock = currentGrowthTime >= blockActivationTime && !isTransitioning;
        if (shouldBlock && deferBlockingUntilPlayerClear && IsPlayerOverlappingBlockingVolume())
        {
            shouldBlock = false;
        }

        if (shouldBlock != blockApplied)
        {
            blockApplied = shouldBlock;
            foreach (var col in collidersToEnableOnGrowth)
            {
                if (col != null)
                {
                    col.enabled = shouldBlock;
                }
            }
        }

        foreach (var col in collidersToEnableOnGrowth)
        {
            if (col is CapsuleCollider capsule)
            {
                ApplyCapsuleBlockingSize(capsule);
            }
        }
    }

    private bool IsPlayerOverlappingBlockingVolume()
    {
        for (int i = 0; i < collidersToEnableOnGrowth.Length; i++)
        {
            Collider blockingCollider = collidersToEnableOnGrowth[i];
            if (blockingCollider == null)
            {
                continue;
            }

            Collider[] overlaps = blockingCollider is CapsuleCollider capsule
                ? QueryCapsuleBlockingOverlaps(capsule)
                : QueryBoundsBlockingOverlaps(blockingCollider);

            for (int j = 0; j < overlaps.Length; j++)
            {
                Collider candidate = overlaps[j];
                if (candidate == null ||
                    candidate == blockingCollider ||
                    candidate.GetComponentInParent<GrowthPlant>() == this)
                {
                    continue;
                }

                if (candidate.GetComponentInParent<CharacterController>() != null ||
                    candidate.gameObject.tag == "Player")
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void ApplyCapsuleBlockingSize(CapsuleCollider capsule)
    {
        if (capsule == null)
        {
            return;
        }

        float scale = Mathf.Max(0.05f, runtimeScaleMultiplier);
        capsule.center = autoBlockingColliderCenter * scale;
        capsule.radius = autoBlockingColliderRadius * scale;
        capsule.height = autoBlockingColliderHeight * scale;
    }

    private Collider[] QueryCapsuleBlockingOverlaps(CapsuleCollider capsule)
    {
        ApplyCapsuleBlockingSize(capsule);

        Transform capsuleTransform = capsule.transform;
        Vector3 center = capsuleTransform.TransformPoint(capsule.center);
        float maxAxisScale = Mathf.Max(
            Mathf.Abs(capsuleTransform.lossyScale.x),
            Mathf.Abs(capsuleTransform.lossyScale.z));
        float verticalScale = Mathf.Abs(capsuleTransform.lossyScale.y);
        float radius = capsule.radius * maxAxisScale + blockingPlayerClearancePadding;
        float height = Mathf.Max(capsule.height * verticalScale, radius * 2f);
        Vector3 halfSegment = capsuleTransform.up * Mathf.Max(0f, (height * 0.5f) - radius);

        return Physics.OverlapCapsule(
            center - halfSegment,
            center + halfSegment,
            radius,
            ~0,
            QueryTriggerInteraction.Ignore);
    }

    private Collider[] QueryBoundsBlockingOverlaps(Collider blockingCollider)
    {
        Bounds bounds = blockingCollider.bounds;
        if (bounds.size.sqrMagnitude < 0.0001f)
        {
            bounds = new Bounds(blockingCollider.transform.position, Vector3.one * blockingPlayerClearancePadding * 2f);
        }

        bounds.Expand(blockingPlayerClearancePadding * 2f);
        return Physics.OverlapBox(
            bounds.center,
            bounds.extents,
            Quaternion.identity,
            ~0,
            QueryTriggerInteraction.Ignore);
    }

}
