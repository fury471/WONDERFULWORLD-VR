using System.Collections.Generic;
using UnityEngine;

namespace ButterflyHouse.Plants
{
    /// <summary>
    /// Central manager for all fruits in the scene.
    /// Handles fruit spawning, stage upgrades, and global fruit effects.
    /// </summary>
    public class FruitManager : MonoBehaviour
    {
        public static FruitManager Instance { get; private set; }
        
        [Header("Fruit Management")]
        [SerializeField] private List<GenerativeFruit> allFruits = new List<GenerativeFruit>();
        
        [Header("Celestial Fruit")]
        [SerializeField] private GameObject celestialFruitPrefab;
        [SerializeField] private int maxCelestialFruits = 3;
        [SerializeField] private float celestialFruitSpawnInterval = 60f;
        
        private float _celestialFruitSpawnTimer = 0f;
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            
            // Find all existing fruits in scene
            FindAllFruits();
        }
        
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
        
        private void Update()
        {
            // Spawn celestial fruits periodically in late stages
            Core.EcosystemStateController stateController = Core.EcosystemStateController.Instance;
            if (stateController != null && stateController.ProgressionStage >= 4)
            {
                int celestialCount = GetCelestialFruitCount();
                if (celestialCount < maxCelestialFruits)
                {
                    _celestialFruitSpawnTimer += Time.deltaTime;
                    if (_celestialFruitSpawnTimer >= celestialFruitSpawnInterval)
                    {
                        SpawnCelestialFruit();
                        _celestialFruitSpawnTimer = 0f;
                    }
                }
            }
        }
        
        private void FindAllFruits()
        {
            allFruits.Clear();
            allFruits.AddRange(FindObjectsByType<GenerativeFruit>(FindObjectsSortMode.InstanceID));
        }
        
        /// <summary>
        /// Register a fruit with the manager.
        /// </summary>
        public void RegisterFruit(GenerativeFruit fruit)
        {
            if (fruit != null && !allFruits.Contains(fruit))
            {
                allFruits.Add(fruit);
            }
        }
        
        /// <summary>
        /// Unregister a fruit from the manager.
        /// </summary>
        public void UnregisterFruit(GenerativeFruit fruit)
        {
            if (fruit != null)
            {
                allFruits.Remove(fruit);
            }
        }
        
        /// <summary>
        /// Upgrade all fruits to a specific stage.
        /// </summary>
        public void UpgradeAllFruit(FruitGrowthSystem.FruitStage stage)
        {
            FindAllFruits(); // Refresh fruit list
            
            foreach (var fruit in allFruits)
            {
                if (fruit == null) continue;
                
                var growthSystem = fruit.GetComponent<FruitGrowthSystem>();
                if (growthSystem != null)
                {
                    // Force stage upgrade directly
                    while ((int)growthSystem.CurrentStage < (int)stage && (int)growthSystem.CurrentStage < 3)
                    {
                        growthSystem.ForceAdvance();
                    }
                }
            }
            
            Debug.Log($"FruitManager: Upgraded all fruits to {stage}");
        }
        
        /// <summary>
        /// Upgrade some fruits to a specific stage (random selection).
        /// </summary>
        public void UpgradeSomeFruit(FruitGrowthSystem.FruitStage stage, float upgradeChance = 0.5f)
        {
            FindAllFruits(); // Refresh fruit list
            
            foreach (var fruit in allFruits)
            {
                if (fruit == null) continue;
                
                if (Random.value < upgradeChance)
                {
                    var growthSystem = fruit.GetComponent<FruitGrowthSystem>();
                    if (growthSystem != null)
                    {
                        while ((int)growthSystem.CurrentStage < (int)stage && (int)growthSystem.CurrentStage < 3)
                        {
                            growthSystem.ForceAdvance();
                        }
                    }
                }
            }
            
            Debug.Log($"FruitManager: Upgraded some fruits to {stage}");
        }
        
        /// <summary>
        /// Spawn a new celestial fruit.
        /// </summary>
        public void SpawnCelestialFruit()
        {
            if (celestialFruitPrefab == null)
            {
                Debug.LogWarning("FruitManager: Cannot spawn celestial fruit - prefab not assigned");
                return;
            }
            
            // Spawn at random position
            Vector3 spawnPos = new Vector3(
                Random.Range(-10f, 10f),
                Random.Range(2f, 5f),
                Random.Range(-10f, 10f)
            );
            
            GameObject fruitObj = Instantiate(celestialFruitPrefab, spawnPos, Quaternion.identity);
            GenerativeFruit fruit = fruitObj.GetComponent<GenerativeFruit>();
            if (fruit != null)
            {
                RegisterFruit(fruit);
                
                // Force to Celestial stage
                var growthSystem = fruit.GetComponent<FruitGrowthSystem>();
                if (growthSystem != null)
                {
                    // Force to highest stage
                    while ((int)growthSystem.CurrentStage < 3)
                    {
                        growthSystem.ForceAdvance();
                    }
                }
            }
            
            Debug.Log($"FruitManager: Spawned celestial fruit at {spawnPos}");
        }
        
        /// <summary>
        /// Get count of celestial fruits.
        /// </summary>
        public int GetCelestialFruitCount()
        {
            int count = 0;
            foreach (var fruit in allFruits)
            {
                if (fruit != null && fruit.CurrentStage == FruitGrowthSystem.FruitStage.Celestial)
                {
                    count++;
                }
            }
            return count;
        }
        
        /// <summary>
        /// Get all fruits in a radius.
        /// </summary>
        public List<GenerativeFruit> GetFruitsInRadius(Vector3 position, float radius)
        {
            List<GenerativeFruit> nearbyFruits = new List<GenerativeFruit>();
            
            foreach (var fruit in allFruits)
            {
                if (fruit == null) continue;
                
                float distance = Vector3.Distance(fruit.transform.position, position);
                if (distance <= radius)
                {
                    nearbyFruits.Add(fruit);
                }
            }
            
            return nearbyFruits;
        }
        
        /// <summary>
        /// Trigger Fruit Bloom event (all fruits glow simultaneously).
        /// </summary>
        public void TriggerFruitBloom()
        {
            Debug.Log("FruitManager: Fruit Bloom event triggered!");
            
            foreach (var fruit in allFruits)
            {
                if (fruit == null) continue;
                
                FruitVisualController visualController = fruit.GetComponent<FruitVisualController>();
                if (visualController != null)
                {
                    visualController.Pulse(1f, 2f);
                }
                
                // Play audio
                fruit.PlayMelody();
            }
        }
        
        /// <summary>
        /// Get all active fruits.
        /// </summary>
        public List<GenerativeFruit> GetAllFruits()
        {
            // Clean up null references
            allFruits.RemoveAll(f => f == null);
            return new List<GenerativeFruit>(allFruits);
        }
        
        /// <summary>
        /// Called when a flower is pollinated.
        /// </summary>
        public void OnFlowerPollinated(Flowers.Flower flower)
        {
            if (flower == null) return;
            
            // Meta-Flowers can spawn fruit seeds
            if (flower.CurrentStage == Flowers.Flower.FlowerStage.Meta)
            {
                Vector3 spawnPos = flower.transform.position + Vector3.up * 0.15f;
                TrySpawnFruitAt(spawnPos);
            }
        }
        
        /// <summary>
        /// Called when progression stage changes.
        /// </summary>
        public void OnProgressionStageChanged(int newStage)
        {
            Debug.Log($"FruitManager: Progression stage changed to {newStage}");
            
            // Stage-specific fruit upgrades (already handled in ProgressionStageManager)
            // This is here for compatibility with EcosystemOrchestrator
        }
        
        /// <summary>
        /// Try to spawn a fruit at the specified position (e.g., from Meta-Flowers).
        /// </summary>
        public void TrySpawnFruitAt(Vector3 position)
        {
            if (celestialFruitPrefab == null)
            {
                Debug.LogWarning("FruitManager: Cannot spawn fruit - prefab not assigned");
                return;
            }
            
            // Spawn at specified position
            GameObject fruitObj = Instantiate(celestialFruitPrefab, position, Quaternion.identity);
            GenerativeFruit fruit = fruitObj.GetComponent<GenerativeFruit>();
            if (fruit != null)
            {
                RegisterFruit(fruit);
                
                // Start as Seed stage (will evolve naturally)
                var growthSystem = fruit.GetComponent<FruitGrowthSystem>();
                if (growthSystem != null)
                {
                    // Keep as Seed, will evolve through interactions
                }
                
                Debug.Log($"FruitManager: Spawned fruit at {position} from Meta-Flower pollination");
            }
        }
    }
}

