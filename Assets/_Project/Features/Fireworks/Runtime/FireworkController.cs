using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WonderfulWorld.Features.Fireworks
{
    [DisallowMultipleComponent]
    public class FireworkController : MonoBehaviour
    {
        private static readonly MathFireworkPattern[] AllShowcaseMathPatterns =
        {
            MathFireworkPattern.Heart,
            MathFireworkPattern.Ring,
            MathFireworkPattern.Spiral,
            MathFireworkPattern.Sphere,
            MathFireworkPattern.Flower,
            MathFireworkPattern.Star,
            MathFireworkPattern.Mobius
        };

        [Header("Launch")]
        [SerializeField] private Transform launchPoint;
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 targetOffset = new Vector3(0f, 2f, 6f);

        [Header("Showcase")]
        [SerializeField] private bool playOnStart;
        [SerializeField] private bool loopShowcase;
        [SerializeField] private float initialDelay = 0.5f;
        [SerializeField] private float delayBetweenShowcaseLaunches = 3.6f;
        [SerializeField] private string showcaseText = "DREAM";
        [SerializeField]
        private List<MathFireworkPattern> showcaseMathPatterns = new List<MathFireworkPattern>
        {
            MathFireworkPattern.Heart,
            MathFireworkPattern.Sphere,
            MathFireworkPattern.Flower,
            MathFireworkPattern.Mobius
        };

        [Header("Point Cloud Fireworks")]
        [SerializeField] private PointCloudFireworkRenderer pointCloudRenderer;
        [SerializeField] private float pointCloudHeightOffset = 13f;
        [SerializeField] private float pointCloudForwardOffset = 7f;
        [SerializeField] private float pointCloudScale = 3.8f;
        [SerializeField] private float textPointCloudScaleMultiplier = 1.5f;
        [SerializeField] private float mathPointCloudScaleMultiplier = 3.8f;
        [SerializeField] private int textPointBudget = 980;
        [SerializeField] private int mathPointBudget = 4800;
        [SerializeField] private Color textFireworkColor = new Color(1f, 0.76f, 0.48f, 1f);
        [SerializeField] private Color mathFireworkColor = new Color(0.56f, 0.95f, 1f, 1f);

        [Header("Performance")]
        [SerializeField] private FireworkQualityMode qualityMode = FireworkQualityMode.Balanced;
        [SerializeField] private bool usePatternBudgetTuning = true;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip launchAudioClip;
        [SerializeField] private AudioClip burstAudioClip;
        [SerializeField] private float launchAudioVolume = 0.55f;
        [SerializeField] private float burstAudioVolume = 0.9f;
        [SerializeField] private float pointCloudBurstAudioDelay = 1.05f;

        private Coroutine showcaseRoutine;
        private Coroutine pointCloudAudioRoutine;

        public event Action SequenceStarted;
        public event Action SequenceStopped;
        public event Action<PointCloudFireworkRequest, Vector3> PointCloudFireworkSpawned;

        public bool IsPlaying => showcaseRoutine != null || (pointCloudRenderer != null && pointCloudRenderer.IsPlaying);
        public bool IsShowcasePlaying => showcaseRoutine != null;

        public int PatternCount => showcaseMathPatterns?.Count ?? 0;

        public string GetText()
        {
            return showcaseText;
        }

        private void Reset()
        {
            launchPoint = transform;
            EnsureShowcasePatterns();
        }

        private void Awake()
        {
            if (launchPoint == null)
            {
                launchPoint = transform;
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            EnsureShowcasePatterns();
        }

        private void Start()
        {
            if (playOnStart)
            {
                PlaySequence();
            }
        }

        private void OnValidate()
        {
            pointCloudHeightOffset = Mathf.Max(0f, pointCloudHeightOffset);
            pointCloudForwardOffset = Mathf.Max(0f, pointCloudForwardOffset);
            pointCloudScale = Mathf.Max(0.1f, pointCloudScale);
            textPointCloudScaleMultiplier = Mathf.Max(0.1f, textPointCloudScaleMultiplier);
            mathPointCloudScaleMultiplier = Mathf.Max(0.1f, mathPointCloudScaleMultiplier);
            textPointBudget = Mathf.Clamp(textPointBudget, FireworkPointCloudGenerator.MinPointBudget, FireworkPointCloudGenerator.MaxPointBudget);
            mathPointBudget = Mathf.Clamp(mathPointBudget, FireworkPointCloudGenerator.MinPointBudget, FireworkPointCloudGenerator.MaxPointBudget);
            initialDelay = Mathf.Max(0f, initialDelay);
            delayBetweenShowcaseLaunches = Mathf.Max(0.1f, delayBetweenShowcaseLaunches);
            pointCloudBurstAudioDelay = Mathf.Max(0f, pointCloudBurstAudioDelay);
            launchAudioVolume = Mathf.Clamp01(launchAudioVolume);
            burstAudioVolume = Mathf.Clamp01(burstAudioVolume);
            showcaseText = FireworkPointCloudGenerator.SanitizeText(showcaseText);
            EnsureShowcasePatterns();
        }

        public void PlaySequence()
        {
            BeginShowcase(PlayConfiguredShowcaseRoutine());
        }

        public void PlayAllSequence()
        {
            BeginShowcase(PlayAllShowcaseRoutine());
        }

        private void BeginShowcase(IEnumerator routine)
        {
            if (showcaseRoutine != null)
            {
                StopCoroutine(showcaseRoutine);
            }

            showcaseRoutine = StartCoroutine(routine);
            SequenceStarted?.Invoke();
        }

        public void PlayPattern(int patternIndex)
        {
            EnsureShowcasePatterns();
            int safeIndex = Mathf.Abs(patternIndex) % showcaseMathPatterns.Count;
            LaunchMathFirework(showcaseMathPatterns[safeIndex]);
        }

        public void LaunchTextFirework(string text)
        {
            string sanitizedText = FireworkPointCloudGenerator.SanitizeText(text);
            PointCloudFireworkRequest request = PointCloudFireworkRequest.Text(
                sanitizedText,
                textFireworkColor,
                pointCloudScale * textPointCloudScaleMultiplier,
                ResolveTextPointBudget(sanitizedText));
            LaunchPointCloudFirework(request);
        }

        public void LaunchMathFirework(MathFireworkPattern pattern)
        {
            PointCloudFireworkRequest request = PointCloudFireworkRequest.Math(
                pattern,
                mathFireworkColor,
                pointCloudScale * mathPointCloudScaleMultiplier,
                ResolveMathPointBudget(pattern));
            LaunchPointCloudFirework(request);
        }

        public void LaunchPointCloudFirework(PointCloudFireworkRequest request)
        {
            if (!Application.isPlaying)
            {
                Debug.Log("[Fireworks] Enter Play Mode to preview point-cloud fireworks safely.");
                return;
            }

            EnsurePointCloudRenderer();

            List<Vector3> points = FireworkPointCloudGenerator.Generate(request);
            Vector3 origin = launchPoint != null ? launchPoint.position : transform.position;
            Vector3 center = ResolvePointCloudCenter();
            PlayLaunchAudio();
            pointCloudRenderer.Play(
                points,
                origin,
                center,
                request.color,
                request.particleSizeMultiplier,
                request.autoRotate,
                request.rotationSpeedDegrees);
            ScheduleBurstAudio(pointCloudBurstAudioDelay);
            PointCloudFireworkSpawned?.Invoke(request, center);
        }

        public void StopSequence()
        {
            if (showcaseRoutine != null)
            {
                StopCoroutine(showcaseRoutine);
                showcaseRoutine = null;
            }

            SequenceStopped?.Invoke();
        }

        public void RefreshPatternsFromLibrary()
        {
        }

        private IEnumerator PlayConfiguredShowcaseRoutine()
        {
            if (initialDelay > 0f)
            {
                yield return new WaitForSeconds(initialDelay);
            }

            do
            {
                LaunchTextFirework(showcaseText);
                yield return new WaitForSeconds(delayBetweenShowcaseLaunches);

                EnsureShowcasePatterns();
                for (int i = 0; i < showcaseMathPatterns.Count; i++)
                {
                    LaunchMathFirework(showcaseMathPatterns[i]);
                    yield return new WaitForSeconds(delayBetweenShowcaseLaunches);
                }
            }
            while (loopShowcase);

            showcaseRoutine = null;
            SequenceStopped?.Invoke();
        }

        private IEnumerator PlayAllShowcaseRoutine()
        {
            if (initialDelay > 0f)
            {
                yield return new WaitForSeconds(initialDelay);
            }

            do
            {
                LaunchTextFirework(showcaseText);
                yield return new WaitForSeconds(delayBetweenShowcaseLaunches);

                for (int i = 0; i < AllShowcaseMathPatterns.Length; i++)
                {
                    LaunchMathFirework(AllShowcaseMathPatterns[i]);
                    yield return new WaitForSeconds(delayBetweenShowcaseLaunches);
                }
            }
            while (loopShowcase);

            showcaseRoutine = null;
            SequenceStopped?.Invoke();
        }

        private void EnsureShowcasePatterns()
        {
            if (showcaseMathPatterns == null)
            {
                showcaseMathPatterns = new List<MathFireworkPattern>();
            }

            if (showcaseMathPatterns.Count == 0)
            {
                showcaseMathPatterns.Add(MathFireworkPattern.Heart);
                showcaseMathPatterns.Add(MathFireworkPattern.Sphere);
                showcaseMathPatterns.Add(MathFireworkPattern.Flower);
                showcaseMathPatterns.Add(MathFireworkPattern.Mobius);
            }
        }

        private Vector3 ResolvePointCloudCenter()
        {
            if (target != null)
            {
                return target.position + targetOffset;
            }

            Transform spawnRoot = launchPoint != null ? launchPoint : transform;
            return spawnRoot.position
                + Vector3.up * pointCloudHeightOffset
                + spawnRoot.forward * pointCloudForwardOffset;
        }

        private int ResolveTextPointBudget(string text)
        {
            int visibleLength = Mathf.Max(1, string.IsNullOrWhiteSpace(text) ? 0 : text.Trim().Length);
            int lengthAwareBudget = 520 + visibleLength * 320;
            return Mathf.Clamp(
                Mathf.RoundToInt(Mathf.Max(textPointBudget, lengthAwareBudget) * ResolveQualityMultiplier()),
                FireworkPointCloudGenerator.MinPointBudget,
                FireworkPointCloudGenerator.MaxPointBudget);
        }

        private int ResolveMathPointBudget(MathFireworkPattern pattern)
        {
            float multiplier = ResolveQualityMultiplier();
            if (usePatternBudgetTuning)
            {
                multiplier *= ResolvePatternBudgetMultiplier(pattern);
            }

            return Mathf.Clamp(
                Mathf.RoundToInt(mathPointBudget * multiplier),
                FireworkPointCloudGenerator.MinPointBudget,
                FireworkPointCloudGenerator.MaxPointBudget);
        }

        private float ResolveQualityMultiplier()
        {
            return qualityMode switch
            {
                FireworkQualityMode.Performance => 0.72f,
                FireworkQualityMode.Showcase => 1.12f,
                _ => 1f
            };
        }

        private static float ResolvePatternBudgetMultiplier(MathFireworkPattern pattern)
        {
            return pattern switch
            {
                MathFireworkPattern.Heart => 1.04f,
                MathFireworkPattern.Sphere => 1.02f,
                MathFireworkPattern.Flower => 1.04f,
                MathFireworkPattern.Mobius => 0.82f,
                MathFireworkPattern.Spiral => 0.78f,
                MathFireworkPattern.Ring => 0.72f,
                MathFireworkPattern.Star => 0.86f,
                _ => 1f
            };
        }

        private void EnsurePointCloudRenderer()
        {
            if (pointCloudRenderer != null)
            {
                return;
            }

            pointCloudRenderer = GetComponentInChildren<PointCloudFireworkRenderer>(true);
            if (pointCloudRenderer != null)
            {
                return;
            }

            GameObject rendererObject = new GameObject("PointCloudFireworkRenderer");
            rendererObject.transform.SetParent(transform, false);
            pointCloudRenderer = rendererObject.AddComponent<PointCloudFireworkRenderer>();
        }

        private void PlayLaunchAudio()
        {
            if (launchAudioClip == null)
            {
                return;
            }

            EnsureAudioSource();
            audioSource.PlayOneShot(launchAudioClip, launchAudioVolume);
        }

        private void ScheduleBurstAudio(float delay)
        {
            if (burstAudioClip == null)
            {
                return;
            }

            if (pointCloudAudioRoutine != null)
            {
                StopCoroutine(pointCloudAudioRoutine);
            }

            pointCloudAudioRoutine = StartCoroutine(PlayBurstAudioAfterDelay(Mathf.Max(0f, delay)));
        }

        private IEnumerator PlayBurstAudioAfterDelay(float delay)
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            EnsureAudioSource();
            audioSource.PlayOneShot(burstAudioClip, burstAudioVolume);
            pointCloudAudioRoutine = null;
        }

        private void EnsureAudioSource()
        {
            if (audioSource != null)
            {
                return;
            }

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1f;
                audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
                audioSource.maxDistance = 80f;
            }
        }
    }
}
