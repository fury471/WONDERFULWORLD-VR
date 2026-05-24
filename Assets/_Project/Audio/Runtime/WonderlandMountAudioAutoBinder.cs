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

            MountFootstepProfile profile = ResolveProfile(mountRoot.transform);
            WonderlandAudioCue cue = WonderlandRuntimeAudioLibrary.ResolveMountFootstepCue(profile);
            if (cue == null)
            {
                return;
            }

            MountFootstepAudio footstepAudio = mountRoot.GetComponent<MountFootstepAudio>();
            if (footstepAudio == null)
            {
                footstepAudio = mountRoot.AddComponent<MountFootstepAudio>();
            }

            footstepAudio.Configure(cue, mountRoot.transform, ResolveEmitter(mountRoot.transform), profile);
        }

        private static MountFootstepProfile ResolveProfile(Transform root)
        {
            if (root != null && root.TryGetComponent(out WonderlandMountAudioProfile explicitProfile))
            {
                return explicitProfile.Profile;
            }

            MountFootstepAudio existingFootsteps = root != null ? root.GetComponent<MountFootstepAudio>() : null;
            if (existingFootsteps != null && existingFootsteps.HasProfileOverride)
            {
                return existingFootsteps.ProfileOverride;
            }

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
            if (root != null && root.TryGetComponent(out WonderlandMountAudioProfile explicitProfile) &&
                explicitProfile.FootstepEmitter != null)
            {
                return explicitProfile.FootstepEmitter;
            }

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
