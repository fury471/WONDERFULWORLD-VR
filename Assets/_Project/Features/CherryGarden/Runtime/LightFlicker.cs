using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
[RequireComponent(typeof(Light))]
public class LightFlicker : MonoBehaviour
{
    [SerializeField, FormerlySerializedAs("m_PositionJitterScale")] private float positionJitterScale;
    [SerializeField, FormerlySerializedAs("m_RotationJitterScale")] private float rotationJitterScale;
    [SerializeField, FormerlySerializedAs("m_IntensityJitterScale")] private float intensityJitterScale;
    [SerializeField, FormerlySerializedAs("m_Timescale"), Min(0f)] private float timescale = 1f;

    private Vector3 initialPosition;
    private float initialIntensity;
    private Quaternion initialRotation;
    private Vector3 noiseSeed;
    private Light cachedLight;
    private float flickerIntensityOffset = 1f;

    public float ModifiedIntensity => initialIntensity + flickerIntensityOffset;

    private void Awake()
    {
        cachedLight = GetComponent<Light>();
        initialIntensity = cachedLight.intensity;
        initialPosition = transform.position;
        initialRotation = transform.rotation;

        int seed = Mathf.Abs(gameObject.GetInstanceID());
        noiseSeed = new Vector3(
            SeedToRange(seed, 17),
            SeedToRange(seed, 31),
            SeedToRange(seed, 47));
    }

    private void Update()
    {
        float time = Time.time * timescale;
        Vector3 noise = PerlinNoise3D(new Vector3(time + noiseSeed.x, time + noiseSeed.y, time + noiseSeed.z), 2, 1f);
        noise = noise * 2f - Vector3.one;

        transform.SetPositionAndRotation(
            initialPosition + noise * positionJitterScale,
            initialRotation * Quaternion.Euler(noise * rotationJitterScale));

        flickerIntensityOffset = noise.x * intensityJitterScale;
        cachedLight.intensity = ModifiedIntensity;
    }

    private static float SeedToRange(int seed, int salt)
    {
        uint value = (uint)(seed * 73856093 ^ salt * 19349663);
        return value % 10000 / 40.322f;
    }

    private static Vector3 PerlinNoise3D(Vector3 uv, int octaves, float frequency)
    {
        Vector3 output = Vector3.zero;
        for (int i = 0; i < octaves; i++)
        {
            float octaveFrequency = frequency * (i + 1);
            output.x += Mathf.PerlinNoise(uv.x * octaveFrequency, 0.13f * i);
            output.y += Mathf.PerlinNoise(uv.y * octaveFrequency, 3.71f + 0.17f * i);
            output.z += Mathf.PerlinNoise(uv.z * octaveFrequency, 7.43f + 0.19f * i);
        }

        return output / Mathf.Max(1, octaves);
    }
}
