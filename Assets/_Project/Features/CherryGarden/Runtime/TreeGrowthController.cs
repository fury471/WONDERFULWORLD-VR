using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class TreeGrowthController : MonoBehaviour
{
    private static readonly int GrowthProperty = Shader.PropertyToID("_Growth");

    [Header("Renderer")]
    [SerializeField] private Renderer targetRenderer;

    [Header("Growth Materials")]
    [SerializeField] private Material material2_BarkRough;
    [SerializeField] private Material material4_Knot;
    [SerializeField] private Material material3_Bark;
    [SerializeField] private Material material1_Tree;

    [Header("Growth")]
    [SerializeField] private float maxGrowth = 8f;
    [SerializeField, Min(0f)] private float startDelay = 3f;
    [SerializeField, Min(0.01f)] private float barkRoughDuration = 6f;
    [SerializeField, Min(0.01f)] private float knotDuration = 1f;
    [SerializeField, Min(0.01f)] private float barkDuration = 2f;
    [SerializeField, Min(0.01f)] private float treeCrownDuration = 6f;
    [SerializeField, Min(0f)] private float loopWaitTime = 10f;
    [SerializeField] private bool loop = true;

    private readonly Dictionary<Material, List<int>> materialSlots = new();
    private readonly Dictionary<int, MaterialPropertyBlock> propertyBlocks = new();
    private Coroutine growthRoutine;

    private void Awake()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<Renderer>();
        }

        CacheMaterialSlots();
    }

    private void OnValidate()
    {
        maxGrowth = Mathf.Max(0f, maxGrowth);
        startDelay = Mathf.Max(0f, startDelay);
        barkRoughDuration = Mathf.Max(0.01f, barkRoughDuration);
        knotDuration = Mathf.Max(0.01f, knotDuration);
        barkDuration = Mathf.Max(0.01f, barkDuration);
        treeCrownDuration = Mathf.Max(0.01f, treeCrownDuration);
        loopWaitTime = Mathf.Max(0f, loopWaitTime);
    }

    private void OnEnable()
    {
        if (targetRenderer == null)
        {
            Debug.LogWarning($"{nameof(TreeGrowthController)} on {name} has no target renderer.", this);
            enabled = false;
            return;
        }

        CacheMaterialSlots();
        growthRoutine = StartCoroutine(GrowthSequence());
    }

    private void OnDisable()
    {
        if (growthRoutine != null)
        {
            StopCoroutine(growthRoutine);
            growthRoutine = null;
        }
    }

    public void RestartGrowth()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        if (growthRoutine != null)
        {
            StopCoroutine(growthRoutine);
        }

        growthRoutine = StartCoroutine(GrowthSequence());
    }

    public void SetFullyGrown()
    {
        if (targetRenderer == null)
        {
            return;
        }

        CacheMaterialSlots();
        SetGrowthValues(maxGrowth);
    }

    private IEnumerator GrowthSequence()
    {
        do
        {
            SetGrowthValues(0f);

            if (startDelay > 0f)
            {
                yield return new WaitForSeconds(startDelay);
            }

            yield return AnimateGrowth(material2_BarkRough, barkRoughDuration, maxGrowth);
            yield return AnimateGrowth(material4_Knot, knotDuration, maxGrowth);
            yield return AnimateGrowth(material3_Bark, barkDuration, maxGrowth);
            yield return AnimateGrowth(material1_Tree, treeCrownDuration, maxGrowth);

            if (loop && loopWaitTime > 0f)
            {
                yield return new WaitForSeconds(loopWaitTime);
            }
        }
        while (loop);
    }

    private IEnumerator AnimateGrowth(Material material, float duration, float targetValue)
    {
        if (material == null || !materialSlots.ContainsKey(material))
        {
            yield break;
        }

        if (duration <= 0f)
        {
            SetGrowthValue(material, targetValue);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float ratio = Mathf.Clamp01(elapsed / duration);
            SetGrowthValue(material, ratio * targetValue);
            yield return null;
        }

        SetGrowthValue(material, targetValue);
    }

    private void SetGrowthValues(float value)
    {
        SetGrowthValue(material1_Tree, value);
        SetGrowthValue(material2_BarkRough, value);
        SetGrowthValue(material3_Bark, value);
        SetGrowthValue(material4_Knot, value);
    }

    private void SetGrowthValue(Material material, float value)
    {
        if (material == null || targetRenderer == null || !materialSlots.TryGetValue(material, out List<int> slots))
        {
            return;
        }

        for (int i = 0; i < slots.Count; i++)
        {
            int materialIndex = slots[i];
            if (!propertyBlocks.TryGetValue(materialIndex, out MaterialPropertyBlock block))
            {
                block = new MaterialPropertyBlock();
                propertyBlocks.Add(materialIndex, block);
            }

            targetRenderer.GetPropertyBlock(block, materialIndex);
            block.SetFloat(GrowthProperty, value);
            targetRenderer.SetPropertyBlock(block, materialIndex);
        }
    }

    private void CacheMaterialSlots()
    {
        materialSlots.Clear();
        propertyBlocks.Clear();

        if (targetRenderer == null)
        {
            return;
        }

        Material[] materials = targetRenderer.sharedMaterials;
        for (int i = 0; i < materials.Length; i++)
        {
            Material material = materials[i];
            if (material == null)
            {
                continue;
            }

            if (!materialSlots.TryGetValue(material, out List<int> slots))
            {
                slots = new List<int>();
                materialSlots.Add(material, slots);
            }

            slots.Add(i);
        }
    }
}
