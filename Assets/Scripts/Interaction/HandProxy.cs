using UnityEngine;

namespace ButterflyHouse.Interaction
{
    /// <summary>
    /// Represents a hand proxy for hand tracking interaction.
    /// Can be attached to hand-tracking skeletons or controller proxies.
    /// </summary>
    public class HandProxy : MonoBehaviour
    {
        public enum HandType
        {
            Left,
            Right,
            Unknown
        }
        
        [Header("Hand Identity")]
        [SerializeField] private HandType handType = HandType.Unknown;
        
        [Header("Colliders")]
        [SerializeField] private Collider[] handColliders;
        [SerializeField] private bool autoCreateColliders = true;
        
        [Header("Landing Target")]
        [SerializeField] private LandingTarget landingTarget;
        [SerializeField] private bool createLandingTarget = true;
        
        private void Awake()
        {
            // Auto-create colliders if needed
            if (autoCreateColliders && (handColliders == null || handColliders.Length == 0))
            {
                // Create a simple sphere collider for the palm
                var collider = gameObject.AddComponent<SphereCollider>();
                collider.radius = 0.05f;
                handColliders = new Collider[] { collider };
            }
            
            // Create landing target if needed
            if (createLandingTarget && landingTarget == null)
            {
                GameObject landingObj = new GameObject("LandingTarget");
                landingObj.transform.SetParent(transform);
                landingObj.transform.localPosition = Vector3.zero;
                
                // Add collider for landing target
                var landingCollider = landingObj.AddComponent<SphereCollider>();
                landingCollider.radius = 0.08f;
                landingCollider.isTrigger = true;
                
                // Add LandingTarget component
                landingTarget = landingObj.AddComponent<LandingTarget>();
                
                // Ensure it's on the correct layer if needed
                // landingObj.layer = LayerMask.NameToLayer("LandingTarget");
            }
        }
        
        /// <summary>
        /// Update hand position and rotation (call from hand tracking update).
        /// </summary>
        public void UpdateHandPose(Vector3 position, Quaternion rotation)
        {
            transform.position = position;
            transform.rotation = rotation;
        }
        
        public HandType Hand => handType;
        public LandingTarget LandingTarget => landingTarget;
        
        /// <summary>
        /// Check if this hand is currently being tracked.
        /// </summary>
        public bool IsTracked => gameObject.activeSelf;
        
        /// <summary>
        /// Enable or disable hand tracking.
        /// </summary>
        public void SetTracked(bool tracked)
        {
            gameObject.SetActive(tracked);
            
            if (landingTarget != null)
            {
                landingTarget.enabled = tracked;
            }
        }
    }
}

