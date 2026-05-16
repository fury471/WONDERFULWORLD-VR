using UnityEngine;

namespace ButterflyHouse.Plants
{
    /// <summary>
    /// Controls visual properties of fruit using MaterialPropertyBlock.
    /// Handles stage-based colors, emission, and visual effects.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class FruitVisualController : MonoBehaviour
    {
        [Header("Renderer")]
        [SerializeField] private Renderer fruitRenderer;
        [SerializeField] private string baseColorProperty = "_BaseColor";
        [SerializeField] private string emissionProperty = "_EmissionStrength";
        
        private MaterialPropertyBlock _mpb;
        private FruitGrowthSystem.FruitStage _currentStage = FruitGrowthSystem.FruitStage.Seed;
        
        [Header("Stage Colors")]
        [SerializeField] private Color seedColor = new Color(1f, 1f, 1f, 0.5f); // White, low brightness
        [SerializeField] private Color harmonicColor = new Color(0.8f, 0.4f, 1f, 1f); // Purple
        [SerializeField] private Color resonantColor = new Color(0.4f, 1f, 0.8f, 1f); // Cyan-Green
        [SerializeField] private Color celestialColor = new Color(1f, 0.7f, 0.1f, 1f); // Gold-Orange
        
        [Header("Pulse Settings")]
        [SerializeField] private float pulseFrequency = 1f;
        [SerializeField] private float pulseAmplitude = 0.5f;
        private float _pulseTime = 0f;
        
        private void Awake()
        {
            if (fruitRenderer == null)
                fruitRenderer = GetComponent<Renderer>();
            
            _mpb = new MaterialPropertyBlock();
        }
        
        private void Update()
        {
            // Pulse visuals based on stage
            UpdatePulse();
        }
        
        private void UpdatePulse()
        {
            _pulseTime += Time.deltaTime * pulseFrequency;
            
            float emission = 0.5f + Mathf.Sin(_pulseTime) * pulseAmplitude * 0.5f;
            
            // Stage-specific pulse frequency
            switch (_currentStage)
            {
                case FruitGrowthSystem.FruitStage.Seed:
                    emission = 0.3f + Mathf.Sin(_pulseTime * 1f) * 0.2f; // Low brightness
                    break;
                    
                case FruitGrowthSystem.FruitStage.Harmonic:
                    emission = 0.5f + Mathf.Sin(_pulseTime * 2f) * 0.3f; // Pulse frequency doubles
                    break;
                    
                case FruitGrowthSystem.FruitStage.Resonant:
                    emission = 0.6f + Mathf.Sin(_pulseTime * 3f) * 0.4f; // Faster pulse
                    break;
                    
                case FruitGrowthSystem.FruitStage.Celestial:
                    emission = 0.7f + Mathf.Sin(_pulseTime * 4f) * 0.5f; // Dramatic pulses
                    break;
            }
            
            SetEmission(emission);
        }
        
        /// <summary>
        /// Set emission strength (0-1).
        /// </summary>
        public void SetEmission(float value)
        {
            if (fruitRenderer == null) return;
            
            fruitRenderer.GetPropertyBlock(_mpb);
            if (fruitRenderer.sharedMaterial != null)
            {
                if (fruitRenderer.sharedMaterial.HasProperty(emissionProperty))
                    _mpb.SetFloat(emissionProperty, value);
                else if (fruitRenderer.sharedMaterial.HasProperty("_Emission"))
                    _mpb.SetFloat("_Emission", value);
            }
            fruitRenderer.SetPropertyBlock(_mpb);
        }
        
        /// <summary>
        /// Called when fruit stage changes.
        /// </summary>
        public void OnStageChanged(FruitGrowthSystem.FruitStage stage)
        {
            _currentStage = stage;
            
            Color stageColor = GetStageColor(stage);
            SetColor(stageColor);
            
            // Update pulse frequency based on stage
            switch (stage)
            {
                case FruitGrowthSystem.FruitStage.Seed:
                    pulseFrequency = 1f;
                    break;
                    
                case FruitGrowthSystem.FruitStage.Harmonic:
                    pulseFrequency = 2f; // Doubles
                    break;
                    
                case FruitGrowthSystem.FruitStage.Resonant:
                    pulseFrequency = 3f;
                    break;
                    
                case FruitGrowthSystem.FruitStage.Celestial:
                    pulseFrequency = 4f; // Dramatic pulses
                    break;
            }
        }
        
        private Color GetStageColor(FruitGrowthSystem.FruitStage stage)
        {
            switch (stage)
            {
                case FruitGrowthSystem.FruitStage.Seed:
                    return seedColor;
                    
                case FruitGrowthSystem.FruitStage.Harmonic:
                    return harmonicColor;
                    
                case FruitGrowthSystem.FruitStage.Resonant:
                    return resonantColor;
                    
                case FruitGrowthSystem.FruitStage.Celestial:
                    return celestialColor;
                    
                default:
                    return Color.white;
            }
        }
        
        /// <summary>
        /// Set base color of the fruit.
        /// </summary>
        public void SetColor(Color color)
        {
            if (fruitRenderer == null) return;
            
            fruitRenderer.GetPropertyBlock(_mpb);
            if (fruitRenderer.sharedMaterial != null)
            {
                if (fruitRenderer.sharedMaterial.HasProperty(baseColorProperty))
                    _mpb.SetColor(baseColorProperty, color);
                else if (fruitRenderer.sharedMaterial.HasProperty("_Color"))
                    _mpb.SetColor("_Color", color);
            }
            fruitRenderer.SetPropertyBlock(_mpb);
        }
        
        /// <summary>
        /// Pulse the emission for a brief moment.
        /// </summary>
        public void Pulse(float intensity = 1f, float duration = 0.3f)
        {
            StartCoroutine(PulseCoroutine(intensity, duration));
        }
        
        private System.Collections.IEnumerator PulseCoroutine(float intensity, float duration)
        {
            float startEmission = 0.5f;
            float targetEmission = Mathf.Clamp01(0.5f + intensity);
            
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float current = Mathf.Lerp(targetEmission, startEmission, t);
                SetEmission(current);
                yield return null;
            }
            
            SetEmission(startEmission);
        }
    }
}

