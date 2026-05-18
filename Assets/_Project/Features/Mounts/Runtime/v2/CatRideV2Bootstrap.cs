using UnityEngine;

/// <summary>
/// Drop this component on an empty GameObject in any scene that doesn't yet have the V2 cat
/// mount authored in. At scene start it instantiates the referenced mount prefab at the
/// configured spawn pose and wires up the player rig / locomotion references on the spawned
/// <see cref="CatRideControllerV2"/> so the mount is ready to use without manual setup.
///
/// This is the production-friendly way to roll Haobo's sandbox mount into the main park scene
/// without having to manually merge two large .unity files. Once the mount is happily placed
/// you can replace this with a normal prefab instance in the scene and remove this component.
/// </summary>
[DisallowMultipleComponent]
public sealed class CatRideV2Bootstrap : MonoBehaviour
{
    [Header("Prefab")]
    [Tooltip("Assign the V2 mount prefab (typically MountRouteTestRoot_V2 from Features/Mounts/Prefabs).")]
    [SerializeField] private GameObject mountPrefab;
    [SerializeField] private Transform spawnPose;
    [Tooltip("If true and there is already a CatRideControllerV2 in the scene, skips spawning a new one.")]
    [SerializeField] private bool skipIfAnyMountExists = true;

    [Header("Auto-wired Rig References")]
    [Tooltip("Searched at spawn time if any of these are empty. Names match Wonderland defaults.")]
    [SerializeField] private string xrOriginName = "WonderlandXROrigin";
    [SerializeField] private string locomotionChildName = "Locomotion";
    [SerializeField] private string deviceSimulatorChildName = "XR Device Simulator";

    [Header("Lifecycle")]
    [SerializeField] private bool destroyBootstrapAfterSpawn = true;

    private GameObject spawnedInstance;

    private void Start()
    {
        Spawn();
    }

    public void Spawn()
    {
        if (spawnedInstance != null)
        {
            return;
        }

        if (mountPrefab == null)
        {
            Debug.LogWarning("[CatRideV2Bootstrap] Mount prefab is not assigned; skipping spawn.", this);
            return;
        }

        if (skipIfAnyMountExists)
        {
            CatRideControllerV2 existing = FindAnyObjectByType<CatRideControllerV2>(FindObjectsInactive.Include);
            if (existing != null)
            {
                if (destroyBootstrapAfterSpawn)
                {
                    Destroy(gameObject);
                }
                return;
            }
        }

        Vector3 position = spawnPose != null ? spawnPose.position : transform.position;
        Quaternion rotation = spawnPose != null ? spawnPose.rotation : transform.rotation;
        spawnedInstance = Instantiate(mountPrefab, position, rotation);
        spawnedInstance.name = mountPrefab.name + "_AutoSpawn";

        CatRideControllerV2 controller = spawnedInstance.GetComponentInChildren<CatRideControllerV2>(true);
        if (controller != null)
        {
            WireUpControllerReferences(controller);
        }
        else
        {
            Debug.LogWarning("[CatRideV2Bootstrap] Spawned prefab has no CatRideControllerV2; check the prefab reference.", this);
        }

        if (destroyBootstrapAfterSpawn)
        {
            Destroy(gameObject);
        }
    }

    private void WireUpControllerReferences(CatRideControllerV2 controller)
    {
        GameObject rig = !string.IsNullOrEmpty(xrOriginName) ? GameObject.Find(xrOriginName) : null;
        if (rig == null)
        {
            return;
        }

        // Use reflection to set private serialized fields without expanding the V2 controller's
        // public surface. This keeps the bootstrap a non-invasive addition.
        SetPrivateField(controller, "playerRigRoot", rig);

        Transform locomotion = FindChildRecursive(rig.transform, locomotionChildName);
        if (locomotion != null)
        {
            SetPrivateField(controller, "locomotionRoot", locomotion.gameObject);
        }

        Transform deviceSim = FindChildRecursive(rig.transform, deviceSimulatorChildName);
        if (deviceSim != null)
        {
            SetPrivateField(controller, "xrDeviceSimulatorRoot", deviceSim.gameObject);
        }
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        if (target == null)
        {
            return;
        }

        var field = target.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Public);
        if (field != null)
        {
            field.SetValue(target, value);
        }
    }

    private static Transform FindChildRecursive(Transform root, string name)
    {
        if (root == null || string.IsNullOrEmpty(name))
        {
            return null;
        }

        if (string.Equals(root.name, name, System.StringComparison.OrdinalIgnoreCase))
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildRecursive(root.GetChild(i), name);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
