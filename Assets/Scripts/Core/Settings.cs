using UnityEngine;

namespace ButterflyHouse.Core
{
    /// <summary>
    /// Global settings for the Butterfly House experience.
    /// Provides runtime adjustable parameters for performance and quality.
    /// </summary>
    [CreateAssetMenu(fileName = "ButterflyHouseSettings", menuName = "Butterfly House/Settings")]
    public class Settings : ScriptableObject
    {
        [Header("Performance")]
        [Range(5, 50)]
        public int maxButterflies = 20;
        
        [Header("Visual Effects")]
        public bool enableTrails = true;
        public bool enablePostProcessing = true;
        [Range(0f, 2f)]
        public float globalBloomIntensity = 0.8f;
        
        [Header("Audio")]
        [Range(0f, 1f)]
        public float masterVolume = 0.7f;
        [Range(0f, 1f)]
        public float butterflyVolume = 0.6f;
        [Range(0f, 1f)]
        public float plantVolume = 0.8f;
        [Range(0f, 1f)]
        public float ambienceVolume = 0.5f;
        
        [Header("Experience")]
        [Range(0.5f, 3f)]
        public float timeScale = 1f;
        [Range(0.1f, 5f)]
        public float spawnRateMultiplier = 1f;
        
        private static Settings _instance;
        
        public static Settings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<Settings>("ButterflyHouseSettings");
                    if (_instance == null)
                    {
                        Debug.LogWarning("ButterflyHouseSettings not found in Resources. Creating default.");
                        _instance = CreateInstance<Settings>();
                    }
                }
                return _instance;
            }
        }
    }
}

