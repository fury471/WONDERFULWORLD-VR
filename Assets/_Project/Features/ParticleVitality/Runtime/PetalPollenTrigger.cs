using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PetalPollenTrigger : MonoBehaviour
{
    [SerializeField] private List<PetalPollenSource> sources = new List<PetalPollenSource>();
    [SerializeField] private bool autoDiscoverChildSources = true;
    [SerializeField] private bool randomizeSources = true;

    [Header("Crystal Visual")]
    [SerializeField] private bool useCrystalStoneVisual = true;
    [SerializeField] private Color crystalBaseColor = new Color(1f, 0.72f, 0.84f, 0.9f);
    [SerializeField] private Color crystalEmissionColor = new Color(1f, 0.36f, 0.66f, 1f);
    [SerializeField, Min(0f)] private float crystalEmissionIntensity = 0.62f;
    [SerializeField] private Color crystalHighlightColor = new Color(1f, 0.78f, 0.94f, 0.52f);
    [SerializeField, Min(0f)] private float crystalHighlightEmissionIntensity = 1.75f;
    [SerializeField, Min(1f)] private float crystalHighlightScale = 1.06f;

    private int nextSourceIndex;
    private Material crystalMaterial;
    private Material crystalHighlightMaterial;

    public PetalPollenSource PrimarySource
    {
        get
        {
            RefreshSourcesIfNeeded();
            return TryGetSource(0, out PetalPollenSource source) ? source : null;
        }
    }

    public Vector3 InteractionPosition => transform.position;

    private void Awake()
    {
        ApplyCrystalStoneVisual();
    }

    private void OnDestroy()
    {
        if (crystalMaterial != null)
        {
            Destroy(crystalMaterial);
        }

        if (crystalHighlightMaterial != null)
        {
            Destroy(crystalHighlightMaterial);
        }
    }

    public PetalPollenSource PickSource()
    {
        RefreshSourcesIfNeeded();
        RemoveMissingSources();

        if (sources.Count == 0)
        {
            return null;
        }

        if (randomizeSources)
        {
            return sources[Random.Range(0, sources.Count)];
        }

        PetalPollenSource source = sources[nextSourceIndex % sources.Count];
        nextSourceIndex++;
        return source;
    }

    public bool ContainsSource(PetalPollenSource source)
    {
        if (source == null)
        {
            return false;
        }

        RefreshSourcesIfNeeded();
        return sources.Contains(source);
    }

    public void SetInteractionFocus(float amount)
    {
        RefreshSourcesIfNeeded();
        for (int i = sources.Count - 1; i >= 0; i--)
        {
            PetalPollenSource source = sources[i];
            if (source == null)
            {
                sources.RemoveAt(i);
                continue;
            }

            source.SetInteractionFocus(amount);
        }
    }

    private void RefreshSourcesIfNeeded()
    {
        if (!autoDiscoverChildSources)
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
        sources.AddRange(GetComponentsInChildren<PetalPollenSource>(true));
    }

    private bool TryGetSource(int index, out PetalPollenSource source)
    {
        RemoveMissingSources();
        if (index < 0 || index >= sources.Count)
        {
            source = null;
            return false;
        }

        source = sources[index];
        return source != null;
    }

    private void RemoveMissingSources()
    {
        for (int i = sources.Count - 1; i >= 0; i--)
        {
            if (sources[i] == null)
            {
                sources.RemoveAt(i);
            }
        }
    }

    private void ApplyCrystalStoneVisual()
    {
        if (!useCrystalStoneVisual)
        {
            return;
        }

        MeshFilter meshFilter = GetComponent<MeshFilter>();
        Renderer renderer = GetComponent<Renderer>();
        if (meshFilter == null || renderer == null)
        {
            return;
        }

        Texture2D texture = CrystalStoneOrbStyle.LoadSharedTexture();
        CrystalStoneOrbStyle.ApplyMesh(meshFilter);
        crystalMaterial = CrystalStoneOrbStyle.CreateMaterial(
            texture,
            crystalBaseColor,
            crystalEmissionColor,
            crystalEmissionIntensity,
            false,
            "M_FlowerGarden_CrystalStoneOrb_Runtime");
        renderer.sharedMaterial = crystalMaterial;

        Transform highlight = transform.Find("FlowerGarden_CrystalHighlightVeins");
        if (highlight == null)
        {
            GameObject highlightObject = new GameObject("FlowerGarden_CrystalHighlightVeins");
            highlightObject.AddComponent<MeshFilter>();
            highlightObject.AddComponent<MeshRenderer>();
            highlightObject.name = "FlowerGarden_CrystalHighlightVeins";
            highlightObject.transform.SetParent(transform, false);
            highlight = highlightObject.transform;
        }

        highlight.localPosition = Vector3.zero;
        highlight.localRotation = Quaternion.identity;
        highlight.localScale = Vector3.one * crystalHighlightScale;
        CrystalStoneOrbStyle.ApplyMesh(highlight.GetComponent<MeshFilter>());
        crystalHighlightMaterial = CrystalStoneOrbStyle.CreateMaterial(
            texture,
            crystalHighlightColor,
            crystalEmissionColor,
            crystalHighlightEmissionIntensity,
            true,
            "M_FlowerGarden_CrystalHighlightVeins_Runtime");

        Renderer highlightRenderer = highlight.GetComponent<Renderer>();
        if (highlightRenderer != null)
        {
            highlightRenderer.sharedMaterial = crystalHighlightMaterial;
            highlightRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            highlightRenderer.receiveShadows = false;
        }
    }
}
