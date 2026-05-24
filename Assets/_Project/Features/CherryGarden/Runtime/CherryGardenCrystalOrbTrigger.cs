using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

[DefaultExecutionOrder(-50)]
[DisallowMultipleComponent]
public sealed class CherryGardenCrystalOrbTrigger : MonoBehaviour
{
    private const string DefaultTextureResourcePath = "CherryGarden/CrystalOrbTexture";

    [Header("Targets")]
    [SerializeField] private TreeGrowthController treeGrowthController;
    [SerializeField] private FlowerVortexEffect flowerVortexEffect;
    [SerializeField] private Transform orbAnchor;

    [Header("Orb")]
    [SerializeField] private string textureResourcePath = DefaultTextureResourcePath;
    [SerializeField] private float orbRadius = 1.05f;
    [SerializeField] private Vector3 treeRelativeOffset = new Vector3(0f, 2.35f, 0f);
    [SerializeField] private Color orbColor = new Color(1f, 0.72f, 0.84f, 0.9f);
    [SerializeField] private Color emissionColor = new Color(1f, 0.36f, 0.66f, 1f);
    [SerializeField, Min(0f)] private float emissionIntensity = 0.62f;
    [SerializeField, Min(0f)] private float pointLightIntensity = 0.24f;
    [SerializeField, Min(0.1f)] private float pointLightRange = 4.8f;
    [SerializeField] private Vector2 textureDriftSpeed = new Vector2(0.018f, 0.032f);
    [SerializeField, Min(0f)] private float collapseDuration = 0.72f;
    [SerializeField, Min(0f)] private float shakeAmplitude = 0.16f;
    [SerializeField, Min(0f)] private float shakeFrequency = 58f;
    [SerializeField, Min(0f)] private float collapseLightBoost = 1.35f;
    [SerializeField] private bool createOrbOnStart = true;

    [Header("Growth Audio")]
    [SerializeField] private string growthLoopCueName = "WW_SFX_GrowthRustle";
    [SerializeField] private bool useProceduralEarthGrowthAudio = true;
    [SerializeField, Range(0f, 2f)] private float growthLoopVolumeScale = 1.1f;
    [SerializeField, Min(0f)] private float growthLoopFadeOutSeconds = 0.32f;

    [Header("Interaction")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform rightRayOrigin;
    [SerializeField] private LayerMask interactLayers = ~0;
    [SerializeField] private float maxInteractDistance = 36f;
    [SerializeField] private float recognitionRadius = 1.2f;
    [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;
    [SerializeField] private bool enableMouseDebugClick = true;

    private readonly RaycastHit[] raycastHits = new RaycastHit[8];
    private GameObject orbRoot;
    private Renderer[] orbRenderers;
    private Collider[] orbColliders;
    private Material orbMaterial;
    private Material haloMaterial;
    private Light orbLight;
    private AudioSource growthAudioSource;
    private Coroutine growthAudioRoutine;
    private static AudioClip proceduralEarthGrowthClip;
    private QuestInteractableFeedback interactionFeedback;
    private bool activated;
    private bool hovering;
    private bool rightTriggerLastFrame;
    private Vector3 baseOrbScale;
    private Vector3 baseOrbPosition;

    private void Awake()
    {
        CacheTargets();
        PrepareCherryGarden();

        if (createOrbOnStart)
        {
            CreateOrbIfNeeded();
        }
    }

    private void OnDestroy()
    {
        if (growthAudioRoutine != null)
        {
            StopCoroutine(growthAudioRoutine);
            growthAudioRoutine = null;
        }

        StopGrowthAudio(true);

        if (orbMaterial != null)
        {
            Destroy(orbMaterial);
        }

        if (haloMaterial != null)
        {
            Destroy(haloMaterial);
        }
    }

    private void Update()
    {
        if (activated || orbRoot == null)
        {
            return;
        }

        CacheInteractionReferences();
        AnimateIdleOrb();
        UpdateHover();

        if (!WasInteractPressed(out bool useMouseRay))
        {
            return;
        }

        if (TryBuildInteractionRay(useMouseRay, out Ray ray) && RayHitsOrb(ray))
        {
            Activate();
        }
    }

    public void Configure(TreeGrowthController treeGrowth, FlowerVortexEffect flowerVortex)
    {
        treeGrowthController = treeGrowth;
        flowerVortexEffect = flowerVortex;
        CacheTargets();
        PrepareCherryGarden();
        CreateOrbIfNeeded();
    }

    public void Activate()
    {
        if (activated)
        {
            return;
        }

        activated = true;
        interactionFeedback?.SetInteractable(false);
        interactionFeedback?.PulseSelect(null);
        WonderfulWorld.Audio.WonderlandAudioOneShotPlayer.PlayAt("WW_SFX_CrystalSelect", ResolveOrbPosition(), volumeScale: 1f, maxVoices: 4);
        StartCoroutine(ActivateSequence());
    }

    private void CacheTargets()
    {
        if (treeGrowthController == null)
        {
            treeGrowthController = FindFirstObjectByType<TreeGrowthController>();
        }

        if (flowerVortexEffect == null)
        {
            flowerVortexEffect = FindFirstObjectByType<FlowerVortexEffect>();
        }

        if (playerCamera == null)
        {
            playerCamera = ResolvePlayerCamera();
        }
    }

    private void CacheInteractionReferences()
    {
        if (rightRayOrigin == null)
        {
            rightRayOrigin = QuestInteractionUtils.FindControllerRayOrigin(true);
        }

        if (playerCamera == null)
        {
            playerCamera = ResolvePlayerCamera();
        }
    }

    private void PrepareCherryGarden()
    {
        treeGrowthController?.SetSeedState();
        flowerVortexEffect?.SetEffectHidden();
    }

    private void CreateOrbIfNeeded()
    {
        if (orbRoot != null)
        {
            return;
        }

        orbRoot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        orbRoot.name = "CherryGarden_CrystalOrb";
        orbRoot.transform.SetParent(transform, true);
        orbRoot.transform.position = ResolveOrbPosition();
        orbRoot.transform.localScale = Vector3.one * (orbRadius * 2f);
        baseOrbScale = orbRoot.transform.localScale;
        baseOrbPosition = orbRoot.transform.position;
        CrystalStoneOrbStyle.ApplyMesh(orbRoot.GetComponent<MeshFilter>());

        Texture2D texture = Resources.Load<Texture2D>(string.IsNullOrWhiteSpace(textureResourcePath)
            ? DefaultTextureResourcePath
            : textureResourcePath);

        orbMaterial = CreateOrbMaterial(texture, orbColor, false, 1.15f);
        Renderer orbRenderer = orbRoot.GetComponent<Renderer>();
        if (orbRenderer != null)
        {
            orbRenderer.sharedMaterial = orbMaterial;
        }

        GameObject halo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        halo.name = "CherryGarden_CrystalOrbHighlightVeins";
        halo.transform.SetParent(orbRoot.transform, false);
        halo.transform.localScale = Vector3.one * 1.06f;
        CrystalStoneOrbStyle.ApplyMesh(halo.GetComponent<MeshFilter>());
        Collider haloCollider = halo.GetComponent<Collider>();
        if (haloCollider != null)
        {
            Destroy(haloCollider);
        }

        haloMaterial = CreateOrbMaterial(texture, new Color(1f, 0.78f, 0.94f, 0.52f), true, 2.85f);
        Renderer haloRenderer = halo.GetComponent<Renderer>();
        if (haloRenderer != null)
        {
            haloRenderer.sharedMaterial = haloMaterial;
        }

        orbLight = orbRoot.AddComponent<Light>();
        orbLight.type = LightType.Point;
        orbLight.color = emissionColor;
        orbLight.intensity = pointLightIntensity;
        orbLight.range = pointLightRange;

        interactionFeedback = orbRoot.AddComponent<QuestInteractableFeedback>();
        interactionFeedback.Configure(new Color(1f, 0.48f, 0.72f, 0.74f), 0.026f);

        orbRenderers = orbRoot.GetComponentsInChildren<Renderer>(true);
        orbColliders = orbRoot.GetComponentsInChildren<Collider>(true);
    }

    private Material CreateOrbMaterial(Texture texture, Color color, bool additive, float emissionMultiplier = 1f)
    {
        return CrystalStoneOrbStyle.CreateMaterial(
            texture,
            color,
            emissionColor,
            emissionIntensity * emissionMultiplier,
            additive,
            additive ? "M_CherryGarden_CrystalOrb_Glow_Runtime" : "M_CherryGarden_CrystalOrb_Runtime");
    }

    private void SetMaterialColor(Material material, Color color, float emissionMultiplier)
    {
        CrystalStoneOrbStyle.SetMaterialColor(material, color, emissionColor, emissionMultiplier);
    }

    private Vector3 ResolveOrbPosition()
    {
        if (orbAnchor != null)
        {
            return orbAnchor.position;
        }

        if (treeGrowthController != null)
        {
            return treeGrowthController.transform.position + treeRelativeOffset;
        }

        if (flowerVortexEffect != null)
        {
            return flowerVortexEffect.transform.position + treeRelativeOffset;
        }

        return transform.position + treeRelativeOffset;
    }

    private void AnimateIdleOrb()
    {
        float pulse = 1f + Mathf.Sin(Time.time * 2.6f) * 0.035f;
        orbRoot.transform.position = baseOrbPosition + Vector3.up * Mathf.Sin(Time.time * 1.8f) * 0.035f;
        orbRoot.transform.localScale = baseOrbScale * pulse;
        orbRoot.transform.Rotate(Vector3.up, 18f * Time.deltaTime, Space.World);

        if (orbLight != null)
        {
            orbLight.intensity = pointLightIntensity * (1f + Mathf.Sin(Time.time * 3.1f) * 0.14f);
        }

        AnimateTexture(orbMaterial, textureDriftSpeed);
        AnimateTexture(haloMaterial, -textureDriftSpeed * 0.55f);
    }

    private void AnimateTexture(Material material, Vector2 speed)
    {
        if (material == null)
        {
            return;
        }

        Vector2 offset = new Vector2(
            Mathf.Repeat(Time.time * speed.x, 1f),
            Mathf.Repeat(Time.time * speed.y, 1f));

        if (material.HasProperty("_BaseMap"))
        {
            material.SetTextureOffset("_BaseMap", offset);
        }

        if (material.HasProperty("_MainTex"))
        {
            material.SetTextureOffset("_MainTex", offset);
        }
    }

    private void UpdateHover()
    {
        bool hover = false;
        if (TryBuildInteractionRay(false, out Ray ray))
        {
            hover = RayHitsOrb(ray);
        }

        if (hover != hovering)
        {
            interactionFeedback?.SetHovered(hover);
            hovering = hover;
        }
    }

    private bool WasInteractPressed(out bool useMouseRay)
    {
        useMouseRay = false;

        bool rightTriggerPressed = false;
        QuestInteractionUtils.TryReadTriggerButton(true, out rightTriggerPressed);
        bool pressedThisFrame = rightTriggerPressed && !rightTriggerLastFrame;
        rightTriggerLastFrame = rightTriggerPressed;
        if (pressedThisFrame)
        {
            return true;
        }

        if (enableMouseDebugClick && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            useMouseRay = true;
            return true;
        }

        return false;
    }

    private bool TryBuildInteractionRay(bool useMouseRay, out Ray ray)
    {
        if (useMouseRay && playerCamera != null && Mouse.current != null)
        {
            ray = playerCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            return true;
        }

        if (rightRayOrigin != null)
        {
            ray = new Ray(rightRayOrigin.position, rightRayOrigin.forward);
            return true;
        }

        if (playerCamera != null)
        {
            ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            return true;
        }

        ray = default;
        return false;
    }

    private static Camera ResolvePlayerCamera()
    {
        return QuestInteractionUtils.FindHeadCamera();
    }

    private bool RayHitsOrb(Ray ray)
    {
        int hitCount = Physics.RaycastNonAlloc(
            ray,
            raycastHits,
            Mathf.Max(0.1f, maxInteractDistance),
            interactLayers,
            triggerInteraction);

        Collider nearestCollider = null;
        float nearestDistance = float.PositiveInfinity;
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = raycastHits[i];
            if (hit.distance < nearestDistance)
            {
                nearestDistance = hit.distance;
                nearestCollider = hit.collider;
            }
        }

        if (IsOrbCollider(nearestCollider))
        {
            return true;
        }

        Vector3 orbPosition = orbRoot != null ? orbRoot.transform.position : transform.position;
        Vector3 direction = ray.direction.sqrMagnitude > 0.0001f ? ray.direction.normalized : Vector3.forward;
        float projectedDistance = Vector3.Dot(orbPosition - ray.origin, direction);
        if (projectedDistance < 0f || projectedDistance > maxInteractDistance)
        {
            return false;
        }

        Vector3 closestPoint = ray.origin + direction * projectedDistance;
        return Vector3.Distance(orbPosition, closestPoint) <= Mathf.Max(orbRadius, recognitionRadius);
    }

    private bool IsOrbCollider(Collider candidate)
    {
        if (candidate == null || orbColliders == null)
        {
            return false;
        }

        for (int i = 0; i < orbColliders.Length; i++)
        {
            if (orbColliders[i] == candidate)
            {
                return true;
            }
        }

        return candidate.transform.IsChildOf(orbRoot.transform);
    }

    private IEnumerator ActivateSequence()
    {
        yield return CollapseOrb();
        Vector3 growthPosition = treeGrowthController != null ? treeGrowthController.transform.position : ResolveOrbPosition();
        if (growthAudioRoutine != null)
        {
            StopCoroutine(growthAudioRoutine);
        }

        growthAudioRoutine = StartCoroutine(PlayGrowthAudioForTree(growthPosition));
        treeGrowthController?.PlayGrowthOnce();
        flowerVortexEffect?.PlayOnce();
    }

    private IEnumerator PlayGrowthAudioForTree(Vector3 growthPosition)
    {
        float startDelay = treeGrowthController != null ? treeGrowthController.GrowthStartDelay : 0f;
        if (startDelay > 0f)
        {
            yield return new WaitForSeconds(startDelay);
        }

        float activeDuration = treeGrowthController != null
            ? treeGrowthController.ActiveGrowthDuration
            : Mathf.Max(0.01f, flowerVortexEffect != null ? flowerVortexEffect.TotalEffectDuration : 6f);

        WonderfulWorld.Audio.WonderlandAudioCue cue = WonderfulWorld.Audio.WonderlandRuntimeAudioLibrary.LoadCue(growthLoopCueName);
        AudioClip clip = useProceduralEarthGrowthAudio ? GetProceduralEarthGrowthClip() : (cue != null ? cue.PickClip() : null);
        if (clip == null)
        {
            growthAudioRoutine = null;
            yield break;
        }

        EnsureGrowthAudioSource(growthPosition);
        if (cue != null)
        {
            cue.ApplyTo(growthAudioSource, assignClip: false);
        }

        growthAudioSource.clip = clip;
        growthAudioSource.loop = true;
        growthAudioSource.playOnAwake = false;
        growthAudioSource.pitch = cue != null && !useProceduralEarthGrowthAudio ? cue.ResolvePitch() : 1f;
        growthAudioSource.volume = cue != null
            ? cue.ResolveVolume(growthLoopVolumeScale)
            : Mathf.Clamp01(growthLoopVolumeScale);
        growthAudioSource.time = 0f;
        growthAudioSource.Play();

        yield return new WaitForSeconds(Mathf.Max(0.01f, activeDuration));
        yield return FadeOutGrowthAudio();
        growthAudioRoutine = null;
    }

    private void EnsureGrowthAudioSource(Vector3 position)
    {
        if (growthAudioSource == null)
        {
            GameObject audioObject = new GameObject("CherryGarden_TreeGrowthAudio");
            audioObject.transform.SetParent(transform, true);
            growthAudioSource = audioObject.AddComponent<AudioSource>();
        }

        growthAudioSource.transform.position = position;
        growthAudioSource.spatialBlend = 1f;
        growthAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
    }

    private IEnumerator FadeOutGrowthAudio()
    {
        if (growthAudioSource == null)
        {
            yield break;
        }

        float duration = Mathf.Max(0f, growthLoopFadeOutSeconds);
        float startVolume = growthAudioSource.volume;
        if (duration > 0f)
        {
            float elapsed = 0f;
            while (elapsed < duration && growthAudioSource != null)
            {
                elapsed += Time.deltaTime;
                growthAudioSource.volume = Mathf.Lerp(startVolume, 0f, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
        }

        StopGrowthAudio(true);
    }

    private void StopGrowthAudio(bool immediate)
    {
        if (growthAudioSource == null)
        {
            return;
        }

        if (immediate)
        {
            growthAudioSource.Stop();
            growthAudioSource.clip = null;
            growthAudioSource.volume = 0f;
        }
    }

    private static AudioClip GetProceduralEarthGrowthClip()
    {
        if (proceduralEarthGrowthClip != null)
        {
            return proceduralEarthGrowthClip;
        }

        const int sampleRate = 44100;
        const float lengthSeconds = 2.8f;
        int samples = Mathf.CeilToInt(sampleRate * lengthSeconds);
        float[] data = new float[samples];
        float lowpass = 0f;

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float loopT = t / lengthSeconds;
            float loopEnvelope = 0.84f + Mathf.Sin(loopT * Mathf.PI * 2f) * 0.16f;
            float rumble = Mathf.Sin(t * Mathf.PI * 2f * 43f)
                + Mathf.Sin(t * Mathf.PI * 2f * 71f) * 0.45f
                + Mathf.Sin(t * Mathf.PI * 2f * 118f) * 0.2f;

            float noise = DeterministicSignedNoise(i);
            lowpass = Mathf.Lerp(lowpass, noise, 0.075f);
            float grain = lowpass * (0.18f + 0.1f * Mathf.Sin(t * Mathf.PI * 2f * 3.7f));
            float cracks = ResolveCrackBurst(t, 0.34f)
                + ResolveCrackBurst(t, 1.18f) * 0.7f
                + ResolveCrackBurst(t, 2.04f) * 0.55f;

            data[i] = Mathf.Clamp((rumble * 0.11f + grain + cracks * noise * 0.28f) * loopEnvelope, -0.85f, 0.85f);
        }

        proceduralEarthGrowthClip = AudioClip.Create("CherryGarden_ProceduralEarthGrowthLoop", samples, 1, sampleRate, false);
        proceduralEarthGrowthClip.SetData(data, 0);
        return proceduralEarthGrowthClip;
    }

    private static float ResolveCrackBurst(float time, float center)
    {
        float distance = Mathf.Abs(time - center);
        if (distance > 0.16f)
        {
            return 0f;
        }

        return Mathf.Pow(1f - distance / 0.16f, 2.6f);
    }

    private static float DeterministicSignedNoise(int index)
    {
        return Mathf.Repeat(Mathf.Sin(index * 12.9898f) * 43758.5453f, 1f) * 2f - 1f;
    }

    private IEnumerator CollapseOrb()
    {
        WonderfulWorld.Audio.WonderlandAudioOneShotPlayer.PlayAt("WW_SFX_CrystalCollapse", ResolveOrbPosition(), volumeScale: 1f, maxVoices: 4);
        float duration = Mathf.Max(0.01f, collapseDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float collapse = Mathf.SmoothStep(0f, 1f, t);
            ApplyCollapse(collapse);
            yield return null;
        }

        ApplyCollapse(1f);
        orbRoot.SetActive(false);
    }

    private void ApplyCollapse(float collapse)
    {
        float remaining = 1f - collapse;
        float tremor = Mathf.Sin(Time.time * shakeFrequency) * shakeAmplitude * remaining;
        Vector3 shake = new Vector3(
            Mathf.Sin(Time.time * shakeFrequency * 1.17f),
            Mathf.Cos(Time.time * shakeFrequency * 0.91f),
            Mathf.Sin(Time.time * shakeFrequency * 1.43f)) * tremor;
        float lightPulse = 1f + Mathf.Sin(Time.time * shakeFrequency * 0.42f) * 0.35f;
        float energySpike = Mathf.Lerp(collapseLightBoost, 0f, collapse);

        if (orbMaterial != null)
        {
            Color color = orbColor;
            color.a *= Mathf.Lerp(1f, 0.78f, collapse);
            SetMaterialColor(orbMaterial, color, emissionIntensity * (1f + energySpike) * lightPulse);
        }

        if (haloMaterial != null)
        {
            Color color = new Color(1f, 0.78f, 0.94f, Mathf.Lerp(0.42f, 0f, collapse));
            SetMaterialColor(haloMaterial, color, emissionIntensity * 2.85f * (1f + energySpike) * lightPulse);
        }

        if (orbLight != null)
        {
            orbLight.intensity = pointLightIntensity * (1f + energySpike) * lightPulse * remaining;
        }

        if (orbRoot != null)
        {
            float scale = Mathf.Lerp(1.08f, 0.02f, collapse);
            orbRoot.transform.position = baseOrbPosition + shake;
            orbRoot.transform.localScale = baseOrbScale * scale;
        }

        if (orbRenderers == null)
        {
            return;
        }

        for (int i = 0; i < orbRenderers.Length; i++)
        {
            if (orbRenderers[i] != null)
            {
                orbRenderers[i].enabled = collapse < 0.995f;
            }
        }
    }
}

public static class CherryGardenCrystalOrbBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (Object.FindFirstObjectByType<CherryGardenCrystalOrbTrigger>() != null)
        {
            return;
        }

        TreeGrowthController treeGrowth = Object.FindFirstObjectByType<TreeGrowthController>();
        FlowerVortexEffect flowerVortex = Object.FindFirstObjectByType<FlowerVortexEffect>();
        if (treeGrowth == null && flowerVortex == null)
        {
            return;
        }

        GameObject triggerRoot = new GameObject("CherryGarden_CrystalOrbTrigger");
        CherryGardenCrystalOrbTrigger trigger = triggerRoot.AddComponent<CherryGardenCrystalOrbTrigger>();
        trigger.Configure(treeGrowth, flowerVortex);
    }
}

public static class CrystalStoneOrbStyle
{
    private const string SharedTextureResourcePath = "CherryGarden/CrystalOrbTexture";

    private static Mesh sharedMesh;

    public static Texture2D LoadSharedTexture()
    {
        return Resources.Load<Texture2D>(SharedTextureResourcePath);
    }

    public static void ApplyMesh(MeshFilter meshFilter)
    {
        if (meshFilter == null)
        {
            return;
        }

        meshFilter.sharedMesh = GetSharedMesh();
    }

    public static Material CreateMaterial(
        Texture texture,
        Color baseColor,
        Color emissionColor,
        float emissionIntensity,
        bool additive,
        string materialName)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }

        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        Material material = new Material(shader);
        material.name = materialName;
        material.renderQueue = additive ? 3050 : 3000;
        material.SetOverrideTag("RenderType", "Transparent");

        if (texture != null)
        {
            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
            }

            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", texture);
            }

            if (material.HasProperty("_EmissionMap"))
            {
                material.SetTexture("_EmissionMap", texture);
            }
        }

        SetMaterialColor(material, baseColor, emissionColor, emissionIntensity);

        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 1f);
        }

        if (material.HasProperty("_Blend"))
        {
            material.SetFloat("_Blend", additive ? 1f : 0f);
        }

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", additive ? 0.35f : 0.94f);
        }

        if (material.HasProperty("_Metallic"))
        {
            material.SetFloat("_Metallic", 0f);
        }

        if (material.HasProperty("_SpecColor"))
        {
            material.SetColor("_SpecColor", new Color(1f, 0.9f, 0.96f, 1f));
        }

        if (material.HasProperty("_EnvironmentReflections"))
        {
            material.SetFloat("_EnvironmentReflections", 1f);
        }

        if (material.HasProperty("_SpecularHighlights"))
        {
            material.SetFloat("_SpecularHighlights", 1f);
        }

        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.EnableKeyword("_EMISSION");
        material.EnableKeyword("_EMISSION_MAP");
        return material;
    }

    public static void SetMaterialColor(Material material, Color baseColor, Color emissionColor, float emissionIntensity)
    {
        if (material == null)
        {
            return;
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", baseColor);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", baseColor);
        }

        if (material.HasProperty("_EmissionColor"))
        {
            material.SetColor("_EmissionColor", emissionColor * emissionIntensity);
        }
    }

    private static Mesh GetSharedMesh()
    {
        if (sharedMesh != null)
        {
            return sharedMesh;
        }

        float t = (1f + Mathf.Sqrt(5f)) * 0.5f;
        Vector3[] baseVertices =
        {
            new Vector3(-1f, t, 0f),
            new Vector3(1f, t, 0f),
            new Vector3(-1f, -t, 0f),
            new Vector3(1f, -t, 0f),
            new Vector3(0f, -1f, t),
            new Vector3(0f, 1f, t),
            new Vector3(0f, -1f, -t),
            new Vector3(0f, 1f, -t),
            new Vector3(t, 0f, -1f),
            new Vector3(t, 0f, 1f),
            new Vector3(-t, 0f, -1f),
            new Vector3(-t, 0f, 1f),
        };

        float[] radialOffsets =
        {
            1f, 0.94f, 1.03f, 0.98f,
            0.95f, 1.04f, 0.97f, 1.02f,
            0.96f, 1.05f, 0.93f, 1.01f,
        };

        for (int i = 0; i < baseVertices.Length; i++)
        {
            baseVertices[i] = Vector3.Scale(baseVertices[i].normalized * radialOffsets[i], new Vector3(0.96f, 1.06f, 1.01f));
        }

        float maxMagnitude = 0f;
        for (int i = 0; i < baseVertices.Length; i++)
        {
            maxMagnitude = Mathf.Max(maxMagnitude, baseVertices[i].magnitude);
        }

        for (int i = 0; i < baseVertices.Length; i++)
        {
            baseVertices[i] = baseVertices[i] / maxMagnitude * 0.5f;
        }

        int[] baseTriangles =
        {
            0, 11, 5,
            0, 5, 1,
            0, 1, 7,
            0, 7, 10,
            0, 10, 11,
            1, 5, 9,
            5, 11, 4,
            11, 10, 2,
            10, 7, 6,
            7, 1, 8,
            3, 9, 4,
            3, 4, 2,
            3, 2, 6,
            3, 6, 8,
            3, 8, 9,
            4, 9, 5,
            2, 4, 11,
            6, 2, 10,
            8, 6, 7,
            9, 8, 1,
        };

        Vector3[] facetedVertices = new Vector3[baseTriangles.Length];
        int[] facetedTriangles = new int[baseTriangles.Length];
        for (int i = 0; i < baseTriangles.Length; i++)
        {
            facetedVertices[i] = baseVertices[baseTriangles[i]];
            facetedTriangles[i] = i;
        }

        sharedMesh = new Mesh
        {
            name = "WW_CrystalStoneOrb_FacetedMesh",
            vertices = facetedVertices,
            triangles = facetedTriangles,
            bounds = new Bounds(Vector3.zero, Vector3.one)
        };
        sharedMesh.RecalculateNormals();
        return sharedMesh;
    }
}
