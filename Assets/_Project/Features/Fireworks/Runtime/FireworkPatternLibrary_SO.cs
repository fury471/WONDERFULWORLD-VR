using System.Collections.Generic;
using UnityEngine;

namespace WonderfulWorld.Features.Fireworks
{
    [CreateAssetMenu(
        fileName = "FireworkPatternLibrary_SO",
        menuName = "WonderfulWorld/Fireworks/Legacy Pattern Library")]
    public class FireworkPatternLibrary_SO : ScriptableObject
    {
        [SerializeField] private List<FireworkPattern> patterns = new List<FireworkPattern>();

        public IReadOnlyList<FireworkPattern> Patterns => patterns;

        private void OnEnable()
        {
            if (patterns == null || patterns.Count == 0)
            {
                patterns = LegacyFireworkPatternDefaults.Create();
            }
        }

        public List<FireworkPattern> CreatePatternCopies()
        {
            if (patterns == null || patterns.Count == 0)
            {
                patterns = LegacyFireworkPatternDefaults.Create();
            }

            List<FireworkPattern> copies = new List<FireworkPattern>(patterns.Count);
            for (int i = 0; i < patterns.Count; i++)
            {
                FireworkPattern pattern = patterns[i];
                copies.Add(new FireworkPattern
                {
                    patternName = pattern.patternName,
                    shape = pattern.shape,
                    effectPrefab = pattern.effectPrefab,
                    color = pattern.color,
                    heightOffset = pattern.heightOffset,
                    radius = pattern.radius,
                    delayAfterLaunch = pattern.delayAfterLaunch,
                    sizeMultiplier = pattern.sizeMultiplier,
                    sparkLifetime = pattern.sparkLifetime,
                    debugBurstCount = pattern.debugBurstCount,
                    fanArc = pattern.fanArc
                });
            }

            return copies;
        }

        [ContextMenu("Reset To Legacy Defaults")]
        private void ResetToDefaults()
        {
            patterns = LegacyFireworkPatternDefaults.Create();
        }
    }
}
