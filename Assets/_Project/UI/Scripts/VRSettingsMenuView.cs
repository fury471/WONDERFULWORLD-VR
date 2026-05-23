using TMPro;
using UnityEngine;
using UnityEngine.UI;

#pragma warning disable 0649

namespace Wonderland.UI
{
    [DisallowMultipleComponent]
    public sealed class VRSettingsMenuView : MonoBehaviour
    {
        [Header("Language Buttons")]
        [SerializeField] private Button englishButton;
        [SerializeField] private Button chineseButton;
        [SerializeField] private Button swedishButton;

        [Header("Button Labels")]
        [SerializeField] private TMP_Text englishLabel;
        [SerializeField] private TMP_Text chineseLabel;
        [SerializeField] private TMP_Text swedishLabel;

        [Header("Visual State")]
        [SerializeField] private Color selectedColor = new Color(0.18f, 0.58f, 0.72f, 1f);
        [SerializeField] private Color normalColor = new Color(0.92f, 0.95f, 0.94f, 1f);
        [SerializeField] private Color selectedTextColor = Color.white;
        [SerializeField] private Color normalTextColor = new Color(0.08f, 0.12f, 0.14f, 1f);

        private void Awake()
        {
            if (englishButton != null) englishButton.onClick.AddListener(SetEnglish);
            if (chineseButton != null) chineseButton.onClick.AddListener(SetChinese);
            if (swedishButton != null) swedishButton.onClick.AddListener(SetSwedish);
        }

        private void OnEnable()
        {
            UILanguageService.LanguageChanged += Refresh;
            Refresh(UILanguageService.GetCurrentOrDefault());
        }

        private void OnDisable()
        {
            UILanguageService.LanguageChanged -= Refresh;
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

        private static void SetLanguage(UILanguage language)
        {
            if (UILanguageService.Instance != null)
            {
                UILanguageService.Instance.SetLanguage(language);
            }
        }

        private void Refresh(UILanguage language)
        {
            ApplyButtonState(englishButton, englishLabel, language == UILanguage.English);
            ApplyButtonState(chineseButton, chineseLabel, language == UILanguage.ChineseSimplified);
            ApplyButtonState(swedishButton, swedishLabel, language == UILanguage.Swedish);
        }

        private void ApplyButtonState(Button button, TMP_Text label, bool selected)
        {
            if (button != null && button.targetGraphic != null)
            {
                button.targetGraphic.color = selected ? selectedColor : normalColor;
            }

            if (label != null)
            {
                label.color = selected ? selectedTextColor : normalTextColor;
            }
        }
    }
}

#pragma warning restore 0649
