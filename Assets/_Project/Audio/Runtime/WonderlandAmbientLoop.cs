using System.Collections;
using UnityEngine;

namespace WonderfulWorld.Audio
{
    [DisallowMultipleComponent]
    public sealed class WonderlandAmbientLoop : MonoBehaviour
    {
        [SerializeField] private WonderlandAudioCue cue;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private bool playOnEnable = true;
        [SerializeField, Range(0f, 1f)] private float volumeScale = 1f;
        [SerializeField, Min(0f)] private float fadeInSeconds = 1f;
        [SerializeField, Min(0f)] private float fadeOutSeconds = 0.35f;

        private Coroutine fadeRoutine;

        private void Reset()
        {
            audioSource = GetComponent<AudioSource>();
        }

        private void Awake()
        {
            ConfigureSource(assignClip: true);
        }

        private void OnEnable()
        {
            ConfigureSource(assignClip: true);
            if (playOnEnable)
            {
                Play();
            }
        }

        private void OnDisable()
        {
            Stop(immediate: true);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying && audioSource != null && cue != null)
            {
                cue.ApplyTo(audioSource, assignClip: audioSource.clip == null);
                audioSource.playOnAwake = false;
            }
        }
#endif

        public void Play()
        {
            ConfigureSource(assignClip: true);
            if (audioSource == null || audioSource.clip == null)
            {
                return;
            }

            StartFade(targetVolume: ResolveTargetVolume(), duration: fadeInSeconds, playBeforeFade: true, stopAfterFade: false);
        }

        public void Configure(WonderlandAudioCue cue, bool playOnEnable, float volumeScale, float fadeInSeconds, float fadeOutSeconds)
        {
            this.cue = cue;
            this.playOnEnable = playOnEnable;
            this.volumeScale = Mathf.Clamp01(volumeScale);
            this.fadeInSeconds = Mathf.Max(0f, fadeInSeconds);
            this.fadeOutSeconds = Mathf.Max(0f, fadeOutSeconds);
            ConfigureSource(assignClip: true);
        }

        public void Stop(bool immediate = false)
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

            StartFade(targetVolume: 0f, duration: fadeOutSeconds, playBeforeFade: false, stopAfterFade: true);
        }

        private void ConfigureSource(bool assignClip)
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
                cue.ApplyTo(audioSource, assignClip: assignClip || audioSource.clip == null);
            }

            audioSource.playOnAwake = false;
        }

        private float ResolveTargetVolume()
        {
            return cue != null ? cue.Volume * volumeScale : volumeScale;
        }

        private void StartFade(float targetVolume, float duration, bool playBeforeFade, bool stopAfterFade)
        {
            if (audioSource == null)
            {
                return;
            }

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
