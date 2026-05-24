using System.Collections.Generic;
using UnityEngine;

namespace WonderfulWorld.Audio
{
    public sealed class WonderlandAudioOneShotPlayer : MonoBehaviour
    {
        private const string PlayerName = "WW_Audio_OneShotPlayer";

        private static WonderlandAudioOneShotPlayer instance;

        private readonly List<AudioSource> sources = new List<AudioSource>(24);

        public static void Play2D(string cueName, float volumeScale = 1f, int maxVoices = 4)
        {
            Play(cueName, Vector3.zero, true, volumeScale, maxVoices);
        }

        public static void PlayAt(string cueName, Vector3 position, float volumeScale = 1f, int maxVoices = 4)
        {
            Play(cueName, position, false, volumeScale, maxVoices);
        }

        public static void Play(WonderlandAudioCue cue, Vector3 position, bool force2D, float volumeScale = 1f, int maxVoices = 4)
        {
            if (cue == null)
            {
                return;
            }

            Instance.PlayInternal(cue, cue.name, position, force2D, volumeScale, maxVoices);
        }

        private static void Play(string cueName, Vector3 position, bool force2D, float volumeScale, int maxVoices)
        {
            WonderlandAudioCue cue = WonderlandRuntimeAudioLibrary.LoadCue(cueName);
            if (cue == null)
            {
                return;
            }

            Instance.PlayInternal(cue, cueName, position, force2D, volumeScale, maxVoices);
        }

        private static WonderlandAudioOneShotPlayer Instance
        {
            get
            {
                if (instance != null)
                {
                    return instance;
                }

                GameObject existing = GameObject.Find(PlayerName);
                if (existing != null)
                {
                    instance = existing.GetComponent<WonderlandAudioOneShotPlayer>();
                }

                if (instance == null)
                {
                    GameObject go = new GameObject(PlayerName);
                    DontDestroyOnLoad(go);
                    instance = go.AddComponent<WonderlandAudioOneShotPlayer>();
                }

                return instance;
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void PlayInternal(WonderlandAudioCue cue, string limiterKey, Vector3 position, bool force2D, float volumeScale, int maxVoices)
        {
            AudioClip clip = cue.PickClip();
            if (clip == null || !CanPlay(limiterKey, Mathf.Max(1, maxVoices)))
            {
                return;
            }

            AudioSource source = GetIdleSource();
            source.name = limiterKey;
            source.transform.position = position;
            source.Stop();
            cue.ApplyTo(source, assignClip: false);
            source.loop = false;
            source.playOnAwake = false;
            if (force2D)
            {
                source.spatialBlend = 0f;
            }

            source.pitch = cue.ResolvePitch();
            source.PlayOneShot(clip, cue.ResolveVolume(volumeScale));
        }

        private bool CanPlay(string limiterKey, int maxVoices)
        {
            if (string.IsNullOrWhiteSpace(limiterKey))
            {
                return true;
            }

            int activeCount = 0;
            for (int i = 0; i < sources.Count; i++)
            {
                AudioSource source = sources[i];
                if (source != null && source.isPlaying && source.name == limiterKey)
                {
                    activeCount++;
                }
            }

            return activeCount < maxVoices;
        }

        private AudioSource GetIdleSource()
        {
            for (int i = 0; i < sources.Count; i++)
            {
                AudioSource source = sources[i];
                if (source != null && !source.isPlaying)
                {
                    return source;
                }
            }

            GameObject sourceObject = new GameObject("WW_Audio_OneShotSource");
            sourceObject.transform.SetParent(transform, false);
            AudioSource newSource = sourceObject.AddComponent<AudioSource>();
            newSource.playOnAwake = false;
            sources.Add(newSource);
            return newSource;
        }
    }
}
