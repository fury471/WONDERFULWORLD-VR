using UnityEngine;

namespace ButterflyHouse.Butterflies
{
    /// <summary>
    /// ScriptableObject defining butterfly archetypes/variants.
    /// Each archetype represents a unique butterfly species with distinct visual and audio properties.
    /// </summary>
    [CreateAssetMenu(fileName = "NewButterflyArchetype", menuName = "Butterfly/Archetype")]
    public class ButterflyArchetype : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public string displayName;
        
        [Header("Visual Properties")]
        [Tooltip("Color gradient for wings over time")]
        public Gradient wingColorGradient = new Gradient();
        [Range(0.1f, 2f)]
        public float baseScale = 1f;
        
        [Header("Animation Curves")]
        [Tooltip("Flap frequency curve over butterfly lifetime")]
        public AnimationCurve flapFrequencyCurve = AnimationCurve.Linear(0f, 2f, 1f, 2f);
        [Tooltip("Flight speed curve over butterfly lifetime")]
        public AnimationCurve flightSpeedCurve = AnimationCurve.Linear(0f, 0.5f, 1f, 0.5f);
        
        [Header("Audio Properties")]
        [Tooltip("Base audio clip for this butterfly type")]
        public AudioClip baseTone;
        [Range(0.5f, 2f)]
        [Tooltip("Base pitch multiplier")]
        public float basePitch = 1f;
        [Range(0f, 1f)]
        public float audioVolume = 0.6f;
        
        [Header("Flight Behavior")]
        [Range(1f, 10f)]
        public float minFlightRadius = 2f;
        [Range(1f, 20f)]
        public float maxFlightRadius = 8f;
        [Range(0.1f, 5f)]
        public float noiseScale = 0.5f;
        [Range(0.5f, 5f)]
        public float turnSpeed = 2f;
        
        [Header("Lifespan")]
        [Range(10f, 120f)]
        public float lifetime = 60f;
        [Range(5f, 30f)]
        public float landingInterval = 15f;
        
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(id))
            {
                id = name;
            }
            
            if (string.IsNullOrEmpty(displayName))
            {
                displayName = name;
            }
        }
    }
}

