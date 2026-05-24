using UnityEngine;
using UnityEngine.Audio;

namespace WonderfulWorld.Audio
{
    [CreateAssetMenu(fileName = "WW_AudioCue", menuName = "Wonderful World/Audio/Cue")]
    public sealed class WonderlandAudioCue : ScriptableObject
    {
        [SerializeField] private AudioClip[] clips = new AudioClip[0];
        [SerializeField, Range(0f, 1f)] private float volume = 1f;
        [SerializeField] private bool loop;
        [SerializeField] private bool playOnAwake;
        [SerializeField] private AudioMixerGroup mixerGroup;

        [Header("Spatial")]
        [SerializeField, Range(0f, 1f)] private float spatialBlend = 1f;
        [SerializeField, Min(0.01f)] private float minDistance = 1f;
        [SerializeField, Min(0.01f)] private float maxDistance = 25f;
        [SerializeField] private AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;
        [SerializeField, Range(0f, 5f)] private float dopplerLevel = 0f;
        [SerializeField, Range(0, 256)] private int priority = 128;

        [Header("Variation")]
        [SerializeField, Min(0f)] private float randomPitchRange = 0.04f;
        [SerializeField, Range(0f, 1f)] private float randomVolumeRange = 0.06f;

        public bool Loop => loop;
        public bool PlayOnAwake => playOnAwake;
        public float Volume => volume;

        public AudioClip PickClip()
        {
            if (clips == null || clips.Length == 0)
            {
                return null;
            }

            if (clips.Length == 1)
            {
                return clips[0];
            }

            return clips[Random.Range(0, clips.Length)];
        }

        public float ResolveVolume(float scale = 1f)
        {
            float variance = randomVolumeRange <= 0f ? 0f : Random.Range(-randomVolumeRange, randomVolumeRange);
            return Mathf.Clamp01(volume * Mathf.Max(0f, scale) * (1f + variance));
        }

        public float ResolvePitch()
        {
            return Mathf.Max(0.01f, 1f + Random.Range(-randomPitchRange, randomPitchRange));
        }

        public void ApplyTo(AudioSource source, bool assignClip)
        {
            if (source == null)
            {
                return;
            }

            if (assignClip)
            {
                AudioClip clip = PickClip();
                if (clip != null)
                {
                    source.clip = clip;
                }
            }

            source.outputAudioMixerGroup = mixerGroup;
            source.volume = volume;
            source.loop = loop;
            source.playOnAwake = playOnAwake;
            source.spatialBlend = spatialBlend;
            source.minDistance = minDistance;
            source.maxDistance = Mathf.Max(minDistance + 0.01f, maxDistance);
            source.rolloffMode = rolloffMode;
            source.dopplerLevel = dopplerLevel;
            source.priority = priority;
        }
    }
}
