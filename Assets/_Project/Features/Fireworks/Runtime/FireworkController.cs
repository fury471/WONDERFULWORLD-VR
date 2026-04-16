using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WonderfulWorld.Features.Fireworks
{
    public class FireworkController : MonoBehaviour
    {
        [Header("Playback")]
        [SerializeField] private Transform launchPoint;
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 targetOffset = new Vector3(0f, 2f, 6f);
        [SerializeField] private FireworkPatternLibrary_SO patternLibrary;
        [SerializeField] private List<FireworkPattern> patterns = new List<FireworkPattern>();
        [SerializeField] private bool loopSequence;
        [SerializeField] private bool playOnStart;
        [SerializeField] private float initialDelay = 0.5f;
        [Header("Debug Spark Timing")]
        [SerializeField] private float debugSparkVisibleDuration = 3f;
        [SerializeField] private float debugSparkFadeDuration = 1f;

        private Coroutine playbackRoutine;
        private bool isPlaying;
        private Transform debugSparkRoot;

        public event Action SequenceStarted;
        public event Action SequenceStopped;
        public event Action<FireworkPattern, Vector3> PatternSpawned;

        public bool IsPlaying => isPlaying;
        public int PatternCount => patterns.Count;
        public IReadOnlyList<FireworkPattern> Patterns => patterns;

        private void Reset()
        {
            launchPoint = transform;

            if (patterns.Count == 0)
            {
                patterns = GetDefaultPatterns();
            }
        }

        private void Awake()
        {
            if (launchPoint == null)
            {
                launchPoint = transform;
            }

            LoadPatternsFromLibraryIfNeeded(forceRefresh: patternLibrary != null);
        }

        private void Start()
        {
            if (playOnStart)
            {
                PlaySequence();
            }
        }

        public void PlaySequence()
        {
            if (playbackRoutine != null)
            {
                StopCoroutine(playbackRoutine);
            }

            playbackRoutine = StartCoroutine(PlaySequenceRoutine());
            SequenceStarted?.Invoke();
        }

        public void PlayPattern(int patternIndex)
        {
            if (patternIndex < 0 || patternIndex >= patterns.Count)
            {
                Debug.LogWarning($"Pattern index {patternIndex} is out of range for {nameof(FireworkController)} on {name}.", this);
                return;
            }

            SpawnPattern(patterns[patternIndex]);
        }

        public void StopSequence()
        {
            if (playbackRoutine != null)
            {
                StopCoroutine(playbackRoutine);
                playbackRoutine = null;
            }

            isPlaying = false;
            SequenceStopped?.Invoke();
        }

        public void RefreshPatternsFromLibrary()
        {
            LoadPatternsFromLibraryIfNeeded(forceRefresh: true);
        }

        private void OnValidate()
        {
            if (patternLibrary != null)
            {
                LoadPatternsFromLibraryIfNeeded(forceRefresh: true);
            }
            else if (patterns == null || patterns.Count == 0)
            {
                patterns = GetDefaultPatterns();
            }
        }

        public List<string> GetPatternNames()
        {
            return patterns.Select(pattern => string.IsNullOrWhiteSpace(pattern.patternName) ? "Pattern" : pattern.patternName).ToList();
        }

        private IEnumerator PlaySequenceRoutine()
        {
            isPlaying = true;

            if (initialDelay > 0f)
            {
                yield return new WaitForSeconds(initialDelay);
            }

            do
            {
                for (int i = 0; i < patterns.Count; i++)
                {
                    FireworkPattern pattern = patterns[i];
                    SpawnPattern(pattern);

                    if (pattern.delayAfterLaunch > 0f)
                    {
                        yield return new WaitForSeconds(pattern.delayAfterLaunch);
                    }
                    else
                    {
                        yield return null;
                    }
                }
            }
            while (loopSequence);

            isPlaying = false;
            playbackRoutine = null;
            SequenceStopped?.Invoke();
        }

        private void SpawnPattern(FireworkPattern pattern)
        {
            Vector3 spawnPosition = ResolveSpawnPosition(pattern);
            PatternSpawned?.Invoke(pattern, spawnPosition);

            if (pattern.effectPrefab != null)
            {
                ParticleSystem spawned = Instantiate(pattern.effectPrefab, spawnPosition, Quaternion.identity);
                var main = spawned.main;
                main.startColor = pattern.color;
                main.startSizeMultiplier *= pattern.sizeMultiplier;
                spawned.Play();
                Destroy(spawned.gameObject, main.duration + main.startLifetime.constantMax + 1f);
                return;
            }

            SpawnDebugBurst(pattern, spawnPosition);
        }

        private Vector3 ResolveSpawnPosition(FireworkPattern pattern)
        {
            if (target != null)
            {
                return target.position + targetOffset;
            }

            Transform spawnRoot = launchPoint != null ? launchPoint : transform;
            return spawnRoot.position + Vector3.up * pattern.heightOffset;
        }

        private void SpawnDebugBurst(FireworkPattern pattern, Vector3 center)
        {
            float step = 360f / Mathf.Max(1, pattern.debugBurstCount);

            for (int i = 0; i < pattern.debugBurstCount; i++)
            {
                float angle = step * i;
                Vector3 direction;
                Vector3 point;

                switch (pattern.shape)
                {
                    case FireworkShape.Heart:
                        point = center + GetHeartPoint(i, pattern.debugBurstCount, pattern.radius);
                        CreateDebugSpark(point, pattern);
                        continue;
                    case FireworkShape.Ring:
                        direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                        break;
                    case FireworkShape.Star:
                        point = center + GetStarPoint(i, pattern.debugBurstCount, pattern.radius);
                        CreateDebugSpark(point, pattern);
                        continue;
                    default:
                        direction = Quaternion.Euler(0f, angle, angle * 0.5f) * Vector3.up;
                        break;
                }

                point = center + direction.normalized * pattern.radius;
                CreateDebugSpark(point, pattern);
            }
        }

        private static Vector3 GetStarPoint(int index, int count, float radius)
        {
            const int vertexCount = 10;
            Vector3[] vertices = new Vector3[vertexCount];
            float outerRadius = radius;
            float innerRadius = radius * 0.42f;

            for (int i = 0; i < vertexCount; i++)
            {
                float angle = -Mathf.PI * 0.5f + i * Mathf.PI / 5f;
                float currentRadius = i % 2 == 0 ? outerRadius : innerRadius;
                vertices[i] = new Vector3(Mathf.Cos(angle) * currentRadius, Mathf.Sin(angle) * currentRadius, 0f);
            }

            float normalized = count <= 1 ? 0f : index / (float)count;
            float scaled = normalized * vertexCount;
            int startIndex = Mathf.FloorToInt(scaled) % vertexCount;
            int endIndex = (startIndex + 1) % vertexCount;
            float edgeT = scaled - Mathf.Floor(scaled);
            return Vector3.Lerp(vertices[startIndex], vertices[endIndex], edgeT);
        }

        private static Vector3 GetHeartPoint(int index, int count, float radius)
        {
            float t = count <= 1 ? 0f : index / (float)(count - 1);
            float angle = Mathf.Lerp(0f, Mathf.PI * 2f, t);
            float x = 16f * Mathf.Pow(Mathf.Sin(angle), 3f);
            float y =
                13f * Mathf.Cos(angle) -
                5f * Mathf.Cos(2f * angle) -
                2f * Mathf.Cos(3f * angle) -
                Mathf.Cos(4f * angle);

            Vector3 point = new Vector3(x, y, 0f);
            point.x *= radius / 18f;
            point.y *= radius / 18f;
            point.y += radius * 0.2f;
            return point;
        }

        private void CreateDebugSpark(Vector3 position, FireworkPattern pattern)
        {
            GameObject spark = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            spark.name = $"FireworkSpark_{pattern.patternName}";
            spark.transform.position = position;
            spark.transform.localScale = Vector3.one * Mathf.Max(0.1f, pattern.sizeMultiplier * 0.35f);
            spark.transform.SetParent(GetOrCreateDebugSparkRoot(), true);

            if (Application.isPlaying)
            {
                spark.hideFlags = HideFlags.HideInHierarchy;
            }

            Collider collider = spark.GetComponent<Collider>();
            if (collider != null)
            {
                SafeDestroy(collider);
            }

            Renderer renderer = spark.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material sparkMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                sparkMaterial.color = pattern.color;

                if (!Application.isPlaying)
                {
                    sparkMaterial.hideFlags = HideFlags.HideAndDontSave;
                }

                renderer.sharedMaterial = sparkMaterial;
                StartCoroutine(FadeAndDestroySpark(renderer, spark, sparkMaterial, pattern));
                return;
            }

            SafeDestroy(spark);
        }

        private Transform GetOrCreateDebugSparkRoot()
        {
            if (debugSparkRoot != null)
            {
                return debugSparkRoot;
            }

            Transform existing = transform.Find("_DebugFireworkSparks");
            if (existing != null)
            {
                debugSparkRoot = existing;
                return debugSparkRoot;
            }

            GameObject root = new GameObject("_DebugFireworkSparks");
            root.transform.SetParent(transform, false);

            if (Application.isPlaying)
            {
                root.hideFlags = HideFlags.HideInHierarchy;
            }

            debugSparkRoot = root.transform;
            return debugSparkRoot;
        }

        private IEnumerator FadeAndDestroySpark(Renderer renderer, GameObject spark, Material sparkMaterial, FireworkPattern pattern)
        {
            float visibleDuration = Mathf.Max(0f, debugSparkVisibleDuration);
            float fadeDuration = Mathf.Max(0.1f, debugSparkFadeDuration);
            Color startColor = sparkMaterial.color;

            yield return new WaitForSeconds(visibleDuration);

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                Color fadedColor = startColor;
                fadedColor.a = Mathf.Lerp(startColor.a, 0f, t);
                sparkMaterial.color = fadedColor;
                yield return null;
            }

            SafeDestroy(sparkMaterial);

            if (spark != null)
            {
                SafeDestroy(spark);
            }
        }

        private static void SafeDestroy(UnityEngine.Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }

        private void LoadPatternsFromLibraryIfNeeded(bool forceRefresh = false)
        {
            if (patternLibrary != null)
            {
                if (forceRefresh || patterns.Count == 0)
                {
                    patterns = patternLibrary.CreatePatternCopies();
                }

                return;
            }

            if (patterns.Count == 0)
            {
                patterns = GetDefaultPatterns();
            }
        }

        internal static List<FireworkPattern> GetDefaultPatterns()
        {
            return new List<FireworkPattern>
            {
                new FireworkPattern
                {
                    patternName = "Star",
                    shape = FireworkShape.Star,
                    color = new Color(1f, 0.45f, 0.2f),
                    heightOffset = 8f,
                    radius = 2.5f,
                    delayAfterLaunch = 1f,
                    sizeMultiplier = 1.2f,
                    sparkLifetime = 1f,
                    debugBurstCount = 30
                },
                new FireworkPattern
                {
                    patternName = "Ring",
                    shape = FireworkShape.Ring,
                    color = new Color(0.3f, 0.8f, 1f),
                    heightOffset = 11f,
                    radius = 3.2f,
                    delayAfterLaunch = 1.2f,
                    sizeMultiplier = 1f,
                    sparkLifetime = 1.1f,
                    debugBurstCount = 14
                },
                new FireworkPattern
                {
                    patternName = "Heart",
                    shape = FireworkShape.Heart,
                    color = new Color(1f, 0.35f, 0.55f),
                    heightOffset = 7f,
                    radius = 3f,
                    delayAfterLaunch = 1.1f,
                    sizeMultiplier = 1.1f,
                    sparkLifetime = 0.9f,
                    debugBurstCount = 40,
                    fanArc = 120f
                }
            };
        }
    }

    public enum FireworkShape
    {
        Star,
        Ring,
        Heart
    }

    [Serializable]
    public class FireworkPattern
    {
        public string patternName = "Pattern";
        public FireworkShape shape = FireworkShape.Star;
        public ParticleSystem effectPrefab;
        public Color color = Color.white;
        public float heightOffset = 8f;
        public float radius = 3f;
        public float delayAfterLaunch = 1f;
        public float sizeMultiplier = 1f;
        public float sparkLifetime = 1f;
        public int debugBurstCount = 12;
        public float fanArc = 90f;
    }

}
