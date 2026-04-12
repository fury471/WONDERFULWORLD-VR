using UnityEngine;

[CreateAssetMenu(fileName = "MountSettings_SO", menuName = "MountSystem/Settings Asset")]
public class MountSettings_SO : ScriptableObject
{
    [Header("Movement")]
    public float autoRideSpeed = 2.0f;
    public float rotateSpeed = 180f;
    public float reachDistance = 0.25f;

    [Header("Transition")]
    public float mountBlendTime = 0.35f;
    public float dismountBlendTime = 0.35f;

    [Header("Offset")]
    public float seatHeightOffset = 0.8f;

    [Header("Debug")]
    public bool debugLogs = true;
}