using TMPro;
using UnityEngine;

namespace Wonderland.UI
{
    public static class LocalizedUIFontProvider
    {
        private static readonly (string family, string style)[] FontCandidates =
        {
            ("Microsoft YaHei", "Regular"),
            ("Microsoft YaHei UI", "Regular"),
            ("DengXian", "Regular"),
            ("SimHei", "Regular"),
            ("SimSun", "Regular"),
            ("Noto Sans CJK SC", "Regular"),
            ("Noto Sans CJK", "Regular"),
            ("Droid Sans Fallback", "Regular")
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

            for (int i = 0; i < FontCandidates.Length; i++)
            {
                var candidate = FontCandidates[i];
                TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(candidate.family, candidate.style, 90);
                if (fontAsset == null)
                {
                    continue;
                }

                fontAsset.name = "Runtime Localized UI Font";
                fontAsset.atlasPopulationMode = AtlasPopulationMode.DynamicOS;
                fontAsset.isMultiAtlasTexturesEnabled = true;
                cachedFont = fontAsset;
                return cachedFont;
            }

            Debug.LogWarning("[Localized UI] Could not find a CJK-capable system font. Assign a CJK TMP Font Asset to your localized UI text if Chinese still appears as boxes.");
            return null;
        }
    }
}
