using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class PetalPollenMagicController : MonoBehaviour
{
    private enum ParticleStage
    {
        FlowingToHand,
        Holding,
        Releasing
    }

    private class MagicParticle
    {
        public ParticleStage stage;
        public Vector3 startPosition;
        public Vector3 controlPosition;
        public Vector3 currentPosition;
        public Vector3 releaseStartPosition;
        public Vector3 releaseSeedPosition;
        public Vector3 releaseDirection;
        public Color color;
        public float age;
        public float releaseAge;
        public float seed;
        public float size;
        public float flowDuration;
        public float releaseOrder;
        public bool isPetal;
    }

    [Header("References")]
    [SerializeField] private Transform handAnchor;
    [SerializeField] private Transform playerHead;
    [SerializeField] private ParticleSystem particleOutput;
    [SerializeField] private ParticleSystem petalOutput;

    [SerializeField] private InputActionReference collectAction;
    [SerializeField] private List<PetalPollenSource> sources = new List<PetalPollenSource>();
    [SerializeField] private bool autoDiscoverSources = true;

    [Header("Beginner Debug")]
    [SerializeField] private bool enableKeyboardFallback = true;
    [SerializeField] private Key keyboardCollectKey = Key.Space;

    [Header("Collection")]
    [SerializeField] private float collectionRadius = 3f;
    [SerializeField] private int maxParticles = 900;
    [SerializeField] private float particlesPerSecond = 180f;
    [Range(0f, 1f)]
    [SerializeField] private float petalChance = 0.18f;
    [SerializeField] private float flowDuration = 0.95f;
    [SerializeField] private float flowArcHeight = 0.75f;
    [SerializeField] private float flowSwirlRadius = 0.16f;

    [Header("Living Sphere")]
    [SerializeField] private float holdDistance = 0.48f;
    [SerializeField] private float holdRadius = 0.32f;
    [SerializeField] private float orbitSpeedDegrees = 46f;
    [SerializeField] private float sphereBreathingAmount = 0.07f;
    [SerializeField] private float sphereJitter = 0.025f;

    [Header("Motion Trails")]
    [SerializeField] private bool enableMotionTrails = true;
    [SerializeField] private Color pollenTrailColor = new Color(1.25f, 0.9f, 0.22f, 1f);
    [SerializeField] private Color petalTrailColor = new Color(1f, 0.78f, 0.9f, 1f);

    [Header("VR Performance")]
    [Range(0.35f, 1f)]
    [SerializeField] private float effectParticleBudgetScale = 0.75f;
    [SerializeField] private int maxRenderedParticlesPerSystem = 1600;

    [Header("Gather Tail")]
    [SerializeField] private float gatherTailLifetime = 0.28f;
    [SerializeField] private float gatherTailMinVertexDistance = 0.018f;
    [SerializeField] private float gatherPollenTailWidth = 0.026f;
    [SerializeField] private float gatherPetalTailWidth = 0.036f;
    [Range(0f, 1f)]
    [SerializeField] private float gatherTailAlpha = 0.22f;

    [Header("Release Tail")]
    [SerializeField] private float releaseLongTailLifetime = 0.52f;
    [SerializeField] private float releaseLongTailMinVertexDistance = 0.018f;
    [SerializeField] private float releasePollenLongTailWidth = 0.038f;
    [SerializeField] private float releasePetalLongTailWidth = 0.052f;
    [Range(0f, 1f)]
    [SerializeField] private float releaseLongTailAlpha = 0.38f;

    [Header("Release")]
    [SerializeField] private bool randomizeReleaseMode = true;
    [SerializeField] private PetalPollenReleaseMode fixedReleaseMode = PetalPollenReleaseMode.GalaxyVeil;
    [SerializeField] private float releaseDuration = 6.5f;
    [SerializeField] private float chargedHoldSeconds = 3f;
    [SerializeField] private float chargedReleaseRadiusBoost = 0.28f;
    [SerializeField] private float chargedReleaseHeightBoost = 0.18f;
    [SerializeField] private float chargedReleaseBrightnessBoost = 0.22f;
    [SerializeField] private float chargedReleaseSizeBoost = 0.16f;
    [SerializeField] private float releaseFlashSeconds = 0.38f;
    [SerializeField] private float releaseModeBlendSeconds = 0.22f;
    [SerializeField] private float releaseSeedRadius = 0.07f;
    [SerializeField] private float releaseBloomSpeed = 3.2f;
    [SerializeField] private float burstRadius = 1.25f;

    [Header("Release Impact")]
    [SerializeField] private bool enableReleaseShockwave = true;
    [SerializeField] private int releaseShockwaveParticleCount = 96;
    [SerializeField] private float releaseShockwaveDuration = 0.62f;
    [SerializeField] private float releaseShockwaveStartRadius = 0.18f;
    [SerializeField] private float releaseShockwaveEndRadius = 1.15f;
    [SerializeField] private float releaseShockwaveHeight = 0.18f;
    [SerializeField] private float releaseShockwaveParticleSize = 0.052f;
    [Range(0f, 1f)]
    [SerializeField] private float releaseShockwaveAlpha = 0.46f;

    [Header("Release Afterglow")]
    [SerializeField] private bool enableReleaseAfterglow = true;
    [SerializeField] private int releaseAfterglowParticleCount = 90;
    [SerializeField] private float releaseAfterglowStart = 0.62f;
    [SerializeField] private float releaseAfterglowRadius = 1.35f;
    [SerializeField] private float releaseAfterglowHeight = 1.15f;
    [SerializeField] private float releaseAfterglowParticleSize = 0.032f;
    [Range(0f, 1f)]
    [SerializeField] private float releaseAfterglowAlpha = 0.34f;
    [SerializeField] private float releaseSettleSeconds = 0.8f;

    [Header("Release Modes")]
    [SerializeField] private float galaxyRadius = 1.35f;
    [SerializeField] private float galaxyHeight = 0.7f;
    [SerializeField] private float mathRibbonScale = 0.38f;
    [SerializeField] private float mathRibbonB = 0.4f;
    [SerializeField] private float mathRibbonURange = 6f;
    [SerializeField] private float mathRibbonVRange = 24f;
    [SerializeField] private float mathRibbonSurfaceWidth = 0.55f;
    [SerializeField] private float mathRibbonGateDistance = 0.82f;
    [SerializeField] private float mathRibbonViewDistance = 1.55f;
    [SerializeField] private float mathRibbonViewHeight = 0.48f;
    [SerializeField] private float mathRibbonSideOffset = 0f;
    [SerializeField] private float mathRibbonImpactScale = 1.45f;
    [SerializeField] private float mathRibbonDepthScale = 1.12f;
    [SerializeField] private float mathRibbonForwardPush = 0.34f;
    [SerializeField] private float mathRibbonVerticalScale = 1.75f;
    [SerializeField] private float mathRibbonCanopyLift = 0.62f;
    [SerializeField] private float mathRibbonParticleSizeMultiplier = 1.28f;
    [SerializeField] private float mathRibbonScatterRadius = 0.72f;
    [SerializeField] private float tornadoGateDistance = 0.85f;
    [SerializeField] private float tornadoViewDistance = 1.35f;
    [SerializeField] private float tornadoViewHeight = 0.18f;
    [SerializeField] private float tornadoHeight = 2.75f;
    [SerializeField] private float tornadoBaseRadius = 0.16f;
    [SerializeField] private float tornadoTopRadius = 1.15f;
    [SerializeField] private float tornadoSpinSpeed = 8.2f;
    [SerializeField] private float tornadoTremble = 0.22f;
    [SerializeField] private float tornadoDissolveRadius = 1.25f;
    [SerializeField] private float tornadoParticleSizeMultiplier = 1.25f;
    [SerializeField] private float tornadoDustFraction = 0.22f;
    [SerializeField] private float tornadoDustRadius = 0.58f;
    [SerializeField] private float tornadoDustHeight = 0.18f;
    [SerializeField] private float tornadoDustSizeMultiplier = 0.42f;
    [SerializeField] private float tornadoWispStartRadius = 0.08f;
    [SerializeField] private float tornadoWispEndRadius = 0.58f;
    [SerializeField] private float tornadoWispTwistStrength = 1.65f;
    [SerializeField] private float tornadoWispRiseHeight = 1.5f;
    [SerializeField] private float tornadoWispRevealSpread = 0.42f;
    [SerializeField] private float tornadoWispStrandWidth = 0.16f;
    [SerializeField] private float tornadoWispPreludePortion = 0.34f;
    [SerializeField] private float tornadoWispYawTurns = 1.05f;

    [Header("Weighted Surprise")]
    [SerializeField] private float spiralBloomWeight = 0.28f;
    [SerializeField] private float mathRibbonWeight = 0.25f;
    [SerializeField] private float tornadoVortexWeight = 0.32f;
    [SerializeField] private float galaxyVeilWeight = 0.3f;
    [SerializeField] private float chargedGalaxyBonusWeight = 0.35f;

    [Header("Look")]
    [SerializeField] private float pollenSize = 0.045f;
    [SerializeField] private float petalSize = 0.12f;
    [SerializeField] private Color secondaryPollenColor = new Color(0.62f, 0.95f, 1f, 1f);
    [SerializeField] private Color galaxyViolet = new Color(0.72f, 0.48f, 1f, 1f);

    [Header("Charge Halo")]
    [SerializeField] private bool enableChargeHalo = true;
    [SerializeField] private int chargeHaloParticleCount = 72;
    [SerializeField] private float chargeHaloRadius = 0.46f;
    [SerializeField] private float chargeHaloVerticalScale = 0.34f;
    [SerializeField] private float chargeHaloParticleSize = 0.034f;
    [Range(0f, 1f)]
    [SerializeField] private float chargeHaloAlpha = 0.46f;

    [Header("Feedback")]
    [SerializeField] private AudioSource magicAudioSource;
    [SerializeField] private AudioClip collectStartClip;
    [SerializeField] private AudioClip releaseClip;
    [SerializeField] private AudioClip chargedReleaseClip;
    [SerializeField] private float collectStartVolume = 0.35f;
    [SerializeField] private float releaseVolume = 0.7f;
    [SerializeField] private bool enableReleaseLightFlash = true;
    [SerializeField] private Color releaseLightColor = new Color(1f, 0.72f, 0.34f, 1f);
    [SerializeField] private float releaseLightIntensity = 2.8f;
    [SerializeField] private float releaseLightRange = 2.6f;
    [SerializeField] private float releaseLightDuration = 0.42f;
    [SerializeField] private bool enableSourceFocusFeedback = true;
    [SerializeField] private float sourceFocusRadius = 2.4f;
    [SerializeField] private float collectingSourceFocusBoost = 0.35f;

    private readonly List<MagicParticle> activeParticles = new List<MagicParticle>();
    private ParticleSystem.Particle[] particleBuffer = new ParticleSystem.Particle[0];
    private ParticleSystem.Particle[] petalBuffer = new ParticleSystem.Particle[0];

    private float spawnAccumulator;
    private float collectStartTime;
    private float releaseStartTime;
    private float releaseCharge;
    private bool isCollecting;
    private bool releaseActive;
    private PetalPollenReleaseMode activeReleaseMode;
    private Vector3 releaseShowcaseCenter;
    private Vector3 releaseOriginCenter;
    private Vector3 releaseMathRibbonGateCenter;
    private Vector3 releaseTornadoCenter;
    private Vector3 releaseTornadoGateCenter;
    private Quaternion releaseTornadoPose = Quaternion.identity;
    private Quaternion releaseShowcasePose = Quaternion.identity;
    private bool hasReleaseShowcasePose;
    private Light releaseLight;
    private float releaseLightAge;
    private float releaseLightPeakIntensity;
    private float releaseLightPeakRange;
    private bool releaseLightActive;

    private void Awake()
    {
        EnsureParticleOutput();
        RefreshSourcesIfNeeded();
    }

    private void OnEnable()
    {
        collectAction?.action?.Enable();
    }

    private void OnDisable()
    {
        collectAction?.action?.Disable();
    }

    private void Update()
    {
        UpdateInput();
        UpdateSourceFocusFeedback();

        if (isCollecting)
        {
            SpawnCollectionParticles();
        }

        UpdateMagicParticles();
        UpdateReleaseLightFeedback();
        RenderParticles();
    }

    public void BeginCollect()
    {
        if (handAnchor == null)
        {
            Debug.LogWarning("[PetalPollenMagic] Assign a hand anchor before collecting.", this);
            return;
        }

        RefreshSourcesIfNeeded();

        if (isCollecting)
        {
            return;
        }

        isCollecting = true;
        releaseActive = false;
        hasReleaseShowcasePose = false;
        collectStartTime = Time.time;
        spawnAccumulator = 0f;
        PlayMagicClip(collectStartClip, collectStartVolume);
    }

    public void Release()
    {
        if (!isCollecting && activeParticles.Count == 0)
        {
            return;
        }

        isCollecting = false;
        releaseActive = true;
        releaseStartTime = Time.time;
        float holdSeconds = Time.time - collectStartTime;
        releaseCharge = Mathf.Clamp01(holdSeconds / Mathf.Max(0.01f, chargedHoldSeconds));
        activeReleaseMode = randomizeReleaseMode ? PickReleaseMode(holdSeconds) : fixedReleaseMode;
        CaptureReleaseShowcasePose();
        PlayMagicClip(holdSeconds >= chargedHoldSeconds && chargedReleaseClip != null ? chargedReleaseClip : releaseClip, releaseVolume);
        BeginReleaseLightFeedback(holdSeconds >= chargedHoldSeconds);

        Vector3 center = GetHoldCenter();
        releaseOriginCenter = center;
        int releaseCount = Mathf.Max(1, activeParticles.Count - 1);
        for (int i = 0; i < activeParticles.Count; i++)
        {
            MagicParticle particle = activeParticles[i];
            particle.stage = ParticleStage.Releasing;
            particle.releaseAge = 0f;
            particle.releaseOrder = i / (float)releaseCount;
            particle.releaseStartPosition = particle.currentPosition;
            particle.releaseDirection = (particle.currentPosition - center).normalized;
            if (particle.releaseDirection.sqrMagnitude < 0.001f)
            {
                particle.releaseDirection = Random.onUnitSphere;
            }

            float seedRadius = particle.isPetal ? releaseSeedRadius * 1.35f : releaseSeedRadius;
            particle.releaseSeedPosition = center + particle.releaseDirection * Random.Range(seedRadius * 0.25f, seedRadius);
        }
    }

    public void Clear()
    {
        isCollecting = false;
        releaseActive = false;
        hasReleaseShowcasePose = false;
        activeParticles.Clear();
        releaseCharge = 0f;
        if (particleOutput != null)
        {
            particleOutput.Clear(true);
        }

        if (petalOutput != null)
        {
            petalOutput.Clear(true);
        }

        if (releaseLight != null)
        {
            releaseLight.enabled = false;
        }

        releaseLightActive = false;
    }

    private void UpdateInput()
    {
        InputAction action = collectAction != null ? collectAction.action : null;
        bool pressed = action != null && action.WasPressedThisFrame();
        bool released = action != null && action.WasReleasedThisFrame();

        if (enableKeyboardFallback && Keyboard.current != null)
        {
            pressed |= Keyboard.current[keyboardCollectKey].wasPressedThisFrame;
            released |= Keyboard.current[keyboardCollectKey].wasReleasedThisFrame;
        }

        if (pressed)
        {
            BeginCollect();
        }

        if (released)
        {
            Release();
        }
    }

    private void SpawnCollectionParticles()
    {
        spawnAccumulator += particlesPerSecond * Time.deltaTime;

        while (spawnAccumulator >= 1f && activeParticles.Count < maxParticles)
        {
            spawnAccumulator -= 1f;
            PetalPollenSource source = PickNearestSource();
            if (source == null)
            {
                return;
            }

            activeParticles.Add(CreateParticle(source));
        }
    }

    private MagicParticle CreateParticle(PetalPollenSource source)
    {
        bool isPetal = source.EmitPetals && Random.value < petalChance;
        source.NotifyExtracted(isPetal);
        Vector3 start = source.GetSpawnPosition();
        Vector3 holdCenter = GetHoldCenter();
        Vector3 sourceToHand = holdCenter - start;
        Vector3 side = Vector3.Cross(sourceToHand.normalized, Vector3.up);
        if (side.sqrMagnitude < 0.001f)
        {
            side = transform.right;
        }

        side.Normalize();
        Vector3 control = Vector3.Lerp(start, holdCenter, 0.48f)
            + Vector3.up * flowArcHeight
            + side * Random.Range(-0.45f, 0.45f);

        Color color = isPetal
            ? Color.Lerp(source.PetalColor, petalTrailColor, 0.32f)
            : Color.Lerp(source.PollenColor, secondaryPollenColor, Random.Range(0f, 0.35f));

        return new MagicParticle
        {
            stage = ParticleStage.FlowingToHand,
            startPosition = start,
            controlPosition = control,
            currentPosition = start,
            color = color,
            age = 0f,
            releaseAge = 0f,
            seed = Random.Range(0f, 1000f),
            size = isPetal ? petalSize * Random.Range(0.75f, 1.35f) : pollenSize * Random.Range(0.75f, 1.25f),
            flowDuration = flowDuration * Random.Range(0.75f, 1.25f),
            isPetal = isPetal
        };
    }

    private void UpdateMagicParticles()
    {
        for (int i = activeParticles.Count - 1; i >= 0; i--)
        {
            MagicParticle particle = activeParticles[i];
            particle.age += Time.deltaTime;

            switch (particle.stage)
            {
                case ParticleStage.FlowingToHand:
                    UpdateFlowingParticle(particle);
                    break;
                case ParticleStage.Holding:
                    UpdateHoldingParticle(particle, i, activeParticles.Count);
                    break;
                case ParticleStage.Releasing:
                    UpdateReleasingParticle(particle, i, activeParticles.Count);
                    if (particle.releaseAge > releaseDuration)
                    {
                        activeParticles.RemoveAt(i);
                    }
                    break;
            }
        }

        if (releaseActive && activeParticles.Count == 0 && Time.time - releaseStartTime > releaseDuration + releaseSettleSeconds)
        {
            releaseActive = false;
        }
    }

    private void UpdateFlowingParticle(MagicParticle particle)
    {
        float t = Mathf.Clamp01(particle.age / Mathf.Max(0.01f, particle.flowDuration));
        float eased = Smoother01(t);
        Vector3 end = GetHoldCenter();
        Vector3 curve = QuadraticBezier(particle.startPosition, particle.controlPosition, end, eased);

        Vector3 handForward = handAnchor != null ? handAnchor.forward : transform.forward;
        Vector3 handRight = handAnchor != null ? handAnchor.right : transform.right;
        Vector3 handUp = handAnchor != null ? handAnchor.up : transform.up;
        float swirl = Mathf.Sin(t * Mathf.PI * 5.5f + particle.seed);
        float swirlRadius = flowSwirlRadius * Mathf.Sin(t * Mathf.PI);
        Vector3 swirlOffset = (handRight * swirl + handUp * Mathf.Cos(t * Mathf.PI * 5.5f + particle.seed)) * swirlRadius;

        particle.currentPosition = curve + swirlOffset + handForward * Mathf.Sin(t * Mathf.PI) * 0.08f;

        if (t >= 1f)
        {
            particle.stage = ParticleStage.Holding;
            particle.age = 0f;
        }
    }

    private void UpdateHoldingParticle(MagicParticle particle, int index, int count)
    {
        Vector3 spherePoint = FibonacciSphere(index, Mathf.Max(1, count));
        float orbitAngle = Time.time * orbitSpeedDegrees + particle.seed * 31f;
        Quaternion orbit = Quaternion.AngleAxis(orbitAngle, Vector3.up)
            * Quaternion.AngleAxis(Mathf.Sin(particle.seed) * 35f, Vector3.right);

        float pulse = 1f + Mathf.Sin(Time.time * 2.1f + particle.seed) * sphereBreathingAmount;
        float charge = GetCharge01();
        float petalOuter = particle.isPetal ? 1.22f : 1f;
        Vector3 target = GetHoldCenter() + orbit * (spherePoint * holdRadius * pulse * petalOuter * Mathf.Lerp(1f, 1.16f, charge));
        target += ResolveSoftJitter(particle.seed, Time.time, sphereJitter);

        particle.currentPosition = Vector3.Lerp(particle.currentPosition, target, Time.deltaTime * 8.5f);
    }

    private void UpdateReleasingParticle(MagicParticle particle, int index, int count)
    {
        particle.releaseAge += Time.deltaTime;
        float t = Mathf.Clamp01(particle.releaseAge / Mathf.Max(0.01f, releaseDuration));

        if (particle.releaseAge < releaseFlashSeconds)
        {
            float flashT = particle.releaseAge / Mathf.Max(0.01f, releaseFlashSeconds);
            float compressT = Smoother01(flashT);
            particle.currentPosition = Vector3.Lerp(particle.releaseStartPosition, particle.releaseSeedPosition, compressT);
            return;
        }

        float showT = Mathf.Clamp01((particle.releaseAge - releaseFlashSeconds) / Mathf.Max(0.01f, releaseDuration - releaseFlashSeconds));
        Vector3 target;
        switch (activeReleaseMode)
        {
            case PetalPollenReleaseMode.SpiralBloom:
                target = ResolveSpiralBloomPosition(particle, index, count, showT);
                break;
            case PetalPollenReleaseMode.MathRibbon:
                target = ResolveMathRibbonPosition(particle, index, count, showT);
                break;
            case PetalPollenReleaseMode.TornadoVortex:
                target = ResolveTornadoVortexPosition(particle, index, count, showT);
                break;
            default:
                target = ResolveGalaxyVeilPosition(particle, index, count, showT);
                break;
        }

        float blendT = Smoother01(Mathf.Clamp01((particle.releaseAge - releaseFlashSeconds) / Mathf.Max(0.01f, releaseModeBlendSeconds)));
        particle.currentPosition = Vector3.Lerp(particle.releaseSeedPosition, target, blendT);
    }

    private Vector3 ResolveGalaxyVeilPosition(MagicParticle particle, int index, int count, float t)
    {
        float bloomT = ReleaseBloom01(t);
        Vector3 center = GetPlayerCenter();
        float radiusImpact = GetChargedReleaseRadiusScale();
        float heightImpact = GetChargedReleaseHeightScale();
        float strand = index % 2 == 0 ? 1f : -1f;
        float angle = particle.seed + (bloomT * Mathf.PI * 1.35f + t * Mathf.PI * 2.15f) * strand + index * 0.037f;
        float radius = Mathf.Lerp(0.18f, galaxyRadius * radiusImpact * (particle.isPetal ? 1.05f : 1f), bloomT);
        float arm = Mathf.Sin(angle * 2f + particle.seed) * 0.28f;
        float height = Mathf.Sin(angle * 1.7f + particle.seed) * galaxyHeight * heightImpact * (0.35f + bloomT * 0.65f);

        Vector3 local = new Vector3(
            Mathf.Cos(angle) * (radius + arm),
            height,
            Mathf.Sin(angle) * (radius + arm));

        Quaternion tilt = Quaternion.Euler(18f, t * 115f, -12f);
        Vector3 drift = Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.24f;
        return Vector3.Lerp(particle.releaseSeedPosition, center + tilt * local + drift, bloomT);
    }

    private Vector3 ResolveSpiralBloomPosition(MagicParticle particle, int index, int count, float t)
    {
        float bloomT = ReleaseBloom01(t);
        Vector3 center = GetHoldCenter();
        float radiusImpact = GetChargedReleaseRadiusScale();
        float heightImpact = GetChargedReleaseHeightScale();
        float progress = index / Mathf.Max(1f, count - 1f);
        float strand = index % 2 == 0 ? 1f : -1f;
        float angle = progress * Mathf.PI * 10f + (bloomT * Mathf.PI * 2f + t * Mathf.PI * 3f) * strand + particle.seed;
        float radius = Mathf.Lerp(0.08f, burstRadius * radiusImpact, bloomT);
        float height = (Mathf.Lerp(-0.7f, 1.4f, progress) + Mathf.Sin(angle * 1.6f) * 0.18f) * heightImpact;
        Vector3 target = center + new Vector3(Mathf.Cos(angle) * radius, height, Mathf.Sin(angle) * radius);
        return Vector3.Lerp(particle.releaseSeedPosition, target, bloomT);
    }

    private Vector3 ResolveMathRibbonPosition(MagicParticle particle, int index, int count, float t)
    {
        float bloomT = ReleaseBloom01(t);
        Vector3 center = ResolveMathRibbonCenter();
        Vector3 gateCenter = ResolveMathRibbonGateCenter();

        int uCount = Mathf.Max(12, Mathf.RoundToInt(Mathf.Sqrt(count) * 1.2f));
        int vCount = Mathf.Max(12, Mathf.CeilToInt(count / (float)uCount));
        int uIndex = index % uCount;
        int vIndex = (index / uCount) % vCount;

        float u01 = uIndex / Mathf.Max(1f, uCount - 1f);
        float v01 = vIndex / Mathf.Max(1f, vCount - 1f);
        float revealHead = Smoother01(Mathf.Clamp01(t / 0.62f));
        float revealOrder = Mathf.Lerp(u01, v01, 0.18f);
        float revealT = Smoother01(Mathf.Clamp01((revealHead - revealOrder) / 0.12f));

        float u = Mathf.Lerp(-mathRibbonURange * 0.5f, mathRibbonURange * 0.5f, u01);
        float vCenter = Mathf.Lerp(-mathRibbonVRange * 0.5f, mathRibbonVRange * 0.5f, v01);
        float surfaceOffset = (Mathf.Repeat(index * 0.6180339f + particle.seed * 0.01f, 1f) - 0.5f) * mathRibbonSurfaceWidth;
        float v = vCenter + surfaceOffset;

        float radiusImpact = GetChargedReleaseRadiusScale();
        float heightImpact = GetChargedReleaseHeightScale();
        Vector3 breatherPoint = ResolveBreatherSurfacePoint(u, v);
        float safeDepthScale = Mathf.Clamp(mathRibbonDepthScale, 0.65f, 1.15f);
        float safeForwardPush = Mathf.Clamp(mathRibbonForwardPush, 0.08f, 0.75f);
        breatherPoint.z = Mathf.Abs(breatherPoint.z) * safeDepthScale + safeForwardPush;
        breatherPoint.x *= radiusImpact;
        breatherPoint.y *= heightImpact;
        breatherPoint.z *= Mathf.Lerp(1f, radiusImpact, 0.45f);
        breatherPoint += ResolveSoftJitter(particle.seed, Time.time, particle.isPetal ? 0.01f : 0.006f);
        Quaternion pose = ResolveMathRibbonPose(center, t);
        Vector3 target = center + pose * breatherPoint;

        float scatterT = Smoother01(Mathf.Clamp01((t - 0.42f) / 0.58f));
        if (scatterT > 0f)
        {
            float safeScatterRadius = Mathf.Clamp(mathRibbonScatterRadius, 0.25f, 0.85f);
            float driftSeed = particle.seed + revealOrder * 13f;
            Vector3 drift = new Vector3(
                Mathf.Sin(Time.time * 0.45f + driftSeed),
                Mathf.Cos(Time.time * 0.32f + driftSeed) * 0.55f,
                Mathf.Cos(Time.time * 0.38f + driftSeed)) * scatterT * safeScatterRadius * (particle.isPetal ? 0.95f : 0.58f);

            Vector3 tangent = ResolveBreatherSurfaceTangent(u, v);
            Vector3 tangentDrift = pose * tangent * scatterT * Mathf.Sin(revealOrder * Mathf.PI * 2f + Time.time * 0.6f) * 0.36f;
            Vector3 canopyFloat = Vector3.up * Mathf.Sin(scatterT * Mathf.PI) * (particle.isPetal ? 0.34f : 0.5f);
            Vector3 gentleFall = Vector3.down * scatterT * scatterT * (particle.isPetal ? 0.34f : 0.12f);
            target += drift + tangentDrift + canopyFloat + gentleFall;
        }

        Vector3 headLocal = ResolveBreatherSurfacePoint(
            Mathf.Lerp(-mathRibbonURange * 0.5f, mathRibbonURange * 0.5f, revealHead),
            Mathf.Lerp(-mathRibbonVRange * 0.5f, mathRibbonVRange * 0.5f, revealHead));
        headLocal.z = Mathf.Abs(headLocal.z) * safeDepthScale + safeForwardPush;
        headLocal.x *= radiusImpact;
        headLocal.y *= heightImpact;
        headLocal.z *= Mathf.Lerp(1f, radiusImpact, 0.45f);
        Vector3 headPoint = center + pose * headLocal;
        Vector3 revealPoint = Vector3.Lerp(headPoint, target, revealT);
        Vector3 launchLift = Vector3.up * Mathf.Sin(bloomT * Mathf.PI) * 0.42f;
        Vector3 gateJitter = pose * ResolveSoftJitter(particle.seed, Time.time, particle.isPetal ? 0.055f : 0.035f);
        Vector3 gatePoint = gateCenter + gateJitter;
        float gateT = Smoother01(Mathf.Clamp01(bloomT / 0.42f));
        float unfoldT = Smoother01(Mathf.Clamp01((bloomT - 0.18f) / 0.82f));
        Vector3 toGate = Vector3.Lerp(particle.releaseSeedPosition, gatePoint, gateT);
        return Vector3.Lerp(toGate, revealPoint + launchLift, unfoldT);
    }

    private Vector3 ResolveTornadoVortexPosition(MagicParticle particle, int index, int count, float t)
    {
        float preludePortion = Mathf.Clamp(tornadoWispPreludePortion, 0.16f, 0.42f);
        float preludeT = Smoother01(Mathf.Clamp01(t / preludePortion));
        float accelerateT = EaseInCubic(preludeT);
        float burstT = EaseOutCubic(Mathf.Clamp01((t - preludePortion) / 0.22f));
        float formT = Smoother01(Mathf.Clamp01((t - preludePortion * 0.86f) / 0.3f));
        float dissolveT = Smoother01(Mathf.Clamp01((t - 0.58f) / 0.42f));

        Vector3 center = ResolveTornadoCenter();
        Vector3 gateCenter = ResolveTornadoGateCenter();
        Quaternion pose = ResolveTornadoPose(center);

        bool isDust = IsTornadoDustParticle(particle);
        float height01 = ResolveTornadoFlow01(particle, index, count, isDust);
        float radiusImpact = GetChargedReleaseRadiusScale();
        float heightImpact = GetChargedReleaseHeightScale();
        float strand = index % 2 == 0 ? 1f : -1f;
        float safeHeight = Mathf.Clamp(tornadoHeight * heightImpact, 1.2f, 3.8f);
        float baseRadius = Mathf.Clamp(tornadoBaseRadius * radiusImpact, 0.08f, 0.68f);
        float topRadius = Mathf.Clamp(tornadoTopRadius * radiusImpact, baseRadius + 0.1f, 1.7f);
        float spinSpeed = Mathf.Clamp(tornadoSpinSpeed, 2f, 13f);
        float tremble = Mathf.Clamp(tornadoTremble, 0.02f, 0.36f);
        float dissolveRadius = Mathf.Clamp(tornadoDissolveRadius * radiusImpact, 0.45f, 2.4f);

        float twist = height01 * Mathf.PI * 7.5f + particle.seed * 0.19f;
        float spin = Time.time * spinSpeed * strand + t * Mathf.PI * 10f + twist;
        float breathing = 1f + Mathf.Sin(Time.time * 8.5f + particle.seed + height01 * 9f) * tremble;
        float waist = 0.82f + Mathf.Sin(height01 * Mathf.PI) * 0.22f;
        float radius = Mathf.Lerp(baseRadius, topRadius, height01) * waist * breathing;
        radius *= Mathf.Lerp(0.18f, 1f, formT);

        Vector3 radial = new Vector3(Mathf.Cos(spin), 0f, Mathf.Sin(spin));
        Vector3 local = radial * radius;
        local.y = Mathf.Lerp(-0.45f, safeHeight, height01) * Mathf.Lerp(0.35f, 1f, formT);
        local.z += 0.16f + height01 * 0.26f;

        if (isDust)
        {
            float dustSeed = Deterministic01(particle.seed * 0.37f + 5.1f);
            float dustRadius = Mathf.Clamp(tornadoDustRadius, baseRadius, 1.05f);
            float dustHeight = Mathf.Clamp(tornadoDustHeight, 0.04f, 0.42f);
            float dustSpin = Time.time * spinSpeed * 1.35f + particle.seed * 0.41f + t * Mathf.PI * 12f;
            spin = dustSpin;
            radial = new Vector3(Mathf.Cos(dustSpin), 0f, Mathf.Sin(dustSpin));
            radius = Mathf.Lerp(baseRadius * 0.55f, dustRadius, dustSeed) * Mathf.Lerp(0.28f, 1f, formT);
            local = radial * radius;
            local.y = -0.5f + dustHeight * dustSeed + Mathf.Sin(Time.time * 9f + particle.seed) * 0.025f;
            local.z += Mathf.Lerp(0.04f, 0.18f, dustSeed);
        }

        Vector3 spineWobble = new Vector3(
            Mathf.Sin(Time.time * 2.7f + height01 * 8f + particle.seed),
            Mathf.Sin(Time.time * 3.3f + particle.seed) * 0.18f,
            Mathf.Cos(Time.time * 2.2f + height01 * 7f + particle.seed)) * tremble * Mathf.Lerp(0.25f, 1f, formT);

        Vector3 target = center + pose * (local + spineWobble);
        if (dissolveT > 0f)
        {
            Vector3 worldRadial = pose * radial;
            Vector3 upwardBloom = Vector3.up * Mathf.Sin(dissolveT * Mathf.PI) * (isDust ? 0.12f : (particle.isPetal ? 0.35f : 0.52f));
            Vector3 outward = worldRadial * dissolveRadius * dissolveT * dissolveT * (isDust ? 0.55f : 1f);
            Vector3 airyFall = Vector3.down * dissolveT * dissolveT * (isDust ? 0.05f : (particle.isPetal ? 0.36f : 0.12f));
            target += outward + upwardBloom + airyFall;
        }

        Vector3 preludeCloud = ResolveTornadoPreludeCloudPoint(particle, index, count, pose, gateCenter, center, preludeT, accelerateT, isDust);
        return Vector3.Lerp(preludeCloud, target, burstT);
    }

    private Vector3 ResolveTornadoPreludeCloudPoint(MagicParticle particle, int index, int count, Quaternion pose, Vector3 gateCenter, Vector3 tornadoCenter, float preludeT, float accelerateT, bool isDust)
    {
        Vector3 start = particle.releaseSeedPosition;
        float order = ResolveTornadoFlow01(particle, index, count, isDust);
        float revealWindow = Mathf.Clamp(tornadoWispRevealSpread, 0.08f, 0.65f);
        float revealT = Smoother01(Mathf.Clamp01((preludeT * (1f + revealWindow) - order * 0.82f) / Mathf.Max(0.001f, revealWindow)));

        float startRadius = Mathf.Clamp(tornadoWispStartRadius, 0.08f, 0.4f);
        float endRadius = Mathf.Clamp(tornadoWispEndRadius, startRadius + 0.08f, 1.15f);
        float twistStrength = Mathf.Clamp(tornadoWispTwistStrength, 0.5f, 3.5f);
        float layerRise = Mathf.Clamp(tornadoWispRiseHeight, 0.15f, 1.8f);
        float ribbonWidth = Mathf.Clamp(tornadoWispStrandWidth, 0f, 0.42f);

        float yawTremble = (Mathf.Sin(Time.time * Mathf.Lerp(1.4f, 6.2f, accelerateT))
            + Mathf.Sin(Time.time * Mathf.Lerp(3.1f, 10.5f, accelerateT)) * 0.38f) * Mathf.Lerp(0.8f, 7.5f, accelerateT);
        Quaternion yawSpin = Quaternion.AngleAxis(accelerateT * Mathf.Clamp(tornadoWispYawTurns, 0f, 2.5f) * 360f + yawTremble, Vector3.up);

        float heightT = order;
        int strandIndex = Mathf.Abs(index % 7);
        float strandAngle = (strandIndex / 7f) * Mathf.PI * 2f + Deterministic01(particle.seed * 0.031f + 1.9f) * 0.44f;
        float lane = Deterministic01(particle.seed * 0.131f + 4.6f) - 0.5f;
        float curl = Time.time * Mathf.Lerp(0.65f, 4.8f, accelerateT)
            + heightT * Mathf.Lerp(1.2f, 5.4f, accelerateT) * twistStrength
            + particle.seed * 0.073f;
        float radiusGrow = Smoother01(preludeT) * (0.36f + heightT * 0.64f);
        float radius = Mathf.Lerp(startRadius, endRadius, radiusGrow);
        radius *= isDust ? 0.34f : (particle.isPetal ? 0.74f : 0.58f);

        Vector3 sphereCenter = Vector3.Lerp(start, gateCenter, revealT * 0.42f);
        Vector3 baseCenter = Vector3.Lerp(sphereCenter, tornadoCenter, Smoother01(preludeT) * 0.58f);
        Vector3 verticalWisp = Vector3.up * (heightT * layerRise * (isDust ? 0.24f : 1f));

        Vector3 radial = new Vector3(
            Mathf.Cos(strandAngle + curl) * radius,
            0f,
            Mathf.Sin(strandAngle + curl) * radius);
        Vector3 strandLean = pose * new Vector3(
            Mathf.Sin(heightT * Mathf.PI + particle.seed) * ribbonWidth * 0.42f,
            0f,
            heightT * 0.2f);
        Vector3 jitter = new Vector3(
            Mathf.Sin(Time.time * 9.5f + particle.seed),
            Mathf.Sin(Time.time * 12.0f + heightT * 6f + particle.seed) * 0.45f,
            Mathf.Cos(Time.time * 8.2f + particle.seed)) * Mathf.Lerp(0.01f, 0.14f, accelerateT) * (isDust ? 0.45f : 1f);

        Vector3 wispTarget = baseCenter + pose * radial + verticalWisp + strandLean + jitter;
        Vector3 spinPivot = baseCenter + Vector3.up * layerRise * 0.48f;
        Vector3 rotatedTarget = spinPivot + yawSpin * (wispTarget - spinPivot);
        Vector3 liftDraw = Vector3.up * Mathf.Sin(revealT * Mathf.PI) * Mathf.Lerp(0.04f, 0.18f, accelerateT);
        return Vector3.Lerp(start, rotatedTarget + liftDraw, revealT);
    }

    private float ResolveTornadoFlow01(MagicParticle particle, int index, int count, bool isDust)
    {
        if (isDust)
        {
            return Deterministic01(particle.seed * 0.37f + 5.1f) * 0.18f;
        }

        float orderNoise = (Deterministic01(particle.seed * 0.071f + 2.4f) - 0.5f) * 0.045f;
        return Mathf.Clamp01(index / Mathf.Max(1f, count - 1f) + orderNoise);
    }

    private PetalPollenSource PickNearestSource()
    {
        RefreshSourcesIfNeeded();

        PetalPollenSource best = null;
        float bestDistance = float.MaxValue;
        Vector3 handPosition = handAnchor != null ? handAnchor.position : transform.position;

        for (int i = sources.Count - 1; i >= 0; i--)
        {
            PetalPollenSource source = sources[i];
            if (source == null)
            {
                sources.RemoveAt(i);
                continue;
            }

            float distance = Vector3.Distance(handPosition, source.transform.position);
            if (distance <= collectionRadius && distance < bestDistance)
            {
                best = source;
                bestDistance = distance;
            }
        }

        return best;
    }

    private void UpdateSourceFocusFeedback()
    {
        if (!enableSourceFocusFeedback)
        {
            return;
        }

        RefreshSourcesIfNeeded();

        Vector3 handPosition = handAnchor != null ? handAnchor.position : transform.position;
        float radius = Mathf.Max(0.01f, sourceFocusRadius);
        for (int i = sources.Count - 1; i >= 0; i--)
        {
            PetalPollenSource source = sources[i];
            if (source == null)
            {
                sources.RemoveAt(i);
                continue;
            }

            float distance = Vector3.Distance(handPosition, source.transform.position);
            if (distance > radius)
            {
                continue;
            }

            float focus = 1f - Mathf.Clamp01(distance / radius);
            focus = Smoother01(focus);
            if (isCollecting && distance <= collectionRadius)
            {
                focus = Mathf.Clamp01(focus + collectingSourceFocusBoost);
            }

            source.SetInteractionFocus(focus);
        }
    }

    private void RefreshSourcesIfNeeded()
    {
        if (!autoDiscoverSources)
        {
            return;
        }

        for (int i = 0; i < sources.Count; i++)
        {
            if (sources[i] != null)
            {
                return;
            }
        }

        sources.Clear();
        PetalPollenSource[] discovered = FindObjectsOfType<PetalPollenSource>(true);
        sources.AddRange(discovered);
    }

    private void PlayMagicClip(AudioClip clip, float volume)
    {
        if (clip == null)
        {
            return;
        }

        EnsureMagicAudioSource();
        magicAudioSource.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    private void EnsureMagicAudioSource()
    {
        if (magicAudioSource != null)
        {
            return;
        }

        magicAudioSource = GetComponentInChildren<AudioSource>(true);
        if (magicAudioSource == null)
        {
            magicAudioSource = gameObject.AddComponent<AudioSource>();
        }

        magicAudioSource.playOnAwake = false;
        magicAudioSource.spatialBlend = 1f;
        magicAudioSource.rolloffMode = AudioRolloffMode.Linear;
        magicAudioSource.maxDistance = 8f;
    }

    private void BeginReleaseLightFeedback(bool charged)
    {
        if (!enableReleaseLightFlash)
        {
            return;
        }

        EnsureReleaseLight();
        releaseLight.transform.position = GetHoldCenter();
        releaseLight.color = releaseLightColor;
        releaseLightPeakRange = releaseLightRange * (charged ? 1.25f : 1f);
        releaseLightPeakIntensity = releaseLightIntensity * (charged ? 1.35f : 1f);
        releaseLight.range = releaseLightPeakRange;
        releaseLight.intensity = releaseLightPeakIntensity;
        releaseLight.enabled = true;
        releaseLightAge = 0f;
        releaseLightActive = true;
    }

    private void UpdateReleaseLightFeedback()
    {
        if (!releaseLightActive || releaseLight == null)
        {
            return;
        }

        releaseLightAge += Time.deltaTime;
        float duration = Mathf.Max(0.05f, releaseLightDuration);
        float t = Mathf.Clamp01(releaseLightAge / duration);
        float fade = 1f - Smoother01(t);
        releaseLight.transform.position = Vector3.Lerp(releaseLight.transform.position, GetHoldCenter(), Time.deltaTime * 10f);
        releaseLight.intensity = releaseLightPeakIntensity * fade;
        releaseLight.range = releaseLightPeakRange * Mathf.Lerp(1.1f, 0.55f, t);

        if (t >= 1f)
        {
            releaseLight.enabled = false;
            releaseLightActive = false;
        }
    }

    private void EnsureReleaseLight()
    {
        if (releaseLight != null)
        {
            return;
        }

        GameObject child = new GameObject("PetalPollen_ReleaseLight");
        child.transform.SetParent(transform, false);
        releaseLight = child.AddComponent<Light>();
        releaseLight.type = LightType.Point;
        releaseLight.shadows = LightShadows.None;
        releaseLight.enabled = false;
    }

    private PetalPollenReleaseMode PickReleaseMode(float holdSeconds)
    {
        float galaxyWeight = galaxyVeilWeight;
        if (holdSeconds >= chargedHoldSeconds)
        {
            galaxyWeight += chargedGalaxyBonusWeight;
        }

        float total = Mathf.Max(0f, spiralBloomWeight)
            + Mathf.Max(0f, mathRibbonWeight)
            + Mathf.Max(0f, tornadoVortexWeight)
            + Mathf.Max(0f, galaxyWeight);

        if (total <= 0.001f)
        {
            return PetalPollenReleaseMode.GalaxyVeil;
        }

        float roll = Random.value * total;
        roll -= Mathf.Max(0f, spiralBloomWeight);
        if (roll <= 0f)
        {
            return PetalPollenReleaseMode.SpiralBloom;
        }

        roll -= Mathf.Max(0f, mathRibbonWeight);
        if (roll <= 0f)
        {
            return PetalPollenReleaseMode.MathRibbon;
        }

        roll -= Mathf.Max(0f, tornadoVortexWeight);
        if (roll <= 0f)
        {
            return PetalPollenReleaseMode.TornadoVortex;
        }

        return PetalPollenReleaseMode.GalaxyVeil;
    }

    private void RenderParticles()
    {
        EnsureParticleOutput();

        int pollenCount = 0;
        int petalCount = 0;

        for (int i = 0; i < activeParticles.Count; i++)
        {
            if (activeParticles[i].isPetal)
            {
                petalCount++;
            }
            else
            {
                pollenCount++;
            }
        }

        int chargeHaloCount = GetChargeHaloParticleCount();
        int releaseShockwaveCount = GetReleaseShockwaveParticleCount();
        int releaseAfterglowCount = GetReleaseAfterglowParticleCount();

        int totalPollenCount = pollenCount + chargeHaloCount + releaseShockwaveCount + releaseAfterglowCount;
        int totalPetalCount = petalCount;

        EnsureBufferSize(totalPollenCount);
        EnsurePetalBufferSize(totalPetalCount);

        int pollenIndex = 0;
        int petalIndex = 0;

        for (int i = 0; i < activeParticles.Count; i++)
        {
            MagicParticle magic = activeParticles[i];

            ParticleSystem.Particle particle = new ParticleSystem.Particle
            {
                position = magic.currentPosition,
                velocity = Vector3.zero,
                startLifetime = releaseDuration + 1f,
                remainingLifetime = releaseDuration + 1f,
                startColor = ResolveColor(magic),
                startSize = ResolveSize(magic),
                randomSeed = (uint)Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(magic.seed) * 100000f)),
                rotation3D = new Vector3(
                    0f,
                    0f,
                    Time.time * (magic.isPetal ? 140f : 40f) + magic.seed * 19f)
            };

            if (magic.isPetal)
            {
                petalBuffer[petalIndex] = particle;
                petalIndex++;
            }
            else
            {
                particleBuffer[pollenIndex] = particle;
                pollenIndex++;
            }
        }

        for (int i = 0; i < chargeHaloCount; i++)
        {
            particleBuffer[pollenIndex] = BuildChargeHaloParticle(i, chargeHaloCount);
            pollenIndex++;
        }

        for (int i = 0; i < releaseShockwaveCount; i++)
        {
            particleBuffer[pollenIndex] = BuildReleaseShockwaveParticle(i, releaseShockwaveCount);
            pollenIndex++;
        }

        for (int i = 0; i < releaseAfterglowCount; i++)
        {
            particleBuffer[pollenIndex] = BuildReleaseAfterglowParticle(i, releaseAfterglowCount);
            pollenIndex++;
        }

        if (particleOutput != null)
        {
            ParticleSystem.MainModule main = particleOutput.main;
            main.maxParticles = Mathf.Max(maxRenderedParticlesPerSystem, totalPollenCount);
            ConfigureLongTail(particleOutput, false);

            if (!particleOutput.isPlaying)
            {
                particleOutput.Play(true);
            }

            particleOutput.SetParticles(particleBuffer, totalPollenCount);
        }

        if (petalOutput != null)
        {
            ParticleSystem.MainModule main = petalOutput.main;
            main.maxParticles = Mathf.Max(maxRenderedParticlesPerSystem, totalPetalCount);
            ConfigureLongTail(petalOutput, true);

            if (!petalOutput.isPlaying)
            {
                petalOutput.Play(true);
            }

            petalOutput.SetParticles(petalBuffer, totalPetalCount);
        }
    }

    private int GetChargeHaloParticleCount()
    {
        if (!enableChargeHalo || !isCollecting)
        {
            return 0;
        }

        float charge = GetCharge01();
        if (charge <= 0.02f)
        {
            return 0;
        }

        int budgetedCount = ApplyEffectParticleBudget(chargeHaloParticleCount);
        return Mathf.Clamp(Mathf.CeilToInt(budgetedCount * Smoother01(charge)), 0, budgetedCount);
    }

    private ParticleSystem.Particle BuildChargeHaloParticle(int index, int count)
    {
        float charge = GetCharge01();
        float normalized = index / Mathf.Max(1f, count);
        float ring = index % 2 == 0 ? 0f : 1f;
        float angle = normalized * Mathf.PI * 2f + Time.time * Mathf.Lerp(0.7f, 2.8f, charge) * (ring == 0f ? 1f : -1.25f);
        float wobble = Mathf.Sin(Time.time * 3.1f + index * 0.73f) * 0.035f;

        Vector3 forward = playerHead != null ? playerHead.forward : (handAnchor != null ? handAnchor.forward : transform.forward);
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f)
        {
            forward = transform.forward;
        }

        forward.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        Vector3 up = Vector3.up;

        float radius = chargeHaloRadius * Mathf.Lerp(0.72f, 1.18f, Smoother01(charge));
        float verticalScale = chargeHaloVerticalScale * Mathf.Lerp(0.75f, 1.1f, charge);
        Vector3 center = GetHoldCenter();
        Vector3 ringOffset = right * (Mathf.Cos(angle) * radius)
            + up * (Mathf.Sin(angle) * radius * verticalScale)
            + forward * (Mathf.Sin(angle * 1.7f + ring * 2.1f) * (0.055f + wobble));

        Color color = Color.Lerp(pollenTrailColor, galaxyViolet, 0.35f + charge * 0.35f);
        color.a = chargeHaloAlpha * Mathf.Lerp(0.45f, 1f, charge) * (0.74f + Mathf.Sin(angle * 2f + Time.time * 4.2f) * 0.26f);

        return new ParticleSystem.Particle
        {
            position = center + ringOffset,
            velocity = Vector3.zero,
            startLifetime = 1f,
            remainingLifetime = 1f,
            startColor = color,
            startSize = chargeHaloParticleSize * Mathf.Lerp(0.65f, 1.15f, charge),
            randomSeed = (uint)Mathf.Max(1, index + 9137),
            rotation3D = Vector3.zero
        };
    }

    private int GetReleaseShockwaveParticleCount()
    {
        if (!enableReleaseShockwave || !releaseActive)
        {
            return 0;
        }

        float age = Time.time - releaseStartTime;
        if (age < 0f || age > releaseShockwaveDuration)
        {
            return 0;
        }

        return Mathf.Max(0, ApplyEffectParticleBudget(releaseShockwaveParticleCount));
    }

    private ParticleSystem.Particle BuildReleaseShockwaveParticle(int index, int count)
    {
        float age = Mathf.Max(0f, Time.time - releaseStartTime);
        float lifeT = Mathf.Clamp01(age / Mathf.Max(0.01f, releaseShockwaveDuration));
        float expandT = EaseOutCubic(lifeT);
        float fade = 1f - Smoother01(lifeT);

        float normalized = index / Mathf.Max(1f, count);
        float angle = normalized * Mathf.PI * 2f + Time.time * 0.55f;
        float radiusScale = GetChargedReleaseRadiusScale();
        float radius = Mathf.Lerp(releaseShockwaveStartRadius, releaseShockwaveEndRadius * radiusScale, expandT);
        float lift = Mathf.Sin(normalized * Mathf.PI * 2f * 3f + Time.time * 5.8f) * releaseShockwaveHeight * (1f - lifeT);

        Vector3 forward = playerHead != null ? playerHead.forward : (handAnchor != null ? handAnchor.forward : transform.forward);
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f)
        {
            forward = transform.forward;
        }

        forward.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        Vector3 up = Vector3.up;

        Vector3 position = releaseOriginCenter
            + right * (Mathf.Cos(angle) * radius)
            + forward * (Mathf.Sin(angle) * radius * 0.72f)
            + up * lift;

        Color color = Color.Lerp(pollenTrailColor, galaxyViolet, 0.35f + releaseCharge * 0.22f);
        color.a = releaseShockwaveAlpha * fade * Mathf.Lerp(0.72f, 1.15f, releaseCharge);

        return new ParticleSystem.Particle
        {
            position = position,
            velocity = Vector3.zero,
            startLifetime = 1f,
            remainingLifetime = 1f,
            startColor = color,
            startSize = releaseShockwaveParticleSize * Mathf.Lerp(0.9f, 1.25f, releaseCharge) * Mathf.Lerp(1.05f, 0.28f, lifeT),
            randomSeed = (uint)Mathf.Max(1, index + 27119),
            rotation3D = Vector3.zero
        };
    }

    private int GetReleaseAfterglowParticleCount()
    {
        if (!enableReleaseAfterglow || !releaseActive)
        {
            return 0;
        }

        float age = Time.time - releaseStartTime;
        float releaseT = Mathf.Clamp01(age / Mathf.Max(0.01f, releaseDuration));
        if (releaseT < releaseAfterglowStart || age > releaseDuration + releaseSettleSeconds)
        {
            return 0;
        }

        float reveal = Smoother01(Mathf.Clamp01((releaseT - releaseAfterglowStart) / Mathf.Max(0.001f, 1f - releaseAfterglowStart)));
        if (age > releaseDuration)
        {
            float settleT = Mathf.Clamp01((age - releaseDuration) / Mathf.Max(0.01f, releaseSettleSeconds));
            reveal *= 1f - Smoother01(settleT);
        }

        int budgetedCount = ApplyEffectParticleBudget(releaseAfterglowParticleCount);
        return Mathf.Clamp(Mathf.CeilToInt(budgetedCount * reveal), 0, budgetedCount);
    }

    private int ApplyEffectParticleBudget(int requestedCount)
    {
        return Mathf.Max(0, Mathf.RoundToInt(requestedCount * Mathf.Clamp(effectParticleBudgetScale, 0.35f, 1f)));
    }

    private ParticleSystem.Particle BuildReleaseAfterglowParticle(int index, int count)
    {
        float age = Time.time - releaseStartTime;
        float releaseT = Mathf.Clamp01(age / Mathf.Max(0.01f, releaseDuration));
        float afterT = Smoother01(Mathf.Clamp01((releaseT - releaseAfterglowStart) / Mathf.Max(0.001f, 1f - releaseAfterglowStart)));
        float settleFade = 1f;
        if (age > releaseDuration)
        {
            float settleT = Mathf.Clamp01((age - releaseDuration) / Mathf.Max(0.01f, releaseSettleSeconds));
            settleFade = 1f - Smoother01(settleT);
        }
        float normalized = index / Mathf.Max(1f, count);
        float seed = index * 12.9898f + 78.233f;
        float ring = Mathf.Repeat(normalized * 3.7f + Deterministic01(seed), 1f);
        float angle = normalized * Mathf.PI * 2f * 2.3f + Time.time * Mathf.Lerp(0.12f, 0.38f, releaseCharge) + seed * 0.013f;
        float radius = releaseAfterglowRadius * GetChargedReleaseRadiusScale() * Mathf.Lerp(0.45f, 1f, ring);
        float height = Mathf.Lerp(-0.18f, releaseAfterglowHeight * GetChargedReleaseHeightScale(), Deterministic01(seed * 0.37f));

        Vector3 center = GetPlayerCenter();
        Vector3 position = center
            + new Vector3(Mathf.Cos(angle) * radius, height, Mathf.Sin(angle) * radius)
            + ResolveSoftJitter(seed, Time.time, Mathf.Lerp(0.035f, 0.11f, afterT));

        float sparkle = 0.72f + Mathf.Sin(Time.time * 5.7f + seed) * 0.28f;
        float fade = Mathf.Lerp(0.25f, 1f, afterT) * settleFade;
        Color color = Color.Lerp(pollenTrailColor, galaxyViolet, 0.48f + releaseCharge * 0.22f);
        color.a = releaseAfterglowAlpha * fade * sparkle;

        return new ParticleSystem.Particle
        {
            position = position,
            velocity = Vector3.zero,
            startLifetime = 1f,
            remainingLifetime = 1f,
            startColor = color,
            startSize = releaseAfterglowParticleSize * Mathf.Lerp(0.7f, 1.25f, sparkle),
            randomSeed = (uint)Mathf.Max(1, index + 49157),
            rotation3D = Vector3.zero
        };
    }

    private void ConfigureLongTail(ParticleSystem output, bool isPetal)
    {
        ParticleSystem.TrailModule trails = output.trails;
        trails.enabled = enableMotionTrails;
        if (!enableMotionTrails)
        {
            return;
        }

        trails.mode = ParticleSystemTrailMode.PerParticle;
        trails.ratio = 1f;
        bool useReleaseTail = releaseActive;
        float lifetime = useReleaseTail ? releaseLongTailLifetime : gatherTailLifetime;
        float minVertexDistance = useReleaseTail ? releaseLongTailMinVertexDistance : gatherTailMinVertexDistance;
        float pollenWidth = useReleaseTail ? releasePollenLongTailWidth : gatherPollenTailWidth;
        float petalWidth = useReleaseTail ? releasePetalLongTailWidth : gatherPetalTailWidth;
        float alpha = useReleaseTail ? releaseLongTailAlpha : gatherTailAlpha;

        trails.lifetime = new ParticleSystem.MinMaxCurve(lifetime);
        trails.minVertexDistance = minVertexDistance;
        trails.worldSpace = true;
        trails.dieWithParticles = false;
        trails.sizeAffectsWidth = false;
        trails.sizeAffectsLifetime = false;
        trails.inheritParticleColor = true;

        float width = isPetal ? petalWidth : pollenWidth;
        AnimationCurve widthCurve = new AnimationCurve(
            new Keyframe(0f, width),
            new Keyframe(0.22f, width * (isPetal ? 0.68f : 0.72f)),
            new Keyframe(1f, 0f));
        trails.widthOverTrail = new ParticleSystem.MinMaxCurve(1f, widthCurve);

        Gradient gradient = new Gradient();
        Color tint = isPetal ? petalTrailColor : pollenTrailColor;
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(tint, 0f),
                new GradientColorKey(tint, 0.55f),
                new GradientColorKey(tint, 1f)
            },
            new[]
            {
                new GradientAlphaKey(alpha * (isPetal ? 0.82f : 1f), 0f),
                new GradientAlphaKey(alpha * (isPetal ? 0.34f : 0.43f), 0.55f),
                new GradientAlphaKey(0f, 1f)
            });
        trails.colorOverTrail = new ParticleSystem.MinMaxGradient(gradient);

        ParticleSystemRenderer renderer = output.GetComponent<ParticleSystemRenderer>();
        if (renderer != null && renderer.sharedMaterial != null)
        {
            renderer.trailMaterial = renderer.sharedMaterial;
        }
    }

    private Color ResolveColor(MagicParticle magic)
    {
        Color color = magic.color;
        if (activeReleaseMode == PetalPollenReleaseMode.GalaxyVeil && magic.stage == ParticleStage.Releasing)
        {
            color = Color.Lerp(color, galaxyViolet, magic.isPetal ? 0.25f : 0.42f);
        }

        if (activeReleaseMode == PetalPollenReleaseMode.MathRibbon && magic.stage == ParticleStage.Releasing && magic.releaseAge > releaseFlashSeconds)
        {
            float showT = Mathf.Clamp01((magic.releaseAge - releaseFlashSeconds) / Mathf.Max(0.01f, releaseDuration - releaseFlashSeconds));
            float revealHead = Smoother01(Mathf.Clamp01(showT / 0.58f));
            float revealAlpha = Smoother01(Mathf.Clamp01((revealHead - magic.releaseOrder) / 0.12f));
            color.a *= Mathf.Lerp(0.32f, 1f, revealAlpha);
        }

        if (magic.stage == ParticleStage.Holding && isCollecting)
        {
            float charge = GetCharge01();
            float pulse = 0.88f + Mathf.Sin(Time.time * 5.2f + magic.seed) * 0.12f;
            color = Color.Lerp(color, magic.isPetal ? galaxyViolet : secondaryPollenColor, charge * 0.28f);
            color.r *= Mathf.Lerp(1f, 1.25f, charge) * pulse;
            color.g *= Mathf.Lerp(1f, 1.25f, charge) * pulse;
            color.b *= Mathf.Lerp(1f, 1.25f, charge) * pulse;
        }

        if (magic.stage == ParticleStage.Releasing)
        {
            float chargedBrightness = 1f + releaseCharge * Mathf.Max(0f, chargedReleaseBrightnessBoost);
            color.r *= chargedBrightness;
            color.g *= chargedBrightness;
            color.b *= chargedBrightness;

            float t = Mathf.Clamp01(magic.releaseAge / Mathf.Max(0.01f, releaseDuration));
            float fadeStart = 0.72f;
            if (activeReleaseMode == PetalPollenReleaseMode.TornadoVortex)
            {
                fadeStart = magic.isPetal ? 0.78f : 0.88f;
            }

            color.a *= 1f - Smoother01(Mathf.Clamp01((t - fadeStart) / Mathf.Max(0.001f, 1f - fadeStart)));
        }

        float twinkle = magic.isPetal ? 1f : 0.85f + Mathf.Sin(Time.time * 7.5f + magic.seed) * 0.28f;
        color.r *= twinkle;
        color.g *= twinkle;
        color.b *= twinkle;
        return color;
    }

    private float ResolveSize(MagicParticle magic)
    {
        float size = magic.size;
        if (magic.stage == ParticleStage.Releasing)
        {
            size *= 1f + releaseCharge * Mathf.Max(0f, chargedReleaseSizeBoost);
        }

        if (magic.stage == ParticleStage.Releasing && magic.releaseAge < releaseFlashSeconds * 1.5f)
        {
            float flash = 1f - Mathf.Clamp01(magic.releaseAge / Mathf.Max(0.01f, releaseFlashSeconds * 1.5f));
            float maxPop = magic.isPetal ? 1.03f : 1.16f;
            size *= Mathf.Lerp(1f, maxPop, flash);
        }

        if (activeReleaseMode == PetalPollenReleaseMode.MathRibbon && magic.stage == ParticleStage.Releasing)
        {
            float showT = Mathf.Clamp01((magic.releaseAge - releaseFlashSeconds) / Mathf.Max(0.01f, releaseDuration - releaseFlashSeconds));
            float visibleBoost = Mathf.Lerp(1.25f, 1f, Smoother01(Mathf.Clamp01((showT - 0.55f) / 0.45f)));
            size *= Mathf.Clamp(mathRibbonParticleSizeMultiplier, 1f, 1.35f) * visibleBoost;
        }

        if (activeReleaseMode == PetalPollenReleaseMode.TornadoVortex && magic.stage == ParticleStage.Releasing)
        {
            float showT = Mathf.Clamp01((magic.releaseAge - releaseFlashSeconds) / Mathf.Max(0.01f, releaseDuration - releaseFlashSeconds));
            float preludePortion = Mathf.Clamp(tornadoWispPreludePortion, 0.16f, 0.42f);
            float burstSizeT = Smoother01(Mathf.Clamp01((showT - preludePortion) / 0.24f));
            float buildBoost = Mathf.Lerp(1.35f, 1f, Smoother01(Mathf.Clamp01((showT - 0.5f) / 0.5f)));
            float tornadoSize = IsTornadoDustParticle(magic)
                ? Mathf.Clamp(tornadoDustSizeMultiplier, 0.18f, 0.75f)
                : magic.isPetal
                    ? Mathf.Lerp(0.38f, Mathf.Clamp(tornadoParticleSizeMultiplier, 1f, 1.55f), burstSizeT)
                    : Mathf.Lerp(0.72f, Mathf.Clamp(tornadoParticleSizeMultiplier, 1f, 1.55f), burstSizeT);
            size *= tornadoSize * buildBoost;
        }

        return size;
    }

    private float GetChargedReleaseRadiusScale()
    {
        return 1f + releaseCharge * Mathf.Max(0f, chargedReleaseRadiusBoost);
    }

    private float GetChargedReleaseHeightScale()
    {
        return 1f + releaseCharge * Mathf.Max(0f, chargedReleaseHeightBoost);
    }

    private bool IsTornadoDustParticle(MagicParticle magic)
    {
        if (magic.isPetal)
        {
            return false;
        }

        return Deterministic01(magic.seed * 0.173f + 11.7f) < Mathf.Clamp01(tornadoDustFraction);
    }

    private float GetCharge01()
    {
        if (!isCollecting)
        {
            return 0f;
        }

        return Mathf.Clamp01((Time.time - collectStartTime) / Mathf.Max(0.01f, chargedHoldSeconds));
    }

    private Vector3 GetHoldCenter()
    {
        Transform anchor = handAnchor != null ? handAnchor : transform;
        return anchor.position + anchor.forward * holdDistance;
    }

    private Vector3 GetPlayerCenter()
    {
        if (playerHead != null)
        {
            return playerHead.position + Vector3.down * 0.35f;
        }

        return GetHoldCenter();
    }

    private void EnsureParticleOutput()
    {
        if (particleOutput == null)
        {
            particleOutput = ResolveParticleOutput("PollenRenderer");
        }

        if (particleOutput == null)
        {
            particleOutput = CreateParticleOutput("PollenRenderer");
        }

        if (petalOutput == null)
        {
            petalOutput = ResolveParticleOutput("PetalRenderer");
        }

        if (petalOutput == null)
        {
            petalOutput = CreateParticleOutput("PetalRenderer");
        }

        ConfigureParticleOutput(particleOutput);
        ConfigureParticleOutput(petalOutput);
    }

    private ParticleSystem ResolveParticleOutput(string childName)
    {
        Transform child = transform.Find("Renderers/" + childName);
        return child != null ? child.GetComponent<ParticleSystem>() : null;
    }

    private ParticleSystem CreateParticleOutput(string childName)
    {
        Transform renderers = transform.Find("Renderers");
        if (renderers == null)
        {
            GameObject renderersObject = new GameObject("Renderers");
            renderersObject.transform.SetParent(transform, false);
            renderers = renderersObject.transform;
        }

        GameObject child = new GameObject(childName);
        child.transform.SetParent(renderers, false);
        return child.AddComponent<ParticleSystem>();
    }

    private void ConfigureParticleOutput(ParticleSystem output)
    {
        if (output == null)
        {
            return;
        }

        ParticleSystem.MainModule main = output.main;
        main.playOnAwake = false;
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = maxParticles;
        main.startSpeed = 0f;
        main.startLifetime = releaseDuration + 1f;

        ParticleSystem.EmissionModule emission = output.emission;
        emission.enabled = false;

        ParticleSystem.ShapeModule shape = output.shape;
        shape.enabled = false;

        ParticleSystemRenderer renderer = output.GetComponent<ParticleSystemRenderer>();
        if (renderer == null)
        {
            renderer = output.gameObject.AddComponent<ParticleSystemRenderer>();
        }

        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.alignment = ParticleSystemRenderSpace.View;
    }

    private void EnsureBufferSize(int count)
    {
        if (particleBuffer.Length < count)
        {
            particleBuffer = new ParticleSystem.Particle[Mathf.Max(1, count)];
        }
    }

    private void EnsurePetalBufferSize(int count)
    {
        if (petalBuffer.Length < count)
        {
            petalBuffer = new ParticleSystem.Particle[Mathf.Max(1, count)];
        }
    }

    private static Vector3 FibonacciSphere(int index, int count)
    {
        float i = index + 0.5f;
        float phi = Mathf.Acos(1f - 2f * i / count);
        float theta = Mathf.PI * (3f - Mathf.Sqrt(5f)) * index;
        return new Vector3(
            Mathf.Sin(phi) * Mathf.Cos(theta),
            Mathf.Cos(phi),
            Mathf.Sin(phi) * Mathf.Sin(theta));
    }

    private static Vector3 ResolveSoftJitter(float seed, float time, float amount)
    {
        return new Vector3(
            Mathf.PerlinNoise(seed, time * 0.9f) - 0.5f,
            Mathf.PerlinNoise(seed + 17.1f, time * 0.8f) - 0.5f,
            Mathf.PerlinNoise(seed + 31.7f, time * 0.7f) - 0.5f) * amount;
    }

    private static Vector3 QuadraticBezier(Vector3 a, Vector3 b, Vector3 c, float t)
    {
        Vector3 ab = Vector3.Lerp(a, b, t);
        Vector3 bc = Vector3.Lerp(b, c, t);
        return Vector3.Lerp(ab, bc, t);
    }

    private Vector3 ResolveBreatherSurfacePoint(float u, float v)
    {
        float b = Mathf.Clamp(mathRibbonB, 0.08f, 0.92f);
        float r = 1f - b * b;
        float w = Mathf.Sqrt(Mathf.Max(0.0001f, r));
        float bu = b * u;
        float cosh = (float)System.Math.Cosh(bu);
        float sinh = (float)System.Math.Sinh(bu);
        float sinWv = Mathf.Sin(w * v);
        float cosWv = Mathf.Cos(w * v);
        float denominator = b * (w * w * cosh * cosh + b * b * sinWv * sinWv);
        denominator = Mathf.Sign(denominator) * Mathf.Max(0.0001f, Mathf.Abs(denominator));

        float formulaY = -u + (2f * r * cosh * sinh) / denominator;
        float formulaZ = (2f * w * cosh * (-w * Mathf.Cos(v) * cosWv - Mathf.Sin(v) * sinWv)) / denominator;
        float formulaX = (2f * w * cosh * (-w * Mathf.Sin(v) * cosWv + Mathf.Cos(v) * sinWv)) / denominator;

        Vector3 raw = new Vector3(formulaY, formulaZ, formulaX);
        if (!IsFinite(raw))
        {
            raw = new Vector3(u * 0.35f, Mathf.Sin(v), Mathf.Cos(v));
        }

        float atanNormalize = 2f / Mathf.PI;
        Vector3 bounded = new Vector3(
            Mathf.Atan(raw.x) * atanNormalize,
            Mathf.Atan(raw.y) * atanNormalize,
            Mathf.Atan(raw.z) * atanNormalize);

        float safeImpactScale = Mathf.Clamp(mathRibbonImpactScale, 0.85f, 1.55f);
        float safeShapeScale = Mathf.Clamp(mathRibbonScale, 0.24f, 0.48f) / 0.38f;
        float safeVerticalScale = Mathf.Clamp(mathRibbonVerticalScale, 1f, 2.15f);
        float safeCanopyLift = Mathf.Clamp(mathRibbonCanopyLift, 0f, 0.9f);
        float u01 = Mathf.InverseLerp(-mathRibbonURange * 0.5f, mathRibbonURange * 0.5f, u);
        float canopy = Mathf.Sin(u01 * Mathf.PI) * safeCanopyLift;
        Vector3 readableSpine = new Vector3(
            (u01 - 0.5f) * 1.58f,
            Mathf.Sin(u01 * Mathf.PI * 2.2f) * 0.3f + canopy,
            Mathf.Cos(u01 * Mathf.PI * 1.6f) * 0.22f);

        Vector3 shaped = new Vector3(
            bounded.x * 0.94f,
            bounded.y * 0.7f,
            bounded.z * 0.52f);

        Vector3 point = (readableSpine + shaped) * safeImpactScale * safeShapeScale;
        point.y *= safeVerticalScale;
        return Vector3.ClampMagnitude(point, 2.05f * safeImpactScale);
    }

    private static bool IsFinite(Vector3 value)
    {
        return !float.IsNaN(value.x) && !float.IsInfinity(value.x)
            && !float.IsNaN(value.y) && !float.IsInfinity(value.y)
            && !float.IsNaN(value.z) && !float.IsInfinity(value.z);
    }

    private static float Deterministic01(float seed)
    {
        return Mathf.Repeat(Mathf.Sin(seed * 12.9898f) * 43758.5453f, 1f);
    }

    private Vector3 ResolveBreatherSurfaceTangent(float u, float v)
    {
        const float delta = 0.035f;
        Vector3 before = ResolveBreatherSurfacePoint(u - delta, v);
        Vector3 after = ResolveBreatherSurfacePoint(u + delta, v);
        Vector3 tangent = after - before;
        return tangent.sqrMagnitude > 0.001f ? tangent.normalized : Vector3.forward;
    }

    private Quaternion ResolveMathRibbonPose(Vector3 center, float t)
    {
        Quaternion basePose = hasReleaseShowcasePose ? releaseShowcasePose : ResolveFallbackMathRibbonPose(center);
        return basePose * Quaternion.Euler(
            -8f + Mathf.Sin(Time.time * 0.28f) * 2f,
            Time.time * 6f + t * 18f,
            Mathf.Cos(Time.time * 0.31f) * 3f);
    }

    private Vector3 ResolveMathRibbonCenter()
    {
        if (hasReleaseShowcasePose)
        {
            return releaseShowcaseCenter;
        }

        float safeDistance = Mathf.Clamp(mathRibbonViewDistance, 1.05f, 1.8f);
        float safeHeight = Mathf.Clamp(mathRibbonViewHeight, -0.15f, 0.85f);
        float safeSideOffset = Mathf.Clamp(mathRibbonSideOffset, -0.45f, 0.45f);
        Vector3 viewPosition;
        Vector3 viewForward;
        Vector3 viewRight;
        if (TryResolveViewFrame(out viewPosition, out viewForward, out viewRight))
        {
            return viewPosition
                + viewForward * safeDistance
                + viewRight * safeSideOffset
                + Vector3.up * safeHeight;
        }

        Transform anchor = handAnchor != null ? handAnchor : transform;
        return anchor.position
            + anchor.forward * safeDistance
            + anchor.right * safeSideOffset
            + Vector3.up * safeHeight;
    }

    private Vector3 ResolveMathRibbonGateCenter()
    {
        if (hasReleaseShowcasePose)
        {
            return releaseMathRibbonGateCenter;
        }

        float safeGateDistance = Mathf.Clamp(mathRibbonGateDistance, 0.45f, 1.15f);
        float safeHeight = Mathf.Clamp(mathRibbonViewHeight * 0.55f, 0.02f, 0.55f);
        float safeSideOffset = Mathf.Clamp(mathRibbonSideOffset, -0.45f, 0.45f);
        Vector3 viewPosition;
        Vector3 viewForward;
        Vector3 viewRight;
        if (TryResolveViewFrame(out viewPosition, out viewForward, out viewRight))
        {
            return viewPosition
                + viewForward * safeGateDistance
                + viewRight * safeSideOffset
                + Vector3.up * safeHeight;
        }

        Transform anchor = handAnchor != null ? handAnchor : transform;
        return anchor.position
            + anchor.forward * safeGateDistance
            + anchor.right * safeSideOffset
            + Vector3.up * safeHeight;
    }

    private Vector3 ResolveTornadoCenter()
    {
        if (hasReleaseShowcasePose)
        {
            return releaseTornadoCenter;
        }

        float safeDistance = Mathf.Clamp(tornadoViewDistance, 0.9f, 2f);
        float safeHeight = Mathf.Clamp(tornadoViewHeight, -0.15f, 0.85f);
        Vector3 viewPosition;
        Vector3 viewForward;
        Vector3 viewRight;
        if (TryResolveViewFrame(out viewPosition, out viewForward, out viewRight))
        {
            return viewPosition + viewForward * safeDistance + Vector3.up * safeHeight;
        }

        Transform anchor = handAnchor != null ? handAnchor : transform;
        return anchor.position + anchor.forward * safeDistance + Vector3.up * safeHeight;
    }

    private Vector3 ResolveTornadoGateCenter()
    {
        if (hasReleaseShowcasePose)
        {
            return releaseTornadoGateCenter;
        }

        float safeGateDistance = Mathf.Clamp(tornadoGateDistance, 0.45f, 1.25f);
        float safeHeight = Mathf.Clamp(tornadoViewHeight * 0.45f, 0.02f, 0.45f);
        Vector3 viewPosition;
        Vector3 viewForward;
        Vector3 viewRight;
        if (TryResolveViewFrame(out viewPosition, out viewForward, out viewRight))
        {
            return viewPosition + viewForward * safeGateDistance + Vector3.up * safeHeight;
        }

        Transform anchor = handAnchor != null ? handAnchor : transform;
        return anchor.position + anchor.forward * safeGateDistance + Vector3.up * safeHeight;
    }

    private Quaternion ResolveTornadoPose(Vector3 center)
    {
        return hasReleaseShowcasePose ? releaseTornadoPose : ResolveFallbackMathRibbonPose(center);
    }

    private void CaptureReleaseShowcasePose()
    {
        releaseShowcaseCenter = ResolveMathRibbonCenter();
        releaseMathRibbonGateCenter = ResolveMathRibbonGateCenter();
        releaseShowcasePose = ResolveFallbackMathRibbonPose(releaseShowcaseCenter);
        releaseTornadoCenter = ResolveTornadoCenter();
        releaseTornadoGateCenter = ResolveTornadoGateCenter();
        releaseTornadoPose = ResolveFallbackMathRibbonPose(releaseTornadoCenter);
        hasReleaseShowcasePose = true;
    }

    private Quaternion ResolveFallbackMathRibbonPose(Vector3 center)
    {
        Vector3 viewPosition;
        Vector3 viewForward;
        Vector3 viewRight;
        if (TryResolveViewFrame(out viewPosition, out viewForward, out viewRight))
        {
            Vector3 fromView = center - viewPosition;
            if (fromView.sqrMagnitude < 0.001f)
            {
                fromView = viewForward;
            }

            fromView.Normalize();
            return Quaternion.LookRotation(fromView, Vector3.up);
        }

        Transform anchor = handAnchor != null ? handAnchor : transform;
        return Quaternion.LookRotation(anchor.forward, Vector3.up);
    }

    private bool TryResolveViewFrame(out Vector3 position, out Vector3 forward, out Vector3 right)
    {
        Transform view = playerHead;
        if (view == null && Camera.main != null)
        {
            view = Camera.main.transform;
        }

        if (view == null)
        {
            position = Vector3.zero;
            forward = Vector3.forward;
            right = Vector3.right;
            return false;
        }

        position = view.position;
        forward = view.forward;
        right = view.right;

        forward.y = 0f;
        right.y = 0f;
        if (forward.sqrMagnitude < 0.001f)
        {
            forward = Vector3.ProjectOnPlane(view.forward, Vector3.up);
        }

        if (right.sqrMagnitude < 0.001f)
        {
            right = Vector3.Cross(Vector3.up, forward);
        }

        forward = forward.sqrMagnitude > 0.001f ? forward.normalized : transform.forward;
        right = right.sqrMagnitude > 0.001f ? right.normalized : transform.right;
        return true;
    }

    private static float Smoother01(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * t * (t * (t * 6f - 15f) + 10f);
    }

    private float ReleaseBloom01(float t)
    {
        return EaseOutCubic(Mathf.Clamp01(t * releaseBloomSpeed));
    }

    private static float EaseOutCubic(float t)
    {
        t = Mathf.Clamp01(t);
        float inverse = 1f - t;
        return 1f - inverse * inverse * inverse;
    }

    private static float EaseInCubic(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * t;
    }
}
