using System.Collections;
using UnityEngine;

namespace WonderfulWorld.Audio
{
    [DisallowMultipleComponent]
    public sealed class WonderlandAmbientAccentPlayer : MonoBehaviour
    {
        [SerializeField] private WonderlandAudioCue cue;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private bool playOnEnable = true;
        [SerializeField, Range(0f, 1f)] private float volumeScale = 1f;
        [SerializeField, Min(0f)] private float minDelaySeconds = 18f;
        [SerializeField, Min(0f)] private float maxDelaySeconds = 42f;
        [SerializeField, Min(0.1f)] private float minWindowSeconds = 5f;
        [SerializeField, Min(0.1f)] private float maxWindowSeconds = 12f;
        [SerializeField, Min(0f)] private float fadeInSeconds = 1.25f;
        [SerializeField, Min(0f)] private float fadeOutSeconds = 2f;
        [SerializeField] private bool randomizeClipStart = true;

        private Coroutine playRoutine;

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

        public void Configure(
            WonderlandAudioCue cue,
            bool playOnEnable,
            float volumeScale,
            float minDelaySeconds,
            float maxDelaySeconds,
            float minWindowSeconds,
            float maxWindowSeconds,
            float fadeInSeconds,
            float fadeOutSeconds,
            bool randomizeClipStart)
        {
            this.cue = cue;
            this.playOnEnable = playOnEnable;
            this.volumeScale = Mathf.Clamp01(volumeScale);
            this.minDelaySeconds = Mathf.Max(0f, minDelaySeconds);
            this.maxDelaySeconds = Mathf.Max(this.minDelaySeconds, maxDelaySeconds);
            this.minWindowSeconds = Mathf.Max(0.1f, minWindowSeconds);
            this.maxWindowSeconds = Mathf.Max(this.minWindowSeconds, maxWindowSeconds);
            this.fadeInSeconds = Mathf.Max(0f, fadeInSeconds);
            this.fadeOutSeconds = Mathf.Max(0f, fadeOutSeconds);
            this.randomizeClipStart = randomizeClipStart;
            ConfigureSource(assignClip: true);
        }

        public void Play()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (playRoutine != null)
            {
                StopCoroutine(playRoutine);
            }

            playRoutine = StartCoroutine(PlayRoutine());
        }

        public void Stop(bool immediate = false)
        {
            if (playRoutine != null)
            {
                StopCoroutine(playRoutine);
                playRoutine = null;
            }

            if (audioSource == null)
            {
                return;
            }

            if (immediate || !Application.isPlaying)
            {
                audioSource.Stop();
                audioSource.volume = 0f;
                return;
            }

            StartCoroutine(FadeTo(0f, fadeOutSeconds, stopAfterFade: true));
        }

        private IEnumerator PlayRoutine()
        {
            yield return WaitForRandomDelay();

            while (enabled)
            {
                ConfigureSource(assignClip: true);
                if (audioSource == null || audioSource.clip == null)
                {
                    yield return WaitForRandomDelay();
                    continue;
                }

                float windowSeconds = Random.Range(minWindowSeconds, maxWindowSeconds);
                if (randomizeClipStart && audioSource.clip.length > windowSeconds + 1f)
                {
                    audioSource.time = Random.Range(0f, audioSource.clip.length - windowSeconds);
                }

                audioSource.volume = 0f;
                audioSource.Play();
                yield return FadeTo(ResolveTargetVolume(), fadeInSeconds, stopAfterFade: false);
                yield return new WaitForSeconds(Mathf.Max(0.1f, windowSeconds));
                yield return FadeTo(0f, fadeOutSeconds, stopAfterFade: true);
                yield return WaitForRandomDelay();
            }

            playRoutine = null;
        }

        private IEnumerator WaitForRandomDelay()
        {
            float delay = Random.Range(minDelaySeconds, Mathf.Max(minDelaySeconds, maxDelaySeconds));
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }
        }

        private IEnumerator FadeTo(float targetVolume, float duration, bool stopAfterFade)
        {
            if (audioSource == null)
            {
                yield break;
            }

            if (duration <= 0f)
            {
                audioSource.volume = targetVolume;
                if (stopAfterFade)
                {
                    audioSource.Stop();
                }

                yield break;
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

            audioSource.loop = false;
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }

        private float ResolveTargetVolume()
        {
            return cue != null ? cue.ResolveVolume(volumeScale) : volumeScale;
        }
    }
}
