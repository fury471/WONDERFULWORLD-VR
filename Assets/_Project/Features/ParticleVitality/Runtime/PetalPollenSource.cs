using UnityEngine;

public class PetalPollenSource : MonoBehaviour
{
    [Header("Spawn")]
    [SerializeField] private Transform emissionPoint;
    [SerializeField] private float spawnRadius = 0.18f;
    [SerializeField] private bool emitPetals = true;

    [Header("Look")]
    [SerializeField] private Color pollenColor = new Color(1f, 0.82f, 0.32f, 1f);
    [SerializeField] private Color petalColor = new Color(1f, 0.55f, 0.78f, 1f);

    public bool EmitPetals => emitPetals;
    public Color PollenColor => pollenColor;
    public Color PetalColor => petalColor;

    public Vector3 GetSpawnPosition()
    {
        Transform root = emissionPoint != null ? emissionPoint : transform;
        Vector3 random = Random.insideUnitSphere * spawnRadius;
        random.y = Mathf.Abs(random.y) * 0.55f;
        return root.TransformPoint(random);
    }

    private void OnDrawGizmosSelected()
    {
        Transform root = emissionPoint != null ? emissionPoint : transform;
        Gizmos.color = pollenColor;
        Gizmos.DrawWireSphere(root.position, spawnRadius);
    }
}
