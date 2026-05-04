using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Randomizes mushroom/plant colors and drives a color gradient during GrowthPlant growth (0..1).
/// 
/// Attach to the same GameObject as GrowthPlant (or a child). It will:
/// - pick a random "common" color preset at startup
/// - apply a growth-time gradient so colors shift as the plant grows
/// 
/// Uses MaterialPropertyBlock (does not instantiate materials).
/// </summary>
public class GrowthColorGradientDriver : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GrowthPlant growthPlant;
    [Tooltip("Optional root. If null, uses this transform.")]
    [SerializeField] private Transform rendererSearchRoot;

    [Header("Randomization")]
    [SerializeField] private bool randomizeOnEnable = true;
    [Tooltip("If enabled, use a stable seed so the same mushroom keeps the same colors across play sessions.")]
    [SerializeField] private bool deterministicSeed = false;
    [SerializeField] private int seedOverride = 0;

    [Header("Growth Gradient")]
    [Tooltip("How quickly the color reaches the 'final' look as the plant grows.")]
    [SerializeField] private AnimationCurve colorOverGrowth = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Tooltip("Darken the starting color by this multiplier (0..1).")]
    [Range(0f, 1f)]
    [SerializeField] private float startBrightness = 0.35f;
    [Tooltip("Optional: add a small hue drift over growth for a more magical look.")]
    [Range(-0.2f, 0.2f)]
    [SerializeField] private float hueDrift = 0f;

    [Header("Shader Properties")]
    [Tooltip("Common color property names. First match wins.")]
    [SerializeField] private string[] colorPropertyNames = { "_BaseColor", "_Color", "_TintColor" };

    [Header("Presets")]
    [Tooltip("When enabled, ignores serialized presets and uses only: Pink / Blue / Yellow / Brown.")]
    [SerializeField] private bool forceDefaultFourPresets = true;

    [SerializeField] private ColorPreset[] presets = new[]
    {
        new ColorPreset("Pink", new Color(0.92f, 0.30f, 0.62f), new Color(0.99f, 0.90f, 0.95f)),
        new ColorPreset("Blue", new Color(0.22f, 0.55f, 0.92f), new Color(0.90f, 0.95f, 0.99f)),
        new ColorPreset("Yellow", new Color(0.98f, 0.86f, 0.22f), new Color(0.99f, 0.98f, 0.90f)),
        new ColorPreset("Brown", new Color(0.56f, 0.34f, 0.20f), new Color(0.96f, 0.92f, 0.86f))
    };
    
    private static readonly ColorPreset[] DefaultFourPresets =
    {
        new ColorPreset("Pink", new Color(0.92f, 0.30f, 0.62f), new Color(0.99f, 0.90f, 0.95f)),
        new ColorPreset("Blue", new Color(0.22f, 0.55f, 0.92f), new Color(0.90f, 0.95f, 0.99f)),
        new ColorPreset("Yellow", new Color(0.98f, 0.86f, 0.22f), new Color(0.99f, 0.98f, 0.90f)),
        new ColorPreset("Brown", new Color(0.56f, 0.34f, 0.20f), new Color(0.96f, 0.92f, 0.86f))
    };

    [Serializable]
    public struct ColorPreset
    {
        public string name;
        public Color capColor;
        public Color stemColor;

        public ColorPreset(string name, Color capColor, Color stemColor)
        {
            this.name = name;
            this.capColor = capColor;
            this.stemColor = stemColor;
        }
    }

    private readonly List<Renderer> renderers = new();
    private MaterialPropertyBlock mpb;
    private int colorPropertyId = -1;

    private Color currentCapColor;
    private Color currentStemColor;

    private void Awake()
    {
        if (growthPlant == null)
        {
            growthPlant = GetComponent<GrowthPlant>() ?? GetComponentInParent<GrowthPlant>();
        }

        if (rendererSearchRoot == null)
        {
            rendererSearchRoot = transform;
        }

        if (mpb == null)
        {
            mpb = new MaterialPropertyBlock();
        }

        if (forceDefaultFourPresets)
        {
            presets = DefaultFourPresets;
        }

        ResolveColorProperty();
        CacheRenderers();
    }

    private void OnEnable()
    {
        if (forceDefaultFourPresets)
        {
            presets = DefaultFourPresets;
        }

        if (randomizeOnEnable)
        {
            RandomizePreset();
        }
    }

    private void OnValidate()
    {
        if (forceDefaultFourPresets)
        {
            presets = DefaultFourPresets;
        }
    }

    private void Update()
    {
        if (growthPlant == null || renderers.Count == 0 || colorPropertyId == -1)
        {
            return;
        }

        float growth = Mathf.Clamp01(growthPlant.CurrentGrowthTime);
        float t = Mathf.Clamp01(colorOverGrowth.Evaluate(growth));

        Color cap = ApplyGrowthGradient(currentCapColor, t);
        Color stem = ApplyGrowthGradient(currentStemColor, t);

        ApplyToRenderers(cap, stem);
    }

    [ContextMenu("Randomize Preset Now")]
    public void RandomizePreset()
    {
        if (presets == null || presets.Length == 0)
        {
            currentCapColor = Color.white;
            currentStemColor = new Color(0.95f, 0.95f, 0.95f);
            return;
        }

        int seed = 0;
        if (deterministicSeed)
        {
            seed = seedOverride != 0 ? seedOverride : gameObject.GetInstanceID();
        }
        else
        {
            seed = Environment.TickCount ^ gameObject.GetInstanceID();
        }

        var state = UnityEngine.Random.state;
        UnityEngine.Random.InitState(seed);
        int index = UnityEngine.Random.Range(0, presets.Length);
        UnityEngine.Random.state = state;

        currentCapColor = presets[index].capColor;
        currentStemColor = presets[index].stemColor;
    }

    private void CacheRenderers()
    {
        renderers.Clear();
        if (rendererSearchRoot == null)
        {
            return;
        }

        Renderer[] found = rendererSearchRoot.GetComponentsInChildren<Renderer>(true);
        if (found == null)
        {
            return;
        }

        for (int i = 0; i < found.Length; i++)
        {
            if (found[i] != null)
            {
                renderers.Add(found[i]);
            }
        }
    }

    private void ResolveColorProperty()
    {
        colorPropertyId = -1;
        if (colorPropertyNames == null || colorPropertyNames.Length == 0)
        {
            return;
        }

        // Find the first property that exists on any renderer material.
        Renderer[] found = GetComponentsInChildren<Renderer>(true);
        if (found == null || found.Length == 0)
        {
            return;
        }

        for (int p = 0; p < colorPropertyNames.Length; p++)
        {
            string prop = colorPropertyNames[p];
            if (string.IsNullOrWhiteSpace(prop))
            {
                continue;
            }

            for (int r = 0; r < found.Length; r++)
            {
                Renderer renderer = found[r];
                if (renderer == null)
                {
                    continue;
                }

                Material mat = renderer.sharedMaterial;
                if (mat != null && mat.HasProperty(prop))
                {
                    colorPropertyId = Shader.PropertyToID(prop);
                    return;
                }
            }
        }
    }

    private Color ApplyGrowthGradient(Color target, float t)
    {
        // Start darker and slightly desaturated, then move to target.
        Color start = target * Mathf.Lerp(0.05f, 1f, startBrightness);
        start.a = target.a;

        Color blended = Color.Lerp(start, target, t);

        if (Mathf.Abs(hueDrift) > 0.0001f)
        {
            Color.RGBToHSV(blended, out float h, out float s, out float v);
            h = Mathf.Repeat(h + hueDrift * t, 1f);
            blended = Color.HSVToRGB(h, Mathf.Clamp01(s), Mathf.Clamp01(v));
            blended.a = target.a;
        }

        return blended;
    }

    private void ApplyToRenderers(Color cap, Color stem)
    {
        for (int i = 0; i < renderers.Count; i++)
        {
            Renderer r = renderers[i];
            if (r == null)
            {
                continue;
            }

            // Heuristic: name contains "stem" uses stem color, otherwise cap color.
            Color chosen = r.name.IndexOf("stem", StringComparison.OrdinalIgnoreCase) >= 0 ? stem : cap;

            r.GetPropertyBlock(mpb);
            mpb.SetColor(colorPropertyId, chosen);
            r.SetPropertyBlock(mpb);
        }
    }
}
