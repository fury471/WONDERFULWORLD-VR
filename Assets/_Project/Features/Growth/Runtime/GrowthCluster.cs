using UnityEngine;

public class GrowthCluster : MonoBehaviour
{
    [SerializeField] private GrowthPlant[] plants;

    public void AdvanceAllPlants()
    {
        if (plants == null)
        {
            return;
        }

        foreach (var plant in plants)
        {
            if (plant != null)
            {
                plant.AdvanceStage();
            }
        }
    }

    public void RegressAllPlants()
    {
        if (plants == null)
        {
            return;
        }

        foreach (var plant in plants)
        {
            if (plant != null)
            {
                plant.RegressStage();
            }
        }
    }
}
