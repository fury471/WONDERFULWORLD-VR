using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class TerrainProductionReferenceTools
{
    private const string ProductionTerrainFolder = "Assets/_Project/World/Persistent/Terrain/Production";
    private const string AutoRepairEditorPrefsKey = "WWP.ProductionTerrainTreePrototypeRepair.20260516";

    [InitializeOnLoadMethod]
    private static void AutoRepairProductionTreePrototypesOnce()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorPrefs.GetBool(AutoRepairEditorPrefsKey, false))
        {
            return;
        }

        EditorApplication.delayCall += () =>
        {
            if (EditorPrefs.GetBool(AutoRepairEditorPrefsKey, false))
            {
                return;
            }

            var fixedAssets = RepairTreePrototypesInFolder(ProductionTerrainFolder);
            if (fixedAssets > 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            EditorPrefs.SetBool(AutoRepairEditorPrefsKey, true);
            Debug.Log($"Production terrain tree prototype auto repair complete. Fixed {fixedAssets} TerrainData asset(s).");
        };
    }

    [MenuItem("Tools/Wonderful World/Terrain/Repair Production Tree Prototypes")]
    public static void RepairProductionTreePrototypes()
    {
        var fixedAssets = RepairTreePrototypesInFolder(ProductionTerrainFolder);
        if (fixedAssets > 0)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        Debug.Log($"Production terrain tree prototype repair complete. Fixed {fixedAssets} TerrainData asset(s).");
    }

    public static void RepairProductionTreePrototypesBatch()
    {
        RepairProductionTreePrototypes();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorApplication.Exit(0);
    }

    private static int RepairTreePrototypesInFolder(string folder)
    {
        if (!AssetDatabase.IsValidFolder(folder))
        {
            Debug.LogWarning($"Terrain production folder not found: {folder}");
            return 0;
        }

        var fixedAssets = 0;
        var guids = AssetDatabase.FindAssets("t:TerrainData", new[] { folder });

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var terrainData = AssetDatabase.LoadAssetAtPath<TerrainData>(path);
            if (terrainData == null)
            {
                continue;
            }

            var prototypes = terrainData.treePrototypes;
            if (prototypes == null || prototypes.Length == 0)
            {
                continue;
            }

            var validPrototypes = new List<TreePrototype>(prototypes.Length);
            var oldToNewIndex = new int[prototypes.Length];
            var changed = false;

            for (var i = 0; i < prototypes.Length; i++)
            {
                if (prototypes[i].prefab == null)
                {
                    oldToNewIndex[i] = -1;
                    changed = true;
                    Debug.LogWarning($"Removed missing tree prefab at index {i} from {path}.");
                    continue;
                }

                oldToNewIndex[i] = validPrototypes.Count;
                validPrototypes.Add(prototypes[i]);
            }

            if (!changed)
            {
                continue;
            }

            var treeInstances = terrainData.treeInstances;
            var validInstances = new List<TreeInstance>(treeInstances.Length);

            foreach (var treeInstance in treeInstances)
            {
                if (treeInstance.prototypeIndex < 0 || treeInstance.prototypeIndex >= oldToNewIndex.Length)
                {
                    continue;
                }

                var newIndex = oldToNewIndex[treeInstance.prototypeIndex];
                if (newIndex < 0)
                {
                    continue;
                }

                var remappedTreeInstance = treeInstance;
                remappedTreeInstance.prototypeIndex = newIndex;
                validInstances.Add(remappedTreeInstance);
            }

            terrainData.treeInstances = validInstances.ToArray();
            terrainData.treePrototypes = validPrototypes.ToArray();
            EditorUtility.SetDirty(terrainData);
            fixedAssets++;
        }

        return fixedAssets;
    }
}
