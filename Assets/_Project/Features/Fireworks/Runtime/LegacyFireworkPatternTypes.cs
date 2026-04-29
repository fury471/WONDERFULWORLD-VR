using System;
using System.Collections.Generic;
using UnityEngine;

namespace WonderfulWorld.Features.Fireworks
{
    public enum FireworkShape
    {
        Star,
        Ring,
        Heart,
        Flower,
        Spiral
    }

    [Serializable]
    public class FireworkPattern
    {
        public string patternName = "Pattern";
        public FireworkShape shape = FireworkShape.Star;
        public ParticleSystem effectPrefab;
        public Color color = Color.white;
        public float heightOffset = 8f;
        public float radius = 3f;
        public float delayAfterLaunch = 1f;
        public float sizeMultiplier = 1f;
        public float sparkLifetime = 1f;
        public int debugBurstCount = 12;
        public float fanArc = 90f;
    }

    public static class LegacyFireworkPatternDefaults
    {
        public static List<FireworkPattern> Create()
        {
            return new List<FireworkPattern>
            {
                new FireworkPattern { patternName = "Star", shape = FireworkShape.Star, color = new Color(1f, 0.45f, 0.2f) },
                new FireworkPattern { patternName = "Ring", shape = FireworkShape.Ring, color = new Color(0.3f, 0.8f, 1f) },
                new FireworkPattern { patternName = "Heart", shape = FireworkShape.Heart, color = new Color(1f, 0.35f, 0.55f) },
                new FireworkPattern { patternName = "Flower", shape = FireworkShape.Flower, color = new Color(1f, 0.72f, 0.9f) },
                new FireworkPattern { patternName = "Spiral", shape = FireworkShape.Spiral, color = new Color(0.58f, 1f, 0.78f) }
            };
        }
    }
}
