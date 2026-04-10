using UnityEngine;
using System.Collections.Generic;

public class CatRideRouteController : MonoBehaviour
{
    public List<Transform> routePoints = new List<Transform>();
    public MountSettings_SO settings;

    [HideInInspector] public int currentIndex = 0;
    [HideInInspector] public bool isRunning = false;

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

}
