using UnityEngine;

public class LotusGlowController : MonoBehaviour 
{
    [Header("Identity")]
    public int noteId; // A=0, B=1, C=2...

    [Header("Visual References")]
    [SerializeField] private MeshRenderer glowRenderer; 

    [Header("Shader Graph Controls (Optional)")]
    [Tooltip("If your hint shader graph exposes a PulseSpeed float, set its reference name here.")]
    [SerializeField] private string pulseSpeedProperty = "_PulseSpeed";
    [Tooltip("If your hint shader graph exposes a PulseAmount float, set its reference name here.")]
    [SerializeField] private string pulseAmountProperty = "_PulseAmount";
    [Tooltip("If your hint shader graph exposes a GlowIntensity float, set its reference name here.")]
    [SerializeField] private string glowIntensityProperty = "_GlowIntensity";

    [Header("Defaults Applied On Enable")]
    [Tooltip("If enabled, writes the values below into the material when the hint is enabled. Leave off to keep material values as-authored.")]
    [SerializeField] private bool applyShaderDefaultsOnEnable = false;
    [Tooltip("Optional: written to shader graph PulseSpeed when Apply Shader Defaults On Enable is true.")]
    public float pulseSpeed = 2.5f;
    [Tooltip("Optional: written to shader graph PulseAmount when Apply Shader Defaults On Enable is true.")]
    public float pulseAmount = 0.15f;
    [Tooltip("Optional: written to shader graph GlowIntensity when Apply Shader Defaults On Enable is true.")]
    public float glowIntensity = 1.0f;

    private Material instanceMaterial;

    void Awake() 
    {
        if (glowRenderer != null) 
        {
            // Create an instance so each pad can have independent shader parameters.
            instanceMaterial = glowRenderer.material;

            // Ensure the indicator is hidden at the start
            glowRenderer.gameObject.SetActive(false); 
        }
    }

    /// <summary>
    /// Public method used by LotusSongManager to turn the hint on or off.
    /// </summary>
    public void SetGlowActive(bool active)
    {
        if (glowRenderer != null)
        {
            glowRenderer.gameObject.SetActive(active);
            if (active && applyShaderDefaultsOnEnable)
            {
                ApplyShaderDefaults();
            }
        }
    }

    private void ApplyShaderDefaults()
    {
        if (instanceMaterial == null)
            return;

        if (!string.IsNullOrWhiteSpace(pulseSpeedProperty) && instanceMaterial.HasFloat(pulseSpeedProperty))
            instanceMaterial.SetFloat(pulseSpeedProperty, pulseSpeed);

        if (!string.IsNullOrWhiteSpace(pulseAmountProperty) && instanceMaterial.HasFloat(pulseAmountProperty))
            instanceMaterial.SetFloat(pulseAmountProperty, pulseAmount);

        if (!string.IsNullOrWhiteSpace(glowIntensityProperty) && instanceMaterial.HasFloat(glowIntensityProperty))
            instanceMaterial.SetFloat(glowIntensityProperty, glowIntensity);
    }
}
