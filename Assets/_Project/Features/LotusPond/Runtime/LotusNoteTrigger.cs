using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class LotusNoteTrigger : MonoBehaviour
{
    public event System.Action<LotusNoteTrigger> NoteActivationStarted;
    public event System.Action<LotusNoteTrigger> NoteTriggered;
    [Header("Unity Events (Editor Only)")]
    // 2. 添加 UnityEvent，它会在 Inspector 面板显示面板
    public UnityEvent onTriggered;

    public float CooldownSeconds => cooldownSeconds;

    [Header("Settings")]
    [SerializeField] private LotusScaleSettingsSO settings;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip noteClip;

    [Header("Ripple")]
    [SerializeField] private LotusRippleController rippleController;

    [Header("Water Magic Impact")]
    [SerializeField] private bool enableWaterMagicOnRaycast = true;
    [SerializeField] private GameObject waterImpactEffectPrefab;
    [SerializeField] private float impactEffectScale = 4f;
    [SerializeField] private float impactEffectLifetimeSeconds = 1.4f;
    [SerializeField] private float impactEffectSimulationSpeed = 1.35f;
    [SerializeField, Range(0f, 1f)] private float impactEffectAlphaMultiplier = 0.68f;
    [SerializeField] private Vector3 impactEffectWorldOffset = new Vector3(0f, -0.25f, 0f);
    [SerializeField] private bool spawnImpactEffectAtLeafCenter = true;

    [Header("Water Magic Projectile")]
    [SerializeField] private float projectileFlightSeconds = 2.3f;
    [SerializeField] private float projectileArcHeight = 1.45f;
    [SerializeField] private float projectileSideCurve = 0.72f;
    [SerializeField] private float projectileSecondarySideCurve = -0.38f;
    [SerializeField] private float trailWidth = 0.014f;
    [SerializeField] private float trailVisibleFraction = 0.34f;
    [SerializeField] private int trailSegments = 30;
    [SerializeField] private float haloWidthMultiplier = 2.15f;
    [SerializeField] private float spiralRadius = 0.09f;
    [SerializeField] private float spiralRadiusVariation = 0.055f;
    [SerializeField] private float spiralTurns = 2.7f;
    [SerializeField] private int spiralStrandCount = 3;
    [SerializeField] private float strandWidthMultiplier = 0.42f;
    [SerializeField] private float impactSparkSeconds = 0.22f;
    [SerializeField] private int impactSparkCount = 10;
    [SerializeField] private Color waterCoreColor = new Color(0.35f, 0.9f, 1f, 1f);
    [SerializeField] private Color waterTrailColor = new Color(0.72f, 0.98f, 1f, 0.82f);

    [Header("Impact Audio Reliability")]
    [SerializeField, Range(0f, 1f)] private float impactAudioSpatialBlend = 0.35f;
    [SerializeField, Min(0f)] private float minimumAudibleDistance = 24f;

    [Header("Trigger")]
    [SerializeField] private bool triggerOnlyOncePerStay = true;
    [SerializeField] private float cooldownSeconds = 0.25f;
    [SerializeField] private string[] allowedTags;
    [SerializeField] private bool logDebugMessages;

    [Header("Water Droplet Generation")]
    [SerializeField] private GameObject waterDropPrefab;
    [SerializeField] private int minDrops = 3;        
    [SerializeField] private int maxDrops = 7;          
    [SerializeField] private float spawnRadius = 0.4f; 
    [SerializeField] private bool autoSpawnRadius = true;
    
    [Range(0.1f, 1.0f)]
    [SerializeField] private float spawnRadiusMultiplier = 0.7f;

    private float nextAllowedTriggerTime;
    private bool objectStillInside;
    private Material runtimeMagicMaterial;

    // Pooled magic-projectile assembly: created lazily on first trigger and reused thereafter.
    // Multiple in-flight projectiles (allowed because cooldown < flight duration) round-robin
    // across the pool; the pool grows only when needed and never shrinks, so steady-state is
    // alloc-free.
    private readonly List<MagicProjectileInstance> magicProjectilePool = new List<MagicProjectileInstance>(2);
    private Vector3[] sparkDirectionsBuffer;
    private float[] sparkLengthsBuffer;
    private static readonly AnimationCurve SharedTailTaperCurve = CreateTailTaperCurve();

    private class MagicProjectileInstance
    {
        public GameObject root;
        public Transform rootTransform;
        public LineRenderer halo;
        public LineRenderer core;
        public LineRenderer[] strands;
        public LineRenderer[] sparks;
        public Light glow;
        public bool inUse;
    }

    [Header("Wobble Settings (Physical Response)")]
    [SerializeField] private float wobbleIntensity = 5f; 
    [SerializeField] private float duration = 0.5f;      
    [SerializeField] private float stiffness = 200f;     // Higher = faster/snappier vibration
    [SerializeField] private float damping = 10f;        // Higher = stops faster (less like jelly)

    private Quaternion originalRotation;
    private Coroutine wobbleCoroutine;


    private void Start()
    {
       originalRotation = transform.localRotation;
       GenerateInitialDroplets();
    }

    private void OnDestroy()
    {
        if (runtimeMagicMaterial != null)
        {
            Destroy(runtimeMagicMaterial);
        }

        for (int i = 0; i < magicProjectilePool.Count; i++)
        {
            MagicProjectileInstance instance = magicProjectilePool[i];
            if (instance != null && instance.root != null)
            {
                Destroy(instance.root);
            }
        }

        magicProjectilePool.Clear();
    }
    private void Reset()
    {
        audioSource = GetComponent<AudioSource>();
        if (rippleController == null)
        {
            rippleController = GetComponentInChildren<LotusRippleController>(true);
        }
    }

    private void Awake()
    {
        EnsureAudioSource();
        if (rippleController == null) rippleController = GetComponentInChildren<LotusRippleController>(true);

        ApplySettings();
    }
    private void OnValidate() => ApplySettings();

    private void OnTriggerEnter(Collider other) => TryTrigger(other);

    private void OnTriggerStay(Collider other)
    {
        if (!triggerOnlyOncePerStay) TryTrigger(other);
    }

    private void GenerateInitialDroplets()
    {
        if (waterDropPrefab == null) return;
        int count = Random.Range(minDrops, maxDrops + 1);
        // Get the MeshRenderer of the leaf itself
        MeshRenderer leafMesh = GetComponentInChildren<MeshRenderer>();
        float effectiveSpawnRadius = spawnRadius;

        if (autoSpawnRadius && leafMesh != null)
        {
            // leafMesh.bounds is WORLD space
            Bounds worldBounds = leafMesh.bounds;
            float radiusWorld = Mathf.Min(worldBounds.extents.x, worldBounds.extents.z);

            // Convert WORLD radius to LOCAL radius (because we spawn via localPosition under this transform)
            float radiusLocal = transform.InverseTransformVector(Vector3.right * radiusWorld).magnitude;

            effectiveSpawnRadius = Mathf.Max(0.01f, radiusLocal * spawnRadiusMultiplier);
        }

        for (int i = 0; i < count; i++)
        {
            Vector2 randomPoint = Random.insideUnitCircle * spawnRadius;
            Vector3 localPos = new Vector3(randomPoint.x, 0.01f, randomPoint.y); // Y微抬防止穿模

            GameObject drop = Instantiate(waterDropPrefab, transform);
            drop.transform.localPosition = localPos;

            float s = Random.Range(0.2f, 0.4f) / 4.5f;
            
            drop.transform.localScale = new Vector3(s, s * 0.5f, s); 
            drop.transform.localRotation = Quaternion.Euler(0, Random.Range(0, 360), 0);

            WaterDropSlide slideScript = drop.GetComponent<WaterDropSlide>();
            if (slideScript != null && leafMesh != null)
            {
                slideScript.Initialize(leafMesh); // This sets the radius automatically!
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsAllowedCollider(other)) return;
        objectStillInside = false;
    }

    private void TryTrigger(Collider other)
    {
        if (!IsAllowedCollider(other)) return;
        if (triggerOnlyOncePerStay && objectStillInside) return;

        objectStillInside = true;
        // Default direction for physics trigger (from collider center)
        Vector3 hitDir = (other.transform.position - transform.position).normalized;
        TriggerNoteInternal($"LotusNoteTrigger fired by {other.name}", hitDir);
    }

    /// <summary>
    /// Public trigger for Raycasts. Call this and pass hit.point for point-of-impact deflection.
    /// </summary>
    public void TriggerNote(Vector3 worldHitPoint)
    {
        Vector3 fallbackOrigin = ResolveFallbackMagicOrigin(worldHitPoint);
        TriggerNote(worldHitPoint, fallbackOrigin);
    }

    public void TriggerNote(Vector3 worldHitPoint, Vector3 magicOrigin)
    {
        objectStillInside = false;
        // Calculate hit direction relative to the lotus center
        Vector3 localHitPoint = transform.InverseTransformPoint(worldHitPoint);
        Vector3 hitDir = new Vector3(localHitPoint.x, 0, localHitPoint.z).normalized;

        if (enableWaterMagicOnRaycast)
        {
            TryStartWaterMagicTrigger(worldHitPoint, magicOrigin, hitDir);
            return;
        }

        TriggerNoteInternal($"LotusNoteTrigger activated by Raycast at {worldHitPoint}", hitDir);
    }

    /// <summary>
    /// Overload for keyboard debug or simple triggers.
    /// </summary>
    public void TriggerNote()
    {
        objectStillInside = false;
        // Default deflection from the front
        TriggerNoteInternal($"LotusNoteTrigger activated via generic call", Vector3.forward);
    }

    private void TriggerNoteInternal(string debugMessage, Vector3 hitDir)
    {
        TriggerNoteInternal(debugMessage, hitDir, false, transform.position + Vector3.up * 0.05f);
    }

    private void TriggerNoteInternal(string debugMessage, Vector3 hitDir, bool cooldownAlreadyReserved, Vector3 impactPoint)
    {
        if (!cooldownAlreadyReserved && Time.time < nextAllowedTriggerTime) return;

        if (!cooldownAlreadyReserved)
        {
            nextAllowedTriggerTime = Time.time + cooldownSeconds;
        }

        // 1. Audio Logic
        EnsureAudioSource();
        if (audioSource != null)
        {
            if (noteClip != null) audioSource.PlayOneShot(noteClip);
            else audioSource.Play();
        }

        // 2. Visual Ripple Logic
        if (rippleController != null) rippleController.PlayRipple();

        PlayWaterImpactEffect(impactPoint);

        // 3. Event Notification
        NoteTriggered?.Invoke(this);
        onTriggered?.Invoke();

        if (logDebugMessages) Debug.Log(debugMessage);

        // 4. Physical Wobble Logic
        if (wobbleCoroutine != null) StopCoroutine(wobbleCoroutine);
        wobbleCoroutine = StartCoroutine(DoPhysicalWobble(hitDir));

        // Validation Warnings
        if (logDebugMessages && audioSource == null) Debug.LogWarning($"[LotusNoteTrigger] No AudioSource on {name}.");
        if (logDebugMessages && rippleController == null) Debug.LogWarning($"[LotusNoteTrigger] No RippleController on {name}.");
    }

    private void EnsureAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null && noteClip != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            return;
        }

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = impactAudioSpatialBlend;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.maxDistance = Mathf.Max(audioSource.maxDistance, minimumAudibleDistance);
    }

    private void TryStartWaterMagicTrigger(Vector3 worldHitPoint, Vector3 magicOrigin, Vector3 hitDir)
    {
        if (Time.time < nextAllowedTriggerTime)
        {
            return;
        }

        nextAllowedTriggerTime = Time.time + cooldownSeconds;
        NoteActivationStarted?.Invoke(this);
        StartCoroutine(TriggerAfterWaterMagicFlight(worldHitPoint, magicOrigin, hitDir));
    }

    private IEnumerator TriggerAfterWaterMagicFlight(Vector3 worldHitPoint, Vector3 magicOrigin, Vector3 hitDir)
    {
        yield return FlyWaterMagicProjectile(magicOrigin, worldHitPoint);
        TriggerNoteInternal($"LotusNoteTrigger activated by water magic at {worldHitPoint}", hitDir, true, worldHitPoint);
    }

    private IEnumerator FlyWaterMagicProjectile(Vector3 start, Vector3 end)
    {
        MagicProjectileInstance projectile = AcquireMagicProjectile();
        projectile.root.SetActive(true);

        Transform magicTransform = projectile.rootTransform;
        LineRenderer core = projectile.core;
        LineRenderer halo = projectile.halo;
        LineRenderer[] strands = projectile.strands;
        Light glow = projectile.glow;

        Vector3 travel = end - start;
        Vector3 travelDirection = travel.sqrMagnitude > 0.0001f ? travel.normalized : transform.forward;
        Vector3 side = Vector3.Cross(Vector3.up, travelDirection);
        if (side.sqrMagnitude < 0.0001f)
        {
            side = Vector3.right;
        }

        side.Normalize();

        // pathForward / right / up are constant for the whole flight, so compute once and pass into
        // UpdateRibbonLine instead of recomputing inside it for every line, every frame.
        Vector3 pathForward = travelDirection;
        Vector3 right = Vector3.Cross(Vector3.up, pathForward);
        if (right.sqrMagnitude < 0.0001f)
        {
            right = Vector3.right;
        }

        right.Normalize();
        Vector3 up = Vector3.Cross(pathForward, right).normalized;

        Vector3 controlA = Vector3.Lerp(start, end, 0.32f) + Vector3.up * (projectileArcHeight * 0.58f) + side * projectileSideCurve;
        Vector3 controlB = Vector3.Lerp(start, end, 0.76f) + Vector3.up * projectileArcHeight + side * projectileSecondarySideCurve;
        float flightSeconds = Mathf.Max(0.05f, projectileFlightSeconds);
        float elapsed = 0f;

        while (elapsed < flightSeconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / flightSeconds);
            float eased = t * t * (3f - 2f * t);
            Vector3 current = CubicBezier(start, controlA, controlB, end, eased);

            magicTransform.position = current;
            UpdateRibbonLine(core, start, controlA, controlB, end, eased, 0f, 0f, false, right, up);
            UpdateRibbonLine(halo, start, controlA, controlB, end, eased, 0f, 0f, false, right, up);

            for (int i = 0; i < strands.Length; i++)
            {
                float phase = i / Mathf.Max(1f, strands.Length) * Mathf.PI * 2f;
                UpdateRibbonLine(strands[i], start, controlA, controlB, end, eased, phase, spiralRadius, true, right, up);
            }

            if (glow != null) glow.intensity = Mathf.Lerp(0.75f, 1.65f, Mathf.Sin(t * Mathf.PI));
            yield return null;
        }

        magicTransform.position = end;
        UpdateRibbonLine(core, start, controlA, controlB, end, 1f, 0f, 0f, false, right, up);
        UpdateRibbonLine(halo, start, controlA, controlB, end, 1f, 0f, 0f, false, right, up);
        for (int i = 0; i < strands.Length; i++)
        {
            float phase = i / Mathf.Max(1f, strands.Length) * Mathf.PI * 2f;
            UpdateRibbonLine(strands[i], start, controlA, controlB, end, 1f, phase, spiralRadius, true, right, up);
        }

        yield return PlayImpactSparks(projectile, end, travelDirection);
        projectile.root.SetActive(false);
        ReleaseMagicProjectile(projectile);
    }

    private MagicProjectileInstance AcquireMagicProjectile()
    {
        for (int i = 0; i < magicProjectilePool.Count; i++)
        {
            MagicProjectileInstance candidate = magicProjectilePool[i];
            if (!candidate.inUse && candidate.root != null)
            {
                candidate.inUse = true;
                return candidate;
            }
        }

        MagicProjectileInstance created = CreateMagicProjectileInstance();
        created.inUse = true;
        magicProjectilePool.Add(created);
        return created;
    }

    private void ReleaseMagicProjectile(MagicProjectileInstance projectile)
    {
        if (projectile == null) return;
        projectile.inUse = false;
    }

    private MagicProjectileInstance CreateMagicProjectileInstance()
    {
        MagicProjectileInstance instance = new MagicProjectileInstance();
        instance.root = new GameObject("LotusWaterMagicRibbon");
        instance.root.SetActive(false);
        instance.rootTransform = instance.root.transform;

        instance.halo = CreateMagicLine(instance.root, "OuterWaterGlow", trailWidth * haloWidthMultiplier, waterTrailColor, 0.012f, 0.22f);
        instance.core = CreateMagicLine(instance.root, "InnerWaterThread", trailWidth, waterCoreColor, 0.03f, 0.9f);

        int strandCount = Mathf.Clamp(spiralStrandCount, 0, 6);
        instance.strands = new LineRenderer[strandCount];
        for (int i = 0; i < strandCount; i++)
        {
            instance.strands[i] = CreateMagicLine(
                instance.root,
                $"SpiralWaterThread_{i + 1}",
                trailWidth * strandWidthMultiplier,
                Color.Lerp(waterTrailColor, waterCoreColor, 0.35f),
                0f,
                0.68f);
        }

        instance.glow = instance.root.AddComponent<Light>();
        instance.glow.color = waterCoreColor;
        instance.glow.intensity = 1.1f;
        instance.glow.range = 1.45f;

        int sparkCount = Mathf.Clamp(impactSparkCount, 0, 32);
        instance.sparks = new LineRenderer[sparkCount];
        for (int i = 0; i < sparkCount; i++)
        {
            LineRenderer spark = CreateMagicLine(instance.root, $"ImpactWaterSpark_{i + 1}", trailWidth * 0.42f, waterCoreColor, 0.86f, 0f);
            spark.positionCount = 2;
            spark.enabled = false;
            instance.sparks[i] = spark;
        }

        return instance;
    }

    private LineRenderer CreateMagicLine(GameObject parent, string lineName, float width, Color color, float startAlpha, float endAlpha)
    {
        GameObject lineObject = new GameObject(lineName);
        lineObject.transform.SetParent(parent.transform, false);

        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.sharedMaterial = GetRuntimeMagicMaterial();
        line.positionCount = Mathf.Max(6, trailSegments);
        line.widthMultiplier = Mathf.Max(0.004f, width);
        line.numCapVertices = 4;
        line.alignment = LineAlignment.View;
        line.textureMode = LineTextureMode.Stretch;
        line.widthCurve = SharedTailTaperCurve;
        line.startColor = new Color(color.r, color.g, color.b, startAlpha);
        line.endColor = new Color(color.r, color.g, color.b, endAlpha);
        return line;
    }

    private IEnumerator PlayImpactSparks(MagicProjectileInstance projectile, Vector3 impactPoint, Vector3 incomingDirection)
    {
        LineRenderer[] sparks = projectile.sparks;
        int sparkCount = sparks != null ? sparks.Length : 0;
        if (sparkCount == 0 || impactSparkSeconds <= 0f)
        {
            yield break;
        }

        if (sparkDirectionsBuffer == null || sparkDirectionsBuffer.Length < sparkCount)
        {
            sparkDirectionsBuffer = new Vector3[sparkCount];
            sparkLengthsBuffer = new float[sparkCount];
        }

        Vector3[] directions = sparkDirectionsBuffer;
        float[] lengths = sparkLengthsBuffer;

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
            float lift = Mathf.Lerp(0.12f, 0.58f, Halton(i + 1, 3));
            directions[i] = (baseRight * Mathf.Cos(angle) + baseUp * Mathf.Sin(angle) + Vector3.up * lift).normalized;
            lengths[i] = Mathf.Lerp(0.14f, 0.34f, Halton(i + 1, 5));
            sparks[i].enabled = true;
        }

        float elapsed = 0f;
        float sparkSeconds = Mathf.Max(0.05f, impactSparkSeconds);
        while (elapsed < sparkSeconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / sparkSeconds);
            float fade = 1f - t;
            float sinPhase = Mathf.Sin(t * Mathf.PI * 0.85f);

            for (int i = 0; i < sparkCount; i++)
            {
                Vector3 tip = impactPoint + directions[i] * (lengths[i] * sinPhase);
                sparks[i].startColor = new Color(waterCoreColor.r, waterCoreColor.g, waterCoreColor.b, 0.82f * fade);
                sparks[i].endColor = new Color(waterTrailColor.r, waterTrailColor.g, waterTrailColor.b, 0f);
                sparks[i].SetPosition(0, impactPoint);
                sparks[i].SetPosition(1, tip);
            }

            yield return null;
        }

        // Hide the sparks again so they're invisible when the pool item is reused. Keep the
        // LineRenderers themselves enabled for the next flight via projectile pooling.
        for (int i = 0; i < sparkCount; i++)
        {
            sparks[i].enabled = false;
        }
    }

    private void PlayWaterImpactEffect(Vector3 impactPoint)
    {
        if (waterImpactEffectPrefab == null)
        {
            return;
        }

        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, transform.up);
        Vector3 spawnPoint = spawnImpactEffectAtLeafCenter ? transform.position : impactPoint;
        spawnPoint += impactEffectWorldOffset;
        GameObject instance = Instantiate(waterImpactEffectPrefab, spawnPoint, rotation);
        instance.transform.localScale *= Mathf.Max(0.01f, impactEffectScale);
        ConfigureImpactEffectParticles(instance);
        Destroy(instance, ResolveImpactEffectLifetime(instance));
    }

    private void ConfigureImpactEffectParticles(GameObject instance)
    {
        float speed = Mathf.Max(0.01f, impactEffectSimulationSpeed);
        float alpha = Mathf.Clamp01(impactEffectAlphaMultiplier);

        // Reuse a single GetComponentsInChildren result instead of running the walk twice,
        // and reuse the buffer across triggers to keep per-tap allocations down.
        instance.GetComponentsInChildren<ParticleSystem>(true, GetImpactEffectBuffer());
        List<ParticleSystem> buffer = impactEffectListBuffer;
        for (int i = 0; i < buffer.Count; i++)
        {
            ParticleSystem.MainModule main = buffer[i].main;
            main.simulationSpeed *= speed;
            main.startColor = ScaleGradientAlpha(main.startColor, alpha);
        }
    }

    // GetComponentsInChildren<T>(includeInactive, List<T>) reuses the buffer, so we only allocate
    // the list itself once per LotusNoteTrigger lifetime.
    private List<ParticleSystem> impactEffectListBuffer;

    private List<ParticleSystem> GetImpactEffectBuffer()
    {
        if (impactEffectListBuffer == null)
        {
            impactEffectListBuffer = new List<ParticleSystem>(16);
        }
        else
        {
            impactEffectListBuffer.Clear();
        }

        return impactEffectListBuffer;
    }

    private static ParticleSystem.MinMaxGradient ScaleGradientAlpha(ParticleSystem.MinMaxGradient gradient, float alpha)
    {
        switch (gradient.mode)
        {
            case ParticleSystemGradientMode.Color:
                return new ParticleSystem.MinMaxGradient(WithScaledAlpha(gradient.color, alpha));
            case ParticleSystemGradientMode.TwoColors:
                return new ParticleSystem.MinMaxGradient(
                    WithScaledAlpha(gradient.colorMin, alpha),
                    WithScaledAlpha(gradient.colorMax, alpha));
            case ParticleSystemGradientMode.Gradient:
            case ParticleSystemGradientMode.TwoGradients:
            case ParticleSystemGradientMode.RandomColor:
                return gradient;
            default:
                return gradient;
        }
    }

    private static Color WithScaledAlpha(Color color, float alpha)
    {
        color.a *= alpha;
        return color;
    }

    private float ResolveImpactEffectLifetime(GameObject instance)
    {
        if (impactEffectLifetimeSeconds > 0f)
        {
            return impactEffectLifetimeSeconds;
        }

        return ResolveParticleLifetime(instance);
    }

    private Material GetRuntimeMagicMaterial()
    {
        if (runtimeMagicMaterial != null)
        {
            return runtimeMagicMaterial;
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

        runtimeMagicMaterial = new Material(shader);
        runtimeMagicMaterial.renderQueue = 3000;
        if (runtimeMagicMaterial.HasProperty("_Surface"))
        {
            runtimeMagicMaterial.SetFloat("_Surface", 1f);
        }

        if (runtimeMagicMaterial.HasProperty("_Blend"))
        {
            runtimeMagicMaterial.SetFloat("_Blend", 1f);
        }

        runtimeMagicMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        runtimeMagicMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
        runtimeMagicMaterial.SetFloat("_ZWrite", 0f);
        runtimeMagicMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        runtimeMagicMaterial.EnableKeyword("_ALPHABLEND_ON");

        if (runtimeMagicMaterial.HasProperty("_BaseColor"))
        {
            runtimeMagicMaterial.SetColor("_BaseColor", waterCoreColor);
        }
        else if (runtimeMagicMaterial.HasProperty("_Color"))
        {
            runtimeMagicMaterial.SetColor("_Color", waterCoreColor);
        }

        return runtimeMagicMaterial;
    }

    private void UpdateRibbonLine(LineRenderer trail, Vector3 start, Vector3 controlA, Vector3 controlB, Vector3 end, float visibleT, float phase, float radius, bool spiral, Vector3 right, Vector3 up)
    {
        int count = trail.positionCount;
        float tailStart = Mathf.Clamp01(visibleT - Mathf.Clamp01(trailVisibleFraction));
        float visibleSpan = Mathf.Max(0.001f, visibleT - tailStart);

        for (int i = 0; i < count; i++)
        {
            float segmentT = i / Mathf.Max(1f, count - 1);
            float t = Mathf.Clamp01(tailStart + visibleSpan * segmentT);
            Vector3 point = CubicBezier(start, controlA, controlB, end, t);
            if (spiral)
            {
                float taper = Mathf.Sin(segmentT * Mathf.PI);
                float unevenRadius = radius + spiralRadiusVariation * (0.5f + 0.5f * Mathf.Sin(t * Mathf.PI * 5.2f + phase * 1.7f));
                unevenRadius *= Mathf.Lerp(0.62f, 1.08f, Mathf.PerlinNoise(t * 3.1f, phase));
                float angle = phase + t * spiralTurns * Mathf.PI * 2f + Time.time * 5.1f;
                point += (right * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * unevenRadius * taper;
            }

            trail.SetPosition(i, point);
        }
    }

    private static AnimationCurve CreateTailTaperCurve()
    {
        return new AnimationCurve(
            new Keyframe(0f, 0.04f),
            new Keyframe(0.28f, 0.2f),
            new Keyframe(0.78f, 0.7f),
            new Keyframe(1f, 1f));
    }

    private static Vector3 ResolveFallbackMagicOrigin(Vector3 target)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            Transform cameraTransform = mainCamera.transform;
            return cameraTransform.position + cameraTransform.forward * 0.45f;
        }

        return target + Vector3.up * 1.25f - Vector3.forward * 1.5f;
    }

    private static float ResolveParticleLifetime(GameObject root)
    {
        float lifetime = 1f;
        ParticleSystem[] particles = root.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particles.Length; i++)
        {
            ParticleSystem.MainModule main = particles[i].main;
            float startLifetime = main.startLifetime.constantMax;
            lifetime = Mathf.Max(lifetime, main.duration + startLifetime);
        }

        return lifetime;
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

    private IEnumerator DoPhysicalWobble(Vector3 hitDir)
    {
        float elapsed = 0f;
        float velocity = wobbleIntensity * 10f; // Initial impulse velocity
        float currentAngle = 0f;

        // Calculate rotation axis perpendicular to hit direction (Cross product logic)
        // If hit on right (X+), rotate around Z- axis.
        Vector3 rotationAxis = new Vector3(hitDir.z, 0, -hitDir.x);

        WaterDropSlide[] drops = GetComponentsInChildren<WaterDropSlide>();
        foreach (var drop in drops)
        {
            drop.StartSliding(hitDir.normalized); 
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            // Spring Physics Calculation
            float force = -stiffness * currentAngle; 
            velocity += force * Time.deltaTime;
            velocity *= (1f - damping * Time.deltaTime); // Apply energy loss
            currentAngle += velocity * Time.deltaTime;

            // Apply rotation using AngleAxis along the calculated perpendicular axis
            transform.localRotation = originalRotation * Quaternion.AngleAxis(currentAngle, rotationAxis);

            yield return null;
            
            // Early exit if vibration is negligible
            if (Mathf.Abs(currentAngle) < 0.05f && elapsed > 0.2f) break;
        }

        transform.localRotation = originalRotation;
    }

    public void ConfigureDebug(AudioSource source, AudioClip clip)
    {
        audioSource = source;
        noteClip = clip;
        if (rippleController == null) rippleController = GetComponentInChildren<LotusRippleController>(true);
    }

    public void SetSettings(LotusScaleSettingsSO scaleSettings)
    {
        settings = scaleSettings;
        ApplySettings();
    }

    private bool IsAllowedCollider(Collider other)
    {
        if (other == null) return false;
        if (allowedTags == null || allowedTags.Length == 0) return true;

        for (int i = 0; i < allowedTags.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(allowedTags[i]) && other.CompareTag(allowedTags[i])) return true;
        }
        return false;
    }

    private void ApplySettings()
    {
        if (settings == null) return;

        cooldownSeconds = settings.cooldownSeconds;
        // if (settings.sharedNoteClip != null) noteClip = settings.sharedNoteClip;

        if (audioSource != null)
        {
            audioSource.volume = settings.volume;
            audioSource.minDistance = settings.minDistance;
            audioSource.maxDistance = Mathf.Max(settings.maxDistance, minimumAudibleDistance);
            audioSource.spatialBlend = impactAudioSpatialBlend;
        }

        if (rippleController != null) rippleController.SetSettings(settings);
    }
}
