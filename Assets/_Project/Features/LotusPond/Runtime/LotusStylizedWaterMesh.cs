using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public sealed class LotusStylizedWaterMesh : MonoBehaviour
{
    public enum WaterShape
    {
        Lake = 0,
        Channel = 1,
        Waterfall = 2
    }

    [SerializeField] private WaterShape shape = WaterShape.Lake;
    [SerializeField] private Vector3 centerOffset;

    [Header("Lake")]
    [SerializeField, Min(0.01f)] private float width = 8f;
    [SerializeField, Min(0.01f)] private float length = 6f;
    [SerializeField, Min(0f)] private float cornerCut = 0.75f;

    [Header("Channel / Waterfall")]
    [SerializeField] private Vector3 surfaceNormal = Vector3.up;
    [SerializeField, Min(0.01f)] private float startWidth = 1f;
    [SerializeField, Min(0.01f)] private float endWidth = 1f;
    [SerializeField] private bool doubleSided;
    [SerializeField] private Vector3[] localPath =
    {
        new Vector3(0f, 0f, -2f),
        new Vector3(0f, 0f, 2f)
    };

    private Mesh generatedMesh;

    private void OnEnable() => Rebuild();

    private void OnValidate() => Rebuild();

    public void Rebuild()
    {
        Mesh mesh = GetOrCreateMesh();
        if (shape == WaterShape.Lake)
        {
            BuildLake(mesh);
        }
        else
        {
            BuildStrip(mesh);
        }

        GetComponent<MeshFilter>().sharedMesh = mesh;

        if (TryGetComponent(out MeshCollider meshCollider))
        {
            meshCollider.sharedMesh = null;
        }
    }

    private void BuildLake(Mesh mesh)
    {
        float halfWidth = width * 0.5f;
        float halfLength = length * 0.5f;
        float cut = Mathf.Min(cornerCut, halfWidth * 0.85f, halfLength * 0.85f);

        Vector3[] vertices =
        {
            centerOffset + new Vector3(-halfWidth + cut, 0f, -halfLength),
            centerOffset + new Vector3(halfWidth - cut, 0f, -halfLength),
            centerOffset + new Vector3(halfWidth, 0f, -halfLength + cut),
            centerOffset + new Vector3(halfWidth, 0f, halfLength - cut),
            centerOffset + new Vector3(halfWidth - cut, 0f, halfLength),
            centerOffset + new Vector3(-halfWidth + cut, 0f, halfLength),
            centerOffset + new Vector3(-halfWidth, 0f, halfLength - cut),
            centerOffset + new Vector3(-halfWidth, 0f, -halfLength + cut)
        };

        Vector2[] uvs = new Vector2[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            uvs[i] = new Vector2(
                Mathf.InverseLerp(-halfWidth, halfWidth, vertices[i].x - centerOffset.x),
                Mathf.InverseLerp(-halfLength, halfLength, vertices[i].z - centerOffset.z));
        }

        int[] triangles =
        {
            0, 7, 6,
            0, 6, 5,
            0, 5, 4,
            0, 4, 3,
            0, 3, 2,
            0, 2, 1
        };

        ApplyMesh(mesh, vertices, uvs, triangles);
    }

    private void BuildStrip(Mesh mesh)
    {
        if (localPath == null || localPath.Length < 2)
        {
            mesh.Clear();
            return;
        }

        Vector3 normal = surfaceNormal.sqrMagnitude > 0.0001f ? surfaceNormal.normalized : Vector3.up;
        int vertexCount = localPath.Length * 2;
        Vector3[] vertices = new Vector3[vertexCount];
        Vector2[] uvs = new Vector2[vertexCount];
        int segmentCount = localPath.Length - 1;
        int[] triangles = new int[segmentCount * (doubleSided ? 12 : 6)];
        float totalLength = GetPathLength(localPath);
        float travelled = 0f;

        for (int i = 0; i < localPath.Length; i++)
        {
            if (i > 0)
            {
                travelled += Vector3.Distance(localPath[i - 1], localPath[i]);
            }

            float t = localPath.Length > 1 ? i / (float)(localPath.Length - 1) : 0f;
            float pathU = totalLength > 0.0001f ? travelled / totalLength : t;
            Vector3 tangent = GetTangent(localPath, i);
            Vector3 side = Vector3.Cross(normal, tangent);
            if (side.sqrMagnitude < 0.0001f)
            {
                side = Vector3.Cross(Vector3.forward, tangent);
            }

            side.Normalize();
            float half = Mathf.Lerp(startWidth, endWidth, t) * 0.5f;
            Vector3 center = localPath[i] + centerOffset;
            int v = i * 2;
            vertices[v] = center - side * half;
            vertices[v + 1] = center + side * half;
            uvs[v] = new Vector2(0f, pathU);
            uvs[v + 1] = new Vector2(1f, pathU);
        }

        int ti = 0;
        for (int i = 0; i < segmentCount; i++)
        {
            int a = i * 2;
            int b = a + 1;
            int c = a + 2;
            int d = a + 3;

            triangles[ti++] = a;
            triangles[ti++] = c;
            triangles[ti++] = b;
            triangles[ti++] = b;
            triangles[ti++] = c;
            triangles[ti++] = d;

            if (doubleSided)
            {
                triangles[ti++] = a;
                triangles[ti++] = b;
                triangles[ti++] = c;
                triangles[ti++] = b;
                triangles[ti++] = d;
                triangles[ti++] = c;
            }
        }

        ApplyMesh(mesh, vertices, uvs, triangles);
    }

    private Mesh GetOrCreateMesh()
    {
        if (generatedMesh == null)
        {
            generatedMesh = new Mesh
            {
                name = $"{gameObject.name}_StylizedWaterMesh",
                hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild
            };
        }

        return generatedMesh;
    }

    private static void ApplyMesh(Mesh mesh, Vector3[] vertices, Vector2[] uvs, int[] triangles)
    {
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    private static Vector3 GetTangent(Vector3[] path, int index)
    {
        if (index == 0)
        {
            return (path[1] - path[0]).normalized;
        }

        if (index == path.Length - 1)
        {
            return (path[index] - path[index - 1]).normalized;
        }

        return (path[index + 1] - path[index - 1]).normalized;
    }

    private static float GetPathLength(Vector3[] path)
    {
        float length = 0f;
        for (int i = 1; i < path.Length; i++)
        {
            length += Vector3.Distance(path[i - 1], path[i]);
        }

        return length;
    }
}
