using System.Collections;
using UnityEngine;

namespace ButterflyHouse.Butterflies
{
    /// <summary>
    /// Controls visual properties of butterfly wings using MaterialPropertyBlock.
    /// Handles color, emission, wave deformation, and other shader parameters.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class ButterflyVisualController : MonoBehaviour
    {
        [Header("Renderer")]
        [SerializeField] private Renderer wingRenderer;
        [SerializeField] private string baseColorProperty = "_BaseColor";
        [SerializeField] private string emissionProperty = "_EmissionStrength";
        [SerializeField] private string waveAmplitudeProperty = "_WaveAmplitude";
        [SerializeField] private string waveFrequencyProperty = "_WaveFrequency";
        [SerializeField] private string flapFrequencyProperty = "_FlapFrequency";
        
        private MaterialPropertyBlock _mpb;
        private ButterflyArchetype _archetype;
        private float _currentEmission = 0f;

        // Cached shader property IDs — looked up once from the serialized names. Avoids
        // string→hash conversion on every wing-shader write (called by butterfly Update path).
        private int _baseColorId;
        private int _emissionId;
        private int _waveAmplitudeId;
        private int _waveFrequencyId;
        private int _flapFrequencyId;

        private void Awake()
        {
            if (wingRenderer == null)
                wingRenderer = GetComponent<Renderer>();

            _mpb = new MaterialPropertyBlock();
            _baseColorId = Shader.PropertyToID(baseColorProperty);
            _emissionId = Shader.PropertyToID(emissionProperty);
            _waveAmplitudeId = Shader.PropertyToID(waveAmplitudeProperty);
            _waveFrequencyId = Shader.PropertyToID(waveFrequencyProperty);
            _flapFrequencyId = Shader.PropertyToID(flapFrequencyProperty);
        }
        
        /// <summary>
        /// Initialize with an archetype.
        /// </summary>
        public void Initialize(ButterflyArchetype archetype)
        {
            _archetype = archetype;
            
            // Set initial color from gradient
            if (archetype != null && archetype.wingColorGradient != null)
            {
                SetColor(archetype.wingColorGradient.Evaluate(0f));
            }
        }
        
        /// <summary>
        /// Set the base color of the wings.
        /// </summary>
        public void SetColor(Color color)
        {
            if (wingRenderer == null) return;

            wingRenderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(_baseColorId, color);
            wingRenderer.SetPropertyBlock(_mpb);
        }

        /// <summary>
        /// Set emission strength (0-1).
        /// </summary>
        public void SetEmission(float value)
        {
            if (wingRenderer == null) return;

            _currentEmission = Mathf.Clamp01(value);

            wingRenderer.GetPropertyBlock(_mpb);
            _mpb.SetFloat(_emissionId, _currentEmission);
            wingRenderer.SetPropertyBlock(_mpb);
        }

        /// <summary>
        /// Set waveform parameters for visual deformation (sine, saw, square, FM, pure waveform).
        /// </summary>
        public void SetWaveParams(float amplitude, float frequency)
        {
            if (wingRenderer == null) return;

            wingRenderer.GetPropertyBlock(_mpb);
            _mpb.SetFloat(_waveAmplitudeId, amplitude);
            _mpb.SetFloat(_waveFrequencyId, frequency);
            wingRenderer.SetPropertyBlock(_mpb);
        }

        /// <summary>
        /// Set wing flap frequency.
        /// </summary>
        public void SetFlapFrequency(float frequency)
        {
            if (wingRenderer == null) return;

            wingRenderer.GetPropertyBlock(_mpb);
            _mpb.SetFloat(_flapFrequencyId, frequency);
            wingRenderer.SetPropertyBlock(_mpb);
        }
        
        /// <summary>
        /// Pulse the emission for a brief moment.
        /// </summary>
        public void Pulse(float intensity = 1f, float duration = 0.3f)
        {
            StartCoroutine(PulseCoroutine(intensity, duration));
        }
        
        private IEnumerator PulseCoroutine(float intensity, float duration)
        {
            float startEmission = _currentEmission;
            float targetEmission = Mathf.Clamp01(startEmission + intensity);
            
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
        
        /// <summary>
        /// Fade out the butterfly visually.
        /// </summary>
        public IEnumerator FadeOut(float duration)
        {
            wingRenderer.GetPropertyBlock(_mpb);
            Color startColor = _mpb.GetColor(_baseColorId);
            float startEmission = _currentEmission;
            
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                Color currentColor = startColor;
                currentColor.a = Mathf.Lerp(startColor.a, 0f, t);
                SetColor(currentColor);
                
                SetEmission(Mathf.Lerp(startEmission, 0f, t));
                
                yield return null;
            }
            
            // Ensure fully faded
            Color finalColor = startColor;
            finalColor.a = 0f;
            SetColor(finalColor);
            SetEmission(0f);
        }
    }
}

