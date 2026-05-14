using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class TerrainSeamCoverBuilder : MonoBehaviour
{
    private const float NeighborTolerance = 0.08f;

    [SerializeField] private bool buildOnStart = true;
    [SerializeField, Range(0.01f, 0.4f)] private float seamWidth = 0.12f;
    [SerializeField, Range(0f, 0.12f)] private float surfaceOffset = 0.025f;
    [SerializeField, Range(8, 128)] private int samplesPerSeam = 64;
    [SerializeField] private Color seamColor = new Color(0.34f, 0.49f, 0.35f, 1f);

    private readonly List<GameObject> seamObjects = new();
    private Material seamMaterial;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateRuntimeInstance()
    {
        if (FindFirstObjectByType<TerrainSeamCoverBuilder>() != null)
        {
            return;
        }

        if (Terrain.activeTerrains == null || Terrain.activeTerrains.Length < 2)
        {
            return;
        }

        GameObject root = new GameObject("RuntimeTerrainSeamCovers");
        root.AddComponent<TerrainSeamCoverBuilder>();
    }

    private void Start()
    {
        if (buildOnStart)
        {
            Rebuild();
        }
    }

    private void OnDestroy()
    {
        Clear();
        if (seamMaterial != null)
        {
            Destroy(seamMaterial);
        }
    }

    public void Rebuild()
    {
        Clear();

        Terrain[] terrains = Terrain.activeTerrains;
        if (terrains == null || terrains.Length < 2)
        {
            return;
        }

        for (int i = 0; i < terrains.Length; i++)
        {
            Terrain terrain = terrains[i];
            if (terrain == null || terrain.terrainData == null)
            {
                continue;
            }

            Terrain east = FindNeighbor(terrains, terrain, 1, 0);
            if (east != null)
            {
                BuildEastWestSeam(terrain, east);
            }

            Terrain north = FindNeighbor(terrains, terrain, 0, 1);
            if (north != null)
            {
                BuildSouthNorthSeam(terrain, north);
            }
        }
    }

    private void Clear()
    {
        for (int i = seamObjects.Count - 1; i >= 0; i--)
        {
            if (seamObjects[i] != null)
            {
                Destroy(seamObjects[i]);
            }
        }

        seamObjects.Clear();
    }

    private Terrain FindNeighbor(IReadOnlyList<Terrain> terrains, Terrain source, int xDirection, int zDirection)
    {
        Vector3 sourcePosition = source.transform.position;
        Vector3 sourceSize = source.terrainData.size;
        Vector3 expected = sourcePosition + new Vector3(sourceSize.x * xDirection, 0f, sourceSize.z * zDirection);

        for (int i = 0; i < terrains.Count; i++)
        {
            Terrain candidate = terrains[i];
            if (candidate == null || candidate == source || candidate.terrainData == null)
            {
                continue;
            }

            Vector3 delta = candidate.transform.position - expected;
            if (Mathf.Abs(delta.x) <= NeighborTolerance &&
                Mathf.Abs(delta.y) <= NeighborTolerance &&
                Mathf.Abs(delta.z) <= NeighborTolerance)
            {
                return candidate;
            }
        }

        return null;
    }

    private void BuildEastWestSeam(Terrain west, Terrain east)
    {
        int count = Mathf.Max(2, samplesPerSeam);
        Vector3[] vertices = new Vector3[count * 2];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[(count - 1) * 6];

        Vector3 westPosition = west.transform.position;
        Vector3 westSize = west.terrainData.size;
        float seamX = westPosition.x + westSize.x;
        float zMin = westPosition.z;
        float zMax = westPosition.z + westSize.z;

        for (int i = 0; i < count; i++)
        {
            float t = i / Mathf.Max(1f, count - 1f);
            float z = Mathf.Lerp(zMin, zMax, t);
            float y = Mathf.Max(
                west.SampleHeight(new Vector3(seamX - 0.01f, 0f, z)) + west.transform.position.y,
                east.SampleHeight(new Vector3(seamX + 0.01f, 0f, z)) + east.transform.position.y) + surfaceOffset;

            vertices[i * 2] = new Vector3(seamX - seamWidth * 0.5f, y, z);
            vertices[i * 2 + 1] = new Vector3(seamX + seamWidth * 0.5f, y, z);
            uvs[i * 2] = new Vector2(0f, t);
            uvs[i * 2 + 1] = new Vector2(1f, t);
        }

        FillStripTriangles(triangles, count);
        CreateSeamObject($"TerrainSeam_EW_{west.name}_{east.name}", vertices, uvs, triangles);
    }

    private void BuildSouthNorthSeam(Terrain south, Terrain north)
    {
        int count = Mathf.Max(2, samplesPerSeam);
        Vector3[] vertices = new Vector3[count * 2];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[(count - 1) * 6];

        Vector3 southPosition = south.transform.position;
        Vector3 southSize = south.terrainData.size;
        float seamZ = southPosition.z + southSize.z;
        float xMin = southPosition.x;
        float xMax = southPosition.x + southSize.x;

        for (int i = 0; i < count; i++)
        {
            float t = i / Mathf.Max(1f, count - 1f);
            float x = Mathf.Lerp(xMin, xMax, t);
            float y = Mathf.Max(
                south.SampleHeight(new Vector3(x, 0f, seamZ - 0.01f)) + south.transform.position.y,
                north.SampleHeight(new Vector3(x, 0f, seamZ + 0.01f)) + north.transform.position.y) + surfaceOffset;

            vertices[i * 2] = new Vector3(x, y, seamZ - seamWidth * 0.5f);
            vertices[i * 2 + 1] = new Vector3(x, y, seamZ + seamWidth * 0.5f);
            uvs[i * 2] = new Vector2(0f, t);
            uvs[i * 2 + 1] = new Vector2(1f, t);
        }

        FillStripTriangles(triangles, count);
        CreateSeamObject($"TerrainSeam_NS_{south.name}_{north.name}", vertices, uvs, triangles);
    }

    private static void FillStripTriangles(int[] triangles, int count)
    {
        for (int i = 0; i < count - 1; i++)
        {
            int v = i * 2;
            int tri = i * 6;
            triangles[tri] = v;
            triangles[tri + 1] = v + 2;
            triangles[tri + 2] = v + 1;
            triangles[tri + 3] = v + 1;
            triangles[tri + 4] = v + 2;
            triangles[tri + 5] = v + 3;
        }
    }

    private void CreateSeamObject(string objectName, Vector3[] vertices, Vector2[] uvs, int[] triangles)
    {
        Mesh mesh = new Mesh
        {
            name = objectName + "_Mesh"
        };
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        GameObject seam = new GameObject(objectName);
        seam.transform.SetParent(transform, false);

        MeshFilter filter = seam.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;

        MeshRenderer renderer = seam.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = GetSeamMaterial();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = true;

        seamObjects.Add(seam);
    }

    private Material GetSeamMaterial()
    {
        if (seamMaterial != null)
        {
            return seamMaterial;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        seamMaterial = new Material(shader)
        {
            name = "Runtime Terrain Seam Cover"
        };

        if (seamMaterial.HasProperty("_BaseColor"))
        {
            seamMaterial.SetColor("_BaseColor", seamColor);
        }
        else if (seamMaterial.HasProperty("_Color"))
        {
            seamMaterial.SetColor("_Color", seamColor);
        }

        return seamMaterial;
    }
}
