using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class TerrainGrassDetailInstaller
{
    private const string WonderlandParkScenePath = "Assets/_Project/World/Persistent/World_WonderlandPark.unity";

    private static readonly string[] GrassPrefabPaths =
    {
        "Assets/_Project/World/Shared/Vegetation/Grass/Prefabs/WW_Grass_Detail_01A.prefab",
        "Assets/_Project/World/Shared/Vegetation/Grass/Prefabs/WW_Grass_Detail_01B.prefab",
        "Assets/_Project/World/Shared/Vegetation/Grass/Prefabs/WW_Grass_Detail_02A.prefab",
        "Assets/_Project/World/Shared/Vegetation/Grass/Prefabs/WW_Grass_Detail_02D.prefab",
    };

    [MenuItem("Wonderland/World/Install Toon Grass Detail Prototypes")]
    public static void InstallToonGrassDetailPrototypes()
    {
        Terrain[] terrains = Terrain.activeTerrains;
        if (terrains == null || terrains.Length == 0)
        {
            Debug.LogWarning("TerrainGrassDetailInstaller: no active terrains found in the current scene.");
            return;
        }

        GameObject[] grassPrefabs = LoadGrassPrefabs();
        if (grassPrefabs.Length == 0)
        {
            Debug.LogError("TerrainGrassDetailInstaller: no grass prefabs could be loaded.");
            return;
        }

        HashSet<TerrainData> edited = new();
        for (int i = 0; i < terrains.Length; i++)
        {
            Terrain terrain = terrains[i];
            if (terrain == null || terrain.terrainData == null || !edited.Add(terrain.terrainData))
            {
                continue;
            }

            Undo.RegisterCompleteObjectUndo(terrain.terrainData, "Install Toon Grass Details");
            InstallOnTerrainData(terrain.terrainData, grassPrefabs);
            EditorUtility.SetDirty(terrain.terrainData);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"TerrainGrassDetailInstaller: installed {grassPrefabs.Length} grass detail prototypes on {edited.Count} TerrainData assets.");
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
            changed |= CleanMissingTreePrototypes(terrain.terrainData, out int treeCount);

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
            $"TerrainGrassDetailInstaller: cleaned {removedDetails} missing detail prototypes and {removedTrees} missing tree prototypes across {editedCount} TerrainData assets.");
    }

    public static void CleanMissingTerrainPrototypesInWonderlandPark()
    {
        EditorSceneManager.OpenScene(WonderlandParkScenePath);
        CleanMissingTerrainPrototypes();
        EditorSceneManager.SaveOpenScenes();
    }

    private static GameObject[] LoadGrassPrefabs()
    {
        List<GameObject> prefabs = new();
        for (int i = 0; i < GrassPrefabPaths.Length; i++)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GrassPrefabPaths[i]);
            if (prefab == null)
            {
                Debug.LogWarning($"TerrainGrassDetailInstaller: missing grass prefab at {GrassPrefabPaths[i]}.");
                continue;
            }

            prefabs.Add(prefab);
        }

        return prefabs.ToArray();
    }

    private static void InstallOnTerrainData(TerrainData terrainData, IReadOnlyList<GameObject> grassPrefabs)
    {
        List<DetailPrototype> prototypes = new(terrainData.detailPrototypes ?? System.Array.Empty<DetailPrototype>());
        for (int i = 0; i < grassPrefabs.Count; i++)
        {
            GameObject prefab = grassPrefabs[i];
            if (HasPrototype(prototypes, prefab))
            {
                continue;
            }

            prototypes.Add(CreateGrassPrototype(prefab));
        }

        terrainData.detailPrototypes = prototypes.ToArray();
    }

    private static bool HasPrototype(IReadOnlyList<DetailPrototype> prototypes, GameObject prefab)
    {
        for (int i = 0; i < prototypes.Count; i++)
        {
            if (prototypes[i] != null && prototypes[i].prototype == prefab)
            {
                return true;
            }
        }

        return false;
    }

    private static DetailPrototype CreateGrassPrototype(GameObject prefab)
    {
        DetailPrototype prototype = new()
        {
            prototype = prefab,
            usePrototypeMesh = true,
            renderMode = DetailRenderMode.VertexLit,
            minWidth = 0.75f,
            maxWidth = 1.35f,
            minHeight = 0.75f,
            maxHeight = 1.35f,
            noiseSpread = 0.7f,
            bendFactor = 0.25f,
            healthyColor = Color.white,
            dryColor = Color.white,
        };

#if UNITY_2022_2_OR_NEWER
        prototype.useInstancing = true;
#endif

        return prototype;
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

    private static bool CleanMissingTreePrototypes(TerrainData terrainData, out int removedCount)
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
            if (prototype == null || prototype.prefab == null)
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
}
