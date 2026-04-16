using UnityEngine;

[CreateAssetMenu(fileName = "WeatherPreset_", menuName = "WonderfulWorld/Weather/Weather Preset")]
public class WeatherPreset : ScriptableObject
{
    [Header("Identity")]
    public WeatherType weatherType;

    [Header("Sky")]
    public Material skyboxMaterial;

    [Header("Lighting")]
    public Color directionalLightColor = Color.white;
    [Range(0f, 8f)] public float directionalLightIntensity = 1f;
    public Color ambientLightColor = Color.gray;

    [Header("Fog")]
    public bool fogEnabled = true;
    public Color fogColor = Color.gray;
    [Range(0f, 0.1f)] public float fogDensity = 0.01f;

    [Header("Particles")]
    [Range(0f, 1f)] public float particleEmissionMultiplier = 0f;
    public bool rainParticlesEnabled;
    public bool windParticlesEnabled;

    [Header("Audio And Wind")]
    [Range(0f, 1f)] public float ambienceVolume = 1f;
    [Range(0f, 1f)] public float windStrength = 0f;
}
