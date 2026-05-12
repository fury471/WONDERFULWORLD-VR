using System.Collections.Generic;
using UnityEngine;

namespace WonderfulWorld.Features.Fireworks
{
    [DisallowMultipleComponent]
    public class PointCloudFireworkRenderer : MonoBehaviour
    {
        [Header("Particle Output")]
        [SerializeField] private ParticleSystem particleSystemOutput;
        [SerializeField] private Material particleMaterial;
        [SerializeField] private int maxParticles = FireworkPointCloudGenerator.MaxPointBudget;
        [SerializeField] private float particleSize = 0.2f;
        [SerializeField] private bool sortParticlesByDistance;

        [Header("Timing")]
        [SerializeField] private float launchDuration = 0.82f;
        [SerializeField] private float expandDuration = 0.52f;
        [SerializeField] private float holdDuration = 1.2f;
        [SerializeField] private float fadeDuration = 0.42f;
        [SerializeField] private float extraFadeDuration = 0.08f;
        [SerializeField] private float emberDuration = 0.2f;
        [SerializeField] private float delayJitter = 0.06f;

        [Header("Motion")]
        [SerializeField] private float launchArcHeight = 4.2f;
        [SerializeField] private float launchTrailWidth = 0.045f;
        [SerializeField] private float launchTrailDepth = 0.02f;
        [SerializeField] private float bloomOvershoot = 0.44f;
        [SerializeField] private float holdExpansion = 0.48f;
        [SerializeField] private float idleDrift = 0.045f;
        [SerializeField] private float fallSpeed = 0.72f;
        [SerializeField] private Vector3 softWind = new Vector3(0.28f, 0f, 0.08f);

        [Header("Look")]
        [SerializeField] private Color secondaryColor = new Color(0.55f, 0.95f, 1f, 1f);
        [SerializeField] private float colorVariation = 0.22f;
        [SerializeField] private bool useRainbowTwinkle = true;
        [SerializeField, Range(0f, 1f)] private float rainbowBlend = 0.62f;
        [SerializeField] private float rainbowCycleSpeed = 0.14f;
        [SerializeField] private float burstFlashSizeMultiplier = 2.4f;
        [SerializeField] private float emberSizeMultiplier = 0.55f;

        [Header("Polish Layers")]
        [SerializeField] private bool useAccentParticles = true;
        [SerializeField, Range(0f, 0.35f)] private float accentParticleRatio = 0.16f;
        [SerializeField] private int maxAccentParticles = 620;
        [SerializeField] private int shockwaveParticleCount = 72;
        [SerializeField] private Color flashColor = new Color(1.4f, 1.12f, 0.72f, 1f);
        [SerializeField] private Color emberColor = new Color(1f, 0.48f, 0.18f, 1f);
        [SerializeField] private float shockwaveExpansion = 1.18f;
        [SerializeField] private float accentScatter = 0.38f;

        private const float MinFadeOffset = -0.04f;
        private const float MaxFadeOffset = 0.22f;
        private const float MinFadeDurationMultiplier = 0.72f;
        private const float MaxFadeDurationMultiplier = 1.05f;
        private const float LaunchBeamParticleRatio = 0.065f;
        private const byte ShapeParticle = 0;
        private const byte ShockwaveParticle = 1;
        private const byte EmberParticle = 2;
        private static Material defaultParticleMaterial;

        private readonly List<Vector3> localTargets = new List<Vector3>();
        private ParticleSystem.Particle[] particles = new ParticleSystem.Particle[0];
        private byte[] particleKinds = new byte[0];
        private float[] delays = new float[0];
        private float[] seeds = new float[0];
        private Color[] colors = new Color[0];
        private float[] fadeOffsets = new float[0];
        private float[] fadeDurations = new float[0];
        private float[] fallMultipliers = new float[0];
        private Vector3 launchOrigin;
        private Vector3 bloomCenter;
        private Quaternion shapeRotation = Quaternion.identity;
        private float elapsed;
        private float activeParticleSizeMultiplier = 1f;
        private bool activeAutoRotate;
        private float activeRotationSpeedDegrees;
        private Vector3 activeRotationAxis = Vector3.zero;
        private float activeExtraHoldDuration;
        private bool isPlaying;

        public bool IsPlaying => isPlaying;

        private void Awake()
        {
            EnsureParticleSystem();
        }

        private void OnValidate()
        {
            maxParticles = Mathf.Clamp(maxParticles, FireworkPointCloudGenerator.MinPointBudget, FireworkPointCloudGenerator.MaxPointBudget);
            particleSize = Mathf.Max(0.01f, particleSize);
            launchDuration = Mathf.Max(0.05f, launchDuration);
            expandDuration = Mathf.Max(0.05f, expandDuration);
            holdDuration = Mathf.Max(0f, holdDuration);
            fadeDuration = Mathf.Max(0.05f, fadeDuration);
            extraFadeDuration = Mathf.Max(0f, extraFadeDuration);
            emberDuration = Mathf.Max(0f, emberDuration);
            delayJitter = Mathf.Max(0f, delayJitter);
            launchTrailWidth = Mathf.Max(0f, launchTrailWidth);
            launchTrailDepth = Mathf.Max(0f, launchTrailDepth);
            rainbowBlend = Mathf.Clamp01(rainbowBlend);
            rainbowCycleSpeed = Mathf.Max(0f, rainbowCycleSpeed);
            burstFlashSizeMultiplier = Mathf.Max(1f, burstFlashSizeMultiplier);
            emberSizeMultiplier = Mathf.Clamp(emberSizeMultiplier, 0.05f, 1f);
            maxAccentParticles = Mathf.Clamp(maxAccentParticles, 0, FireworkPointCloudGenerator.MaxPointBudget);
            shockwaveParticleCount = Mathf.Clamp(shockwaveParticleCount, 0, FireworkPointCloudGenerator.MaxPointBudget);
            shockwaveExpansion = Mathf.Max(0.1f, shockwaveExpansion);
            accentScatter = Mathf.Max(0f, accentScatter);

            if (particleSystemOutput != null)
            {
                EnsureParticleSystem();
            }
        }

        public void Play(
            IReadOnlyList<Vector3> targetPoints,
            Vector3 origin,
            Vector3 center,
            Color primaryColor,
            float sizeMultiplier = 1f,
            bool autoRotate = false,
            float rotationSpeedDegrees = 0f,
            Vector3 rotationAxis = default,
            float extraHoldDuration = 0f)
        {
            if (targetPoints == null || targetPoints.Count == 0)
            {
                return;
            }

            EnsureParticleSystem();

            int shapeCount = Mathf.Min(FireworkPointCloudGenerator.MaxPointBudget, targetPoints.Count);
            int accentCount = ResolveAccentCount(shapeCount);
            int count = Mathf.Min(FireworkPointCloudGenerator.MaxPointBudget, shapeCount + accentCount);
            if (count > maxParticles)
            {
                maxParticles = count;
                ParticleSystem.MainModule main = particleSystemOutput.main;
                main.maxParticles = maxParticles;
            }

            EnsureBuffers(count);
            localTargets.Clear();

            Bounds localBounds = BuildBounds(targetPoints, shapeCount);
            for (int i = 0; i < shapeCount; i++)
            {
                localTargets.Add(targetPoints[i]);
                particleKinds[i] = ShapeParticle;
                delays[i] = Random.Range(0f, delayJitter);
                seeds[i] = Random.value * 100f;
                colors[i] = Color.Lerp(primaryColor, secondaryColor, Random.Range(0f, colorVariation));
                fadeOffsets[i] = Mathf.Lerp(MinFadeOffset, MaxFadeOffset, Mathf.Pow(Random.value, 0.72f));
                fadeDurations[i] = Random.Range(MinFadeDurationMultiplier, MaxFadeDurationMultiplier);
                fallMultipliers[i] = Random.Range(0.65f, 1.55f);
            }

            for (int i = shapeCount; i < count; i++)
            {
                int accentIndex = i - shapeCount;
                bool shockwave = accentIndex < Mathf.Min(shockwaveParticleCount, accentCount);
                particleKinds[i] = shockwave ? ShockwaveParticle : EmberParticle;
                localTargets.Add(shockwave
                    ? CreateShockwaveTarget(accentIndex, localBounds)
                    : CreateEmberTarget(accentIndex, localBounds));
                delays[i] = shockwave ? Random.Range(0f, delayJitter * 0.22f) : Random.Range(0f, delayJitter * 0.55f);
                seeds[i] = Random.value * 100f;
                colors[i] = shockwave
                    ? Color.Lerp(flashColor, primaryColor, Random.Range(0.15f, 0.45f))
                    : Color.Lerp(primaryColor, emberColor, Random.Range(0.35f, 0.75f));
                fadeOffsets[i] = shockwave ? Random.Range(-0.08f, 0.02f) : Mathf.Lerp(0.03f, MaxFadeOffset, Mathf.Pow(Random.value, 0.65f));
                fadeDurations[i] = shockwave ? Random.Range(0.45f, 0.82f) : Random.Range(MinFadeDurationMultiplier, MaxFadeDurationMultiplier);
                fallMultipliers[i] = shockwave ? Random.Range(0.35f, 0.75f) : Random.Range(0.9f, 1.85f);
            }

            launchOrigin = origin;
            bloomCenter = center;
            shapeRotation = ResolveReadableRotation(center);
            activeParticleSizeMultiplier = Mathf.Max(0.1f, sizeMultiplier);
            activeAutoRotate = autoRotate;
            activeRotationSpeedDegrees = rotationSpeedDegrees;
            activeRotationAxis = rotationAxis.sqrMagnitude > 0.0001f ? rotationAxis : Vector3.up;
            activeExtraHoldDuration = Mathf.Max(0f, extraHoldDuration);
            elapsed = 0f;
            isPlaying = true;

            particleSystemOutput.Clear(true);
            particleSystemOutput.Play(true);
            UpdateParticles();
        }

        public void StopAndClear()
        {
            isPlaying = false;
            if (particleSystemOutput != null)
            {
                particleSystemOutput.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private void Update()
        {
            if (!isPlaying)
            {
                return;
            }

            elapsed += Time.deltaTime;
            UpdateParticles();

            if (elapsed > PlaybackDuration)
            {
                StopAndClear();
            }
        }

        private void UpdateParticles()
        {
            int count = localTargets.Count;
            if (count == 0 || particleSystemOutput == null)
            {
                return;
            }

            for (int i = 0; i < count; i++)
            {
                float localTime = elapsed - delays[i];
                Vector3 position = ResolvePosition(i, localTime);
                Color color = ResolveColor(i, localTime);

                particles[i].position = position;
                particles[i].startSize = ResolveSize(i, localTime);
                particles[i].startColor = color;
                particles[i].startLifetime = PlaybackDuration;
                particles[i].remainingLifetime = PlaybackDuration;
                particles[i].velocity = Vector3.zero;
            }

            particleSystemOutput.SetParticles(particles, count);
        }

        private Vector3 ResolvePosition(int index, float localTime)
        {
            byte particleKind = particleKinds[index];
            Vector3 target = ResolveWorldTarget(localTargets[index]);

            if (localTime <= 0f)
            {
                return launchOrigin;
            }

            if (localTime < launchDuration)
            {
                if (!IsLaunchBeamParticle(index))
                {
                    return launchOrigin;
                }

                float rawT = Mathf.Clamp01(localTime / launchDuration);
                float t = EaseOutCubic(rawT);
                Vector3 mid = Vector3.Lerp(launchOrigin, bloomCenter, 0.55f) + Vector3.up * launchArcHeight;
                Vector3 path = QuadraticBezier(launchOrigin, mid, bloomCenter, t);
                return path + ResolveLaunchTrailOffset(index, rawT);
            }

            localTime -= launchDuration;
            if (particleKind == ShockwaveParticle)
            {
                float shockT = Mathf.Clamp01(localTime / Mathf.Max(0.001f, expandDuration * 0.58f));
                Vector3 wave = Vector3.LerpUnclamped(bloomCenter, target, Smoother01(shockT));
                Vector3 lift = Vector3.up * Mathf.Sin(shockT * Mathf.PI) * 0.16f;
                return wave + lift;
            }

            if (particleKind == EmberParticle)
            {
                float sparkT = Mathf.Clamp01(localTime / Mathf.Max(0.001f, expandDuration * 0.86f));
                Vector3 sparkTarget = Vector3.LerpUnclamped(bloomCenter, target, 1f + 0.08f * Mathf.Sin(seeds[index]));
                if (sparkT < 1f)
                {
                    return Vector3.LerpUnclamped(bloomCenter, sparkTarget, Smoother01(sparkT));
                }

                float age = localTime - expandDuration * 0.86f;
                Vector3 fall = Vector3.down * fallSpeed * fallMultipliers[index] * age * age * 0.16f;
                Vector3 accentWind = softWind * fallMultipliers[index] * age * 0.24f;
                return sparkTarget + ResolveDrift(index, age) + fall + accentWind;
            }

            if (localTime < expandDuration)
            {
                float t = Smooth01(localTime / expandDuration);
                Vector3 overshootTarget = Vector3.LerpUnclamped(bloomCenter, target, 1f + bloomOvershoot);
                Vector3 expanded = Vector3.LerpUnclamped(bloomCenter, overshootTarget, t);
                return Vector3.Lerp(expanded, target, Mathf.Clamp01((t - 0.72f) / 0.28f));
            }

            localTime -= expandDuration;
            if (localTime < ActiveHoldDuration)
            {
                float holdT = Mathf.Clamp01(localTime / Mathf.Max(0.001f, ActiveHoldDuration));
                Vector3 fromCenter = target - bloomCenter;
                Vector3 slowBloom = fromCenter.normalized * holdExpansion * Smooth01(holdT);
                return target + slowBloom + ResolveDrift(index, localTime);
            }

            localTime -= ActiveHoldDuration;
            float fadeDelay = Mathf.Max(0f, fadeOffsets[index]);
            float particleTail = ResolveParticleFadeTail(index);
            float particleFadeT = Mathf.Clamp01((localTime - fadeDelay) / Mathf.Max(0.001f, particleTail));
            float scatterT = Smoother01(particleFadeT);
            float globalFadeT = Mathf.Clamp01(localTime / EffectiveFadeDuration);
            float emberT = Mathf.Clamp01((localTime - fadeDelay - EffectiveFadeDuration * 0.32f) / Mathf.Max(0.001f, emberDuration));
            float fallMultiplier = fallMultipliers[index];
            Vector3 emberFall = Vector3.down * fallSpeed * fallMultiplier * (globalFadeT * globalFadeT * 0.42f + emberT * 1.9f);
            Vector3 wind = softWind * fallMultiplier * (globalFadeT * 0.35f + emberT * 0.9f);
            Vector3 scatter = ResolveFadeScatter(index) * scatterT * (0.18f + fallMultiplier * 0.16f);
            Vector3 sparkleDrift = ResolveDrift(index, ActiveHoldDuration + localTime) * (1f + scatterT * 2.2f);
            return target + sparkleDrift + scatter + emberFall + wind;
        }

        private Vector3 ResolveWorldTarget(Vector3 localTarget)
        {
            if (activeAutoRotate)
            {
                float rotateTime = Mathf.Max(0f, elapsed - launchDuration);
                float angle = rotateTime * activeRotationSpeedDegrees;
                Quaternion rotation = Quaternion.identity;
                if (Mathf.Abs(activeRotationAxis.y) > 0.001f)
                {
                    rotation = Quaternion.AngleAxis(angle * activeRotationAxis.y, Vector3.up) * rotation;
                }

                if (Mathf.Abs(activeRotationAxis.x) > 0.001f)
                {
                    rotation = Quaternion.AngleAxis(angle * activeRotationAxis.x, Vector3.right) * rotation;
                }

                if (Mathf.Abs(activeRotationAxis.z) > 0.001f)
                {
                    rotation = Quaternion.AngleAxis(angle * activeRotationAxis.z, Vector3.forward) * rotation;
                }

                localTarget = rotation * localTarget;
            }

            return bloomCenter + shapeRotation * localTarget;
        }

        private Vector3 ResolveLaunchTrailOffset(int index, float t)
        {
            float seed = seeds[index];
            float angle = seed * 6.283185f + t * Mathf.PI * 1.35f;
            float envelope = Mathf.Pow(Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI), 0.55f);
            float strand = 0.35f + Mathf.Abs(Mathf.Sin(seed * 1.73f)) * 0.65f;
            Vector3 right = shapeRotation * Vector3.right;
            Vector3 forward = shapeRotation * Vector3.forward;
            Vector3 beamEdge = right * Mathf.Cos(angle) * launchTrailWidth;
            beamEdge += forward * Mathf.Sin(angle * 0.83f) * launchTrailDepth;
            beamEdge += softWind * (t * t) * 0.045f;
            return beamEdge * envelope * strand;
        }

        private bool IsLaunchBeamParticle(int index)
        {
            if (particleKinds[index] != ShapeParticle)
            {
                return false;
            }

            return Mathf.Repeat(seeds[index] * 0.7548777f, 1f) < LaunchBeamParticleRatio;
        }

        private Vector3 ResolveFadeScatter(int index)
        {
            float seed = seeds[index];
            float azimuth = seed * 2.399963f;
            float y = Mathf.Sin(seed * 1.618f) * 0.55f;
            float radius = Mathf.Sqrt(Mathf.Max(0f, 1f - y * y));
            Vector3 local = new Vector3(Mathf.Cos(azimuth) * radius, y, Mathf.Sin(azimuth) * radius);
            return shapeRotation * local;
        }

        private Color ResolveColor(int index, float localTime)
        {
            Color color = colors[index];
            byte particleKind = particleKinds[index];

            if (localTime <= 0f)
            {
                color.a = 0f;
                return color;
            }

            if (localTime < launchDuration)
            {
                if (!IsLaunchBeamParticle(index))
                {
                    color.a = 0f;
                    return color;
                }

                float t = Mathf.Clamp01(localTime / launchDuration);
                float beamCore = Mathf.Pow(Mathf.Sin(t * Mathf.PI), 0.35f);
                Color beamColor = Color.Lerp(new Color(0.55f, 0.92f, 1.35f, 1f), flashColor, 0.28f);
                color = Color.Lerp(beamColor, color, Smooth01(t) * 0.18f);
                float flutter = 0.88f + Mathf.Sin(Time.time * 10f + seeds[index]) * 0.1f;
                color.r *= flutter;
                color.g *= flutter;
                color.b *= flutter;
                color.a = Mathf.Lerp(0.08f, 0.58f, beamCore) * (0.72f + Mathf.Abs(Mathf.Sin(seeds[index])) * 0.22f);
                return color;
            }

            float alpha = 1f;
            float fadeStart = particleKind == ShockwaveParticle
                ? launchDuration + expandDuration * 0.18f
                : launchDuration + expandDuration + ActiveHoldDuration + fadeOffsets[index];
            if (localTime > fadeStart)
            {
                float fadeT = Mathf.Clamp01((localTime - fadeStart) / ResolveParticleFadeTail(index));
                float sparkleGate = 0.82f + Mathf.Sin(Time.time * 18f + seeds[index] * 1.9f) * 0.18f;
                alpha = (1f - Smoother01(fadeT)) * sparkleGate;
            }

            float twinkle = particleKind == EmberParticle
                ? 0.72f + Mathf.Sin(Time.time * 12f + seeds[index]) * 0.5f
                : 0.84f + Mathf.Sin(Time.time * 9f + seeds[index]) * 0.34f;
            if (localTime < launchDuration + expandDuration * 0.22f)
            {
                twinkle += particleKind == ShapeParticle ? 0.45f : 0.82f;
            }

            if (useRainbowTwinkle && particleKind != ShockwaveParticle)
            {
                float bloomAge = Mathf.Max(0f, localTime - launchDuration);
                float hue = Mathf.Repeat(seeds[index] * 0.071f + bloomAge * rainbowCycleSpeed, 1f);
                float saturation = particleKind == EmberParticle ? 0.72f : 0.86f;
                float value = particleKind == EmberParticle ? 1.05f : 1.22f;
                Color rainbow = Color.HSVToRGB(hue, saturation, value);
                float bloomIn = Smooth01(Mathf.Clamp01(bloomAge / Mathf.Max(0.001f, expandDuration * 0.55f)));
                float mix = rainbowBlend * bloomIn * (particleKind == EmberParticle ? 0.7f : 1f);
                color = Color.Lerp(color, rainbow, mix);
            }

            color.r *= twinkle;
            color.g *= twinkle;
            color.b *= twinkle;
            color.a = alpha;
            return color;
        }

        private float ResolveSize(int index, float localTime)
        {
            if (localTime <= 0f)
            {
                return 0f;
            }

            byte particleKind = particleKinds[index];
            float pop = 1f;
            float burstTime = launchDuration;
            if (particleKind == ShockwaveParticle)
            {
                float shockAge = Mathf.Max(0f, localTime - launchDuration);
                float shockT = Mathf.Clamp01(shockAge / Mathf.Max(0.001f, expandDuration * 0.55f));
                pop = Mathf.Lerp(burstFlashSizeMultiplier * 1.25f, 0.15f, Smoother01(shockT));
            }
            else if (particleKind == EmberParticle)
            {
                float sparkAge = Mathf.Max(0f, localTime - launchDuration);
                float fadeT = Mathf.Clamp01((localTime - (launchDuration + expandDuration * 0.4f)) / ResolveParticleFadeTail(index));
                float pulse = 0.82f + Mathf.Sin(Time.time * 10f + seeds[index]) * 0.25f;
                pop = Mathf.Lerp(1.25f, emberSizeMultiplier * 0.72f, Smoother01(fadeT)) * pulse;
            }
            else if (localTime < burstTime)
            {
                if (!IsLaunchBeamParticle(index))
                {
                    return 0f;
                }

                float launchT = Mathf.Clamp01(localTime / burstTime);
                float launchPulse = 0.86f + Mathf.Sin(Time.time * 7f + seeds[index]) * 0.12f;
                pop = Mathf.Lerp(0.13f, 0.25f, Smooth01(launchT)) * launchPulse;
            }
            else if (localTime < burstTime + expandDuration * 0.32f)
            {
                float flashT = 1f - Mathf.Clamp01((localTime - burstTime) / (expandDuration * 0.32f));
                pop = Mathf.Lerp(1f, burstFlashSizeMultiplier, flashT);
            }
            else if (localTime > launchDuration + expandDuration + ActiveHoldDuration)
            {
                float globalFadeT = Mathf.Clamp01((localTime - launchDuration - expandDuration - ActiveHoldDuration) / EffectiveFadeDuration);
                float fadeStart = launchDuration + expandDuration + ActiveHoldDuration + fadeOffsets[index];
                float particleFadeT = Mathf.Clamp01((localTime - fadeStart) / ResolveParticleFadeTail(index));
                float emberSize = Mathf.Lerp(1f, emberSizeMultiplier, Smoother01(globalFadeT));
                pop = emberSize * (1f - Smoother01(particleFadeT));
            }

            return particleSize * activeParticleSizeMultiplier * pop;
        }

        private float ResolveParticleFadeTail(int index)
        {
            if (particleKinds[index] == ShockwaveParticle)
            {
                return Mathf.Max(0.18f, expandDuration * 0.65f);
            }

            if (particleKinds[index] == EmberParticle)
            {
                return EffectiveFadeDuration * 0.9f + emberDuration * fadeDurations[index];
            }

            return EffectiveFadeDuration * fadeDurations[index] + emberDuration;
        }

        private Vector3 ResolveDrift(int index, float time)
        {
            float seed = seeds[index];
            return new Vector3(
                Mathf.Sin(time * 1.7f + seed) * idleDrift,
                Mathf.Cos(time * 1.3f + seed * 0.7f) * idleDrift,
                Mathf.Sin(time * 1.1f + seed * 0.3f) * idleDrift * 0.5f);
        }

        private Quaternion ResolveReadableRotation(Vector3 center)
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                return transform.rotation;
            }

            Vector3 normal = center - camera.transform.position;
            if (normal.sqrMagnitude < 0.001f)
            {
                normal = transform.forward;
            }

            return Quaternion.LookRotation(normal.normalized, Vector3.up);
        }

        private void EnsureBuffers(int count)
        {
            if (particles.Length < count)
            {
                particles = new ParticleSystem.Particle[count];
                particleKinds = new byte[count];
                delays = new float[count];
                seeds = new float[count];
                colors = new Color[count];
                fadeOffsets = new float[count];
                fadeDurations = new float[count];
                fallMultipliers = new float[count];
            }
        }

        private int ResolveAccentCount(int shapeCount)
        {
            if (!useAccentParticles || shapeCount <= 0)
            {
                return 0;
            }

            int ratioCount = Mathf.RoundToInt(shapeCount * accentParticleRatio);
            int requested = Mathf.Max(shockwaveParticleCount, ratioCount);
            requested = Mathf.Min(requested, maxAccentParticles);
            return Mathf.Clamp(requested, 0, FireworkPointCloudGenerator.MaxPointBudget - shapeCount);
        }

        private static Bounds BuildBounds(IReadOnlyList<Vector3> points, int count)
        {
            Bounds bounds = new Bounds(Vector3.zero, Vector3.one);
            if (count <= 0)
            {
                return bounds;
            }

            bounds = new Bounds(points[0], Vector3.zero);
            for (int i = 1; i < count; i++)
            {
                bounds.Encapsulate(points[i]);
            }

            return bounds;
        }

        private Vector3 CreateShockwaveTarget(int accentIndex, Bounds localBounds)
        {
            float baseRadius = Mathf.Max(localBounds.extents.x, localBounds.extents.y, localBounds.extents.z) * shockwaveExpansion + 0.25f;
            int armCount = 7;
            int arm = accentIndex % armCount;
            float armPhase = arm * Mathf.PI * 2f / armCount;
            float goldenAngle = accentIndex * 2.399963f;
            float angle = armPhase + Mathf.Sin(goldenAngle * 1.37f) * 0.28f + Random.Range(-0.18f, 0.18f);
            float radiusBand = Mathf.Lerp(0.42f, 1.18f, Mathf.Pow(Random.value, 0.62f));
            float brokenEdge = (accentIndex % 5 == 0) ? Random.Range(1.12f, 1.34f) : Random.Range(0.76f, 1.08f);
            float radius = baseRadius * radiusBand * brokenEdge;
            float verticalScatter = Random.Range(-0.22f, 0.28f) * baseRadius;
            float depthScatter = Random.Range(-0.34f, 0.34f) * baseRadius;

            return new Vector3(
                Mathf.Cos(angle) * radius + Random.Range(-0.12f, 0.12f) * baseRadius,
                Mathf.Sin(angle) * radius * 0.72f + verticalScatter,
                depthScatter);
        }

        private Vector3 CreateEmberTarget(int accentIndex, Bounds localBounds)
        {
            float radius = Mathf.Max(localBounds.extents.x, localBounds.extents.y, localBounds.extents.z) + accentScatter;
            float angle = (accentIndex * 2.399963f + Random.value * 0.22f) % (Mathf.PI * 2f);
            float vertical = Random.Range(-0.55f, 0.75f);
            float radial = radius * Random.Range(0.65f, 1.16f);
            return new Vector3(
                Mathf.Cos(angle) * radial,
                vertical * radius,
                Mathf.Sin(angle) * radial * 0.36f);
        }

        private void EnsureParticleSystem()
        {
            if (particleSystemOutput == null)
            {
                Transform existing = transform.Find("_PointCloudFireworkParticles");
                if (existing != null)
                {
                    particleSystemOutput = existing.GetComponent<ParticleSystem>();
                }
            }

            if (particleSystemOutput == null)
            {
                GameObject child = new GameObject("_PointCloudFireworkParticles");
                child.transform.SetParent(transform, false);
                particleSystemOutput = child.AddComponent<ParticleSystem>();
            }

            ParticleSystem.MainModule main = particleSystemOutput.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = maxParticles;
            main.startLifetime = PlaybackDuration;
            main.startSize = particleSize;
            main.startSpeed = 0f;

            ParticleSystem.EmissionModule emission = particleSystemOutput.emission;
            emission.enabled = false;

            ParticleSystem.ShapeModule shape = particleSystemOutput.shape;
            shape.enabled = false;

            ParticleSystemRenderer renderer = particleSystemOutput.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.sortMode = sortParticlesByDistance ? ParticleSystemSortMode.Distance : ParticleSystemSortMode.None;
            renderer.sharedMaterial = ResolveParticleMaterial();
        }

        private Material ResolveParticleMaterial()
        {
            if (defaultParticleMaterial != null)
            {
                return defaultParticleMaterial;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            if (shader == null)
            {
                shader = Shader.Find("Particles/Standard Unlit");
            }

            if (shader == null && particleMaterial != null && particleMaterial.shader != null && particleMaterial.shader.name != "Hidden/InternalErrorShader")
            {
                return particleMaterial;
            }

            if (shader == null)
            {
                Debug.LogWarning("[Fireworks] Could not find a particle shader. Assign a particle material on PointCloudFireworkRenderer if particles are invisible.");
                return null;
            }

            defaultParticleMaterial = new Material(shader);
            defaultParticleMaterial.name = "FireworkVertexColorParticle";
            return defaultParticleMaterial;
        }

        private float EffectiveFadeDuration => fadeDuration + extraFadeDuration;

        private float ActiveHoldDuration => holdDuration + activeExtraHoldDuration;

        private float PlaybackDuration => launchDuration
            + expandDuration
            + ActiveHoldDuration
            + MaxFadeOffset
            + EffectiveFadeDuration * MaxFadeDurationMultiplier
            + emberDuration
            + delayJitter
            + 0.12f;

        private static float Smooth01(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        private static float Smoother01(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * t * (t * (t * 6f - 15f) + 10f);
        }

        private static float EaseOutCubic(float t)
        {
            t = 1f - Mathf.Clamp01(t);
            return 1f - t * t * t;
        }

        private static Vector3 QuadraticBezier(Vector3 a, Vector3 b, Vector3 c, float t)
        {
            Vector3 ab = Vector3.Lerp(a, b, t);
            Vector3 bc = Vector3.Lerp(b, c, t);
            return Vector3.Lerp(ab, bc, t);
        }
    }
}
