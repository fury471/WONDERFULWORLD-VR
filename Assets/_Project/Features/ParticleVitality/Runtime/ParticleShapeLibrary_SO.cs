using UnityEngine;

[CreateAssetMenu(
    fileName = "ParticleShapeLibrary_SO",
    menuName = "WonderfulWorld/Particle Vitality/Particle Shape Library")]
public class ParticleShapeLibrary_SO : ScriptableObject
{
    [Header("Hold Cloud")]
    [SerializeField] private ParticleShapePreset holdCloud =
        new("Hold Cloud", ParticleShapeSystem.ShapeType.HoldCloud, 0.18f, 0.09f, 1f);

    [Header("Sphere")]
    [SerializeField] private ParticleShapePreset sphere =
        new("Sphere", ParticleShapeSystem.ShapeType.Sphere, 0.25f, 0f, 1f);

    [Header("Heart")]
    [SerializeField] private ParticleShapePreset heart =
        new("Heart", ParticleShapeSystem.ShapeType.Heart, 0.3f, 0f, 1f);

    [Header("Spiral")]
    [SerializeField] private ParticleShapePreset spiral =
        new("Spiral", ParticleShapeSystem.ShapeType.Spiral, 0.28f, 0.4f, 1f);

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
