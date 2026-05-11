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
        public Vector3 rotationAxis;
        public float extraHoldDuration;
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
                rotationAxis = Vector3.zero,
                extraHoldDuration = 1.5f,
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
                autoRotate = IsRotatingPattern(pattern),
                rotationSpeedDegrees = ResolveRotationSpeed(pattern),
                rotationAxis = ResolveRotationAxis(pattern),
                extraHoldDuration = 0f,
                pointBudget = pointBudget,
                displayName = pattern == MathFireworkPattern.Ring ? "DNA Helix" : pattern.ToString()
            };
        }

        private static bool IsRotatingPattern(MathFireworkPattern pattern)
        {
            return pattern == MathFireworkPattern.Sphere
                || pattern == MathFireworkPattern.Ring
                || pattern == MathFireworkPattern.Spiral
                || pattern == MathFireworkPattern.Mobius;
        }

        private static float ResolveRotationSpeed(MathFireworkPattern pattern)
        {
            return pattern switch
            {
                MathFireworkPattern.Ring => 18f,
                MathFireworkPattern.Spiral => 14f,
                MathFireworkPattern.Sphere => 12f,
                MathFireworkPattern.Mobius => 14f,
                _ => 0f
            };
        }

        private static Vector3 ResolveRotationAxis(MathFireworkPattern pattern)
        {
            return pattern switch
            {
                MathFireworkPattern.Ring => Vector3.right,
                MathFireworkPattern.Spiral => Vector3.up,
                MathFireworkPattern.Sphere => Vector3.up,
                MathFireworkPattern.Mobius => new Vector3(0.62f, 1f, 0f),
                _ => Vector3.zero
            };
        }
    }
}
