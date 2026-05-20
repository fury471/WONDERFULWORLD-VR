using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class StylizedWaterSplashLoop : MonoBehaviour
{
    [SerializeField] private Material splashMaterial;
    [SerializeField, Min(0.1f)] private float width = 2.8f;
    [SerializeField, Min(0.1f)] private float height = 1.2f;
    [SerializeField, Min(1f)] private float burstRate = 58f;
    [SerializeField, Min(0.1f)] private float mistRate = 26f;

    private const string BurstName = "Splash_Burst";
    private const string MistName = "Splash_Mist";

    private void Awake() => Rebuild();

    private void OnEnable() => Rebuild();

    private void OnValidate() => Rebuild();

    [ContextMenu("Rebuild Splash")]
    public void Rebuild()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        Configure(Resolve(BurstName), false);
        Configure(Resolve(MistName), true);
    }

    private Transform Resolve(string childName)
    {
        Transform child = transform.Find(childName);
        if (child != null)
        {
            return child;
        }

        GameObject go = new GameObject(childName);
        go.hideFlags = HideFlags.DontSaveInEditor;
        child = go.transform;
        child.SetParent(transform, false);
        return child;
    }

    private void Configure(Transform holder, bool mist)
    {
        ParticleSystem ps = GetOrAdd<ParticleSystem>(holder.gameObject);
        ParticleSystemRenderer renderer = GetOrAdd<ParticleSystemRenderer>(holder.gameObject);
        bool shouldPlay = ps.isPlaying || ps.main.playOnAwake;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        holder.localPosition = mist ? new Vector3(0f, 0.16f, 0.16f) : Vector3.zero;
        holder.localRotation = Quaternion.identity;
        holder.localScale = Vector3.one;

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.duration = mist ? 2f : 1.15f;
        main.startLifetime = mist ? new ParticleSystem.MinMaxCurve(1.45f, 2.7f) : new ParticleSystem.MinMaxCurve(0.5f, 0.95f);
        main.startSpeed = mist ? new ParticleSystem.MinMaxCurve(0.22f, 0.72f) : new ParticleSystem.MinMaxCurve(1.15f, 3.7f);
        main.startSize = mist ? new ParticleSystem.MinMaxCurve(0.28f, 0.7f) : new ParticleSystem.MinMaxCurve(0.17f, 0.62f);
        main.startColor = mist
            ? new ParticleSystem.MinMaxGradient(new Color(0.62f, 0.9f, 1f, 0.44f), new Color(1f, 1f, 1f, 0.76f))
            : new ParticleSystem.MinMaxGradient(new Color(0.62f, 0.88f, 1f, 0.62f), new Color(1f, 1f, 1f, 0.92f));
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.maxParticles = mist ? 500 : 260;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = mist ? mistRate * 3.15f : burstRate;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = mist ? new Vector3(width * 0.9f, 0.14f, 0.2f) : new Vector3(width, 0.08f, 0.18f);

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = mist ? new ParticleSystem.MinMaxCurve(-0.48f, 0.48f) : new ParticleSystem.MinMaxCurve(-1.75f, 1.75f);
        velocity.y = mist ? new ParticleSystem.MinMaxCurve(0.2f, 0.7f) : new ParticleSystem.MinMaxCurve(0.8f, height * 2.25f);
        velocity.z = mist ? new ParticleSystem.MinMaxCurve(0.02f, 0.58f) : new ParticleSystem.MinMaxCurve(-0.5f, 1.25f);

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, mist
            ? new AnimationCurve(new Keyframe(0f, 0.28f), new Keyframe(0.34f, 0.72f), new Keyframe(1f, 0.92f))
            : new AnimationCurve(new Keyframe(0f, 0.06f), new Keyframe(0.14f, 0.95f), new Keyframe(0.58f, 0.62f), new Keyframe(1f, 0.035f)));

        var color = ps.colorOverLifetime;
        color.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.72f, 0.92f, 1f), 0f),
                new GradientColorKey(Color.white, 0.36f),
                new GradientColorKey(new Color(0.5f, 0.82f, 0.95f), 1f)
            },
            mist
                ? new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.76f, 0.16f), new GradientAlphaKey(0.52f, 0.72f), new GradientAlphaKey(0f, 1f) }
                : new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.92f, 0.1f), new GradientAlphaKey(0.46f, 0.52f), new GradientAlphaKey(0f, 1f) });
        color.color = gradient;

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = mist ? 0.62f : 0.16f;
        noise.frequency = mist ? 0.82f : 0.58f;
        noise.scrollSpeed = mist ? 0.28f : 0.38f;

        renderer.renderMode = mist ? ParticleSystemRenderMode.Billboard : ParticleSystemRenderMode.Stretch;
        renderer.sharedMaterial = splashMaterial;
        renderer.sortingFudge = mist ? -0.1f : 0.15f;
        renderer.velocityScale = mist ? 0f : 0.18f;
        renderer.lengthScale = mist ? 1f : 1.95f;

        if (shouldPlay)
        {
            ps.Play();
        }
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        if (!go.TryGetComponent(out T component))
        {
            component = go.AddComponent<T>();
        }

        return component;
    }
}
