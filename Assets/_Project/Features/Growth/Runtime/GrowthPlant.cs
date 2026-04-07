using UnityEngine;

public class GrowthPlant : MonoBehaviour
{
    [System.Serializable]
    public class PlantPartBinding
    {
        public string partName;
        public Transform target;
    }

    [Header("Config")]
    [SerializeField] private GrowthProfile_SO growthProfile;
    [SerializeField] private PlantPartBinding[] bindings;
    [SerializeField] private float growthDuration = 1.0f;
    [SerializeField] private bool playOnStart;

    [Header("Debug")]
    [SerializeField] [Range(0f, 1f)] private float currentGrowthTime;

    [Header("Traversal Blocking")]
    [SerializeField] private float blockActivationTime = 0.5f;
    [SerializeField] private Collider[] collidersToEnableOnGrowth;
    private bool blockApplied;


    private float targetGrowthTime;
    private bool isTransitioning;

    public GrowthProfile_SO Profile => growthProfile;

    

    private void Start()
    {
        targetGrowthTime = currentGrowthTime;
        ApplyGrowth(currentGrowthTime);
        ApplyTraversalBlocking();

        if (playOnStart)
        {
            AdvanceStage();
        }
    }

    private void Update()
    {
        if (!isTransitioning)
        {
            return;
        }

        currentGrowthTime = Mathf.MoveTowards(
            currentGrowthTime,
            targetGrowthTime,
            Time.deltaTime / Mathf.Max(0.0001f, growthDuration));

        ApplyGrowth(currentGrowthTime);
        ApplyTraversalBlocking();

        if (Mathf.Approximately(currentGrowthTime, targetGrowthTime))
        {
            currentGrowthTime = targetGrowthTime;
            isTransitioning = false;
            ApplyGrowth(currentGrowthTime);
        }
    }

    public void AdvanceStage()
    {
        float[] stageTimes = GetStageTimes();
        if (stageTimes.Length == 0)
        {
            return;
        }

        for (int i = 0; i < stageTimes.Length; i++)
        {
            if (stageTimes[i] > currentGrowthTime + 0.001f)
            {
                targetGrowthTime = stageTimes[i];
                isTransitioning = true;
                return;
            }
        }

        targetGrowthTime = stageTimes[stageTimes.Length - 1];
        isTransitioning = true;
    }

    public void RegressStage()
    {
        float[] stageTimes = GetStageTimes();
        if (stageTimes.Length == 0)
        {
            return;
        }

        for (int i = stageTimes.Length - 1; i >= 0; i--)
        {
            if (stageTimes[i] < currentGrowthTime - 0.001f)
            {
                targetGrowthTime = stageTimes[i];
                isTransitioning = true;
                return;
            }
        }

        targetGrowthTime = stageTimes[0];
        isTransitioning = true;
    }

    public void SetGrowthTimeImmediate(float value)
    {
        currentGrowthTime = Mathf.Clamp01(value);
        targetGrowthTime = currentGrowthTime;
        isTransitioning = false;
        ApplyGrowth(currentGrowthTime);
    }

    public bool IsTransitioning()
    {
        return isTransitioning;
    }

    private float[] GetStageTimes()
    {
        if (growthProfile == null || growthProfile.Parts == null || growthProfile.Parts.Length == 0)
        {
            return System.Array.Empty<float>();
        }

        GrowthProfile_SO.PartProfile referencePart = growthProfile.Parts[0];
        if (referencePart == null || referencePart.states == null || referencePart.states.Length == 0)
        {
            return System.Array.Empty<float>();
        }

        float[] times = new float[referencePart.states.Length];
        for (int i = 0; i < referencePart.states.Length; i++)
        {
            times[i] = referencePart.states[i].time;
        }

        return times;
    }

    private void ApplyGrowth(float growthTime)
    {
        if (growthProfile == null || growthProfile.Parts == null)
        {
            return;
        }

        growthTime = Mathf.Clamp01(growthTime);

        foreach (var part in growthProfile.Parts)
        {
            if (part == null || part.states == null || part.states.Length == 0)
            {
                continue;
            }

            Transform target = FindTarget(part.partName);
            if (target == null)
            {
                continue;
            }

            ApplyInterpolatedState(target, part.states, growthTime);
        }
    }

    private Transform FindTarget(string partName)
    {
        if (bindings == null)
        {
            return null;
        }

        foreach (var binding in bindings)
        {
            if (binding != null && binding.partName == partName)
            {
                return binding.target;
            }
        }

        return null;
    }

    private void ApplyInterpolatedState(Transform target, GrowthProfile_SO.PartState[] states, float growthTime)
    {
        if (states.Length == 1)
        {
            ApplyState(target, states[0]);
            return;
        }

        if (growthTime <= states[0].time)
        {
            ApplyState(target, states[0]);
            return;
        }

        if (growthTime >= states[states.Length - 1].time)
        {
            ApplyState(target, states[states.Length - 1]);
            return;
        }

        GrowthProfile_SO.PartState fromState = states[0];
        GrowthProfile_SO.PartState toState = states[states.Length - 1];

        for (int i = 0; i < states.Length - 1; i++)
        {
            if (growthTime >= states[i].time && growthTime <= states[i + 1].time)
            {
                fromState = states[i];
                toState = states[i + 1];
                break;
            }
        }

        float range = Mathf.Max(0.0001f, toState.time - fromState.time);
        float t = Mathf.Clamp01((growthTime - fromState.time) / range);

        target.localScale = Vector3.Lerp(fromState.localScale, toState.localScale, t);
        target.localPosition = Vector3.Lerp(fromState.localPosition, toState.localPosition, t);
    }

    private void ApplyState(Transform target, GrowthProfile_SO.PartState state)
    {
        target.localScale = state.localScale;
        target.localPosition = state.localPosition;
    }

    private void ApplyTraversalBlocking()
{
    if (collidersToEnableOnGrowth == null)
    {
        return;
    }

    bool shouldBlock = currentGrowthTime >= blockActivationTime;
    if (shouldBlock == blockApplied)
    {
        return;
    }

    blockApplied = shouldBlock;
    foreach (var col in collidersToEnableOnGrowth)
    {
        if (col != null)
        {
            col.enabled = shouldBlock;
        }
    }
}

}
