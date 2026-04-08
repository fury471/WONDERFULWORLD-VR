using UnityEngine;

[CreateAssetMenu(
    fileName = "LotusScale_SO",
    menuName = "WonderfulWorld/Lotus Pond/Lotus Scale Settings")]
public class LotusScaleSettingsSO : ScriptableObject
{
    [Header("Audio")]
    public AudioClip sharedNoteClip;
    [Min(0f)] public float cooldownSeconds = 1f;
    [Min(0f)] public float volume = 0.8f;
    [Min(0f)] public float minDistance = 1f;
    [Min(0f)] public float maxDistance = 8f;

    [Header("Ripple")]
    public Vector3 rippleStartScale = new Vector3(0.2f, 0.2f, 0.2f);
    public Vector3 rippleEndScale = new Vector3(3.5f, 3.5f, 3.5f);
    [Min(0.01f)] public float rippleDuration = 2f;
    public bool hideRippleWhenIdle = true;
}
