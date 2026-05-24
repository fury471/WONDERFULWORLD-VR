using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WonderfulWorld.Audio
{
    [DisallowMultipleComponent]
    public sealed class WonderlandProximityLoop : MonoBehaviour
    {
        [SerializeField] private WonderlandAudioCue cue;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private bool requireCharacterController = true;
        [SerializeField] private bool stopOnExit = true;
        [SerializeField, Range(0f, 1f)] private float volumeScale = 1f;
        [SerializeField, Min(0f)] private float fadeInSeconds = 0.12f;
        [SerializeField, Min(0f)] private float fadeOutSeconds = 0.2f;
        [SerializeField] private bool logDebug;

        private readonly HashSet<Component> occupants = new HashSet<Component>();
        private Coroutine fadeRoutine;

        private void Reset()
        {
            audioSource = GetComponent<AudioSource>();
        }

        private void Awake()
        {
            ConfigureSource();
        }

        private void OnDisable()
        {
            occupants.Clear();
            StopPlayback(immediate: true);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!TryResolveTriggerActor(other, out Component actor))
            {
                return;
            }

            bool wasEmpty = occupants.Count == 0;
            occupants.Add(actor);
            if (wasEmpty)
            {
                Play();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!TryResolveTriggerActor(other, out Component actor))
            {
                return;
            }

            occupants.Remove(actor);
            if (stopOnExit && occupants.Count == 0)
            {
                StopPlayback(immediate: false);
            }
        }

        public void Play()
        {
            ConfigureSource();
            if (audioSource == null || audioSource.clip == null)
            {
                return;
            }

            if (logDebug)
            {
                Debug.Log($"[WonderlandProximityLoop] Playing {audioSource.clip.name}.", this);
            }

            StartFade(ResolveTargetVolume(), fadeInSeconds, playBeforeFade: true, stopAfterFade: false);
        }

        public void Configure(
            WonderlandAudioCue cue,
            AudioSource audioSource,
            bool requireCharacterController,
            bool stopOnExit,
            float volumeScale,
            float fadeInSeconds,
            float fadeOutSeconds)
        {
            this.cue = cue;
            this.audioSource = audioSource;
            this.requireCharacterController = requireCharacterController;
            this.stopOnExit = stopOnExit;
            this.volumeScale = Mathf.Clamp01(volumeScale);
            this.fadeInSeconds = Mathf.Max(0f, fadeInSeconds);
            this.fadeOutSeconds = Mathf.Max(0f, fadeOutSeconds);
            ConfigureSource();
        }

        public void StopPlayback(bool immediate)
        {
            if (audioSource == null)
            {
                return;
            }

            if (immediate || fadeOutSeconds <= 0f || !Application.isPlaying)
            {
                if (fadeRoutine != null)
                {
                    StopCoroutine(fadeRoutine);
                    fadeRoutine = null;
                }

                audioSource.Stop();
                audioSource.volume = 0f;
                return;
            }

            StartFade(0f, fadeOutSeconds, playBeforeFade: false, stopAfterFade: true);
        }

        private bool TryResolveTriggerActor(Collider other, out Component actor)
        {
            actor = null;
            if (other == null)
            {
                return false;
            }

            if (!requireCharacterController)
            {
                actor = other;
                return true;
            }

            actor = other.GetComponentInParent<CharacterController>();
            return actor != null;
        }

        private void ConfigureSource()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            if (cue != null)
            {
                cue.ApplyTo(audioSource, assignClip: true);
            }

            audioSource.playOnAwake = false;
        }

        private float ResolveTargetVolume()
        {
            return cue != null ? cue.Volume * volumeScale : volumeScale;
        }

        private void StartFade(float targetVolume, float duration, bool playBeforeFade, bool stopAfterFade)
        {
            if (!Application.isPlaying || duration <= 0f)
            {
                if (playBeforeFade && !audioSource.isPlaying)
                {
                    audioSource.Play();
                }

                audioSource.volume = targetVolume;
                if (stopAfterFade)
                {
                    audioSource.Stop();
                }

                return;
            }

            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
            }

            fadeRoutine = StartCoroutine(FadeRoutine(targetVolume, duration, playBeforeFade, stopAfterFade));
        }

        private IEnumerator FadeRoutine(float targetVolume, float duration, bool playBeforeFade, bool stopAfterFade)
        {
            if (playBeforeFade && !audioSource.isPlaying)
            {
                audioSource.volume = 0f;
                audioSource.Play();
            }

            float startVolume = audioSource.volume;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                audioSource.volume = Mathf.Lerp(startVolume, targetVolume, t);
                yield return null;
            }

            audioSource.volume = targetVolume;
            if (stopAfterFade)
            {
                audioSource.Stop();
            }

            fadeRoutine = null;
        }
    }
}
