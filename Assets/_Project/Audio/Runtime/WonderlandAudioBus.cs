using UnityEngine;
using UnityEngine.Audio;

namespace WonderfulWorld.Audio
{
    public static class WonderlandAudioBus
    {
        public const string MasterVolumeParameter = "MasterVolume";
        public const string MasterVolumePrefKey = "WW.Settings.Audio.MasterVolume";

        private const float DefaultMasterVolume = 0.85f;
        private const float MinAudibleLinear = 0.0001f;
        private const float MutedDecibels = -80f;

        private static AudioMixer mixer;

        public static float MasterVolume => PlayerPrefs.GetFloat(MasterVolumePrefKey, DefaultMasterVolume);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplySavedSettings()
        {
            ApplyMasterVolume(MasterVolume);
        }

        public static void SetMixer(AudioMixer audioMixer, bool applyImmediately = false)
        {
            mixer = audioMixer;
            if (applyImmediately)
            {
                ApplyMasterVolume(MasterVolume);
            }
        }

        public static void SetMasterVolume(float linearVolume, bool save)
        {
            float clamped = Mathf.Clamp01(linearVolume);
            if (save)
            {
                PlayerPrefs.SetFloat(MasterVolumePrefKey, clamped);
                PlayerPrefs.Save();
            }

            ApplyMasterVolume(clamped);
        }

        public static float LinearToDecibels(float linearVolume)
        {
            return linearVolume <= MinAudibleLinear
                ? MutedDecibels
                : Mathf.Log10(Mathf.Clamp01(linearVolume)) * 20f;
        }

        private static void ApplyMasterVolume(float linearVolume)
        {
            float clamped = Mathf.Clamp01(linearVolume);
            AudioListener.volume = clamped;

            if (mixer != null)
            {
                mixer.SetFloat(MasterVolumeParameter, LinearToDecibels(clamped));
            }
        }
    }
}
