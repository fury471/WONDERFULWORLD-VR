using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class LotusGlowController : MonoBehaviour 
{
    [Header("Identity")]
    public int noteId; // A=0, B=1, C=2...

    [Header("Visual References")]
    [SerializeField] private MeshRenderer glowRenderer; 

    [Header("Bubble Song Hint")]
    [SerializeField] private GameObject bubbleHintPrefab;
    [SerializeField] private GameObject bubbleExplosionPrefab;
    [SerializeField] private Vector3 bubbleHintWorldOffset = new Vector3(0f, 2.35f, 0f);
    [SerializeField, Min(0.01f)] private float bubbleHintScale = 1.2f;
    [SerializeField, Min(0.01f)] private float bubbleExplosionScale = 1.8f;
    [SerializeField, Min(0.01f)] private float bubbleExplosionLifetime = 1.1f;
    [SerializeField] private bool useBubbleHint = true;
    [Tooltip("Keeps the score hint visible even if the imported looping bubble prefab is not compatible with this render pipeline.")]
    [SerializeField] private bool forceProceduralBubbleHint = true;
    [SerializeField] private Color proceduralBubbleColor = new Color(0.5f, 0.94f, 1f, 0.28f);
    [SerializeField] private Color proceduralBubbleRimColor = new Color(0.9f, 1f, 1f, 0.78f);
    [SerializeField] private Color proceduralBubbleParticleColor = new Color(0.65f, 0.96f, 1f, 0.72f);
    [SerializeField, Min(0f)] private float bubbleBobAmplitude = 0.08f;
    [SerializeField, Min(0.01f)] private float bubbleBobSpeed = 1.8f;
    [SerializeField, Min(0.01f)] private float bubbleTransferSeconds = 0.46f;
    [SerializeField, Min(0f)] private float bubbleTransferArcHeight = 1.15f;
    [SerializeField] private float bubbleTransferSideCurve = 0.52f;
    [SerializeField, Min(0.01f)] private float sameNoteHopSeconds = 0.34f;
    [SerializeField, Min(0f)] private float sameNoteHopHeight = 0.62f;

    [Header("Shader Graph Controls (Optional)")]
    [Tooltip("If your hint shader graph exposes a PulseSpeed float, set its reference name here.")]
    [SerializeField] private string pulseSpeedProperty = "_PulseSpeed";
    [Tooltip("If your hint shader graph exposes a PulseAmount float, set its reference name here.")]
    [SerializeField] private string pulseAmountProperty = "_PulseAmount";
    [Tooltip("If your hint shader graph exposes a GlowIntensity float, set its reference name here.")]
    [SerializeField] private string glowIntensityProperty = "_GlowIntensity";

    [Header("Defaults Applied On Enable")]
    [Tooltip("If enabled, writes the values below into the material when the hint is enabled. Leave off to keep material values as-authored.")]
    [SerializeField] private bool applyShaderDefaultsOnEnable = false;
    [Tooltip("Optional: written to shader graph PulseSpeed when Apply Shader Defaults On Enable is true.")]
    public float pulseSpeed = 2.5f;
    [Tooltip("Optional: written to shader graph PulseAmount when Apply Shader Defaults On Enable is true.")]
    public float pulseAmount = 0.15f;
    [Tooltip("Optional: written to shader graph GlowIntensity when Apply Shader Defaults On Enable is true.")]
    public float glowIntensity = 1.0f;

    private Material instanceMaterial;
    private GameObject activeBubbleHint;
    private Coroutine bubbleHintRoutine;
    private Material proceduralBubbleMaterial;
    private Material proceduralBubbleRimMaterial;
    private Material proceduralBubbleParticleMaterial;

    void Awake() 
    {
        if (glowRenderer != null) 
        {
            // Create an instance so each pad can have independent shader parameters.
            instanceMaterial = glowRenderer.material;

            // Ensure the indicator is hidden at the start
            glowRenderer.gameObject.SetActive(false); 
        }
    }

    /// <summary>
    /// Public method used by LotusSongManager to turn the hint on or off.
    /// </summary>
    public void SetGlowActive(bool active)
    {
        SetGlowActive(active, !active);
    }

    public void SetGlowActive(bool active, bool explodeOnDisable)
    {
        SetGlowActive(active, explodeOnDisable, null);
    }

    public void SetGlowActiveFrom(Vector3 startWorldPosition)
    {
        SetGlowActive(true, false, startWorldPosition);
    }

    public void PlaySameNoteHop()
    {
        if (!useBubbleHint)
            return;

        if (activeBubbleHint == null)
        {
            ShowBubbleHint(null);
        }

        if (activeBubbleHint == null)
            return;

        if (bubbleHintRoutine != null)
        {
            StopCoroutine(bubbleHintRoutine);
            bubbleHintRoutine = null;
        }

        bubbleHintRoutine = StartCoroutine(HopBubbleHint(activeBubbleHint.transform));
    }

    public Vector3 CurrentBubbleWorldPosition => activeBubbleHint != null ? activeBubbleHint.transform.position : ResolveBubblePosition();

    private void SetGlowActive(bool active, bool explodeOnDisable, Vector3? transferStartWorldPosition)
    {
        if (glowRenderer != null)
        {
            glowRenderer.gameObject.SetActive(active);
            if (active && applyShaderDefaultsOnEnable)
            {
                ApplyShaderDefaults();
            }
        }

        if (active)
        {
            ShowBubbleHint(transferStartWorldPosition);
        }
        else
        {
            HideBubbleHint(explodeOnDisable);
        }
    }

    private void ShowBubbleHint(Vector3? transferStartWorldPosition)
    {
        if (!useBubbleHint || activeBubbleHint != null)
        {
            return;
        }

        if (forceProceduralBubbleHint || bubbleHintPrefab == null)
        {
            activeBubbleHint = CreateProceduralBubbleHint();
        }
        else
        {
            activeBubbleHint = Instantiate(bubbleHintPrefab, ResolveBubblePosition(), Quaternion.identity, transform);
            activeBubbleHint.transform.localScale = Vector3.one * bubbleHintScale;
            PlayAllParticles(activeBubbleHint);
        }

        if (activeBubbleHint != null)
        {
            if (transferStartWorldPosition.HasValue)
            {
                bubbleHintRoutine = StartCoroutine(TransferBubbleHint(activeBubbleHint.transform, transferStartWorldPosition.Value));
            }
            else
            {
                bubbleHintRoutine = StartCoroutine(AnimateBubbleHint(activeBubbleHint.transform));
            }
        }
    }

    private void HideBubbleHint(bool explode)
    {
        bool hadBubble = activeBubbleHint != null;
        Vector3 bubblePosition = activeBubbleHint != null ? activeBubbleHint.transform.position : ResolveBubblePosition();
        if (bubbleHintRoutine != null)
        {
            StopCoroutine(bubbleHintRoutine);
            bubbleHintRoutine = null;
        }

        if (activeBubbleHint != null)
        {
            Destroy(activeBubbleHint);
            activeBubbleHint = null;
        }

        if (explode && hadBubble && useBubbleHint && bubbleExplosionPrefab != null)
        {
            GameObject explosion = Instantiate(bubbleExplosionPrefab, bubblePosition, Quaternion.identity);
            explosion.transform.localScale = Vector3.one * bubbleExplosionScale;
            PlayAllParticles(explosion);
            Destroy(explosion, bubbleExplosionLifetime);
        }
    }

    private GameObject CreateProceduralBubbleHint()
    {
        GameObject root = new GameObject("Lotus_Bubble_Hint_Procedural");
        root.transform.SetParent(transform, false);
        root.transform.localPosition = bubbleHintWorldOffset;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "Bubble Shell";
        sphere.transform.SetParent(root.transform, false);
        sphere.transform.localScale = Vector3.one * bubbleHintScale;
        Collider sphereCollider = sphere.GetComponent<Collider>();
        if (sphereCollider != null)
        {
            Destroy(sphereCollider);
        }

        MeshRenderer sphereRenderer = sphere.GetComponent<MeshRenderer>();
        if (sphereRenderer != null)
        {
            sphereRenderer.sharedMaterial = GetProceduralBubbleMaterial();
            sphereRenderer.shadowCastingMode = ShadowCastingMode.Off;
            sphereRenderer.receiveShadows = false;
        }

        CreateBubbleRing(root.transform, "Horizontal Rim", Quaternion.identity, bubbleHintScale * 0.52f);
        CreateBubbleRing(root.transform, "Vertical Rim", Quaternion.Euler(90f, 0f, 0f), bubbleHintScale * 0.5f);
        CreateBubbleParticleMist(root.transform);

        return root;
    }

    private void CreateBubbleRing(Transform parent, string name, Quaternion localRotation, float radius)
    {
        GameObject ring = new GameObject(name);
        ring.transform.SetParent(parent, false);
        ring.transform.localRotation = localRotation;

        LineRenderer line = ring.AddComponent<LineRenderer>();
        line.sharedMaterial = GetProceduralBubbleRimMaterial();
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = 40;
        line.widthMultiplier = Mathf.Max(0.012f, bubbleHintScale * 0.018f);
        line.numCornerVertices = 3;
        line.numCapVertices = 3;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;

        for (int i = 0; i < line.positionCount; i++)
        {
            float angle = i / (float)line.positionCount * Mathf.PI * 2f;
            line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
        }
    }

    private void CreateBubbleParticleMist(Transform parent)
    {
        GameObject particlesObject = new GameObject("Bubble Motions");
        particlesObject.transform.SetParent(parent, false);

        ParticleSystem particles = particlesObject.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.playOnAwake = false;
        main.duration = 1f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.75f, 1.25f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
        main.startSize = new ParticleSystem.MinMaxCurve(bubbleHintScale * 0.035f, bubbleHintScale * 0.09f);
        main.startColor = new ParticleSystem.MinMaxGradient(proceduralBubbleParticleColor);
        main.maxParticles = 18;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 9f;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = bubbleHintScale * 0.24f;

        ParticleSystemRenderer renderer = particlesObject.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = GetProceduralBubbleParticleMaterial();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        particles.Play(true);
    }

    private IEnumerator AnimateBubbleHint(Transform bubbleTransform)
    {
        float phase = Random.value * Mathf.PI * 2f;
        Vector3 baseLocalPosition = bubbleTransform.localPosition;
        while (bubbleTransform != null)
        {
            float pulse = Mathf.Sin(Time.time * bubbleBobSpeed + phase);
            bubbleTransform.localPosition = baseLocalPosition + Vector3.up * (pulse * bubbleBobAmplitude);
            bubbleTransform.localScale = Vector3.one * (1f + pulse * 0.035f);
            yield return null;
        }
    }

    private IEnumerator TransferBubbleHint(Transform bubbleTransform, Vector3 startWorldPosition)
    {
        Vector3 endLocalPosition = bubbleHintWorldOffset;
        Vector3 startLocalPosition = transform.InverseTransformPoint(startWorldPosition);
        Vector3 startWorld = transform.TransformPoint(startLocalPosition);
        Vector3 endWorld = transform.TransformPoint(endLocalPosition);
        Vector3 travel = endWorld - startWorld;
        Vector3 travelDirection = travel.sqrMagnitude > 0.0001f ? travel.normalized : transform.forward;
        Vector3 side = Vector3.Cross(Vector3.up, travelDirection);
        if (side.sqrMagnitude < 0.0001f)
        {
            side = transform.right;
        }

        side.Normalize();
        Vector3 controlAWorld = Vector3.Lerp(startWorld, endWorld, 0.34f) + Vector3.up * (bubbleTransferArcHeight * 0.55f) + side * bubbleTransferSideCurve;
        Vector3 controlBWorld = Vector3.Lerp(startWorld, endWorld, 0.78f) + Vector3.up * bubbleTransferArcHeight - side * (bubbleTransferSideCurve * 0.45f);
        float transferSeconds = Mathf.Max(0.05f, bubbleTransferSeconds);
        float elapsed = 0f;

        while (bubbleTransform != null && elapsed < transferSeconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / transferSeconds);
            float eased = t * t * (3f - 2f * t);
            Vector3 worldPosition = CubicBezier(startWorld, controlAWorld, controlBWorld, endWorld, eased);
            bubbleTransform.localPosition = transform.InverseTransformPoint(worldPosition);
            bubbleTransform.localScale = Vector3.one * Mathf.Lerp(0.62f, 1f, Mathf.Sin(eased * Mathf.PI * 0.5f));
            yield return null;
        }

        if (bubbleTransform != null)
        {
            bubbleTransform.localPosition = endLocalPosition;
            bubbleTransform.localScale = Vector3.one;
            bubbleHintRoutine = StartCoroutine(AnimateBubbleHint(bubbleTransform));
        }
    }

    private IEnumerator HopBubbleHint(Transform bubbleTransform)
    {
        Vector3 baseLocalPosition = bubbleHintWorldOffset;
        float hopSeconds = Mathf.Max(0.05f, sameNoteHopSeconds);
        float elapsed = 0f;

        while (bubbleTransform != null && elapsed < hopSeconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / hopSeconds);
            float lift = Mathf.Sin(t * Mathf.PI) * sameNoteHopHeight;
            float squash = Mathf.Sin(t * Mathf.PI);
            bubbleTransform.localPosition = baseLocalPosition + Vector3.up * lift;
            bubbleTransform.localScale = Vector3.one * (1f + squash * 0.16f);
            yield return null;
        }

        if (bubbleTransform != null)
        {
            bubbleTransform.localPosition = baseLocalPosition;
            bubbleTransform.localScale = Vector3.one;
            bubbleHintRoutine = StartCoroutine(AnimateBubbleHint(bubbleTransform));
        }
    }

    private void PlayAllParticles(GameObject root)
    {
        if (root == null)
            return;

        ParticleSystem[] systems = root.GetComponentsInChildren<ParticleSystem>(true);
        foreach (ParticleSystem system in systems)
        {
            system.Clear(true);
            system.Play(true);
        }
    }

    private Material GetProceduralBubbleMaterial()
    {
        if (proceduralBubbleMaterial == null)
        {
            proceduralBubbleMaterial = CreateTransparentMaterial("Lotus Procedural Bubble Shell", proceduralBubbleColor, false);
        }

        return proceduralBubbleMaterial;
    }

    private Material GetProceduralBubbleRimMaterial()
    {
        if (proceduralBubbleRimMaterial == null)
        {
            proceduralBubbleRimMaterial = CreateTransparentMaterial("Lotus Procedural Bubble Rim", proceduralBubbleRimColor, true);
        }

        return proceduralBubbleRimMaterial;
    }

    private Material GetProceduralBubbleParticleMaterial()
    {
        if (proceduralBubbleParticleMaterial == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
                shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");

            proceduralBubbleParticleMaterial = new Material(shader);
            proceduralBubbleParticleMaterial.name = "Lotus Procedural Bubble Particles";
            SetMaterialColor(proceduralBubbleParticleMaterial, proceduralBubbleParticleColor);
            ConfigureTransparentMaterial(proceduralBubbleParticleMaterial, true);
        }

        return proceduralBubbleParticleMaterial;
    }

    private Material CreateTransparentMaterial(string materialName, Color color, bool additive)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        Material material = new Material(shader) { name = materialName };
        SetMaterialColor(material, color);
        ConfigureTransparentMaterial(material, additive);
        return material;
    }

    private void SetMaterialColor(Material material, Color color)
    {
        if (material == null)
            return;

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
    }

    private void ConfigureTransparentMaterial(Material material, bool additive)
    {
        if (material == null)
            return;

        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", additive ? 1f : 0f);
        material.SetFloat("_AlphaClip", 0f);
        material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", additive ? (int)BlendMode.One : (int)BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = (int)RenderQueue.Transparent;
    }

    private static Vector3 CubicBezier(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t)
    {
        float inverse = 1f - t;
        return inverse * inverse * inverse * a
            + 3f * inverse * inverse * t * b
            + 3f * inverse * t * t * c
            + t * t * t * d;
    }

    private void OnDestroy()
    {
        if (bubbleHintRoutine != null)
        {
            StopCoroutine(bubbleHintRoutine);
            bubbleHintRoutine = null;
        }

        if (proceduralBubbleMaterial != null)
            Destroy(proceduralBubbleMaterial);
        if (proceduralBubbleRimMaterial != null)
            Destroy(proceduralBubbleRimMaterial);
        if (proceduralBubbleParticleMaterial != null)
            Destroy(proceduralBubbleParticleMaterial);
    }

    private Vector3 ResolveBubblePosition()
    {
        return transform.position + bubbleHintWorldOffset;
    }

    private void ApplyShaderDefaults()
    {
        if (instanceMaterial == null)
            return;

        if (!string.IsNullOrWhiteSpace(pulseSpeedProperty) && instanceMaterial.HasFloat(pulseSpeedProperty))
            instanceMaterial.SetFloat(pulseSpeedProperty, pulseSpeed);

        if (!string.IsNullOrWhiteSpace(pulseAmountProperty) && instanceMaterial.HasFloat(pulseAmountProperty))
            instanceMaterial.SetFloat(pulseAmountProperty, pulseAmount);

        if (!string.IsNullOrWhiteSpace(glowIntensityProperty) && instanceMaterial.HasFloat(glowIntensityProperty))
            instanceMaterial.SetFloat(glowIntensityProperty, glowIntensity);
    }
}
