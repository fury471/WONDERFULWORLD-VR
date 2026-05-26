using TMPro;
using UnityEngine;

namespace Wonderland.UI
{
    public static class LocalizedUIFontProvider
    {
        private const string ProjectCjkFontAssetResourcePath = "Fonts/NotoSansCJKsc-Regular-UI-SDF";

        private static TMP_FontAsset cachedFont;
        private static bool attemptedResolve;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void PreloadLocalizedFont()
        {
            GetBestLocalizedFont();
        }

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

            cachedFont = Resources.Load<TMP_FontAsset>(ProjectCjkFontAssetResourcePath);
            if (cachedFont != null)
            {
                cachedFont.ReadFontAssetDefinition();
                return cachedFont;
            }

            Debug.LogWarning(
                $"[Localized UI] Prebuilt TMP font asset was not found at Resources/{ProjectCjkFontAssetResourcePath}. " +
                "Localized text will fall back to its original font.");
            return null;
        }

#if UNITY_EDITOR
        public static void ClearCachedFontForEditor()
        {
            cachedFont = null;
            attemptedResolve = false;
        }
#endif
    }
}
