using UnityEngine;

namespace ButterflyHouse.Core
{
    /// <summary>
    /// Manages light cycle phases (Dawn, Noon, Dusk, Midnight).
    /// Each phase affects butterfly behavior and visual style.
    /// </summary>
    public class LightCycle : MonoBehaviour
    {
        public enum LightPhase
        {
            Dawn,
            Noon,
            Dusk,
            Midnight
        }
        
        [Header("Cycle Settings")]
        [SerializeField] private float cycleDuration = 300f; // 5 minutes per cycle
        [SerializeField] private LightPhase currentPhase = LightPhase.Dawn;
        
        [Header("Lighting")]
        [SerializeField] private Light directionalLight;
        [SerializeField] private Color dawnColor = new Color(1f, 0.9f, 0.8f, 1f);
        [SerializeField] private Color noonColor = new Color(1f, 1f, 0.95f, 1f);
        [SerializeField] private Color duskColor = new Color(0.8f, 0.6f, 1f, 1f);
        [SerializeField] private Color midnightColor = new Color(0.3f, 0.2f, 0.5f, 1f);
        
        [Header("Ambient Colors")]
        [SerializeField] private Color dawnAmbient = new Color(0.8f, 0.7f, 0.9f, 1f);
        [SerializeField] private Color noonAmbient = new Color(1f, 0.95f, 0.9f, 1f);
        [SerializeField] private Color duskAmbient = new Color(0.6f, 0.5f, 0.7f, 1f);
        [SerializeField] private Color midnightAmbient = new Color(0.2f, 0.15f, 0.3f, 1f);
        
        private float _cycleTimer = 0f;
        
        // Events
        public System.Action<LightPhase> OnPhaseChanged;
        
        private void Awake()
        {
            if (directionalLight == null)
                directionalLight = FindObjectOfType<Light>();
        }
        
        private void Start()
        {
            currentPhase = LightPhase.Dawn;
            UpdateLighting();
        }
        
        public void UpdateCycle(float totalTime)
        {
            _cycleTimer = totalTime % (cycleDuration * 4f); // 4 phases
            
            LightPhase newPhase = GetPhaseFromTime(_cycleTimer);
            
            if (newPhase != currentPhase)
            {
                currentPhase = newPhase;
                OnPhaseChanged?.Invoke(currentPhase);
                UpdateLighting();
                ApplyPhaseEffects();
            }
        }
        
        private LightPhase GetPhaseFromTime(float time)
        {
            float phaseTime = time % cycleDuration;
            float phaseRatio = time / cycleDuration;
            
            if (phaseRatio < 1f)
                return LightPhase.Dawn;
            else if (phaseRatio < 2f)
                return LightPhase.Noon;
            else if (phaseRatio < 3f)
                return LightPhase.Dusk;
            else
                return LightPhase.Midnight;
        }
        
        private void UpdateLighting()
        {
            Color lightColor;
            Color ambientColor;
            
            switch (currentPhase)
            {
                case LightPhase.Dawn:
                    lightColor = dawnColor;
                    ambientColor = dawnAmbient;
                    break;
                    
                case LightPhase.Noon:
                    lightColor = noonColor;
                    ambientColor = noonAmbient;
                    break;
                    
                case LightPhase.Dusk:
                    lightColor = duskColor;
                    ambientColor = duskAmbient;
                    break;
                    
                case LightPhase.Midnight:
                    lightColor = midnightColor;
                    ambientColor = midnightAmbient;
                    break;
                    
                default:
                    lightColor = Color.white;
                    ambientColor = Color.white;
                    break;
            }
            
            if (directionalLight != null)
            {
                directionalLight.color = lightColor;
                
                // Adjust intensity
                switch (currentPhase)
                {
                    case LightPhase.Dawn:
                        directionalLight.intensity = 0.6f;
                        break;
                    case LightPhase.Noon:
                        directionalLight.intensity = 1f;
                        break;
                    case LightPhase.Dusk:
                        directionalLight.intensity = 0.5f;
                        break;
                    case LightPhase.Midnight:
                        directionalLight.intensity = 0.3f;
                        break;
                }
            }
            
            RenderSettings.ambientSkyColor = ambientColor;
            RenderSettings.ambientEquatorColor = ambientColor * 0.7f;
            RenderSettings.ambientGroundColor = ambientColor * 0.5f;
        }
        
        private void ApplyPhaseEffects()
        {
            switch (currentPhase)
            {
                case LightPhase.Dawn:
                    // New births increase
                    Debug.Log("Dawn: New butterfly births increase");
                    break;
                    
                case LightPhase.Noon:
                    // Flight speed increases
                    Debug.Log("Noon: Flight speed increases");
                    break;
                    
                case LightPhase.Dusk:
                    // Harmonics intensify
                    Debug.Log("Dusk: Harmonics intensify");
                    break;
                    
                case LightPhase.Midnight:
                    // Plants whisper, butterflies become waveforms
                    Debug.Log("Midnight: Plants whisper, butterflies become waveforms");
                    break;
            }
        }
        
        public LightPhase CurrentPhase => currentPhase;
    }
}

