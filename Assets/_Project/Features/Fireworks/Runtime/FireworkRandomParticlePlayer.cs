using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WonderfulWorld.Features.Fireworks
{
    [DisallowMultipleComponent]
    public sealed class FireworkRandomParticlePlayer : MonoBehaviour
    {
        [Header("Effects")]
        [SerializeField] private List<GameObject> fireworkLoopGroups = new();
        [SerializeField] private bool autoCacheChildren = true;
        [SerializeField] private bool resetEffectsOnAwake = true;
        [SerializeField] private bool activateChildObjectsOnPlay = true;
        [SerializeField] private float effectLocalHeightOffset = 8f;

        [Header("Presentation Scale")]
        [SerializeField, Min(0.1f)] private float effectVisualScale = 1.35f;
        [SerializeField, Min(0.1f)] private float effectRangeScale = 1.35f;

        [Header("Playback")]
        [SerializeField] private bool autoPlayOnStart;
        [SerializeField, Min(0.1f)] private float totalPlayDuration = 28f;
        [SerializeField, Min(0.05f)] private float minDelayBetween = 1.2f;
        [SerializeField, Min(0.05f)] private float maxDelayBetween = 2.6f;
        [SerializeField] private bool autoStopLoopingEffects = true;
        [SerializeField, Min(0.1f)] private float effectPlayDuration = 5.5f;
        [SerializeField] private bool deactivateGroupAfterStop = true;

        [Header("Production")]
        [SerializeField] private bool stopExistingSequenceOnTrigger = true;
        [SerializeField] private bool logDebug;

        private readonly Dictionary<GameObject, float> cooldownTimers = new();
        private readonly Dictionary<GameObject, Vector3> originalLocalPositions = new();
        private readonly Dictionary<ParticleSystem, ParticleSystemProfile> originalParticleProfiles = new();
        private readonly List<Coroutine> activeGroupRoutines = new();
        private Coroutine continuousRoutine;
        private int activeGroupCount;

        public bool IsPlaying => continuousRoutine != null || activeGroupCount > 0;

        private void Awake()
        {
            EnsureGroups();

            if (resetEffectsOnAwake)
            {
                StopAllEffects(clearParticles: true, deactivateGroups: true);
            }
        }

        private void Start()
        {
            if (autoPlayOnStart)
            {
                PlayContinuousSequence();
            }
        }

        private void OnDisable()
        {
            StopSequence(clearParticles: true);
        }

        [ContextMenu("Fireworks/Cache Child Effects")]
        public void CacheAndResetAllEffects()
        {
            fireworkLoopGroups.Clear();
            originalLocalPositions.Clear();
            foreach (Transform child in transform)
            {
                fireworkLoopGroups.Add(child.gameObject);
                originalLocalPositions[child.gameObject] = child.localPosition;
                child.gameObject.SetActive(false);
            }
        }

        [ContextMenu("Fireworks/Play Particle Sequence")]
        public void PlayContinuousSequence()
        {
            EnsureGroups();
            if (fireworkLoopGroups.Count == 0)
            {
                if (logDebug)
                {
                    Debug.LogWarning($"{nameof(FireworkRandomParticlePlayer)} on {name} has no particle groups.", this);
                }

                return;
            }

            if (stopExistingSequenceOnTrigger)
            {
                StopSequence(clearParticles: true);
            }
            else if (continuousRoutine != null)
            {
                return;
            }

            continuousRoutine = StartCoroutine(ContinuousLaunchRoutine());
        }

        [ContextMenu("Fireworks/Stop Particle Sequence")]
        public void StopSequence()
        {
            StopSequence(clearParticles: true);
        }

        public void StopSequence(bool clearParticles)
        {
            if (continuousRoutine != null)
            {
                StopCoroutine(continuousRoutine);
                continuousRoutine = null;
            }

            for (int i = activeGroupRoutines.Count - 1; i >= 0; i--)
            {
                if (activeGroupRoutines[i] != null)
                {
                    StopCoroutine(activeGroupRoutines[i]);
                }
            }

            activeGroupRoutines.Clear();
            activeGroupCount = 0;
            StopAllEffects(clearParticles, deactivateGroups: false);
        }

        private IEnumerator ContinuousLaunchRoutine()
        {
            float elapsedTime = 0f;
            cooldownTimers.Clear();

            while (elapsedTime < totalPlayDuration)
            {
                GameObject selectedGroup = SelectAvailableGroup();
                if (selectedGroup != null)
                {
                    cooldownTimers[selectedGroup] = Time.time + Mathf.Max(effectPlayDuration * 0.55f, minDelayBetween);
                    Coroutine groupRoutine = StartCoroutine(PlaySingleFireworkGroup(selectedGroup));
                    activeGroupRoutines.Add(groupRoutine);
                }

                float delay = Random.Range(Mathf.Min(minDelayBetween, maxDelayBetween), Mathf.Max(minDelayBetween, maxDelayBetween));
                yield return new WaitForSeconds(delay);
                elapsedTime += delay;
            }

            continuousRoutine = null;
        }

        private GameObject SelectAvailableGroup()
        {
            List<GameObject> availableGroups = new();
            for (int i = 0; i < fireworkLoopGroups.Count; i++)
            {
                GameObject group = fireworkLoopGroups[i];
                if (group != null && (!cooldownTimers.TryGetValue(group, out float readyTime) || Time.time >= readyTime))
                {
                    availableGroups.Add(group);
                }
            }

            if (availableGroups.Count == 0)
            {
                return null;
            }

            return availableGroups[Random.Range(0, availableGroups.Count)];
        }

        private IEnumerator PlaySingleFireworkGroup(GameObject groupObject)
        {
            if (groupObject == null)
            {
                yield break;
            }

            activeGroupCount++;
            CacheOriginalLocalPosition(groupObject);
            groupObject.transform.localPosition = originalLocalPositions[groupObject] + Vector3.up * effectLocalHeightOffset;
            groupObject.SetActive(true);
            if (activateChildObjectsOnPlay)
            {
                SetChildObjectsActive(groupObject.transform, true);
            }

            ParticleSystemRenderer[] renderers = groupObject.GetComponentsInChildren<ParticleSystemRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].enabled = true;
            }

            ParticleSystem[] particles = groupObject.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < particles.Length; i++)
            {
                particles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ApplyParticlePresentationScale(particles[i]);
            }

            for (int i = 0; i < particles.Length; i++)
            {
                particles[i].Play(true);
            }

            AudioSource[] audioSources = groupObject.GetComponentsInChildren<AudioSource>(true);
            for (int i = 0; i < audioSources.Length; i++)
            {
                audioSources[i].Stop();
                audioSources[i].Play();
            }

            if (autoStopLoopingEffects)
            {
                yield return new WaitForSeconds(effectPlayDuration);
                StopGroupParticles(particles, clearParticles: false);
                if (deactivateGroupAfterStop && groupObject != null)
                {
                    groupObject.SetActive(false);
                }
            }

            activeGroupCount = Mathf.Max(0, activeGroupCount - 1);
        }

        private void EnsureGroups()
        {
            if (!autoCacheChildren || fireworkLoopGroups.Count > 0)
            {
                fireworkLoopGroups.RemoveAll(group => group == null);
                for (int i = 0; i < fireworkLoopGroups.Count; i++)
                {
                    CacheOriginalLocalPosition(fireworkLoopGroups[i]);
                }

                return;
            }

            foreach (Transform child in transform)
            {
                fireworkLoopGroups.Add(child.gameObject);
                originalLocalPositions[child.gameObject] = child.localPosition;
            }
        }

        private void CacheOriginalLocalPosition(GameObject group)
        {
            if (group != null && !originalLocalPositions.ContainsKey(group))
            {
                originalLocalPositions[group] = group.transform.localPosition;
            }
        }

        private void StopAllEffects(bool clearParticles, bool deactivateGroups)
        {
            for (int i = 0; i < fireworkLoopGroups.Count; i++)
            {
                GameObject group = fireworkLoopGroups[i];
                if (group == null)
                {
                    continue;
                }

                StopGroupParticles(group.GetComponentsInChildren<ParticleSystem>(true), clearParticles);
                AudioSource[] audioSources = group.GetComponentsInChildren<AudioSource>(true);
                for (int audioIndex = 0; audioIndex < audioSources.Length; audioIndex++)
                {
                    audioSources[audioIndex].Stop();
                }

                if (deactivateGroups)
                {
                    group.SetActive(false);
                }
            }
        }

        private void ApplyParticlePresentationScale(ParticleSystem particle)
        {
            if (particle == null)
            {
                return;
            }

            if (!originalParticleProfiles.TryGetValue(particle, out ParticleSystemProfile profile))
            {
                ParticleSystem.MainModule originalMain = particle.main;
                profile = new ParticleSystemProfile
                {
                    startSize = originalMain.startSize,
                    startSpeed = originalMain.startSpeed
                };
                originalParticleProfiles.Add(particle, profile);
            }

            ParticleSystem.MainModule main = particle.main;
            main.startSize = ScaleMinMaxCurve(profile.startSize, effectVisualScale);
            main.startSpeed = ScaleMinMaxCurve(profile.startSpeed, effectRangeScale);
        }

        private static ParticleSystem.MinMaxCurve ScaleMinMaxCurve(ParticleSystem.MinMaxCurve curve, float multiplier)
        {
            switch (curve.mode)
            {
                case ParticleSystemCurveMode.Constant:
                    return new ParticleSystem.MinMaxCurve(curve.constant * multiplier);

                case ParticleSystemCurveMode.TwoConstants:
                    return new ParticleSystem.MinMaxCurve(curve.constantMin * multiplier, curve.constantMax * multiplier);

                case ParticleSystemCurveMode.Curve:
                    return new ParticleSystem.MinMaxCurve(curve.curveMultiplier * multiplier, curve.curve);

                case ParticleSystemCurveMode.TwoCurves:
                    return new ParticleSystem.MinMaxCurve(curve.curveMultiplier * multiplier, curve.curveMin, curve.curveMax);

                default:
                    return curve;
            }
        }

        private struct ParticleSystemProfile
        {
            public ParticleSystem.MinMaxCurve startSize;
            public ParticleSystem.MinMaxCurve startSpeed;
        }

        private static void StopGroupParticles(ParticleSystem[] particles, bool clearParticles)
        {
            ParticleSystemStopBehavior stopBehavior = clearParticles
                ? ParticleSystemStopBehavior.StopEmittingAndClear
                : ParticleSystemStopBehavior.StopEmitting;
            for (int i = 0; i < particles.Length; i++)
            {
                if (particles[i] != null)
                {
                    particles[i].Stop(true, stopBehavior);
                }
            }
        }

        private static void SetChildObjectsActive(Transform root, bool active)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                child.gameObject.SetActive(active);
                SetChildObjectsActive(child, active);
            }
        }
    }
}
