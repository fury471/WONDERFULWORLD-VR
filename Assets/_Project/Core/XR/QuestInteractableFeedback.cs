using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

[DisallowMultipleComponent]
public sealed class QuestInteractableFeedback : MonoBehaviour
{
    [Header("Outline")]
    [SerializeField] private bool buildOutlineOnEnable = true;
    [SerializeField] private Renderer[] targetRenderers;
    [SerializeField] private Collider[] targetColliders;
    [SerializeField] private Color outlineColor = new Color(0.55f, 0.9f, 1f, 0.62f);
    [SerializeField, Range(0.002f, 0.08f)] private float outlineInflation = 0.018f;
    [SerializeField, Range(0f, 1f)] private float idleAlpha = 0f;
    [SerializeField, Range(0f, 1f)] private float hoverAlpha = 0.58f;
    [SerializeField] private bool includeInactiveRenderers;

    [Header("Haptics")]
    [SerializeField, Range(0f, 1f)] private float hoverHapticAmplitude = 0.1f;
    [SerializeField, Min(0f)] private float hoverHapticDuration = 0.018f;
    [SerializeField, Min(0f)] private float sameTargetHoverCooldown = 0.55f;
    [SerializeField, Range(0f, 1f)] private float selectHapticAmplitude = 0.14f;
    [SerializeField, Min(0f)] private float selectHapticDuration = 0.025f;
    [SerializeField, Range(0f, 1f)] private float impactHapticAmplitude = 0.18f;
    [SerializeField, Min(0f)] private float impactHapticDuration = 0.03f;

    private readonly List<GameObject> outlineObjects = new List<GameObject>(8);
    private readonly List<LineRenderer> fallbackRings = new List<LineRenderer>(3);
    private Material outlineMaterial;
    private bool built;
    private bool hovered;
    private bool interactable = true;
    private float lastHoverHapticTime = -999f;

    public bool IsInteractable => interactable;

    private void OnEnable()
    {
        if (buildOutlineOnEnable)
        {
            EnsureBuilt();
        }

        ApplyVisualState(true);
    }

    private void OnDisable()
    {
        hovered = false;
        ApplyVisualState(false);
    }

    private void Update()
    {
        if (hovered && interactable)
        {
            ApplyVisualState(true);
        }
    }

    private void OnDestroy()
    {
        if (outlineMaterial != null)
        {
            Destroy(outlineMaterial);
        }
    }

    public void Configure(Color color, float inflation = -1f)
    {
        outlineColor = color;
        if (inflation >= 0f)
        {
            outlineInflation = inflation;
        }

        if (outlineMaterial != null)
        {
            ApplyMaterialColor(CurrentAlpha());
        }
    }

    public void SetInteractable(bool value)
    {
        interactable = value;
        if (!interactable)
        {
            hovered = false;
        }

        ApplyVisualState(true);
    }

    public void SetHovered(bool value, HapticImpulsePlayer hapticPlayer = null, bool pulseOnEnter = true)
    {
        EnsureBuilt();

        bool entered = value && !hovered;
        hovered = value && interactable;
        ApplyVisualState(true);

        if (!entered || !hovered || !pulseOnEnter)
        {
            return;
        }

        if (Time.unscaledTime - lastHoverHapticTime < sameTargetHoverCooldown)
        {
            return;
        }

        QuestInteractionUtils.SendHaptic(hapticPlayer, hoverHapticAmplitude, hoverHapticDuration);
        lastHoverHapticTime = Time.unscaledTime;
    }

    public void PulseSelect(HapticImpulsePlayer hapticPlayer)
    {
        QuestInteractionUtils.SendHaptic(hapticPlayer, selectHapticAmplitude, selectHapticDuration);
    }

    public void PulseImpact(HapticImpulsePlayer hapticPlayer)
    {
        QuestInteractionUtils.SendHaptic(hapticPlayer, impactHapticAmplitude, impactHapticDuration);
    }

    public bool ContainsCollider(Collider candidate)
    {
        if (candidate == null)
        {
            return false;
        }

        EnsureTargets();
        for (int i = 0; i < targetColliders.Length; i++)
        {
            Collider target = targetColliders[i];
            if (target == candidate)
            {
                return true;
            }
        }

        return candidate.transform.IsChildOf(transform);
    }

    private void EnsureBuilt()
    {
        if (built)
        {
            return;
        }

        built = true;
        EnsureTargets();
        EnsureMaterial();
        BuildMeshOutlines();

        if (outlineObjects.Count == 0)
        {
            BuildFallbackBoundsRings();
        }

        ApplyVisualState(false);
    }

    private void EnsureTargets()
    {
        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            targetRenderers = GetComponentsInChildren<Renderer>(includeInactiveRenderers);
        }

        if (targetColliders == null || targetColliders.Length == 0)
        {
            targetColliders = GetComponentsInChildren<Collider>(true);
        }
    }

    private void EnsureMaterial()
    {
        if (outlineMaterial != null)
        {
            return;
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

        outlineMaterial = new Material(shader);
        outlineMaterial.name = $"{name}_QuestHoverOutline";
        outlineMaterial.renderQueue = 3100;

        if (outlineMaterial.HasProperty("_Surface"))
        {
            outlineMaterial.SetFloat("_Surface", 1f);
        }

        if (outlineMaterial.HasProperty("_Blend"))
        {
            outlineMaterial.SetFloat("_Blend", 1f);
        }

        if (outlineMaterial.HasProperty("_Cull"))
        {
            outlineMaterial.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Front);
        }

        outlineMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        outlineMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
        outlineMaterial.SetFloat("_ZWrite", 0f);
        outlineMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        outlineMaterial.EnableKeyword("_ALPHABLEND_ON");
        ApplyMaterialColor(0f);
    }

    private void BuildMeshOutlines()
    {
        if (targetRenderers == null)
        {
            return;
        }

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            Renderer renderer = targetRenderers[i];
            if (renderer == null || renderer.GetComponentInParent<QuestInteractableFeedback>() != this && renderer.transform.IsChildOf(transform) && renderer.name.Contains("QuestHoverOutline"))
            {
                continue;
            }

            Mesh mesh = null;
            MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                mesh = meshFilter.sharedMesh;
            }
            else if (renderer is SkinnedMeshRenderer)
            {
                continue;
            }

            if (mesh == null)
            {
                continue;
            }

            GameObject outline = new GameObject(renderer.name + "_QuestHoverOutline");
            outline.transform.SetParent(renderer.transform, false);
            outline.transform.localPosition = Vector3.zero;
            outline.transform.localRotation = Quaternion.identity;
            outline.transform.localScale = Vector3.one * (1f + outlineInflation);
            outline.layer = renderer.gameObject.layer;

            MeshFilter outlineFilter = outline.AddComponent<MeshFilter>();
            outlineFilter.sharedMesh = mesh;
            MeshRenderer outlineRenderer = outline.AddComponent<MeshRenderer>();
            outlineRenderer.sharedMaterial = outlineMaterial;
            outlineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            outlineRenderer.receiveShadows = false;
            outlineObjects.Add(outline);
        }
    }

    private void BuildFallbackBoundsRings()
    {
        Bounds bounds = ResolveBounds();
        float radius = Mathf.Max(0.15f, bounds.extents.magnitude * 0.65f);
        Vector3 center = transform.InverseTransformPoint(bounds.center);

        for (int i = 0; i < 3; i++)
        {
            GameObject ringObject = new GameObject($"QuestHoverRing_{i + 1}");
            ringObject.transform.SetParent(transform, false);
            ringObject.transform.localPosition = center;
            ringObject.transform.localRotation = Quaternion.identity;

            LineRenderer ring = ringObject.AddComponent<LineRenderer>();
            ring.sharedMaterial = outlineMaterial;
            ring.useWorldSpace = false;
            ring.loop = true;
            ring.positionCount = 72;
            ring.widthMultiplier = Mathf.Max(0.006f, radius * 0.012f);
            ring.numCapVertices = 3;

            Quaternion plane = i == 0
                ? Quaternion.identity
                : (i == 1 ? Quaternion.Euler(90f, 0f, 0f) : Quaternion.Euler(0f, 90f, 0f));

            for (int p = 0; p < ring.positionCount; p++)
            {
                float angle = p / (float)ring.positionCount * Mathf.PI * 2f;
                Vector3 point = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                ring.SetPosition(p, plane * point);
            }

            fallbackRings.Add(ring);
            outlineObjects.Add(ringObject);
        }
    }

    private Bounds ResolveBounds()
    {
        EnsureTargets();
        bool hasBounds = false;
        Bounds bounds = new Bounds(transform.position, Vector3.one * 0.35f);

        if (targetRenderers != null)
        {
            for (int i = 0; i < targetRenderers.Length; i++)
            {
                Renderer renderer = targetRenderers[i];
                if (renderer == null)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }
        }

        if (!hasBounds && targetColliders != null)
        {
            for (int i = 0; i < targetColliders.Length; i++)
            {
                Collider collider = targetColliders[i];
                if (collider == null)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = collider.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(collider.bounds);
                }
            }
        }

        return bounds;
    }

    private void ApplyVisualState(bool animate)
    {
        if (outlineMaterial == null)
        {
            return;
        }

        float alpha = CurrentAlpha();
        if (animate && hovered && interactable)
        {
            alpha *= 0.82f + Mathf.Sin(Time.unscaledTime * 5.5f) * 0.18f;
        }

        ApplyMaterialColor(alpha);
        bool visible = alpha > 0.001f;
        for (int i = 0; i < outlineObjects.Count; i++)
        {
            GameObject outline = outlineObjects[i];
            if (outline != null && outline.activeSelf != visible)
            {
                outline.SetActive(visible);
            }
        }
    }

    private float CurrentAlpha()
    {
        return hovered && interactable ? hoverAlpha : idleAlpha;
    }

    private void ApplyMaterialColor(float alpha)
    {
        Color color = outlineColor;
        color.a = Mathf.Clamp01(alpha);

        if (outlineMaterial.HasProperty("_BaseColor"))
        {
            outlineMaterial.SetColor("_BaseColor", color);
        }

        if (outlineMaterial.HasProperty("_Color"))
        {
            outlineMaterial.SetColor("_Color", color);
        }

        if (outlineMaterial.HasProperty("_EmissionColor"))
        {
            outlineMaterial.SetColor("_EmissionColor", color * Mathf.Lerp(1f, 2.4f, Mathf.Clamp01(alpha)));
        }
    }
}
