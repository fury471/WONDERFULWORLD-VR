using System;
using System.Collections.Generic;
using UnityEngine;

namespace WonderfulWorld.Features.Fireworks
{
    public static class FireworkPointCloudGenerator
    {
        public const int MaxTextLength = 128;
        public const int MinPointBudget = 32;
        public const int MaxPointBudget = 8000;
        private const int MaxTextLineLength = 32;
        private const int GlyphWidth = 5;
        private const int GlyphHeight = 7;
        private const float GlyphSpacing = 1.25f;
        private const float LineSpacing = 2.25f;
        private const float GoldenAngle = Mathf.PI * (3f - 2.236068f);

        private static readonly Dictionary<char, string[]> Glyphs = new Dictionary<char, string[]>
        {
            ['A'] = new[] { "01110", "10001", "10001", "11111", "10001", "10001", "10001" },
            ['B'] = new[] { "11110", "10001", "10001", "11110", "10001", "10001", "11110" },
            ['C'] = new[] { "01111", "10000", "10000", "10000", "10000", "10000", "01111" },
            ['D'] = new[] { "11110", "10001", "10001", "10001", "10001", "10001", "11110" },
            ['E'] = new[] { "11111", "10000", "10000", "11110", "10000", "10000", "11111" },
            ['F'] = new[] { "11111", "10000", "10000", "11110", "10000", "10000", "10000" },
            ['G'] = new[] { "01111", "10000", "10000", "10111", "10001", "10001", "01110" },
            ['H'] = new[] { "10001", "10001", "10001", "11111", "10001", "10001", "10001" },
            ['I'] = new[] { "11111", "00100", "00100", "00100", "00100", "00100", "11111" },
            ['J'] = new[] { "00111", "00010", "00010", "00010", "00010", "10010", "01100" },
            ['K'] = new[] { "10001", "10010", "10100", "11000", "10100", "10010", "10001" },
            ['L'] = new[] { "10000", "10000", "10000", "10000", "10000", "10000", "11111" },
            ['M'] = new[] { "10001", "11011", "10101", "10101", "10001", "10001", "10001" },
            ['N'] = new[] { "10001", "11001", "10101", "10011", "10001", "10001", "10001" },
            ['O'] = new[] { "01110", "10001", "10001", "10001", "10001", "10001", "01110" },
            ['P'] = new[] { "11110", "10001", "10001", "11110", "10000", "10000", "10000" },
            ['Q'] = new[] { "01110", "10001", "10001", "10001", "10101", "10010", "01101" },
            ['R'] = new[] { "11110", "10001", "10001", "11110", "10100", "10010", "10001" },
            ['S'] = new[] { "01111", "10000", "10000", "01110", "00001", "00001", "11110" },
            ['T'] = new[] { "11111", "00100", "00100", "00100", "00100", "00100", "00100" },
            ['U'] = new[] { "10001", "10001", "10001", "10001", "10001", "10001", "01110" },
            ['V'] = new[] { "10001", "10001", "10001", "10001", "10001", "01010", "00100" },
            ['W'] = new[] { "10001", "10001", "10001", "10101", "10101", "10101", "01010" },
            ['X'] = new[] { "10001", "10001", "01010", "00100", "01010", "10001", "10001" },
            ['Y'] = new[] { "10001", "10001", "01010", "00100", "00100", "00100", "00100" },
            ['Z'] = new[] { "11111", "00001", "00010", "00100", "01000", "10000", "11111" },
            [' '] = new[] { "00000", "00000", "00000", "00000", "00000", "00000", "00000" }
        };

        public static string SanitizeText(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return "DREAM";
            }

            char[] result = new char[Mathf.Min(MaxTextLength, input.Length)];
            int count = 0;
            bool previousWasSpace = true;
            for (int i = 0; i < input.Length && count < MaxTextLength; i++)
            {
                char c = char.ToUpperInvariant(input[i]);
                if (c >= 'A' && c <= 'Z')
                {
                    result[count++] = c;
                    previousWasSpace = false;
                }
                else if (c == ' ' && !previousWasSpace)
                {
                    result[count++] = c;
                    previousWasSpace = true;
                }
            }

            return count == 0 ? "DREAM" : new string(result, 0, count).Trim();
        }

        public static List<Vector3> Generate(PointCloudFireworkRequest request)
        {
            int pointBudget = Mathf.Clamp(request.pointBudget, MinPointBudget, MaxPointBudget);
            float scale = Mathf.Max(0.1f, request.scale);
            return request.kind == PointCloudFireworkKind.Text
                ? GenerateText(request.text, pointBudget, scale)
                : GenerateMathPattern(request.mathPattern, pointBudget, scale);
        }

        public static List<Vector3> GenerateText(string input, int pointBudget, float scale)
        {
            string text = SanitizeText(input);
            List<string> lines = WrapText(text);
            List<Vector3> basePoints = new List<Vector3>();
            int longestLineLength = 1;

            for (int lineIndex = 0; lineIndex < lines.Count; lineIndex++)
            {
                string line = lines[lineIndex];
                longestLineLength = Mathf.Max(longestLineLength, line.Length);
                float lineWidth = GetLineWidth(line);
                float cursor = -lineWidth * 0.5f;
                float lineY = -lineIndex * (GlyphHeight + LineSpacing);

                for (int i = 0; i < line.Length; i++)
                {
                    char c = line[i];
                    if (!Glyphs.TryGetValue(c, out string[] glyph))
                    {
                        continue;
                    }

                    for (int y = 0; y < GlyphHeight; y++)
                    {
                        string row = glyph[y];
                        for (int x = 0; x < GlyphWidth; x++)
                        {
                            if (row[x] != '1')
                            {
                                continue;
                            }

                            basePoints.Add(new Vector3(cursor + x, lineY + GlyphHeight - 1 - y, 0f));
                        }
                    }

                    cursor += GlyphWidth + GlyphSpacing;
                }
            }

            CenterAndScale(basePoints, scale / Mathf.Max(1f, longestLineLength * 0.55f));
            return ExpandPoints(basePoints, pointBudget, 0.042f * scale);
        }

        private static List<string> WrapText(string text)
        {
            List<string> lines = new List<string>();
            string[] words = text.Split(' ');
            string currentLine = string.Empty;

            for (int i = 0; i < words.Length; i++)
            {
                string word = words[i];
                if (string.IsNullOrEmpty(word))
                {
                    continue;
                }

                if (currentLine.Length == 0)
                {
                    currentLine = word;
                    continue;
                }

                int joinedLength = currentLine.Length + 1 + word.Length;
                if (currentLine.Length >= MaxTextLineLength || joinedLength > MaxTextLineLength)
                {
                    lines.Add(currentLine);
                    currentLine = word;
                }
                else
                {
                    currentLine += " " + word;
                }
            }

            if (currentLine.Length > 0)
            {
                lines.Add(currentLine);
            }

            if (lines.Count == 0)
            {
                lines.Add("DREAM");
            }

            return lines;
        }

        private static float GetLineWidth(string line)
        {
            return string.IsNullOrEmpty(line) ? GlyphWidth : line.Length * GlyphWidth + Mathf.Max(0, line.Length - 1) * GlyphSpacing;
        }

        public static List<Vector3> GenerateMathPattern(MathFireworkPattern pattern, int pointBudget, float scale)
        {
            int count = Mathf.Clamp(pointBudget, MinPointBudget, MaxPointBudget);
            List<Vector3> points = new List<Vector3>(count);
            switch (pattern)
            {
                case MathFireworkPattern.Heart:
                    AddHeartPattern(points, count);
                    break;
                case MathFireworkPattern.DoubleHelix:
                    AddRingPattern(points, count);
                    break;
                case MathFireworkPattern.Spiral:
                    AddSpiralPattern(points, count);
                    break;
                case MathFireworkPattern.Sphere:
                    AddSpherePattern(points, count);
                    break;
                case MathFireworkPattern.Flower:
                    AddFlowerPattern(points, count);
                    break;
                case MathFireworkPattern.Star:
                    AddStarPattern(points, count);
                    break;
                case MathFireworkPattern.Mobius:
                    AddMobiusPattern(points, count);
                    break;
                default:
                    AddSpherePattern(points, count);
                    break;
            }

            for (int i = 0; i < points.Count; i++)
            {
                points[i] *= scale;
            }

            return points;
        }

        private static Vector3 HeartPoint(float t)
        {
            float angle = t * Mathf.PI * 2f;
            float x = 16f * Mathf.Pow(Mathf.Sin(angle), 3f);
            float y =
                13f * Mathf.Cos(angle)
                - 5f * Mathf.Cos(2f * angle)
                - 2f * Mathf.Cos(3f * angle)
                - Mathf.Cos(4f * angle);

            return new Vector3(x / 18f, y / 18f + 0.1f, Mathf.Sin(angle * 3f) * 0.05f);
        }

        private static Vector3 StarPoint(float t)
        {
            const int vertexCount = 10;
            float scaled = t * vertexCount;
            int vertex = Mathf.FloorToInt(scaled);
            float edgeT = scaled - vertex;
            return Vector3.Lerp(StarVertex(vertex), StarVertex(vertex + 1), edgeT);
        }

        private static void AddHeartPattern(List<Vector3> points, int count)
        {
            int outlineCount = Mathf.RoundToInt(count * 0.48f);
            int interiorCount = count - outlineCount;
            int outlineLayers = 5;
            AddParametricLayers(points, outlineCount, outlineLayers, 0.92f, 1.02f, HeartPoint, 0.11f, 0.02f);
            AddParametricLayers(points, interiorCount, 12, 0.18f, 0.82f, HeartPoint, 0.28f, 0.34f);
            TrimToCount(points, count);
        }

        private static void AddRingPattern(List<Vector3> points, int count)
        {
            int helixCount = Mathf.RoundToInt(count * 0.58f);
            int rungCount = Mathf.RoundToInt(count * 0.3f);
            int glowCount = count - helixCount - rungCount;

            AddDnaHelixStrands(points, helixCount);
            AddDnaRungs(points, rungCount);
            AddDnaGlowCloud(points, glowCount);
            TrimToCount(points, count);
        }

        private static void AddDnaHelixStrands(List<Vector3> points, int pointCount)
        {
            if (pointCount <= 0)
            {
                return;
            }

            int strands = 2;
            int perStrand = Mathf.Max(24, Mathf.CeilToInt(pointCount / (float)strands));
            int targetCount = points.Count + pointCount;
            for (int strand = 0; strand < strands && points.Count < targetCount; strand++)
            {
                float phase = strand * Mathf.PI;

                for (int i = 0; i < perStrand && points.Count < targetCount; i++)
                {
                    float t = perStrand <= 1 ? 0f : i / (float)(perStrand - 1);
                    float angle = t * Mathf.PI * 2f * 2.35f + phase;
                    Vector3 point = DnaPoint(angle, t, 1f);
                    points.Add(point);
                }
            }
        }

        private static void AddDnaRungs(List<Vector3> points, int pointCount)
        {
            if (pointCount <= 0)
            {
                return;
            }

            int rungSegments = 22;
            int pointsPerRung = Mathf.Max(3, Mathf.CeilToInt(pointCount / (float)rungSegments));
            int targetCount = points.Count + pointCount;
            for (int rung = 0; rung < rungSegments && points.Count < targetCount; rung++)
            {
                float t = rungSegments <= 1 ? 0f : rung / (float)(rungSegments - 1);
                float angle = t * Mathf.PI * 2f * 2.35f;
                Vector3 a = DnaPoint(angle, t, 0.78f);
                Vector3 b = DnaPoint(angle + Mathf.PI, t, 0.78f);

                for (int i = 0; i < pointsPerRung && points.Count < targetCount; i++)
                {
                    float u = pointsPerRung <= 1 ? 0.5f : i / (float)(pointsPerRung - 1);
                    Vector3 point = Vector3.Lerp(a, b, u);
                    point += DnaNormalJitter(angle, 0.018f);
                    points.Add(point);
                }
            }
        }

        private static void AddDnaGlowCloud(List<Vector3> points, int pointCount)
        {
            if (pointCount <= 0)
            {
                return;
            }

            for (int i = 0; i < pointCount; i++)
            {
                float t = pointCount <= 1 ? 0.5f : (i + 0.5f) / pointCount;
                float angle = t * Mathf.PI * 2f * 2.35f + (i % 2) * Mathf.PI;
                Vector3 center = DnaPoint(angle, t, Mathf.Lerp(0.85f, 1.12f, Hash01(i, 17)));
                float halo = Mathf.Sqrt(Hash01(i, 23)) * Mathf.Lerp(0.035f, 0.12f, Hash01(i, 29));
                float haloAngle = i * GoldenAngle;
                Vector3 offset = new Vector3(Mathf.Cos(haloAngle) * halo, Mathf.Lerp(-0.035f, 0.035f, Hash01(i, 31)), Mathf.Sin(haloAngle) * halo);
                points.Add(center + TiltDnaPoint(offset));
            }
        }

        private static Vector3 DnaPoint(float angle, float t, float radiusMultiplier)
        {
            float radius = 0.56f * radiusMultiplier;
            float length = Mathf.Lerp(-1.15f, 1.15f, t);
            Vector3 point = new Vector3(
                length,
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius);
            return TiltDnaPoint(point);
        }

        private static Vector3 DnaNormalJitter(float angle, float amount)
        {
            Vector3 normal = new Vector3(0f, Mathf.Cos(angle), Mathf.Sin(angle)).normalized;
            float signed = Mathf.Sin(angle * 12.9898f + 78.233f);
            return TiltDnaPoint(normal * signed * amount);
        }

        private static Vector3 TiltDnaPoint(Vector3 point)
        {
            Quaternion tilt = Quaternion.Euler(0f, 0f, -7f);
            return tilt * point;
        }

        private static void AddSpiralPattern(List<Vector3> points, int count)
        {
            int strands = 4;
            for (int i = 0; i < count; i++)
            {
                int strand = i % strands;
                float t = count <= strands ? 0f : (i / strands) / (float)Mathf.Max(1, count / strands - 1);
                float angle = t * Mathf.PI * 8.5f + strand * Mathf.PI * 0.5f;
                float radius = Mathf.Lerp(0.18f, 1.05f, t) + (strand - 1.5f) * 0.035f;
                float tubeOffset = Mathf.Sin(t * Mathf.PI * 16f + strand) * 0.1f;
                points.Add(new Vector3(
                    Mathf.Cos(angle) * radius,
                    Mathf.Lerp(-0.88f, 0.88f, t) + tubeOffset,
                    Mathf.Sin(angle) * radius));
            }
        }

        private static void AddSpherePattern(List<Vector3> points, int count)
        {
            int silhouetteCount = Mathf.RoundToInt(count * 0.26f);
            int guideCount = Mathf.RoundToInt(count * 0.24f);
            int shellCount = Mathf.RoundToInt(count * 0.34f);
            int coreCount = count - silhouetteCount - guideCount - shellCount;

            AddCircleLayers(points, silhouetteCount, 6, 0.98f, 1.025f, 0.075f);
            AddSphereGuideCurves(points, guideCount);
            AddFibonacciSphereShell(points, shellCount, 0.96f, 0.018f);
            AddFibonacciSphereShell(points, Mathf.Max(0, coreCount), 0.62f, 0.026f);
            TrimToCount(points, count);
        }

        private static void AddFlowerPattern(List<Vector3> points, int count)
        {
            int outlineCount = Mathf.RoundToInt(count * 0.46f);
            int interiorCount = count - outlineCount;
            AddParametricLayers(points, outlineCount, 5, 0.94f, 1.04f, FlowerPoint, 0.12f, 0.02f);
            AddParametricLayers(points, interiorCount, 10, 0.2f, 0.82f, FlowerPoint, 0.28f, 0.28f);
            TrimToCount(points, count);
        }

        private static void AddStarPattern(List<Vector3> points, int count)
        {
            int outlineCount = Mathf.RoundToInt(count * 0.5f);
            int interiorCount = count - outlineCount;
            AddParametricLayers(points, outlineCount, 5, 0.95f, 1.03f, StarPoint, 0.08f, 0.01f);
            AddParametricLayers(points, interiorCount, 9, 0.18f, 0.82f, StarPoint, 0.24f, 0.22f);
            TrimToCount(points, count);
        }

        private static void AddMobiusPattern(List<Vector3> points, int count)
        {
            int edgeCount = Mathf.RoundToInt(count * 0.46f);
            int stripeCount = count - edgeCount;
            int edgeSideCount = Mathf.Max(1, edgeCount / 2);

            AddMobiusEdge(points, edgeSideCount, -1f);
            AddMobiusEdge(points, edgeCount - edgeSideCount, 1f);
            AddMobiusStripes(points, stripeCount);
            TrimToCount(points, count);
        }

        private static void AddMobiusEdge(List<Vector3> points, int pointCount, float side)
        {
            if (pointCount <= 0)
            {
                return;
            }

            float halfWidth = 0.34f;
            for (int i = 0; i < pointCount; i++)
            {
                float t = pointCount <= 1 ? 0f : i / (float)pointCount;
                points.Add(MobiusPoint(t, side * halfWidth));
            }
        }

        private static void AddMobiusStripes(List<Vector3> points, int pointCount)
        {
            if (pointCount <= 0)
            {
                return;
            }

            int stripes = 9;
            int perStripe = Mathf.Max(16, Mathf.CeilToInt(pointCount / (float)stripes));
            int targetCount = points.Count + pointCount;
            float halfWidth = 0.34f;

            for (int stripe = 0; stripe < stripes && points.Count < targetCount; stripe++)
            {
                float stripeT = stripes <= 1 ? 0.5f : stripe / (float)(stripes - 1);
                float v = Mathf.Lerp(-halfWidth * 0.72f, halfWidth * 0.72f, stripeT);
                float phase = stripe * 0.019f;

                for (int i = 0; i < perStripe && points.Count < targetCount; i++)
                {
                    float t = Mathf.Repeat((i + 0.5f) / perStripe + phase, 1f);
                    points.Add(MobiusPoint(t, v));
                }
            }
        }

        private static Vector3 MobiusPoint(float t, float v)
        {
            float u = t * Mathf.PI * 2f;
            float twist = u * 0.5f;
            float major = 0.78f;
            float radial = major + v * Mathf.Cos(twist);
            return new Vector3(
                Mathf.Cos(u) * radial,
                v * Mathf.Sin(twist),
                Mathf.Sin(u) * radial);
        }

        private static Vector3 FlowerPoint(float t)
        {
            float angle = t * Mathf.PI * 2f;
            float radius = 0.35f + Mathf.Abs(Mathf.Cos(angle * 3f)) * 0.7f;
            return new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, Mathf.Sin(angle * 6f) * 0.08f);
        }

        private static void AddParametricLayers(
            List<Vector3> points,
            int pointCount,
            int layers,
            float minScale,
            float maxScale,
            Func<float, Vector3> sampler,
            float depth,
            float phaseStep)
        {
            if (pointCount <= 0)
            {
                return;
            }

            layers = Mathf.Max(1, layers);
            int perLayer = Mathf.Max(8, Mathf.CeilToInt(pointCount / (float)layers));
            int targetCount = points.Count + pointCount;
            for (int layer = 0; layer < layers && points.Count < targetCount; layer++)
            {
                float layerT = layers <= 1 ? 1f : layer / (float)(layers - 1);
                float shapeScale = Mathf.Lerp(minScale, maxScale, layerT);
                float z = Mathf.Lerp(-depth, depth, layerT) + Mathf.Sin(layer * 1.7f) * depth * 0.18f;
                float phase = layer * phaseStep;

                for (int i = 0; i < perLayer && points.Count < targetCount; i++)
                {
                    float t = Mathf.Repeat((i + 0.5f) / perLayer + phase, 1f);
                    Vector3 p = sampler(t) * shapeScale;
                    p.z += z;
                    points.Add(p);
                }
            }
        }

        private static void AddCircleLayers(List<Vector3> points, int pointCount, int layers, float minRadius, float maxRadius, float depth)
        {
            if (pointCount <= 0)
            {
                return;
            }

            layers = Mathf.Max(1, layers);
            int perLayer = Mathf.Max(12, Mathf.CeilToInt(pointCount / (float)layers));
            int targetCount = points.Count + pointCount;
            for (int layer = 0; layer < layers && points.Count < targetCount; layer++)
            {
                float layerT = layers <= 1 ? 1f : layer / (float)(layers - 1);
                float radius = Mathf.Lerp(minRadius, maxRadius, layerT);
                float z = Mathf.Lerp(-depth, depth, layerT);
                float phase = layer * 0.037f;

                for (int i = 0; i < perLayer && points.Count < targetCount; i++)
                {
                    float angle = ((i + 0.5f) / perLayer + phase) * Mathf.PI * 2f;
                    points.Add(new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, z));
                }
            }
        }

        private static void AddSphereLatitudeRings(List<Vector3> points, int pointCount, int ringCount)
        {
            if (pointCount <= 0)
            {
                return;
            }

            ringCount = Mathf.Max(3, ringCount);
            int used = 0;
            for (int ring = 0; ring < ringCount && used < pointCount; ring++)
            {
                float v = (ring + 0.5f) / ringCount;
                float y = Mathf.Lerp(-0.92f, 0.92f, v);
                float radius = Mathf.Sqrt(Mathf.Max(0f, 1f - y * y));
                int remaining = pointCount - used;
                int samples = Mathf.Min(remaining, Mathf.Max(12, Mathf.RoundToInt(radius * pointCount / ringCount * 1.6f)));
                float phase = ring * 0.061f;

                for (int i = 0; i < samples; i++)
                {
                    float angle = ((i + 0.5f) / samples + phase) * Mathf.PI * 2f;
                    points.Add(new Vector3(Mathf.Cos(angle) * radius, y, Mathf.Sin(angle) * radius));
                }

            used += samples;
        }
    }

        private static void AddSphereGuideCurves(List<Vector3> points, int pointCount)
        {
            if (pointCount <= 0)
            {
                return;
            }

            int curveCount = 7;
            int perCurve = Mathf.Max(16, Mathf.CeilToInt(pointCount / (float)curveCount));
            int targetCount = points.Count + pointCount;
            for (int curve = 0; curve < curveCount && points.Count < targetCount; curve++)
            {
                Quaternion rotation = ResolveSphereGuideRotation(curve);
                float radius = curve < 3 ? 1f : Mathf.Lerp(0.38f, 0.82f, (curve - 3) / 3f);
                float y = curve < 3 ? 0f : Mathf.Lerp(-0.52f, 0.52f, (curve - 3) / 3f);
                float phase = curve * 0.031f;

                for (int i = 0; i < perCurve && points.Count < targetCount; i++)
                {
                    float angle = ((i + 0.5f) / perCurve + phase) * Mathf.PI * 2f;
                    Vector3 point = new Vector3(Mathf.Cos(angle) * radius, y, Mathf.Sin(angle) * radius);
                    if (curve < 3)
                    {
                        point = rotation * point;
                    }

                    points.Add(point);
                }
            }
        }

        private static Quaternion ResolveSphereGuideRotation(int curve)
        {
            switch (curve)
            {
                case 0:
                    return Quaternion.identity;
                case 1:
                    return Quaternion.Euler(90f, 0f, 0f);
                case 2:
                    return Quaternion.Euler(0f, 90f, 0f);
                default:
                    return Quaternion.identity;
            }
        }

        private static void AddFibonacciSphereShell(List<Vector3> points, int pointCount, float radius, float wave)
        {
            if (pointCount <= 0)
            {
                return;
            }

            for (int i = 0; i < pointCount; i++)
            {
                float t = pointCount <= 1 ? 0.5f : (i + 0.5f) / pointCount;
                float y = 1f - 2f * t;
                float ringRadius = Mathf.Sqrt(Mathf.Max(0f, 1f - y * y));
                float angle = i * GoldenAngle;
                float shellWave = 1f + Mathf.Sin(i * 0.47f) * wave;

                points.Add(new Vector3(
                    Mathf.Cos(angle) * ringRadius * radius * shellWave,
                    y * radius * shellWave,
                    Mathf.Sin(angle) * ringRadius * radius * shellWave));
            }
        }

        private static void TrimToCount(List<Vector3> points, int count)
        {
            if (points.Count > count)
            {
                points.RemoveRange(count, points.Count - count);
            }
        }

        private static Vector3 StarVertex(int index)
        {
            float angle = -Mathf.PI * 0.5f + (index % 10) * Mathf.PI / 5f;
            float radius = index % 2 == 0 ? 1f : 0.42f;
            return new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
        }

        private static void CenterAndScale(List<Vector3> points, float scale)
        {
            if (points.Count == 0)
            {
                return;
            }

            Bounds bounds = new Bounds(points[0], Vector3.zero);
            for (int i = 1; i < points.Count; i++)
            {
                bounds.Encapsulate(points[i]);
            }

            Vector3 center = bounds.center;
            for (int i = 0; i < points.Count; i++)
            {
                points[i] = (points[i] - center) * scale;
            }
        }

        private static List<Vector3> ExpandPoints(List<Vector3> basePoints, int pointBudget, float scatter)
        {
            if (basePoints.Count == 0)
            {
                basePoints.Add(Vector3.zero);
            }

            List<Vector3> expanded = new List<Vector3>(pointBudget);
            for (int i = 0; i < pointBudget; i++)
            {
                Vector3 point = basePoints[i % basePoints.Count];
                float angle = i * GoldenAngle;
                float radius = Mathf.Sqrt(Hash01(i, 41)) * scatter;
                Vector2 jitter = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                float depthJitter = Mathf.Lerp(-scatter, scatter, Hash01(i, 43)) * 0.35f;
                expanded.Add(point + new Vector3(jitter.x, jitter.y, depthJitter));
            }

            return expanded;
        }

        private static float Hash01(int index, int salt)
        {
            unchecked
            {
                uint hash = (uint)index;
                hash ^= (uint)salt * 0x9E3779B9u;
                hash ^= hash >> 16;
                hash *= 0x7FEB352Du;
                hash ^= hash >> 15;
                hash *= 0x846CA68Bu;
                hash ^= hash >> 16;
                return (hash & 0x00FFFFFFu) / 16777215f;
            }
        }

    }
}
