using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class PetalPollenMagicController : MonoBehaviour
{
    private static readonly int[] MagicBloomLayerPetalCounts = { 6, 9, 12 };

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

    [Header("Release")]
    [SerializeField] private bool randomizeReleaseMode = true;
    [SerializeField] private PetalPollenReleaseMode fixedReleaseMode = PetalPollenReleaseMode.GalaxyVeil;
    [SerializeField] private float releaseDuration = 6.5f;
    [SerializeField] private float chargedHoldSeconds = 3f;
    [SerializeField] private float releaseFlashSeconds = 0.38f;
    [SerializeField] private float releaseSeedRadius = 0.07f;
    [SerializeField] private float releaseBloomSpeed = 3.2f;
    [SerializeField] private float burstRadius = 1.25f;
    [SerializeField] private float galaxyRadius = 1.35f;
    [SerializeField] private float galaxyHeight = 0.7f;
    [SerializeField] private float petalRainHeight = 1.65f;
    [SerializeField] private float roseRotationXSpeed = 18f;
    [SerializeField] private float roseRotationZSpeed = 12f;
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
    [SerializeField] private float petalRainWeight = 0.3f;
    [SerializeField] private float spiralBloomWeight = 0.25f;
    [SerializeField] private float flowerConstellationWeight = 0.2f;
    [SerializeField] private float mathRibbonWeight = 0.25f;
    [SerializeField] private float tornadoVortexWeight = 0.25f;
    [SerializeField] private float galaxyVeilWeight = 0.25f;
    [SerializeField] private float chargedGalaxyBonusWeight = 0.35f;

    [Header("Look")]
    [SerializeField] private float pollenSize = 0.045f;
    [SerializeField] private float petalSize = 0.12f;
    [SerializeField] private Color secondaryPollenColor = new Color(0.62f, 0.95f, 1f, 1f);
    [SerializeField] private Color galaxyViolet = new Color(0.72f, 0.48f, 1f, 1f);

    private readonly List<MagicParticle> activeParticles = new List<MagicParticle>();
    private ParticleSystem.Particle[] particleBuffer = new ParticleSystem.Particle[0];
    private ParticleSystem.Particle[] petalBuffer = new ParticleSystem.Particle[0];

    private float spawnAccumulator;
    private float collectStartTime;
    private float releaseStartTime;
    private bool isCollecting;
    private bool releaseActive;
    private PetalPollenReleaseMode activeReleaseMode;
    private Vector3 releaseShowcaseCenter;
    private Vector3 releaseMathRibbonGateCenter;
    private Vector3 releaseTornadoCenter;
    private Vector3 releaseTornadoGateCenter;
    private Quaternion releaseTornadoPose = Quaternion.identity;
    private Quaternion releaseShowcasePose = Quaternion.identity;
    private bool hasReleaseShowcasePose;

    private void Awake()
    {
        EnsureParticleOutput();
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

        if (isCollecting)
        {
            SpawnCollectionParticles();
        }

        UpdateMagicParticles();
        RenderParticles();
    }

    public void BeginCollect()
    {
        if (handAnchor == null)
        {
            Debug.LogWarning("[PetalPollenMagic] Assign a hand anchor before collecting.", this);
            return;
        }

        if (isCollecting)
        {
            return;
        }

        isCollecting = true;
        releaseActive = false;
        hasReleaseShowcasePose = false;
        collectStartTime = Time.time;
        spawnAccumulator = 0f;
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
        activeReleaseMode = randomizeReleaseMode ? PickReleaseMode(Time.time - collectStartTime) : fixedReleaseMode;
        CaptureReleaseShowcasePose();

        Vector3 center = GetHoldCenter();
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
        if (particleOutput != null)
        {
            particleOutput.Clear(true);
        }

        if (petalOutput != null)
        {
            petalOutput.Clear(true);
        }
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
            ? source.PetalColor
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

        if (releaseActive && activeParticles.Count == 0)
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
        float petalOuter = particle.isPetal ? 1.22f : 1f;
        Vector3 target = GetHoldCenter() + orbit * (spherePoint * holdRadius * pulse * petalOuter);
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
        switch (activeReleaseMode)
        {
            case PetalPollenReleaseMode.PetalRain:
                particle.currentPosition = ResolvePetalRainPosition(particle, showT);
                break;
            case PetalPollenReleaseMode.SpiralBloom:
                particle.currentPosition = ResolveSpiralBloomPosition(particle, index, count, showT);
                break;
            case PetalPollenReleaseMode.FlowerConstellation:
                particle.currentPosition = ResolveFlowerConstellationPosition(particle, index, count, showT);
                break;
            case PetalPollenReleaseMode.MathRibbon:
                particle.currentPosition = ResolveMathRibbonPosition(particle, index, count, showT);
                break;
            case PetalPollenReleaseMode.TornadoVortex:
                particle.currentPosition = ResolveTornadoVortexPosition(particle, index, count, showT);
                break;
            default:
                particle.currentPosition = ResolveGalaxyVeilPosition(particle, index, count, showT);
                break;
        }
    }

    private Vector3 ResolveGalaxyVeilPosition(MagicParticle particle, int index, int count, float t)
    {
        float bloomT = ReleaseBloom01(t);
        Vector3 center = GetPlayerCenter();
        float strand = index % 2 == 0 ? 1f : -1f;
        float angle = particle.seed + (bloomT * Mathf.PI * 1.35f + t * Mathf.PI * 2.15f) * strand + index * 0.037f;
        float radius = Mathf.Lerp(0.18f, galaxyRadius * (particle.isPetal ? 1.05f : 1f), bloomT);
        float arm = Mathf.Sin(angle * 2f + particle.seed) * 0.28f;
        float height = Mathf.Sin(angle * 1.7f + particle.seed) * galaxyHeight * (0.35f + bloomT * 0.65f);

        Vector3 local = new Vector3(
            Mathf.Cos(angle) * (radius + arm),
            height,
            Mathf.Sin(angle) * (radius + arm));

        Quaternion tilt = Quaternion.Euler(18f, t * 115f, -12f);
        Vector3 drift = Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.24f;
        return Vector3.Lerp(particle.releaseSeedPosition, center + tilt * local + drift, bloomT);
    }

    private Vector3 ResolvePetalRainPosition(MagicParticle particle, float t)
    {
        float bloomT = ReleaseBloom01(t);
        Vector3 center = GetPlayerCenter();
        float angle = particle.seed + t * Mathf.PI * 1.35f;
        float radiusPulse = 0.18f + Mathf.Sin(t * Mathf.PI) * 0.18f;
        float radius = Mathf.Lerp(0.22f, burstRadius + radiusPulse, bloomT);
        float lift = Mathf.Sin(Mathf.Clamp01(t / 0.34f) * Mathf.PI) * 0.55f;

        Vector3 blossomRing = center + new Vector3(
            Mathf.Cos(angle) * radius,
            petalRainHeight + lift,
            Mathf.Sin(angle) * radius);

        float driftAngle = angle * 0.72f + Mathf.Sin(t * Mathf.PI * 2f + particle.seed) * 0.5f;
        Vector3 windDrift = new Vector3(Mathf.Cos(driftAngle), 0f, Mathf.Sin(driftAngle)) * (0.12f + t * 0.34f);
        float fallT = Smoother01(Mathf.Clamp01((t - 0.18f) / 0.82f));
        float slowFall = fallT * fallT * (petalRainHeight + (particle.isPetal ? 0.55f : 0.95f));
        Vector3 shimmer = ResolveSoftJitter(particle.seed, Time.time, particle.isPetal ? 0.07f : 0.12f);

        Vector3 target = blossomRing + windDrift + shimmer - Vector3.up * slowFall;

        if (!particle.isPetal)
        {
            float fireflyAngle = angle * 1.6f + Time.time * 0.75f;
            target += new Vector3(Mathf.Cos(fireflyAngle), Mathf.Sin(fireflyAngle * 1.3f), Mathf.Sin(fireflyAngle)) * 0.12f;
        }

        return Vector3.Lerp(particle.releaseSeedPosition, target, bloomT);
    }

    private Vector3 ResolveSpiralBloomPosition(MagicParticle particle, int index, int count, float t)
    {
        float bloomT = ReleaseBloom01(t);
        Vector3 center = GetHoldCenter();
        float progress = index / Mathf.Max(1f, count - 1f);
        float strand = index % 2 == 0 ? 1f : -1f;
        float angle = progress * Mathf.PI * 10f + (bloomT * Mathf.PI * 2f + t * Mathf.PI * 3f) * strand + particle.seed;
        float radius = Mathf.Lerp(0.08f, burstRadius, bloomT);
        float height = Mathf.Lerp(-0.7f, 1.4f, progress) + Mathf.Sin(angle * 1.6f) * 0.18f;
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

        Vector3 breatherPoint = ResolveBreatherSurfacePoint(u, v);
        float safeDepthScale = Mathf.Clamp(mathRibbonDepthScale, 0.65f, 1.15f);
        float safeForwardPush = Mathf.Clamp(mathRibbonForwardPush, 0.08f, 0.75f);
        breatherPoint.z = Mathf.Abs(breatherPoint.z) * safeDepthScale + safeForwardPush;
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
        float strand = index % 2 == 0 ? 1f : -1f;
        float safeHeight = Mathf.Clamp(tornadoHeight, 1.2f, 3.4f);
        float baseRadius = Mathf.Clamp(tornadoBaseRadius, 0.08f, 0.55f);
        float topRadius = Mathf.Clamp(tornadoTopRadius, baseRadius + 0.1f, 1.45f);
        float spinSpeed = Mathf.Clamp(tornadoSpinSpeed, 2f, 13f);
        float tremble = Mathf.Clamp(tornadoTremble, 0.02f, 0.36f);
        float dissolveRadius = Mathf.Clamp(tornadoDissolveRadius, 0.45f, 2f);

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

    private Vector3 ResolveFlowerConstellationPosition(MagicParticle particle, int index, int count, float t)
    {
        float gatherT = Smoother01(Mathf.Clamp01(t / 0.16f));
        float openT = Smoother01(Mathf.Clamp01((t - 0.12f) / 0.38f));
        float holdT = Smoother01(Mathf.Clamp01((t - 0.48f) / 0.2f));
        float scatterT = Smoother01(Mathf.Clamp01((t - 0.74f) / 0.26f));

        Vector3 center = Vector3.Lerp(GetHoldCenter(), GetPlayerCenter() + Vector3.up * 0.38f, 0.5f);
        Vector3 budLocal = ResolveMagicBloomBudPoint(index, count, particle.seed);
        Vector3 flowerLocal = particle.isPetal
            ? ResolveMagicBloomPetalSurface(index, count, particle.seed)
            : ResolveMagicBloomPollenCore(index, count, particle.seed);

        float unfurlDelay = ResolveMagicBloomUnfurlDelay(index, count);
        float delayedOpenT = Smoother01(Mathf.Clamp01((openT - unfurlDelay) / Mathf.Max(0.001f, 1f - unfurlDelay)));
        Vector3 local = Vector3.Lerp(budLocal, flowerLocal, delayedOpenT);

        float lifePulse = Mathf.Sin(t * Mathf.PI * 2f + particle.seed) * 0.018f * (1f - scatterT);
        local += ResolveSoftJitter(particle.seed, Time.time, Mathf.Lerp(0.012f, 0.004f, delayedOpenT)) + local.normalized * lifePulse;

        Quaternion flowerPose = ResolveMagicBloomPose(center, scatterT, holdT);
        Vector3 shapedPosition = center + flowerPose * local;

        if (scatterT > 0f)
        {
            Vector3 scatterDirection = (flowerPose * local).normalized;
            if (scatterDirection.sqrMagnitude < 0.001f)
            {
                scatterDirection = particle.releaseDirection;
            }

            float scatterDistance = Mathf.Lerp(0f, particle.isPetal ? 1.35f : 0.95f, scatterT);
            Vector3 spiralWind = new Vector3(
                Mathf.Sin(Time.time * 0.9f + particle.seed),
                0f,
                Mathf.Cos(Time.time * 0.62f + particle.seed)) * scatterT * 0.36f;
            Vector3 liftThenFall = Vector3.up * Mathf.Sin(scatterT * Mathf.PI) * 0.42f
                + Vector3.down * scatterT * scatterT * (particle.isPetal ? 0.9f : 0.5f);
            shapedPosition += scatterDirection * scatterDistance + spiralWind + liftThenFall;
        }

        return Vector3.Lerp(particle.releaseSeedPosition, shapedPosition, gatherT);
    }

    private Quaternion ResolveMagicBloomPose(Vector3 center, float scatterT, float holdT)
    {
        Camera camera = Camera.main;
        Vector3 facing = camera != null ? center - camera.transform.position : transform.forward;
        facing.y = 0f;
        if (facing.sqrMagnitude < 0.001f)
        {
            facing = transform.forward;
            facing.y = 0f;
        }

        Quaternion faceYaw = Quaternion.LookRotation(facing.normalized, Vector3.up);
        float spinFade = 1f - scatterT * 0.55f;
        return faceYaw * Quaternion.Euler(
            -28f + Mathf.Sin(Time.time * 0.48f) * 5f + Time.time * roseRotationXSpeed * spinFade,
            Time.time * Mathf.Lerp(18f, 38f, holdT) * spinFade,
            Mathf.Cos(Time.time * 0.42f) * 6f + Time.time * roseRotationZSpeed * spinFade);
    }

    private static Vector3 ResolveMagicBloomPetalSurface(int index, int count, float seed)
    {
        int layer = Mathf.Abs(index) % MagicBloomLayerPetalCounts.Length;
        int petalsInLayer = MagicBloomLayerPetalCounts[layer];
        int petalIndex = (index / MagicBloomLayerPetalCounts.Length) % petalsInLayer;
        float layerT = layer / (float)(MagicBloomLayerPetalCounts.Length - 1);

        float u = Mathf.Repeat(index * 0.7548777f + seed * 0.017f, 1f);
        float v = Mathf.Repeat(index * 0.5698403f + seed * 0.023f, 1f);
        float width = (u - 0.5f) * 2f;
        float length = Mathf.Sqrt(v);

        if (index % 7 == 0)
        {
            width = Mathf.Sign(width == 0f ? 1f : width) * Mathf.Lerp(0.72f, 1f, u);
        }

        float baseAngle = petalIndex / (float)petalsInLayer * Mathf.PI * 2f + layer * 0.27f;
        float petalSpread = Mathf.Lerp(0.26f, 0.13f, layerT);
        float angle = baseAngle + width * petalSpread * Mathf.Lerp(0.35f, 1f, length);

        float rootRadius = Mathf.Lerp(0.08f, 0.28f, layerT);
        float petalLength = Mathf.Lerp(0.34f, 0.82f, layerT);
        float edgeTaper = 1f - Mathf.Abs(width);
        float radius = rootRadius + petalLength * length * Mathf.Lerp(0.74f, 1.13f, edgeTaper);

        float bowlHeight = Mathf.Lerp(0.28f, -0.16f, layerT);
        float petalSlope = Mathf.Lerp(0.18f, -0.34f, layerT) * length;
        float edgeCurl = Mathf.Sin(length * Mathf.PI) * Mathf.Lerp(0.05f, 0.18f, layerT);
        float sideCurl = Mathf.Abs(width) * Mathf.Abs(width) * Mathf.Lerp(0.05f, 0.16f, layerT);
        float y = bowlHeight + petalSlope + edgeCurl + sideCurl;

        return new Vector3(Mathf.Cos(angle) * radius, y, Mathf.Sin(angle) * radius);
    }

    private static Vector3 ResolveMagicBloomPollenCore(int index, int count, float seed)
    {
        float u = Mathf.Repeat(index * 0.6180339f + seed * 0.013f, 1f);
        float angle = u * Mathf.PI * 2f * 2.6f;
        float radius = Mathf.Lerp(0.018f, 0.2f, Mathf.Sqrt(u));
        float height = Mathf.Lerp(0.02f, 0.22f, Mathf.Repeat(index * 0.371f + seed, 1f));
        return new Vector3(Mathf.Cos(angle) * radius, height, Mathf.Sin(angle) * radius);
    }

    private static Vector3 ResolveMagicBloomBudPoint(int index, int count, float seed)
    {
        float t = index / Mathf.Max(1f, count - 1f);
        float angle = index * Mathf.PI * (3f - Mathf.Sqrt(5f)) + seed * 0.011f;
        float bulge = Mathf.Sin(t * Mathf.PI);
        float radius = Mathf.Lerp(0.025f, 0.16f, bulge);
        float y = Mathf.Lerp(-0.24f, 0.46f, t);
        return new Vector3(Mathf.Cos(angle) * radius, y, Mathf.Sin(angle) * radius);
    }

    private static float ResolveMagicBloomUnfurlDelay(int index, int count)
    {
        float normalized = index / Mathf.Max(1f, count - 1f);
        return Mathf.Clamp01(normalized * 0.22f);
    }

    private PetalPollenSource PickNearestSource()
    {
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

    private PetalPollenReleaseMode PickReleaseMode(float holdSeconds)
    {
        float galaxyWeight = galaxyVeilWeight;
        if (holdSeconds >= chargedHoldSeconds)
        {
            galaxyWeight += chargedGalaxyBonusWeight;
        }

        float total = Mathf.Max(0f, petalRainWeight)
            + Mathf.Max(0f, spiralBloomWeight)
            + Mathf.Max(0f, flowerConstellationWeight)
            + Mathf.Max(0f, mathRibbonWeight)
            + Mathf.Max(0f, tornadoVortexWeight)
            + Mathf.Max(0f, galaxyWeight);

        if (total <= 0.001f)
        {
            return PetalPollenReleaseMode.GalaxyVeil;
        }

        float roll = Random.value * total;
        roll -= Mathf.Max(0f, petalRainWeight);
        if (roll <= 0f)
        {
            return PetalPollenReleaseMode.PetalRain;
        }

        roll -= Mathf.Max(0f, spiralBloomWeight);
        if (roll <= 0f)
        {
            return PetalPollenReleaseMode.SpiralBloom;
        }

        roll -= Mathf.Max(0f, flowerConstellationWeight);
        if (roll <= 0f)
        {
            return PetalPollenReleaseMode.FlowerConstellation;
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

        EnsureBufferSize(pollenCount);
        EnsurePetalBufferSize(petalCount);

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

        if (particleOutput != null)
        {
            if (!particleOutput.isPlaying)
            {
                particleOutput.Play(true);
            }

            particleOutput.SetParticles(particleBuffer, pollenCount);
        }

        if (petalOutput != null)
        {
            if (!petalOutput.isPlaying)
            {
                petalOutput.Play(true);
            }

            petalOutput.SetParticles(petalBuffer, petalCount);
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

        if (magic.stage == ParticleStage.Releasing)
        {
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

    private bool IsTornadoDustParticle(MagicParticle magic)
    {
        if (magic.isPetal)
        {
            return false;
        }

        return Deterministic01(magic.seed * 0.173f + 11.7f) < Mathf.Clamp01(tornadoDustFraction);
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
            particleOutput = GetComponentInChildren<ParticleSystem>(true);
        }

        if (particleOutput == null)
        {
            GameObject child = new GameObject("_PetalPollenMagicParticles");
            child.transform.SetParent(transform, false);
            particleOutput = child.AddComponent<ParticleSystem>();
        }

        ParticleSystem.MainModule main = particleOutput.main;
        main.playOnAwake = false;
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = maxParticles;
        main.startSpeed = 0f;
        main.startLifetime = releaseDuration + 1f;

        ParticleSystem.EmissionModule emission = particleOutput.emission;
        emission.enabled = false;

        ParticleSystem.ShapeModule shape = particleOutput.shape;
        shape.enabled = false;

        ParticleSystemRenderer renderer = particleOutput.GetComponent<ParticleSystemRenderer>();
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
