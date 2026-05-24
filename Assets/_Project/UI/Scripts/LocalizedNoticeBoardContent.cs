using System.Collections.Generic;
using UnityEngine;

#pragma warning disable 0649

namespace Wonderland.UI
{
    [CreateAssetMenu(menuName = "Wonderful World/UI/Localized Notice Board Content")]
    public sealed class LocalizedNoticeBoardContent : ScriptableObject
    {
        [SerializeField] private string contentId = "notice-board";
        [SerializeField] private string displayName = "Notice Board";
        [SerializeField] private Sprite fallbackSprite;
        [SerializeField] private List<LocalizedSpriteSet> localizedSprites = new List<LocalizedSpriteSet>();

        public string ContentId => contentId;
        public string DisplayName => displayName;

        public Sprite GetSprite(UILanguage language)
        {
            for (int i = 0; i < localizedSprites.Count; i++)
            {
                LocalizedSpriteSet entry = localizedSprites[i];
                if (entry.language == language && entry.sprite != null)
                {
                    return entry.sprite;
                }
            }

            return fallbackSprite;
        }

#if UNITY_EDITOR
        public void SetEditorData(string newContentId, string newDisplayName, Sprite fallback, IList<LocalizedSpriteSet> sprites)
        {
            contentId = newContentId;
            displayName = newDisplayName;
            fallbackSprite = fallback;
            localizedSprites.Clear();

            if (sprites == null)
            {
                return;
            }

            for (int i = 0; i < sprites.Count; i++)
            {
                localizedSprites.Add(sprites[i]);
            }
        }
#endif
    }
}

#pragma warning restore 0649
