using UnityEngine;

namespace ButterflyHouse.Flowers
{
    /// <summary>
    /// Controls visual properties of flowers using MaterialPropertyBlock.
    /// Handles petal animations, emission, pollination effects.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class FlowerVisualController : MonoBehaviour
    {
        [Header("Renderer")]
        [SerializeField] private Renderer flowerRenderer;
        [SerializeField] private string baseColorProperty = "_BaseColor";
        [SerializeField] private string emissionProperty = "_EmissionStrength";
        [SerializeField] private string petalOpenProperty = "_PetalOpen";
        [SerializeField] private string nectarPulseProperty = "_NectarPulse";
        
        [Header("Visual Effects")]
        [SerializeField] private ParticleSystem pollinationBurst;
        [SerializeField] private float pulseDecaySpeed = 5f;
        
        private MaterialPropertyBlock _mpb;
        private Flower.FlowerStage _currentStage = Flower.FlowerStage.Bud;
        private float _nectarPulseValue = 0f;
        
        [Header("Stage Colors")]
        [SerializeField] private Color budColor = new Color(0.8f, 0.9f, 0.5f, 1f); // Light green-yellow
        [SerializeField] private Color bloomColor = new Color(1f, 0.7f, 0.9f, 1f); // Pink
        [SerializeField] private Color radiantColor = new Color(1f, 0.5f, 0.3f, 1f); // Orange-red
        [SerializeField] private Color metaColor = new Color(0.9f, 0.3f, 1f, 1f); // Purple-pink
        
        private void Awake()
        {
            if (flowerRenderer == null)
                flowerRenderer = GetComponent<Renderer>();
            
            _mpb = new MaterialPropertyBlock();
            
            if (pollinationBurst == null)
                pollinationBurst = GetComponentInChildren<ParticleSystem>();
        }
        
        private void Update()
        {
            // Decay nectar pulse
            if (_nectarPulseValue > 0f)
            {
                _nectarPulseValue = Mathf.Max(0f, _nectarPulseValue - Time.deltaTime * pulseDecaySpeed);
                
                flowerRenderer.GetPropertyBlock(_mpb);
                if (flowerRenderer.sharedMaterial != null && flowerRenderer.sharedMaterial.HasProperty(nectarPulseProperty))
                {
                    _mpb.SetFloat(nectarPulseProperty, _nectarPulseValue);
                }
                flowerRenderer.SetPropertyBlock(_mpb);
            }
            
            // Update petal breathing/pulsing for Radiant and Meta stages
            if (_currentStage == Flower.FlowerStage.Radiant || _currentStage == Flower.FlowerStage.Meta)
            {
                UpdatePetalBreathing();
            }
        }
        
        private void UpdatePetalBreathing()
        {
            float breathingPhase = Time.time * 2f; // Breathing frequency
            float breathingAmplitude = _currentStage == Flower.FlowerStage.Meta ? 0.2f : 0.1f;
            float petalOpenVariation = Mathf.Sin(breathingPhase) * breathingAmplitude;
            
            flowerRenderer.GetPropertyBlock(_mpb);
            float basePetalOpen = GetBasePetalOpen(_currentStage);
            if (flowerRenderer.sharedMaterial != null && flowerRenderer.sharedMaterial.HasProperty(petalOpenProperty))
            {
                _mpb.SetFloat(petalOpenProperty, basePetalOpen + petalOpenVariation);
            }
            flowerRenderer.SetPropertyBlock(_mpb);
        }
        
        /// <summary>
        /// Called when flower stage changes.
        /// </summary>
        public void OnStageChanged(Flower.FlowerStage stage)
        {
            _currentStage = stage;
            
            Color stageColor = GetStageColor(stage);
            float petalOpen = GetBasePetalOpen(stage);
            float emission = GetStageEmission(stage);
            
            SetColor(stageColor);
            SetPetalOpen(petalOpen);
            SetEmission(emission);
        }
        
        private Color GetStageColor(Flower.FlowerStage stage)
        {
            switch (stage)
            {
                case Flower.FlowerStage.Bud:
                    return budColor;
                    
                case Flower.FlowerStage.Bloom:
                    return bloomColor;
                    
                case Flower.FlowerStage.Radiant:
                    return radiantColor;
                    
                case Flower.FlowerStage.Meta:
                    return metaColor;
                    
                default:
                    return Color.white;
            }
        }
        
        private float GetBasePetalOpen(Flower.FlowerStage stage)
        {
            switch (stage)
            {
                case Flower.FlowerStage.Bud:
                    return 0f; // Closed
                    
                case Flower.FlowerStage.Bloom:
                    return 0.5f; // Half open
                    
                case Flower.FlowerStage.Radiant:
                    return 0.9f; // Nearly fully open
                    
                case Flower.FlowerStage.Meta:
                    return 1.2f; // Over-extended / fractal
                    
                default:
                    return 0f;
            }
        }
        
        private float GetStageEmission(Flower.FlowerStage stage)
        {
            switch (stage)
            {
                case Flower.FlowerStage.Bud:
                    return 0.3f; // Low emission
                    
                case Flower.FlowerStage.Bloom:
                    return 0.6f; // Moderate emission
                    
                case Flower.FlowerStage.Radiant:
                    return 1.2f; // Strong bioluminescence
                    
                case Flower.FlowerStage.Meta:
                    return 1.8f; // Maximum emission
                    
                default:
                    return 0.5f;
            }
        }
        
        /// <summary>
        /// Set base color of the flower.
        /// </summary>
        public void SetColor(Color color)
        {
            if (flowerRenderer == null) return;
            
            flowerRenderer.GetPropertyBlock(_mpb);
            if (flowerRenderer.sharedMaterial != null)
            {
                if (flowerRenderer.sharedMaterial.HasProperty(baseColorProperty))
                    _mpb.SetColor(baseColorProperty, color);
                else if (flowerRenderer.sharedMaterial.HasProperty("_Color"))
                    _mpb.SetColor("_Color", color);
            }
            flowerRenderer.SetPropertyBlock(_mpb);
        }
        
        /// <summary>
        /// Set petal open amount (0 = closed, 1 = fully open).
        /// </summary>
        public void SetPetalOpen(float value)
        {
            if (flowerRenderer == null) return;
            
            flowerRenderer.GetPropertyBlock(_mpb);
            if (flowerRenderer.sharedMaterial != null && flowerRenderer.sharedMaterial.HasProperty(petalOpenProperty))
            {
                _mpb.SetFloat(petalOpenProperty, value);
            }
            flowerRenderer.SetPropertyBlock(_mpb);
        }
        
        /// <summary>
        /// Set emission strength (0-1).
        /// </summary>
        public void SetEmission(float value)
        {
            if (flowerRenderer == null) return;
            
            flowerRenderer.GetPropertyBlock(_mpb);
            if (flowerRenderer.sharedMaterial != null)
            {
                if (flowerRenderer.sharedMaterial.HasProperty(emissionProperty))
                    _mpb.SetFloat(emissionProperty, value);
                else if (flowerRenderer.sharedMaterial.HasProperty("_Emission"))
                    _mpb.SetFloat("_Emission", value);
            }
            flowerRenderer.SetPropertyBlock(_mpb);
        }
        
        /// <summary>
        /// Called when butterfly sips nectar (subtle pulse).
        /// </summary>
        public void OnNectarSipped()
        {
            _nectarPulseValue = 1f;
            
            flowerRenderer.GetPropertyBlock(_mpb);
            if (flowerRenderer.sharedMaterial != null && flowerRenderer.sharedMaterial.HasProperty(nectarPulseProperty))
            {
                _mpb.SetFloat(nectarPulseProperty, _nectarPulseValue);
            }
            flowerRenderer.SetPropertyBlock(_mpb);
        }
        
        /// <summary>
        /// Called when flower is pollinated (particle burst).
        /// </summary>
        public void OnPollinatedBurst()
        {
            // Play particle burst
            if (pollinationBurst != null)
            {
                pollinationBurst.Play();
            }
            
            // Brief emission spike
            float currentEmission = GetStageEmission(_currentStage);
            StartCoroutine(EmissionBurstCoroutine(currentEmission, currentEmission * 1.5f, 0.3f));
        }
        
        private System.Collections.IEnumerator EmissionBurstCoroutine(float start, float peak, float duration)
        {
            float elapsed = 0f;
            
            // Rise to peak
            while (elapsed < duration * 0.5f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (duration * 0.5f);
                float current = Mathf.Lerp(start, peak, t);
                SetEmission(current);
                yield return null;
            }
            
            // Return to base
            elapsed = duration * 0.5f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = (elapsed - duration * 0.5f) / (duration * 0.5f);
                float current = Mathf.Lerp(peak, start, t);
                SetEmission(current);
                yield return null;
            }
            
            SetEmission(start);
        }
    }
}

