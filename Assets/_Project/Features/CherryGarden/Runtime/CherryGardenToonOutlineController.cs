using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public sealed class CherryGardenToonOutlineController : MonoBehaviour
{
    private const string OutlineSuffix = "_CherryToonOutline";

    [Header("Runtime")]
    [SerializeField] private bool enableToonOutline = true;
    [SerializeField] private bool buildOnStart = true;
    [SerializeField, Min(0)] private int maxOutlineRenderers = 420;
    [SerializeField, Min(0f)] private float maxVisibleDistance = 95f;

    [Header("Look")]
    [SerializeField] private Color outlineColor = Color.black;
    [SerializeField, Range(0.001f, 0.06f)] private float architectureOutlineWidth = 0.018f;
    [SerializeField, Range(0.001f, 0.06f)] private float vegetationOutlineWidth = 0.012f;
    [SerializeField, Range(0.001f, 0.06f)] private float propOutlineWidth = 0.015f;
    [SerializeField, Range(0f, 1f)] private float growthVisibilityThreshold = 0.03f;

    [Header("Filtering")]
    [SerializeField] private bool skipTransparentMaterials = true;
    [SerializeField] private bool skipVfxAndDecals = true;
    [SerializeField] private string[] excludedNameContains =
    {
        "Petal",
        "Particle",
        "VFX",
        "Decal",
        "Smoke",
        "Trail",
        "Light",
        "Glass",
        "Water",
        "WashiLight",
        "Cherry_shrub",
        "Bamboo_Seedling",
        "Japanese_Spurge",
        "Liriope",
        "Wisteria",
        "Leaves",
        "Vine"
    };

    private readonly List<OutlineBinding> outlineBindings = new();
    private readonly List<GameObject> outlineObjects = new();
    private readonly Dictionary<float, Material> outlineMaterials = new();
    private Material noDrawMaterial;
    private MaterialPropertyBlock growthProbeBlock;
    private Camera cachedCamera;
    private bool built;

    private void Start()
    {
        if (buildOnStart)
        {
            RebuildOutlines();
        }
    }

    private void OnEnable()
    {
        SetOutlineVisible(enableToonOutline);
    }

    private void OnDisable()
    {
        SetOutlineVisible(false);
    }

    private void OnDestroy()
    {
        ClearOutlines();
        foreach (Material material in outlineMaterials.Values)
        {
            DestroySafe(material);
        }

        outlineMaterials.Clear();
        DestroySafe(noDrawMaterial);
        noDrawMaterial = null;
    }

    private void OnValidate()
    {
        ApplyMaterialSettings();
    }

    private void LateUpdate()
    {
        if (!built)
        {
            return;
        }

        SetOutlineVisible(enableToonOutline && IsWithinVisibleDistance());
    }

    [ContextMenu("Rebuild Toon Outlines")]
    public void RebuildOutlines()
    {
        ClearOutlines();
        built = true;

        if (!enableToonOutline)
        {
            return;
        }

        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>(includeInactive: true);
        int created = 0;
        for (int i = 0; i < renderers.Length && created < maxOutlineRenderers; i++)
        {
            MeshRenderer sourceRenderer = renderers[i];
            if (!ShouldOutline(sourceRenderer))
            {
                continue;
            }

            if (CreateOutlineRenderer(sourceRenderer))
            {
                created++;
            }
        }
    }

    [ContextMenu("Clear Toon Outlines")]
    public void ClearOutlines()
    {
        for (int i = outlineObjects.Count - 1; i >= 0; i--)
        {
            DestroySafe(outlineObjects[i]);
        }

        outlineObjects.Clear();
        outlineBindings.Clear();

        Transform[] children = GetComponentsInChildren<Transform>(includeInactive: true);
        for (int i = children.Length - 1; i >= 0; i--)
        {
            Transform child = children[i];
            if (child != null && child != transform && child.name.EndsWith(OutlineSuffix, StringComparison.Ordinal))
            {
                DestroySafe(child.gameObject);
            }
        }
    }

    private bool ShouldOutline(MeshRenderer sourceRenderer)
    {
        if (sourceRenderer == null || sourceRenderer.GetComponent<MeshFilter>()?.sharedMesh == null)
        {
            return false;
        }

        if (sourceRenderer.gameObject.name.EndsWith(OutlineSuffix, StringComparison.Ordinal))
        {
            return false;
        }

        if (IsHeroCherryTree(sourceRenderer.transform))
        {
            return false;
        }

        if (skipVfxAndDecals && IsExcludedByName(sourceRenderer.transform))
        {
            return false;
        }

        if (skipTransparentMaterials && UsesTransparentMaterial(sourceRenderer))
        {
            return false;
        }

        if (!HasAnyOutlineableMaterial(sourceRenderer))
        {
            return false;
        }

        return true;
    }

    private bool CreateOutlineRenderer(MeshRenderer sourceRenderer)
    {
        MeshFilter sourceFilter = sourceRenderer.GetComponent<MeshFilter>();
        if (sourceFilter == null || sourceFilter.sharedMesh == null)
        {
            return false;
        }

        GameObject outline = new GameObject(sourceRenderer.gameObject.name + OutlineSuffix);
        outline.hideFlags = HideFlags.DontSave;
        outline.transform.SetParent(sourceRenderer.transform, false);
        outline.transform.localPosition = Vector3.zero;
        outline.transform.localRotation = Quaternion.identity;
        outline.transform.localScale = Vector3.one;

        MeshFilter outlineFilter = outline.AddComponent<MeshFilter>();
        outlineFilter.sharedMesh = sourceFilter.sharedMesh;

        MeshRenderer outlineRenderer = outline.AddComponent<MeshRenderer>();
        outlineRenderer.sharedMaterials = BuildMaterialArray(sourceRenderer.sharedMaterials, GetOutlineWidth(sourceRenderer.transform));
        outlineRenderer.shadowCastingMode = ShadowCastingMode.Off;
        outlineRenderer.receiveShadows = false;
        outlineRenderer.lightProbeUsage = LightProbeUsage.Off;
        outlineRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        outlineRenderer.allowOcclusionWhenDynamic = true;
        outlineRenderer.enabled = false;

        outlineObjects.Add(outline);
        outlineBindings.Add(new OutlineBinding(sourceRenderer, outlineRenderer));
        return true;
    }

    private Material[] BuildMaterialArray(Material[] sourceMaterials, float width)
    {
        Material outlineMaterial = GetOutlineMaterial(width);
        Material noDraw = GetNoDrawMaterial();
        int count = Mathf.Max(1, sourceMaterials.Length);
        Material[] materials = new Material[count];
        for (int i = 0; i < count; i++)
        {
            Material sourceMaterial = i < sourceMaterials.Length ? sourceMaterials[i] : null;
            materials[i] = IsOutlineableMaterial(sourceMaterial) ? outlineMaterial : noDraw;
        }

        return materials;
    }

    private Material GetOutlineMaterial(float width)
    {
        float key = Mathf.Round(width * 10000f) / 10000f;
        if (outlineMaterials.TryGetValue(key, out Material material) && material != null)
        {
            return material;
        }

        Shader shader = Shader.Find("Wonderland/CherryGarden/Toon Outline URP");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }

        material = new Material(shader)
        {
            name = $"Cherry Garden Toon Outline {key:0.####}",
            renderQueue = 1990
        };

        if (material.HasProperty("_OutlineColor"))
        {
            material.SetColor("_OutlineColor", outlineColor);
        }

        if (material.HasProperty("_OutlineWidth"))
        {
            material.SetFloat("_OutlineWidth", key);
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", outlineColor);
        }

        outlineMaterials[key] = material;
        ApplyMaterialSettings(material, key);
        return material;
    }

    private Material GetNoDrawMaterial()
    {
        if (noDrawMaterial != null)
        {
            return noDrawMaterial;
        }

        Shader shader = Shader.Find("Wonderland/CherryGarden/No Draw URP");
        if (shader == null)
        {
            shader = Shader.Find("Hidden/InternalErrorShader");
        }

        noDrawMaterial = new Material(shader)
        {
            name = "Cherry Garden Toon Outline No Draw",
            renderQueue = 1990
        };
        return noDrawMaterial;
    }

    private void ApplyMaterialSettings()
    {
        foreach (KeyValuePair<float, Material> pair in outlineMaterials)
        {
            ApplyMaterialSettings(pair.Value, pair.Key);
        }
    }

    private void ApplyMaterialSettings(Material material, float width)
    {
        if (material == null)
        {
            return;
        }

        if (material.HasProperty("_OutlineColor"))
        {
            material.SetColor("_OutlineColor", outlineColor);
        }

        if (material.HasProperty("_OutlineWidth"))
        {
            material.SetFloat("_OutlineWidth", width);
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", outlineColor);
        }
    }

    private float GetOutlineWidth(Transform source)
    {
        string path = GetHierarchyPath(source);
        if (path.IndexOf("Vegetation", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return vegetationOutlineWidth;
        }

        if (path.IndexOf("Props", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return propOutlineWidth;
        }

        return architectureOutlineWidth;
    }

    private bool UsesTransparentMaterial(Renderer sourceRenderer)
    {
        Material[] materials = sourceRenderer.sharedMaterials;
        for (int i = 0; i < materials.Length; i++)
        {
            Material material = materials[i];
            if (material == null)
            {
                continue;
            }

            if (material.renderQueue >= (int)RenderQueue.Transparent)
            {
                return true;
            }

            if (material.HasProperty("_Surface") && material.GetFloat("_Surface") > 0.5f)
            {
                return true;
            }

            string shaderName = material.shader != null ? material.shader.name : string.Empty;
            if (shaderName.IndexOf("Decal", StringComparison.OrdinalIgnoreCase) >= 0 ||
                shaderName.IndexOf("Particle", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasAnyOutlineableMaterial(Renderer sourceRenderer)
    {
        Material[] materials = sourceRenderer.sharedMaterials;
        for (int i = 0; i < materials.Length; i++)
        {
            if (IsOutlineableMaterial(materials[i]))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsOutlineableMaterial(Material material)
    {
        if (material == null)
        {
            return false;
        }

        string materialName = material.name;
        if (IsWoodyTreeMaterial(materialName))
        {
            return true;
        }

        string shaderName = material.shader != null ? material.shader.name : string.Empty;
        if (materialName.IndexOf("CherryTree", StringComparison.OrdinalIgnoreCase) >= 0 ||
            materialName.IndexOf("GardenPlants", StringComparison.OrdinalIgnoreCase) >= 0 ||
            materialName.IndexOf("Wisteria", StringComparison.OrdinalIgnoreCase) >= 0 ||
            shaderName.IndexOf("Vegetation", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return false;
        }

        if (material.HasProperty("_Growth") && material.HasProperty("_ScatteringIntensity"))
        {
            return false;
        }

        return true;
    }

    private static bool IsWoodyTreeMaterial(string materialName)
    {
        return materialName.IndexOf("CherryTreeBark", StringComparison.OrdinalIgnoreCase) >= 0 ||
               materialName.IndexOf("CherryTreeKnot", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool IsHeroCherryTree(Transform source)
    {
        string path = GetHierarchyPath(source);
        return path.IndexOf("HeroCherryTree", StringComparison.OrdinalIgnoreCase) >= 0 ||
               path.IndexOf("01_HeroCherryTree", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private bool IsExcludedByName(Transform source)
    {
        if (excludedNameContains == null || excludedNameContains.Length == 0)
        {
            return false;
        }

        string path = GetHierarchyPath(source);
        for (int i = 0; i < excludedNameContains.Length; i++)
        {
            string token = excludedNameContains[i];
            if (!string.IsNullOrWhiteSpace(token) &&
                path.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsWithinVisibleDistance()
    {
        if (maxVisibleDistance <= 0f)
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

        float sqrDistance = (cachedCamera.transform.position - transform.position).sqrMagnitude;
        return sqrDistance <= maxVisibleDistance * maxVisibleDistance;
    }

    private void SetOutlineVisible(bool visible)
    {
        for (int i = 0; i < outlineBindings.Count; i++)
        {
            OutlineBinding binding = outlineBindings[i];
            if (binding.OutlineRenderer == null)
            {
                continue;
            }

            bool sourceVisible = binding.SourceRenderer != null &&
                binding.SourceRenderer.enabled &&
                binding.SourceRenderer.gameObject.activeInHierarchy &&
                IsSourceGrowthVisible(binding.SourceRenderer);
            binding.OutlineRenderer.enabled = visible && sourceVisible;
        }
    }

    private bool IsSourceGrowthVisible(Renderer sourceRenderer)
    {
        Material[] materials = sourceRenderer.sharedMaterials;
        bool hasGrowthMaterial = false;
        for (int i = 0; i < materials.Length; i++)
        {
            Material material = materials[i];
            if (!IsOutlineableMaterial(material) || !TryGetGrowthValue(sourceRenderer, i, material, out float growth))
            {
                continue;
            }

            hasGrowthMaterial = true;
            if (growth > growthVisibilityThreshold)
            {
                return true;
            }
        }

        return !hasGrowthMaterial;
    }

    private bool TryGetGrowthValue(Renderer sourceRenderer, int materialIndex, Material material, out float growth)
    {
        growth = 0f;
        if (sourceRenderer == null || material == null || !material.HasProperty("_Growth"))
        {
            return false;
        }

        if (growthProbeBlock == null)
        {
            growthProbeBlock = new MaterialPropertyBlock();
        }

        growthProbeBlock.Clear();
        sourceRenderer.GetPropertyBlock(growthProbeBlock, materialIndex);
        growth = growthProbeBlock.isEmpty ? material.GetFloat("_Growth") : growthProbeBlock.GetFloat("_Growth");
        return true;
    }

    private static string GetHierarchyPath(Transform source)
    {
        if (source == null)
        {
            return string.Empty;
        }

        string path = source.name;
        Transform current = source.parent;
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }

    private static void DestroySafe(UnityEngine.Object target)
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

    private readonly struct OutlineBinding
    {
        public OutlineBinding(Renderer sourceRenderer, Renderer outlineRenderer)
        {
            SourceRenderer = sourceRenderer;
            OutlineRenderer = outlineRenderer;
        }

        public Renderer SourceRenderer { get; }
        public Renderer OutlineRenderer { get; }
    }
}
