using UnityEngine;

public enum ScaleState
{
    Normal,
    Small,
    Large
}

[CreateAssetMenu(fileName = "ScaleSettings_SO", menuName = "WonderfulWorld/Scale Shift/Scale Settings")]
public class ScaleSettings : ScriptableObject
{
    [System.Serializable]
    public struct ScaleProfile
    {
        [Header("Player")]
        public float playerScale;
        public float moveSpeedMultiplier;
        public float interactionDistanceMultiplier;

        [Header("Camera")]
        public float nearClip;
        public float eyeHeightMultiplier;

        [Header("Collision")]
        public float controllerHeightMultiplier;
        public float controllerRadiusMultiplier;
    }

    [Header("Profiles")]
    public ScaleProfile normal = new ScaleProfile
    {
        playerScale = 1f,
        moveSpeedMultiplier = 1f,
        interactionDistanceMultiplier = 1f,
        nearClip = 0.05f,
        eyeHeightMultiplier = 1f,
        controllerHeightMultiplier = 1f,
        controllerRadiusMultiplier = 1f
    };

    public ScaleProfile small = new ScaleProfile
    {
        playerScale = 0.5f,
        moveSpeedMultiplier = 0.65f,
        interactionDistanceMultiplier = 0.6f,
        nearClip = 0.01f,
        eyeHeightMultiplier = 0.55f,
        controllerHeightMultiplier = 0.5f,
        controllerRadiusMultiplier = 0.5f
    };

    public ScaleProfile large = new ScaleProfile
    {
        playerScale = 1.75f,
        moveSpeedMultiplier = 1.35f,
        interactionDistanceMultiplier = 1.4f,
        nearClip = 0.08f,
        eyeHeightMultiplier = 1.25f,
        controllerHeightMultiplier = 1.75f,
        controllerRadiusMultiplier = 1.75f
    };

    [Header("Transition")]
    public float blinkDuration = 0.15f;
    public float cooldown = 0.5f;
    public float fadeOutDuration = 0.08f;
    public float blackHoldDuration = 0.05f;
    public float fadeInDuration = 0.12f;
}
