using System.Linq;
using UnityEngine;

public class WashiLightController : MonoBehaviour
{
    private enum ChannelIndex
    {
        R,
        G,
        B,
        A
    }

    [SerializeField] private Vector3 size = new(4f, 2f, 0.5f);
    [SerializeField] private Material referenceMaterial;
    [SerializeField] private Texture2D texture;
    [SerializeField] private ChannelIndex channelIndex = ChannelIndex.R;
    [SerializeField] private float minIntensity = 0.5f;
    [SerializeField] private float maxIntensity = 2f;
    [SerializeField, Min(0.01f)] private float randomDuration = 1f;
    [SerializeField, Min(0.01f)] private float presenceRandomDuration = 5f;
    [SerializeField, Range(0f, 1f)] private float presenceAmount = 0.5f;
    [SerializeField] private Renderer[] renderers;
    [SerializeField] private bool displayAffectedRenderers;

#if UNITY_EDITOR
    [SerializeField] private Shader shaderSearchFilter;
    [SerializeField] private string objectsSearchFilter = "WashiLight";
#endif

    private Material instancedMaterial;
    private Vector3 worldU = Vector3.right;
    private Vector3 worldV = Vector3.up;
    private Vector4 tilingOffset = new(1f, 1f, 0f, 0f);
    private float intensitySeed;
    private float presenceSeed;

    private Material InstancedMaterial
    {
        get
        {
            if (instancedMaterial == null && referenceMaterial != null)
            {
                instancedMaterial = Instantiate(referenceMaterial);
                instancedMaterial.name = $"{gameObject.name}_{referenceMaterial.name}_Instance";
            }

            return instancedMaterial;
        }
    }

    private void OnValidate()
    {
        size.x = Mathf.Abs(size.x);
        size.y = Mathf.Abs(size.y);
        size.z = Mathf.Abs(size.z);
    }

    private void Start()
    {
        intensitySeed = Random.value * 100f;
        presenceSeed = Random.value * 100f;
        ApplyTexture();
    }

    private void Update()
    {
        Material material = InstancedMaterial;
        if (material == null)
        {
            return;
        }

        float intensity = Mathf.Lerp(minIntensity, maxIntensity, Mathf.PerlinNoise(Time.time / randomDuration, intensitySeed));
        bool isPresent = presenceAmount >= 1f || Mathf.PerlinNoise(Time.time / presenceRandomDuration, presenceSeed) < presenceAmount;
        material.SetFloat("_Intensity", intensity);

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].enabled = isPresent;
            }
        }
    }

    [ContextMenu("Apply Texture")]
    private void ApplyTexture()
    {
        Material material = InstancedMaterial;
        if (material == null || renderers == null)
        {
            return;
        }

        transform.localScale = Vector3.one;
        worldU = transform.right;
        worldV = transform.up;

        Vector3 start = transform.TransformPoint(-0.5f * size);
        Vector3 end = transform.TransformPoint(0.5f * size);
        Vector2 start2D = new(Vector3.Dot(start, worldU), Vector3.Dot(start, worldV));
        Vector2 end2D = new(Vector3.Dot(end, worldU), Vector3.Dot(end, worldV));

        tilingOffset = new Vector4(end2D.x - start2D.x, end2D.y - start2D.y, start2D.x, start2D.y);
        material.SetVector("_World_U", worldU);
        material.SetVector("_World_V", worldV);
        material.SetVector("_TilingOffset", tilingOffset);
        material.SetTexture("_Texture", texture);
        material.SetFloat("_Texture_Channel", (int)channelIndex);

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].sharedMaterial = material;
            }
        }
    }

#if UNITY_EDITOR
    private void GetRenderers()
    {
        GetRenderersActive(false);
    }

    private void GetRenderersActive(bool includeInactive)
    {
        transform.localScale = Vector3.one;
        Bounds bounds = BuildBounds();
        FindObjectsInactive inactiveMode = includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude;

        renderers = FindObjectsByType<Renderer>(inactiveMode, FindObjectsSortMode.None)
            .Where(renderer => renderer != null
                && renderer.bounds.Intersects(bounds)
                && renderer.gameObject.name.Contains(objectsSearchFilter)
                && (shaderSearchFilter == null || renderer.sharedMaterials.Any(material => material != null && material.shader == shaderSearchFilter)))
            .ToArray();
    }

    private void GetAndEnableFacingRenderers()
    {
        GetRenderersActive(true);

        foreach (Renderer rendererTarget in renderers)
        {
            if (rendererTarget != null)
            {
                rendererTarget.gameObject.SetActive(Vector3.Dot(rendererTarget.transform.forward, transform.forward) < 0f);
            }
        }

        renderers = renderers.Where(rendererTarget => rendererTarget != null && rendererTarget.gameObject.activeSelf).ToArray();
    }

    [ContextMenu("Get Matching Renderers In Volume")]
    private void ContextGetRenderers()
    {
        GetRenderers();
    }

    [ContextMenu("Get Matching Facing Renderers In Volume")]
    private void ContextGetAndEnableFacingRenderers()
    {
        GetAndEnableFacingRenderers();
    }
#endif

    private Bounds BuildBounds()
    {
        Bounds bounds = new(transform.position, Vector3.zero);
        float x = size.x * 0.5f;
        float y = size.y * 0.5f;
        float z = size.z * 0.5f;

        bounds.Encapsulate(transform.TransformPoint(new Vector3(x, y, z)));
        bounds.Encapsulate(transform.TransformPoint(new Vector3(-x, y, z)));
        bounds.Encapsulate(transform.TransformPoint(new Vector3(x, -y, z)));
        bounds.Encapsulate(transform.TransformPoint(new Vector3(-x, -y, z)));
        bounds.Encapsulate(transform.TransformPoint(new Vector3(x, y, -z)));
        bounds.Encapsulate(transform.TransformPoint(new Vector3(-x, y, -z)));
        bounds.Encapsulate(transform.TransformPoint(new Vector3(x, -y, -z)));
        bounds.Encapsulate(transform.TransformPoint(new Vector3(-x, -y, -z)));
        return bounds;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        if (displayAffectedRenderers && renderers != null)
        {
            foreach (Renderer rendererTarget in renderers)
            {
                if (rendererTarget == null || !rendererTarget.enabled || !rendererTarget.gameObject.activeSelf)
                {
                    continue;
                }

                Mesh mesh = rendererTarget.GetComponent<MeshFilter>()?.sharedMesh;
                if (mesh != null)
                {
                    Gizmos.matrix = rendererTarget.transform.localToWorldMatrix;
                    Gizmos.DrawWireMesh(mesh);
                }
            }
        }

        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, size);
    }
}
