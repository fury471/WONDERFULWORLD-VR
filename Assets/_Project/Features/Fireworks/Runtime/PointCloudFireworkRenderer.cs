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
        [SerializeField] private float launchDuration = 1.05f;
        [SerializeField] private float expandDuration = 1.25f;
        [SerializeField] private float holdDuration = 2.9f;
        [SerializeField] private float fadeDuration = 2.15f;
        [SerializeField] private float extraFadeDuration = 0.5f;
        [SerializeField] private float emberDuration = 1.15f;
        [SerializeField] private float delayJitter = 0.34f;

        [Header("Motion")]
        [SerializeField] private float launchArcHeight = 4.2f;
        [SerializeField] private float bloomOvershoot = 0.44f;
        [SerializeField] private float holdExpansion = 0.48f;
        [SerializeField] private float idleDrift = 0.045f;
        [SerializeField] private float fallSpeed = 0.72f;
        [SerializeField] private Vector3 softWind = new Vector3(0.28f, 0f, 0.08f);

        [Header("Look")]
        [SerializeField] private Color secondaryColor = new Color(0.55f, 0.95f, 1f, 1f);
        [SerializeField] private float colorVariation = 0.22f;
        [SerializeField] private float burstFlashSizeMultiplier = 2.4f;
        [SerializeField] private float emberSizeMultiplier = 0.55f;

        private const float MinFadeOffset = -0.35f;
        private const float MaxFadeOffset = 0.65f;
        private const float MinFadeDurationMultiplier = 0.9f;
        private const float MaxFadeDurationMultiplier = 1.25f;
        private static Material defaultParticleMaterial;

        private readonly List<Vector3> localTargets = new List<Vector3>();
        private ParticleSystem.Particle[] particles = new ParticleSystem.Particle[0];
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
            burstFlashSizeMultiplier = Mathf.Max(1f, burstFlashSizeMultiplier);
            emberSizeMultiplier = Mathf.Clamp(emberSizeMultiplier, 0.05f, 1f);

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
            float rotationSpeedDegrees = 0f)
        {
            if (targetPoints == null || targetPoints.Count == 0)
            {
                return;
            }

            EnsureParticleSystem();

            int count = Mathf.Min(FireworkPointCloudGenerator.MaxPointBudget, targetPoints.Count);
            if (count > maxParticles)
            {
                maxParticles = count;
                ParticleSystem.MainModule main = particleSystemOutput.main;
                main.maxParticles = maxParticles;
            }

            EnsureBuffers(count);
            localTargets.Clear();

            for (int i = 0; i < count; i++)
            {
                localTargets.Add(targetPoints[i]);
                delays[i] = Random.Range(0f, delayJitter);
                seeds[i] = Random.value * 100f;
                colors[i] = Color.Lerp(primaryColor, secondaryColor, Random.Range(0f, colorVariation));
                fadeOffsets[i] = Random.Range(MinFadeOffset, MaxFadeOffset);
                fadeDurations[i] = Random.Range(MinFadeDurationMultiplier, MaxFadeDurationMultiplier);
                fallMultipliers[i] = Random.Range(0.65f, 1.55f);
            }

            launchOrigin = origin;
            bloomCenter = center;
            shapeRotation = ResolveReadableRotation(center);
            activeParticleSizeMultiplier = Mathf.Max(0.1f, sizeMultiplier);
            activeAutoRotate = autoRotate;
            activeRotationSpeedDegrees = rotationSpeedDegrees;
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
            Vector3 target = ResolveWorldTarget(localTargets[index]);

            if (localTime <= 0f)
            {
                return launchOrigin;
            }

            if (localTime < launchDuration)
            {
                float t = Smooth01(localTime / launchDuration);
                Vector3 mid = Vector3.Lerp(launchOrigin, bloomCenter, 0.55f) + Vector3.up * launchArcHeight;
                return QuadraticBezier(launchOrigin, mid, bloomCenter, t);
            }

            localTime -= launchDuration;
            if (localTime < expandDuration)
            {
                float t = Smooth01(localTime / expandDuration);
                Vector3 overshootTarget = Vector3.LerpUnclamped(bloomCenter, target, 1f + bloomOvershoot);
                Vector3 expanded = Vector3.LerpUnclamped(bloomCenter, overshootTarget, t);
                return Vector3.Lerp(expanded, target, Mathf.Clamp01((t - 0.72f) / 0.28f));
            }

            localTime -= expandDuration;
            if (localTime < holdDuration)
            {
                float holdT = Mathf.Clamp01(localTime / Mathf.Max(0.001f, holdDuration));
                Vector3 fromCenter = target - bloomCenter;
                Vector3 slowBloom = fromCenter.normalized * holdExpansion * Smooth01(holdT);
                return target + slowBloom + ResolveDrift(index, localTime);
            }

            localTime -= holdDuration;
            float fadeT = Mathf.Clamp01(localTime / EffectiveFadeDuration);
            float emberT = Mathf.Clamp01((localTime - EffectiveFadeDuration * 0.45f) / Mathf.Max(0.001f, emberDuration));
            float fallMultiplier = fallMultipliers[index];
            Vector3 emberFall = Vector3.down * fallSpeed * fallMultiplier * (fadeT * fadeT + emberT * 1.7f);
            Vector3 wind = softWind * fallMultiplier * (fadeT + emberT * 0.8f);
            return target + ResolveDrift(index, holdDuration + localTime) + emberFall + wind;
        }

        private Vector3 ResolveWorldTarget(Vector3 localTarget)
        {
            if (activeAutoRotate)
            {
                float rotateTime = Mathf.Max(0f, elapsed - launchDuration);
                Quaternion yaw = Quaternion.AngleAxis(rotateTime * activeRotationSpeedDegrees, Vector3.up);
                Quaternion pitch = Quaternion.AngleAxis(rotateTime * activeRotationSpeedDegrees * 0.62f, Vector3.right);
                localTarget = yaw * pitch * localTarget;
            }

            return bloomCenter + shapeRotation * localTarget;
        }

        private Color ResolveColor(int index, float localTime)
        {
            Color color = colors[index];

            if (localTime <= 0f)
            {
                color.a = 0f;
                return color;
            }

            float alpha = 1f;
            float fadeStart = launchDuration + expandDuration + holdDuration + fadeOffsets[index];
            if (localTime > fadeStart)
            {
                float fadeT = Mathf.Clamp01((localTime - fadeStart) / ResolveParticleFadeTail(index));
                alpha = 1f - Smoother01(fadeT);
            }

            float twinkle = 0.84f + Mathf.Sin(Time.time * 9f + seeds[index]) * 0.34f;
            if (localTime < launchDuration + expandDuration * 0.22f)
            {
                twinkle += 0.45f;
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

            float pop = 1f;
            float burstTime = launchDuration;
            if (localTime < burstTime)
            {
                pop = Mathf.Lerp(0.58f, 1.05f, Smooth01(localTime / burstTime));
            }
            else if (localTime < burstTime + expandDuration * 0.32f)
            {
                float flashT = 1f - Mathf.Clamp01((localTime - burstTime) / (expandDuration * 0.32f));
                pop = Mathf.Lerp(1f, burstFlashSizeMultiplier, flashT);
            }
            else if (localTime > launchDuration + expandDuration + holdDuration)
            {
                float globalFadeT = Mathf.Clamp01((localTime - launchDuration - expandDuration - holdDuration) / EffectiveFadeDuration);
                float fadeStart = launchDuration + expandDuration + holdDuration + fadeOffsets[index];
                float particleFadeT = Mathf.Clamp01((localTime - fadeStart) / ResolveParticleFadeTail(index));
                float emberSize = Mathf.Lerp(1f, emberSizeMultiplier, Smoother01(globalFadeT));
                pop = emberSize * (1f - Smoother01(particleFadeT));
            }

            return particleSize * activeParticleSizeMultiplier * pop;
        }

        private float ResolveParticleFadeTail(int index)
        {
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
                delays = new float[count];
                seeds = new float[count];
                colors = new Color[count];
                fadeOffsets = new float[count];
                fadeDurations = new float[count];
                fallMultipliers = new float[count];
            }
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

        private float PlaybackDuration => launchDuration
            + expandDuration
            + holdDuration
            + MaxFadeOffset
            + EffectiveFadeDuration * MaxFadeDurationMultiplier
            + emberDuration
            + delayJitter
            + 0.25f;

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

        private static Vector3 QuadraticBezier(Vector3 a, Vector3 b, Vector3 c, float t)
        {
            Vector3 ab = Vector3.Lerp(a, b, t);
            Vector3 bc = Vector3.Lerp(b, c, t);
            return Vector3.Lerp(ab, bc, t);
        }
    }
}
