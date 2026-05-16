using System.Collections;
using UnityEngine;

namespace ButterflyHouse.Plants
{
    /// <summary>
    /// Controls visual properties of generative plants.
    /// Handles breathing, swaying, and pulse effects.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class PlantVisualController : MonoBehaviour
    {
        [Header("Renderer")]
        [SerializeField] private Renderer plantRenderer;
        [SerializeField] private string oscillationProperty = "_Oscillation";
        [SerializeField] private string pulseProperty = "_PulseIntensity";
        [SerializeField] private string pulseCenterProperty = "_PulseCenter";
        
        [Header("Breathing")]
        [Range(0.5f, 3f)]
        [SerializeField] private float breathingSpeed = 1f;
        [Range(0f, 0.2f)]
        [SerializeField] private float breathingAmplitude = 0.05f;
        
        private MaterialPropertyBlock _mpb;
        private float _oscillation = 0f;
        private Vector3 _pulseCenter;
        private float _pulseIntensity = 0f;
        
        private void Awake()
        {
            if (plantRenderer == null)
                plantRenderer = GetComponent<Renderer>();
            
            _mpb = new MaterialPropertyBlock();
        }
        
        private void Update()
        {
            // Continuous breathing effect
            float breathing = Mathf.Sin(Time.time * breathingSpeed) * breathingAmplitude;
            
            // Fade pulse over time
            _pulseIntensity = Mathf.Lerp(_pulseIntensity, 0f, Time.deltaTime * 2f);
            
            // Update shader properties
            plantRenderer.GetPropertyBlock(_mpb);
            _mpb.SetFloat(oscillationProperty, _oscillation + breathing);
            _mpb.SetFloat(pulseProperty, _pulseIntensity);
            _mpb.SetVector(pulseCenterProperty, _pulseCenter);
            plantRenderer.SetPropertyBlock(_mpb);
        }
        
        /// <summary>
        /// Set the oscillation amount (for swaying).
        /// </summary>
        public void SetOscillation(float oscillation)
        {
            _oscillation = oscillation;
        }
        
        /// <summary>
        /// Pulse the plant at a specific point (visual feedback for touch).
        /// </summary>
        public void PulseAtPoint(Vector3 point)
        {
            _pulseCenter = transform.InverseTransformPoint(point);
            _pulseIntensity = 1f;
            
            StartCoroutine(PulseCoroutine());
        }
        
        private IEnumerator PulseCoroutine()
        {
            float duration = 0.5f;
            float elapsed = 0f;
            float startIntensity = _pulseIntensity;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                // Pulse curve: quick rise, slow fall
                _pulseIntensity = Mathf.Lerp(startIntensity, 0f, t * t);
                
                yield return null;
            }
            
            _pulseIntensity = 0f;
        }
        
        /// <summary>
        /// Pulse the entire plant uniformly.
        /// </summary>
        public void Pulse()
        {
            _pulseCenter = Vector3.zero;
            _pulseIntensity = 1f;
            
            StartCoroutine(PulseCoroutine());
        }
    }
}

