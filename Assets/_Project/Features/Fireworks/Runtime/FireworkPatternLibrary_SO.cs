using System.Collections.Generic;
using UnityEngine;

namespace WonderfulWorld.Features.Fireworks
{
    [CreateAssetMenu(
        fileName = "FireworkPatternLibrary_SO",
        menuName = "WonderfulWorld/Fireworks/Pattern Library")]
    public class FireworkPatternLibrary_SO : ScriptableObject
    {
        [SerializeField] private List<FireworkPattern> patterns = new List<FireworkPattern>();

        public IReadOnlyList<FireworkPattern> Patterns => patterns;

        private void OnEnable()
        {
            if (patterns == null || patterns.Count == 0)
            {
                patterns = FireworkController.GetDefaultPatterns();
            }
        }

        public List<FireworkPattern> CreatePatternCopies()
        {
            if (patterns == null || patterns.Count == 0)
            {
                patterns = FireworkController.GetDefaultPatterns();
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

        [ContextMenu("Reset To Default M2 Patterns")]
        private void ResetToDefaults()
        {
            patterns = FireworkController.GetDefaultPatterns();
        }
    }
}
