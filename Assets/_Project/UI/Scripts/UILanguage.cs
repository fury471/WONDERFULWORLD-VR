using System;
using UnityEngine;

namespace Wonderland.UI
{
    public enum UILanguage
    {
        English = 0,
        ChineseSimplified = 1,
        Swedish = 2
    }

    [Serializable]
    public struct LocalizedSpriteSet
    {
        public UILanguage language;
        public Sprite sprite;
    }
}
