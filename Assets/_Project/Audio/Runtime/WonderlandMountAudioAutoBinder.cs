using UnityEngine;

namespace WonderfulWorld.Audio
{
    public enum MountFootstepProfile
    {
        Cat,
        Dog,
        Horse
    }

    public static class WonderlandMountAudioAutoBinder
    {
        public static void EnsureFootsteps(GameObject mountRoot)
        {
            if (mountRoot == null)
            {
                return;
            }

            WonderlandAudioCue cue = WonderlandRuntimeAudioLibrary.ResolveMountFootstepCue(mountRoot.transform);
            if (cue == null)
            {
                return;
            }

            MountFootstepAudio footstepAudio = mountRoot.GetComponent<MountFootstepAudio>();
            if (footstepAudio == null)
            {
                footstepAudio = mountRoot.AddComponent<MountFootstepAudio>();
            }

            footstepAudio.Configure(cue, mountRoot.transform, ResolveEmitter(mountRoot.transform), ResolveProfile(mountRoot.transform));
        }

        private static MountFootstepProfile ResolveProfile(Transform root)
        {
            if (WonderlandRuntimeAudioLibrary.HasComponentNamed(root, "HorseSummonV2") ||
                WonderlandRuntimeAudioLibrary.HierarchyContainsName(root, "Horse"))
            {
                return MountFootstepProfile.Horse;
            }

            string path = WonderlandRuntimeAudioLibrary.GetHierarchyPath(root);
            if (path.IndexOf("Horse", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return MountFootstepProfile.Horse;
            }

            if (path.IndexOf("Dog", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                WonderlandRuntimeAudioLibrary.HierarchyContainsName(root, "Dog"))
            {
                return MountFootstepProfile.Dog;
            }

            return MountFootstepProfile.Cat;
        }

        private static Transform ResolveEmitter(Transform root)
        {
            return FindChildContains(root, "Visual") ??
                   FindChildContains(root, "Kitty") ??
                   FindChildContains(root, "Dog") ??
                   FindChildContains(root, "Horse") ??
                   root;
        }

        private static Transform FindChildContains(Transform root, string token)
        {
            if (root == null || string.IsNullOrEmpty(token))
            {
                return null;
            }

            if (root.name.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindChildContains(root.GetChild(i), token);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
