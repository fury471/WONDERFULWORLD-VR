using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class TerrainGrassDetailInstaller
{
    private const string WonderlandParkScenePath = "Assets/_Project/World/Persistent/World_WonderlandPark.unity";

    private const float PerformanceDetailDistance = 38f;
    private const float PerformanceDetailDensity = 0.62f;

    private static readonly GrassDetailInstallSpec[] GrassDetails =
    {
        new GrassDetailInstallSpec(
            "Assets/_Project/World/Shared/Vegetation/Grass/Prefabs/WW_Grass_Detail_ReferenceMeadow_Lush.prefab",
            0.58f,
            1.28f,
            0.3f,
            0.6f,
            0.95f,
            0.16f),
        new GrassDetailInstallSpec(
            "Assets/_Project/World/Shared/Vegetation/Grass/Prefabs/WW_Grass_Detail_ReferenceMeadow_Mixed.prefab",
            0.5f,
            1.18f,
            0.3f,
            0.6f,
            1.25f,
            0.13f),
        new GrassDetailInstallSpec(
            "Assets/_Project/World/Shared/Vegetation/Grass/Prefabs/WW_Grass_Detail_ReferenceMeadow_WarmAccent.prefab",
            0.42f,
            1.04f,
            0.3f,
            0.6f,
            1.65f,
            0.08f),
        new GrassDetailInstallSpec(
            "Assets/_Project/World/Shared/Vegetation/Flowers/Prefabs/WW_Flower_Detail_GoldenCluster.prefab",
            0.44f,
            0.44f,
            0.5f,
            0.5f,
            1.45f,
            0.05f),
        new GrassDetailInstallSpec(
            "Assets/_Project/World/Shared/Vegetation/Flowers/Prefabs/WW_Flower_Detail_WhiteDaisy.prefab",
            0.5f,
            0.5f,
            0.58f,
            0.58f,
            1.55f,
            0.045f),
        new GrassDetailInstallSpec(
            "Assets/_Project/World/Shared/Vegetation/Flowers/Prefabs/WW_Flower_Detail_RedPoppy.prefab",
            0.56f,
            0.56f,
            0.76f,
            0.76f,
            1.7f,
            0.04f),
        new GrassDetailInstallSpec(
            "Assets/_Project/World/Shared/Vegetation/Flowers/Prefabs/WW_GroundPetals_Detail.prefab",
            1.1f,
            1.85f,
            1f,
            1f,
            2.4f,
            0f),
    };

    private static readonly string[] LegacyGrassPrefabPaths =
    {
        "Assets/_Project/World/Shared/Vegetation/Grass/Prefabs/WW_Grass_Detail_01A.prefab",
        "Assets/_Project/World/Shared/Vegetation/Grass/Prefabs/WW_Grass_Detail_01B.prefab",
        "Assets/_Project/World/Shared/Vegetation/Grass/Prefabs/WW_Grass_Detail_02A.prefab",
        "Assets/_Project/World/Shared/Vegetation/Grass/Prefabs/WW_Grass_Detail_02D.prefab",
    };

    [MenuItem("Wonderland/World/Install Toon Grass Detail Prototypes")]
    public static void InstallToonGrassDetailPrototypes()
    {
        InstallToonVegetationDetailPrototypes();
    }

    [MenuItem("Wonderland/World/Install Toon Vegetation Detail Prototypes")]
    public static void InstallToonVegetationDetailPrototypes()
    {
        Terrain[] terrains = Terrain.activeTerrains;
        if (terrains == null || terrains.Length == 0)
        {
            Debug.LogWarning("TerrainGrassDetailInstaller: no active terrains found in the current scene.");
            return;
        }

        GrassDetailInstallSpec[] grassDetails = LoadGrassDetails();
        if (grassDetails.Length == 0)
        {
            Debug.LogError("TerrainGrassDetailInstaller: no vegetation detail prefabs could be loaded.");
            return;
        }

        Dictionary<GameObject, int> legacyGrassRemap = LoadLegacyGrassRemap();
        HashSet<TerrainData> edited = new();
        for (int i = 0; i < terrains.Length; i++)
        {
            Terrain terrain = terrains[i];
            if (terrain == null || terrain.terrainData == null || !edited.Add(terrain.terrainData))
            {
                continue;
            }

            Undo.RegisterCompleteObjectUndo(terrain.terrainData, "Install Toon Vegetation Details");
            Undo.RecordObject(terrain, "Install Toon Vegetation Details");
            ConfigureTerrainForDenseStylizedGrass(terrain);
            InstallOnTerrainData(terrain.terrainData, grassDetails, legacyGrassRemap);
            SyncTerrainDataNameWithAssetFilename(terrain.terrainData);
            bool cleanedInvalidTrees = CleanInvalidTreePrototypes(terrain.terrainData, out int invalidTreeCount);
            EditorUtility.SetDirty(terrain.terrainData);
            EditorUtility.SetDirty(terrain);

            if (cleanedInvalidTrees)
            {
                Debug.Log(
                    $"TerrainGrassDetailInstaller: removed {invalidTreeCount} invalid Terrain tree prototypes from {terrain.terrainData.name}. " +
                    "These prefabs had no valid MeshRenderer and could not be instanced by Terrain.");
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log(
            $"TerrainGrassDetailInstaller: installed {grassDetails.Length} performance vegetation detail prototypes on {edited.Count} TerrainData assets. " +
            $"Terrain detail distance={PerformanceDetailDistance}, density={PerformanceDetailDensity}.");
    }

    [MenuItem("Wonderland/World/Install Toon Grass Detail Prototypes In Wonderland Park")]
    public static void InstallToonGrassDetailPrototypesInWonderlandPark()
    {
        InstallToonVegetationDetailPrototypesInWonderlandPark();
    }

    [MenuItem("Wonderland/World/Install Toon Vegetation Detail Prototypes In Wonderland Park")]
    public static void InstallToonVegetationDetailPrototypesInWonderlandPark()
    {
        EditorSceneManager.OpenScene(WonderlandParkScenePath);
        InstallToonVegetationDetailPrototypes();
        EditorSceneManager.SaveOpenScenes();
    }

    [MenuItem("Wonderland/World/Clean Missing Terrain Prototypes")]
    public static void CleanMissingTerrainPrototypes()
    {
        Terrain[] terrains = Terrain.activeTerrains;
        if (terrains == null || terrains.Length == 0)
        {
            Debug.LogWarning("TerrainGrassDetailInstaller: no active terrains found in the current scene.");
            return;
        }

        int editedCount = 0;
        int removedDetails = 0;
        int removedTrees = 0;
        HashSet<TerrainData> edited = new();
        for (int i = 0; i < terrains.Length; i++)
        {
            Terrain terrain = terrains[i];
            if (terrain == null || terrain.terrainData == null || !edited.Add(terrain.terrainData))
            {
                continue;
            }

            Undo.RegisterCompleteObjectUndo(terrain.terrainData, "Clean Missing Terrain Prototypes");
            bool changed = false;
            changed |= CleanMissingDetailPrototypes(terrain.terrainData, out int detailCount);
            changed |= CleanInvalidTreePrototypes(terrain.terrainData, out int treeCount);

            if (changed)
            {
                removedDetails += detailCount;
                removedTrees += treeCount;
                editedCount++;
                EditorUtility.SetDirty(terrain.terrainData);
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log(
            $"TerrainGrassDetailInstaller: cleaned {removedDetails} missing detail prototypes and {removedTrees} missing or invalid tree prototypes across {editedCount} TerrainData assets.");
    }

    public static void CleanMissingTerrainPrototypesInWonderlandPark()
    {
        EditorSceneManager.OpenScene(WonderlandParkScenePath);
        CleanMissingTerrainPrototypes();
        EditorSceneManager.SaveOpenScenes();
    }

    private static GrassDetailInstallSpec[] LoadGrassDetails()
    {
        List<GrassDetailInstallSpec> details = new();
        for (int i = 0; i < GrassDetails.Length; i++)
        {
            GrassDetailInstallSpec detail = GrassDetails[i];
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(detail.PrefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"TerrainGrassDetailInstaller: missing grass prefab at {detail.PrefabPath}.");
                continue;
            }

            details.Add(detail.WithPrefab(prefab));
        }

        return details.ToArray();
    }

    private static Dictionary<GameObject, int> LoadLegacyGrassRemap()
    {
        Dictionary<GameObject, int> remap = new();
        for (int i = 0; i < LegacyGrassPrefabPaths.Length; i++)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LegacyGrassPrefabPaths[i]);
            if (prefab == null)
            {
                continue;
            }

            remap[prefab] = i switch
            {
                0 => 0, // Old 01A becomes ReferenceMeadow_Lush.
                1 => 0, // Old 01B becomes ReferenceMeadow_Lush.
                2 => 1, // Old 02A becomes ReferenceMeadow_Mixed.
                3 => 2, // Old 02D becomes ReferenceMeadow_WarmAccent.
                _ => 0,
            };
        }

        return remap;
    }

    private static void ConfigureTerrainForDenseStylizedGrass(Terrain terrain)
    {
        terrain.detailObjectDistance = PerformanceDetailDistance;
        terrain.detailObjectDensity = PerformanceDetailDensity;
    }

    private static void InstallOnTerrainData(
        TerrainData terrainData,
        IReadOnlyList<GrassDetailInstallSpec> grassDetails,
        IReadOnlyDictionary<GameObject, int> legacyGrassRemap)
    {
        DetailPrototype[] oldPrototypes = terrainData.detailPrototypes ?? System.Array.Empty<DetailPrototype>();
        bool canMigrateLayers = terrainData.detailWidth > 0 && terrainData.detailHeight > 0;

        List<DetailPrototype> prototypes = new();
        List<int[,]> layers = new();
        List<int[,]> pendingGrassLayers = new(grassDetails.Count);
        for (int i = 0; i < grassDetails.Count; i++)
        {
            pendingGrassLayers.Add(null);
        }

        for (int i = 0; i < oldPrototypes.Length; i++)
        {
            DetailPrototype oldPrototype = oldPrototypes[i];
            int targetGrassIndex = FindGrassDetailIndex(grassDetails, oldPrototype?.prototype);
            if (targetGrassIndex < 0 && oldPrototype != null && legacyGrassRemap.TryGetValue(oldPrototype.prototype, out int legacyTarget))
            {
                targetGrassIndex = legacyTarget;
            }

            if (targetGrassIndex >= 0)
            {
                if (canMigrateLayers)
                {
                    int[,] pendingLayer = pendingGrassLayers[targetGrassIndex];
                    MergeLayerInto(ref pendingLayer, terrainData.GetDetailLayer(0, 0, terrainData.detailWidth, terrainData.detailHeight, i));
                    pendingGrassLayers[targetGrassIndex] = pendingLayer;
                }

                continue;
            }

            prototypes.Add(oldPrototype);
            if (canMigrateLayers)
            {
                layers.Add(terrainData.GetDetailLayer(0, 0, terrainData.detailWidth, terrainData.detailHeight, i));
            }
        }

        for (int i = 0; i < grassDetails.Count; i++)
        {
            prototypes.Add(CreateGrassPrototype(grassDetails[i]));
            if (canMigrateLayers)
            {
                layers.Add(pendingGrassLayers[i] ?? new int[terrainData.detailHeight, terrainData.detailWidth]);
            }
        }

        terrainData.detailPrototypes = prototypes.ToArray();
        if (!canMigrateLayers)
        {
            return;
        }

        for (int i = 0; i < layers.Count; i++)
        {
            terrainData.SetDetailLayer(0, 0, i, layers[i]);
        }
    }

    private static int FindGrassDetailIndex(IReadOnlyList<GrassDetailInstallSpec> grassDetails, GameObject prefab)
    {
        if (prefab == null)
        {
            return -1;
        }

        for (int i = 0; i < grassDetails.Count; i++)
        {
            if (grassDetails[i].Prefab == prefab)
            {
                return i;
            }
        }

        return -1;
    }

    private static void MergeLayerInto(ref int[,] target, int[,] source)
    {
        if (source == null)
        {
            return;
        }

        if (target == null)
        {
            target = source;
            return;
        }

        int height = Mathf.Min(target.GetLength(0), source.GetLength(0));
        int width = Mathf.Min(target.GetLength(1), source.GetLength(1));
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                target[y, x] = Mathf.Max(target[y, x], source[y, x]);
            }
        }
    }

    private static void SyncTerrainDataNameWithAssetFilename(TerrainData terrainData)
    {
        string assetPath = AssetDatabase.GetAssetPath(terrainData);
        if (string.IsNullOrEmpty(assetPath))
        {
            return;
        }

        string filename = Path.GetFileNameWithoutExtension(assetPath);
        if (!string.IsNullOrEmpty(filename) && terrainData.name != filename)
        {
            terrainData.name = filename;
        }
    }

    private static DetailPrototype CreateGrassPrototype(GrassDetailInstallSpec detail)
    {
        DetailPrototype prototype = new()
        {
            prototype = detail.Prefab,
            usePrototypeMesh = true,
            renderMode = DetailRenderMode.VertexLit,
            minWidth = detail.MinWidth,
            maxWidth = detail.MaxWidth,
            minHeight = detail.MinHeight,
            maxHeight = detail.MaxHeight,
            noiseSpread = detail.NoiseSpread,
            healthyColor = Color.white,
            dryColor = Color.white,
        };

#if !UNITY_6000_0_OR_NEWER
        prototype.bendFactor = detail.BendFactor;
#endif

#if UNITY_2022_2_OR_NEWER
        prototype.useInstancing = true;
#endif

        return prototype;
    }

    private readonly struct GrassDetailInstallSpec
    {
        public GrassDetailInstallSpec(
            string prefabPath,
            float minWidth,
            float maxWidth,
            float minHeight,
            float maxHeight,
            float noiseSpread,
            float bendFactor)
            : this(prefabPath, null, minWidth, maxWidth, minHeight, maxHeight, noiseSpread, bendFactor)
        {
        }

        private GrassDetailInstallSpec(
            string prefabPath,
            GameObject prefab,
            float minWidth,
            float maxWidth,
            float minHeight,
            float maxHeight,
            float noiseSpread,
            float bendFactor)
        {
            PrefabPath = prefabPath;
            Prefab = prefab;
            MinWidth = minWidth;
            MaxWidth = maxWidth;
            MinHeight = minHeight;
            MaxHeight = maxHeight;
            NoiseSpread = noiseSpread;
            BendFactor = bendFactor;
        }

        public string PrefabPath { get; }
        public GameObject Prefab { get; }
        public float MinWidth { get; }
        public float MaxWidth { get; }
        public float MinHeight { get; }
        public float MaxHeight { get; }
        public float NoiseSpread { get; }
        public float BendFactor { get; }

        public GrassDetailInstallSpec WithPrefab(GameObject prefab)
        {
            return new GrassDetailInstallSpec(PrefabPath, prefab, MinWidth, MaxWidth, MinHeight, MaxHeight, NoiseSpread, BendFactor);
        }
    }

    private static bool CleanMissingDetailPrototypes(TerrainData terrainData, out int removedCount)
    {
        removedCount = 0;
        DetailPrototype[] oldPrototypes = terrainData.detailPrototypes;
        if (oldPrototypes == null || oldPrototypes.Length == 0)
        {
            return false;
        }

        int detailWidth = terrainData.detailWidth;
        int detailHeight = terrainData.detailHeight;
        List<DetailPrototype> keptPrototypes = new();
        List<int[,]> keptLayers = new();
        for (int i = 0; i < oldPrototypes.Length; i++)
        {
            DetailPrototype prototype = oldPrototypes[i];
            if (!IsValidDetailPrototype(prototype))
            {
                removedCount++;
                continue;
            }

            keptPrototypes.Add(prototype);
            keptLayers.Add(terrainData.GetDetailLayer(0, 0, detailWidth, detailHeight, i));
        }

        if (removedCount == 0)
        {
            return false;
        }

        terrainData.detailPrototypes = keptPrototypes.ToArray();
        for (int i = 0; i < keptLayers.Count; i++)
        {
            terrainData.SetDetailLayer(0, 0, i, keptLayers[i]);
        }

        return true;
    }

    private static bool IsValidDetailPrototype(DetailPrototype prototype)
    {
        if (prototype == null)
        {
            return false;
        }

        if (prototype.usePrototypeMesh)
        {
            return IsValidDetailMeshPrototype(prototype.prototype);
        }

        return prototype.prototypeTexture != null;
    }

    private static bool IsValidDetailMeshPrototype(GameObject prefab)
    {
        if (prefab == null)
        {
            return false;
        }

        MeshFilter meshFilter = prefab.GetComponentInChildren<MeshFilter>(includeInactive: true);
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            return false;
        }

        Renderer renderer = prefab.GetComponentInChildren<Renderer>(includeInactive: true);
        if (renderer == null)
        {
            return false;
        }

        Material[] materials = renderer.sharedMaterials;
        if (materials == null || materials.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < materials.Length; i++)
        {
            if (materials[i] != null && materials[i].shader != null)
            {
                return true;
            }
        }

        return false;
    }

    private static bool CleanInvalidTreePrototypes(TerrainData terrainData, out int removedCount)
    {
        removedCount = 0;
        TreePrototype[] oldPrototypes = terrainData.treePrototypes;
        if (oldPrototypes == null || oldPrototypes.Length == 0)
        {
            return false;
        }

        Dictionary<int, int> remap = new();
        List<TreePrototype> keptPrototypes = new();
        for (int i = 0; i < oldPrototypes.Length; i++)
        {
            TreePrototype prototype = oldPrototypes[i];
            if (!IsValidTreePrototype(prototype))
            {
                removedCount++;
                continue;
            }

            remap[i] = keptPrototypes.Count;
            keptPrototypes.Add(prototype);
        }

        if (removedCount == 0)
        {
            return false;
        }

        List<TreeInstance> keptTrees = new();
        TreeInstance[] oldTrees = terrainData.treeInstances;
        for (int i = 0; i < oldTrees.Length; i++)
        {
            TreeInstance tree = oldTrees[i];
            if (!remap.TryGetValue(tree.prototypeIndex, out int newIndex))
            {
                continue;
            }

            tree.prototypeIndex = newIndex;
            keptTrees.Add(tree);
        }

        terrainData.treePrototypes = keptPrototypes.ToArray();
        terrainData.treeInstances = keptTrees.ToArray();
        return true;
    }

    private static bool IsValidTreePrototype(TreePrototype prototype)
    {
        if (prototype == null || prototype.prefab == null)
        {
            return false;
        }

        MeshRenderer renderer = prototype.prefab.GetComponentInChildren<MeshRenderer>(includeInactive: true);
        if (renderer == null)
        {
            return false;
        }

        MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            return false;
        }

        Material[] materials = renderer.sharedMaterials;
        if (materials == null || materials.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < materials.Length; i++)
        {
            if (materials[i] != null && materials[i].shader != null)
            {
                return true;
            }
        }

        return false;
    }
}
