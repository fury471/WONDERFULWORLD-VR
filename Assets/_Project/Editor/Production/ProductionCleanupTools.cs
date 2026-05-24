using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ProductionCleanupTools
{
    private const string MainScenePath = "Assets/_Project/World/Persistent/World_WonderlandPark.unity";
    private const string AuditReportPath = "Docs/Production_Audit.md";
    private const string AssetReferenceReportPath = "Docs/Asset_Reference_Audit.md";

    private static readonly string[] StandardProjectFolders =
    {
        "Assets/_Project/Scenes",
        "Assets/_Project/Prefabs",
        "Assets/_Project/Materials",
        "Assets/_Project/Textures",
        "Assets/_Project/Models",
        "Assets/_Project/Scripts",
        "Assets/_Project/Audio",
        "Assets/_Project/Animations",
        "Assets/_Project/VFX",
        "Assets/_Project/Settings",
        "Assets/_Project/Editor/Production"
    };

    private static readonly string[] CanonicalRootOrder =
    {
        "GlobalSystem",
        "XR",
        "Lighting",
        "Terrain",
        "World_Regions",
        "Decorations",
        "UI",
        "Debug"
    };

    private static readonly string[] RequiredRootNames = CanonicalRootOrder
        .Where(rootName => rootName != "Debug")
        .ToArray();

    private static readonly string[] CleanupCandidateFolders =
    {
        "Assets/_TempArt",
        "Assets/_Recovery",
        "Assets/Scenes",
        "Assets/Scripts"
    };

    private static readonly string[] DecorativeRootPrefixes =
    {
        "TFF_",
        "SM_",
        "P_",
        "Rock",
        "Tree",
        "Flower",
        "Grass",
        "Bush",
        "Stone",
        "Water",
        "Bridge",
        "Pavilion"
    };

    private static readonly string[] ProductionDebugFlagNames =
    {
        "enableDebugLog",
        "logDebug",
        "logDebugMessages"
    };

    [MenuItem("Wonderful World/Production/Create Standard Project Folders")]
    public static void CreateStandardProjectFolders()
    {
        foreach (string folder in StandardProjectFolders)
        {
            EnsureFolder(folder);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ProductionCleanup] Standard project folders are present.");
    }

    [MenuItem("Wonderful World/Production/Generate Production Audit")]
    public static void GenerateProductionAudit()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        CreateStandardProjectFolders();

        Scene previousScene = SceneManager.GetActiveScene();
        string previousScenePath = previousScene.path;
        Scene scene = OpenMainSceneForAudit();

        StringBuilder report = new StringBuilder(16384);
        report.AppendLine("# Production Audit");
        report.AppendLine();
        report.AppendLine("- Project: Butterfly House / Wonderful World");
        report.AppendLine("- Main scene: `Assets/_Project/World/Persistent/World_WonderlandPark.unity`");
        report.AppendLine("- Unity version: `6000.3.11f1`");
        report.AppendLine("- Target runtime: PC VR through Quest 3 Link, OpenXR, URP");
        report.AppendLine();

        AppendSceneAudit(report, scene);
        AppendAssetAudit(report);
        AppendMainSceneDependencyAudit(report);
        AppendDebugFlagAudit(report);
        AppendValidationChecklist(report);

        Directory.CreateDirectory(Path.GetDirectoryName(AuditReportPath));
        File.WriteAllText(AuditReportPath, report.ToString(), Encoding.UTF8);
        AssetDatabase.Refresh();

        if (!string.IsNullOrEmpty(previousScenePath) && previousScenePath != scene.path)
        {
            EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single);
        }

        Debug.Log($"[ProductionCleanup] Wrote {AuditReportPath}.");
    }

    [MenuItem("Wonderful World/Production/Generate Asset Reference Audit")]
    public static void GenerateAssetReferenceAudit()
    {
        CreateStandardProjectFolders();

        StringBuilder report = new StringBuilder(16384);
        report.AppendLine("# Asset Reference Audit");
        report.AppendLine();
        report.AppendLine("- Main scene: `Assets/_Project/World/Persistent/World_WonderlandPark.unity`");
        report.AppendLine("- Rule: move or rename Unity assets only through Unity/AssetDatabase so GUID references remain intact.");
        report.AppendLine();

        AppendMainSceneDependencyAudit(report);
        AppendCleanupCandidateAudit(report);
        AppendNamingAudit(report);
        AppendAssetMovePlan(report);

        Directory.CreateDirectory(Path.GetDirectoryName(AssetReferenceReportPath));
        File.WriteAllText(AssetReferenceReportPath, report.ToString(), Encoding.UTF8);
        AssetDatabase.Refresh();

        Debug.Log($"[ProductionCleanup] Wrote {AssetReferenceReportPath}.");
    }

    [MenuItem("Wonderful World/Production/Normalize Main Scene Hierarchy")]
    public static void NormalizeMainSceneHierarchy()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError($"[ProductionCleanup] Could not open main scene: {MainScenePath}");
            return;
        }

        Dictionary<string, GameObject> roots = scene.GetRootGameObjects()
            .GroupBy(go => go.name)
            .ToDictionary(group => group.Key, group => group.First());

        foreach (string rootName in RequiredRootNames)
        {
            if (!roots.ContainsKey(rootName))
            {
                GameObject created = new GameObject(rootName);
                SceneManager.MoveGameObjectToScene(created, scene);
                roots[rootName] = created;
            }
        }

        if (roots.TryGetValue("WW_UI_System", out GameObject uiSystem) &&
            roots.TryGetValue("UI", out GameObject uiRoot))
        {
            Undo.SetTransformParent(uiSystem.transform, uiRoot.transform, "Move WW_UI_System under UI");
        }

        MoveDecorativeOrphanRoots(scene, roots);
        RemoveEmptyInactiveDebugRoot(roots);
        ApplyRootOrder(scene);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[ProductionCleanup] Main scene hierarchy normalized and saved.");
    }

    private static Scene OpenMainSceneForAudit()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.path == MainScenePath && activeScene.isLoaded)
        {
            return activeScene;
        }

        return EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
    }

    private static void AppendSceneAudit(StringBuilder report, Scene scene)
    {
        report.AppendLine("## Scene Hierarchy");
        report.AppendLine();

        if (!scene.IsValid() || !scene.isLoaded)
        {
            report.AppendLine("- Main scene could not be loaded.");
            report.AppendLine();
            return;
        }

        GameObject[] roots = scene.GetRootGameObjects();
        report.AppendLine($"- Root objects: {roots.Length}");
        report.AppendLine();
        report.AppendLine("| Root | Active | Direct children | Total objects | Missing scripts |");
        report.AppendLine("| --- | --- | ---: | ---: | ---: |");

        foreach (GameObject root in roots.OrderBy(go => go.transform.GetSiblingIndex()))
        {
            int totalObjects = root.GetComponentsInChildren<Transform>(true).Length;
            int missingScripts = CountMissingScripts(root);
            report.AppendLine($"| `{root.name}` | {root.activeSelf} | {root.transform.childCount} | {totalObjects} | {missingScripts} |");
        }

        report.AppendLine();
        report.AppendLine("Recommended root grouping:");
        report.AppendLine();
        report.AppendLine("- `GlobalSystem`: event system, global managers, language/settings services, runtime profiles.");
        report.AppendLine("- `XR`: the production XR Origin and controller rig only.");
        report.AppendLine("- `Lighting`: directional light, sky, probes, volumes, and time-of-day atmosphere.");
        report.AppendLine("- `Terrain`: terrain tiles, terrain data instances, and terrain-only colliders.");
        report.AppendLine("- `World_Regions`: Human Entry, Flower Garden, Lotus Pond, Cat Garden, Fireworks Clearing, Mushroom Growth, Cherry Garden.");
        report.AppendLine("- `Decorations`: world art that is not owned by a specific region.");
        report.AppendLine("- `UI`: world-space UI, welcome boards, notice board overlay, and system menu.");
        report.AppendLine("- `Debug`: temporary disabled helpers only; delete it when empty.");
        report.AppendLine();
    }

    private static void AppendAssetAudit(StringBuilder report)
    {
        report.AppendLine("## Asset Organization");
        report.AppendLine();
        report.AppendLine("Production-owned assets should live under `Assets/_Project`. Third-party packages may stay under their vendor folders. Temporary, recovery, and sandbox content must not be referenced by the production scene.");
        report.AppendLine();

        string[] allAssets = AssetDatabase.GetAllAssetPaths()
            .Where(path => path.StartsWith("Assets/"))
            .Where(path => !AssetDatabase.IsValidFolder(path))
            .ToArray();

        var topLevelGroups = allAssets
            .GroupBy(GetTopLevelAssetFolder)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key);

        report.AppendLine("| Top-level folder | Asset count | Classification |");
        report.AppendLine("| --- | ---: | --- |");
        foreach (var group in topLevelGroups)
        {
            report.AppendLine($"| `{group.Key}` | {group.Count()} | {ClassifyTopLevelFolder(group.Key)} |");
        }

        report.AppendLine();
        report.AppendLine("Naming conventions:");
        report.AppendLine();
        report.AppendLine("- Textures: `T_Description`.");
        report.AppendLine("- Materials: `M_Description`.");
        report.AppendLine("- Static meshes and model assets: `SM_Description` when project-authored.");
        report.AppendLine("- Prefabs: `P_Description` for generic prefabs, or feature prefixes such as `WW_`, `Lotus`, `Growth`, and `CatRide` where already established.");
        report.AppendLine("- Audio: `SFX_Description`, `AMB_Description`, or `MUS_Description`.");
        report.AppendLine("- ScriptableObjects: `FeatureName_SO` or a descriptive feature-local asset name.");
        report.AppendLine();
    }

    private static void AppendMainSceneDependencyAudit(StringBuilder report)
    {
        report.AppendLine("## Main Scene Dependencies");
        report.AppendLine();

        string[] dependencies = AssetDatabase.GetDependencies(MainScenePath, recursive: true)
            .Where(path => path.StartsWith("Assets/"))
            .Where(path => !AssetDatabase.IsValidFolder(path))
            .Distinct()
            .OrderBy(path => path)
            .ToArray();

        report.AppendLine($"- Asset dependencies discovered by `AssetDatabase.GetDependencies`: {dependencies.Length}");
        report.AppendLine();

        var groups = dependencies
            .GroupBy(GetTopLevelAssetFolder)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key);

        report.AppendLine("| Top-level folder | Referenced assets | Classification |");
        report.AppendLine("| --- | ---: | --- |");
        foreach (var group in groups)
        {
            report.AppendLine($"| `{group.Key}` | {group.Count()} | {ClassifyTopLevelFolder(group.Key)} |");
        }

        report.AppendLine();
        report.AppendLine("Referenced assets outside `Assets/_Project`:");
        report.AppendLine();

        string[] externalDependencies = dependencies
            .Where(path => !path.StartsWith("Assets/_Project/"))
            .ToArray();

        if (externalDependencies.Length == 0)
        {
            report.AppendLine("- None.");
        }
        else
        {
            foreach (string path in externalDependencies.Take(150))
            {
                report.AppendLine($"- `{path}`");
            }

            if (externalDependencies.Length > 150)
            {
                report.AppendLine($"- ...and {externalDependencies.Length - 150} more.");
            }
        }

        report.AppendLine();
        AppendUnresolvedGuidAudit(report);
    }

    private static void AppendCleanupCandidateAudit(StringBuilder report)
    {
        report.AppendLine("## Cleanup Candidates");
        report.AppendLine();

        string[] dependencies = AssetDatabase.GetDependencies(MainScenePath, recursive: true)
            .Where(path => path.StartsWith("Assets/"))
            .Distinct()
            .ToArray();

        report.AppendLine("| Folder | Assets | Referenced by main scene | Recommendation |");
        report.AppendLine("| --- | ---: | ---: | --- |");
        foreach (string folder in CleanupCandidateFolders)
        {
            string[] assets = AssetDatabase.FindAssets(string.Empty, new[] { folder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => !string.IsNullOrEmpty(path))
                .Where(path => !AssetDatabase.IsValidFolder(path))
                .Distinct()
                .ToArray();

            int referencedCount = assets.Count(assetPath => dependencies.Contains(assetPath));
            string recommendation = referencedCount > 0
                ? "Keep referenced assets; move confirmed production assets through AssetDatabase only."
                : "Candidate for removal after team confirmation.";

            report.AppendLine($"| `{folder}` | {assets.Length} | {referencedCount} | {recommendation} |");
        }

        report.AppendLine();
    }

    private static void AppendNamingAudit(StringBuilder report)
    {
        report.AppendLine("## Naming Audit");
        report.AppendLine();

        string[] productionAssets = AssetDatabase.FindAssets(string.Empty, new[] { "Assets/_Project" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => !string.IsNullOrEmpty(path))
            .Where(path => !AssetDatabase.IsValidFolder(path))
            .Distinct()
            .OrderBy(path => path)
            .ToArray();

        List<string> namingWarnings = new List<string>();
        for (int i = 0; i < productionAssets.Length; i++)
        {
            string path = productionAssets[i];
            string fileName = Path.GetFileNameWithoutExtension(path);
            string extension = Path.GetExtension(path).ToLowerInvariant();
            string expectedPrefix = GetExpectedAssetPrefix(path, extension);
            if (string.IsNullOrEmpty(expectedPrefix) || fileName.StartsWith(expectedPrefix))
            {
                continue;
            }

            namingWarnings.Add($"- `{path}` should usually start with `{expectedPrefix}`.");
        }

        if (namingWarnings.Count == 0)
        {
            report.AppendLine("- No obvious naming-prefix warnings under `Assets/_Project`.");
        }
        else
        {
            report.AppendLine($"- Warnings: {namingWarnings.Count}");
            report.AppendLine();
            foreach (string warning in namingWarnings.Take(150))
            {
                report.AppendLine(warning);
            }

            if (namingWarnings.Count > 150)
            {
                report.AppendLine($"- ...and {namingWarnings.Count - 150} more.");
            }
        }

        report.AppendLine();
    }

    private static void AppendAssetMovePlan(StringBuilder report)
    {
        report.AppendLine("## Asset Move Plan");
        report.AppendLine();
        report.AppendLine("Use this as a review queue. Do not move these paths from the operating system.");
        report.AppendLine();
        report.AppendLine("1. Keep vendor and package assets in their vendor folders unless the team decides to internalize them.");
        report.AppendLine("2. For project-authored assets outside `Assets/_Project`, use Unity Project window drag/drop or `AssetDatabase.MoveAsset`.");
        report.AppendLine("3. Re-run this report after each move batch and open the main scene before committing.");
        report.AppendLine("4. Delete `_Recovery`, `_TempArt`, template scenes, or legacy scripts only after this report shows zero production references and the team confirms they are obsolete.");
        report.AppendLine();
    }

    private static void AppendDebugFlagAudit(StringBuilder report)
    {
        report.AppendLine("## Production Debug Flags");
        report.AppendLine();

        string[] scannedAssets = AssetDatabase.FindAssets(string.Empty, new[] { "Assets/_Project" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => path.EndsWith(".unity") || path.EndsWith(".prefab"))
            .Where(path => !path.Contains("/Sandbox/"))
            .Where(path => !Path.GetFileNameWithoutExtension(path).Contains("_bak"))
            .Distinct()
            .OrderBy(path => path)
            .ToArray();

        List<string> enabledFlags = new List<string>();
        foreach (string path in scannedAssets)
        {
            if (!File.Exists(path))
            {
                continue;
            }

            int lineNumber = 0;
            foreach (string line in File.ReadLines(path))
            {
                lineNumber++;
                string trimmed = line.Trim();
                for (int i = 0; i < ProductionDebugFlagNames.Length; i++)
                {
                    string flag = ProductionDebugFlagNames[i];
                    if (trimmed == $"{flag}: 1")
                    {
                        enabledFlags.Add($"| `{path}` | {lineNumber} | `{flag}` |");
                    }
                }
            }
        }

        if (enabledFlags.Count == 0)
        {
            report.AppendLine("- No enabled production debug flags found in non-sandbox scenes or prefabs.");
        }
        else
        {
            report.AppendLine("| Asset | Line | Flag |");
            report.AppendLine("| --- | ---: | --- |");
            foreach (string enabledFlag in enabledFlags)
            {
                report.AppendLine(enabledFlag);
            }
        }

        report.AppendLine();
    }

    private static void AppendValidationChecklist(StringBuilder report)
    {
        report.AppendLine("## Validation Checklist");
        report.AppendLine();
        report.AppendLine("Run this checklist after each cleanup or optimization batch:");
        report.AppendLine();
        report.AppendLine("1. Open `World_WonderlandPark.unity` in Unity with no missing-script warnings.");
        report.AppendLine("2. Enter Play Mode and confirm XR Origin, teleport, snap turn, recenter, system menu, notice boards, audio, and onboarding still work.");
        report.AppendLine("3. Walk each region: Human Entry, Flower Garden, Lotus Pond, Cat Garden, Fireworks Clearing, Mushroom Growth, Cherry Garden.");
        report.AppendLine("4. Use Unity Profiler and Frame Debugger in a headset-linked Play Mode session.");
        report.AppendLine("5. Use OVR Metrics Tool or OpenXR Toolkit to confirm stable 72/90 Hz frame pacing through Quest 3 Link.");
        report.AppendLine("6. Specifically inspect skybox, camera clear flags, near/far clipping, transparent effects, render textures, post volumes, and custom shaders if black blocks or flicker are visible.");
        report.AppendLine();
    }

    private static int CountMissingScripts(GameObject root)
    {
        int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(root);
        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            GameObject child = children[i].gameObject;
            if (child != root)
            {
                count += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child);
            }
        }

        return count;
    }

    private static void RemoveEmptyInactiveDebugRoot(Dictionary<string, GameObject> roots)
    {
        if (!roots.TryGetValue("Debug", out GameObject debugRoot))
        {
            return;
        }

        bool onlyTransform = debugRoot.GetComponents<Component>().Length == 1;
        if (!debugRoot.activeSelf && debugRoot.transform.childCount == 0 && onlyTransform)
        {
            Undo.DestroyObjectImmediate(debugRoot);
        }
    }

    private static void MoveDecorativeOrphanRoots(Scene scene, Dictionary<string, GameObject> roots)
    {
        if (!roots.TryGetValue("Decorations", out GameObject decorationsRoot))
        {
            return;
        }

        GameObject[] sceneRoots = scene.GetRootGameObjects();
        for (int i = 0; i < sceneRoots.Length; i++)
        {
            GameObject root = sceneRoots[i];
            if (root == null ||
                root == decorationsRoot ||
                CanonicalRootOrder.Contains(root.name) ||
                !ShouldMoveRootUnderDecorations(root))
            {
                continue;
            }

            Undo.SetTransformParent(root.transform, decorationsRoot.transform, $"Move {root.name} under Decorations");
        }
    }

    private static void ApplyRootOrder(Scene scene)
    {
        Dictionary<string, GameObject> roots = scene.GetRootGameObjects()
            .GroupBy(go => go.name)
            .ToDictionary(group => group.Key, group => group.First());

        for (int i = 0; i < CanonicalRootOrder.Length; i++)
        {
            if (roots.TryGetValue(CanonicalRootOrder[i], out GameObject root))
            {
                root.transform.SetSiblingIndex(i);
            }
        }
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string parent = Path.GetDirectoryName(folderPath).Replace('\\', '/');
        string folder = Path.GetFileName(folderPath);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folder);
    }

    private static string GetTopLevelAssetFolder(string assetPath)
    {
        string[] parts = assetPath.Split('/');
        return parts.Length >= 2 ? $"Assets/{parts[1]}" : assetPath;
    }

    private static string ClassifyTopLevelFolder(string folder)
    {
        if (folder == "Assets/_Project")
        {
            return "Production-owned";
        }

        if (folder == "Assets/_Recovery" || folder == "Assets/_TempArt")
        {
            return "Cleanup candidate; verify references before deleting";
        }

        if (folder == "Assets/Scenes" || folder == "Assets/Scripts")
        {
            return "Legacy/template candidate; should not be referenced by production";
        }

        if (folder == "Assets/Samples" ||
            folder == "Assets/TextMesh Pro" ||
            folder == "Assets/Toon Fantasy Nature" ||
            folder == "Assets/VRTemplateAssets" ||
            folder == "Assets/XR" ||
            folder == "Assets/XRI" ||
            folder == "Assets/Settings" ||
            folder == "Assets/URPDefaultResources" ||
            folder == "Assets/ithappy" ||
            folder == "Assets/NamuFX")
        {
            return "Third-party, package, or Unity template support";
        }

        return "Review and either move through AssetDatabase or document as vendor content";
    }

    private static void AppendUnresolvedGuidAudit(StringBuilder report)
    {
        if (!File.Exists(MainScenePath))
        {
            return;
        }

        string sceneText = File.ReadAllText(MainScenePath);
        System.Text.RegularExpressions.MatchCollection matches =
            System.Text.RegularExpressions.Regex.Matches(sceneText, "guid: ([0-9a-f]{32})");

        HashSet<string> unresolved = new HashSet<string>();
        for (int i = 0; i < matches.Count; i++)
        {
            string guid = matches[i].Groups[1].Value;
            if (IsBuiltInGuid(guid))
            {
                continue;
            }

            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path))
            {
                unresolved.Add(guid);
            }
        }

        report.AppendLine("Unresolved scene GUIDs:");
        report.AppendLine();
        if (unresolved.Count == 0)
        {
            report.AppendLine("- None.");
        }
        else
        {
            foreach (string guid in unresolved.OrderBy(value => value))
            {
                report.AppendLine($"- `{guid}`");
            }
        }

        report.AppendLine();
    }

    private static bool ShouldMoveRootUnderDecorations(GameObject root)
    {
        if (root == null)
        {
            return false;
        }

        for (int i = 0; i < DecorativeRootPrefixes.Length; i++)
        {
            if (root.name.StartsWith(DecorativeRootPrefixes[i], System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string GetExpectedAssetPrefix(string path, string extension)
    {
        if (path.Contains("/Scripts/") || extension == ".cs")
        {
            return string.Empty;
        }

        if (extension == ".mat")
        {
            return "M_";
        }

        if (extension == ".prefab")
        {
            return "P_";
        }

        if (extension == ".asset")
        {
            return "SO_";
        }

        if (extension == ".png" || extension == ".jpg" || extension == ".jpeg" || extension == ".tga" || extension == ".exr")
        {
            return "T_";
        }

        if (extension == ".fbx" || extension == ".obj" || extension == ".blend")
        {
            return "SM_";
        }

        if (extension == ".wav" || extension == ".mp3" || extension == ".ogg")
        {
            return "SFX_";
        }

        return string.Empty;
    }

    private static bool IsBuiltInGuid(string guid)
    {
        return guid == "00000000000000000000000000000000" ||
               guid == "0000000000000000e000000000000000" ||
               guid == "0000000000000000f000000000000000";
    }
}
