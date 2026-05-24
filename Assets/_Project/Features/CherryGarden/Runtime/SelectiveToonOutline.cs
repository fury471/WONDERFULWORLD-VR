using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class SelectiveToonOutline : MonoBehaviour
{
    private const string OutlineSuffix = "_SelectiveToonOutline";

    [SerializeField] private bool rebuildOnEnable = true;
    [SerializeField, Min(0)] private int maxOutlineRenderers = 256;
    [SerializeField] private Material outlineMaterial;
    [SerializeField] private string[] includedNameContains = { "Outlined_" };
    [SerializeField] private string[] excludedNameContains =
    {
        "Unoutlined",
        "NoOutline",
        "Leaf",
        "Leaves",
        "Foliage",
        "Flower",
        "Blossom",
        "Petal",
        "Grass",
        "VFX",
        "Particle",
        "Smoke",
        "Glass",
        "Water"
    };

    private readonly List<GameObject> outlineObjects = new();

    private void OnEnable()
    {
        if (rebuildOnEnable)
        {
            RebuildOutlines();
        }
    }

    private void OnValidate()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.EditorApplication.delayCall -= RebuildOutlinesIfValid;
            UnityEditor.EditorApplication.delayCall += RebuildOutlinesIfValid;
            return;
        }
#endif

        RebuildOutlines();
    }

    private void OnDisable()
    {
        ClearOutlines();
    }

    [ContextMenu("Rebuild Toon Outlines")]
    public void RebuildOutlines()
    {
        ClearOutlines();

        if (outlineMaterial == null)
        {
            return;
        }

        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>(includeInactive: true);
        int created = 0;
        for (int i = 0; i < renderers.Length && created < maxOutlineRenderers; i++)
        {
            MeshRenderer source = renderers[i];
            if (ShouldOutline(source) && CreateOutline(source))
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

    private bool ShouldOutline(MeshRenderer source)
    {
        if (source == null || source.gameObject.name.EndsWith(OutlineSuffix, StringComparison.Ordinal))
        {
            return false;
        }

        MeshFilter filter = source.GetComponent<MeshFilter>();
        if (filter == null || filter.sharedMesh == null)
        {
            return false;
        }

        string path = GetHierarchyPath(source.transform);
        if (ContainsAny(path, excludedNameContains))
        {
            return false;
        }

        if (includedNameContains != null && includedNameContains.Length > 0 && !ContainsAny(path, includedNameContains))
        {
            return false;
        }

        Material[] materials = source.sharedMaterials;
        for (int i = 0; i < materials.Length; i++)
        {
            string materialName = materials[i] != null ? materials[i].name : string.Empty;
            if (ContainsAny(materialName, excludedNameContains))
            {
                return false;
            }
        }

        return true;
    }

    private bool CreateOutline(MeshRenderer source)
    {
        MeshFilter sourceFilter = source.GetComponent<MeshFilter>();
        if (sourceFilter == null || sourceFilter.sharedMesh == null)
        {
            return false;
        }

        GameObject outline = new(source.gameObject.name + OutlineSuffix)
        {
            hideFlags = HideFlags.DontSave
        };
        outline.transform.SetParent(source.transform, false);
        outline.transform.localPosition = Vector3.zero;
        outline.transform.localRotation = Quaternion.identity;
        outline.transform.localScale = Vector3.one;

        MeshFilter outlineFilter = outline.AddComponent<MeshFilter>();
        outlineFilter.sharedMesh = sourceFilter.sharedMesh;

        MeshRenderer outlineRenderer = outline.AddComponent<MeshRenderer>();
        Material[] outlineMaterials = new Material[Mathf.Max(1, source.sharedMaterials.Length)];
        for (int i = 0; i < outlineMaterials.Length; i++)
        {
            outlineMaterials[i] = outlineMaterial;
        }

        outlineRenderer.sharedMaterials = outlineMaterials;
        outlineRenderer.shadowCastingMode = ShadowCastingMode.Off;
        outlineRenderer.receiveShadows = false;
        outlineRenderer.lightProbeUsage = LightProbeUsage.Off;
        outlineRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        outlineRenderer.allowOcclusionWhenDynamic = true;

        outlineObjects.Add(outline);
        return true;
    }

    private static bool ContainsAny(string source, string[] tokens)
    {
        if (string.IsNullOrEmpty(source) || tokens == null)
        {
            return false;
        }

        for (int i = 0; i < tokens.Length; i++)
        {
            string token = tokens[i];
            if (!string.IsNullOrWhiteSpace(token) &&
                source.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    private static string GetHierarchyPath(Transform source)
    {
        string path = source != null ? source.name : string.Empty;
        Transform current = source != null ? source.parent : null;
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }

#if UNITY_EDITOR
    private void RebuildOutlinesIfValid()
    {
        if (this != null && isActiveAndEnabled)
        {
            RebuildOutlines();
        }
    }
#endif

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
}
