using TMPro;
using UnityEngine;
using UnityEngine.UI;

#pragma warning disable 0649

namespace Wonderland.UI
{
    [DisallowMultipleComponent]
    public sealed class VRTutorialMenuView : MonoBehaviour
    {
        [System.Serializable]
        public struct TutorialPage
        {
            [TextArea(1, 2)] public string englishTitle;
            [TextArea(3, 6)] public string englishBody;
            [TextArea(1, 2)] public string chineseTitle;
            [TextArea(3, 6)] public string chineseBody;
            [TextArea(1, 2)] public string swedishTitle;
            [TextArea(3, 6)] public string swedishBody;
        }

        [Header("Wiring")]
        [SerializeField] private VRSystemMenuController systemMenu;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private TMP_Text pageCounterText;
        [SerializeField] private Button previousButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button backButton;
        [SerializeField] private Button cancelButton;

        [Header("Content")]
        [SerializeField] private TutorialPage[] pages;

        [Header("Button State")]
        [SerializeField] private Color enabledButtonColor = new Color(0.90f, 0.95f, 0.94f, 1f);
        [SerializeField] private Color disabledButtonColor = new Color(0.32f, 0.36f, 0.38f, 0.65f);
        [SerializeField] private Color enabledTextColor = new Color(0.08f, 0.12f, 0.14f, 1f);
        [SerializeField] private Color disabledTextColor = new Color(0.64f, 0.68f, 0.68f, 1f);

        private int pageIndex;
        private bool wired;

        private void Awake()
        {
            ResolveReferences();
            ApplyLocalizedFontToChildren();
            WireButtons();
        }

        private void OnEnable()
        {
            UILanguageService.LanguageChanged += ApplyLanguage;
            ApplyLanguage(UILanguageService.GetCurrentOrDefault());
        }

        private void OnDisable()
        {
            UILanguageService.LanguageChanged -= ApplyLanguage;
        }

        public void ShowFirstPage()
        {
            pageIndex = 0;
            ApplyLanguage(UILanguageService.GetCurrentOrDefault());
        }

        public void PreviousPage()
        {
            if (pageIndex <= 0)
            {
                return;
            }

            pageIndex--;
            ApplyLanguage(UILanguageService.GetCurrentOrDefault());
        }

        public void NextPage()
        {
            if (pages == null || pageIndex >= pages.Length - 1)
            {
                return;
            }

            pageIndex++;
            ApplyLanguage(UILanguageService.GetCurrentOrDefault());
        }

        public void Back()
        {
            ResolveReferences();
            if (systemMenu != null)
            {
                systemMenu.ShowMainPanel();
            }
        }

        public void Cancel()
        {
            ResolveReferences();
            if (systemMenu != null)
            {
                systemMenu.CloseMenu();
            }
        }

        private void ResolveReferences()
        {
            if (systemMenu == null)
            {
                systemMenu = GetComponentInParent<VRSystemMenuController>(true);
            }
        }

        private void WireButtons()
        {
            if (wired)
            {
                return;
            }

            if (previousButton != null) previousButton.onClick.AddListener(PreviousPage);
            if (nextButton != null) nextButton.onClick.AddListener(NextPage);
            if (backButton != null) backButton.onClick.AddListener(Back);
            if (cancelButton != null) cancelButton.onClick.AddListener(Cancel);
            wired = true;
        }

        private void ApplyLanguage(UILanguage language)
        {
            if (pages == null || pages.Length == 0)
            {
                if (titleText != null) titleText.text = string.Empty;
                if (bodyText != null) bodyText.text = string.Empty;
                if (pageCounterText != null) pageCounterText.text = string.Empty;
                return;
            }

            pageIndex = Mathf.Clamp(pageIndex, 0, pages.Length - 1);
            TutorialPage page = pages[pageIndex];

            if (titleText != null)
            {
                titleText.text = Select(language, page.englishTitle, page.chineseTitle, page.swedishTitle);
            }

            if (bodyText != null)
            {
                bodyText.text = Select(language, page.englishBody, page.chineseBody, page.swedishBody);
            }

            if (pageCounterText != null)
            {
                pageCounterText.text = $"{pageIndex + 1}/{pages.Length}";
            }

            RefreshButtonState(previousButton, pageIndex > 0);
            RefreshButtonState(nextButton, pageIndex < pages.Length - 1);
        }

        private static string Select(UILanguage language, string english, string chinese, string swedish)
        {
            switch (language)
            {
                case UILanguage.ChineseSimplified:
                    return string.IsNullOrEmpty(chinese) ? english : chinese;
                case UILanguage.Swedish:
                    return string.IsNullOrEmpty(swedish) ? english : swedish;
                default:
                    return english;
            }
        }

        private void RefreshButtonState(Button button, bool enabled)
        {
            if (button == null)
            {
                return;
            }

            button.interactable = enabled;
            if (button.targetGraphic != null)
            {
                button.targetGraphic.color = enabled ? enabledButtonColor : disabledButtonColor;
            }

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.color = enabled ? enabledTextColor : disabledTextColor;
            }
        }

        private void ApplyLocalizedFontToChildren()
        {
            TMP_FontAsset localizedFont = LocalizedUIFontProvider.GetBestLocalizedFont();
            if (localizedFont == null)
            {
                return;
            }

            TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null)
                {
                    texts[i].font = localizedFont;
                }
            }
        }
    }
}

#pragma warning restore 0649
