using System.Collections.Generic;
using UnityEngine;
using ButterflyHouse.Flowers;

namespace ButterflyHouse.Plants
{
    /// <summary>
    /// Central manager for all plants in the scene.
    /// Handles plant registration, touch events, and flower pollination.
    /// </summary>
    public class PlantManager : MonoBehaviour
    {
        public static PlantManager Instance { get; private set; }
        
        [Header("Plant Management")]
        [SerializeField] private List<GenerativePlant> plants = new List<GenerativePlant>();
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            
            // Find all existing plants in scene
            FindAllPlants();
        }
        
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
        
        private void FindAllPlants()
        {
            plants.Clear();
            plants.AddRange(FindObjectsByType<GenerativePlant>(FindObjectsSortMode.InstanceID));
        }
        
        /// <summary>
        /// Register a plant with the manager.
        /// </summary>
        public void RegisterPlant(GenerativePlant plant)
        {
            if (plant != null && !plants.Contains(plant))
            {
                plants.Add(plant);
            }
        }
        
        /// <summary>
        /// Unregister a plant from the manager.
        /// </summary>
        public void UnregisterPlant(GenerativePlant plant)
        {
            if (plant != null)
            {
                plants.Remove(plant);
            }
        }
        
        /// <summary>
        /// Called when a plant is touched by the player.
        /// </summary>
        public void OnPlantTouched(GenerativePlant plant)
        {
            if (plant == null) return;
            
            // Notify plant growth system
            PlantGrowthSystem growthSystem = plant.GetComponent<PlantGrowthSystem>();
            if (growthSystem != null)
            {
                growthSystem.OnTouched();
            }
            
            Debug.Log($"PlantManager: Plant {plant.name} touched");
        }
        
        /// <summary>
        /// Called when a flower is pollinated.
        /// </summary>
        public void OnFlowerPollinated(Flower flower)
        {
            if (flower == null) return;
            
            // Notify parent plant
            GenerativePlant parentPlant = flower.ParentPlant;
            if (parentPlant == null)
            {
                parentPlant = flower.GetComponentInParent<GenerativePlant>();
            }
            
            if (parentPlant != null)
            {
                PlantGrowthSystem growthSystem = parentPlant.GetComponent<PlantGrowthSystem>();
                if (growthSystem != null)
                {
                    // Encourage plant growth from pollination
                    growthSystem.OnButterflyVisit();
                }
            }
            
            Debug.Log($"PlantManager: Flower pollinated on plant {parentPlant?.name ?? "unknown"}");
        }
        
        /// <summary>
        /// Called when progression stage changes.
        /// </summary>
        public void OnProgressionStageChanged(int newStage)
        {
            Debug.Log($"PlantManager: Progression stage changed to {newStage}");
            
            // Stage-specific plant behaviors can be added here
            // For example: spawn new plants, upgrade existing plants, etc.
            foreach (var plant in plants)
            {
                if (plant == null) continue;
                
                PlantGrowthSystem growthSystem = plant.GetComponent<PlantGrowthSystem>();
                if (growthSystem != null)
                {
                    // Plants might respond to stage changes
                    // Could trigger growth, spawn flowers, etc.
                }
            }
        }
        
        /// <summary>
        /// Get all active plants.
        /// </summary>
        public List<GenerativePlant> GetAllPlants()
        {
            // Clean up null references
            plants.RemoveAll(p => p == null);
            return new List<GenerativePlant>(plants);
        }
    }
}

