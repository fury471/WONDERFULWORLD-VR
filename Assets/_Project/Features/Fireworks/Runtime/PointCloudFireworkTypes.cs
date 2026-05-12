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
        DoubleHelix,
        Spiral,
        Sphere,
        Flower,
        Star,
        Mobius
    }

    public enum FireworkShowcaseStepKind
    {
        Text,
        MathPattern
    }

    [Serializable]
    public class FireworkShowcaseStep
    {
        public bool enabled = true;
        public FireworkShowcaseStepKind kind = FireworkShowcaseStepKind.MathPattern;
        public string textOverride = string.Empty;
        public MathFireworkPattern mathPattern = MathFireworkPattern.Heart;

        public static FireworkShowcaseStep Text(string textOverride = "")
        {
            return new FireworkShowcaseStep
            {
                enabled = true,
                kind = FireworkShowcaseStepKind.Text,
                textOverride = textOverride,
                mathPattern = MathFireworkPattern.Heart
            };
        }

        public static FireworkShowcaseStep Math(MathFireworkPattern pattern)
        {
            return new FireworkShowcaseStep
            {
                enabled = true,
                kind = FireworkShowcaseStepKind.MathPattern,
                textOverride = string.Empty,
                mathPattern = pattern
            };
        }
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
                displayName = pattern == MathFireworkPattern.DoubleHelix ? "Double Helix" : pattern.ToString()
            };
        }

        private static bool IsRotatingPattern(MathFireworkPattern pattern)
        {
            return pattern == MathFireworkPattern.Sphere
                || pattern == MathFireworkPattern.DoubleHelix
                || pattern == MathFireworkPattern.Spiral
                || pattern == MathFireworkPattern.Mobius;
        }

        private static float ResolveRotationSpeed(MathFireworkPattern pattern)
        {
            return pattern switch
            {
                MathFireworkPattern.DoubleHelix => 18f,
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
                MathFireworkPattern.DoubleHelix => Vector3.right,
                MathFireworkPattern.Spiral => Vector3.up,
                MathFireworkPattern.Sphere => Vector3.up,
                MathFireworkPattern.Mobius => new Vector3(0.62f, 1f, 0f),
                _ => Vector3.zero
            };
        }
    }
}
