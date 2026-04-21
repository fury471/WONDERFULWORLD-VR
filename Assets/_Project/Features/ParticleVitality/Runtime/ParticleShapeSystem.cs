using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ParticleShapeSystem : MonoBehaviour
{
    public enum ShapeType
    {
        HoldCloud,
        Sphere,
        Heart,
        Spiral
    }
    public enum lifeCycleType
    {
        Init, // 默认值为 0
        Active,    // 默认值为 1
        nature,     // 默认值为 2
    }

    [Header("References")]
    [SerializeField] private ParticlePreviewAnchor previewAnchor;
    [SerializeField] private ParticleSystem displayParticleSystem;
    [SerializeField] private ParticleShapeLibrary_SO shapeLibrary;

    [Header("Budget")]
    [SerializeField] private int maxParticles = 48;

    [Header("Motion")]
    [SerializeField] private float moveSpeed = 8f;

    [Header("Display Particles")]
    [SerializeField] private float displayParticleLifetime = 999f;
    [SerializeField] private float displayParticleSize = 0.5f;
    [SerializeField] private Color displayParticleColor = Color.white;

    [Header("Selection UI")]
    [SerializeField] private bool showPostCollectOptions;
    [SerializeField] private Vector2 selectionPanelSize = new(260f, 190f);
    [SerializeField] private float selectionUiDistanceFromCamera = 0.8f;
    [SerializeField] private float postCollectOptionsDelaySeconds = 3f;

    [Header("Shape Tuning")]
    [SerializeField] private ShapeType currentShape = ShapeType.HoldCloud;
    [SerializeField] private float holdRadius = 0.18f;
    [SerializeField] private float shapeRadius = 0.25f;
    [SerializeField] private float spiralHeight = 0.4f;
    [SerializeField] private float heartDepth = 0.14f;
    [SerializeField] private float heartPulseAmplitude = 0.08f;
    [SerializeField] private float heartPulseSpeed = 2.2f;

    private readonly List<Vector3> currentWorldPositions = new();
    private readonly List<Vector3> targetLocalPositions = new();
    private ParticleSystem.Particle[] captureBuffer = Array.Empty<ParticleSystem.Particle>();
    private bool allowShapeMotion=false;

    public ParticlePreviewAnchor PreviewAnchor => previewAnchor;
    public ShapeType CurrentShape => currentShape;
    private lifeCycleType currentLifeCycle = lifeCycleType.Init;

    [SerializeField] private float jitterStrength = 0.02f; // 颤动幅度
    [SerializeField] private float jitterSpeed = 5f;      // 颤动频率

    [Header("Juice")]
    [SerializeField] private float globalPulseSpeed = 2f;
    [SerializeField] private float globalPulseAmplitude = 0.1f;


    private void Awake()
    {
        ConfigureDisplayParticleSystem();
    }

  

    public void CaptureParticlesFromSystem(ParticleSystem sourceSystem)
    {
        if (sourceSystem == null)
        {
            ClearParticles();
            return;
        }

        int liveCount = sourceSystem.particleCount;
        if (liveCount <= 0)
        {
            ClearParticles();
            return;
        }

        if (captureBuffer.Length < liveCount)
        {
            captureBuffer = new ParticleSystem.Particle[liveCount];
        }

        int capturedCount = sourceSystem.GetParticles(captureBuffer);

        currentWorldPositions.Clear();
        targetLocalPositions.Clear();

        int sampleCount = Mathf.Min(maxParticles, capturedCount);
        for (int i = 0; i < sampleCount; i++)
        {
            int sourceIndex = Mathf.FloorToInt(i * (capturedCount / (float)sampleCount));
            sourceIndex = Mathf.Clamp(sourceIndex, 0, capturedCount - 1);

            ParticleSystem.MainModule main = sourceSystem.main;

            Vector3 worldPos;
            if (main.simulationSpace == ParticleSystemSimulationSpace.World)
            {
                worldPos = captureBuffer[sourceIndex].position;
            }
            else
            {
                worldPos = sourceSystem.transform.TransformPoint(captureBuffer[sourceIndex].position);
            }
            currentWorldPositions.Add(worldPos);
            targetLocalPositions.Add(Vector3.zero);
        }

    }


    public void SetShape(ShapeType shape)
    {
        allowShapeMotion = true;
        currentLifeCycle = lifeCycleType.Active;
        currentShape = shape;
        RebuildTargets();
    }

    public void CycleShape()
    {
        int nextValue = ((int)currentShape + 1) % System.Enum.GetValues(typeof(ShapeType)).Length;
        SetShape((ShapeType)nextValue);
    }


    public void ClearParticles()
    {
        allowShapeMotion = false;
        currentLifeCycle = lifeCycleType.nature;

        int count = displayParticleSystem.particleCount;
        if (count > 0)
        {
            displayParticleSystem.GetParticles(captureBuffer);
            for (int i = 0; i < count; i++)
            {
                // 给初速度
                captureBuffer[i].velocity = new Vector3(
                        UnityEngine.Random.Range(-0.8f, 0.8f), // 水平晃动
                        UnityEngine.Random.Range(-0.2f, -0.5f), // 缓慢下落
                        UnityEngine.Random.Range(-0.8f, 0.8f)
                    );
            }
            displayParticleSystem.SetParticles(captureBuffer, count);
        }

        currentWorldPositions.Clear();
        targetLocalPositions.Clear();
    }

    private void LateUpdate()
    {

        var main = displayParticleSystem.main;
       
                
        if (lifeCycleType.Active == currentLifeCycle)
        {
            main.simulationSpeed = 0f;
            MoveParticles();
            SyncDisplayParticles(forceResetLifetime: false);
        
        }else if (lifeCycleType.nature == currentLifeCycle)
        {
    
            main.simulationSpeed = 0.5f; 
            
            // 如果所有粒子都自然消失了，可以回到 Init 状态
            if (displayParticleSystem.particleCount == 0)
            {
                currentLifeCycle = lifeCycleType.Init;
            }
        }
        
    }


    private void MoveParticles()
    {
        if (previewAnchor == null)
        {
            return;
        }

        float currentMoveSpeed = GetCurrentMoveSpeed();

        for (int i = 0; i < currentWorldPositions.Count; i++)
        {
            Vector3 targetWorldPosition = GetTargetWorldPosition(targetLocalPositions[i]);

            // 为每个粒子计算一个独特的随机偏移 (Perlin Noise 效果最平滑)
            // 使用 i 作为种子偏移，确保每个粒子的抖动轨迹不同
            float offsetX = (Mathf.PerlinNoise(Time.time * jitterSpeed, i * 0.1f) - 0.5f) * jitterStrength;
            float offsetY = (Mathf.PerlinNoise(Time.time * jitterSpeed, i * 0.2f) - 0.5f) * jitterStrength;
            float offsetZ = (Mathf.PerlinNoise(Time.time * jitterSpeed, i * 0.3f) - 0.5f) * jitterStrength;
            Vector3 jitteredTarget = targetWorldPosition + new Vector3(offsetX, offsetY, offsetZ);
            currentWorldPositions[i] = Vector3.Lerp(
                currentWorldPositions[i],
                jitteredTarget,
                Time.deltaTime * currentMoveSpeed);
        }
       
    }

    private Vector3 GetTargetWorldPosition(Vector3 localTargetPosition)
    {
        if (previewAnchor == null)
        {
            return localTargetPosition;
        }
        // 计算全局脉动系数
        // 使用 Sin 函数随时间产生 0.9 到 1.1 之间的缩放倍率
        float pulse = 1f + Mathf.Sin(Time.time * globalPulseSpeed) * globalPulseAmplitude;
        
        // 应用缩放
        Vector3 dynamicLocalPos = localTargetPosition * pulse;
        return currentShape == ShapeType.HoldCloud
            ? previewAnchor.GetHoldWorldPosition(dynamicLocalPos)
            : previewAnchor.GetShapeWorldPosition(dynamicLocalPos);
    }

    private void RebuildTargets()
    {
        targetLocalPositions.Clear();
        int total = currentWorldPositions.Count;

        for (int i = 0; i < total; i++)
        {
            targetLocalPositions.Add(GetTargetLocalPosition(i, total));
        }
    }

    private Vector3 GetTargetLocalPosition(int index, int total)
    {
        if (total <= 0)
        {
            return Vector3.zero;
        }

        return currentShape switch
        {
            ShapeType.Sphere => GetSpherePosition(index, total),
            ShapeType.Heart => GetHeartPosition(index, total),
            ShapeType.Spiral => GetSpiralPosition(index, total),
            _ => GetHoldCloudPosition(index, total)
        };
    }

    private Vector3 GetHoldCloudPosition(int index, int total)
    {
        float radius = GetShapeRadius(ShapeType.HoldCloud, holdRadius);
        float heightRange = GetShapeHeight(ShapeType.HoldCloud, holdRadius * 0.8f);

        float t = index + 0.5f;
        float count = Mathf.Max(1, total);
        float inclination = Mathf.Acos(1f - 2f * t / count);
        float azimuth = Mathf.PI * (3f - Mathf.Sqrt(5f)) * index;

        float x = Mathf.Sin(inclination) * Mathf.Cos(azimuth);
        float y = Mathf.Cos(inclination);
        float z = Mathf.Sin(inclination) * Mathf.Sin(azimuth);

        Vector3 shellPosition = new Vector3(x, y, z) * radius;
        shellPosition.y *= heightRange / Mathf.Max(0.0001f, radius);
        return shellPosition;
    }

    private Vector3 GetSpherePosition(int index, int total)
    {
        float radius = GetShapeRadius(ShapeType.Sphere, shapeRadius);
        float t = (index + 0.5f) / total;
        float inclination = Mathf.Acos(1f - 2f * t);
        float azimuth = Mathf.PI * (1f + Mathf.Sqrt(5f)) * index;

        float x = Mathf.Sin(inclination) * Mathf.Cos(azimuth);
        float y = Mathf.Cos(inclination);
        float z = Mathf.Sin(inclination) * Mathf.Sin(azimuth);

        return new Vector3(x, y, z) * radius;
    }

    private Vector3 GetHeartPosition(int index, int total)
    {
        float baseScale = GetShapeRadius(ShapeType.Heart, shapeRadius);
        float pulse = 1f + Mathf.Sin(Time.time * heartPulseSpeed) * heartPulseAmplitude;
        float scale = baseScale * pulse;

        int depthLayers = Mathf.Max(2, Mathf.RoundToInt(Mathf.Sqrt(total) * 0.6f));
        int particlesPerLayer = Mathf.Max(1, Mathf.CeilToInt(total / (float)depthLayers));
        int layerIndex = index / particlesPerLayer;
        int indexInLayer = index % particlesPerLayer;

        float layerT = depthLayers <= 1 ? 0.5f : layerIndex / (float)(depthLayers - 1);
        float t = (indexInLayer / Mathf.Max(1f, particlesPerLayer)) * Mathf.PI * 2f;

        float x2D = 16f * Mathf.Pow(Mathf.Sin(t), 3f);
        float y2D =
            13f * Mathf.Cos(t)
            - 5f * Mathf.Cos(2f * t)
            - 2f * Mathf.Cos(3f * t)
            - Mathf.Cos(4f * t);

        float normalization = 17f;
        float x = (x2D / normalization) * scale;
        float y = (y2D / normalization) * scale;
        float z = Mathf.Lerp(-heartDepth * 0.5f, heartDepth * 0.5f, layerT);

        float layerScale = Mathf.Lerp(0.75f, 1f, 1f - Mathf.Abs(layerT - 0.5f) * 2f);
        return new Vector3(x * layerScale, y * layerScale, z);
    }

    private Vector3 GetSpiralPosition(int index, int total)
    {
        float spiralRadius = GetShapeRadius(ShapeType.Spiral, shapeRadius);
        float heightRange = GetShapeHeight(ShapeType.Spiral, spiralHeight);
        float progress = index / Mathf.Max(1f, (float)(total - 1));
        float angle = progress * Mathf.PI * 6f;
        float radius = Mathf.Lerp(spiralRadius * 0.25f, spiralRadius, progress);
        float height = Mathf.Lerp(-heightRange * 0.5f, heightRange * 0.5f, progress);
        return new Vector3(Mathf.Cos(angle) * radius, height, Mathf.Sin(angle) * radius);
    }

    private float GetCurrentMoveSpeed()
    {
        if (TryGetCurrentPreset(out ParticleShapePreset preset))
        {
            return moveSpeed * preset.MoveSpeedMultiplier;
        }

        return moveSpeed;
    }

    private float GetShapeRadius(ShapeType shapeType, float fallback)
    {
        if (shapeLibrary != null && shapeLibrary.TryGetPreset(shapeType, out ParticleShapePreset preset))
        {
            return preset.Radius > 0f ? preset.Radius : fallback;
        }

        return fallback;
    }

    private float GetShapeHeight(ShapeType shapeType, float fallback)
    {
        if (shapeLibrary != null && shapeLibrary.TryGetPreset(shapeType, out ParticleShapePreset preset))
        {
            return Mathf.Approximately(preset.Height, 0f) ? fallback : preset.Height;
        }

        return fallback;
    }

    private bool TryGetCurrentPreset(out ParticleShapePreset preset)
    {
        if (shapeLibrary != null && shapeLibrary.TryGetPreset(currentShape, out preset))
        {
            return true;
        }

        preset = default;
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        if (previewAnchor == null)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        float gizmoRadius = GetShapeRadius(currentShape, shapeRadius);
        Vector3 gizmoCenter = currentShape == ShapeType.HoldCloud
            ? previewAnchor.GetHoldWorldPosition(Vector3.zero)
            : previewAnchor.GetShapeWorldPosition(Vector3.zero);
        Gizmos.DrawWireSphere(gizmoCenter, gizmoRadius);
    }



    private void SyncDisplayParticles(bool forceResetLifetime)
    {
        if (displayParticleSystem == null)
        {
            return;
        }

        EnsureParticleBufferSize(currentWorldPositions.Count);

        for (int i = 0; i < currentWorldPositions.Count; i++)
        {
            captureBuffer[i].position = currentWorldPositions[i];
            captureBuffer[i].startSize = displayParticleSize;
        
            if (forceResetLifetime || captureBuffer[i].remainingLifetime <= 0f)
            {
                    captureBuffer[i].startLifetime = displayParticleLifetime;
                    captureBuffer[i].remainingLifetime = displayParticleLifetime;
                    captureBuffer[i].velocity = Vector3.zero;
                
            }
        }
        // Debug.Log($"当前世界坐标列表的大小为: {currentWorldPositions.Count},{currentLifeCycle}");
        if (!displayParticleSystem.isPlaying)
        {
            displayParticleSystem.Play();
        }
        displayParticleSystem.SetParticles(captureBuffer, currentWorldPositions.Count);
    }

    private void EnsureParticleBufferSize(int count)
    {
        if (captureBuffer.Length < count)
        {
            captureBuffer = new ParticleSystem.Particle[Mathf.Max(count, 1)];
        }
    }

    private void ConfigureDisplayParticleSystem()
    {
        if (displayParticleSystem == null)
        {
            return;
        }

        var main = displayParticleSystem.main;
        main.playOnAwake = false;
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = displayParticleLifetime;
        main.startSize = displayParticleSize;

        displayParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

}

[CreateAssetMenu(
    fileName = "ParticleShapeLibrary_SO",
    menuName = "WonderfulWorld/Particle Vitality/Particle Shape Library")]
public class ParticleShapeLibrary_SO : ScriptableObject
{
    [Header("Hold Cloud")]
    [SerializeField] private ParticleShapePreset holdCloud = new("Hold Cloud", ParticleShapeSystem.ShapeType.HoldCloud, 0.18f, 0.09f, 1f);

    [Header("Sphere")]
    [SerializeField] private ParticleShapePreset sphere = new("Sphere", ParticleShapeSystem.ShapeType.Sphere, 0.25f, 0f, 1f);

    [Header("Heart")]
    [SerializeField] private ParticleShapePreset heart = new("Heart", ParticleShapeSystem.ShapeType.Heart, 0.3f, 0f, 1f);

    [Header("Spiral")]
    [SerializeField] private ParticleShapePreset spiral = new("Spiral", ParticleShapeSystem.ShapeType.Spiral, 0.28f, 0.4f, 1f);

    public bool TryGetPreset(ParticleShapeSystem.ShapeType shapeType, out ParticleShapePreset preset)
    {
        switch (shapeType)
        {
            case ParticleShapeSystem.ShapeType.HoldCloud:
                preset = holdCloud;
                return true;
            case ParticleShapeSystem.ShapeType.Sphere:
                preset = sphere;
                return true;
            case ParticleShapeSystem.ShapeType.Heart:
                preset = heart;
                return true;
            case ParticleShapeSystem.ShapeType.Spiral:
                preset = spiral;
                return true;
            default:
                preset = default;
                return false;
        }
    }
}

[Serializable]
public struct ParticleShapePreset
{
    [SerializeField] private string displayName;
    [SerializeField] private ParticleShapeSystem.ShapeType shapeType;
    [SerializeField] private float radius;
    [SerializeField] private float height;
    [SerializeField] private float moveSpeedMultiplier;

    public ParticleShapePreset(
        string displayName,
        ParticleShapeSystem.ShapeType shapeType,
        float radius,
        float height,
        float moveSpeedMultiplier)
    {
        this.displayName = displayName;
        this.shapeType = shapeType;
        this.radius = radius;
        this.height = height;
        this.moveSpeedMultiplier = moveSpeedMultiplier;
    }

    public string DisplayName => displayName;
    public ParticleShapeSystem.ShapeType ShapeType => shapeType;
    public float Radius => radius;
    public float Height => height;
    public float MoveSpeedMultiplier => moveSpeedMultiplier <= 0f ? 1f : moveSpeedMultiplier;
}