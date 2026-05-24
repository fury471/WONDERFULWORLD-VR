using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class FlowerVortexEffect : MonoBehaviour
{
    private const float PetalVisualScale = 0.2f;

    [Header("References")]
    [SerializeField] private Transform treeCenter;
    [SerializeField] private GameObject petalPrefab;

    [Header("Petal Pool")]
    [SerializeField, Min(1)] private int petalCount = 1200;
    [SerializeField, Min(1)] private int maxPetalCount = 1800;
    [SerializeField] private bool playOnStart = false;
    [SerializeField] private bool loopEffect = false;
    [SerializeField] private bool holdStaticBloomAfterPlay = true;
    [SerializeField, Min(0f)] private float maxSimulationDistance = 85f;

    [Header("Timeline")]
    [SerializeField, Min(0f)] private float delayBeforeStart = 3f;
    [SerializeField, Min(0.01f)] private float spiralDuration = 6f;
    [SerializeField, Min(0.01f)] private float gatherDuration = 9f;
    [SerializeField, Min(0.01f)] private float scatterLifetime = 10f;

    [Header("Spiral")]
    [SerializeField] private float maxGrowthHeight = 10f;
    [SerializeField, Min(1)] private int tilesX = 2;
    [SerializeField, Min(1)] private int tilesY = 2;
    [SerializeField] private float spiralBaseRadius = 1.5f;
    [SerializeField] private float spiralExpansion = 12f;
    [SerializeField] private float spiralBandHeight = 4f;
    [SerializeField] private float swirlSpeed = 200f;
    [SerializeField, Range(0f, 30f)] private float volumeChaos = 18f;

    [Header("Gather")]
    [SerializeField] private float gatherRadius = 3.0f;
    [SerializeField] private float gatherHeightOffset = 15f;
    [SerializeField] private float sphereSpinSpeed = 300f;
    [SerializeField] private float sphereTurbulence = 4.5f;

    [Header("Scatter")]
    [SerializeField] private float explosionForce = 65f;
    [SerializeField, Range(0f, 5f)] private float airResistance = 1.2f;
    [SerializeField] private float scatterGravity = 0.6f;

    private enum EffectPhase
    {
        Waiting,
        SpiralingUp,
        Gathering,
        Exploded,
        StaticBloom
    }

    [SerializeField] private EffectPhase currentPhase = EffectPhase.Waiting;

    private readonly List<PetalData> petals = new();
    private float globalTimer;
    private Vector3 sphereCenter;
    private float sphereRotationAngle;
    private Mesh[] uvMeshes;
    private bool initialized;
    private bool playing;
    private bool completed;
    private Camera cachedCamera;

    private Vector3 Origin => treeCenter != null ? treeCenter.position : transform.position;
    public float TotalEffectDuration => delayBeforeStart + spiralDuration + gatherDuration + scatterLifetime;
    public bool IsPlaying => playing;
    public bool IsComplete => completed;

    private void Awake()
    {
        petalCount = Mathf.Clamp(petalCount, 1, maxPetalCount);
    }

    private void OnValidate()
    {
        maxPetalCount = Mathf.Max(1, maxPetalCount);
        petalCount = Mathf.Clamp(petalCount, 1, maxPetalCount);
        delayBeforeStart = Mathf.Max(0f, delayBeforeStart);
        spiralDuration = Mathf.Max(0.01f, spiralDuration);
        gatherDuration = Mathf.Max(0.01f, gatherDuration);
        scatterLifetime = Mathf.Max(0.01f, scatterLifetime);
        tilesX = Mathf.Max(1, tilesX);
        tilesY = Mathf.Max(1, tilesY);
    }

    private void Start()
    {
        InitializeIfNeeded();
        if (playOnStart)
        {
            RestartEffect();
        }
        else
        {
            SetEffectHidden();
        }
    }

    private void OnDestroy()
    {
        if (uvMeshes == null)
        {
            return;
        }

        for (int i = 0; i < uvMeshes.Length; i++)
        {
            if (uvMeshes[i] != null)
            {
                Destroy(uvMeshes[i]);
            }
        }
    }

    public void RestartEffect()
    {
        InitializeIfNeeded();
        if (!initialized)
        {
            return;
        }

        ResetLoop();
        playing = true;
        completed = false;
    }

    public void PlayOnce()
    {
        loopEffect = false;
        RestartEffect();
    }

    public void SetEffectHidden()
    {
        playing = false;
        completed = false;
        globalTimer = 0f;
        sphereRotationAngle = 0f;
        currentPhase = EffectPhase.Waiting;

        for (int i = 0; i < petals.Count; i++)
        {
            petals[i].gameObject.SetActive(false);
        }
    }

    public void SetStaticBloom()
    {
        InitializeIfNeeded();
        if (!initialized)
        {
            return;
        }

        playing = false;
        completed = true;
        ChangePhase(EffectPhase.StaticBloom);
        PlaceStaticBloom();
    }

    private void InitializeIfNeeded()
    {
        if (initialized)
        {
            return;
        }

        if (petalPrefab == null)
        {
            Debug.LogWarning($"{nameof(FlowerVortexEffect)} on {name} has no petal prefab.", this);
            enabled = false;
            return;
        }

        InitializeUVMeshes();
        SpawnPetals();
        ResetLoop();
        initialized = true;
    }

    private void Update()
    {
        if (!initialized)
        {
            return;
        }

        if (!playing)
        {
            return;
        }

        if (!ShouldSimulate())
        {
            return;
        }

        globalTimer += Time.deltaTime;

        float spiralStart = delayBeforeStart;
        float gatherStart = spiralStart + spiralDuration;
        float scatterStart = gatherStart + gatherDuration;
        float cycleEnd = scatterStart + scatterLifetime;

        if (globalTimer < spiralStart)
        {
            ChangePhase(EffectPhase.Waiting);
            return;
        }

        if (globalTimer < gatherStart)
        {
            ChangePhase(EffectPhase.SpiralingUp);
            UpdateVolumeSpiral((globalTimer - spiralStart) / spiralDuration);
            return;
        }

        if (globalTimer < scatterStart)
        {
            ChangePhase(EffectPhase.Gathering);
            UpdateToDynamicSphere((globalTimer - gatherStart) / gatherDuration);
            return;
        }

        if (globalTimer < cycleEnd)
        {
            ChangePhase(EffectPhase.Exploded);
            UpdateExplodedPhysics(globalTimer - scatterStart);
            return;
        }

        if (loopEffect)
        {
            ResetLoop();
            playing = true;
            return;
        }

        if (holdStaticBloomAfterPlay)
        {
            SetStaticBloom();
            return;
        }

        SetEffectHidden();
        completed = true;
    }

    private void ChangePhase(EffectPhase newPhase)
    {
        if (currentPhase == newPhase)
        {
            return;
        }

        currentPhase = newPhase;

        if (newPhase == EffectPhase.Gathering)
        {
            sphereCenter = Origin + Vector3.up * gatherHeightOffset;
            for (int i = 0; i < petals.Count; i++)
            {
                petals[i].GatherStartPosition = petals[i].CachedTransform.position;
            }
        }
        else if (newPhase == EffectPhase.Exploded)
        {
            ExplodeOmni();
        }
    }

    private void PlaceStaticBloom()
    {
        Vector3 bloomCenter = Origin + Vector3.up * (maxGrowthHeight * 0.72f);
        Vector3 trunkForward = treeCenter != null ? treeCenter.forward : transform.forward;
        Quaternion treeRotation = Quaternion.LookRotation(
            Vector3.ProjectOnPlane(trunkForward, Vector3.up).sqrMagnitude > 0.0001f
                ? Vector3.ProjectOnPlane(trunkForward, Vector3.up).normalized
                : Vector3.forward,
            Vector3.up);

        for (int i = 0; i < petals.Count; i++)
        {
            PetalData data = petals[i];
            Vector3 spherePoint = GetFibonacciSpherePoint(i, petals.Count);
            float petalOffset = Mathf.Repeat(data.NoiseOffset * 0.173f, 1f);
            float canopyRadius = Mathf.Lerp(1.6f, 4.8f, petalOffset);
            float verticalSquash = Mathf.Lerp(0.34f, 0.72f, Mathf.Repeat(data.NoiseOffset * 0.317f, 1f));
            Vector3 localOffset = new Vector3(
                spherePoint.x * canopyRadius,
                spherePoint.y * canopyRadius * verticalSquash,
                spherePoint.z * canopyRadius * 0.82f);

            Transform petalTransform = data.CachedTransform;
            petalTransform.SetPositionAndRotation(
                bloomCenter + treeRotation * localOffset,
                treeRotation * Quaternion.Euler(90f + spherePoint.y * 25f, data.NoiseOffset * 360f, petalOffset * 180f));
            petalTransform.localScale = Vector3.one * PetalVisualScale;
            data.gameObject.SetActive(true);
        }
    }

    private void UpdateVolumeSpiral(float t)
    {
        Vector3 origin = Origin;
        float baseHeight = t * maxGrowthHeight;
        float currentRadius = spiralBaseRadius + t * spiralExpansion * 0.5f;
        float currentBandHeight = t * spiralBandHeight;

        for (int i = 0; i < petals.Count; i++)
        {
            PetalData data = petals[i];
            float petalOffset = (float)i / petals.Count;
            float angle = Time.time * swirlSpeed + petalOffset * 360f * 4f;
            float noiseOffset = data.NoiseOffset;

            float verticalOffset = (Mathf.PerlinNoise(noiseOffset, 0f) - 0.5f) * currentBandHeight;
            float radialNoise = (Mathf.PerlinNoise(Time.time * 3.0f, noiseOffset) - 0.5f) * volumeChaos * t;
            float targetHeight = baseHeight + verticalOffset + (Mathf.PerlinNoise(Time.time * 2.0f, noiseOffset * 2f) - 0.5f) * volumeChaos * 0.5f * t;

            Vector3 targetPosition = origin + new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * (currentRadius + radialNoise),
                targetHeight,
                Mathf.Sin(angle * Mathf.Deg2Rad) * (currentRadius + radialNoise));

            Transform petalTransform = data.CachedTransform;
            petalTransform.position = Vector3.Lerp(petalTransform.position, targetPosition, Time.deltaTime * 6f);
            petalTransform.Rotate(data.RotationAxis, 800f * Time.deltaTime);
        }
    }

    private void UpdateToDynamicSphere(float t)
    {
        float smoothT = Mathf.SmoothStep(0f, 1f, t);
        float dynamicSpinSpeed = Mathf.Lerp(swirlSpeed * 2.5f, sphereSpinSpeed, t);
        sphereRotationAngle += Time.deltaTime * dynamicSpinSpeed;

        for (int i = 0; i < petals.Count; i++)
        {
            PetalData data = petals[i];
            Vector3 rawSpherePoint = GetFibonacciSpherePoint(i, petals.Count);

            float noiseTime = Time.time * 2f;
            float noiseX = Mathf.PerlinNoise(noiseTime + data.NoiseOffset, 0f) - 0.5f;
            float noiseZ = Mathf.PerlinNoise(noiseTime, data.NoiseOffset) - 0.5f;

            Vector3 chaoticOffset = new Vector3(noiseX, 0f, noiseZ) * sphereTurbulence;
            Quaternion sphereRotation = Quaternion.Euler(noiseX * 20f, sphereRotationAngle + data.NoiseOffset * 0.2f, noiseZ * 20f);
            Vector3 targetSpherePosition = sphereCenter + sphereRotation * (rawSpherePoint * gatherRadius + chaoticOffset);

            Transform petalTransform = data.CachedTransform;
            petalTransform.position = Vector3.Lerp(data.GatherStartPosition, targetSpherePosition, smoothT);

            float dynamicSelfRotation = Mathf.Lerp(800f, 200f * noiseX, smoothT);
            petalTransform.Rotate(data.RotationAxis, dynamicSelfRotation * Time.deltaTime);
        }
    }

    private void ExplodeOmni()
    {
        for (int i = 0; i < petals.Count; i++)
        {
            PetalData data = petals[i];
            Vector3 direction = (data.CachedTransform.position - sphereCenter).normalized;
            direction = Vector3.Lerp(direction, Random.onUnitSphere, 0.7f).normalized;
            data.Velocity = direction * explosionForce * Random.Range(0.5f, 1.5f);
            data.RotationSpeed = Random.Range(800f, 1500f);
        }
    }

    private void UpdateExplodedPhysics(float elapsedInPhase)
    {
        float lifeT = 1f - elapsedInPhase / scatterLifetime;

        for (int i = 0; i < petals.Count; i++)
        {
            PetalData data = petals[i];
            GameObject petal = data.gameObject;
            if (!petal.activeSelf)
            {
                continue;
            }

            data.Velocity += Vector3.down * scatterGravity * Time.deltaTime;
            data.Velocity *= 1f - airResistance * Time.deltaTime;

            Transform petalTransform = data.CachedTransform;
            petalTransform.position += data.Velocity * Time.deltaTime;
            petalTransform.Rotate(data.RotationAxis, data.RotationSpeed * Time.deltaTime);
            petalTransform.localScale = Vector3.one * Mathf.Clamp01(lifeT * 1.5f) * PetalVisualScale;

            if (lifeT <= 0f)
            {
                petal.SetActive(false);
            }
        }
    }

    private void ResetLoop()
    {
        globalTimer = 0f;
        sphereRotationAngle = 0f;
        Vector3 resetPosition = Origin;

        for (int i = 0; i < petals.Count; i++)
        {
            PetalData data = petals[i];
            data.CachedTransform.SetPositionAndRotation(resetPosition, Random.rotation);
            data.CachedTransform.localScale = Vector3.one * PetalVisualScale;
            data.gameObject.SetActive(true);
        }

        currentPhase = EffectPhase.Waiting;
        completed = false;
    }

    private void InitializeUVMeshes()
    {
        if (uvMeshes != null)
        {
            return;
        }

        MeshFilter prefabFilter = petalPrefab.GetComponent<MeshFilter>();
        if (prefabFilter == null || prefabFilter.sharedMesh == null)
        {
            return;
        }

        Mesh originalMesh = prefabFilter.sharedMesh;
        int totalFrames = tilesX * tilesY;
        uvMeshes = new Mesh[totalFrames];

        float tileX = 1f / tilesX;
        float tileY = 1f / tilesY;

        for (int i = 0; i < totalFrames; i++)
        {
            Mesh frameMesh = Instantiate(originalMesh);
            frameMesh.name = $"{originalMesh.name}_PetalFrame_{i}";

            int column = i % tilesX;
            int row = i / tilesX;
            Vector2[] uvs = frameMesh.uv;

            for (int j = 0; j < uvs.Length; j++)
            {
                uvs[j] = new Vector2(uvs[j].x * tileX + column * tileX, uvs[j].y * tileY + row * tileY);
            }

            frameMesh.uv = uvs;
            uvMeshes[i] = frameMesh;
        }
    }

    private void SpawnPetals()
    {
        if (petals.Count > 0)
        {
            return;
        }

        Vector3 spawnPosition = Origin;

        for (int i = 0; i < petalCount; i++)
        {
            GameObject petal = Instantiate(petalPrefab, spawnPosition, Random.rotation, transform);
            petal.transform.localScale = Vector3.one * PetalVisualScale;

            PetalData data = petal.AddComponent<PetalData>();
            data.Initialize(Random.value * 1000f, Random.onUnitSphere);
            petals.Add(data);

            MeshFilter filter = petal.GetComponent<MeshFilter>();
            if (filter != null && uvMeshes != null && uvMeshes.Length > 0)
            {
                filter.sharedMesh = uvMeshes[Random.Range(0, uvMeshes.Length)];
            }
        }
    }

    private bool ShouldSimulate()
    {
        if (maxSimulationDistance <= 0f)
        {
            return true;
        }

        if (cachedCamera == null)
        {
            cachedCamera = QuestInteractionUtils.FindHeadCamera();
        }

        if (cachedCamera == null)
        {
            return true;
        }

        float sqrDistance = (cachedCamera.transform.position - Origin).sqrMagnitude;
        return sqrDistance <= maxSimulationDistance * maxSimulationDistance;
    }

    private static Vector3 GetFibonacciSpherePoint(int index, int count)
    {
        if (count <= 1)
        {
            return Vector3.up;
        }

        float y = 1f - index / (float)(count - 1) * 2f;
        float radius = Mathf.Sqrt(1f - y * y);
        float goldenRatio = (1f + Mathf.Sqrt(5f)) / 2f;
        float angle = 2f * Mathf.PI * goldenRatio * index;
        return new Vector3(Mathf.Cos(angle) * radius, y, Mathf.Sin(angle) * radius);
    }

    private sealed class PetalData : MonoBehaviour
    {
        public Transform CachedTransform { get; private set; }
        public Vector3 Velocity { get; set; }
        public Vector3 RotationAxis { get; private set; }
        public float NoiseOffset { get; private set; }
        public float RotationSpeed { get; set; }
        public Vector3 GatherStartPosition { get; set; }

        public void Initialize(float noiseOffset, Vector3 rotationAxis)
        {
            CachedTransform = transform;
            NoiseOffset = noiseOffset;
            RotationAxis = rotationAxis == Vector3.zero ? Vector3.up : rotationAxis;
        }
    }
}
