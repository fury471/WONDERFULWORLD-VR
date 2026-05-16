using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CherryGardenRegionBuilder
{
    private const string SandboxScenePath = "Assets/_Project/Sandbox/Wenao/World_WonderlandPark_M3_YuFu.unity";
    private const string SandboxRegionRootName = "Region_CherryGarden_Wenao";
    private const string RegionRootName = "Region_CherryGarden";
    private const string RegionPrefabPath = "Assets/_Project/World/Regions/CherryGarden/CherryGarden.prefab";
    private const string WorldRegionsRootName = "World_Regions";

    [MenuItem("Wonderland/Cherry Garden/Rebuild Cherry Garden Region Prefab")]
    public static void RebuildRegionPrefab()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        string previousScenePath = SceneManager.GetActiveScene().path;
        Scene sandboxScene = default;
        try
        {
            sandboxScene = EditorSceneManager.OpenScene(SandboxScenePath, OpenSceneMode.Single);
            GameObject regionRoot = FindRootObject(sandboxScene, SandboxRegionRootName);
            if (regionRoot == null)
            {
                Debug.LogError($"Could not find {SandboxRegionRootName} in {SandboxScenePath}.");
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(RegionPrefabPath));
            regionRoot.name = RegionRootName;
            EnsureProductionComponents(regionRoot);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(regionRoot, RegionPrefabPath, out bool success);
            if (!success || prefab == null)
            {
                Debug.LogError($"Failed to save cherry garden region prefab at {RegionPrefabPath}.");
                return;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Rebuilt cherry garden region prefab: {RegionPrefabPath}", prefab);
        }
        finally
        {
            bool hasPreviousScene = !string.IsNullOrEmpty(previousScenePath);
            bool openedDifferentScene = sandboxScene.IsValid() && previousScenePath != sandboxScene.path;

            if (hasPreviousScene && openedDifferentScene)
            {
                EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single);
            }
        }
    }

    [MenuItem("Wonderland/Cherry Garden/Place Cherry Garden Region Prefab In Active Scene")]
    public static void PlaceRegionPrefabInActiveScene()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RegionPrefabPath);
        if (prefab == null)
        {
            Debug.LogError($"Cherry garden prefab does not exist yet. Run '{nameof(RebuildRegionPrefab)}' first.");
            return;
        }

        Transform parent = GameObject.Find(WorldRegionsRootName)?.transform;
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        if (instance == null)
        {
            Debug.LogError($"Failed to instantiate {RegionPrefabPath}.");
            return;
        }

        Undo.RegisterCreatedObjectUndo(instance, "Place Cherry Garden Region");
        instance.name = RegionRootName;
        EnsureProductionComponents(instance);
        if (parent != null)
        {
            Undo.SetTransformParent(instance.transform, parent, "Parent Cherry Garden Region");
        }

        Selection.activeGameObject = instance;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    private static GameObject FindRootObject(Scene scene, string rootName)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            GameObject result = FindInChildren(roots[i].transform, rootName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    private static void EnsureProductionComponents(GameObject regionRoot)
    {
        if (regionRoot.GetComponent<CherryGardenToonOutlineController>() == null)
        {
            regionRoot.AddComponent<CherryGardenToonOutlineController>();
        }
    }

    private static GameObject FindInChildren(Transform parent, string targetName)
    {
        if (parent.name == targetName)
        {
            return parent.gameObject;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            GameObject result = FindInChildren(parent.GetChild(i), targetName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }
}
