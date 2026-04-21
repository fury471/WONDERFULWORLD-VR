using UnityEngine;
using System.Collections.Generic;

public class CatRideRouteController : MonoBehaviour
{
    [Header("Route")]
    public List<Transform> routePoints = new List<Transform>();
    public MountSettings_SO settings;

    [Header("Auto Alignment")]
    [SerializeField] private bool autoAlignToCatRouteAnchors = true;
    [SerializeField] private float rideClearanceHeight = 0.35f;

    [HideInInspector] public int currentIndex = 0;
    [HideInInspector] public bool isRunning = false;

    private void Awake()
    {
        TryAutoAlignRouteToCatRoute();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
        {
            TryAutoAlignRouteToCatRoute();
        }
    }
#endif

    public void StartRoute()
    {
        if (!ValidateRouteSetup())
        {
            isRunning = false;
            return;
        }

        currentIndex = 0;
        isRunning = true;

        if (settings.debugLogs)
        {
            Debug.Log("[CatRideRouteController] Route Started");
        }
    }

    void Update()
    {
        if (!isRunning)
        {
            return;
        }

        if (!ValidateRouteRuntime())
        {
            isRunning = false;
            return;
        }

        Transform target = routePoints[currentIndex];

        // Move
        transform.position = Vector3.MoveTowards(
            transform.position,
            target.position,
            settings.autoRideSpeed * Time.deltaTime
        );

        // Rotate
        Vector3 direction = target.position - transform.position;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                settings.rotateSpeed * Time.deltaTime
            );
        }

        // Arrival check
        if (Vector3.Distance(transform.position, target.position) < settings.reachDistance)
        {
            currentIndex++;

            if (currentIndex >= routePoints.Count)
            {
                isRunning = false;

                if (settings.debugLogs)
                {
                    Debug.Log("[CatRideRouteController] Route Finished");
                }
            }
        }
    }

    private bool ValidateRouteSetup()
    {
        if (settings == null)
        {
            Debug.LogError("[CatRideRouteController] Missing MountSettings_SO reference.");
            return false;
        }

        if (routePoints == null || routePoints.Count == 0)
        {
            Debug.LogError("[CatRideRouteController] routePoints is empty.");
            return false;
        }

        for (int i = 0; i < routePoints.Count; i++)
        {
            if (routePoints[i] == null)
            {
                Debug.LogError($"[CatRideRouteController] routePoints[{i}] is null.");
                return false;
            }
        }

        return true;
    }

    private bool ValidateRouteRuntime()
    {
        if (settings == null)
        {
            Debug.LogError("[CatRideRouteController] Missing MountSettings_SO reference.");
            return false;
        }

        if (routePoints == null || routePoints.Count == 0)
        {
            Debug.LogError("[CatRideRouteController] routePoints is empty.");
            return false;
        }

        if (currentIndex < 0 || currentIndex >= routePoints.Count)
        {
            return false;
        }

        if (routePoints[currentIndex] == null)
        {
            Debug.LogError($"[CatRideRouteController] routePoints[{currentIndex}] is null.");
            return false;
        }

        return true;
    }
    public Vector3 GetMountFacingDirection()
    {
        if (routePoints == null || routePoints.Count < 2)
        {
            return transform.forward;
        }

        Transform from = routePoints[0];
        Transform to = routePoints[1];

        if (from == null || to == null)
        {
            return transform.forward;
        }

        Vector3 direction = to.position - from.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
        {
            return transform.forward;
        }

        return direction.normalized;
    }

    private void TryAutoAlignRouteToCatRoute()
    {
        if (!autoAlignToCatRouteAnchors || routePoints == null || routePoints.Count == 0)
        {
            return;
        }

        Transform startAnchor = FindNearbyTransform("MountStartAncho");
        if (startAnchor == null)
        {
            startAnchor = FindNearbyTransform("RouteIn");
        }

        Transform midAnchor = FindNearbyTransform("CatPathMidAnchor");
        Transform endAnchor = FindNearbyTransform("MountEndAnchor");

        if (startAnchor == null || endAnchor == null)
        {
            return;
        }

        Transform mountPlatform = FindNearbyTransform("MountPlatform");
        Transform ridePathA = FindNearbyTransform("RidePath_A");
        Transform ridePathB = FindNearbyTransform("RidePath_B");
        Transform ridePathC = FindNearbyTransform("RidePath_C");
        Transform exitPad = FindNearbyTransform("ExitPad");

        Vector3 startPosition = GetRideSurfacePosition(startAnchor, mountPlatform != null ? mountPlatform : startAnchor);

        // Keep the cat mount visually at the route start in the integrated scene.
        transform.position = startPosition;

        Vector3 node1Position = ridePathA != null
            ? GetRideSurfacePosition(ridePathA, ridePathA)
            : Vector3.Lerp(startPosition, endAnchor.position, 0.18f);

        Vector3 node2Position = ridePathB != null
            ? GetRideSurfacePosition(ridePathB, ridePathB)
            : (midAnchor != null
                ? midAnchor.position
                : Vector3.Lerp(node1Position, endAnchor.position, 0.35f));

        Vector3 node3Position = Vector3.Lerp(node1Position, node2Position, 0.55f);

        Vector3 node4Position = ridePathC != null
            ? GetRideSurfacePosition(ridePathC, ridePathC)
            : Vector3.Lerp(node2Position, endAnchor.position, 0.55f);

        Vector3 node5Position = exitPad != null
            ? GetRideSurfacePosition(exitPad, exitPad)
            : GetRideSurfacePosition(endAnchor, endAnchor);

        // Route is authored as six points in the current prefab.
        SetRoutePointPosition(0, startPosition);
        SetRoutePointPosition(1, node1Position);
        SetRoutePointPosition(2, node3Position);
        SetRoutePointPosition(3, node2Position);
        SetRoutePointPosition(4, node4Position);
        SetRoutePointPosition(5, node5Position);
    }

    private Vector3 GetRideSurfacePosition(Transform positionSource, Transform surfaceSource)
    {
        if (positionSource == null)
        {
            return Vector3.zero;
        }

        Vector3 position = positionSource.position;

        if (surfaceSource != null)
        {
            Collider surfaceCollider = surfaceSource.GetComponent<Collider>();
            if (surfaceCollider != null)
            {
                position.y = surfaceCollider.bounds.max.y + rideClearanceHeight;
                return position;
            }

            Renderer surfaceRenderer = surfaceSource.GetComponent<Renderer>();
            if (surfaceRenderer != null)
            {
                position.y = surfaceRenderer.bounds.max.y + rideClearanceHeight;
                return position;
            }
        }

        position.y += rideClearanceHeight;
        return position;
    }

    private void SetRoutePointPosition(int index, Vector3 position)
    {
        if (routePoints == null || index < 0 || index >= routePoints.Count)
        {
            return;
        }

        Transform point = routePoints[index];
        if (point == null)
        {
            return;
        }

        point.position = position;
    }

    private Transform FindNearbyTransform(string targetName)
    {
        Transform searchRoot = transform;

        while (searchRoot != null)
        {
            Transform found = FindChildRecursive(searchRoot, targetName);
            if (found != null)
            {
                return found;
            }

            searchRoot = searchRoot.parent;
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
