using UnityEngine;

public class GrowthController : MonoBehaviour
{
    [Header("Single Plant Target")]
    [SerializeField] private GrowthPlant targetPlant;

    [Header("Cluster Target")]
    [SerializeField] private GrowthCluster targetCluster;

    public void TriggerGrowth()
    {
        if (targetCluster != null)
        {
            targetCluster.AdvanceAllPlants();
            return;
        }

        if (targetPlant != null)
        {
            targetPlant.AdvanceStage();
        }
    }

    public void TriggerGrowthReverse()
    {
        if (targetCluster != null)
        {
            targetCluster.RegressAllPlants();
            return;
        }

        if (targetPlant != null)
        {
            targetPlant.RegressStage();
        }
    }
}
