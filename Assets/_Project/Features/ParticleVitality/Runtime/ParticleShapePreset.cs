using System;
using UnityEngine;

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
