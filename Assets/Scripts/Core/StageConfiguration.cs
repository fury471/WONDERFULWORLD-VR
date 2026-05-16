using UnityEngine;
using ButterflyHouse.Butterflies;

namespace ButterflyHouse.Core
{
    /// <summary>
    /// ScriptableObject configuration for a progression stage.
    /// Defines stage-specific settings and behaviors.
    /// </summary>
    [CreateAssetMenu(fileName = "StageConfig_", menuName = "Butterfly House/Stage Configuration")]
    public class StageConfiguration : ScriptableObject
    {
        [Header("Stage Info")]
        public int stageNumber = 0;
        public string stageName = "Emergence";
        public string description = "Basic butterflies spawn";
        
        [Header("Visual Settings")]
        [Range(1f, 5f)]
        public float trailTime = 1f;
        [Range(0.05f, 0.4f)]
        public float trailWidth = 0.05f;
        [Range(0f, 1f)]
        public float trailLuminescence = 0.5f;
        
        [Header("Audio Settings")]
        [Range(0f, 1f)]
        public float audioDensity = 0.3f;
        [Range(0f, 1f)]
        public float harmonicIntensity = 0f;
        
        [Header("Behavior")]
        public bool enableFlocking = false;
        public bool enableWindCurrents = false;
        public bool enablePollination = false;
        public bool enablePlantSpreading = false;
        
        [Header("Archetypes")]
        public ButterflyArchetype[] unlockedArchetypes;
    }
}

