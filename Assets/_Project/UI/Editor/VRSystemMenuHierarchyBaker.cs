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
            DestroyChild(root.transform, "TutorialPanel");

            RectTransform settingsPage = CreatePage("SettingsPage", settingsPanel);
            RectTransform languagePage = CreatePage("LanguagePage", settingsPanel);
            languagePage.gameObject.SetActive(false);
            RectTransform tutorialPanel = CreateMenuPanel("TutorialPanel", root.transform);
            tutorialPanel.gameObject.SetActive(false);

            BuildSettingsPage(settingsPage);
            BuildLanguagePage(languagePage);
            BuildTutorialPanel(tutorialPanel, systemMenu);
            RemoveLegacySettingsChildren(settingsPanel, settingsPage, languagePage);
            Button tutorialButton = EnsureMainButtonLayout(mainPanel, out Button restartButton);

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
            Set(menuObject, "tutorialButton", tutorialButton);
            Set(menuObject, "tutorialPanel", tutorialPanel.gameObject);
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

        private static void BuildTutorialPanel(RectTransform panel, VRSystemMenuController systemMenu)
        {
            TMP_Text title = AddRawText(panel, "TutorialTitle", "Tutorial", 34f, new Vector2(-250f, 170f), new Vector2(240f, 50f), TextAlignmentOptions.Left, Color.white);
            TMP_Text counter = AddRawText(panel, "TutorialPageCounter", "1/7", 21f, new Vector2(260f, 170f), new Vector2(100f, 36f), TextAlignmentOptions.Right, new Color(0.82f, 0.88f, 0.86f, 1f));
            TMP_Text body = AddRawText(panel, "TutorialBody", "Use Previous and Next to browse park basics.", 22f, new Vector2(0f, 34f), new Vector2(560f, 220f), TextAlignmentOptions.TopLeft, Color.white);
            body.textWrappingMode = TextWrappingModes.Normal;
            body.enableAutoSizing = true;
            body.fontSizeMin = 16f;
            body.fontSizeMax = 22f;

            Button previousButton = AddButton(panel, "TutorialPreviousButton", "Previous", "\u4e0a\u4e00\u9875", "F\u00f6reg\u00e5ende", new Vector2(-142f, -108f), new Vector2(210f, 46f));
            Button nextButton = AddButton(panel, "TutorialNextButton", "Next", "\u4e0b\u4e00\u9875", "N\u00e4sta", new Vector2(142f, -108f), new Vector2(210f, 46f));
            Button backButton = AddButton(panel, "TutorialBackButton", "Back", "\u8fd4\u56de", "Tillbaka", new Vector2(80f, -176f), new Vector2(210f, 46f));
            Button cancelButton = AddButton(panel, "TutorialCancelButton", "Close", "\u5173\u95ed", "St\u00e4ng", new Vector2(310f, -176f), new Vector2(210f, 46f));

            VRTutorialMenuView view = panel.GetComponent<VRTutorialMenuView>();
            if (view == null)
            {
                view = panel.gameObject.AddComponent<VRTutorialMenuView>();
            }

            SerializedObject viewObject = new SerializedObject(view);
            Set(viewObject, "systemMenu", systemMenu);
            Set(viewObject, "titleText", title);
            Set(viewObject, "bodyText", body);
            Set(viewObject, "pageCounterText", counter);
            Set(viewObject, "previousButton", previousButton);
            Set(viewObject, "nextButton", nextButton);
            Set(viewObject, "backButton", backButton);
            Set(viewObject, "cancelButton", cancelButton);
            SetTutorialPages(viewObject);
            viewObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Button EnsureMainButtonLayout(Transform mainPanel, out Button restartButton)
        {
            Button settingsButton = Find<Button>(mainPanel, "SettingsButton");
            Button tutorialButton = Find<Button>(mainPanel, "TutorialButton");
            if (tutorialButton == null)
            {
                tutorialButton = AddButton((RectTransform)mainPanel, "TutorialButton", "Tutorial", "\u6559\u7a0b", "Tutorial", new Vector2(0f, 18f), new Vector2(360f, 58f));
            }

            restartButton = EnsureMainRestartButton(mainPanel);
            Button cancelButton = Find<Button>(mainPanel, "CancelButton");
            Button exitButton = Find<Button>(mainPanel, "ExitButton");

            SetButtonPosition(settingsButton, new Vector2(0f, 92f), new Vector2(360f, 58f));
            SetButtonPosition(tutorialButton, new Vector2(0f, 22f), new Vector2(360f, 58f));
            SetButtonPosition(restartButton, new Vector2(0f, -48f), new Vector2(360f, 58f));
            SetButtonPosition(cancelButton, new Vector2(0f, -118f), new Vector2(360f, 58f));
            SetButtonPosition(exitButton, new Vector2(0f, -188f), new Vector2(360f, 58f));
            return tutorialButton;
        }

        private static Button EnsureMainRestartButton(Transform mainPanel)
        {
            Button existing = Find<Button>(mainPanel, "RestartButton");
            if (existing != null)
            {
                return existing;
            }

            existing = Find<Button>(mainPanel, "Button_Restart");
            if (existing != null)
            {
                return existing;
            }

            Debug.LogWarning("[VRSystemMenuHierarchyBaker] Existing restart button was not found. Leaving restartButton unassigned instead of creating a duplicate.");
            return null;
        }

        private static void SetButtonPosition(Button button, Vector2 position, Vector2 size)
        {
            if (button == null)
            {
                return;
            }

            RectTransform rect = button.GetComponent<RectTransform>();
            if (rect == null)
            {
                return;
            }

            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.rectTransform.sizeDelta = size;
            }
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

        private static RectTransform CreateMenuPanel(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(720f, 480f);
            return rect;
        }

        private static TMP_Text AddRawText(RectTransform parent, string name, string value, float fontSize, Vector2 position, Vector2 size, TextAlignmentOptions alignment, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            TMP_Text text = go.GetComponent<TMP_Text>();
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            return text;
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

        private static void SetTutorialPages(SerializedObject viewObject)
        {
            SerializedProperty pages = viewObject.FindProperty("pages");
            if (pages == null)
            {
                return;
            }

            string[,] content =
            {
                {
                    "Quick Start",
                    "Move with the left stick. Turn with the right stick. Hold right B to recenter. Use the right stick button to change scale.",
                    "\u5feb\u901f\u5f00\u59cb",
                    "\u5de6\u6447\u6746\u79fb\u52a8\u3002\u53f3\u6447\u6746\u8f6c\u5411\u3002\u957f\u6309\u53f3\u624b B \u952e\u91cd\u65b0\u5bf9\u6b63\u3002\u7528\u53f3\u6447\u6746\u6309\u952e\u5207\u6362\u7f29\u653e\u3002",
                    "Snabbstart",
                    "R\u00f6r dig med v\u00e4nster spak. Vrid med h\u00f6ger spak. H\u00e5ll h\u00f6ger B f\u00f6r att centrera. Anv\u00e4nd h\u00f6ger spakknapp f\u00f6r skala."
                },
                {
                    "Notice Boards",
                    "Every park area has a notice board. Point the right controller at the board and press the right index trigger to open local story, controls, and hints.",
                    "\u516c\u544a\u724c",
                    "\u6bcf\u4e2a\u56ed\u533a\u90fd\u6709\u516c\u544a\u724c\u3002\u7528\u53f3\u624b\u63a7\u5236\u5668\u6307\u5411\u516c\u544a\u724c\uff0c\u6309\u53f3\u624b\u98df\u6307\u952e\u6253\u5f00\u672c\u533a\u57df\u7684\u6545\u4e8b\u3001\u64cd\u4f5c\u548c\u63d0\u793a\u3002",
                    "Anslagstavlor",
                    "Varje omr\u00e5de har en anslagstavla. Peka med h\u00f6ger kontroll och tryck h\u00f6ger avtryckare f\u00f6r lokal ber\u00e4ttelse, kontroller och tips."
                },
                {
                    "Human Entry",
                    "Start here when you need orientation. The board summarizes basic movement, comfort, and where to go next.",
                    "\u4eba\u7c7b\u5165\u53e3",
                    "\u5982\u679c\u9700\u8981\u786e\u8ba4\u65b9\u5411\uff0c\u4ece\u8fd9\u91cc\u5f00\u59cb\u3002\u516c\u544a\u724c\u4f1a\u6c47\u603b\u57fa\u7840\u79fb\u52a8\u3001\u8212\u9002\u8bbe\u7f6e\u548c\u4e0b\u4e00\u6b65\u53bb\u54ea\u91cc\u3002",
                    "M\u00e4nsklig entr\u00e9",
                    "B\u00f6rja h\u00e4r n\u00e4r du vill orientera dig. Tavlan sammanfattar r\u00f6relse, komfort och n\u00e4sta plats."
                },
                {
                    "Flower Field",
                    "Use the right index trigger on flowers, butterflies, and the board. This area is best explored slowly and up close.",
                    "\u82b1\u7530",
                    "\u5bf9\u82b1\u6735\u3001\u8774\u8776\u548c\u516c\u544a\u724c\u6309\u53f3\u624b\u98df\u6307\u952e\u4e92\u52a8\u3002\u8fd9\u4e2a\u533a\u57df\u9002\u5408\u6162\u6162\u770b\u3001\u8d70\u8fd1\u770b\u3002",
                    "Blomster\u00e4ng",
                    "Anv\u00e4nd h\u00f6ger avtryckare p\u00e5 blommor, fj\u00e4rilar och tavlan. Utforska l\u00e5ngsamt och n\u00e4ra."
                },
                {
                    "Lotus Pond",
                    "Look for quiet interaction points around the water. The board explains the pond activity and audio cues.",
                    "\u8377\u82b1\u6c60",
                    "\u5728\u6c34\u8fb9\u5bfb\u627e\u5b89\u9759\u7684\u4e92\u52a8\u70b9\u3002\u516c\u544a\u724c\u4f1a\u8bf4\u660e\u8377\u82b1\u6c60\u7684\u4f53\u9a8c\u548c\u58f0\u97f3\u63d0\u793a\u3002",
                    "Lotusdamm",
                    "Leta efter lugna interaktioner vid vattnet. Tavlan f\u00f6rklarar dammens aktivitet och ljudsignaler."
                },
                {
                    "Cat Route",
                    "Read the board before riding. It explains mount controls, comfort expectations, and how to stop safely.",
                    "\u732b\u8def\u7ebf",
                    "\u9a91\u4e58\u524d\u5148\u9605\u8bfb\u516c\u544a\u724c\u3002\u5b83\u4f1a\u8bf4\u660e\u5750\u9a91\u64cd\u4f5c\u3001\u8212\u9002\u9884\u671f\u548c\u5b89\u5168\u505c\u4e0b\u7684\u65b9\u6cd5\u3002",
                    "Kattrutt",
                    "L\u00e4s tavlan f\u00f6re ridning. Den f\u00f6rklarar kontroller, komfort och hur du stannar s\u00e4kert."
                },
                {
                    "Fireworks Clearing",
                    "The board gives timing and interaction notes. Keep the menu button in mind if you want to adjust comfort or audio.",
                    "\u70df\u82b1\u7a7a\u5730",
                    "\u516c\u544a\u724c\u4f1a\u63d0\u4f9b\u65f6\u673a\u548c\u4e92\u52a8\u8bf4\u660e\u3002\u5982\u679c\u8981\u8c03\u6574\u8212\u9002\u5ea6\u6216\u97f3\u91cf\uff0c\u8bb0\u5f97\u6309\u83dc\u5355\u952e\u3002",
                    "Fyrverkerigl\u00e4nta",
                    "Tavlan ger timing och interaktionstips. Anv\u00e4nd menyknappen om du vill justera komfort eller ljud."
                }
            };

            pages.arraySize = content.GetLength(0);
            for (int i = 0; i < content.GetLength(0); i++)
            {
                SerializedProperty page = pages.GetArrayElementAtIndex(i);
                page.FindPropertyRelative("englishTitle").stringValue = content[i, 0];
                page.FindPropertyRelative("englishBody").stringValue = content[i, 1];
                page.FindPropertyRelative("chineseTitle").stringValue = content[i, 2];
                page.FindPropertyRelative("chineseBody").stringValue = content[i, 3];
                page.FindPropertyRelative("swedishTitle").stringValue = content[i, 4];
                page.FindPropertyRelative("swedishBody").stringValue = content[i, 5];
            }
        }
    }
}
