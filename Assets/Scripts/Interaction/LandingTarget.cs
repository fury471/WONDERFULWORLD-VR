using UnityEngine;

namespace ButterflyHouse.Interaction
{
    /// <summary>
    /// Represents a location where butterflies can land.
    /// Can be on hands, plants, or environment objects.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class LandingTarget : MonoBehaviour
    {
        public enum TargetType
        {
            Hand,
            Plant,
            Fruit,
            Environment
        }
        
        [Header("Target Type")]
        [SerializeField] private TargetType targetType = TargetType.Environment;
        
        [Header("Availability")]
        [SerializeField] private bool isAvailable = true;
        [SerializeField] private int maxConcurrentButterflies = 1;
        [SerializeField] private float reservationTimeout = 10f;
        
        private System.Collections.Generic.List<Butterflies.Butterfly> _reservedBy = new System.Collections.Generic.List<Butterflies.Butterfly>();
        private System.Collections.Generic.Dictionary<Butterflies.Butterfly, float> _reservationTimes = new System.Collections.Generic.Dictionary<Butterflies.Butterfly, float>();
        
        private void Update()
        {
            // Clean up expired reservations
            var expired = new System.Collections.Generic.List<Butterflies.Butterfly>();
            foreach (var kvp in _reservationTimes)
            {
                if (Time.time - kvp.Value > reservationTimeout)
                {
                    expired.Add(kvp.Key);
                }
            }
            
            foreach (var butterfly in expired)
            {
                Release(butterfly);
            }
        }
        
        /// <summary>
        /// Reserve this target for a butterfly.
        /// </summary>
        public bool Reserve(Butterflies.Butterfly butterfly)
        {
            if (butterfly == null) return false;
            
            if (!IsAvailable) return false;
            
            if (_reservedBy.Count >= maxConcurrentButterflies) return false;
            
            if (!_reservedBy.Contains(butterfly))
            {
                _reservedBy.Add(butterfly);
                _reservationTimes[butterfly] = Time.time;
            }
            
            UpdateAvailability();
            return true;
        }
        
        /// <summary>
        /// Release reservation for a butterfly.
        /// </summary>
        public void Release(Butterflies.Butterfly butterfly = null)
        {
            if (butterfly == null)
            {
                // Release all
                _reservedBy.Clear();
                _reservationTimes.Clear();
            }
            else
            {
                _reservedBy.Remove(butterfly);
                _reservationTimes.Remove(butterfly);
            }
            
            UpdateAvailability();
        }
        
        private void UpdateAvailability()
        {
            isAvailable = _reservedBy.Count < maxConcurrentButterflies;
        }
        
        /// <summary>
        /// Check if this target is available for landing.
        /// </summary>
        public bool IsAvailable
        {
            get
            {
                return isAvailable && _reservedBy.Count < maxConcurrentButterflies;
            }
        }
        
        public TargetType Type => targetType;
        
        /// <summary>
        /// Get the number of butterflies currently using this target.
        /// </summary>
        public int CurrentOccupants => _reservedBy.Count;
        
        /// <summary>
        /// Get preferred landing position (world space).
        /// </summary>
        public Vector3 GetLandingPosition()
        {
            return transform.position;
        }
        
        /// <summary>
        /// Get preferred landing rotation (world space).
        /// </summary>
        public Quaternion GetLandingRotation()
        {
            return transform.rotation;
        }
        
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = IsAvailable ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.2f);
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, GetComponent<Collider>().bounds.size.magnitude * 0.5f);
        }
    }
}

