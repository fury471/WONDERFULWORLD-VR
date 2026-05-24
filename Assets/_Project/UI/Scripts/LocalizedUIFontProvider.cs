using TMPro;
using UnityEngine;

namespace Wonderland.UI
{
    public static class LocalizedUIFontProvider
    {
        private const string ProjectCjkFontResourcePath = "Fonts/NotoSansCJKsc-Regular";

        private static readonly (string family, string style)[] FontCandidates =
        {
            ("Noto Sans CJK SC", "Regular"),
            ("Noto Sans SC", "Regular"),
            ("Droid Sans Fallback", "Regular"),
            ("Microsoft YaHei", "Regular"),
            ("Microsoft YaHei UI", "Regular"),
            ("DengXian", "Regular"),
            ("SimHei", "Regular"),
            ("SimSun", "Regular")
        };

        private static TMP_FontAsset cachedFont;
        private static bool attemptedResolve;

        public static TMP_FontAsset GetBestLocalizedFont()
        {
            if (cachedFont != null)
            {
                return cachedFont;
            }

            if (attemptedResolve)
            {
                return null;
            }

            attemptedResolve = true;

            Font bundledFont = Resources.Load<Font>(ProjectCjkFontResourcePath);
            if (bundledFont != null)
            {
                TMP_FontAsset bundledFontAsset = TMP_FontAsset.CreateFontAsset(bundledFont);
                if (bundledFontAsset != null)
                {
                    ConfigureFontAsset(bundledFontAsset, "Runtime Localized UI Font (Noto Sans CJK SC)");
                    cachedFont = bundledFontAsset;
                    return cachedFont;
                }
            }

            for (int i = 0; i < FontCandidates.Length; i++)
            {
                var candidate = FontCandidates[i];
                TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(candidate.family, candidate.style, 90);
                if (fontAsset == null)
                {
                    continue;
                }

                ConfigureFontAsset(fontAsset, "Runtime Localized UI Font");
                cachedFont = fontAsset;
                return cachedFont;
            }

            Debug.LogWarning("[Localized UI] Could not find a CJK-capable system font. Assign a CJK TMP Font Asset to your localized UI text if Chinese still appears as boxes.");
            return null;
        }

#if UNITY_EDITOR
        public static void ClearCachedFontForEditor()
        {
            cachedFont = null;
            attemptedResolve = false;
        }
#endif

        private static void ConfigureFontAsset(TMP_FontAsset fontAsset, string assetName)
        {
            fontAsset.name = assetName;
            fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            fontAsset.isMultiAtlasTexturesEnabled = true;
        }
    }
}
