using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace WonderfulWorld.Features.Fireworks
{
    [DisallowMultipleComponent]
    public class FireworkController : MonoBehaviour
    {
        private static readonly MathFireworkPattern[] DefaultShowcaseMathPatterns =
        {
            MathFireworkPattern.Heart,
            MathFireworkPattern.DoubleHelix,
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
        private List<FireworkShowcaseStep> showcaseSequence = CreateDefaultShowcaseSequence();

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

        [Header("Pattern Palette")]
        [SerializeField] private bool usePatternPalette = true;
        [SerializeField] private Color heartColor = new Color(1f, 0.33f, 0.62f, 1f);
        [FormerlySerializedAs("ringColor")]
        [SerializeField] private Color doubleHelixColor = new Color(0.38f, 0.92f, 1f, 1f);
        [SerializeField] private Color spiralColor = new Color(0.7f, 0.96f, 0.45f, 1f);
        [SerializeField] private Color sphereColor = new Color(0.82f, 0.88f, 1f, 1f);
        [SerializeField] private Color flowerColor = new Color(1f, 0.48f, 0.95f, 1f);
        [SerializeField] private Color starColor = new Color(1f, 0.78f, 0.26f, 1f);
        [SerializeField] private Color mobiusColor = new Color(0.62f, 0.72f, 1f, 1f);

        [Header("Performance")]
        [SerializeField] private FireworkQualityMode qualityMode = FireworkQualityMode.Balanced;
        [SerializeField] private bool usePatternBudgetTuning = true;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip launchAudioClip;
        [SerializeField] private AudioClip burstAudioClip;
        [SerializeField] private float launchAudioVolume = 0.55f;
        [SerializeField] private float burstAudioVolume = 0.9f;
        [SerializeField] private float pointCloudBurstAudioDelay = 0.82f;
        [SerializeField] private bool useProceduralAudioFallback = true;

        private Coroutine showcaseRoutine;
        private static AudioClip proceduralLaunchClip;
        private static AudioClip proceduralBurstClip;

        public event Action SequenceStarted;
        public event Action SequenceStopped;
        public event Action<PointCloudFireworkRequest, Vector3> PointCloudFireworkSpawned;

        public bool IsPlaying => showcaseRoutine != null || (pointCloudRenderer != null && pointCloudRenderer.IsPlaying);
        public bool IsShowcasePlaying => showcaseRoutine != null;

        public int ShowcaseStepCount => showcaseSequence?.Count ?? 0;

        public string ShowcaseText => showcaseText;

        private void Reset()
        {
            launchPoint = transform;
            pointCloudRenderer = GetComponentInChildren<PointCloudFireworkRenderer>(true);
            audioSource = GetComponentInChildren<AudioSource>(true);
            EnsureShowcaseSequence();
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

            EnsureShowcaseSequence();
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
            EnsureShowcaseSequence();
        }

        public void PlaySequence()
        {
            BeginShowcase(PlayConfiguredShowcaseRoutine());
        }

        public void PlayAllSequence()
        {
            PlaySequence();
        }

        public void SetShowcaseText(string text)
        {
            showcaseText = FireworkPointCloudGenerator.SanitizeText(text);
        }

        public void LaunchShowcaseText()
        {
            LaunchTextFirework(showcaseText);
        }

        public void SetQualityMode(FireworkQualityMode mode)
        {
            qualityMode = mode;
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

        public void PlayShowcaseStep(int stepIndex)
        {
            EnsureShowcaseSequence();
            if (showcaseSequence.Count == 0)
            {
                return;
            }

            int safeIndex = Mathf.Abs(stepIndex) % showcaseSequence.Count;
            FireworkShowcaseStep step = showcaseSequence[safeIndex];
            if (step != null && step.enabled)
            {
                LaunchShowcaseStep(step);
            }
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
                ResolvePatternColor(pattern),
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
                request.rotationSpeedDegrees,
                request.rotationAxis,
                request.extraHoldDuration);
            ScheduleBurstAudio(pointCloudBurstAudioDelay, center);
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

        private IEnumerator PlayConfiguredShowcaseRoutine()
        {
            if (initialDelay > 0f)
            {
                yield return new WaitForSeconds(initialDelay);
            }

            do
            {
                EnsureShowcaseSequence();
                for (int i = 0; i < showcaseSequence.Count; i++)
                {
                    FireworkShowcaseStep step = showcaseSequence[i];
                    if (step == null || !step.enabled)
                    {
                        continue;
                    }

                    LaunchShowcaseStep(step);
                    yield return new WaitForSeconds(delayBetweenShowcaseLaunches);
                }
            }
            while (loopShowcase);

            showcaseRoutine = null;
            SequenceStopped?.Invoke();
        }

        private void EnsureShowcaseSequence()
        {
            if (showcaseSequence == null)
            {
                showcaseSequence = new List<FireworkShowcaseStep>();
            }

            for (int i = showcaseSequence.Count - 1; i >= 0; i--)
            {
                FireworkShowcaseStep step = showcaseSequence[i];
                if (step == null)
                {
                    showcaseSequence.RemoveAt(i);
                    continue;
                }

                if (step.kind == FireworkShowcaseStepKind.Text && !string.IsNullOrWhiteSpace(step.textOverride))
                {
                    step.textOverride = FireworkPointCloudGenerator.SanitizeText(step.textOverride);
                }

            }

            if (showcaseSequence.Count == 0)
            {
                showcaseSequence.AddRange(CreateDefaultShowcaseSequence());
            }
        }

        private void LaunchShowcaseStep(FireworkShowcaseStep step)
        {
            if (step.kind == FireworkShowcaseStepKind.Text)
            {
                string text = string.IsNullOrWhiteSpace(step.textOverride)
                    ? showcaseText
                    : step.textOverride;
                LaunchTextFirework(text);
                return;
            }

            LaunchMathFirework(step.mathPattern);
        }

        private static List<FireworkShowcaseStep> CreateDefaultShowcaseSequence()
        {
            List<FireworkShowcaseStep> steps = new List<FireworkShowcaseStep>
            {
                FireworkShowcaseStep.Text(),
            };

            for (int i = 0; i < DefaultShowcaseMathPatterns.Length; i++)
            {
                steps.Add(FireworkShowcaseStep.Math(DefaultShowcaseMathPatterns[i]));
            }

            return steps;
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
                MathFireworkPattern.DoubleHelix => 0.94f,
                MathFireworkPattern.Star => 0.86f,
                _ => 1f
            };
        }

        private Color ResolvePatternColor(MathFireworkPattern pattern)
        {
            if (!usePatternPalette)
            {
                return mathFireworkColor;
            }

            return pattern switch
            {
                MathFireworkPattern.Heart => heartColor,
                MathFireworkPattern.DoubleHelix => doubleHelixColor,
                MathFireworkPattern.Spiral => spiralColor,
                MathFireworkPattern.Sphere => sphereColor,
                MathFireworkPattern.Flower => flowerColor,
                MathFireworkPattern.Star => starColor,
                MathFireworkPattern.Mobius => mobiusColor,
                _ => mathFireworkColor
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
            Vector3 position = launchPoint != null ? launchPoint.position : transform.position;
            if (WonderfulWorld.Audio.WonderlandRuntimeAudioLibrary.LoadCue("WW_SFX_FireworkLaunch") != null)
            {
                WonderfulWorld.Audio.WonderlandAudioOneShotPlayer.PlayAt("WW_SFX_FireworkLaunch", position, volumeScale: 1f, maxVoices: 6);
                return;
            }

            AudioClip clip = launchAudioClip != null
                ? launchAudioClip
                : (useProceduralAudioFallback ? GetProceduralLaunchClip() : null);
            if (clip == null)
            {
                return;
            }

            EnsureAudioSource();
            audioSource.PlayOneShot(clip, launchAudioVolume);
        }

        private void ScheduleBurstAudio(float delay, Vector3 position)
        {
            if (burstAudioClip == null && !useProceduralAudioFallback)
            {
                if (WonderfulWorld.Audio.WonderlandRuntimeAudioLibrary.LoadCue("WW_SFX_FireworkBurst") == null)
                {
                    return;
                }
            }

            if (WonderfulWorld.Audio.WonderlandRuntimeAudioLibrary.LoadCue("WW_SFX_FireworkBurst") != null)
            {
                StartCoroutine(PlayWonderlandBurstAudioAfterDelay(Mathf.Max(0f, delay), position));
                return;
            }

            StartCoroutine(PlayBurstAudioAfterDelay(Mathf.Max(0f, delay)));
        }

        private IEnumerator PlayBurstAudioAfterDelay(float delay)
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            EnsureAudioSource();
            AudioClip clip = burstAudioClip != null ? burstAudioClip : GetProceduralBurstClip();
            if (clip != null)
            {
                audioSource.PlayOneShot(clip, burstAudioVolume);
            }

        }

        private IEnumerator PlayWonderlandBurstAudioAfterDelay(float delay, Vector3 position)
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            WonderfulWorld.Audio.WonderlandAudioOneShotPlayer.PlayAt(
                "WW_SFX_FireworkBurst",
                position,
                volumeScale: 1f,
                maxVoices: 6);
        }

        private void EnsureAudioSource()
        {
            if (audioSource != null)
            {
                return;
            }

            audioSource = GetComponentInChildren<AudioSource>(true);
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1f;
                audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
                audioSource.maxDistance = 80f;
            }
        }

        private static AudioClip GetProceduralLaunchClip()
        {
            if (proceduralLaunchClip == null)
            {
                proceduralLaunchClip = CreateProceduralLaunchClip();
            }

            return proceduralLaunchClip;
        }

        private static AudioClip GetProceduralBurstClip()
        {
            if (proceduralBurstClip == null)
            {
                proceduralBurstClip = CreateProceduralBurstClip();
            }

            return proceduralBurstClip;
        }

        private static AudioClip CreateProceduralLaunchClip()
        {
            const int sampleRate = 24000;
            const float duration = 0.82f;
            int samples = Mathf.RoundToInt(sampleRate * duration);
            float[] data = new float[samples];
            float phase = 0f;

            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)sampleRate;
                float normalized = t / duration;
                float frequency = Mathf.Lerp(520f, 1380f, Mathf.Pow(normalized, 0.55f));
                phase += frequency * Mathf.PI * 2f / sampleRate;
                float envelope = Mathf.Sin(Mathf.Clamp01(normalized) * Mathf.PI);
                float hiss = HashSigned(i) * 0.16f;
                data[i] = (Mathf.Sin(phase) * 0.78f + hiss) * envelope * 0.42f;
            }

            AudioClip clip = AudioClip.Create("Fireworks_ProceduralLaunch", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateProceduralBurstClip()
        {
            const int sampleRate = 24000;
            const float duration = 0.95f;
            int samples = Mathf.RoundToInt(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)sampleRate;
                float normalized = t / duration;
                float boomEnvelope = Mathf.Exp(-normalized * 8.5f);
                float crackleEnvelope = Mathf.Exp(-normalized * 3.2f);
                float lowBoom = Mathf.Sin(t * Mathf.PI * 2f * Mathf.Lerp(92f, 42f, normalized)) * boomEnvelope;
                float crackle = HashSigned(i * 7) * crackleEnvelope;
                float sparkle = Mathf.Sin(t * Mathf.PI * 2f * 880f + HashSigned(i) * 0.8f) * Mathf.Exp(-normalized * 5.5f);
                data[i] = Mathf.Clamp((lowBoom * 0.72f + crackle * 0.34f + sparkle * 0.12f) * 0.78f, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("Fireworks_ProceduralBurst", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static float HashSigned(int value)
        {
            unchecked
            {
                uint hash = (uint)value;
                hash ^= hash >> 16;
                hash *= 0x7FEB352Du;
                hash ^= hash >> 15;
                hash *= 0x846CA68Bu;
                hash ^= hash >> 16;
                return ((hash & 0xFFFFu) / 32767.5f) - 1f;
            }
        }
    }
}
