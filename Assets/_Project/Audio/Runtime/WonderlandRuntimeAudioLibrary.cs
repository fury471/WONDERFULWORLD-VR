using UnityEngine;
using UnityEngine.SceneManagement;

namespace WonderfulWorld.Audio
{
    public static class WonderlandRuntimeAudioLibrary
    {
        private const string CueResourceRoot = "AudioCues/";
        private const string GlobalAmbienceName = "WW_Audio_NightForestAmbience";

        private static bool sceneHookRegistered;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void BootstrapAfterSceneLoad()
        {
            EnsureGlobalNightForestAmbience();

            if (!sceneHookRegistered)
            {
                SceneManager.sceneLoaded += OnSceneLoaded;
                sceneHookRegistered = true;
            }
        }

        public static WonderlandAudioCue LoadCue(string cueName)
        {
            if (string.IsNullOrWhiteSpace(cueName))
            {
                return null;
            }

            return Resources.Load<WonderlandAudioCue>(CueResourceRoot + cueName);
        }

        public static void EnsureGlobalNightForestAmbience()
        {
            WonderlandAudioCue cue = LoadCue("WW_Ambience_NightForest");
            if (cue == null)
            {
                return;
            }

            GameObject ambience = GameObject.Find(GlobalAmbienceName);
            if (ambience == null)
            {
                ambience = new GameObject(GlobalAmbienceName);
                Object.DontDestroyOnLoad(ambience);
            }

            WonderlandAmbientLoop loop = ambience.GetComponent<WonderlandAmbientLoop>();
            if (loop == null)
            {
                loop = ambience.AddComponent<WonderlandAmbientLoop>();
            }

            loop.Configure(cue, playOnEnable: true, volumeScale: 1f, fadeInSeconds: 2.5f, fadeOutSeconds: 0.75f);
            loop.Play();
        }

        public static WonderlandAudioCue ResolveMountFootstepCue(Transform root)
        {
            string path = GetHierarchyPath(root);
            if (HasComponentNamed(root, "HorseSummonV2") ||
                path.IndexOf("Horse", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                HierarchyContainsName(root, "Horse"))
            {
                return LoadCue("WW_Footsteps_Horse");
            }

            if (path.IndexOf("Dog", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                HierarchyContainsName(root, "Dog"))
            {
                return LoadCue("WW_Footsteps_Dog");
            }

            return LoadCue("WW_Footsteps_Cat");
        }

        public static WonderlandAudioCue ResolveMountFootstepCue(MountFootstepProfile profile)
        {
            switch (profile)
            {
                case MountFootstepProfile.Horse:
                    return LoadCue("WW_Footsteps_Horse");
                case MountFootstepProfile.Dog:
                    return LoadCue("WW_Footsteps_Dog");
                default:
                    return LoadCue("WW_Footsteps_Cat");
            }
        }

        public static WonderlandAudioCue ResolveVoiceCue(Transform root, AudioSource source)
        {
            string clipName = source != null && source.clip != null ? source.clip.name : string.Empty;
            string path = GetHierarchyPath(root);
            string probe = clipName + "/" + path;

            if (probe.IndexOf("Horse", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return LoadCue("WW_Voice_Horse");
            }

            if (probe.IndexOf("Dog", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return LoadCue("WW_Voice_Dog");
            }

            if (probe.IndexOf("Cat", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return LoadCue("WW_Voice_Cat");
            }

            return null;
        }

        public static WonderlandAudioCue ResolveVoiceCue(MountFootstepProfile profile)
        {
            switch (profile)
            {
                case MountFootstepProfile.Horse:
                    return LoadCue("WW_Voice_Horse");
                case MountFootstepProfile.Dog:
                    return LoadCue("WW_Voice_Dog");
                default:
                    return LoadCue("WW_Voice_Cat");
            }
        }

        public static string GetHierarchyPath(Transform transform)
        {
            if (transform == null)
            {
                return string.Empty;
            }

            string path = transform.name;
            Transform parent = transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }

        public static bool HasComponentNamed(Transform root, string componentName)
        {
            if (root == null || string.IsNullOrWhiteSpace(componentName))
            {
                return false;
            }

            MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour != null && behaviour.GetType().Name == componentName)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool HierarchyContainsName(Transform root, string token)
        {
            if (root == null || string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            if (root.name.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                if (HierarchyContainsName(root.GetChild(i), token))
                {
                    return true;
                }
            }

            return false;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureGlobalNightForestAmbience();
        }
    }
}
