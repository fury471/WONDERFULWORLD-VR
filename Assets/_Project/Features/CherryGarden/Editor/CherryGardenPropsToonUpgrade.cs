using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class CherryGardenPropsToonUpgrade
{
    private const string PropMaterialsPath = "Assets/_Project/Features/CherryGarden/Art/Props/Materials";
    private const string PropPrefabsPath = "Assets/_Project/Features/CherryGarden/Art/Props/Prefabs";
    private const string PropGeneratedTexturesPath = "Assets/_Project/Features/CherryGarden/Art/Props/Textures/ToonGenerated";
    private const string PergolaMaterialsPath = "Assets/_Project/Art/Props/StylizedVinePergola/Materials";
    private const string PergolaPrefabsPath = "Assets/_Project/Art/Props/StylizedVinePergola/Prefabs";
    private const string PergolaGeneratedTexturesPath = "Assets/_Project/Art/Props/StylizedVinePergola/Materials/ToonGenerated";
    private const string GardenVegetationOutlinedPath = "Assets/_Project/Features/CherryGarden/Art/Vegetation/Textures/ToonGenerated/GardenVegetation_ToonOutlined.png";
    private const string ToonShaderName = "Wonderland/Props/Toon Band Lit URP";
    private const string OutlineShaderName = "Wonderland/CherryGarden/Toon Outline URP";

    [MenuItem("Wonderland/Cherry Garden/Upgrade Props To Toon Shading")]
    public static void Upgrade()
    {
        Shader toonShader = Shader.Find(ToonShaderName);
        Shader outlineShader = Shader.Find(OutlineShaderName);
        if (toonShader == null || outlineShader == null)
        {
            throw new InvalidOperationException("Missing toon or outline shader. Reimport shader assets and try again.");
        }

        AssetDatabase.StartAssetEditing();
        try
        {
            EnsureFolder(PropGeneratedTexturesPath);

            Material propOutline = EnsureOutlineMaterial(
                $"{PropMaterialsPath}/CherryGardenProp_BlackOutline.mat",
                outlineShader,
                0.0125f);

            UpgradeMaterials(PropMaterialsPath, PropGeneratedTexturesPath, toonShader);
            UpgradePrefabs(propOutline);
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        Debug.Log("CherryGarden props upgraded to toon band shading with black outlines.");
    }

    private static void UpgradeMaterials(string materialFolder, string generatedTextureFolder, Shader toonShader)
    {
        string[] materialGuids = AssetDatabase.FindAssets("t:Material", new[] { materialFolder });
        foreach (string materialGuid in materialGuids)
        {
            string materialPath = AssetDatabase.GUIDToAssetPath(materialGuid);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null || material.name.IndexOf("Outline", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                continue;
            }

            Texture baseTexture = GetTexture(material, "_BaseMap", "_MainTex");
            Texture emissionTexture = GetTexture(material, "_EmissionMap");
            Color baseColor = GetColor(material, "_BaseColor", "_Color", Color.white);
            Color emissionColor = GetColor(material, "_EmissionColor", null, Color.black);
            bool hadEmission = emissionTexture != null || emissionColor.maxColorComponent > 0.001f;
            float cutoff = GetFloat(material, "_Cutoff", 0.5f);
            bool alphaClip = GetFloat(material, "_AlphaClip", 0f) > 0.5f || NameSuggestsCutout(material.name);

            bool isPergolaFoliage = material.name.IndexOf("VinePergola_Foliage", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isPergolaBlossom = material.name.IndexOf("VinePergola_Blossom", StringComparison.OrdinalIgnoreCase) >= 0;
            if (isPergolaFoliage || isPergolaBlossom)
            {
                ApplyPergolaVegetationMaterial(material, toonShader, isPergolaBlossom);
                continue;
            }

            if (material.name.IndexOf("VinePergola_Bark", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Texture2D barkToonTexture = StylizeTexture(baseTexture as Texture2D, generatedTextureFolder, material.name);
                ApplyPergolaBarkMaterial(material, toonShader, barkToonTexture);
                continue;
            }

            Texture2D toonTexture = StylizeTexture(baseTexture as Texture2D, generatedTextureFolder, material.name);
            Color average = toonTexture != null ? MultiplyColor(EstimateAverageColor(toonTexture), baseColor) : baseColor;
            Color.RGBToHSV(average, out float hue, out float saturation, out float value);
            Color shadow = Color.HSVToRGB(hue, Mathf.Clamp01(saturation * 1.08f), Mathf.Clamp01(value * 0.58f));
            Color highlight = Color.HSVToRGB(hue, Mathf.Clamp01(saturation * 0.74f), Mathf.Clamp01(value * 1.22f + 0.08f));

            material.shader = toonShader;
            if (toonTexture != null)
            {
                material.SetTexture("_BaseMap", toonTexture);
            }

            material.SetColor("_BaseColor", Color.white);
            material.SetColor("_ShadowColor", new Color(shadow.r, shadow.g, shadow.b, 1f));
            material.SetColor("_HighlightColor", new Color(highlight.r, highlight.g, highlight.b, 1f));
            material.SetFloat("_RampThreshold", NameSuggestsStone(material.name) ? 0.55f : 0.48f);
            material.SetFloat("_RampSoftness", 0.018f);
            material.SetFloat("_AmbientStrength", NameSuggestsLantern(material.name) ? 0.5f : 0.38f);
            material.SetFloat("_Cutoff", cutoff);
            material.SetFloat("_AlphaClip", alphaClip ? 1f : 0f);
            material.SetFloat("_Cull", 2f);

            if (hadEmission && emissionTexture != null)
            {
                material.SetTexture("_EmissionMap", emissionTexture);
                material.SetColor("_EmissionColor", emissionColor.maxColorComponent > 0.001f ? emissionColor : Color.white);
                material.SetFloat("_EmissionStrength", NameSuggestsLantern(material.name) ? 1.15f : 0.65f);
                material.EnableKeyword("_EMISSION");
            }
            else
            {
                material.SetFloat("_EmissionStrength", 0f);
                material.DisableKeyword("_EMISSION");
            }

            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
        }
    }

    private static void ApplyPergolaVegetationMaterial(Material material, Shader toonShader, bool blossomOrFruit)
    {
        Texture2D outlinedAtlas = AssetDatabase.LoadAssetAtPath<Texture2D>(GardenVegetationOutlinedPath);
        material.shader = toonShader;
        if (outlinedAtlas != null)
        {
            material.SetTexture("_BaseMap", outlinedAtlas);
            material.SetTexture("_MainTex", outlinedAtlas);
        }

        material.SetColor("_BaseColor", blossomOrFruit ? new Color(1f, 0.94f, 1f, 1f) : Color.white);
        material.SetColor("_ShadowColor", blossomOrFruit ? new Color(0.48f, 0.32f, 0.62f, 1f) : new Color(0.58f, 0.74f, 0.45f, 1f));
        material.SetColor("_HighlightColor", blossomOrFruit ? new Color(1.22f, 0.98f, 1.28f, 1f) : new Color(1.16f, 1.14f, 0.96f, 1f));
        material.SetFloat("_RampThreshold", blossomOrFruit ? 0.36f : 0.38f);
        material.SetFloat("_RampSoftness", 0.026f);
        material.SetFloat("_AmbientStrength", blossomOrFruit ? 0.82f : 0.78f);
        material.SetFloat("_Cutoff", blossomOrFruit ? 0.22f : 0.24f);
        material.SetFloat("_AlphaClip", 1f);
        material.SetFloat("_Cull", 0f);
        material.SetFloat("_EmissionStrength", 0f);
        material.doubleSidedGI = true;
        material.DisableKeyword("_EMISSION");
        material.EnableKeyword("_ALPHATEST_ON");
        material.enableInstancing = true;
        EditorUtility.SetDirty(material);
    }

    private static void ApplyPergolaBarkMaterial(Material material, Shader toonShader, Texture2D toonTexture)
    {
        material.shader = toonShader;
        if (toonTexture != null)
        {
            material.SetTexture("_BaseMap", toonTexture);
            material.SetTexture("_MainTex", toonTexture);
        }

        material.SetColor("_BaseColor", Color.white);
        material.SetColor("_ShadowColor", new Color(0.38f, 0.25f, 0.15f, 1f));
        material.SetColor("_HighlightColor", new Color(0.94f, 0.68f, 0.42f, 1f));
        material.SetFloat("_RampThreshold", 0.42f);
        material.SetFloat("_RampSoftness", 0.024f);
        material.SetFloat("_AmbientStrength", 0.5f);
        material.SetFloat("_Cutoff", 0.324f);
        material.SetFloat("_AlphaClip", 0f);
        material.SetFloat("_Cull", 0f);
        material.SetFloat("_EmissionStrength", 0f);
        material.doubleSidedGI = true;
        material.DisableKeyword("_EMISSION");
        material.DisableKeyword("_ALPHATEST_ON");
        material.enableInstancing = true;
        EditorUtility.SetDirty(material);
    }

    private static Texture2D StylizeTexture(Texture2D source, string outputFolder, string materialName)
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

        string outputPath = $"{outputFolder}/{materialName}_ToonPalette.png";
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
            int width = Mathf.Min(1024, PreviousPowerOfTwo(source.width));
            int height = Mathf.Min(1024, PreviousPowerOfTwo(source.height));
            width = Mathf.Max(64, width);
            height = Mathf.Max(64, height);

            Texture2D output = new(width, height, TextureFormat.RGBA32, true, false);
            Color32[] pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color color = source.GetPixelBilinear((x + 0.5f) / width, (y + 0.5f) / height);
                    Color.RGBToHSV(color, out float h, out float s, out float v);
                    float band = v < 0.34f ? 0.46f : v < 0.68f ? 0.72f : 0.96f;
                    s = Mathf.Clamp01(s * 1.08f);
                    Color stylized = Color.HSVToRGB(h, s, Mathf.Lerp(v, band, 0.76f));
                    stylized.a = color.a;
                    pixels[y * width + x] = stylized;
                }
            }

            output.SetPixels32(pixels);
            output.Apply(updateMipmaps: true, makeNoLongerReadable: false);
            File.WriteAllBytes(outputPath, output.EncodeToPNG());
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
        TextureImporter outputImporter = AssetImporter.GetAtPath(outputPath) as TextureImporter;
        if (outputImporter != null)
        {
            outputImporter.textureType = TextureImporterType.Default;
            outputImporter.sRGBTexture = true;
            outputImporter.alphaSource = TextureImporterAlphaSource.FromInput;
            outputImporter.mipmapEnabled = true;
            outputImporter.npotScale = TextureImporterNPOTScale.ToNearest;
            outputImporter.maxTextureSize = 1024;
            outputImporter.textureCompression = TextureImporterCompression.Compressed;
            outputImporter.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Texture2D>(outputPath);
    }

    private static void UpgradePrefabs(Material propOutline)
    {
        string[] propGuids = AssetDatabase.FindAssets("t:Prefab", new[] { PropPrefabsPath });
        foreach (string guid in propGuids)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                CherryGardenPropToonOutline outline = root.GetComponent<CherryGardenPropToonOutline>();
                if (outline == null)
                {
                    outline = root.AddComponent<CherryGardenPropToonOutline>();
                }

                SerializedObject serialized = new(outline);
                serialized.FindProperty("rebuildOnEnable").boolValue = true;
                serialized.FindProperty("maxOutlineRenderers").intValue = 64;
                serialized.FindProperty("outlineMaterial").objectReferenceValue = propOutline;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>(includeInactive: true);
                for (int i = 0; i < renderers.Length; i++)
                {
                    renderers[i].shadowCastingMode = ShadowCastingMode.On;
                    renderers[i].receiveShadows = true;
                    renderers[i].allowOcclusionWhenDynamic = true;
                }

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }

    private static void ConfigurePergolaOutline(Material outline)
    {
        if (outline == null)
        {
            return;
        }

        outline.SetColor("_OutlineColor", Color.black);
        outline.SetFloat("_OutlineWidth", 0.018f);
        outline.enableInstancing = true;
        EditorUtility.SetDirty(outline);
    }

    private static Material EnsureOutlineMaterial(string path, Shader shader, float width)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        material.shader = shader;
        material.SetColor("_OutlineColor", Color.black);
        material.SetFloat("_OutlineWidth", width);
        material.enableInstancing = true;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void EnsureFolder(string folder)
    {
        string[] parts = folder.Split('/');
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

    private static Texture GetTexture(Material material, params string[] names)
    {
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

    private static Color GetColor(Material material, string first, string second, Color fallback)
    {
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

    private static float GetFloat(Material material, string property, float fallback)
    {
        return material.HasProperty(property) ? material.GetFloat(property) : fallback;
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
            int samples = 0;
            for (int y = 0; y < texture.height; y += Mathf.Max(1, texture.height / 16))
            {
                for (int x = 0; x < texture.width; x += Mathf.Max(1, texture.width / 16))
                {
                    Color color = texture.GetPixel(x, y);
                    total += color * color.a;
                    samples++;
                }
            }

            return samples > 0 ? total / samples : Color.white;
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

    private static int PreviousPowerOfTwo(int value)
    {
        int result = 1;
        while (result * 2 <= value)
        {
            result *= 2;
        }

        return result;
    }

    private static bool NameSuggestsLantern(string name)
    {
        return name.IndexOf("Lantern", StringComparison.OrdinalIgnoreCase) >= 0 ||
               name.IndexOf("WashiLight", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool NameSuggestsStone(string name)
    {
        return name.IndexOf("Stone", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool NameSuggestsCutout(string name)
    {
        return name.IndexOf("Foliage", StringComparison.OrdinalIgnoreCase) >= 0 ||
               name.IndexOf("Blossom", StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
