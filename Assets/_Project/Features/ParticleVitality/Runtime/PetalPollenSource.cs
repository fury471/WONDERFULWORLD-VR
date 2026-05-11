using UnityEngine;

public class PetalPollenSource : MonoBehaviour
{
    [Header("Spawn")]
    [SerializeField] private Transform emissionPoint;
    [SerializeField] private float spawnRadius = 0.18f;
    [SerializeField] private bool emitPetals = true;

    [Header("Look")]
    [SerializeField] private Color pollenColor = new Color(1f, 0.82f, 0.32f, 1f);
    [SerializeField] private Color petalColor = new Color(1f, 0.74f, 0.86f, 1f);

    [Header("Extraction Feedback")]
    [SerializeField] private Transform pulseVisual;
    [SerializeField] private Renderer pulseRenderer;
    [SerializeField] private float pulseScale = 1.35f;
    [SerializeField] private float pulseReturnSpeed = 7.5f;
    [SerializeField] private float glowBoost = 1.8f;
    [SerializeField] private float focusScale = 1.12f;
    [SerializeField] private float focusReturnSpeed = 5.5f;
    [SerializeField] private float focusGlowBoost = 1.28f;
    [SerializeField] private float focusBreathSpeed = 3.2f;

    private MaterialPropertyBlock propertyBlock;
    private Vector3 pulseBaseScale = Vector3.one;
    private bool hasPulseBaseScale;
    private float extractionPulse;
    private float focusTarget;
    private float focusAmount;

    public bool EmitPetals => emitPetals;
    public Color PollenColor => pollenColor;
    public Color PetalColor => petalColor;

    private void Awake()
    {
        ResolveFeedbackReferences();
    }

    private void Update()
    {
        extractionPulse = Mathf.MoveTowards(extractionPulse, 0f, Time.deltaTime * pulseReturnSpeed);
        focusAmount = Mathf.MoveTowards(focusAmount, focusTarget, Time.deltaTime * focusReturnSpeed);
        focusTarget = 0f;

        if (extractionPulse <= 0.001f && focusAmount <= 0.001f)
        {
            extractionPulse = 0f;
            focusAmount = 0f;
            ApplyFeedback(0f, 0f);
            return;
        }

        ApplyFeedback(extractionPulse, focusAmount);
    }

    public Vector3 GetSpawnPosition()
    {
        Transform root = emissionPoint != null ? emissionPoint : transform;
        Vector3 random = Random.insideUnitSphere * spawnRadius;
        random.y = Mathf.Abs(random.y) * 0.55f;
        return root.TransformPoint(random);
    }

    public void NotifyExtracted(bool petal)
    {
        ResolveFeedbackReferences();
        extractionPulse = Mathf.Clamp01(extractionPulse + (petal ? 0.18f : 0.08f));
        ApplyFeedback(extractionPulse, focusAmount);
    }

    public void SetInteractionFocus(float amount)
    {
        ResolveFeedbackReferences();
        focusTarget = Mathf.Max(focusTarget, Mathf.Clamp01(amount));
    }

    private void ResolveFeedbackReferences()
    {
        if (pulseVisual == null)
        {
            Transform marker = transform.Find("Debug_SourceMarker");
            pulseVisual = marker != null ? marker : emissionPoint;
        }

        if (pulseVisual != null && !hasPulseBaseScale)
        {
            pulseBaseScale = pulseVisual.localScale;
            hasPulseBaseScale = true;
        }

        if (pulseRenderer == null && pulseVisual != null)
        {
            pulseRenderer = pulseVisual.GetComponentInChildren<Renderer>();
        }

        if (pulseRenderer != null && propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }
    }

    private void ApplyFeedback(float pulse, float focus)
    {
        float breath = 0.5f + Mathf.Sin(Time.time * focusBreathSpeed + transform.GetInstanceID() * 0.01f) * 0.5f;
        float focusPulse = focus * Mathf.Lerp(0.82f, 1f, breath);
        float amount = Mathf.Clamp01(pulse + focusPulse * 0.55f);

        if (pulseVisual != null)
        {
            float scale = 1f + focusPulse * (focusScale - 1f) + pulse * (pulseScale - 1f);
            pulseVisual.localScale = pulseBaseScale * scale;
        }

        if (pulseRenderer == null)
        {
            return;
        }

        Color glow = Color.Lerp(pollenColor, petalColor, 0.35f)
            * Mathf.Lerp(1f, focusGlowBoost, focusPulse)
            * Mathf.Lerp(1f, glowBoost, pulse);
        glow.a = 1f;
        pulseRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor("_BaseColor", glow);
        propertyBlock.SetColor("_Color", glow);
        propertyBlock.SetColor("_EmissionColor", glow * Mathf.Lerp(0.2f, 1.2f, amount));
        pulseRenderer.SetPropertyBlock(propertyBlock);
    }

    private void OnDrawGizmosSelected()
    {
        Transform root = emissionPoint != null ? emissionPoint : transform;
        Gizmos.color = pollenColor;
        Gizmos.DrawWireSphere(root.position, spawnRadius);
    }
}
