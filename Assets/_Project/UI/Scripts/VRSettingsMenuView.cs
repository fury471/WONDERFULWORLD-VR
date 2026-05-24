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
        private const float ButtonHeight = 42f;

        [Header("Runtime Wiring")]
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private QuestLocomotionComfortProfile locomotionProfile;
        [SerializeField] private VRSystemMenuController systemMenu;

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

        private RectTransform settingsPage;
        private RectTransform languagePage;
        private Slider masterSlider;
        private Slider vignetteSlider;
        private Button teleportButton;
        private Button continuousMoveButton;
        private Button snapTurnButton;
        private Button continuousTurnButton;
        private Button vignetteOffButton;
        private Button vignetteLowButton;
        private Button vignetteMediumButton;
        private Button vignetteHighButton;
        private Button runtimeEnglishButton;
        private Button runtimeChineseButton;
        private Button runtimeSwedishButton;
        private TMP_Text runtimeEnglishLabel;
        private TMP_Text runtimeChineseLabel;
        private TMP_Text runtimeSwedishLabel;
        private bool suppressSliderEvents;

        private void Awake()
        {
            ResolveRuntimeReferences();
            WonderlandAudioBus.SetMixer(audioMixer);
            BuildRuntimeView();
            WireLegacyLanguageButtons();
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
            root.offsetMin = new Vector2(28f, 24f);
            root.offsetMax = new Vector2(-28f, -24f);

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
            layout.spacing = 12f;
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return page;
        }

        private void BuildSettingsPage(Transform parent)
        {
            AddLabel(parent, "Settings", 34, TextAlignmentOptions.Left);
            AddSpacer(parent, 2f);

            AddLabel(parent, "Master Audio", 20, TextAlignmentOptions.Left);
            masterSlider = AddSlider(parent, 0f, 1f, wholeNumbers: false);
            masterSlider.onValueChanged.AddListener(value =>
            {
                if (!suppressSliderEvents)
                {
                    WonderlandAudioBus.SetMasterVolume(value, save: true);
                }
            });

            AddSectionLabel(parent, "Locomotion Mode");
            RectTransform moveRow = AddHorizontalRow(parent);
            teleportButton = AddButton(moveRow, "Teleport", () => SetMovementMode(QuestLocomotionComfortProfile.MovementMode.Teleport));
            continuousMoveButton = AddButton(moveRow, "Continuous", () => SetMovementMode(QuestLocomotionComfortProfile.MovementMode.Smooth));

            AddSectionLabel(parent, "Turn Mode");
            RectTransform turnRow = AddHorizontalRow(parent);
            snapTurnButton = AddButton(turnRow, "Snap", () => SetTurnMode(QuestLocomotionComfortProfile.TurnMode.Snap));
            continuousTurnButton = AddButton(turnRow, "Continuous", () => SetTurnMode(QuestLocomotionComfortProfile.TurnMode.Smooth));

            AddSectionLabel(parent, "Comfort Vignette");
            RectTransform vignetteRow = AddHorizontalRow(parent);
            vignetteOffButton = AddButton(vignetteRow, "Off", () => SetVignetteLevel(0));
            vignetteLowButton = AddButton(vignetteRow, "Low", () => SetVignetteLevel(1));
            vignetteMediumButton = AddButton(vignetteRow, "Med", () => SetVignetteLevel(2));
            vignetteHighButton = AddButton(vignetteRow, "High", () => SetVignetteLevel(3));
            vignetteSlider = AddSlider(parent, 0f, 3f, wholeNumbers: true);
            vignetteSlider.onValueChanged.AddListener(value =>
            {
                if (!suppressSliderEvents)
                {
                    SetVignetteLevel(Mathf.RoundToInt(value));
                }
            });

            AddSpacer(parent, 4f);
            AddButton(parent, "Language", ShowLanguagePage);
            AddFlexibleSpace(parent);
            RectTransform bottomRow = AddHorizontalRow(parent);
            AddButton(bottomRow, "Back", Back);
            AddButton(bottomRow, "Cancel", Cancel);
        }

        private void BuildLanguagePage(Transform parent)
        {
            AddLabel(parent, "Language", 34, TextAlignmentOptions.Left);
            AddSpacer(parent, 8f);
            runtimeEnglishButton = AddButton(parent, "English", SetEnglish, out runtimeEnglishLabel);
            runtimeChineseButton = AddButton(parent, "Chinese", SetChinese, out runtimeChineseLabel);
            runtimeSwedishButton = AddButton(parent, "Svenska", SetSwedish, out runtimeSwedishLabel);
            AddFlexibleSpace(parent);
            RectTransform bottomRow = AddHorizontalRow(parent);
            AddButton(bottomRow, "Back", Back);
            AddButton(bottomRow, "Cancel", Cancel);
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
            if (vignetteSlider != null)
            {
                vignetteSlider.value = vignetteLevel;
            }

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
            layout.minHeight = size + 8f;
            layout.preferredHeight = size + 10f;
            return label;
        }

        private void AddSectionLabel(Transform parent, string text)
        {
            AddSpacer(parent, 4f);
            AddLabel(parent, text, 19, TextAlignmentOptions.Left);
        }

        private Button AddButton(Transform parent, string text, UnityEngine.Events.UnityAction action)
        {
            return AddButton(parent, text, action, out _);
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
            layout.minHeight = 38f;
            layout.preferredHeight = 38f;

            Image background = CreateImage("Background", root, new Color(0.2f, 0.24f, 0.27f, 0.95f));
            Stretch(background.rectTransform, new Vector2(0f, 0.35f), new Vector2(1f, 0.65f));

            RectTransform fillArea = CreateRect("Fill Area", root);
            Stretch(fillArea, new Vector2(0f, 0.35f), new Vector2(1f, 0.65f));
            Image fill = CreateImage("Fill", fillArea, selectedColor);
            Stretch(fill.rectTransform, Vector2.zero, Vector2.one);

            RectTransform handleArea = CreateRect("Handle Slide Area", root);
            Stretch(handleArea, Vector2.zero, Vector2.one);
            Image handle = CreateImage("Handle", handleArea, Color.white);
            handle.rectTransform.sizeDelta = new Vector2(24f, 24f);

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

        private void AddSpacer(Transform parent, float height)
        {
            RectTransform spacer = CreateRect("Spacer", parent);
            LayoutElement layout = spacer.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = height;
            layout.preferredHeight = height;
        }

        private void AddFlexibleSpace(Transform parent)
        {
            RectTransform spacer = CreateRect("FlexibleSpace", parent);
            LayoutElement layout = spacer.gameObject.AddComponent<LayoutElement>();
            layout.flexibleHeight = 1f;
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
