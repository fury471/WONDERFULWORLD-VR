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
        public Vector3 releaseDirection;
        public Color color;
        public float age;
        public float releaseAge;
        public float seed;
        public float size;
        public float flowDuration;
        public bool isPetal;
    }

    [Header("References")]
    [SerializeField] private Transform handAnchor;
    [SerializeField] private Transform playerHead;
    [SerializeField] private ParticleSystem particleOutput;
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
    [SerializeField] private float releaseFlashSeconds = 0.22f;
    [SerializeField] private float burstRadius = 1.9f;
    [SerializeField] private float galaxyRadius = 1.85f;
    [SerializeField] private float galaxyHeight = 0.9f;
    [SerializeField] private float petalRainHeight = 2.2f;

    [Header("Weighted Surprise")]
    [SerializeField] private float petalRainWeight = 0.3f;
    [SerializeField] private float spiralBloomWeight = 0.25f;
    [SerializeField] private float flowerConstellationWeight = 0.2f;
    [SerializeField] private float galaxyVeilWeight = 0.25f;
    [SerializeField] private float chargedGalaxyBonusWeight = 0.35f;

    [Header("Look")]
    [SerializeField] private float pollenSize = 0.045f;
    [SerializeField] private float petalSize = 0.12f;
    [SerializeField] private Color secondaryPollenColor = new Color(0.62f, 0.95f, 1f, 1f);
    [SerializeField] private Color galaxyViolet = new Color(0.72f, 0.48f, 1f, 1f);

    private readonly List<MagicParticle> activeParticles = new List<MagicParticle>();
    private ParticleSystem.Particle[] particleBuffer = new ParticleSystem.Particle[0];
    private float spawnAccumulator;
    private float collectStartTime;
    private float releaseStartTime;
    private bool isCollecting;
    private bool releaseActive;
    private PetalPollenReleaseMode activeReleaseMode;

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

        Vector3 center = GetHoldCenter();
        for (int i = 0; i < activeParticles.Count; i++)
        {
            MagicParticle particle = activeParticles[i];
            particle.stage = ParticleStage.Releasing;
            particle.releaseAge = 0f;
            particle.releaseStartPosition = particle.currentPosition;
            particle.releaseDirection = (particle.currentPosition - center).normalized;
            if (particle.releaseDirection.sqrMagnitude < 0.001f)
            {
                particle.releaseDirection = Random.onUnitSphere;
            }
        }
    }

    public void Clear()
    {
        isCollecting = false;
        releaseActive = false;
        activeParticles.Clear();
        if (particleOutput != null)
        {
            particleOutput.Clear(true);
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
            Vector3 seedPoint = GetHoldCenter();
            particle.currentPosition = Vector3.Lerp(particle.releaseStartPosition, seedPoint, Smoother01(flashT));
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
            default:
                particle.currentPosition = ResolveGalaxyVeilPosition(particle, index, count, showT);
                break;
        }
    }

    private Vector3 ResolveGalaxyVeilPosition(MagicParticle particle, int index, int count, float t)
    {
        Vector3 center = GetPlayerCenter();
        float strand = index % 2 == 0 ? 1f : -1f;
        float angle = particle.seed + t * Mathf.PI * 4.5f * strand + index * 0.037f;
        float radius = Mathf.Lerp(0.18f, galaxyRadius * (particle.isPetal ? 1.05f : 1f), Smoother01(Mathf.Clamp01(t * 1.6f)));
        float arm = Mathf.Sin(angle * 2f + particle.seed) * 0.28f;
        float height = Mathf.Sin(angle * 1.7f + particle.seed) * galaxyHeight * (0.35f + t * 0.65f);

        Vector3 local = new Vector3(
            Mathf.Cos(angle) * (radius + arm),
            height,
            Mathf.Sin(angle) * (radius + arm));

        Quaternion tilt = Quaternion.Euler(18f, t * 140f, -12f);
        Vector3 drift = Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.24f;
        return Vector3.Lerp(particle.releaseStartPosition, center + tilt * local + drift, Smoother01(Mathf.Clamp01(t * 2.2f)));
    }

    private Vector3 ResolvePetalRainPosition(MagicParticle particle, float t)
    {
        Vector3 center = GetPlayerCenter();
        float angle = particle.seed;
        float radius = Mathf.Lerp(0.25f, burstRadius, Mathf.Clamp01(t * 1.8f));
        Vector3 rise = center
            + new Vector3(Mathf.Cos(angle) * radius, petalRainHeight, Mathf.Sin(angle) * radius)
            + ResolveSoftJitter(particle.seed, Time.time, 0.12f);

        float fallT = Mathf.Clamp01((t - 0.28f) / 0.72f);
        Vector3 fall = rise + Vector3.down * (fallT * fallT * (petalRainHeight + 0.65f));
        fall += new Vector3(Mathf.Sin(Time.time + particle.seed), 0f, Mathf.Cos(Time.time * 0.7f + particle.seed)) * fallT * 0.35f;
        return Vector3.Lerp(particle.releaseStartPosition, fall, Smoother01(Mathf.Clamp01(t * 2.4f)));
    }

    private Vector3 ResolveSpiralBloomPosition(MagicParticle particle, int index, int count, float t)
    {
        Vector3 center = GetHoldCenter();
        float progress = index / Mathf.Max(1f, count - 1f);
        float strand = index % 2 == 0 ? 1f : -1f;
        float angle = progress * Mathf.PI * 10f + t * Mathf.PI * 5f * strand + particle.seed;
        float radius = Mathf.Lerp(0.08f, burstRadius, Smoother01(t));
        float height = Mathf.Lerp(-0.7f, 1.4f, progress) + Mathf.Sin(angle * 1.6f) * 0.18f;
        Vector3 target = center + new Vector3(Mathf.Cos(angle) * radius, height, Mathf.Sin(angle) * radius);
        return Vector3.Lerp(particle.releaseStartPosition, target, Smoother01(Mathf.Clamp01(t * 2f)));
    }

    private Vector3 ResolveFlowerConstellationPosition(MagicParticle particle, int index, int count, float t)
    {
        Vector3 center = GetHoldCenter() + Vector3.up * 0.35f;
        float progress = index / Mathf.Max(1f, count - 1f);
        float angle = progress * Mathf.PI * 2f;
        float petalWave = 0.45f + Mathf.Abs(Mathf.Cos(angle * 3f)) * 0.85f;
        Vector3 flower = new Vector3(Mathf.Cos(angle) * petalWave, Mathf.Sin(angle) * petalWave, Mathf.Sin(angle * 6f) * 0.12f);

        Camera camera = Camera.main;
        Quaternion faceCamera = camera != null
            ? Quaternion.LookRotation((center - camera.transform.position).normalized, Vector3.up)
            : Quaternion.identity;

        float lockT = Mathf.Clamp01(t * 2.1f);
        Vector3 target = center + faceCamera * flower;
        target += ResolveSoftJitter(particle.seed, Time.time, Mathf.Lerp(0.1f, 0.025f, lockT));

        if (t > 0.62f)
        {
            float dissolveT = (t - 0.62f) / 0.38f;
            target += Vector3.down * dissolveT * dissolveT * 1.25f;
        }

        return Vector3.Lerp(particle.releaseStartPosition, target, Smoother01(lockT));
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

        return PetalPollenReleaseMode.GalaxyVeil;
    }

    private void RenderParticles()
    {
        EnsureParticleOutput();
        EnsureBufferSize(activeParticles.Count);

        for (int i = 0; i < activeParticles.Count; i++)
        {
            MagicParticle magic = activeParticles[i];
            Color color = ResolveColor(magic);
            float size = ResolveSize(magic);

            particleBuffer[i].position = magic.currentPosition;
            particleBuffer[i].velocity = Vector3.zero;
            particleBuffer[i].startLifetime = releaseDuration + 1f;
            particleBuffer[i].remainingLifetime = releaseDuration + 1f;
            particleBuffer[i].startColor = color;
            particleBuffer[i].startSize = size;
            particleBuffer[i].rotation3D = new Vector3(0f, 0f, Time.time * (magic.isPetal ? 140f : 40f) + magic.seed * 19f);
        }

        if (!particleOutput.isPlaying)
        {
            particleOutput.Play(true);
        }

        particleOutput.SetParticles(particleBuffer, activeParticles.Count);
    }

    private Color ResolveColor(MagicParticle magic)
    {
        Color color = magic.color;
        if (activeReleaseMode == PetalPollenReleaseMode.GalaxyVeil && magic.stage == ParticleStage.Releasing)
        {
            color = Color.Lerp(color, galaxyViolet, magic.isPetal ? 0.25f : 0.42f);
        }

        if (magic.stage == ParticleStage.Releasing)
        {
            float t = Mathf.Clamp01(magic.releaseAge / Mathf.Max(0.01f, releaseDuration));
            color.a = 1f - Smoother01(Mathf.Clamp01((t - 0.72f) / 0.28f));
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
            size *= Mathf.Lerp(1f, 2.4f, flash);
        }

        return size;
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

    private static float Smoother01(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * t * (t * (t * 6f - 15f) + 10f);
    }
}
