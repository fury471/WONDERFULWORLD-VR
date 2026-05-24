using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using WonderfulWorld.Audio;

#pragma warning disable 0649

namespace Wonderland.UI
{
    [DisallowMultipleComponent]
    public sealed class VRSettingsMenuView : MonoBehaviour
    {
        public const string LocomotionModePrefKey = "WW.Settings.LocomotionMode";
        public const string TurnModePrefKey = "WW.Settings.TurnMode";
        public const string VignetteLevelPrefKey = "WW.Settings.ComfortVignetteLevel";

        private const string RuntimeRootName = "WW_RuntimeSettingsRoot";
        private const float ButtonHeight = 34f;
        private const float SliderHeight = 28f;
        private const float LayoutSpacing = 6f;

        [Header("Runtime Wiring")]
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private QuestLocomotionComfortProfile locomotionProfile;
        [SerializeField] private VRSystemMenuController systemMenu;
        [SerializeField] private bool buildFallbackHierarchyAtRuntime = true;

        [Header("Hierarchy Pages")]
        [SerializeField] private RectTransform settingsPage;
        [SerializeField] private RectTransform languagePage;

        [Header("Hierarchy Controls")]
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Button teleportButton;
        [SerializeField] private Button continuousMoveButton;
        [SerializeField] private Button snapTurnButton;
        [SerializeField] private Button continuousTurnButton;
        [SerializeField] private Button vignetteOffButton;
        [SerializeField] private Button vignetteLowButton;
        [SerializeField] private Button vignetteMediumButton;
        [SerializeField] private Button vignetteHighButton;
        [SerializeField] private Button languageButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button settingsBackButton;
        [SerializeField] private Button settingsCancelButton;
        [SerializeField] private Button languageBackButton;
        [SerializeField] private Button languageCancelButton;

        [Header("Legacy Language Buttons")]
        [SerializeField] private Button englishButton;
        [SerializeField] private Button chineseButton;
        [SerializeField] private Button swedishButton;

        [Header("Legacy Button Labels")]
        [SerializeField] private TMP_Text englishLabel;
        [SerializeField] private TMP_Text chineseLabel;
        [SerializeField] private TMP_Text swedishLabel;

        [Header("Visual State")]
        [SerializeField] private Color selectedColor = new Color(0.18f, 0.58f, 0.72f, 1f);
        [SerializeField] private Color normalColor = new Color(0.92f, 0.95f, 0.94f, 1f);
        [SerializeField] private Color selectedTextColor = Color.white;
        [SerializeField] private Color normalTextColor = new Color(0.08f, 0.12f, 0.14f, 1f);

        private Button runtimeEnglishButton;
        private Button runtimeChineseButton;
        private Button runtimeSwedishButton;
        private TMP_Text runtimeEnglishLabel;
        private TMP_Text runtimeChineseLabel;
        private TMP_Text runtimeSwedishLabel;
        private bool suppressSliderEvents;
        private bool controlsWired;

        private void Awake()
        {
            ResolveRuntimeReferences();
            WonderlandAudioBus.SetMixer(audioMixer);
            BindHierarchyView();
            if (settingsPage == null && buildFallbackHierarchyAtRuntime)
            {
                BuildRuntimeView();
                BindHierarchyView();
            }

            WireLegacyLanguageButtons();
            WireHierarchyControls();
        }

        private void OnEnable()
        {
            UILanguageService.LanguageChanged += RefreshLanguage;
            ShowSettingsPage();
        }

        private void Start()
        {
            ApplySavedPreferences();
            ShowSettingsPage();
        }

        private void OnDisable()
        {
            UILanguageService.LanguageChanged -= RefreshLanguage;
        }

        public void SetEnglish()
        {
            SetLanguage(UILanguage.English);
        }

        public void SetChinese()
        {
            SetLanguage(UILanguage.ChineseSimplified);
        }

        public void SetSwedish()
        {
            SetLanguage(UILanguage.Swedish);
        }

        public void ShowSettingsPage()
        {
            if (settingsPage != null)
            {
                settingsPage.gameObject.SetActive(true);
            }

            if (languagePage != null)
            {
                languagePage.gameObject.SetActive(false);
            }

            RefreshState();
        }

        public void ShowLanguagePage()
        {
            if (settingsPage != null)
            {
                settingsPage.gameObject.SetActive(false);
            }

            if (languagePage != null)
            {
                languagePage.gameObject.SetActive(true);
            }

            RefreshLanguage(UILanguageService.GetCurrentOrDefault());
        }

        public void Back()
        {
            if (languagePage != null && languagePage.gameObject.activeSelf)
            {
                ShowSettingsPage();
                return;
            }

            ResolveRuntimeReferences();
            if (systemMenu != null)
            {
                systemMenu.ShowMainPanel();
            }
        }

        public void Cancel()
        {
            ResolveRuntimeReferences();
            if (systemMenu != null)
            {
                systemMenu.CloseMenu();
            }
        }

        public void Restart()
        {
            ResolveRuntimeReferences();
            if (systemMenu != null)
            {
                systemMenu.RestartCurrentScene();
            }
        }

        public void OnMasterVolumeChanged(float value)
        {
            if (!suppressSliderEvents)
            {
                WonderlandAudioBus.SetMasterVolume(value, save: true);
            }
        }

        public void SelectTeleport() => SetMovementMode(QuestLocomotionComfortProfile.MovementMode.Teleport);
        public void SelectContinuousMove() => SetMovementMode(QuestLocomotionComfortProfile.MovementMode.Smooth);
        public void SelectSnapTurn() => SetTurnMode(QuestLocomotionComfortProfile.TurnMode.Snap);
        public void SelectContinuousTurn() => SetTurnMode(QuestLocomotionComfortProfile.TurnMode.Smooth);
        public void SetVignetteOff() => SetVignetteLevel(0);
        public void SetVignetteLow() => SetVignetteLevel(1);
        public void SetVignetteMedium() => SetVignetteLevel(2);
        public void SetVignetteHigh() => SetVignetteLevel(3);

        private void ResolveRuntimeReferences()
        {
            if (systemMenu == null)
            {
                systemMenu = GetComponentInParent<VRSystemMenuController>(true);
            }

            if (locomotionProfile == null)
            {
#if UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
                locomotionProfile = FindFirstObjectByType<QuestLocomotionComfortProfile>(FindObjectsInactive.Include);
#else
#pragma warning disable CS0618
                locomotionProfile = FindObjectOfType<QuestLocomotionComfortProfile>(true);
#pragma warning restore CS0618
#endif
            }
        }

        private void BindHierarchyView()
        {
            settingsPage = settingsPage != null ? settingsPage : FindRect("SettingsPage", "PageSettings");
            languagePage = languagePage != null ? languagePage : FindRect("LanguagePage", "PageLanguage");
            masterSlider = masterSlider != null ? masterSlider : FindComponent<Slider>("MasterAudioSlider", "MasterSlider", "Slider_MasterAudio");
            teleportButton = teleportButton != null ? teleportButton : FindComponent<Button>("TeleportButton", "Button_Teleport");
            continuousMoveButton = continuousMoveButton != null ? continuousMoveButton : FindComponent<Button>("ContinuousMoveButton", "Button_ContinuousMove", "MoveContinuousButton");
            snapTurnButton = snapTurnButton != null ? snapTurnButton : FindComponent<Button>("SnapTurnButton", "Button_SnapTurn");
            continuousTurnButton = continuousTurnButton != null ? continuousTurnButton : FindComponent<Button>("ContinuousTurnButton", "Button_ContinuousTurn", "TurnContinuousButton");
            vignetteOffButton = vignetteOffButton != null ? vignetteOffButton : FindComponent<Button>("VignetteOffButton", "Button_VignetteOff");
            vignetteLowButton = vignetteLowButton != null ? vignetteLowButton : FindComponent<Button>("VignetteLowButton", "Button_VignetteLow");
            vignetteMediumButton = vignetteMediumButton != null ? vignetteMediumButton : FindComponent<Button>("VignetteMediumButton", "Button_VignetteMedium", "VignetteMedButton");
            vignetteHighButton = vignetteHighButton != null ? vignetteHighButton : FindComponent<Button>("VignetteHighButton", "Button_VignetteHigh");
            languageButton = languageButton != null ? languageButton : FindComponent<Button>("LanguageButton", "Button_Language");
            restartButton = restartButton != null ? restartButton : FindComponent<Button>("RestartButton", "Button_Restart");
            settingsBackButton = settingsBackButton != null ? settingsBackButton : FindComponent<Button>("SettingsBackButton", "BackButton", "Button_Back");
            settingsCancelButton = settingsCancelButton != null ? settingsCancelButton : FindComponent<Button>("SettingsCancelButton", "CancelButton", "Button_Cancel");
            languageBackButton = languageBackButton != null ? languageBackButton : FindComponent<Button>("LanguageBackButton");
            languageCancelButton = languageCancelButton != null ? languageCancelButton : FindComponent<Button>("LanguageCancelButton");
        }

        private void WireHierarchyControls()
        {
            if (controlsWired)
            {
                return;
            }

            Wire(masterSlider, OnMasterVolumeChanged);
            Wire(teleportButton, SelectTeleport);
            Wire(continuousMoveButton, SelectContinuousMove);
            Wire(snapTurnButton, SelectSnapTurn);
            Wire(continuousTurnButton, SelectContinuousTurn);
            Wire(vignetteOffButton, SetVignetteOff);
            Wire(vignetteLowButton, SetVignetteLow);
            Wire(vignetteMediumButton, SetVignetteMedium);
            Wire(vignetteHighButton, SetVignetteHigh);
            Wire(languageButton, ShowLanguagePage);
            Wire(restartButton, Restart);
            Wire(settingsBackButton, Back);
            Wire(settingsCancelButton, Cancel);
            Wire(languageBackButton, Back);
            Wire(languageCancelButton, Cancel);
            controlsWired = true;
        }

        private void BuildRuntimeView()
        {
            Transform existingRoot = transform.Find(RuntimeRootName);
            if (existingRoot != null)
            {
                settingsPage = existingRoot.Find("SettingsPage") as RectTransform;
                languagePage = existingRoot.Find("LanguagePage") as RectTransform;
                return;
            }

            HideLegacyChildren();

            RectTransform root = CreateRect(RuntimeRootName, transform);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = new Vector2(20f, 14f);
            root.offsetMax = new Vector2(-20f, -14f);

            settingsPage = CreatePage("SettingsPage", root);
            languagePage = CreatePage("LanguagePage", root);
            BuildSettingsPage(settingsPage);
            BuildLanguagePage(languagePage);
        }

        private void HideLegacyChildren()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child.name != RuntimeRootName)
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        private RectTransform CreatePage(string pageName, Transform parent)
        {
            RectTransform page = CreateRect(pageName, parent);
            page.anchorMin = Vector2.zero;
            page.anchorMax = Vector2.one;
            page.offsetMin = Vector2.zero;
            page.offsetMax = Vector2.zero;

            VerticalLayoutGroup layout = page.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = LayoutSpacing;
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return page;
        }

        private void BuildSettingsPage(Transform parent)
        {
            AddLocalizedLabel(parent, "Settings", "设置", "Inställningar", 22, TextAlignmentOptions.Left);

            AddLocalizedLabel(parent, "Master Audio", "主音量", "Huvudvolym", 14, TextAlignmentOptions.Left);
            masterSlider = AddSlider(parent, 0f, 1f, wholeNumbers: false);
            masterSlider.onValueChanged.AddListener(OnMasterVolumeChanged);

            AddLocalizedLabel(parent, "Locomotion", "移动", "Rörelse", 14, TextAlignmentOptions.Left);
            RectTransform moveRow = AddHorizontalRow(parent);
            teleportButton = AddLocalizedButton(moveRow, "Teleport", "传送", "Teleport", SelectTeleport);
            continuousMoveButton = AddLocalizedButton(moveRow, "Continuous", "连续", "Kontinuerlig", SelectContinuousMove);

            AddLocalizedLabel(parent, "Turn", "转向", "Vridning", 14, TextAlignmentOptions.Left);
            RectTransform turnRow = AddHorizontalRow(parent);
            snapTurnButton = AddLocalizedButton(turnRow, "Snap", "瞬转", "Snäpp", SelectSnapTurn);
            continuousTurnButton = AddLocalizedButton(turnRow, "Continuous", "连续", "Kontinuerlig", SelectContinuousTurn);

            AddLocalizedLabel(parent, "Comfort Vignette", "舒适晕影", "Vinjettering", 14, TextAlignmentOptions.Left);
            RectTransform vignetteRow = AddHorizontalRow(parent);
            vignetteOffButton = AddLocalizedButton(vignetteRow, "Off", "关", "Av", SetVignetteOff);
            vignetteLowButton = AddLocalizedButton(vignetteRow, "Low", "低", "Låg", SetVignetteLow);
            vignetteMediumButton = AddLocalizedButton(vignetteRow, "Med", "中", "Med", SetVignetteMedium);
            vignetteHighButton = AddLocalizedButton(vignetteRow, "High", "高", "Hög", SetVignetteHigh);

            AddFlexibleSpace(parent);
            RectTransform bottomRow = AddHorizontalRow(parent);
            languageButton = AddLocalizedButton(bottomRow, "Language", "语言", "Språk", ShowLanguagePage);
            restartButton = AddLocalizedButton(bottomRow, "Restart", "重启", "Starta om", Restart);
            settingsBackButton = AddLocalizedButton(bottomRow, "Back", "返回", "Tillbaka", Back);
            settingsCancelButton = AddLocalizedButton(bottomRow, "Cancel", "取消", "Avbryt", Cancel);
        }

        private void BuildLanguagePage(Transform parent)
        {
            AddLocalizedLabel(parent, "Language", "语言", "Språk", 22, TextAlignmentOptions.Left);
            runtimeEnglishButton = AddButton(parent, "English", SetEnglish, out runtimeEnglishLabel);
            runtimeChineseButton = AddButton(parent, "中文", SetChinese, out runtimeChineseLabel);
            runtimeSwedishButton = AddButton(parent, "Svenska", SetSwedish, out runtimeSwedishLabel);
            AddFlexibleSpace(parent);
            RectTransform bottomRow = AddHorizontalRow(parent);
            languageBackButton = AddLocalizedButton(bottomRow, "Back", "返回", "Tillbaka", Back);
            languageCancelButton = AddLocalizedButton(bottomRow, "Cancel", "取消", "Avbryt", Cancel);
        }

        private void ApplySavedPreferences()
        {
            ResolveRuntimeReferences();
            WonderlandAudioBus.SetMixer(audioMixer);
            WonderlandAudioBus.SetMasterVolume(WonderlandAudioBus.MasterVolume, save: false);

            if (locomotionProfile != null)
            {
                locomotionProfile.SetMovementMode((QuestLocomotionComfortProfile.MovementMode)
                    PlayerPrefs.GetInt(LocomotionModePrefKey, (int)locomotionProfile.CurrentMovementMode));
                locomotionProfile.SetTurnMode((QuestLocomotionComfortProfile.TurnMode)
                    PlayerPrefs.GetInt(TurnModePrefKey, (int)locomotionProfile.CurrentTurnMode));
                ApplyVignetteLevel(PlayerPrefs.GetInt(VignetteLevelPrefKey, locomotionProfile.ComfortVignetteEnabled ? 2 : 0));
            }

            RefreshState();
        }

        private void SetMovementMode(QuestLocomotionComfortProfile.MovementMode mode)
        {
            ResolveRuntimeReferences();
            if (locomotionProfile != null)
            {
                locomotionProfile.SetMovementMode(mode);
            }

            PlayerPrefs.SetInt(LocomotionModePrefKey, (int)mode);
            PlayerPrefs.Save();
            RefreshState();
        }

        private void SetTurnMode(QuestLocomotionComfortProfile.TurnMode mode)
        {
            ResolveRuntimeReferences();
            if (locomotionProfile != null)
            {
                locomotionProfile.SetTurnMode(mode);
            }

            PlayerPrefs.SetInt(TurnModePrefKey, (int)mode);
            PlayerPrefs.Save();
            RefreshState();
        }

        private void SetVignetteLevel(int level)
        {
            level = Mathf.Clamp(level, 0, 3);
            ApplyVignetteLevel(level);
            PlayerPrefs.SetInt(VignetteLevelPrefKey, level);
            PlayerPrefs.Save();
            RefreshState();
        }

        private void ApplyVignetteLevel(int level)
        {
            ResolveRuntimeReferences();
            if (locomotionProfile == null)
            {
                return;
            }

            if (level <= 0)
            {
                locomotionProfile.SetComfortVignetteEnabled(false);
                return;
            }

            locomotionProfile.SetComfortVignetteEnabled(true);
            locomotionProfile.SetVignetteComfort(level == 1 ? 0.2f : level == 2 ? 0.5f : 0.85f);
        }

        private void RefreshState()
        {
            suppressSliderEvents = true;
            if (masterSlider != null)
            {
                masterSlider.value = WonderlandAudioBus.MasterVolume;
            }

            int vignetteLevel = PlayerPrefs.GetInt(VignetteLevelPrefKey, locomotionProfile != null && locomotionProfile.ComfortVignetteEnabled ? 2 : 0);
            suppressSliderEvents = false;

            QuestLocomotionComfortProfile.MovementMode movementMode = locomotionProfile != null
                ? locomotionProfile.CurrentMovementMode
                : (QuestLocomotionComfortProfile.MovementMode)PlayerPrefs.GetInt(LocomotionModePrefKey, 0);
            QuestLocomotionComfortProfile.TurnMode turnMode = locomotionProfile != null
                ? locomotionProfile.CurrentTurnMode
                : (QuestLocomotionComfortProfile.TurnMode)PlayerPrefs.GetInt(TurnModePrefKey, 0);

            ApplyButtonState(teleportButton, null, movementMode == QuestLocomotionComfortProfile.MovementMode.Teleport);
            ApplyButtonState(continuousMoveButton, null, movementMode == QuestLocomotionComfortProfile.MovementMode.Smooth);
            ApplyButtonState(snapTurnButton, null, turnMode == QuestLocomotionComfortProfile.TurnMode.Snap);
            ApplyButtonState(continuousTurnButton, null, turnMode == QuestLocomotionComfortProfile.TurnMode.Smooth);
            ApplyButtonState(vignetteOffButton, null, vignetteLevel == 0);
            ApplyButtonState(vignetteLowButton, null, vignetteLevel == 1);
            ApplyButtonState(vignetteMediumButton, null, vignetteLevel == 2);
            ApplyButtonState(vignetteHighButton, null, vignetteLevel == 3);
            RefreshLanguage(UILanguageService.GetCurrentOrDefault());
        }

        private void WireLegacyLanguageButtons()
        {
            if (englishButton != null) englishButton.onClick.AddListener(SetEnglish);
            if (chineseButton != null) chineseButton.onClick.AddListener(SetChinese);
            if (swedishButton != null) swedishButton.onClick.AddListener(SetSwedish);
        }

        private static void SetLanguage(UILanguage language)
        {
            if (UILanguageService.Instance != null)
            {
                UILanguageService.Instance.SetLanguage(language);
            }
        }

        private void RefreshLanguage(UILanguage language)
        {
            ApplyButtonState(englishButton, englishLabel, language == UILanguage.English);
            ApplyButtonState(chineseButton, chineseLabel, language == UILanguage.ChineseSimplified);
            ApplyButtonState(swedishButton, swedishLabel, language == UILanguage.Swedish);
            ApplyButtonState(runtimeEnglishButton, runtimeEnglishLabel, language == UILanguage.English);
            ApplyButtonState(runtimeChineseButton, runtimeChineseLabel, language == UILanguage.ChineseSimplified);
            ApplyButtonState(runtimeSwedishButton, runtimeSwedishLabel, language == UILanguage.Swedish);
        }

        private void ApplyButtonState(Button button, TMP_Text label, bool selected)
        {
            if (button != null && button.targetGraphic != null)
            {
                button.targetGraphic.color = selected ? selectedColor : normalColor;
            }

            TMP_Text resolvedLabel = label != null ? label : button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;
            if (resolvedLabel != null)
            {
                resolvedLabel.color = selected ? selectedTextColor : normalTextColor;
            }
        }

        private TMP_Text AddLabel(Transform parent, string text, int size, TextAlignmentOptions alignment)
        {
            GameObject go = new GameObject(text.Replace(" ", string.Empty));
            go.transform.SetParent(parent, false);
            TMP_Text label = go.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.alignment = alignment;
            label.color = Color.white;
            label.enableWordWrapping = false;
            TMP_FontAsset font = LocalizedUIFontProvider.GetBestLocalizedFont();
            if (font != null)
            {
                label.font = font;
            }

            LayoutElement layout = go.AddComponent<LayoutElement>();
            layout.minHeight = size + 2f;
            layout.preferredHeight = size + 4f;
            return label;
        }

        private TMP_Text AddLocalizedLabel(Transform parent, string english, string chineseSimplified, string swedish, int size, TextAlignmentOptions alignment)
        {
            TMP_Text label = AddLabel(parent, english, size, alignment);
            LocalizedUIText localized = label.gameObject.AddComponent<LocalizedUIText>();
            localized.SetTexts(english, chineseSimplified, swedish);
            return label;
        }

        private Button AddButton(Transform parent, string text, UnityEngine.Events.UnityAction action)
        {
            return AddButton(parent, text, action, out _);
        }

        private Button AddLocalizedButton(Transform parent, string english, string chineseSimplified, string swedish, UnityEngine.Events.UnityAction action)
        {
            Button button = AddButton(parent, english, action, out TMP_Text label);
            if (label != null)
            {
                LocalizedUIText localized = label.gameObject.AddComponent<LocalizedUIText>();
                localized.SetTexts(english, chineseSimplified, swedish);
            }
            return button;
        }

        private Button AddButton(Transform parent, string text, UnityEngine.Events.UnityAction action, out TMP_Text label)
        {
            GameObject go = new GameObject(text.Replace(" ", string.Empty) + "Button");
            go.transform.SetParent(parent, false);
            Image image = go.AddComponent<Image>();
            image.color = normalColor;

            Button button = go.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);

            LayoutElement layout = go.AddComponent<LayoutElement>();
            layout.minHeight = ButtonHeight;
            layout.preferredHeight = ButtonHeight;
            layout.flexibleWidth = 1f;

            label = AddLabel(go.transform, text, 18, TextAlignmentOptions.Center);
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            Object.Destroy(label.GetComponent<LayoutElement>());
            label.color = normalTextColor;

            return button;
        }

        private Slider AddSlider(Transform parent, float min, float max, bool wholeNumbers)
        {
            RectTransform root = CreateRect("Slider", parent);
            LayoutElement layout = root.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = SliderHeight;
            layout.preferredHeight = SliderHeight;

            Image background = CreateImage("Background", root, new Color(0.2f, 0.24f, 0.27f, 0.95f));
            Stretch(background.rectTransform, new Vector2(0f, 0.35f), new Vector2(1f, 0.65f));

            RectTransform fillArea = CreateRect("Fill Area", root);
            Stretch(fillArea, new Vector2(0f, 0.35f), new Vector2(1f, 0.65f));
            Image fill = CreateImage("Fill", fillArea, selectedColor);
            Stretch(fill.rectTransform, Vector2.zero, Vector2.one);

            RectTransform handleArea = CreateRect("Handle Slide Area", root);
            Stretch(handleArea, Vector2.zero, Vector2.one);
            Image handle = CreateImage("Handle", handleArea, Color.white);
            handle.rectTransform.sizeDelta = new Vector2(20f, 20f);

            Slider slider = root.gameObject.AddComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = wholeNumbers;
            slider.targetGraphic = handle;
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            return slider;
        }

        private RectTransform AddHorizontalRow(Transform parent)
        {
            RectTransform row = CreateRect("Row", parent);
            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            LayoutElement element = row.gameObject.AddComponent<LayoutElement>();
            element.minHeight = ButtonHeight;
            element.preferredHeight = ButtonHeight;
            return row;
        }

        private void AddFlexibleSpace(Transform parent)
        {
            RectTransform spacer = CreateRect("FlexibleSpace", parent);
            LayoutElement layout = spacer.gameObject.AddComponent<LayoutElement>();
            layout.flexibleHeight = 1f;
        }

        private RectTransform FindRect(params string[] names)
        {
            RectTransform[] rects = GetComponentsInChildren<RectTransform>(true);
            for (int nameIndex = 0; nameIndex < names.Length; nameIndex++)
            {
                for (int i = 0; i < rects.Length; i++)
                {
                    if (rects[i] != null && rects[i].name == names[nameIndex])
                    {
                        return rects[i];
                    }
                }
            }

            return null;
        }

        private T FindComponent<T>(params string[] names) where T : Component
        {
            T[] components = GetComponentsInChildren<T>(true);
            for (int nameIndex = 0; nameIndex < names.Length; nameIndex++)
            {
                for (int i = 0; i < components.Length; i++)
                {
                    if (components[i] != null && components[i].name == names[nameIndex])
                    {
                        return components[i];
                    }
                }
            }

            return null;
        }

        private static void Wire(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null || action == null)
            {
                return;
            }

            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        private static void Wire(Slider slider, UnityEngine.Events.UnityAction<float> action)
        {
            if (slider == null || action == null)
            {
                return;
            }

            slider.onValueChanged.RemoveListener(action);
            slider.onValueChanged.AddListener(action);
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static Image CreateImage(string name, Transform parent, Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}

#pragma warning restore 0649
