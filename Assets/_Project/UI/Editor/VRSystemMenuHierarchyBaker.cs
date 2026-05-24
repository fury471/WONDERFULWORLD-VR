using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Wonderland.UI.Editor
{
    public static class VRSystemMenuHierarchyBaker
    {
        private const string PrefabPath = "Assets/_Project/UI/Prefabs/WW_VRSystemMenu.prefab";
        private const string MixerPath = "Assets/_Project/Audio/Mixers/WW_AudioMixer.mixer";
        private const string MainScenePath = "Assets/_Project/World/Persistent/World_WonderlandPark.unity";

        [MenuItem("Wonderful World/UI/Bake VR System Menu Hierarchy")]
        public static void BakeDefaultPrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Bake(root);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [MenuItem("Wonderful World/UI/Merge Existing WW_UI_System Menu")]
        public static void MergeMainSceneMenu()
        {
            Scene scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            int bakedCount = BakeSceneMenus();
            if (bakedCount > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }

        [MenuItem("Wonderful World/UI/Merge Open Scene Menus")]
        public static void MergeOpenSceneMenus()
        {
            int bakedCount = BakeSceneMenus();
            if (bakedCount > 0)
            {
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }
        }

        public static int BakeSceneMenus()
        {
            int bakedCount = 0;
#if UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
            VRSystemMenuController[] menus = Object.FindObjectsByType<VRSystemMenuController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
#pragma warning disable CS0618
            VRSystemMenuController[] menus = Object.FindObjectsOfType<VRSystemMenuController>(true);
#pragma warning restore CS0618
#endif
            for (int i = 0; i < menus.Length; i++)
            {
                VRSystemMenuController menu = menus[i];
                if (menu == null || EditorUtility.IsPersistent(menu))
                {
                    continue;
                }

                Bake(menu.gameObject);
                PrefabUtility.RecordPrefabInstancePropertyModifications(menu.gameObject);
                EditorUtility.SetDirty(menu.gameObject);
                bakedCount++;
            }

            if (bakedCount == 0)
            {
                Debug.LogWarning("[VRSystemMenuHierarchyBaker] No VRSystemMenuController found in the open scene.");
            }
            else
            {
                Debug.Log($"[VRSystemMenuHierarchyBaker] Merged {bakedCount} existing scene menu(s).");
            }

            return bakedCount;
        }

        public static void Bake(GameObject root)
        {
            VRSystemMenuController systemMenu = root.GetComponent<VRSystemMenuController>();
            Transform mainPanel = root.transform.Find("MainPanel");
            Transform settingsPanel = root.transform.Find("SettingsPanel");
            if (systemMenu == null || mainPanel == null || settingsPanel == null)
            {
                Debug.LogWarning("[VRSystemMenuHierarchyBaker] WW_VRSystemMenu is missing expected root objects.");
                return;
            }

            DestroyChild(settingsPanel, "SettingsPage");
            DestroyChild(settingsPanel, "LanguagePage");

            RectTransform settingsPage = CreatePage("SettingsPage", settingsPanel);
            RectTransform languagePage = CreatePage("LanguagePage", settingsPanel);
            languagePage.gameObject.SetActive(false);

            BuildSettingsPage(settingsPage);
            BuildLanguagePage(languagePage);
            RemoveLegacySettingsChildren(settingsPanel, settingsPage, languagePage);
            Button restartButton = EnsureMainRestartButton(mainPanel);

            VRSettingsMenuView view = settingsPanel.GetComponent<VRSettingsMenuView>();
            if (view == null)
            {
                view = settingsPanel.gameObject.AddComponent<VRSettingsMenuView>();
            }

            SerializedObject viewObject = new SerializedObject(view);
            Set(viewObject, "audioMixer", AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath));
            Set(viewObject, "systemMenu", systemMenu);
            Set(viewObject, "buildFallbackHierarchyAtRuntime", false);
            Set(viewObject, "settingsPage", settingsPage);
            Set(viewObject, "languagePage", languagePage);
            Set(viewObject, "masterSlider", Find<Slider>(settingsPage, "MasterAudioSlider"));
            Set(viewObject, "teleportButton", Find<Button>(settingsPage, "TeleportButton"));
            Set(viewObject, "continuousMoveButton", Find<Button>(settingsPage, "ContinuousMoveButton"));
            Set(viewObject, "snapTurnButton", Find<Button>(settingsPage, "SnapTurnButton"));
            Set(viewObject, "continuousTurnButton", Find<Button>(settingsPage, "ContinuousTurnButton"));
            Set(viewObject, "vignetteOffButton", Find<Button>(settingsPage, "VignetteOffButton"));
            Set(viewObject, "vignetteLowButton", Find<Button>(settingsPage, "VignetteLowButton"));
            Set(viewObject, "vignetteMediumButton", Find<Button>(settingsPage, "VignetteMediumButton"));
            Set(viewObject, "vignetteHighButton", Find<Button>(settingsPage, "VignetteHighButton"));
            Set(viewObject, "languageButton", Find<Button>(settingsPage, "LanguageButton"));
            Set(viewObject, "settingsBackButton", Find<Button>(settingsPage, "SettingsBackButton"));
            Set(viewObject, "settingsCancelButton", Find<Button>(settingsPage, "SettingsCancelButton"));
            Set(viewObject, "languageBackButton", Find<Button>(languagePage, "LanguageBackButton"));
            Set(viewObject, "languageCancelButton", Find<Button>(languagePage, "LanguageCancelButton"));
            Set(viewObject, "englishButton", Find<Button>(languagePage, "EnglishButton"));
            Set(viewObject, "chineseButton", Find<Button>(languagePage, "ChineseButton"));
            Set(viewObject, "swedishButton", Find<Button>(languagePage, "SwedishButton"));
            Set(viewObject, "englishLabel", Find<TMP_Text>(languagePage, "EnglishLabel"));
            Set(viewObject, "chineseLabel", Find<TMP_Text>(languagePage, "ChineseLabel"));
            Set(viewObject, "swedishLabel", Find<TMP_Text>(languagePage, "SwedishLabel"));
            viewObject.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject menuObject = new SerializedObject(systemMenu);
            Set(menuObject, "restartButton", restartButton);
            Set(menuObject, "backButton", Find<Button>(settingsPage, "SettingsBackButton"));
            Set(menuObject, "distanceFromCamera", 1.3f);
            Set(menuObject, "cameraLocalOffset", new Vector3(0f, -0.12f, 0f));
            Set(menuObject, "worldScale", new Vector3(0.0015f, 0.0015f, 0.0015f));
            menuObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildSettingsPage(RectTransform page)
        {
            AddText(page, "SettingsTitle", "Settings", "\u8bbe\u7f6e", "Inst\u00e4llningar", 34f, new Vector2(-250f, 178f), new Vector2(220f, 50f), TextAlignmentOptions.Left);
            AddText(page, "MasterAudioLabel", "Master Audio", "\u4e3b\u97f3\u91cf", "Huvudvolym", 21f, new Vector2(-230f, 118f), new Vector2(250f, 36f), TextAlignmentOptions.Left);
            AddSlider(page, "MasterAudioSlider", new Vector2(130f, 118f), new Vector2(360f, 36f));

            AddText(page, "LocomotionLabel", "Locomotion", "\u79fb\u52a8", "R\u00f6relse", 21f, new Vector2(-230f, 58f), new Vector2(250f, 36f), TextAlignmentOptions.Left);
            AddButton(page, "TeleportButton", "Teleport", "\u4f20\u9001", "Teleport", new Vector2(50f, 58f), new Vector2(155f, 42f));
            AddButton(page, "ContinuousMoveButton", "Continuous", "\u8fde\u7eed", "Kontinuerlig", new Vector2(222f, 58f), new Vector2(170f, 42f));

            AddText(page, "TurnLabel", "Turn", "\u8f6c\u5411", "Vridning", 21f, new Vector2(-230f, 2f), new Vector2(250f, 36f), TextAlignmentOptions.Left);
            AddButton(page, "SnapTurnButton", "Snap", "\u77ac\u8f6c", "Sn\u00e4pp", new Vector2(50f, 2f), new Vector2(155f, 42f));
            AddButton(page, "ContinuousTurnButton", "Continuous", "\u8fde\u7eed", "Kontinuerlig", new Vector2(222f, 2f), new Vector2(170f, 42f));

            AddText(page, "VignetteLabel", "Comfort Vignette", "\u8212\u9002\u6655\u5f71", "Vinjettering", 21f, new Vector2(-210f, -56f), new Vector2(290f, 36f), TextAlignmentOptions.Left);
            AddButton(page, "VignetteOffButton", "Off", "\u5173", "Av", new Vector2(42f, -56f), new Vector2(82f, 40f));
            AddButton(page, "VignetteLowButton", "Low", "\u4f4e", "L\u00e5g", new Vector2(130f, -56f), new Vector2(82f, 40f));
            AddButton(page, "VignetteMediumButton", "Med", "\u4e2d", "Med", new Vector2(218f, -56f), new Vector2(82f, 40f));
            AddButton(page, "VignetteHighButton", "High", "\u9ad8", "H\u00f6g", new Vector2(306f, -56f), new Vector2(82f, 40f));

            AddButton(page, "LanguageButton", "Language", "\u8bed\u8a00", "Spr\u00e5k", new Vector2(80f, -122f), new Vector2(440f, 46f));
            AddButton(page, "SettingsBackButton", "Back", "\u8fd4\u56de", "Tillbaka", new Vector2(80f, -176f), new Vector2(210f, 46f));
            AddButton(page, "SettingsCancelButton", "Cancel", "\u53d6\u6d88", "Avbryt", new Vector2(310f, -176f), new Vector2(210f, 46f));
        }

        private static void BuildLanguagePage(RectTransform page)
        {
            AddText(page, "LanguageTitle", "Language", "\u8bed\u8a00", "Spr\u00e5k", 34f, new Vector2(-250f, 170f), new Vector2(220f, 50f), TextAlignmentOptions.Left);
            AddButton(page, "EnglishButton", "English", "\u82f1\u8bed", "Engelska", new Vector2(80f, 84f), new Vector2(440f, 54f), out _);
            AddButton(page, "ChineseButton", "Chinese", "\u4e2d\u6587", "Kinesiska", new Vector2(80f, 18f), new Vector2(440f, 54f), out _);
            AddButton(page, "SwedishButton", "Svenska", "\u745e\u5178\u8bed", "Svenska", new Vector2(80f, -48f), new Vector2(440f, 54f), out _);
            AddButton(page, "LanguageBackButton", "Back", "\u8fd4\u56de", "Tillbaka", new Vector2(80f, -152f), new Vector2(210f, 48f));
            AddButton(page, "LanguageCancelButton", "Cancel", "\u53d6\u6d88", "Avbryt", new Vector2(310f, -152f), new Vector2(210f, 48f));
        }

        private static Button EnsureMainRestartButton(Transform mainPanel)
        {
            Button existing = Find<Button>(mainPanel, "RestartButton");
            if (existing != null)
            {
                return existing;
            }

            RectTransform exit = Find<RectTransform>(mainPanel, "ExitButton");
            if (exit != null)
            {
                exit.anchoredPosition = new Vector2(0f, -156f);
            }

            return AddButton((RectTransform)mainPanel, "RestartButton", "Restart", "\u91cd\u542f", "Starta om", new Vector2(0f, -88f), new Vector2(360f, 58f));
        }

        private static RectTransform CreatePage(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(640f, 420f);
            return rect;
        }

        private static TMP_Text AddText(RectTransform parent, string name, string english, string chinese, string swedish, float fontSize, Vector2 position, Vector2 size, TextAlignmentOptions alignment)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            TMP_Text text = go.GetComponent<TMP_Text>();
            text.text = english;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;

            LocalizedUIText localized = go.AddComponent<LocalizedUIText>();
            localized.SetTexts(english, chinese, swedish);
            return text;
        }

        private static Button AddButton(RectTransform parent, string name, string english, string chinese, string swedish, Vector2 position, Vector2 size)
        {
            return AddButton(parent, name, english, chinese, swedish, position, size, out _);
        }

        private static Button AddButton(RectTransform parent, string name, string english, string chinese, string swedish, Vector2 position, Vector2 size, out TMP_Text label)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image image = go.GetComponent<Image>();
            image.color = new Color(0.92f, 0.95f, 0.94f, 1f);

            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;

            label = AddText(rect, name.Replace("Button", "Label"), english, chinese, swedish, 23f, Vector2.zero, size, TextAlignmentOptions.Center);
            label.color = new Color(0.08f, 0.12f, 0.14f, 1f);
            label.enableAutoSizing = true;
            label.fontSizeMin = 16f;
            label.fontSizeMax = 23f;
            return button;
        }

        private static Slider AddSlider(RectTransform parent, string name, Vector2 position, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Slider));
            RectTransform root = go.GetComponent<RectTransform>();
            root.SetParent(parent, false);
            root.anchoredPosition = position;
            root.sizeDelta = size;

            Image background = CreateImage("Background", root, new Color(0.2f, 0.24f, 0.27f, 0.95f));
            Stretch(background.rectTransform, new Vector2(0f, 0.36f), new Vector2(1f, 0.64f));

            RectTransform fillArea = CreateRect("Fill Area", root);
            Stretch(fillArea, new Vector2(0f, 0.36f), new Vector2(1f, 0.64f));
            Image fill = CreateImage("Fill", fillArea, new Color(0.18f, 0.58f, 0.72f, 1f));
            Stretch(fill.rectTransform, Vector2.zero, Vector2.one);

            RectTransform handleArea = CreateRect("Handle Slide Area", root);
            Stretch(handleArea, Vector2.zero, Vector2.one);
            Image handle = CreateImage("Handle", handleArea, Color.white);
            handle.rectTransform.sizeDelta = new Vector2(24f, 24f);

            Slider slider = go.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.targetGraphic = handle;
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            return slider;
        }

        private static Image CreateImage(string name, Transform parent, Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void DestroyChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null)
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }

        private static void RemoveLegacySettingsChildren(Transform settingsPanel, RectTransform settingsPage, RectTransform languagePage)
        {
            for (int i = settingsPanel.childCount - 1; i >= 0; i--)
            {
                Transform child = settingsPanel.GetChild(i);
                if (child == settingsPage || child == languagePage)
                {
                    continue;
                }

                Object.DestroyImmediate(child.gameObject);
            }
        }

        private static T Find<T>(Transform root, string name) where T : Component
        {
            T[] components = root.GetComponentsInChildren<T>(true);
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] != null && components[i].name == name)
                {
                    return components[i];
                }
            }

            return null;
        }

        private static void Set(SerializedObject serializedObject, string propertyName, Object value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = value;
            }
        }

        private static void Set(SerializedObject serializedObject, string propertyName, bool value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.boolValue = value;
            }
        }

        private static void Set(SerializedObject serializedObject, string propertyName, float value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.floatValue = value;
            }
        }

        private static void Set(SerializedObject serializedObject, string propertyName, Vector3 value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.vector3Value = value;
            }
        }
    }
}
