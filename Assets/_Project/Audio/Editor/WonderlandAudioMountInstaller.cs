using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using WonderfulWorld.Audio;

namespace WonderfulWorld.Audio.Editor
{
    public static class WonderlandAudioMountInstaller
    {
        private const string ScenePath = "Assets/_Project/World/Persistent/World_WonderlandPark.unity";
        private const string HaoboScenePath = "Assets/_Project/Sandbox/Haobo/UPDATE_World_WonderlandPark_Haobo.unity";
        private const string WaterfallPrefabPath = "Assets/_Project/Art/waterfall/Waterfall.prefab";
        private const string CatMountPrefabPath = "Assets/_Project/Features/Mounts/Prefabs/MountRouteTestRoot_V2.prefab";
        private const string LegacyCatMountPrefabPath = "Assets/_Project/Features/Mounts/Prefabs/CatMount_Root.prefab";

        private const string CueRoot = "Assets/_Project/Audio/Resources/AudioCues";
        private const string MixerPath = "Assets/_Project/Audio/Mixers/WW_AudioMixer.mixer";
        private const string NightForestClip = "Assets/_Project/Audio/Ambience/NightForest_AfterRain_BigSoundBank_0555.ogg";
        private const string WaterfallLoopClip = "Assets/_Project/Audio/SFX/Water/Watercourse_5_2_BigSoundBank_3137.ogg";
        private const string WaterfallDetailClip = "Assets/_Project/Audio/SFX/Water/SmallWaterfall_02_BigSoundBank_0219.ogg";
        private const string AnimalFootstepsClip = "Assets/_Project/Audio/SFX/Mounts/AnimalFootsteps_CC0_Freesound_658429.mp3";
        private const string DogRunClip = "Assets/_Project/Audio/SFX/Mounts/DogRunPast_CC0_Freesound_827320.mp3";
        private const string HorseWalkClip = "Assets/_Project/Audio/SFX/Mounts/HorseWalk_Path_BigSoundBank_1854.ogg";
        private const string HorseConcreteClip = "Assets/_Project/Audio/SFX/Mounts/HorseSteps_Concrete_BigSoundBank_0496.ogg";
        private const string CatVoiceClip = "Assets/_Project/Audio/Music/animals/cat.mp3";
        private const string DogVoiceClip = "Assets/_Project/Audio/Music/animals/dog.mp3";
        private const string HorseVoiceClip = "Assets/_Project/Audio/Music/animals/horse.mp3";
        private const string UiClickClip = "Assets/_Project/Audio/SFX/UI/UI_Click_RaspberryMouse_BigSoundBank_1735.ogg";
        private const string MagicWhooshClip = "Assets/_Project/Audio/SFX/Magic/Whoosh_Rope_BigSoundBank_1796.ogg";
        private const string FirecrackerClip = "Assets/_Project/Audio/SFX/Fireworks/FirecrackerWick_BigSoundBank_1140.ogg";
        private const string ChimesDreamClip = "Assets/_Project/Audio/SFX/Magic/ChimesDream_BigSoundBank_2084.ogg";
        private const string SparklingCandleClip = "Assets/_Project/Audio/SFX/Fireworks/SparklingCandle_BigSoundBank_1279.ogg";
        private const string GrowthLeavesClip = "Assets/_Project/Audio/SFX/Growth/MiscanthusLeaves_BigSoundBank_1814.ogg";

        [MenuItem("Wonderful World/Audio/Install Production Audio")]
        public static void InstallProductionAudio()
        {
            AssetDatabase.StartAssetEditing();
            try
            {
                ConfigureImporters();
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            AssetDatabase.Refresh();

            CueSet cues = CreateOrUpdateCues();
            ApplyToPrefab(WaterfallPrefabPath, root => InstallWaterfallAudio(root.transform, cues));
            ApplyToPrefab(CatMountPrefabPath, root => InstallMountAudio(root, cues, forceProfile: "Cat"));
            ApplyToPrefab(LegacyCatMountPrefabPath, root => InstallLegacyMountAudio(root, cues.CatFootsteps));
            ApplyToScene(ScenePath, cues);
            ApplyToScene(HaoboScenePath, cues);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[WonderlandAudioMountInstaller] Production audio install complete.");
        }

        private static void ConfigureImporters()
        {
            ConfigureImporter(NightForestClip, AudioClipLoadType.Streaming, 0.55f, preload: false, background: true, forceMono: false);
            ConfigureImporter(WaterfallLoopClip, AudioClipLoadType.Streaming, 0.6f, preload: false, background: true, forceMono: true);
            ConfigureImporter(WaterfallDetailClip, AudioClipLoadType.Streaming, 0.6f, preload: false, background: true, forceMono: true);
            ConfigureImporter(AnimalFootstepsClip, AudioClipLoadType.DecompressOnLoad, 0.75f, preload: true, background: false, forceMono: true);
            ConfigureImporter(DogRunClip, AudioClipLoadType.DecompressOnLoad, 0.75f, preload: true, background: false, forceMono: false);
            ConfigureImporter(HorseWalkClip, AudioClipLoadType.CompressedInMemory, 0.75f, preload: true, background: false, forceMono: false);
            ConfigureImporter(HorseConcreteClip, AudioClipLoadType.DecompressOnLoad, 0.75f, preload: true, background: false, forceMono: true);
            ConfigureImporter(CatVoiceClip, AudioClipLoadType.CompressedInMemory, 0.7f, preload: true, background: false, forceMono: false);
            ConfigureImporter(DogVoiceClip, AudioClipLoadType.CompressedInMemory, 0.7f, preload: true, background: false, forceMono: false);
            ConfigureImporter(HorseVoiceClip, AudioClipLoadType.CompressedInMemory, 0.7f, preload: true, background: false, forceMono: false);
            ConfigureImporter(UiClickClip, AudioClipLoadType.DecompressOnLoad, 0.85f, preload: true, background: false, forceMono: true);
            ConfigureImporter(MagicWhooshClip, AudioClipLoadType.DecompressOnLoad, 0.8f, preload: true, background: false, forceMono: false);
            ConfigureImporter(FirecrackerClip, AudioClipLoadType.DecompressOnLoad, 0.8f, preload: true, background: false, forceMono: true);
            ConfigureImporter(ChimesDreamClip, AudioClipLoadType.DecompressOnLoad, 0.8f, preload: true, background: false, forceMono: false);
            ConfigureImporter(SparklingCandleClip, AudioClipLoadType.Streaming, 0.65f, preload: false, background: true, forceMono: true);
            ConfigureImporter(GrowthLeavesClip, AudioClipLoadType.DecompressOnLoad, 0.8f, preload: true, background: false, forceMono: true);
        }

        private static void ConfigureImporter(string path, AudioClipLoadType loadType, float quality, bool preload, bool background, bool forceMono)
        {
            AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[WonderlandAudioMountInstaller] Missing audio importer for {path}.");
                return;
            }

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = loadType;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = quality;
            settings.preloadAudioData = preload;
            importer.defaultSampleSettings = settings;
            importer.loadInBackground = background;
            importer.forceToMono = forceMono;
            importer.SaveAndReimport();
        }

        private static CueSet CreateOrUpdateCues()
        {
            EnsureFolder(CueRoot);

            CueSet cues = new CueSet
            {
                NightForest = CreateCue(
                    $"{CueRoot}/WW_Ambience_NightForest.asset",
                    new[] { LoadClip(NightForestClip) },
                    volume: 0.32f,
                    loop: true,
                    spatialBlend: 0f,
                    minDistance: 1f,
                    maxDistance: 500f,
                    randomPitchRange: 0f,
                    randomVolumeRange: 0f,
                    priority: 180),
                WaterfallLoop = CreateCue(
                    $"{CueRoot}/WW_Spatial_WaterfallLoop.asset",
                    new[] { LoadClip(WaterfallLoopClip) },
                    volume: 0.62f,
                    loop: true,
                    spatialBlend: 1f,
                    minDistance: 4f,
                    maxDistance: 28f,
                    randomPitchRange: 0f,
                    randomVolumeRange: 0f,
                    priority: 150),
                WaterfallDetail = CreateCue(
                    $"{CueRoot}/WW_Spatial_WaterfallDetail.asset",
                    new[] { LoadClip(WaterfallDetailClip) },
                    volume: 0.34f,
                    loop: true,
                    spatialBlend: 1f,
                    minDistance: 1.5f,
                    maxDistance: 14f,
                    randomPitchRange: 0f,
                    randomVolumeRange: 0f,
                    priority: 145),
                CatFootsteps = CreateCue(
                    $"{CueRoot}/WW_Footsteps_Cat.asset",
                    new[] { LoadClip(AnimalFootstepsClip) },
                    volume: 0.24f,
                    loop: false,
                    spatialBlend: 1f,
                    minDistance: 0.75f,
                    maxDistance: 10f,
                    randomPitchRange: 0.09f,
                    randomVolumeRange: 0.18f,
                    priority: 190),
                DogFootsteps = CreateCue(
                    $"{CueRoot}/WW_Footsteps_Dog.asset",
                    new[] { LoadClip(AnimalFootstepsClip) },
                    volume: 0.28f,
                    loop: false,
                    spatialBlend: 1f,
                    minDistance: 0.9f,
                    maxDistance: 13f,
                    randomPitchRange: 0.07f,
                    randomVolumeRange: 0.16f,
                    priority: 188),
                HorseFootsteps = CreateCue(
                    $"{CueRoot}/WW_Footsteps_Horse.asset",
                    new[] { LoadClip(HorseWalkClip), LoadClip(HorseConcreteClip) },
                    volume: 0.25f,
                    loop: false,
                    spatialBlend: 1f,
                    minDistance: 1.6f,
                    maxDistance: 28f,
                    randomPitchRange: 0.05f,
                    randomVolumeRange: 0.12f,
                    priority: 185),
                CatVoice = CreateCue(
                    $"{CueRoot}/WW_Voice_Cat.asset",
                    new[] { LoadClip(CatVoiceClip) },
                    volume: 0.9f,
                    loop: false,
                    spatialBlend: 1f,
                    minDistance: 1f,
                    maxDistance: 8f,
                    randomPitchRange: 0.02f,
                    randomVolumeRange: 0.02f,
                    priority: 100),
                DogVoice = CreateCue(
                    $"{CueRoot}/WW_Voice_Dog.asset",
                    new[] { LoadClip(DogVoiceClip) },
                    volume: 0.9f,
                    loop: false,
                    spatialBlend: 1f,
                    minDistance: 1f,
                    maxDistance: 9f,
                    randomPitchRange: 0.02f,
                    randomVolumeRange: 0.02f,
                    priority: 100),
                HorseVoice = CreateCue(
                    $"{CueRoot}/WW_Voice_Horse.asset",
                    new[] { LoadClip(HorseVoiceClip) },
                    volume: 0.9f,
                    loop: false,
                    spatialBlend: 1f,
                    minDistance: 1f,
                    maxDistance: 12f,
                    randomPitchRange: 0.02f,
                    randomVolumeRange: 0.02f,
                    priority: 100),
                UiClick = CreateCue(
                    $"{CueRoot}/WW_UI_Click.asset",
                    new[] { LoadClip(UiClickClip) },
                    volume: 0.5f,
                    loop: false,
                    spatialBlend: 0f,
                    minDistance: 1f,
                    maxDistance: 5f,
                    randomPitchRange: 0.02f,
                    randomVolumeRange: 0.04f,
                    priority: 64),
                UiHover = CreateCue(
                    $"{CueRoot}/WW_UI_Hover.asset",
                    new[] { LoadClip(UiClickClip) },
                    volume: 0.22f,
                    loop: false,
                    spatialBlend: 0f,
                    minDistance: 1f,
                    maxDistance: 5f,
                    randomPitchRange: 0.03f,
                    randomVolumeRange: 0.04f,
                    priority: 72),
                MountTransition = CreateCue(
                    $"{CueRoot}/WW_SFX_MountTransition.asset",
                    new[] { LoadClip(MagicWhooshClip) },
                    volume: 0.42f,
                    loop: false,
                    spatialBlend: 1f,
                    minDistance: 1f,
                    maxDistance: 12f,
                    randomPitchRange: 0.04f,
                    randomVolumeRange: 0.08f,
                    priority: 120),
                ScaleShift = CreateCue(
                    $"{CueRoot}/WW_SFX_ScaleShift.asset",
                    new[] { LoadClip(MagicWhooshClip) },
                    volume: 0.34f,
                    loop: false,
                    spatialBlend: 0f,
                    minDistance: 1f,
                    maxDistance: 5f,
                    randomPitchRange: 0.06f,
                    randomVolumeRange: 0.04f,
                    priority: 72),
                FireworkLaunch = CreateCue(
                    $"{CueRoot}/WW_SFX_FireworkLaunch.asset",
                    new[] { LoadClip(MagicWhooshClip) },
                    volume: 0.54f,
                    loop: false,
                    spatialBlend: 1f,
                    minDistance: 2f,
                    maxDistance: 45f,
                    randomPitchRange: 0.04f,
                    randomVolumeRange: 0.08f,
                    priority: 112),
                FireworkBurst = CreateCue(
                    $"{CueRoot}/WW_SFX_FireworkBurst.asset",
                    new[] { LoadClip(FirecrackerClip) },
                    volume: 0.72f,
                    loop: false,
                    spatialBlend: 1f,
                    minDistance: 4f,
                    maxDistance: 65f,
                    randomPitchRange: 0.05f,
                    randomVolumeRange: 0.1f,
                    priority: 104),
                CrystalSelect = CreateCue(
                    $"{CueRoot}/WW_SFX_CrystalSelect.asset",
                    new[] { LoadClip(ChimesDreamClip) },
                    volume: 0.52f,
                    loop: false,
                    spatialBlend: 1f,
                    minDistance: 1f,
                    maxDistance: 14f,
                    randomPitchRange: 0.03f,
                    randomVolumeRange: 0.08f,
                    priority: 96),
                CrystalCollapse = CreateCue(
                    $"{CueRoot}/WW_SFX_CrystalCollapse.asset",
                    new[] { LoadClip(MagicWhooshClip), LoadClip(ChimesDreamClip) },
                    volume: 0.5f,
                    loop: false,
                    spatialBlend: 1f,
                    minDistance: 1f,
                    maxDistance: 16f,
                    randomPitchRange: 0.04f,
                    randomVolumeRange: 0.08f,
                    priority: 96),
                GrowthRustle = CreateCue(
                    $"{CueRoot}/WW_SFX_GrowthRustle.asset",
                    new[] { LoadClip(GrowthLeavesClip) },
                    volume: 0.48f,
                    loop: false,
                    spatialBlend: 1f,
                    minDistance: 1f,
                    maxDistance: 16f,
                    randomPitchRange: 0.05f,
                    randomVolumeRange: 0.12f,
                    priority: 136),
                MagicCollect = CreateCue(
                    $"{CueRoot}/WW_SFX_MagicCollect.asset",
                    new[] { LoadClip(ChimesDreamClip) },
                    volume: 0.38f,
                    loop: false,
                    spatialBlend: 0f,
                    minDistance: 1f,
                    maxDistance: 5f,
                    randomPitchRange: 0.02f,
                    randomVolumeRange: 0.06f,
                    priority: 80),
                MagicRelease = CreateCue(
                    $"{CueRoot}/WW_SFX_MagicRelease.asset",
                    new[] { LoadClip(MagicWhooshClip), LoadClip(ChimesDreamClip) },
                    volume: 0.58f,
                    loop: false,
                    spatialBlend: 0f,
                    minDistance: 1f,
                    maxDistance: 5f,
                    randomPitchRange: 0.03f,
                    randomVolumeRange: 0.08f,
                    priority: 76),
                MagicChargedRelease = CreateCue(
                    $"{CueRoot}/WW_SFX_MagicChargedRelease.asset",
                    new[] { LoadClip(SparklingCandleClip) },
                    volume: 0.62f,
                    loop: false,
                    spatialBlend: 0f,
                    minDistance: 1f,
                    maxDistance: 5f,
                    randomPitchRange: 0.02f,
                    randomVolumeRange: 0.04f,
                    priority: 74),
            };

            AssignMixerGroups(cues);
            return cues;
        }

        private static void AssignMixerGroups(CueSet cues)
        {
            AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            if (mixer == null)
            {
                return;
            }

            AudioMixerGroup ambience = FindMixerGroup(mixer, "Ambience");
            AudioMixerGroup sfx = FindMixerGroup(mixer, "SFX");
            AudioMixerGroup ui = FindMixerGroup(mixer, "UI");
            AudioMixerGroup voice = FindMixerGroup(mixer, "Voice");

            AssignMixerGroup(cues.NightForest, ambience);
            AssignMixerGroup(cues.WaterfallLoop, ambience);
            AssignMixerGroup(cues.WaterfallDetail, ambience);
            AssignMixerGroup(cues.CatFootsteps, sfx);
            AssignMixerGroup(cues.DogFootsteps, sfx);
            AssignMixerGroup(cues.HorseFootsteps, sfx);
            AssignMixerGroup(cues.CatVoice, voice);
            AssignMixerGroup(cues.DogVoice, voice);
            AssignMixerGroup(cues.HorseVoice, voice);
            AssignMixerGroup(cues.UiClick, ui);
            AssignMixerGroup(cues.UiHover, ui);
            AssignMixerGroup(cues.MountTransition, sfx);
            AssignMixerGroup(cues.ScaleShift, sfx);
            AssignMixerGroup(cues.FireworkLaunch, sfx);
            AssignMixerGroup(cues.FireworkBurst, sfx);
            AssignMixerGroup(cues.CrystalSelect, sfx);
            AssignMixerGroup(cues.CrystalCollapse, sfx);
            AssignMixerGroup(cues.GrowthRustle, sfx);
            AssignMixerGroup(cues.MagicCollect, sfx);
            AssignMixerGroup(cues.MagicRelease, sfx);
            AssignMixerGroup(cues.MagicChargedRelease, sfx);
        }

        private static AudioMixerGroup FindMixerGroup(AudioMixer mixer, string groupName)
        {
            if (mixer == null)
            {
                return null;
            }

            AudioMixerGroup[] groups = mixer.FindMatchingGroups(groupName);
            for (int i = 0; i < groups.Length; i++)
            {
                if (groups[i] != null && groups[i].name == groupName)
                {
                    return groups[i];
                }
            }

            return null;
        }

        private static void AssignMixerGroup(WonderlandAudioCue cue, AudioMixerGroup group)
        {
            if (cue == null || group == null)
            {
                return;
            }

            SerializedObject serializedCue = new SerializedObject(cue);
            serializedCue.FindProperty("mixerGroup").objectReferenceValue = group;
            serializedCue.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(cue);
        }

        private static AudioClip LoadClip(string path)
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null)
            {
                Debug.LogWarning($"[WonderlandAudioMountInstaller] Missing clip: {path}");
            }

            return clip;
        }

        private static WonderlandAudioCue CreateCue(
            string path,
            AudioClip[] clips,
            float volume,
            bool loop,
            float spatialBlend,
            float minDistance,
            float maxDistance,
            float randomPitchRange,
            float randomVolumeRange,
            int priority)
        {
            WonderlandAudioCue cue = AssetDatabase.LoadAssetAtPath<WonderlandAudioCue>(path);
            if (cue == null)
            {
                cue = ScriptableObject.CreateInstance<WonderlandAudioCue>();
                AssetDatabase.CreateAsset(cue, path);
            }

            SerializedObject serializedCue = new SerializedObject(cue);
            SetClipArray(serializedCue.FindProperty("clips"), clips);
            serializedCue.FindProperty("volume").floatValue = volume;
            serializedCue.FindProperty("loop").boolValue = loop;
            serializedCue.FindProperty("playOnAwake").boolValue = false;
            serializedCue.FindProperty("spatialBlend").floatValue = spatialBlend;
            serializedCue.FindProperty("minDistance").floatValue = minDistance;
            serializedCue.FindProperty("maxDistance").floatValue = maxDistance;
            serializedCue.FindProperty("rolloffMode").enumValueIndex = (int)AudioRolloffMode.Logarithmic;
            serializedCue.FindProperty("dopplerLevel").floatValue = 0f;
            serializedCue.FindProperty("priority").intValue = priority;
            serializedCue.FindProperty("randomPitchRange").floatValue = randomPitchRange;
            serializedCue.FindProperty("randomVolumeRange").floatValue = randomVolumeRange;
            serializedCue.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(cue);
            return cue;
        }

        private static void SetClipArray(SerializedProperty property, AudioClip[] clips)
        {
            property.arraySize = clips.Length;
            for (int i = 0; i < clips.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = clips[i];
            }
        }

        private static void ApplyToPrefab(string prefabPath, Action<GameObject> install)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (root == null)
            {
                Debug.LogWarning($"[WonderlandAudioMountInstaller] Missing prefab: {prefabPath}");
                return;
            }

            try
            {
                install(root);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ApplyToScene(string scenePath, CueSet cues)
        {
            if (string.IsNullOrEmpty(scenePath) || !System.IO.File.Exists(scenePath))
            {
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            InstallSceneAmbience(cues.NightForest);
            InstallAllWaterfalls(cues);
            InstallAllMounts(cues);
            MigrateLegacyProximityVoicePlayers(cues);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void InstallSceneAmbience(WonderlandAudioCue cue)
        {
            GameObject ambience = GameObject.Find("WW_Audio_NightForestAmbience");
            if (ambience == null)
            {
                ambience = new GameObject("WW_Audio_NightForestAmbience");
            }

            WonderlandAmbientLoop loop = EnsureComponent<WonderlandAmbientLoop>(ambience);
            AssignAmbientLoop(loop, cue, volumeScale: 1f, fadeIn: 2.5f, fadeOut: 0.75f);
        }

        private static void InstallAllWaterfalls(CueSet cues)
        {
#if UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
            StylizedWaterfallController[] waterfalls = UnityEngine.Object.FindObjectsByType<StylizedWaterfallController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            StylizedWaterfallController[] waterfalls = UnityEngine.Object.FindObjectsOfType<StylizedWaterfallController>(true);
#endif
            foreach (StylizedWaterfallController waterfall in waterfalls)
            {
                if (waterfall != null)
                {
                    InstallWaterfallAudio(waterfall.transform, cues);
                }
            }
        }

        private static void InstallWaterfallAudio(Transform root, CueSet cues)
        {
            Transform main = EnsureChild(root, "Audio_Waterfall_Main");
            main.localPosition = Vector3.zero;
            AssignAmbientLoop(EnsureComponent<WonderlandAmbientLoop>(main.gameObject), cues.WaterfallLoop, volumeScale: 1f, fadeIn: 0.8f, fadeOut: 0.3f);

            Transform detail = EnsureChild(root, "Audio_Waterfall_Splash");
            detail.localPosition = new Vector3(0f, -2.2f, 2.1f);
            AssignAmbientLoop(EnsureComponent<WonderlandAmbientLoop>(detail.gameObject), cues.WaterfallDetail, volumeScale: 1f, fadeIn: 0.8f, fadeOut: 0.3f);
        }

        private static void InstallAllMounts(CueSet cues)
        {
#if UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
            CatRideControllerV2[] controllers = UnityEngine.Object.FindObjectsByType<CatRideControllerV2>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            CatRideControllerV2[] controllers = UnityEngine.Object.FindObjectsOfType<CatRideControllerV2>(true);
#endif
            foreach (CatRideControllerV2 controller in controllers)
            {
                if (controller != null)
                {
                    InstallMountAudio(controller.gameObject, cues, forceProfile: null);
                }
            }

#if UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
            MountController[] legacyControllers = UnityEngine.Object.FindObjectsByType<MountController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            MountController[] legacyControllers = UnityEngine.Object.FindObjectsOfType<MountController>(true);
#endif
            foreach (MountController controller in legacyControllers)
            {
                if (controller != null)
                {
                    InstallLegacyMountAudio(controller.gameObject, cues.CatFootsteps);
                }
            }
        }

        private static void InstallMountAudio(GameObject root, CueSet cues, string forceProfile)
        {
            WonderlandAudioCue cue = ResolveMountCue(root.transform, cues, forceProfile);
            MountFootstepAudio footstep = EnsureComponent<MountFootstepAudio>(root);
            ConfigureFootstep(footstep, cue, root.transform, ResolveMountEmitter(root.transform), ResolveMountProfile(root.transform, forceProfile));
        }

        private static void InstallLegacyMountAudio(GameObject root, WonderlandAudioCue catCue)
        {
            MountFootstepAudio footstep = EnsureComponent<MountFootstepAudio>(root);
            ConfigureFootstep(footstep, catCue, root.transform, root.transform, MountProfile.Cat);
        }

        private static WonderlandAudioCue ResolveMountCue(Transform root, CueSet cues, string forceProfile)
        {
            MountProfile profile = ResolveMountProfile(root, forceProfile);
            switch (profile)
            {
                case MountProfile.Horse:
                    return cues.HorseFootsteps;
                case MountProfile.Dog:
                    return cues.DogFootsteps;
                default:
                    return cues.CatFootsteps;
            }
        }

        private static MountProfile ResolveMountProfile(Transform root, string forceProfile)
        {
            if (string.IsNullOrEmpty(forceProfile) &&
                (WonderlandRuntimeAudioLibrary.HasComponentNamed(root, "HorseSummonV2") ||
                 WonderlandRuntimeAudioLibrary.HierarchyContainsName(root, "Horse")))
            {
                return MountProfile.Horse;
            }

            string profileText = forceProfile ?? GetHierarchyPath(root);
            if (profileText.IndexOf("Horse", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return MountProfile.Horse;
            }

            if (profileText.IndexOf("Dog", StringComparison.OrdinalIgnoreCase) >= 0 ||
                (string.IsNullOrEmpty(forceProfile) && WonderlandRuntimeAudioLibrary.HierarchyContainsName(root, "Dog")))
            {
                return MountProfile.Dog;
            }

            return MountProfile.Cat;
        }

        private static Transform ResolveMountEmitter(Transform root)
        {
            return FindChildContains(root, "Visual") ??
                   FindChildContains(root, "Kitty") ??
                   FindChildContains(root, "Dog") ??
                   FindChildContains(root, "Horse") ??
                   root;
        }

        private static void ConfigureFootstep(MountFootstepAudio footstep, WonderlandAudioCue cue, Transform movementRoot, Transform emitter, MountProfile profile)
        {
            SerializedObject serializedFootstep = new SerializedObject(footstep);
            serializedFootstep.FindProperty("cue").objectReferenceValue = cue;
            serializedFootstep.FindProperty("movementRoot").objectReferenceValue = movementRoot;
            serializedFootstep.FindProperty("emitter").objectReferenceValue = emitter;
            serializedFootstep.FindProperty("useProfileOverride").boolValue = true;
            serializedFootstep.FindProperty("profileOverride").enumValueIndex = (int)profile;
            serializedFootstep.FindProperty("requireActiveRide").boolValue = true;
            serializedFootstep.FindProperty("allowOverlap").boolValue = false;

            switch (profile)
            {
                case MountProfile.Horse:
                    serializedFootstep.FindProperty("minimumSpeed").floatValue = 0.12f;
                    serializedFootstep.FindProperty("walkStepInterval").floatValue = 0.46f;
                    serializedFootstep.FindProperty("runStepInterval").floatValue = 0.28f;
                    serializedFootstep.FindProperty("speedForRunInterval").floatValue = 4.8f;
                    serializedFootstep.FindProperty("volumeScale").floatValue = 0.78f;
                    serializedFootstep.FindProperty("footstepClipWindowSeconds").floatValue = 0.28f;
                    break;
                case MountProfile.Dog:
                    serializedFootstep.FindProperty("minimumSpeed").floatValue = 0.1f;
                    serializedFootstep.FindProperty("walkStepInterval").floatValue = 0.34f;
                    serializedFootstep.FindProperty("runStepInterval").floatValue = 0.19f;
                    serializedFootstep.FindProperty("speedForRunInterval").floatValue = 3.2f;
                    serializedFootstep.FindProperty("volumeScale").floatValue = 0.68f;
                    serializedFootstep.FindProperty("footstepClipWindowSeconds").floatValue = 0.16f;
                    break;
                default:
                    serializedFootstep.FindProperty("minimumSpeed").floatValue = 0.08f;
                    serializedFootstep.FindProperty("walkStepInterval").floatValue = 0.3f;
                    serializedFootstep.FindProperty("runStepInterval").floatValue = 0.17f;
                    serializedFootstep.FindProperty("speedForRunInterval").floatValue = 2.4f;
                    serializedFootstep.FindProperty("volumeScale").floatValue = 0.62f;
                    serializedFootstep.FindProperty("footstepClipWindowSeconds").floatValue = 0.12f;
                    break;
            }

            serializedFootstep.FindProperty("startupRandomDelay").floatValue = 0.18f;
            serializedFootstep.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(footstep);
        }

        private static void MigrateLegacyProximityVoicePlayers(CueSet cues)
        {
            List<MonoBehaviour> legacyPlayers = new List<MonoBehaviour>();
#if UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
            MonoBehaviour[] behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            MonoBehaviour[] behaviours = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>(true);
#endif
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour != null && behaviour.GetType().Name == "AnimalVoiceProximityPlayer")
                {
                    legacyPlayers.Add(behaviour);
                }
            }

            foreach (MonoBehaviour legacy in legacyPlayers)
            {
                AudioSource source = ReadLegacyAudioSource(legacy);
                WonderlandProximityLoop proximity = EnsureComponent<WonderlandProximityLoop>(legacy.gameObject);
                ConfigureProximity(proximity, source, ResolveVoiceCue(source, cues));
                UnityEngine.Object.DestroyImmediate(legacy, allowDestroyingAssets: true);
            }
        }

        private static AudioSource ReadLegacyAudioSource(MonoBehaviour legacy)
        {
            FieldInfo field =
                legacy.GetType().GetField("audioSource", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public) ??
                legacy.GetType().GetField("targetAudioSource", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            return field != null ? field.GetValue(legacy) as AudioSource : null;
        }

        private static WonderlandAudioCue ResolveVoiceCue(AudioSource source, CueSet cues)
        {
            if (source != null && source.clip != null)
            {
                string clipName = source.clip.name;
                if (clipName.IndexOf("dog", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return cues.DogVoice;
                }

                if (clipName.IndexOf("horse", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return cues.HorseVoice;
                }

                if (clipName.IndexOf("cat", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return cues.CatVoice;
                }
            }

            string path = source != null ? GetHierarchyPath(source.transform) : string.Empty;
            if (path.IndexOf("Dog", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return cues.DogVoice;
            }

            if (path.IndexOf("Horse", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return cues.HorseVoice;
            }

            return cues.CatVoice;
        }

        private static void ConfigureProximity(WonderlandProximityLoop proximity, AudioSource source, WonderlandAudioCue cue)
        {
            SerializedObject serializedProximity = new SerializedObject(proximity);
            serializedProximity.FindProperty("cue").objectReferenceValue = cue;
            serializedProximity.FindProperty("audioSource").objectReferenceValue = source;
            serializedProximity.FindProperty("requireCharacterController").boolValue = true;
            serializedProximity.FindProperty("stopOnExit").boolValue = true;
            serializedProximity.FindProperty("volumeScale").floatValue = 1f;
            serializedProximity.FindProperty("fadeInSeconds").floatValue = 0.08f;
            serializedProximity.FindProperty("fadeOutSeconds").floatValue = 0.12f;
            serializedProximity.FindProperty("logDebug").boolValue = false;
            serializedProximity.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(proximity);
        }

        private static void AssignAmbientLoop(WonderlandAmbientLoop loop, WonderlandAudioCue cue, float volumeScale, float fadeIn, float fadeOut)
        {
            SerializedObject serializedLoop = new SerializedObject(loop);
            serializedLoop.FindProperty("cue").objectReferenceValue = cue;
            serializedLoop.FindProperty("playOnEnable").boolValue = true;
            serializedLoop.FindProperty("volumeScale").floatValue = volumeScale;
            serializedLoop.FindProperty("fadeInSeconds").floatValue = fadeIn;
            serializedLoop.FindProperty("fadeOutSeconds").floatValue = fadeOut;
            serializedLoop.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(loop);
        }

        private static T EnsureComponent<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            if (component == null)
            {
                component = gameObject.AddComponent<T>();
            }

            return component;
        }

        private static Transform EnsureChild(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            if (child != null)
            {
                return child;
            }

            GameObject childObject = new GameObject(childName);
            child = childObject.transform;
            child.SetParent(parent, false);
            return child;
        }

        private static Transform FindChildContains(Transform root, string token)
        {
            if (root == null || string.IsNullOrEmpty(token))
            {
                return null;
            }

            if (root.name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
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

        private static string GetHierarchyPath(Transform transform)
        {
            if (transform == null)
            {
                return string.Empty;
            }

            string path = transform.name;
            Transform parent = transform.parent;
            while (parent != null)
            {
                path = $"{parent.name}/{path}";
                parent = parent.parent;
            }

            return path;
        }

        private static void EnsureFolder(string folderPath)
        {
            string[] parts = folderPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private enum MountProfile
        {
            Cat,
            Dog,
            Horse
        }

        private sealed class CueSet
        {
            public WonderlandAudioCue NightForest;
            public WonderlandAudioCue WaterfallLoop;
            public WonderlandAudioCue WaterfallDetail;
            public WonderlandAudioCue CatFootsteps;
            public WonderlandAudioCue DogFootsteps;
            public WonderlandAudioCue HorseFootsteps;
            public WonderlandAudioCue CatVoice;
            public WonderlandAudioCue DogVoice;
            public WonderlandAudioCue HorseVoice;
            public WonderlandAudioCue UiClick;
            public WonderlandAudioCue UiHover;
            public WonderlandAudioCue MountTransition;
            public WonderlandAudioCue ScaleShift;
            public WonderlandAudioCue FireworkLaunch;
            public WonderlandAudioCue FireworkBurst;
            public WonderlandAudioCue CrystalSelect;
            public WonderlandAudioCue CrystalCollapse;
            public WonderlandAudioCue GrowthRustle;
            public WonderlandAudioCue MagicCollect;
            public WonderlandAudioCue MagicRelease;
            public WonderlandAudioCue MagicChargedRelease;
        }
    }
}
