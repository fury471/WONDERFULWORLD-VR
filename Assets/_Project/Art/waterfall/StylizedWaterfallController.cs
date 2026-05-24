using UnityEngine;
using WonderfulWorld.Audio;

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class StylizedWaterfallController : MonoBehaviour
{
    [Header("Surface")]
    [SerializeField] private Transform surfaceRoot;
    [SerializeField] private Material surfaceMaterial;
    [SerializeField, Min(0.1f)] private float widthTop = 1.2f;
    [SerializeField, Min(0.1f)] private float widthBottom = 1.85f;
    [SerializeField, Min(0.25f)] private float height = 5.2f;
    [SerializeField, Range(2, 24)] private int segments = 14;
    [SerializeField] private float forwardCurve = 0.55f;

    [Header("Splash")]
    [SerializeField] private Material splashMaterial;
    [SerializeField, Min(0.1f)] private float splashWidth = 2.4f;
    [SerializeField, Min(0.1f)] private float splashHeight = 1.15f;
    [SerializeField, Min(1f)] private float splashRate = 46f;
    [SerializeField, Min(0.1f)] private float mistRate = 24f;

    [Header("Audio")]
    [SerializeField] private bool autoInstallAudio = true;
    [SerializeField] private WonderlandAudioCue waterfallLoopCue;
    [SerializeField] private WonderlandAudioCue waterfallDetailCue;

    private const string SurfaceName = "WaterFall";
    private const string SplashRootName = "Waterfall_BottomSplash";
    private const string BurstName = "Splash_Burst";
    private const string MistName = "Splash_Mist";
    private const string AudioMainName = "Audio_Waterfall_Main";
    private const string AudioDetailName = "Audio_Waterfall_Splash";

    private Mesh generatedMesh;
    private bool editorRebuildQueued;
    private bool audioInstallQueued;
    private bool audioInstallQueuedInPlayMode;

    private void Awake() => RebuildSafely();

    private void OnEnable() => RebuildSafely();

    private void RebuildSafely()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            QueueEditorRebuild();
            return;
        }
#endif

        Rebuild();
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            QueueEditorRebuild();
            return;
        }
#endif

        Rebuild();
    }

    [ContextMenu("Rebuild Waterfall")]
    public void Rebuild()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        Transform surface = ResolveSurface();
        BuildSurface(surface);
        BuildSplash(surface);
        QueueAudioInstall();
    }

    private Transform ResolveSurface()
    {
        if (surfaceRoot != null)
        {
            return surfaceRoot;
        }

        Transform found = transform.Find(SurfaceName);
        if (found != null)
        {
            surfaceRoot = found;
            return found;
        }

        GameObject surface = new GameObject(SurfaceName);
        surfaceRoot = surface.transform;
        surfaceRoot.SetParent(transform, false);
        surfaceRoot.localPosition = new Vector3(0f, -1.35f, 1.7f);
        surfaceRoot.localRotation = Quaternion.Euler(-3f, 0f, 0f);
        surfaceRoot.localScale = Vector3.one;
        return surfaceRoot;
    }

    private void BuildSurface(Transform surface)
    {
        MeshFilter meshFilter = GetOrAdd<MeshFilter>(surface.gameObject);
        MeshRenderer meshRenderer = GetOrAdd<MeshRenderer>(surface.gameObject);
        meshRenderer.sharedMaterial = surfaceMaterial;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;

        Mesh mesh = GetSurfaceMesh();
        int segmentCount = Mathf.Max(2, segments);
        Vector3[] vertices = new Vector3[(segmentCount + 1) * 2];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[segmentCount * 6];

        for (int i = 0; i <= segmentCount; i++)
        {
            float t = i / (float)segmentCount;
            float eased = t * t * (3f - 2f * t);
            float halfWidth = Mathf.Lerp(widthTop, widthBottom, eased) * 0.5f;
            float y = Mathf.Lerp(height * 0.5f, -height * 0.5f, t);
            float z = Mathf.Sin(t * Mathf.PI) * forwardCurve + t * 0.18f;
            float edgeTaper = 1f - Mathf.Abs(t - 0.55f) * 0.1f;
            halfWidth *= edgeTaper;

            int v = i * 2;
            vertices[v] = new Vector3(-halfWidth, y, z);
            vertices[v + 1] = new Vector3(halfWidth, y, z);
            uvs[v] = new Vector2(0f, 1f - t);
            uvs[v + 1] = new Vector2(1f, 1f - t);
        }

        int ti = 0;
        for (int i = 0; i < segmentCount; i++)
        {
            int a = i * 2;
            int b = a + 1;
            int c = a + 2;
            int d = a + 3;
            triangles[ti++] = a;
            triangles[ti++] = c;
            triangles[ti++] = b;
            triangles[ti++] = b;
            triangles[ti++] = c;
            triangles[ti++] = d;
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        meshFilter.sharedMesh = mesh;
    }

    private Mesh GetSurfaceMesh()
    {
        if (generatedMesh == null)
        {
            generatedMesh = new Mesh
            {
                name = "WW_StylizedWaterfall_SurfaceMesh",
                hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild
            };
        }

        return generatedMesh;
    }

    private void BuildSplash(Transform surface)
    {
        Transform splashRoot = surface.Find(SplashRootName);
        if (splashRoot == null)
        {
            GameObject splash = new GameObject(SplashRootName);
            splash.hideFlags = HideFlags.DontSaveInEditor;
            splashRoot = splash.transform;
            splashRoot.SetParent(surface, false);
        }

        splashRoot.localPosition = new Vector3(0f, -height * 0.5f - 0.08f, forwardCurve + 0.42f);
        splashRoot.localRotation = Quaternion.identity;
        splashRoot.localScale = Vector3.one;

        ConfigureBurstSystem(ResolveParticleChild(splashRoot, BurstName), false);
        ConfigureBurstSystem(ResolveParticleChild(splashRoot, MistName), true);
    }

    private void BuildAudio()
    {
        if (!autoInstallAudio)
        {
            return;
        }

        if (waterfallLoopCue == null)
        {
            waterfallLoopCue = WonderlandRuntimeAudioLibrary.LoadCue("WW_Spatial_WaterfallLoop");
        }

        if (waterfallDetailCue == null)
        {
            waterfallDetailCue = WonderlandRuntimeAudioLibrary.LoadCue("WW_Spatial_WaterfallDetail");
        }

        Vector3 surfaceLocalPosition = surfaceRoot != null ? surfaceRoot.localPosition : new Vector3(0f, -1.35f, 1.7f);
        Vector3 mainAudioPosition = surfaceLocalPosition + new Vector3(0f, 0f, forwardCurve * 0.45f);
        Vector3 splashAudioPosition = surfaceLocalPosition + new Vector3(0f, -height * 0.5f - 0.08f, forwardCurve + 0.42f);

        ConfigureAudioLoop(ResolveAudioChild(AudioMainName, mainAudioPosition), waterfallLoopCue, 0.8f, 0.3f);
        ConfigureAudioLoop(ResolveAudioChild(AudioDetailName, splashAudioPosition), waterfallDetailCue, 0.8f, 0.3f);
    }

    private void QueueAudioInstall()
    {
        if (!autoInstallAudio)
        {
            return;
        }

        bool isPlaying = Application.isPlaying;
        if (audioInstallQueued && audioInstallQueuedInPlayMode == isPlaying)
        {
            return;
        }

        audioInstallQueued = true;
        audioInstallQueuedInPlayMode = isPlaying;

        if (isPlaying)
        {
            StartCoroutine(InstallAudioNextFrame());
            return;
        }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall += InstallEditorAudioAfterValidation;
#else
        audioInstallQueued = false;
#endif
    }

    private System.Collections.IEnumerator InstallAudioNextFrame()
    {
        yield return null;
        audioInstallQueued = false;
        if (this != null && isActiveAndEnabled)
        {
            BuildAudio();
        }
    }

#if UNITY_EDITOR
    private void QueueEditorRebuild()
    {
        if (editorRebuildQueued)
        {
            return;
        }

        editorRebuildQueued = true;
        UnityEditor.EditorApplication.delayCall += RebuildAfterValidation;
    }

    private void RebuildAfterValidation()
    {
        editorRebuildQueued = false;
        if (this == null || Application.isPlaying || !isActiveAndEnabled)
        {
            return;
        }

        Rebuild();
    }

    private void InstallEditorAudioAfterValidation()
    {
        if (this == null)
        {
            audioInstallQueued = false;
            return;
        }

        if (Application.isPlaying)
        {
            if (!audioInstallQueuedInPlayMode)
            {
                audioInstallQueued = false;
            }

            return;
        }

        audioInstallQueued = false;
        if (!isActiveAndEnabled)
        {
            return;
        }

        BuildAudio();
    }
#endif

    private Transform ResolveAudioChild(string childName, Vector3 localPosition)
    {
        Transform child = transform.Find(childName);
        if (child == null)
        {
            GameObject go = new GameObject(childName);
            child = go.transform;
            child.SetParent(transform, false);
        }

        child.localPosition = localPosition;
        child.localRotation = Quaternion.identity;
        child.localScale = Vector3.one;
        return child;
    }

    private static void ConfigureAudioLoop(Transform holder, WonderlandAudioCue cue, float fadeIn, float fadeOut)
    {
        if (holder == null || cue == null)
        {
            return;
        }

        WonderlandAmbientLoop loop = GetOrAdd<WonderlandAmbientLoop>(holder.gameObject);
        loop.Configure(cue, playOnEnable: true, volumeScale: 1f, fadeInSeconds: fadeIn, fadeOutSeconds: fadeOut);

        if (Application.isPlaying)
        {
            loop.Play();
        }
    }

    private Transform ResolveParticleChild(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child != null)
        {
            return child;
        }

        GameObject go = new GameObject(childName);
        go.hideFlags = HideFlags.DontSaveInEditor;
        child = go.transform;
        child.SetParent(parent, false);
        return child;
    }

    private void ConfigureBurstSystem(Transform holder, bool mist)
    {
        ParticleSystem ps = GetOrAdd<ParticleSystem>(holder.gameObject);
        ParticleSystemRenderer renderer = GetOrAdd<ParticleSystemRenderer>(holder.gameObject);
        bool shouldPlay = ps.isPlaying || ps.main.playOnAwake;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        holder.localPosition = mist ? new Vector3(0f, 0.15f, 0.18f) : Vector3.zero;
        holder.localRotation = Quaternion.identity;
        holder.localScale = Vector3.one;

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.duration = mist ? 2.0f : 1.2f;
        main.startLifetime = mist ? new ParticleSystem.MinMaxCurve(1.45f, 2.8f) : new ParticleSystem.MinMaxCurve(0.55f, 1.05f);
        main.startSpeed = mist ? new ParticleSystem.MinMaxCurve(0.25f, 0.78f) : new ParticleSystem.MinMaxCurve(1.25f, 3.8f);
        main.startSize = mist ? new ParticleSystem.MinMaxCurve(0.28f, 0.72f) : new ParticleSystem.MinMaxCurve(0.18f, 0.64f);
        main.startColor = mist
            ? new ParticleSystem.MinMaxGradient(new Color(0.62f, 0.9f, 1f, 0.46f), new Color(1f, 1f, 1f, 0.78f))
            : new ParticleSystem.MinMaxGradient(new Color(0.62f, 0.88f, 1f, 0.62f), new Color(1f, 1f, 1f, 0.92f));
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.maxParticles = mist ? 520 : 260;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = mist ? mistRate * 3.15f : splashRate;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = mist ? new Vector3(splashWidth * 0.95f, 0.15f, 0.2f) : new Vector3(splashWidth, 0.08f, 0.18f);
        shape.position = Vector3.zero;
        shape.rotation = Vector3.zero;

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = mist ? new ParticleSystem.MinMaxCurve(-0.48f, 0.48f) : new ParticleSystem.MinMaxCurve(-1.65f, 1.65f);
        velocity.y = mist ? new ParticleSystem.MinMaxCurve(0.22f, 0.75f) : new ParticleSystem.MinMaxCurve(0.75f, splashHeight * 2.15f);
        velocity.z = mist ? new ParticleSystem.MinMaxCurve(0.02f, 0.62f) : new ParticleSystem.MinMaxCurve(-0.45f, 1.25f);

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        AnimationCurve sizeCurve = mist
            ? new AnimationCurve(new Keyframe(0f, 0.28f), new Keyframe(0.32f, 0.72f), new Keyframe(1f, 0.95f))
            : new AnimationCurve(new Keyframe(0f, 0.06f), new Keyframe(0.14f, 0.95f), new Keyframe(0.58f, 0.62f), new Keyframe(1f, 0.035f));
        size.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var color = ps.colorOverLifetime;
        color.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.72f, 0.92f, 1f), 0f),
                new GradientColorKey(Color.white, 0.38f),
                new GradientColorKey(new Color(0.5f, 0.82f, 0.95f), 1f)
            },
            mist
                ? new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.78f, 0.16f), new GradientAlphaKey(0.55f, 0.72f), new GradientAlphaKey(0f, 1f) }
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
        renderer.minParticleSize = 0.01f;
        renderer.maxParticleSize = mist ? 1.25f : 1.8f;
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
