using UnityEngine;

public class GrowthCluster : MonoBehaviour
{
    [SerializeField] private GrowthPlant[] plants;

    private void Awake()
    {
        AutoAssignPlants();
    }

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

    private void AutoAssignPlants()
    {
        if (plants != null && plants.Length > 0)
        {
            return;
        }

        Transform searchRoot = transform.parent != null ? transform.parent : transform;
        plants = searchRoot.GetComponentsInChildren<GrowthPlant>(includeInactive: true);
    }
}
