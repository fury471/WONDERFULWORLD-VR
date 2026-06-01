using System;
using TMPro;
using UnityEngine;

#pragma warning disable 0649

namespace Wonderland.UI
{
    [Serializable]
    public struct LocalizedTextEntry
    {
        public UILanguage language;
        [TextArea(1, 4)] public string text;
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_Text))]
    public sealed class LocalizedUIText : MonoBehaviour
    {
        [SerializeField] private TMP_Text targetText;
        [SerializeField] private string fallbackText;
        [SerializeField] private LocalizedTextEntry[] localizedTexts;
        [SerializeField] private bool useLocalizedFontFallback = true;

        private TMP_FontAsset originalFont;

        private void Reset()
        {
            targetText = GetComponent<TMP_Text>();
            fallbackText = targetText != null ? targetText.text : string.Empty;
        }

        private void Awake()
        {
            if (targetText == null)
            {
                targetText = GetComponent<TMP_Text>();
            }

            if (string.IsNullOrEmpty(fallbackText) && targetText != null)
            {
                fallbackText = targetText.text;
            }

            if (targetText != null)
            {
                originalFont = targetText.font;
            }
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

        public void SetTexts(string english, string chineseSimplified, string swedish)
        {
            fallbackText = english;
            localizedTexts = new[]
            {
                new LocalizedTextEntry { language = UILanguage.English, text = english },
                new LocalizedTextEntry { language = UILanguage.ChineseSimplified, text = chineseSimplified },
                new LocalizedTextEntry { language = UILanguage.Swedish, text = swedish }
            };

            ApplyLanguage(UILanguageService.GetCurrentOrDefault());
        }

        private void ApplyLanguage(UILanguage language)
        {
            if (targetText == null)
            {
                return;
            }

            ApplyFont();
            string selectedText = fallbackText;

            if (localizedTexts != null)
            {
                for (int i = 0; i < localizedTexts.Length; i++)
                {
                    LocalizedTextEntry entry = localizedTexts[i];
                    if (entry.language == language && !string.IsNullOrEmpty(entry.text))
                    {
                        selectedText = entry.text;
                        break;
                    }
                }
            }

            EnsureCharacters(selectedText);
            targetText.text = selectedText;
        }

        private void ApplyFont()
        {
            if (!useLocalizedFontFallback)
            {
                return;
            }

            TMP_FontAsset localizedFont = LocalizedUIFontProvider.GetBestLocalizedFont();
            if (localizedFont != null)
            {
                targetText.font = localizedFont;
            }
            else if (originalFont != null)
            {
                targetText.font = originalFont;
            }
        }

        private void EnsureCharacters(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            TMP_FontAsset font = targetText != null ? targetText.font : null;
            font?.TryAddCharacters(value, out _, false);
        }
    }
}

#pragma warning restore 0649
