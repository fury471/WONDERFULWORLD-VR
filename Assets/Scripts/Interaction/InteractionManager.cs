using System.Collections.Generic;
using UnityEngine;

namespace ButterflyHouse.Interaction
{
    /// <summary>
    /// Manages all interaction systems including hand tracking and touch detection.
    /// Bridges Unity XR Interaction Toolkit or platform SDKs with our interaction systems.
    /// </summary>
    public class InteractionManager : MonoBehaviour
    {
        public static InteractionManager Instance { get; private set; }
        
        [Header("Hand Tracking")]
        [SerializeField] private HandProxy leftHandProxy;
        [SerializeField] private HandProxy rightHandProxy;
        [SerializeField] private bool enableHandTracking = true;
        
        [Header("XR Setup")]
        [SerializeField] private bool autoSetupHands = true;
        
        private List<HandProxy> _activeHands = new List<HandProxy>();
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Multiple InteractionManager instances detected. Destroying duplicate.");
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
        }
        
        private void Start()
        {
            if (enableHandTracking && autoSetupHands)
            {
                SetupHandTracking();
            }
        }
        
        private void Update()
        {
            if (enableHandTracking)
            {
                UpdateHandTracking();
            }
        }
        
        /// <summary>
        /// Setup hand tracking proxies (can be extended to integrate with XR SDK).
        /// </summary>
        private void SetupHandTracking()
        {
            // This is a placeholder - in a real implementation, you would:
            // 1. Query hand tracking API (OpenXR, Oculus SDK, etc.)
            // 2. Create HandProxy objects for each tracked hand
            // 3. Update their positions each frame
            
            // For now, create default hand proxies if not assigned
            if (leftHandProxy == null)
            {
                GameObject leftHandObj = new GameObject("LeftHandProxy");
                leftHandObj.transform.SetParent(transform);
                leftHandProxy = leftHandObj.AddComponent<HandProxy>();
                leftHandProxy.enabled = false; // Disable until tracking is available
            }
            
            if (rightHandProxy == null)
            {
                GameObject rightHandObj = new GameObject("RightHandProxy");
                rightHandObj.transform.SetParent(transform);
                rightHandProxy = rightHandObj.AddComponent<HandProxy>();
                rightHandProxy.enabled = false; // Disable until tracking is available
            }
            
            _activeHands.Clear();
            if (leftHandProxy != null && leftHandProxy.IsTracked)
                _activeHands.Add(leftHandProxy);
            if (rightHandProxy != null && rightHandProxy.IsTracked)
                _activeHands.Add(rightHandProxy);
        }
        
        /// <summary>
        /// Update hand tracking (call from XR input system).
        /// </summary>
        private void UpdateHandTracking()
        {
            // Placeholder for hand tracking update logic
            // In a real implementation, query hand positions from XR SDK:
            /*
            if (TryGetHandPose(HandType.Left, out Vector3 leftPos, out Quaternion leftRot))
            {
                if (leftHandProxy != null)
                {
                    leftHandProxy.UpdateHandPose(leftPos, leftRot);
                    leftHandProxy.SetTracked(true);
                    if (!_activeHands.Contains(leftHandProxy))
                        _activeHands.Add(leftHandProxy);
                }
            }
            else
            {
                if (leftHandProxy != null)
                {
                    leftHandProxy.SetTracked(false);
                    _activeHands.Remove(leftHandProxy);
                }
            }
            
            // Same for right hand...
            */
        }
        
        /// <summary>
        /// Get all currently tracked hands.
        /// </summary>
        public List<HandProxy> GetTrackedHands()
        {
            return new List<HandProxy>(_activeHands);
        }
        
        /// <summary>
        /// Get all available landing targets (including hands).
        /// </summary>
        public List<LandingTarget> GetAllLandingTargets()
        {
            var targets = new List<LandingTarget>();
            
            // Add hand targets
            foreach (var hand in _activeHands)
            {
                if (hand.LandingTarget != null && hand.LandingTarget.IsAvailable)
                {
                    targets.Add(hand.LandingTarget);
                }
            }
            
            // Add all other landing targets in scene
            var allTargets = FindObjectsOfType<LandingTarget>();
            foreach (var target in allTargets)
            {
                if (target != null && target.IsAvailable && !targets.Contains(target))
                {
                    targets.Add(target);
                }
            }
            
            return targets;
        }
        
        /// <summary>
        /// Enable or disable hand tracking.
        /// </summary>
        public void SetHandTrackingEnabled(bool enabled)
        {
            enableHandTracking = enabled;
            
            if (!enabled)
            {
                foreach (var hand in _activeHands)
                {
                    if (hand != null)
                        hand.SetTracked(false);
                }
                _activeHands.Clear();
            }
        }
        
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}

