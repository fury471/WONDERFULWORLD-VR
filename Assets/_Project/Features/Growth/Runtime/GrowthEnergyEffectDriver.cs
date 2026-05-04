using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns and drives a "growth energy" particle effect during a GrowthPlant grow transition:
/// - while growing (toward 1.0), intensity ramps from weak -> strong as growth progresses
/// - once fully grown, the effect fades out smoothly
/// 
/// Assign your `growth_energy` prefab in the inspector.
/// </summary>
public class GrowthEnergyEffectDriver : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GrowthPlant growthPlant;
    [SerializeField] private GameObject growthEnergyPrefab;
    [SerializeField] private Transform attachTo;
    [SerializeField] private Vector3 localPositionOffset = Vector3.zero;
    [SerializeField] private Vector3 localRotationOffsetEuler = Vector3.zero;

    [Header("When To Play")]
    [Tooltip("If false, the effect only plays when the plant is transitioning toward growth (target > current).")]
    [SerializeField] private bool playOnAnyTransitionDirection = false;
    [SerializeField] private float fullyGrownThreshold = 0.995f;

    [Header("Intensity Mapping (0..1 Growth Time -> 0..1 Strength)")]
    [SerializeField] private AnimationCurve strengthOverGrowth = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Particle Controls")]
    [SerializeField] private float minRateOverTime = 5f;
    [SerializeField] private float maxRateOverTime = 60f;
    [SerializeField] private float minStartSizeMultiplier = 0.6f;
    [SerializeField] private float maxStartSizeMultiplier = 1.4f;
    [SerializeField] private float minStartAlpha = 0.15f;
    [SerializeField] private float maxStartAlpha = 1.0f;

    [Header("Fade Out")]
    [SerializeField] private float fadeOutSeconds = 1.25f;
    [SerializeField] private bool deactivateAfterFadeOut = true;

    private GameObject instance;
    private readonly List<ParticleSystem> particleSystems = new();
    private readonly List<float> baseStartSizes = new();
    private readonly List<float> baseRates = new();

    private bool fadingOut;
    private float fadeT;
    private bool wasFullyGrown;
    private bool wasTransitioning;
    private bool initializedPlayState;

    private void Awake()
    {
        if (growthPlant == null)
        {
            growthPlant = GetComponent<GrowthPlant>();
            if (growthPlant == null)
            {
                growthPlant = GetComponentInParent<GrowthPlant>();
            }
        }

        if (attachTo == null)
        {
            attachTo = transform;
        }
    }

    private void Start()
    {
        EnsureInstance();
        ApplyTransformOffsets();
        SetEffectActive(false);
        ApplyStrength(0f);
        EnsureParticlesPlaying();
    }

    private void Update()
    {
        if (growthPlant == null || growthEnergyPrefab == null)
        {
            return;
        }

        EnsureInstance();
        ApplyTransformOffsets();

        float growth = Mathf.Clamp01(growthPlant.CurrentGrowthTime);
        bool isTransitioning = growthPlant.IsTransitioning();
        bool isGrowingDirection = growthPlant.TargetGrowthTime > growth + 0.0005f;
        bool shouldPlay = isTransitioning && (playOnAnyTransitionDirection || isGrowingDirection);

        bool fullyGrownNow = !isTransitioning && growth >= fullyGrownThreshold;
        bool transitionEndedThisFrame = wasTransitioning && !isTransitioning;

        if (shouldPlay)
        {
            wasFullyGrown = false;
            fadingOut = false;
            fadeT = 0f;

            float strength = Mathf.Clamp01(strengthOverGrowth.Evaluate(growth));
            SetEffectActive(true);
            ApplyStrength(strength);
            EnsureParticlesPlaying();
            wasTransitioning = isTransitioning;
            return;
        }

        // Not actively growing; if we just ended a grow transition into fully-grown, start fade-out.
        if (fullyGrownNow && transitionEndedThisFrame)
        {
            if (!wasFullyGrown)
            {
                wasFullyGrown = true;
                fadingOut = true;
                fadeT = 0f;
                SetEffectActive(true);
                EnsureParticlesPlaying();
            }
        }
        else
        {
            wasFullyGrown = false;
        }

        if (fadingOut)
        {
            float duration = Mathf.Max(0.01f, fadeOutSeconds);
            fadeT = Mathf.Clamp01(fadeT + Time.deltaTime / duration);
            float strength = 1f - fadeT;
            ApplyStrength(strength);

            if (fadeT >= 0.999f)
            {
                fadingOut = false;
                ApplyStrength(0f);
                StopAllParticles(clear: true);

                if (deactivateAfterFadeOut && instance != null)
                {
                    instance.SetActive(false);
                }
            }

            return;
        }

        // Otherwise keep effect off (do not stop/play toggle; just drive emission to zero).
        SetEffectActive(false);
        ApplyStrength(0f);

        wasTransitioning = isTransitioning;
    }

    private void EnsureInstance()
    {
        if (instance != null)
        {
            return;
        }

        instance = Instantiate(growthEnergyPrefab, attachTo);
        instance.name = $"{growthEnergyPrefab.name}_Instance";
        instance.transform.localPosition = localPositionOffset;
        instance.transform.localRotation = Quaternion.Euler(localRotationOffsetEuler);

        particleSystems.Clear();
        baseStartSizes.Clear();
        baseRates.Clear();
        ParticleSystem[] found = instance.GetComponentsInChildren<ParticleSystem>(true);
        if (found != null && found.Length > 0)
        {
            particleSystems.AddRange(found);
        }

        for (int i = 0; i < particleSystems.Count; i++)
        {
            ParticleSystem ps = particleSystems[i];
            var main = ps.main;
            baseStartSizes.Add(main.startSizeMultiplier);
            var emission = ps.emission;
            baseRates.Add(Mathf.Max(0.0001f, emission.rateOverTimeMultiplier));
        }
    }

    private void ApplyTransformOffsets()
    {
        if (instance == null)
        {
            return;
        }

        if (instance.transform.parent != attachTo)
        {
            instance.transform.SetParent(attachTo, false);
        }

        instance.transform.localPosition = localPositionOffset;
        instance.transform.localRotation = Quaternion.Euler(localRotationOffsetEuler);
    }

    private void ApplyStrength(float strength01)
    {
        if (instance == null)
        {
            return;
        }

        float rate = Mathf.Lerp(minRateOverTime, maxRateOverTime, strength01);
        float sizeMultiplier = Mathf.Lerp(minStartSizeMultiplier, maxStartSizeMultiplier, strength01);
        float alpha = Mathf.Lerp(minStartAlpha, maxStartAlpha, strength01);

        for (int i = 0; i < particleSystems.Count; i++)
        {
            ParticleSystem ps = particleSystems[i];
            if (ps == null)
            {
                continue;
            }

            var emission = ps.emission;
            // Keep particle systems playing continuously; ramp emission to avoid "restart" pops.
            emission.rateOverTimeMultiplier = rate * baseRates[i];

            var main = ps.main;
            main.startSizeMultiplier = sizeMultiplier * Mathf.Max(0.0001f, baseStartSizes[i]);

            ParticleSystem.MinMaxGradient gradient = main.startColor;
            switch (gradient.mode)
            {
                case ParticleSystemGradientMode.Color:
                {
                    Color c = gradient.color;
                    c.a = alpha;
                    main.startColor = c;
                    break;
                }
                case ParticleSystemGradientMode.TwoColors:
                {
                    Color min = gradient.colorMin;
                    Color max = gradient.colorMax;
                    min.a = alpha;
                    max.a = alpha;
                    main.startColor = new ParticleSystem.MinMaxGradient(min, max);
                    break;
                }
                default:
                    // Keep gradients as-authored; alpha control is ambiguous for gradient modes.
                    break;
            }
        }
    }

    private void EnsureParticlesPlaying()
    {
        if (initializedPlayState)
        {
            return;
        }

        for (int i = 0; i < particleSystems.Count; i++)
        {
            ParticleSystem ps = particleSystems[i];
            if (ps == null)
            {
                continue;
            }

            ps.Play(true);
        }

        initializedPlayState = true;
    }

    private void StopAllParticles(bool clear)
    {
        for (int i = 0; i < particleSystems.Count; i++)
        {
            ParticleSystem ps = particleSystems[i];
            if (ps == null)
            {
                continue;
            }

            ps.Stop(true, clear ? ParticleSystemStopBehavior.StopEmittingAndClear : ParticleSystemStopBehavior.StopEmitting);
        }
    }

    private void SetEffectActive(bool active)
    {
        if (instance == null)
        {
            return;
        }

        // If the instance was deactivated after fade-out, reactivate only when explicitly needed.
        if (active)
        {
            if (!instance.activeSelf)
            {
                instance.SetActive(true);
                initializedPlayState = false;
            }
        }
    }
}
