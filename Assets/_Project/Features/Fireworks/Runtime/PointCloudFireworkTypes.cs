using System;
using UnityEngine;

namespace WonderfulWorld.Features.Fireworks
{
    public enum FireworkQualityMode
    {
        Performance,
        Balanced,
        Showcase
    }

    public enum PointCloudFireworkKind
    {
        Text,
        MathPattern
    }

    public enum MathFireworkPattern
    {
        Heart,
        Ring,
        Spiral,
        Sphere,
        Flower,
        Star,
        Mobius
    }

    [Serializable]
    public struct PointCloudFireworkRequest
    {
        public PointCloudFireworkKind kind;
        public string text;
        public MathFireworkPattern mathPattern;
        public Color color;
        public float scale;
        public float particleSizeMultiplier;
        public bool autoRotate;
        public float rotationSpeedDegrees;
        public int pointBudget;
        public string displayName;

        public static PointCloudFireworkRequest Text(string value, Color color, float scale, int pointBudget)
        {
            string sanitized = FireworkPointCloudGenerator.SanitizeText(value);
            return new PointCloudFireworkRequest
            {
                kind = PointCloudFireworkKind.Text,
                text = sanitized,
                mathPattern = MathFireworkPattern.Heart,
                color = color,
                scale = scale,
                particleSizeMultiplier = 1f,
                autoRotate = false,
                rotationSpeedDegrees = 0f,
                pointBudget = pointBudget,
                displayName = string.IsNullOrWhiteSpace(sanitized) ? "Text" : sanitized
            };
        }

        public static PointCloudFireworkRequest Math(MathFireworkPattern pattern, Color color, float scale, int pointBudget)
        {
            return new PointCloudFireworkRequest
            {
                kind = PointCloudFireworkKind.MathPattern,
                text = string.Empty,
                mathPattern = pattern,
                color = color,
                scale = scale,
                particleSizeMultiplier = 1.8f,
                autoRotate = pattern == MathFireworkPattern.Mobius,
                rotationSpeedDegrees = pattern == MathFireworkPattern.Mobius ? 14f : 0f,
                pointBudget = pointBudget,
                displayName = pattern.ToString()
            };
        }
    }
}
