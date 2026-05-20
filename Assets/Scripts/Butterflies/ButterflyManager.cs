using System.Collections.Generic;
using UnityEngine;

namespace ButterflyHouse.Butterflies
{
    /// <summary>
    /// Central manager for all butterflies in the scene.
    /// Handles spawning, pooling, and lifecycle management.
    /// </summary>
    public class ButterflyManager : MonoBehaviour
    {
        public static ButterflyManager Instance { get; private set; }
        
        [Header("Prefabs")]
        [SerializeField] private Butterfly butterflyPrefab;
        
        [Header("Spawn Settings")]
        [SerializeField] private int baseMaxButterflies = 20;
        [SerializeField] private int currentMaxButterflies = 20;
        [SerializeField] private bool enableAutoCleanup = true;
        [SerializeField] private float cleanupInterval = 5f;
        
        [Header("Stage-Based Population")]
        [SerializeField] private bool scalePopulationWithStage = true;
        [SerializeField] private int maxButterfliesStage0 = 20;
        [SerializeField] private int maxButterfliesStage1 = 30;
        [SerializeField] private int maxButterfliesStage2 = 50;
        [SerializeField] private int maxButterfliesStage3 = 75;
        [SerializeField] private int maxButterfliesStage4 = 150;
        [SerializeField] private int maxButterfliesStage5 = 250;
        
        [Header("Population Maintenance")]
        [SerializeField] private bool maintainPopulation = true;
        [SerializeField] private float minPopulationPercent = 0.7f; // Spawn when below 70% of max
        [SerializeField] private float populationCheckInterval = 5f; // Check every 5 seconds
        
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = false;
        
        private float _populationCheckTimer = 0f;
        
        [Header("Bounding Box")]
        [SerializeField] private bool useBoundingBox = true;
        [SerializeField] private Vector3 boundingBoxMin = new Vector3(-10f, 0f, -10f);
        [SerializeField] private Vector3 boundingBoxMax = new Vector3(10f, 5f, 10f);
        [SerializeField] private float boundarySteerStrength = 2f; // How strongly butterflies steer away from boundaries
        [SerializeField] private float boundaryBufferZone = 1f; // Distance from boundary where steering starts (prevents surface dragging)
        [SerializeField] private float surfaceAvoidanceStrength = 5f; // Extra strength when very close to a surface
        [SerializeField] private float groundUpwardBias = 3f; // Upward steering bias when near ground
        
        private readonly List<Butterfly> _activeButterflies = new List<Butterfly>();
        private float _cleanupTimer;
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Multiple ButterflyManager instances detected. Destroying duplicate.");
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            currentMaxButterflies = baseMaxButterflies;
        }
        
        private void Update()
        {
            if (enableAutoCleanup)
            {
                _cleanupTimer += Time.deltaTime;
                if (_cleanupTimer >= cleanupInterval)
                {
                    CleanupDissipatedButterflies();
                    _cleanupTimer = 0f;
                }
            }
            
            // Population maintenance check
            if (maintainPopulation)
            {
                _populationCheckTimer += Time.deltaTime;
                if (_populationCheckTimer >= populationCheckInterval)
                {
                    CheckPopulationMaintenance();
                    _populationCheckTimer = 0f;
                }
            }
        }
        
        /// <summary>
        /// Spawn a butterfly from the given archetype at the specified position.
        /// </summary>
        public Butterfly SpawnButterfly(ButterflyArchetype archetype, Vector3 position)
        {
            if (archetype == null)
            {
                Debug.LogError("Cannot spawn butterfly: archetype is null");
                return null;
            }
            
            if (_activeButterflies.Count >= currentMaxButterflies)
            {
                if (enableDebugLogs)
                    Debug.Log($"Max butterflies ({currentMaxButterflies}) reached. Current: {_activeButterflies.Count}. Cannot spawn new butterfly.");
                return null;
            }
            
            if (butterflyPrefab == null)
            {
                Debug.LogError("Cannot spawn butterfly: prefab is not assigned");
                return null;
            }
            
            var butterfly = Instantiate(butterflyPrefab, position, Quaternion.identity);
            butterfly.Initialize(archetype);
            _activeButterflies.Add(butterfly);
            
            return butterfly;
        }
        
        /// <summary>
        /// Remove a butterfly from the active list (called when butterfly is being destroyed).
        /// </summary>
        public void DespawnButterfly(Butterfly butterfly)
        {
            if (butterfly != null)
            {
                _activeButterflies.Remove(butterfly);
            }
        }
        
        /// <summary>
        /// Get all currently active butterflies.
        /// </summary>
        public List<Butterfly> GetActiveButterflies()
        {
            return new List<Butterfly>(_activeButterflies);
        }
        
        /// <summary>
        /// Get the bounding box bounds.
        /// </summary>
        public bool UseBoundingBox => useBoundingBox;
        public Vector3 BoundingBoxMin => boundingBoxMin;
        public Vector3 BoundingBoxMax => boundingBoxMax;
        public float BoundarySteerStrength => boundarySteerStrength;
        public float BoundaryBufferZone => boundaryBufferZone;
        public float SurfaceAvoidanceStrength => surfaceAvoidanceStrength;
        public float GroundUpwardBias => groundUpwardBias;
        
        /// <summary>
        /// Check if a position is within the bounding box.
        /// </summary>
        public bool IsWithinBounds(Vector3 position)
        {
            if (!useBoundingBox) return true;
            return position.x >= boundingBoxMin.x && position.x <= boundingBoxMax.x &&
                   position.y >= boundingBoxMin.y && position.y <= boundingBoxMax.y &&
                   position.z >= boundingBoxMin.z && position.z <= boundingBoxMax.z;
        }
        
        /// <summary>
        /// Get the steering direction to keep a position within bounds.
        /// Returns a direction vector pointing away from the nearest boundary.
        /// Magnitude is stronger when closer to boundaries (to prevent surface dragging).
        /// </summary>
        public Vector3 GetBoundarySteerDirection(Vector3 position, out float steerStrength)
        {
            steerStrength = boundarySteerStrength;
            
            if (!useBoundingBox) return Vector3.zero;
            
            Vector3 steer = Vector3.zero;
            float maxDistance = 0f;
            
            // Check X bounds with buffer zone
            float distToMinX = position.x - boundingBoxMin.x;
            float distToMaxX = boundingBoxMax.x - position.x;
            
            if (distToMinX < boundaryBufferZone)
            {
                float strength = 1f - (distToMinX / boundaryBufferZone); // 1.0 at boundary, 0.0 at buffer edge
                steer.x += strength; // Steer right (away from left wall)
                maxDistance = Mathf.Max(maxDistance, boundaryBufferZone - distToMinX);
            }
            else if (distToMinX < 0f) // Outside bounds
            {
                steer.x += 1f; // Strong steer right
                maxDistance = Mathf.Max(maxDistance, Mathf.Abs(distToMinX));
            }
            
            if (distToMaxX < boundaryBufferZone)
            {
                float strength = 1f - (distToMaxX / boundaryBufferZone);
                steer.x -= strength; // Steer left (away from right wall)
                maxDistance = Mathf.Max(maxDistance, boundaryBufferZone - distToMaxX);
            }
            else if (distToMaxX < 0f) // Outside bounds
            {
                steer.x -= 1f; // Strong steer left
                maxDistance = Mathf.Max(maxDistance, Mathf.Abs(distToMaxX));
            }
            
            // Check Y bounds (ground/ceiling) with buffer zone
            float distToMinY = position.y - boundingBoxMin.y;
            float distToMaxY = boundingBoxMax.y - position.y;
            
            if (distToMinY < boundaryBufferZone)
            {
                float strength = 1f - (distToMinY / boundaryBufferZone);
                steer.y += strength * groundUpwardBias; // Strong upward steer when near ground
                maxDistance = Mathf.Max(maxDistance, boundaryBufferZone - distToMinY);
            }
            else if (distToMinY < 0f) // Below bounds
            {
                steer.y += groundUpwardBias * 2f; // Very strong upward steer
                maxDistance = Mathf.Max(maxDistance, Mathf.Abs(distToMinY));
            }
            
            if (distToMaxY < boundaryBufferZone)
            {
                float strength = 1f - (distToMaxY / boundaryBufferZone);
                steer.y -= strength; // Steer down (away from ceiling)
                maxDistance = Mathf.Max(maxDistance, boundaryBufferZone - distToMaxY);
            }
            else if (distToMaxY < 0f) // Above bounds
            {
                steer.y -= 1f; // Strong steer down
                maxDistance = Mathf.Max(maxDistance, Mathf.Abs(distToMaxY));
            }
            
            // Check Z bounds with buffer zone
            float distToMinZ = position.z - boundingBoxMin.z;
            float distToMaxZ = boundingBoxMax.z - position.z;
            
            if (distToMinZ < boundaryBufferZone)
            {
                float strength = 1f - (distToMinZ / boundaryBufferZone);
                steer.z += strength; // Steer forward (away from back wall)
                maxDistance = Mathf.Max(maxDistance, boundaryBufferZone - distToMinZ);
            }
            else if (distToMinZ < 0f) // Outside bounds
            {
                steer.z += 1f; // Strong steer forward
                maxDistance = Mathf.Max(maxDistance, Mathf.Abs(distToMinZ));
            }
            
            if (distToMaxZ < boundaryBufferZone)
            {
                float strength = 1f - (distToMaxZ / boundaryBufferZone);
                steer.z -= strength; // Steer back (away from front wall)
                maxDistance = Mathf.Max(maxDistance, boundaryBufferZone - distToMaxZ);
            }
            else if (distToMaxZ < 0f) // Outside bounds
            {
                steer.z -= 1f; // Strong steer back
                maxDistance = Mathf.Max(maxDistance, Mathf.Abs(distToMaxZ));
            }
            
            // Increase steer strength when very close to surfaces (to prevent dragging)
            if (maxDistance > 0f)
            {
                float proximityFactor = 1f + (maxDistance / boundaryBufferZone); // 1.0 at buffer edge, 2.0 at surface
                steerStrength = boundarySteerStrength * proximityFactor;
                
                // Extra strength when touching or very close to surfaces
                if (maxDistance >= boundaryBufferZone * 0.8f)
                {
                    steerStrength += surfaceAvoidanceStrength;
                }
            }
            
            // Normalize if there's steering, otherwise return zero
            if (steer.sqrMagnitude > 0.01f)
            {
                return steer.normalized;
            }
            
            return Vector3.zero;
        }
        
        /// <summary>
        /// Get the steering direction to keep a position within bounds (simpler version without strength).
        /// </summary>
        public Vector3 GetBoundarySteerDirection(Vector3 position)
        {
            float unused;
            return GetBoundarySteerDirection(position, out unused);
        }
        
        /// <summary>
        /// Clamp a position to be within the bounding box.
        /// </summary>
        public Vector3 ClampToBounds(Vector3 position)
        {
            if (!useBoundingBox) return position;
            
            return new Vector3(
                Mathf.Clamp(position.x, boundingBoxMin.x, boundingBoxMax.x),
                Mathf.Clamp(position.y, boundingBoxMin.y, boundingBoxMax.y),
                Mathf.Clamp(position.z, boundingBoxMin.z, boundingBoxMax.z)
            );
        }
        
        /// <summary>
        /// Get the count of active butterflies.
        /// </summary>
        public int ActiveButterflyCount => _activeButterflies.Count;
        
        /// <summary>
        /// Get the current maximum butterfly population (scales with progression stage).
        /// </summary>
        public int CurrentMaxButterflies => currentMaxButterflies;
        
        /// <summary>
        /// Check if we can spawn more butterflies.
        /// </summary>
        public bool CanSpawn => _activeButterflies.Count < currentMaxButterflies;
        
        /// <summary>
        /// Check if we should spawn more butterflies to maintain population.
        /// </summary>
        public bool ShouldSpawnForMaintenance
        {
            get
            {
                if (!maintainPopulation) return false;
                float targetMin = currentMaxButterflies * minPopulationPercent;
                return _activeButterflies.Count < targetMin;
            }
        }
        
        /// <summary>
        /// Remove all butterflies from the scene.
        /// </summary>
        public void ClearAllButterflies()
        {
            foreach (var butterfly in _activeButterflies)
            {
                if (butterfly != null)
                {
                    butterfly.ForceDissipate();
                }
            }
            _activeButterflies.Clear();
        }
        
        private void CleanupDissipatedButterflies()
        {
            _activeButterflies.RemoveAll(b => b == null || b.IsDissipated);
        }
        
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
        
        /// <summary>
        /// Called when progression stage changes.
        /// Updates max butterfly population based on stage.
        /// </summary>
        public void OnProgressionStageChanged(int newStage)
        {
            Debug.Log($"ButterflyManager: Progression stage changed to {newStage}");
            
            if (scalePopulationWithStage)
            {
                int previousMax = currentMaxButterflies;
                
                // Update max butterflies based on stage
                switch (newStage)
                {
                    case 0:
                        currentMaxButterflies = maxButterfliesStage0;
                        break;
                    case 1:
                        currentMaxButterflies = maxButterfliesStage1;
                        break;
                    case 2:
                        currentMaxButterflies = maxButterfliesStage2;
                        break;
                    case 3:
                        currentMaxButterflies = maxButterfliesStage3;
                        break;
                    case 4:
                        currentMaxButterflies = maxButterfliesStage4;
                        break;
                    case 5:
                        currentMaxButterflies = maxButterfliesStage5;
                        break;
                    default:
                        // Default to stage 0 if stage is out of range
                        currentMaxButterflies = maxButterfliesStage0;
                        break;
                }
                
                Debug.Log($"ButterflyManager: Max population updated from {previousMax} to {currentMaxButterflies} (Stage {newStage}). Current population: {_activeButterflies.Count}");
            }
            
            // Stage-specific butterfly behaviors can be added here
            // For example: change flight patterns, spawn bonus butterflies, etc.
        }
        
        /// <summary>
        /// Check if population needs maintenance and log status.
        /// </summary>
        private void CheckPopulationMaintenance()
        {
            int currentCount = _activeButterflies.Count;
            float targetMin = currentMaxButterflies * minPopulationPercent;
            
            if (currentCount < targetMin)
            {
                Debug.Log($"ButterflyManager: Population below target ({currentCount}/{currentMaxButterflies}, target min: {targetMin:F0}). Chrysalises should spawn more frequently.");
            }
        }
        
        private void OnDrawGizmos()
        {
            if (!useBoundingBox) return;
            
            // Draw bounding box
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f); // Orange with transparency
            
            Vector3 center = (boundingBoxMin + boundingBoxMax) * 0.5f;
            Vector3 size = boundingBoxMax - boundingBoxMin;
            
            // Draw wire cube
            Gizmos.DrawWireCube(center, size);
            
            // Draw corners
            Gizmos.color = new Color(1f, 0f, 0f, 1f); // Red for corners
            float cornerSize = 0.2f;
            Gizmos.DrawCube(new Vector3(boundingBoxMin.x, boundingBoxMin.y, boundingBoxMin.z), Vector3.one * cornerSize);
            Gizmos.DrawCube(new Vector3(boundingBoxMax.x, boundingBoxMin.y, boundingBoxMin.z), Vector3.one * cornerSize);
            Gizmos.DrawCube(new Vector3(boundingBoxMin.x, boundingBoxMax.y, boundingBoxMin.z), Vector3.one * cornerSize);
            Gizmos.DrawCube(new Vector3(boundingBoxMax.x, boundingBoxMax.y, boundingBoxMin.z), Vector3.one * cornerSize);
            Gizmos.DrawCube(new Vector3(boundingBoxMin.x, boundingBoxMin.y, boundingBoxMax.z), Vector3.one * cornerSize);
            Gizmos.DrawCube(new Vector3(boundingBoxMax.x, boundingBoxMin.y, boundingBoxMax.z), Vector3.one * cornerSize);
            Gizmos.DrawCube(new Vector3(boundingBoxMin.x, boundingBoxMax.y, boundingBoxMax.z), Vector3.one * cornerSize);
            Gizmos.DrawCube(new Vector3(boundingBoxMax.x, boundingBoxMax.y, boundingBoxMax.z), Vector3.one * cornerSize);
        }
    }
}

