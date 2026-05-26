#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Wonderland.UI.EditorTools
{
    public static class LocalizedUIFontAssetBuilder
    {
        private const string SourceFontPath = "Assets/_Project/UI/Resources/Fonts/NotoSansCJKsc-Regular.otf";
        private const string OutputFontAssetPath = "Assets/_Project/UI/Resources/Fonts/NotoSansCJKsc-Regular-UI-SDF.asset";
        private const string UiRootPath = "Assets/_Project/UI";
        private static readonly Regex UnicodeEscapePattern = new Regex(@"\\u([0-9A-Fa-f]{4})", RegexOptions.Compiled);
        private static readonly Regex LatinEscapePattern = new Regex(@"\\x([0-9A-Fa-f]{2})", RegexOptions.Compiled);

        [MenuItem("Wonderland/UI/Rebuild Localized UI Font Asset")]
        public static void RebuildLocalizedUIFontAsset()
        {
            Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
            if (sourceFont == null)
            {
                throw new FileNotFoundException("Localized UI source font was not found.", SourceFontPath);
            }

            string characters = CollectLocalizedCharacters();
            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont,
                90,
                9,
                GlyphRenderMode.SDFAA,
                2048,
                2048,
                AtlasPopulationMode.Dynamic,
                false);

            if (fontAsset == null)
            {
                throw new System.InvalidOperationException("TMP could not create the localized UI font asset.");
            }

            fontAsset.name = "NotoSansCJKsc-Regular-UI-SDF";
            fontAsset.isMultiAtlasTexturesEnabled = false;

            if (!fontAsset.TryAddCharacters(characters, out string missingCharacters, false))
            {
                Debug.LogWarning(
                    $"[Localized UI] Font atlas was built with missing characters: {missingCharacters}",
                    fontAsset);
            }

            fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;

            AssetDatabase.DeleteAsset(OutputFontAssetPath);
            AssetDatabase.CreateAsset(fontAsset, OutputFontAssetPath);
            AddSubAsset(fontAsset.material, fontAsset);

            Texture2D[] atlasTextures = fontAsset.atlasTextures;
            if (atlasTextures != null)
            {
                for (int i = 0; i < atlasTextures.Length; i++)
                {
                    AddSubAsset(atlasTextures[i], fontAsset);
                }
            }

            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"[Localized UI] Built {OutputFontAssetPath} with {characters.Length} unique characters.",
                fontAsset);
        }

        [MenuItem("Wonderland/UI/Validate Localized UI Font Asset")]
        public static void ValidateLocalizedUIFontAsset()
        {
            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OutputFontAssetPath);
            if (fontAsset == null)
            {
                throw new FileNotFoundException("Localized UI TMP font asset was not found.", OutputFontAssetPath);
            }

            string characters = CollectLocalizedCharacters();
            StringBuilder missingCharacters = new StringBuilder();

            for (int i = 0; i < characters.Length; i++)
            {
                char character = characters[i];
                if (char.IsControl(character))
                {
                    continue;
                }

                if (!fontAsset.HasCharacter(character))
                {
                    missingCharacters.Append(character);
                }
            }

            if (missingCharacters.Length > 0)
            {
                throw new System.InvalidOperationException(
                    $"Localized UI TMP font asset is missing characters: {missingCharacters}");
            }

            Debug.Log(
                $"[Localized UI] Validated {OutputFontAssetPath} with {characters.Length} collected characters.",
                fontAsset);
        }

        public static string CollectLocalizedCharacters()
        {
            SortedSet<char> characters = new SortedSet<char>();
            HashSet<string> scannedPaths = new HashSet<string>();

            AddRange(characters, "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789");
            AddRange(characters, " .,;:!?()[]{}<>+-*/=_@#$%^&'\"`~|\\\r\n\t");
            AddRange(characters, "\u00e5\u00e4\u00f6\u00c5\u00c4\u00d6\u00e9\u00c9\u00e8\u00c8\u00ea\u00ca\u00fc\u00dc\u00f1\u00d1\u00e7\u00c7");

            foreach (string path in Directory.EnumerateFiles(UiRootPath, "*.*", SearchOption.AllDirectories))
            {
                string normalizedPath = path.Replace('\\', '/');
                if (normalizedPath.Contains("/Editor/"))
                {
                    continue;
                }

                AddCharactersFromFile(path, scannedPaths, characters);
            }

            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene == null || !scene.enabled)
                {
                    continue;
                }

                AddCharactersFromFile(scene.path, scannedPaths, characters);
            }

            StringBuilder builder = new StringBuilder(characters.Count);
            foreach (char character in characters)
            {
                if (!char.IsControl(character) || character == '\n' || character == '\r' || character == '\t')
                {
                    builder.Append(character);
                }
            }

            return builder.ToString();
        }

        private static void AddCharactersFromFile(
            string path,
            ISet<string> scannedPaths,
            ISet<char> characters)
        {
            string normalizedPath = path.Replace('\\', '/');
            if (!scannedPaths.Add(normalizedPath))
            {
                return;
            }

            string extension = Path.GetExtension(normalizedPath).ToLowerInvariant();
            if (extension != ".cs" &&
                extension != ".asset" &&
                extension != ".prefab" &&
                extension != ".unity")
            {
                return;
            }

            try
            {
                string text = File.ReadAllText(path, Encoding.UTF8);
                AddRange(characters, text);
                AddRange(characters, DecodeUnityEscapes(text));
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"[Localized UI] Skipped text scan for {path}: {exception.Message}");
            }
        }

        private static string DecodeUnityEscapes(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            string decoded = UnicodeEscapePattern.Replace(
                value,
                match => ((char)System.Convert.ToInt32(match.Groups[1].Value, 16)).ToString());

            decoded = LatinEscapePattern.Replace(
                decoded,
                match => ((char)System.Convert.ToInt32(match.Groups[1].Value, 16)).ToString());

            return decoded;
        }

        private static void AddRange(ISet<char> characters, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            for (int i = 0; i < value.Length; i++)
            {
                characters.Add(value[i]);
            }
        }

        private static void AddSubAsset(Object asset, Object parent)
        {
            if (asset == null || AssetDatabase.Contains(asset))
            {
                return;
            }

            AssetDatabase.AddObjectToAsset(asset, parent);
        }
    }
}
#endif
