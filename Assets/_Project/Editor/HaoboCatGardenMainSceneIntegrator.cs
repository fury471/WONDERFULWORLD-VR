using System;
using System.Reflection;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class HaoboCatGardenMainSceneIntegrator
{
    private const string MainScenePath = "Assets/_Project/World/Persistent/World_WonderlandPark.unity";
    private const string SourceScenePath = "Assets/_Project/Sandbox/Haobo/UPDATE_World_WonderlandPark_Haobo.unity";
    private const string RegionName = "Region_CatGarden";

    [MenuItem("Tools/Wonderland/Integrate Haobo Cat Garden Update")]
    public static void Integrate()
    {
        Scene mainScene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
        GameObject oldRegion = FindInScene(mainScene, RegionName);
        if (oldRegion == null)
        {
            throw new InvalidOperationException($"Could not find {RegionName} in {MainScenePath}.");
        }

        Transform regionParent = oldRegion.transform.parent;
        int siblingIndex = oldRegion.transform.GetSiblingIndex();
        Vector3 localPosition = oldRegion.transform.localPosition;
        Quaternion localRotation = oldRegion.transform.localRotation;
        Vector3 localScale = oldRegion.transform.localScale;

        GameObject xrOrigin = FindInScene(mainScene, "WonderlandXROrigin");
        GameObject locomotionRoot = xrOrigin != null ? FindChildRecursive(xrOrigin.transform, "Locomotion")?.gameObject : null;
        GameObject deviceSimulator = FindInScene(mainScene, "XR Device Simulator");
        Transform cameraOffset = xrOrigin != null ? FindChildRecursive(xrOrigin.transform, "Camera Offset") : null;
        Camera mainCamera = Camera.main != null ? Camera.main : xrOrigin?.GetComponentInChildren<Camera>(true);
        Transform playerView = mainCamera != null ? mainCamera.transform : cameraOffset;
        CharacterController characterController = xrOrigin != null ? xrOrigin.GetComponent<CharacterController>() : null;
        XROrigin xrOriginComponent = xrOrigin != null ? xrOrigin.GetComponent<XROrigin>() : null;

        Scene sourceScene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Additive);
        GameObject sourceRegion = FindInScene(sourceScene, RegionName);
        if (sourceRegion == null)
        {
            throw new InvalidOperationException($"Could not find {RegionName} in {SourceScenePath}.");
        }

        GameObject newRegion = UnityEngine.Object.Instantiate(sourceRegion);
        newRegion.name = RegionName;
        SceneManager.MoveGameObjectToScene(newRegion, mainScene);
        UnityEngine.Object.DestroyImmediate(oldRegion);

        newRegion.transform.SetParent(regionParent, false);
        newRegion.transform.SetSiblingIndex(Mathf.Min(siblingIndex, regionParent.childCount - 1));
        newRegion.transform.localPosition = localPosition;
        newRegion.transform.localRotation = localRotation;
        newRegion.transform.localScale = localScale;

        Transform runtimeAnchors = EnsureChild(newRegion.transform, "_RuntimeAnchors");
        Transform summonTarget = EnsureChild(runtimeAnchors, "HorseSummonTarget");
        CopySourceSummonPose(sourceScene, summonTarget);

        ScaleManager scaleManager = newRegion.GetComponentInChildren<ScaleManager>(true);
        RewireScaleManager(scaleManager, xrOrigin, cameraOffset, mainCamera, characterController);

        CatRideControllerV2 firstRide = null;
        CatRideControllerV2[] rideControllers = newRegion.GetComponentsInChildren<CatRideControllerV2>(true);
        for (int i = 0; i < rideControllers.Length; i++)
        {
            CatRideControllerV2 ride = rideControllers[i];
            if (ride == null)
            {
                continue;
            }

            if (firstRide == null)
            {
                firstRide = ride;
            }

            SetField(ride, "playerRigRoot", xrOrigin);
            SetField(ride, "locomotionRoot", locomotionRoot);
            SetField(ride, "xrDeviceSimulatorRoot", deviceSimulator);
            SetField(ride, "scaleManager", scaleManager);
            SetField(ride, "enableKeyboardDebugControls", false);
            EditorUtility.SetDirty(ride);
        }

        if (scaleManager != null && firstRide != null)
        {
            SetField(scaleManager, "rideController", firstRide);
            EditorUtility.SetDirty(scaleManager);
        }

        HorseSummonV2[] horseSummons = newRegion.GetComponentsInChildren<HorseSummonV2>(true);
        for (int i = 0; i < horseSummons.Length; i++)
        {
            HorseSummonV2 summon = horseSummons[i];
            SetField(summon, "playerRigRoot", xrOrigin != null ? xrOrigin.transform : null);
            SetField(summon, "playerView", playerView);
            SetField(summon, "summonTargetAnchor", summonTarget);
            SetField(summon, "debugLogs", false);
            EditorUtility.SetDirty(summon);
        }

        QuestLocomotionComfortProfile comfortProfile = xrOrigin != null
            ? xrOrigin.GetComponentInChildren<QuestLocomotionComfortProfile>(true)
            : null;
        if (comfortProfile != null)
        {
            comfortProfile.SetMovementMode(QuestLocomotionComfortProfile.MovementMode.Teleport);
            comfortProfile.SetTurnMode(QuestLocomotionComfortProfile.TurnMode.Snap);
            EditorUtility.SetDirty(comfortProfile);
        }

        if (xrOriginComponent != null)
        {
            EditorUtility.SetDirty(xrOriginComponent);
        }

        EditorSceneManager.CloseScene(sourceScene, true);
        EditorSceneManager.MarkSceneDirty(mainScene);
        EditorSceneManager.SaveScene(mainScene);
        AssetDatabase.SaveAssets();
        Debug.Log("[Wonderland] Integrated Haobo CatGarden update into the persistent world.");
    }

    private static void RewireScaleManager(
        ScaleManager scaleManager,
        GameObject xrOrigin,
        Transform cameraOffset,
        Camera mainCamera,
        CharacterController characterController)
    {
        if (scaleManager == null)
        {
            return;
        }

        ScaleTransitionController transition = scaleManager.GetComponent<ScaleTransitionController>();
        SetField(scaleManager, "scaleRoot", xrOrigin != null ? xrOrigin.transform : null);
        SetField(scaleManager, "cameraPivot", cameraOffset);
        SetField(scaleManager, "targetCamera", mainCamera);
        SetField(scaleManager, "transitionController", transition);
        SetField(scaleManager, "characterController", characterController);
        SetField(scaleManager, "keepXrRigShapeDuringScale", true);
        SetField(scaleManager, "enableDebugKeyboardScaleShortcuts", false);
        SetField(scaleManager, "enableQuestThumbstickScale", true);
        EditorUtility.SetDirty(scaleManager);
    }

    private static void CopySourceSummonPose(Scene sourceScene, Transform target)
    {
        GameObject sourceTarget = FindInScene(sourceScene, "HorseSummonTarget ");
        if (sourceTarget == null)
        {
            sourceTarget = FindInScene(sourceScene, "HorseSummonTarget");
        }

        if (sourceTarget == null || target == null)
        {
            return;
        }

        target.position = sourceTarget.transform.position;
        target.rotation = sourceTarget.transform.rotation;
        target.localScale = sourceTarget.transform.localScale;
    }

    private static Transform EnsureChild(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child != null)
        {
            return child;
        }

        GameObject childObject = new GameObject(childName);
        childObject.transform.SetParent(parent, false);
        return childObject.transform;
    }

    private static void SetField(object target, string fieldName, object value)
    {
        if (target == null)
        {
            return;
        }

        if (target is UnityEngine.Object unityObject)
        {
            SerializedObject serializedObject = new SerializedObject(unityObject);
            SerializedProperty property = serializedObject.FindProperty(fieldName);
            if (property != null)
            {
                switch (property.propertyType)
                {
                    case SerializedPropertyType.ObjectReference:
                        property.objectReferenceValue = value as UnityEngine.Object;
                        break;
                    case SerializedPropertyType.Boolean:
                        if (value is bool boolValue)
                        {
                            property.boolValue = boolValue;
                        }
                        break;
                    case SerializedPropertyType.Float:
                        if (value is float floatValue)
                        {
                            property.floatValue = floatValue;
                        }
                        break;
                    case SerializedPropertyType.Integer:
                    case SerializedPropertyType.Enum:
                        if (value is int intValue)
                        {
                            property.intValue = intValue;
                        }
                        break;
                }

                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(unityObject);
                return;
            }
        }

        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field != null)
        {
            field.SetValue(target, value);
        }
    }

    private static GameObject FindInScene(Scene scene, string targetName)
    {
        if (!scene.IsValid() || !scene.isLoaded || string.IsNullOrWhiteSpace(targetName))
        {
            return null;
        }

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Transform found = FindChildRecursive(roots[i].transform, targetName);
            if (found != null)
            {
                return found.gameObject;
            }
        }

        return null;
    }

    private static Transform FindChildRecursive(Transform root, string targetName)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == targetName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildRecursive(root.GetChild(i), targetName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
