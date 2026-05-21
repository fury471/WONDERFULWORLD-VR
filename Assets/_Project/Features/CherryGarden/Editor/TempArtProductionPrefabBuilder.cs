using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class TempArtProductionPrefabBuilder
{
    private const string AutoRunFlagPath = "Temp/RunTempArtProductionBuild.flag";
    private const string OutputRoot = "Assets/_Project/Features/CherryGarden/Art/ImportedInazuma";
    private const string ModelRoot = OutputRoot + "/Models";
    private const string MaterialRoot = OutputRoot + "/Materials";
    private const string GeneratedTextureRoot = OutputRoot + "/Textures/ToonGenerated";
    private const string PrefabRoot = OutputRoot + "/Prefabs";
    private const string CreditRoot = OutputRoot + "/Credits";
    private const string OutlineMaterialPath = MaterialRoot + "/WW_ImportedInazuma_BlackOutline.mat";
    private const string ToonShaderName = "Wonderland/Props/Toon Band Lit URP";
    private const string OutlineShaderName = "Wonderland/CherryGarden/Toon Outline URP";
    private const string PetalReferenceMaterialPath = "Assets/_Project/Features/CherryGarden/Art/Terrain/Materials/CherryPetalParticle_Mat.mat";
    private const string ToonTextureVersionSuffix = "_ToonStyleV2.png";
    private const string SakuraTextureVersionSuffix = "_SakuraStyleV2.png";

    private static readonly ModelSpec[] ModelSpecs =
    {
        new(
            "JapaneseToriiGate",
            "WW_Inazuma_ToriiGate",
            "Assets/_TempArt/Inazuma_Style_Candidates/japanese_torii_gate.glb",
            "JapaneseToriiGate/japanese_torii_gate.glb",
            new[] { "Assets/_TempArt/Inazuma_Style_Candidates/japanese_torii_gate.glb" },
            new[] { "Assets/_TempArt/Inazuma_Style_Candidates/japanese_torii_gate_CREDIT.txt" },
            treeLike: false,
            maxOutlineRenderers: 96),
        new(
            "CherryBlossomTrees",
            "WW_Inazuma_CherryBlossomTrees",
            "Assets/_TempArt/Inazuma_Style_Candidates/cherry_blossom_trees/scene.gltf",
            "CherryBlossomTrees/scene.gltf",
            new[] { "Assets/_TempArt/Inazuma_Style_Candidates/cherry_blossom_trees" },
            new[] { "Assets/_TempArt/Inazuma_Style_Candidates/cherry_blossom_trees/license.txt" },
            treeLike: true,
            maxOutlineRenderers: 128),
        new(
            "UkiyoSakuraSet",
            "WW_Inazuma_UkiyoSakuraSet",
            "Assets/_TempArt/Inazuma_Style_Candidates/ukiyo_sakura/scene.gltf",
            "UkiyoSakuraSet/scene.gltf",
            new[] { "Assets/_TempArt/Inazuma_Style_Candidates/ukiyo_sakura" },
            new[] { "Assets/_TempArt/Inazuma_Style_Candidates/ukiyo_sakura/license.txt" },
            treeLike: false,
            maxOutlineRenderers: 256),
        new(
            "BuddhaGardenSet",
            "WW_Inazuma_BuddhaGardenSet",
            "Assets/_TempArt/Inazuma_Style_Candidates/buddha-statues/source/Budha_scene.fbx",
            "BuddhaGardenSet/source/Budha_scene.fbx",
            new[] { "Assets/_TempArt/Inazuma_Style_Candidates/buddha-statues" },
            Array.Empty<string>(),
            treeLike: false,
            maxOutlineRenderers: 256)
    };

    [InitializeOnLoadMethod]
    private static void RunPendingBuildRequest()
    {
        if (!File.Exists(AutoRunFlagPath))
        {
            return;
        }

        File.Delete(AutoRunFlagPath);
        EditorApplication.delayCall += BuildProductionPrefabs;
    }

    [MenuItem("Wonderland/Art/Build Production Prefabs From _TempArt")]
    public static void BuildProductionPrefabs()
    {
        Shader toonShader = Shader.Find(ToonShaderName);
        Shader outlineShader = Shader.Find(OutlineShaderName);
        if (toonShader == null || outlineShader == null)
        {
            throw new InvalidOperationException("Missing toon or outline shader. Reimport CherryGarden shaders and try again.");
        }

        EnsureFolder(OutputRoot);
        EnsureFolder(ModelRoot);
        EnsureFolder(MaterialRoot);
        EnsureFolder(GeneratedTextureRoot);
        EnsureFolder(PrefabRoot);
        EnsureFolder(CreditRoot);

        Material outlineMaterial = EnsureOutlineMaterial(outlineShader);
        SakuraPalette sakuraPalette = EstimateSakuraPalette();

        CopySourcePackages();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        int prefabCount = 0;
        foreach (ModelSpec spec in ModelSpecs)
        {
            if (BuildPrefab(spec, toonShader, outlineMaterial, sakuraPalette))
            {
                prefabCount++;
            }
        }

        ApplySceneAtmosphereToExistingMaterials(toonShader);
        WriteReadme();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"ImportedInazuma production build complete. Prefabs: {prefabCount}. Output: {OutputRoot}");
    }

    private static bool BuildPrefab(ModelSpec spec, Shader toonShader, Material outlineMaterial, SakuraPalette sakuraPalette)
    {
        string modelAssetPath = $"{ModelRoot}/{spec.TargetModelRelativePath}";
        GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelAssetPath);
        if (modelAsset == null)
        {
            Debug.LogWarning($"Skipping {spec.DisplayName}: model asset not imported at {modelAssetPath}.");
            return false;
        }

        GameObject root = new(spec.DisplayName + "_Prefab");
        GameObject geometryRoot = CreateChild(root.transform, "Geometry");
        GameObject outlinedRoot = CreateChild(geometryRoot.transform, "Outlined_TrunksAndHardSurface");
        GameObject unoutlinedRoot = CreateChild(geometryRoot.transform, "Unoutlined_FoliageAndPetals");
        GameObject transparentRoot = CreateChild(geometryRoot.transform, "Transparent_NoOutline");

        GameObject instance = PrefabUtility.InstantiatePrefab(modelAsset) as GameObject;
        if (instance == null)
        {
            instance = UnityEngine.Object.Instantiate(modelAsset);
        }

        instance.name = "Source_Unpacked";
        instance.transform.SetParent(root.transform, false);
        if (PrefabUtility.IsPartOfPrefabInstance(instance))
        {
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        }

        Dictionary<string, Material> materialCache = new(StringComparer.OrdinalIgnoreCase);
        MeshRenderer[] renderers = instance.GetComponentsInChildren<MeshRenderer>(includeInactive: true);
        int outlinedCount = 0;
        int vegetationCount = 0;
        int transparentCount = 0;

        for (int i = 0; i < renderers.Length; i++)
        {
            MeshRenderer renderer = renderers[i];
            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null)
            {
                continue;
            }

            Material[] sourceMaterials = renderer.sharedMaterials;
            Material[] toonMaterials = new Material[Mathf.Max(1, sourceMaterials.Length)];
            RendererCategory category = RendererCategory.Outlined;
            string categoryName = "HardSurface";

            for (int materialIndex = 0; materialIndex < toonMaterials.Length; materialIndex++)
            {
                Material sourceMaterial = materialIndex < sourceMaterials.Length ? sourceMaterials[materialIndex] : null;
                MaterialDescriptor descriptor = DescribeMaterial(spec, sourceMaterial, sakuraPalette);
                toonMaterials[materialIndex] = EnsureToonMaterial(spec, descriptor, sourceMaterial, toonShader, sakuraPalette, materialCache);
                category = MergeCategory(category, descriptor.Category);
                categoryName = descriptor.CategoryName;
            }

            renderer.sharedMaterials = toonMaterials;
            ConfigureRenderer(renderer, category);

            Transform targetParent = category switch
            {
                RendererCategory.Transparent => transparentRoot.transform,
                RendererCategory.Unoutlined => unoutlinedRoot.transform,
                _ => outlinedRoot.transform
            };

            renderer.transform.SetParent(targetParent, true);
            renderer.gameObject.name = $"R_{SanitizeName(categoryName)}_{i:00}_{SanitizeName(renderer.gameObject.name)}";

            if (category == RendererCategory.Transparent)
            {
                transparentCount++;
            }
            else if (category == RendererCategory.Unoutlined)
            {
                vegetationCount++;
            }
            else
            {
                outlinedCount++;
            }
        }

        UnityEngine.Object.DestroyImmediate(instance);
        RemoveEmptyGroup(transparentRoot);
        RemoveEmptyGroup(unoutlinedRoot);
        RemoveEmptyGroup(outlinedRoot);

        SelectiveToonOutline outline = root.AddComponent<SelectiveToonOutline>();
        SerializedObject outlineObject = new(outline);
        outlineObject.FindProperty("rebuildOnEnable").boolValue = true;
        outlineObject.FindProperty("maxOutlineRenderers").intValue = spec.MaxOutlineRenderers;
        outlineObject.FindProperty("outlineMaterial").objectReferenceValue = outlineMaterial;
        SetStringArray(outlineObject.FindProperty("includedNameContains"), new[] { "Outlined_" });
        SetStringArray(outlineObject.FindProperty("excludedNameContains"), new[]
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
        });
        outlineObject.ApplyModifiedPropertiesWithoutUndo();

        string prefabPath = $"{PrefabRoot}/{spec.DisplayName}_Ready.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        UnityEngine.Object.DestroyImmediate(root);

        Debug.Log($"{spec.DisplayName}: built {prefabPath}. Outlined={outlinedCount}, foliage/petals no outline={vegetationCount}, transparent={transparentCount}");
        return true;
    }

    private static Material EnsureToonMaterial(
        ModelSpec spec,
        MaterialDescriptor descriptor,
        Material sourceMaterial,
        Shader toonShader,
        SakuraPalette sakuraPalette,
        Dictionary<string, Material> cache)
    {
        string sourceName = sourceMaterial != null ? sourceMaterial.name : "Default";
        Texture2D baseTexture = GetTexture(sourceMaterial, "_BaseMap", "_MainTex") as Texture2D;
        Texture2D emissionTexture = GetTexture(sourceMaterial, "_EmissionMap") as Texture2D;
        baseTexture ??= FindTextureForMaterial(spec, sourceName, TextureRole.BaseColor);
        emissionTexture ??= FindTextureForMaterial(spec, sourceName, TextureRole.Emission);
        Color baseColor = GetColor(sourceMaterial, "_BaseColor", "_Color", Color.white);
        Color emissionColor = GetColor(sourceMaterial, "_EmissionColor", null, Color.black);

        string cacheKey = $"{sourceName}|{AssetDatabase.GetAssetPath(baseTexture)}|{descriptor.Kind}";
        if (cache.TryGetValue(cacheKey, out Material cached) && cached != null)
        {
            return cached;
        }

        string materialFolder = $"{MaterialRoot}/{spec.Key}";
        EnsureFolder(materialFolder);
        string materialPath = $"{materialFolder}/{spec.DisplayName}_{SanitizeName(sourceName)}_Toon.mat";

        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            material = new Material(toonShader);
            AssetDatabase.CreateAsset(material, AssetDatabase.GenerateUniqueAssetPath(materialPath));
            materialPath = AssetDatabase.GetAssetPath(material);
        }

        Texture2D toonTexture = StylizeTexture(spec, descriptor, baseTexture, sakuraPalette);
        Color average = toonTexture != null ? MultiplyColor(EstimateAverageColor(toonTexture), baseColor) : baseColor;
        if (descriptor.Kind == MaterialKind.SakuraPetal)
        {
            average = ShiftHue(average, sakuraPalette.Hue, Mathf.Max(sakuraPalette.Saturation, 0.34f));
        }

        CalculateRampColors(descriptor, average, sakuraPalette, out Color shadow, out Color highlight);

        material.shader = toonShader;
        if (toonTexture != null)
        {
            material.SetTexture("_BaseMap", toonTexture);
        }

        material.SetColor("_BaseColor", Color.white);
        material.SetColor("_ShadowColor", shadow);
        material.SetColor("_HighlightColor", highlight);
        material.SetFloat("_RampThreshold", descriptor.Kind == MaterialKind.Stone ? 0.54f : descriptor.Kind == MaterialKind.SakuraPetal ? 0.38f : 0.47f);
        material.SetFloat("_RampSoftness", descriptor.Kind == MaterialKind.SakuraPetal || descriptor.Category == RendererCategory.Unoutlined ? 0.028f : 0.018f);
        material.SetFloat("_AmbientStrength", descriptor.Category == RendererCategory.Unoutlined ? 0.76f : descriptor.Kind == MaterialKind.Emissive ? 0.52f : 0.42f);
        ApplySceneAtmosphere(material, descriptor);
        material.SetFloat("_Cutoff", descriptor.Kind == MaterialKind.SakuraPetal ? 0.34f : descriptor.Category == RendererCategory.Transparent ? 0.28f : descriptor.Category == RendererCategory.Unoutlined ? 0.3f : 0.5f);
        material.SetFloat("_AlphaClip", descriptor.Category == RendererCategory.Transparent || descriptor.Category == RendererCategory.Unoutlined ? 1f : 0f);
        material.SetFloat("_Cull", descriptor.Category == RendererCategory.Unoutlined ? 0f : 2f);
        if (descriptor.Category == RendererCategory.Transparent || descriptor.Category == RendererCategory.Unoutlined)
        {
            material.SetOverrideTag("RenderType", "TransparentCutout");
            material.renderQueue = (int)RenderQueue.AlphaTest;
        }
        else
        {
            material.SetOverrideTag("RenderType", "Opaque");
            material.renderQueue = -1;
        }

        if (emissionTexture != null || descriptor.Kind == MaterialKind.Emissive || emissionColor.maxColorComponent > 0.001f)
        {
            Texture2D toonEmission = StylizeEmissionTexture(spec, sourceName, emissionTexture);
            if (toonEmission != null)
            {
                material.SetTexture("_EmissionMap", toonEmission);
            }

            material.SetColor("_EmissionColor", emissionColor.maxColorComponent > 0.001f ? emissionColor : new Color(1f, 0.72f, 0.46f, 1f));
            material.SetFloat("_EmissionStrength", descriptor.Kind == MaterialKind.Emissive ? 1.12f : 0.55f);
            material.EnableKeyword("_EMISSION");
        }
        else
        {
            material.SetFloat("_EmissionStrength", 0f);
            material.DisableKeyword("_EMISSION");
        }

        material.doubleSidedGI = descriptor.Category == RendererCategory.Unoutlined || descriptor.Category == RendererCategory.Transparent;
        material.enableInstancing = true;
        EditorUtility.SetDirty(material);
        cache[cacheKey] = material;
        return material;
    }

    private static Texture2D StylizeTexture(ModelSpec spec, MaterialDescriptor descriptor, Texture2D source, SakuraPalette sakuraPalette)
    {
        if (source == null)
        {
            return null;
        }

        string sourcePath = AssetDatabase.GetAssetPath(source);
        if (string.IsNullOrEmpty(sourcePath))
        {
            return source;
        }

        string folder = $"{GeneratedTextureRoot}/{spec.Key}";
        EnsureFolder(folder);
        string suffix = descriptor.Kind == MaterialKind.SakuraPetal ? SakuraTextureVersionSuffix : ToonTextureVersionSuffix;
        string outputPath = $"{folder}/{SanitizeName(source.name)}{suffix}";
        Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(outputPath);
        if (existing != null)
        {
            return existing;
        }

        TextureImporter sourceImporter = AssetImporter.GetAtPath(sourcePath) as TextureImporter;
        bool restoreReadable = false;
        TextureImporterCompression restoreCompression = TextureImporterCompression.Compressed;
        if (sourceImporter != null)
        {
            restoreReadable = sourceImporter.isReadable;
            restoreCompression = sourceImporter.textureCompression;
            if (!sourceImporter.isReadable)
            {
                sourceImporter.isReadable = true;
                sourceImporter.textureCompression = TextureImporterCompression.Uncompressed;
                sourceImporter.SaveAndReimport();
            }
        }

        try
        {
            int width = Mathf.Clamp(PreviousPowerOfTwo(source.width), 64, 1024);
            int height = Mathf.Clamp(PreviousPowerOfTwo(source.height), 64, 1024);
            Texture2D output = new(width, height, TextureFormat.RGBA32, true, false);
            Color32[] pixels = new Color32[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float u = (x + 0.5f) / width;
                    float v = (y + 0.5f) / height;
                    Color raw = source.GetPixelBilinear(u, v);
                    float alpha = descriptor.Category == RendererCategory.Outlined ? raw.a : raw.a < 0.08f ? 0f : raw.a;
                    if (alpha <= 0.001f)
                    {
                        pixels[y * width + x] = new Color(0f, 0f, 0f, 0f);
                        continue;
                    }

                    Color smoothed = SampleSmoothed(source, u, v, descriptor.Kind == MaterialKind.SakuraPetal ? 1.7f : descriptor.Kind == MaterialKind.Lacquer ? 2.8f : 1.25f);
                    Color stylized = descriptor.Kind == MaterialKind.SakuraPetal
                        ? StylizeSakuraFoliagePixel(smoothed, sakuraPalette)
                        : StylizeSceneTexturePixel(smoothed, descriptor);
                    stylized.a = alpha;
                    pixels[y * width + x] = stylized;
                }
            }

            output.SetPixels32(pixels);
            output.Apply(updateMipmaps: true, makeNoLongerReadable: false);
            File.WriteAllBytes(ToFullPath(outputPath), output.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(output);
        }
        finally
        {
            if (sourceImporter != null && sourceImporter.isReadable != restoreReadable)
            {
                sourceImporter.isReadable = restoreReadable;
                sourceImporter.textureCompression = restoreCompression;
                sourceImporter.SaveAndReimport();
            }
        }

        AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceUpdate);
        ConfigureGeneratedTexture(outputPath, descriptor.Category != RendererCategory.Outlined);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(outputPath);
    }

    private static Texture2D StylizeEmissionTexture(ModelSpec spec, string materialName, Texture2D source)
    {
        if (source == null)
        {
            return null;
        }

        string sourcePath = AssetDatabase.GetAssetPath(source);
        if (string.IsNullOrEmpty(sourcePath))
        {
            return source;
        }

        string folder = $"{GeneratedTextureRoot}/{spec.Key}";
        EnsureFolder(folder);
        string outputPath = $"{folder}/{SanitizeName(materialName)}_Emission_ToonPalette.png";
        Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(outputPath);
        if (existing != null)
        {
            return existing;
        }

        TextureImporter importer = AssetImporter.GetAtPath(sourcePath) as TextureImporter;
        bool restoreReadable = false;
        if (importer != null)
        {
            restoreReadable = importer.isReadable;
            if (!importer.isReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
            }
        }

        try
        {
            int width = Mathf.Clamp(PreviousPowerOfTwo(source.width), 32, 512);
            int height = Mathf.Clamp(PreviousPowerOfTwo(source.height), 32, 512);
            Texture2D output = new(width, height, TextureFormat.RGBA32, true, false);
            Color32[] pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color color = source.GetPixelBilinear((x + 0.5f) / width, (y + 0.5f) / height);
                    Color.RGBToHSV(color, out float h, out float s, out float v);
                    Color stylized = Color.HSVToRGB(h, Mathf.Clamp01(s * 1.1f), v < 0.18f ? 0f : Mathf.Lerp(v, 1f, 0.4f));
                    stylized.a = color.a;
                    pixels[y * width + x] = stylized;
                }
            }

            output.SetPixels32(pixels);
            output.Apply(updateMipmaps: true, makeNoLongerReadable: false);
            File.WriteAllBytes(ToFullPath(outputPath), output.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(output);
        }
        finally
        {
            if (importer != null && importer.isReadable != restoreReadable)
            {
                importer.isReadable = restoreReadable;
                importer.SaveAndReimport();
            }
        }

        AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceUpdate);
        ConfigureGeneratedTexture(outputPath, hasAlpha: true);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(outputPath);
    }

    private static Color StylizeSceneTexturePixel(Color color, MaterialDescriptor descriptor)
    {
        Color.RGBToHSV(color, out float hue, out float saturation, out float value);

        if (descriptor.Kind == MaterialKind.Lacquer)
        {
            bool redLacquer = (hue < 0.075f || hue > 0.94f) && saturation > 0.22f;
            bool darkTrim = value < 0.24f && !redLacquer;
            if (redLacquer)
            {
                saturation = Mathf.Clamp(Mathf.Max(0.62f, saturation * 1.08f), 0f, 0.9f);
                value = PickBand(value, 0.34f, 0.52f, 0.72f, 0.46f, 0.6f, 0.75f, 0.9f);
            }
            else if (darkTrim)
            {
                saturation = Mathf.Clamp(Mathf.Max(0.25f, saturation * 0.75f), 0f, 0.55f);
                value = PickBand(value, 0.12f, 0.2f, 0.32f, 0.12f, 0.2f, 0.3f, 0.38f);
            }
            else
            {
                saturation = Mathf.Clamp(Mathf.Max(0.3f, saturation * 0.88f), 0f, 0.72f);
                value = PickBand(value, 0.32f, 0.5f, 0.7f, 0.3f, 0.43f, 0.57f, 0.7f);
            }

            return Color.HSVToRGB(hue, saturation, value);
        }

        switch (descriptor.Kind)
        {
            case MaterialKind.Wood:
                saturation = Mathf.Clamp01(Mathf.Max(0.24f, saturation * 1.04f));
                value = PickBand(value, 0.3f, 0.48f, 0.68f, 0.24f, 0.38f, 0.54f, 0.72f);
                break;
            case MaterialKind.Stone:
                saturation = Mathf.Clamp01(saturation * 0.72f);
                value = PickBand(value, 0.34f, 0.52f, 0.72f, 0.42f, 0.56f, 0.7f, 0.84f);
                break;
            case MaterialKind.Vegetation:
                saturation = Mathf.Clamp01(Mathf.Max(0.22f, saturation * 1.08f));
                value = PickBand(value, 0.32f, 0.52f, 0.74f, 0.36f, 0.54f, 0.72f, 0.86f);
                break;
            case MaterialKind.Emissive:
                saturation = Mathf.Clamp01(saturation * 1.12f);
                value = Mathf.Lerp(value, 1f, 0.36f);
                break;
            default:
                saturation = Mathf.Clamp01(saturation * 1.02f);
                value = PickBand(value, 0.32f, 0.52f, 0.74f, 0.32f, 0.5f, 0.68f, 0.86f);
                break;
        }

        return Color.HSVToRGB(hue, saturation, value);
    }

    private static Color StylizeSakuraFoliagePixel(Color color, SakuraPalette sakuraPalette)
    {
        Color.RGBToHSV(color, out float hue, out float saturation, out float value);
        bool branch = hue >= 0.045f
            && hue <= 0.135f
            && saturation > 0.22f
            && value < 0.7f
            && color.r > color.g
            && color.g > color.b;

        if (branch)
        {
            saturation = Mathf.Clamp01(Mathf.Max(0.38f, saturation * 1.04f));
            value = PickBand(value, 0.3f, 0.48f, 0.64f, 0.25f, 0.37f, 0.5f, 0.62f);
            return Color.HSVToRGB(hue, saturation, value);
        }

        bool darkAccent = value < 0.46f && saturation > 0.1f;
        float petalHue = sakuraPalette.Hue;
        saturation = Mathf.Clamp01(Mathf.Max(sakuraPalette.Saturation * 1.02f, Mathf.Min(0.48f, saturation * 0.45f)));
        if (darkAccent)
        {
            petalHue = Repeat01(sakuraPalette.Hue + 0.985f);
            saturation = Mathf.Clamp01(Mathf.Max(saturation, 0.26f));
            value = value < 0.3f ? 0.67f : 0.76f;
        }
        else
        {
            value = Mathf.Max(value, 0.73f);
            value = value < 0.8f ? 0.83f : value < 0.91f ? 0.92f : 0.99f;
        }

        return Color.HSVToRGB(petalHue, saturation, value);
    }

    private static Color SampleSmoothed(Texture2D source, float u, float v, float radiusPixels)
    {
        float offsetX = radiusPixels / Mathf.Max(1, source.width);
        float offsetY = radiusPixels / Mathf.Max(1, source.height);
        Color total = Color.clear;
        float totalWeight = 0f;

        for (int y = -1; y <= 1; y++)
        {
            for (int x = -1; x <= 1; x++)
            {
                float weight = x == 0 && y == 0 ? 4f : x == 0 || y == 0 ? 2f : 1f;
                total += source.GetPixelBilinear(Mathf.Clamp01(u + x * offsetX), Mathf.Clamp01(v + y * offsetY)) * weight;
                totalWeight += weight;
            }
        }

        return total / totalWeight;
    }

    private static float PickBand(float value, float thresholdA, float thresholdB, float thresholdC, float bandA, float bandB, float bandC, float bandD)
    {
        return value < thresholdA ? bandA : value < thresholdB ? bandB : value < thresholdC ? bandC : bandD;
    }

    private static float Repeat01(float value)
    {
        value %= 1f;
        return value < 0f ? value + 1f : value;
    }

    private static MaterialDescriptor DescribeMaterial(ModelSpec spec, Material material, SakuraPalette sakuraPalette)
    {
        string materialName = material != null ? material.name : "Default";
        Texture baseTexture = GetTexture(material, "_BaseMap", "_MainTex");
        string textureName = baseTexture != null ? baseTexture.name : string.Empty;
        string descriptor = $"{materialName} {textureName}";

        if (string.Equals(spec.Key, "JapaneseToriiGate", StringComparison.OrdinalIgnoreCase))
        {
            return new MaterialDescriptor(MaterialKind.Lacquer, RendererCategory.Outlined, "LacqueredWood");
        }

        if (string.Equals(spec.Key, "CherryBlossomTrees", StringComparison.OrdinalIgnoreCase)
            && ContainsAny(descriptor, "foliage", "image_3"))
        {
            return new MaterialDescriptor(MaterialKind.SakuraPetal, RendererCategory.Unoutlined, "SakuraPetals");
        }

        if (ContainsAny(descriptor, "glass", "water", "smoke", "particle", "vfx"))
        {
            return new MaterialDescriptor(MaterialKind.Transparent, RendererCategory.Transparent, "Transparent");
        }

        if (IsSakuraPetalMaterial(descriptor))
        {
            return new MaterialDescriptor(MaterialKind.SakuraPetal, RendererCategory.Unoutlined, "SakuraPetals");
        }

        if (ContainsAny(descriptor, "leaf", "leaves", "foliage", "flower", "grass", "plant", "shrub", "vine", "wisteria"))
        {
            return new MaterialDescriptor(MaterialKind.Vegetation, RendererCategory.Unoutlined, "Vegetation");
        }

        if (spec.TreeLike && !ContainsAny(descriptor, "bark", "trunk", "wood", "branch"))
        {
            return new MaterialDescriptor(MaterialKind.Vegetation, RendererCategory.Unoutlined, "TreeCanopy");
        }

        if (ContainsAny(descriptor, "lantern", "emissive", "dragon_ha"))
        {
            return new MaterialDescriptor(MaterialKind.Emissive, RendererCategory.Outlined, "EmissiveProp");
        }

        if (ContainsAny(descriptor, "stone", "rock", "stair", "terrain", "budha", "buddha"))
        {
            return new MaterialDescriptor(MaterialKind.Stone, RendererCategory.Outlined, "Stone");
        }

        if (ContainsAny(descriptor, "wood", "bark", "trunk", "branch", "torii"))
        {
            return new MaterialDescriptor(MaterialKind.Wood, RendererCategory.Outlined, "Wood");
        }

        return new MaterialDescriptor(MaterialKind.Default, RendererCategory.Outlined, "HardSurface");
    }

    private static void ApplySceneAtmosphere(Material material, MaterialDescriptor descriptor)
    {
        if (material == null)
        {
            return;
        }

        float lightInfluence;
        float ambientFloor;
        float shadowStrength;

        if (descriptor.Kind == MaterialKind.SakuraPetal)
        {
            lightInfluence = 0.4f;
            ambientFloor = 0.24f;
            shadowStrength = 0.28f;
        }
        else if (descriptor.Category == RendererCategory.Unoutlined || descriptor.Kind == MaterialKind.Vegetation)
        {
            lightInfluence = 0.44f;
            ambientFloor = 0.22f;
            shadowStrength = 0.36f;
        }
        else if (descriptor.Kind == MaterialKind.Stone)
        {
            lightInfluence = 0.52f;
            ambientFloor = 0.16f;
            shadowStrength = 0.58f;
        }
        else if (descriptor.Kind == MaterialKind.Lacquer || descriptor.Kind == MaterialKind.Wood)
        {
            lightInfluence = 0.48f;
            ambientFloor = 0.18f;
            shadowStrength = 0.5f;
        }
        else
        {
            lightInfluence = 0.5f;
            ambientFloor = 0.18f;
            shadowStrength = 0.52f;
        }

        SetFloatIfPresent(material, "_LightInfluence", lightInfluence);
        SetFloatIfPresent(material, "_AmbientFloor", ambientFloor);
        SetFloatIfPresent(material, "_ShadowStrength", shadowStrength);
        SetFloatIfPresent(material, "_FogInfluence", 1f);
    }

    [MenuItem("Wonderland/Art/Apply Scene Atmosphere To Toon Materials")]
    public static void ApplySceneAtmosphereToToonMaterials()
    {
        Shader toonShader = Shader.Find(ToonShaderName);
        if (toonShader == null)
        {
            throw new InvalidOperationException("Missing toon shader. Reimport project shaders and try again.");
        }

        ApplySceneAtmosphereToExistingMaterials(toonShader);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Applied scene atmosphere parameters to ImportedInazuma and CherryGarden vegetation toon materials.");
    }

    private static void ApplySceneAtmosphereToExistingMaterials(Shader toonShader)
    {
        string[] searchRoots =
        {
            MaterialRoot,
            "Assets/_Project/Features/CherryGarden/Art/Vegetation/Materials"
        };

        foreach (string guid in AssetDatabase.FindAssets("t:Material", searchRoots))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null || material.shader != toonShader)
            {
                continue;
            }

            string descriptorText = Path.GetFileNameWithoutExtension(path);
            MaterialDescriptor descriptor = DescribeStandaloneAtmosphereMaterial(descriptorText);
            ApplySceneAtmosphere(material, descriptor);
            EditorUtility.SetDirty(material);
        }
    }

    private static MaterialDescriptor DescribeStandaloneAtmosphereMaterial(string descriptor)
    {
        if (IsSakuraPetalMaterial(descriptor) || ContainsAny(descriptor, "flower", "petal", "foliage", "leaves", "leaf", "plant"))
        {
            return new MaterialDescriptor(MaterialKind.Vegetation, RendererCategory.Unoutlined, "Vegetation");
        }

        if (ContainsAny(descriptor, "wisteria", "vine", "wood", "bark", "trunk", "branch"))
        {
            return new MaterialDescriptor(MaterialKind.Wood, RendererCategory.Outlined, "Wood");
        }

        if (ContainsAny(descriptor, "stone", "rock", "stair", "terrain", "budha", "buddha"))
        {
            return new MaterialDescriptor(MaterialKind.Stone, RendererCategory.Outlined, "Stone");
        }

        return new MaterialDescriptor(MaterialKind.Default, RendererCategory.Outlined, "HardSurface");
    }

    private static bool IsSakuraPetalMaterial(string descriptor)
    {
        return ContainsAny(descriptor, "sakura", "cherry", "petal", "blossom", "pink_flower", "pink flower", "foliage_basecolor");
    }

    private static void CalculateRampColors(MaterialDescriptor descriptor, Color average, SakuraPalette sakuraPalette, out Color shadow, out Color highlight)
    {
        Color.RGBToHSV(average, out float hue, out float saturation, out float value);
        if (descriptor.Kind == MaterialKind.SakuraPetal)
        {
            hue = sakuraPalette.Hue;
            saturation = Mathf.Max(saturation, sakuraPalette.Saturation * 0.82f);
            value = Mathf.Max(value, 0.78f);
        }

        float shadowValue = descriptor.Kind == MaterialKind.Stone ? 0.56f : descriptor.Kind == MaterialKind.SakuraPetal ? 0.72f : descriptor.Kind == MaterialKind.Lacquer ? 0.62f : 0.58f;
        float highlightValue = descriptor.Kind == MaterialKind.SakuraPetal ? 1.08f : descriptor.Kind == MaterialKind.Lacquer ? 1.12f : 1.2f;

        shadow = Color.HSVToRGB(hue, Mathf.Clamp01(saturation * 1.08f), Mathf.Clamp01(value * shadowValue));
        highlight = Color.HSVToRGB(hue, Mathf.Clamp01(saturation * 0.72f), Mathf.Clamp01(value * highlightValue + 0.06f));
        shadow.a = 1f;
        highlight.a = 1f;
    }

    private static void ConfigureRenderer(MeshRenderer renderer, RendererCategory category)
    {
        renderer.shadowCastingMode = category == RendererCategory.Transparent ? ShadowCastingMode.Off : ShadowCastingMode.On;
        renderer.receiveShadows = category != RendererCategory.Transparent;
        renderer.lightProbeUsage = LightProbeUsage.BlendProbes;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        renderer.allowOcclusionWhenDynamic = true;
    }

    private static RendererCategory MergeCategory(RendererCategory current, RendererCategory next)
    {
        if (current == RendererCategory.Transparent || next == RendererCategory.Transparent)
        {
            return RendererCategory.Transparent;
        }

        if (current == RendererCategory.Unoutlined || next == RendererCategory.Unoutlined)
        {
            return RendererCategory.Unoutlined;
        }

        return RendererCategory.Outlined;
    }

    private static void CopySourcePackages()
    {
        foreach (ModelSpec spec in ModelSpecs)
        {
            string targetBase = $"{ModelRoot}/{spec.Key}";
            EnsureFolder(targetBase);

            foreach (string source in spec.SourcePackagePaths)
            {
                if (AssetDatabase.IsValidFolder(source))
                {
                    CopyFolderAssets(source, targetBase);
                }
                else
                {
                    string fileName = Path.GetFileName(source);
                    CopyAssetIfMissing(source, $"{targetBase}/{fileName}");
                }
            }

            foreach (string credit in spec.CreditPaths)
            {
                if (File.Exists(ToFullPath(credit)))
                {
                    CopyAssetIfMissing(credit, $"{CreditRoot}/{spec.Key}_{Path.GetFileName(credit)}");
                }
            }
        }

        ExtractJapaneseToriiGateTextures();
    }

    private static void CopyFolderAssets(string sourceFolder, string targetFolder)
    {
        string fullSourceFolder = ToFullPath(sourceFolder);
        if (!Directory.Exists(fullSourceFolder))
        {
            Debug.LogWarning($"Missing temp art folder: {sourceFolder}");
            return;
        }

        string[] files = Directory.GetFiles(fullSourceFolder, "*", SearchOption.AllDirectories);
        for (int i = 0; i < files.Length; i++)
        {
            string fullFile = files[i].Replace('\\', '/');
            if (fullFile.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string relative = fullFile.Substring(fullSourceFolder.Replace('\\', '/').Length).TrimStart('/');
            string sourceAssetPath = ToAssetPath(fullFile);
            string targetAssetPath = $"{targetFolder}/{relative}";
            CopyAssetIfMissing(sourceAssetPath, targetAssetPath);
        }
    }

    private static void CopyAssetIfMissing(string sourceAssetPath, string targetAssetPath)
    {
        if (!File.Exists(ToFullPath(sourceAssetPath)))
        {
            Debug.LogWarning($"Missing temp art asset: {sourceAssetPath}");
            return;
        }

        if (File.Exists(ToFullPath(targetAssetPath)))
        {
            return;
        }

        EnsureFolder(Path.GetDirectoryName(targetAssetPath).Replace('\\', '/'));
        if (!AssetDatabase.CopyAsset(sourceAssetPath, targetAssetPath))
        {
            File.Copy(ToFullPath(sourceAssetPath), ToFullPath(targetAssetPath), overwrite: false);
            AssetDatabase.ImportAsset(targetAssetPath, ImportAssetOptions.ForceUpdate);
        }
    }

    private static void ExtractJapaneseToriiGateTextures()
    {
        string glbPath = $"{ModelRoot}/JapaneseToriiGate/japanese_torii_gate.glb";
        string fullGlbPath = ToFullPath(glbPath);
        if (!File.Exists(fullGlbPath))
        {
            return;
        }

        string textureFolder = $"{ModelRoot}/JapaneseToriiGate/textures";
        EnsureFolder(textureFolder);

        try
        {
            byte[] bytes = File.ReadAllBytes(fullGlbPath);
            if (bytes.Length < 20 || BitConverter.ToUInt32(bytes, 0) != 0x46546C67u)
            {
                return;
            }

            int offset = 12;
            string json = null;
            int binStart = -1;
            int binLength = 0;
            while (offset + 8 <= bytes.Length)
            {
                int chunkLength = BitConverter.ToInt32(bytes, offset);
                uint chunkType = BitConverter.ToUInt32(bytes, offset + 4);
                offset += 8;
                if (offset + chunkLength > bytes.Length)
                {
                    break;
                }

                if (chunkType == 0x4E4F534Au)
                {
                    json = Encoding.UTF8.GetString(bytes, offset, chunkLength);
                }
                else if (chunkType == 0x004E4942u)
                {
                    binStart = offset;
                    binLength = chunkLength;
                }

                offset += chunkLength;
            }

            if (string.IsNullOrWhiteSpace(json) || binStart < 0 || binLength <= 0)
            {
                return;
            }

            GltfRoot root = JsonUtility.FromJson<GltfRoot>(json);
            if (root?.images == null || root.bufferViews == null)
            {
                return;
            }

            string[] names =
            {
                "japanese_torii_gate_baseColor",
                "japanese_torii_gate_occlusionRoughness",
                "japanese_torii_gate_normal"
            };

            for (int i = 0; i < root.images.Length && i < names.Length; i++)
            {
                GltfImage image = root.images[i];
                if (image.bufferView < 0 || image.bufferView >= root.bufferViews.Length)
                {
                    continue;
                }

                GltfBufferView view = root.bufferViews[image.bufferView];
                string extension = string.Equals(image.mimeType, "image/jpeg", StringComparison.OrdinalIgnoreCase) ? ".jpg" : ".png";
                string outputPath = $"{textureFolder}/{names[i]}{extension}";
                if (File.Exists(ToFullPath(outputPath)))
                {
                    continue;
                }

                int sourceOffset = binStart + view.byteOffset;
                if (sourceOffset < binStart || sourceOffset + view.byteLength > binStart + binLength)
                {
                    continue;
                }

                byte[] imageBytes = new byte[view.byteLength];
                Buffer.BlockCopy(bytes, sourceOffset, imageBytes, 0, view.byteLength);
                File.WriteAllBytes(ToFullPath(outputPath), imageBytes);
                AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceUpdate);
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Could not extract embedded torii textures: {exception.Message}");
        }
    }

    private static Material EnsureOutlineMaterial(Shader outlineShader)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(OutlineMaterialPath);
        if (material == null)
        {
            material = new Material(outlineShader);
            AssetDatabase.CreateAsset(material, OutlineMaterialPath);
        }

        material.shader = outlineShader;
        material.SetColor("_OutlineColor", Color.black);
        material.SetFloat("_OutlineWidth", 0.014f);
        material.renderQueue = 1990;
        material.enableInstancing = true;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static SakuraPalette EstimateSakuraPalette()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(PetalReferenceMaterialPath);
        Texture2D texture = GetTexture(material, "_BaseMap", "_MainTex") as Texture2D;
        if (texture == null)
        {
            return new SakuraPalette(0.956f, 0.42f, 0.96f);
        }

        string texturePath = AssetDatabase.GetAssetPath(texture);
        TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        bool restoreReadable = false;
        if (importer != null)
        {
            restoreReadable = importer.isReadable;
            if (!importer.isReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
            }
        }

        try
        {
            double sin = 0d;
            double cos = 0d;
            double saturation = 0d;
            double value = 0d;
            double weight = 0d;
            int stepX = Mathf.Max(1, texture.width / 96);
            int stepY = Mathf.Max(1, texture.height / 96);
            for (int y = 0; y < texture.height; y += stepY)
            {
                for (int x = 0; x < texture.width; x += stepX)
                {
                    Color color = texture.GetPixel(x, y);
                    if (color.a < 0.12f || color.maxColorComponent < 0.25f)
                    {
                        continue;
                    }

                    Color.RGBToHSV(color, out float h, out float s, out float v);
                    if (s < 0.08f)
                    {
                        continue;
                    }

                    double pixelWeight = color.a * Mathf.Clamp01(s * 2f) * Mathf.Clamp01(v);
                    double angle = h * Math.PI * 2d;
                    sin += Math.Sin(angle) * pixelWeight;
                    cos += Math.Cos(angle) * pixelWeight;
                    saturation += s * pixelWeight;
                    value += v * pixelWeight;
                    weight += pixelWeight;
                }
            }

            if (weight <= 0d)
            {
                return new SakuraPalette(0.956f, 0.42f, 0.96f);
            }

            float hue = (float)(Math.Atan2(sin, cos) / (Math.PI * 2d));
            if (hue < 0f)
            {
                hue += 1f;
            }

            return new SakuraPalette(hue, (float)(saturation / weight), (float)(value / weight));
        }
        finally
        {
            if (importer != null && importer.isReadable != restoreReadable)
            {
                importer.isReadable = restoreReadable;
                importer.SaveAndReimport();
            }
        }
    }

    private static Texture GetTexture(Material material, params string[] names)
    {
        if (material == null)
        {
            return null;
        }

        for (int i = 0; i < names.Length; i++)
        {
            if (!string.IsNullOrEmpty(names[i]) && material.HasProperty(names[i]))
            {
                Texture texture = material.GetTexture(names[i]);
                if (texture != null)
                {
                    return texture;
                }
            }
        }

        return null;
    }

    private static Texture2D FindTextureForMaterial(ModelSpec spec, string materialName, TextureRole role)
    {
        string folder = $"{ModelRoot}/{spec.Key}";
        if (!AssetDatabase.IsValidFolder(folder))
        {
            return null;
        }

        string normalizedMaterial = NormalizeSearchText(materialName);
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
        Texture2D best = null;
        int bestScore = 0;
        Texture2D onlyRoleCandidate = null;
        int roleCandidateCount = 0;

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            string fileName = Path.GetFileNameWithoutExtension(path);
            string normalizedFile = NormalizeSearchText(fileName);
            if (IsTextureRoleCandidate(normalizedFile, role))
            {
                roleCandidateCount++;
                onlyRoleCandidate = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }

            int score = ScoreTextureCandidate(normalizedMaterial, normalizedFile, role);
            if (score > bestScore)
            {
                bestScore = score;
                best = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
        }

        if (bestScore > 0)
        {
            return best;
        }

        return roleCandidateCount == 1 ? onlyRoleCandidate : null;
    }

    private static int ScoreTextureCandidate(string materialName, string textureName, TextureRole role)
    {
        if (string.IsNullOrEmpty(textureName))
        {
            return 0;
        }

        if (!IsTextureRoleCandidate(textureName, role))
        {
            return 0;
        }

        int score = 0;
        if (!string.IsNullOrEmpty(materialName) && textureName.Contains(materialName))
        {
            score += 100;
        }

        string[] tokens = materialName.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < tokens.Length; i++)
        {
            if (tokens[i].Length > 1 && textureName.Contains(tokens[i]))
            {
                score += 12;
            }
        }

        return score > 0 ? score + (role == TextureRole.Emission ? 20 : 10) : 0;
    }

    private static bool IsTextureRoleCandidate(string textureName, TextureRole role)
    {
        bool isEmission = ContainsAny(textureName, "emissive", "emission");
        bool isBaseColor = ContainsAny(textureName, "basecolor", "basecolour", "base", "albedo", "diffuse", "color", "colour");
        bool isUtility = ContainsAny(textureName, "normal", "roughness", "metallic", "specular", "mask", "ao", "opacity", "translucency");

        return role == TextureRole.Emission ? isEmission : isBaseColor && !isUtility;
    }

    private static Color GetColor(Material material, string first, string second, Color fallback)
    {
        if (material == null)
        {
            return fallback;
        }

        if (!string.IsNullOrEmpty(first) && material.HasProperty(first))
        {
            return material.GetColor(first);
        }

        if (!string.IsNullOrEmpty(second) && material.HasProperty(second))
        {
            return material.GetColor(second);
        }

        return fallback;
    }

    private static void SetFloatIfPresent(Material material, string propertyName, float value)
    {
        if (material != null && material.HasProperty(propertyName))
        {
            material.SetFloat(propertyName, value);
        }
    }

    private static Color EstimateAverageColor(Texture2D texture)
    {
        if (texture == null)
        {
            return Color.white;
        }

        string path = AssetDatabase.GetAssetPath(texture);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        bool restoreReadable = false;
        if (importer != null)
        {
            restoreReadable = importer.isReadable;
            if (!importer.isReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
            }
        }

        try
        {
            Color total = Color.black;
            float weight = 0f;
            int stepX = Mathf.Max(1, texture.width / 24);
            int stepY = Mathf.Max(1, texture.height / 24);
            for (int y = 0; y < texture.height; y += stepY)
            {
                for (int x = 0; x < texture.width; x += stepX)
                {
                    Color color = texture.GetPixel(x, y);
                    float alpha = Mathf.Max(0.001f, color.a);
                    total += color * alpha;
                    weight += alpha;
                }
            }

            return weight > 0f ? total / weight : Color.white;
        }
        finally
        {
            if (importer != null && importer.isReadable != restoreReadable)
            {
                importer.isReadable = restoreReadable;
                importer.SaveAndReimport();
            }
        }
    }

    private static Color MultiplyColor(Color left, Color right)
    {
        return new Color(left.r * right.r, left.g * right.g, left.b * right.b, left.a * right.a);
    }

    private static Color ShiftHue(Color color, float hue, float minimumSaturation)
    {
        Color.RGBToHSV(color, out _, out float saturation, out float value);
        Color shifted = Color.HSVToRGB(hue, Mathf.Clamp01(Mathf.Max(saturation, minimumSaturation)), value);
        shifted.a = color.a;
        return shifted;
    }

    private static void ConfigureGeneratedTexture(string path, bool hasAlpha)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            return;
        }

        importer.textureType = TextureImporterType.Default;
        importer.sRGBTexture = true;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = hasAlpha;
        importer.mipmapEnabled = true;
        importer.npotScale = TextureImporterNPOTScale.ToNearest;
        importer.maxTextureSize = 1024;
        importer.textureCompression = hasAlpha ? TextureImporterCompression.Uncompressed : TextureImporterCompression.CompressedHQ;
        importer.SaveAndReimport();
    }

    private static void WriteReadme()
    {
        string readmePath = $"{OutputRoot}/README.md";
        string content =
            "# Imported Inazuma Production Prefabs\n\n" +
            "Generated from `Assets/_TempArt/Inazuma_Style_Candidates` with `Wonderland > Art > Build Production Prefabs From _TempArt`.\n\n" +
            "- Prefabs live in `Prefabs/` and reference migrated model sources in `Models/`, not the ignored `_TempArt` folder.\n" +
            "- Materials use `Wonderland/Props/Toon Band Lit URP` with generated toon-style V2 textures in `Textures/ToonGenerated/`; colors stay tied to the source palettes while noise/PBR detail is flattened for the scene style.\n" +
            "- The torii GLB's embedded base-color texture is extracted into `Models/JapaneseToriiGate/textures/` before material generation, so the prefab no longer falls back to white.\n" +
            "- Black outlines are generated by `SelectiveToonOutline` only for `Geometry/Outlined_TrunksAndHardSurface`.\n" +
            "- Leaves, foliage, flowers, and petals are grouped under no-outline branches; cherry/pink petals are hue-matched to `CherryPetalParticle_Mat`.\n" +
            "- Original attribution files are copied into `Credits/`; keep them in the final credits/licensing pass.\n";

        File.WriteAllText(ToFullPath(readmePath), content);
        AssetDatabase.ImportAsset(readmePath, ImportAssetOptions.ForceUpdate);
    }

    private static GameObject CreateChild(Transform parent, string name)
    {
        GameObject child = new(name);
        child.transform.SetParent(parent, false);
        return child;
    }

    private static void RemoveEmptyGroup(GameObject group)
    {
        if (group != null && group.transform.childCount == 0)
        {
            UnityEngine.Object.DestroyImmediate(group);
        }
    }

    private static void SetStringArray(SerializedProperty property, string[] values)
    {
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
        {
            property.GetArrayElementAtIndex(i).stringValue = values[i];
        }
    }

    private static bool ContainsAny(string source, params string[] tokens)
    {
        if (string.IsNullOrEmpty(source) || tokens == null)
        {
            return false;
        }

        for (int i = 0; i < tokens.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(tokens[i]) &&
                source.IndexOf(tokens[i], StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    private static string SanitizeName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Unnamed";
        }

        char[] invalid = Path.GetInvalidFileNameChars();
        string result = value.Trim();
        for (int i = 0; i < invalid.Length; i++)
        {
            result = result.Replace(invalid[i], '_');
        }

        return result.Replace(' ', '_').Replace("__", "_");
    }

    private static string NormalizeSearchText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        value = value.ToLowerInvariant().Replace("__", "_");
        char[] chars = value.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            if (!char.IsLetterOrDigit(chars[i]))
            {
                chars[i] = '_';
            }
        }

        return new string(chars).Trim('_');
    }

    private static int PreviousPowerOfTwo(int value)
    {
        int result = 1;
        while (result * 2 <= value)
        {
            result *= 2;
        }

        return result;
    }

    private static void EnsureFolder(string folder)
    {
        if (string.IsNullOrWhiteSpace(folder) || AssetDatabase.IsValidFolder(folder))
        {
            return;
        }

        string[] parts = folder.Replace('\\', '/').Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }

    private static string ToFullPath(string assetPath)
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName.Replace('\\', '/');
        return $"{projectRoot}/{assetPath.Replace('\\', '/')}";
    }

    private static string ToAssetPath(string fullPath)
    {
        string normalized = fullPath.Replace('\\', '/');
        int index = normalized.IndexOf("/Assets/", StringComparison.OrdinalIgnoreCase);
        return index >= 0 ? normalized.Substring(index + 1) : normalized;
    }

    [Serializable]
    private sealed class GltfRoot
    {
        public GltfImage[] images;
        public GltfBufferView[] bufferViews;
    }

    [Serializable]
    private sealed class GltfImage
    {
        public int bufferView = -1;
        public string mimeType;
    }

    [Serializable]
    private sealed class GltfBufferView
    {
        public int byteOffset;
        public int byteLength;
    }

    private readonly struct ModelSpec
    {
        public ModelSpec(
            string key,
            string displayName,
            string sourceModelPath,
            string targetModelRelativePath,
            string[] sourcePackagePaths,
            string[] creditPaths,
            bool treeLike,
            int maxOutlineRenderers)
        {
            Key = key;
            DisplayName = displayName;
            SourceModelPath = sourceModelPath;
            TargetModelRelativePath = targetModelRelativePath;
            SourcePackagePaths = sourcePackagePaths;
            CreditPaths = creditPaths;
            TreeLike = treeLike;
            MaxOutlineRenderers = maxOutlineRenderers;
        }

        public string Key { get; }
        public string DisplayName { get; }
        public string SourceModelPath { get; }
        public string TargetModelRelativePath { get; }
        public string[] SourcePackagePaths { get; }
        public string[] CreditPaths { get; }
        public bool TreeLike { get; }
        public int MaxOutlineRenderers { get; }
    }

    private readonly struct MaterialDescriptor
    {
        public MaterialDescriptor(MaterialKind kind, RendererCategory category, string categoryName)
        {
            Kind = kind;
            Category = category;
            CategoryName = categoryName;
        }

        public MaterialKind Kind { get; }
        public RendererCategory Category { get; }
        public string CategoryName { get; }
    }

    private readonly struct SakuraPalette
    {
        public SakuraPalette(float hue, float saturation, float value)
        {
            Hue = hue;
            Saturation = saturation;
            Value = value;
        }

        public float Hue { get; }
        public float Saturation { get; }
        public float Value { get; }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "H={0:0.000}, S={1:0.000}, V={2:0.000}", Hue, Saturation, Value);
        }
    }

    private enum MaterialKind
    {
        Default,
        Wood,
        Lacquer,
        Stone,
        Vegetation,
        SakuraPetal,
        Transparent,
        Emissive
    }

    private enum RendererCategory
    {
        Outlined,
        Unoutlined,
        Transparent
    }

    private enum TextureRole
    {
        BaseColor,
        Emission
    }
}
