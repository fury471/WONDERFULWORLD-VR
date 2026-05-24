using UnityEngine;

public class GrowthController : MonoBehaviour
{
    [Header("Single Plant Target")]
    [SerializeField] private GrowthPlant targetPlant;

    [Header("Cluster Target")]
    [SerializeField] private GrowthCluster targetCluster;

    public GrowthPlant TargetPlant => targetPlant;
    public GrowthCluster TargetCluster => targetCluster;

    private void Awake()
    {
        AutoAssignTargets();
    }

    public void TriggerGrowth()
    {
        PlayGrowthAudio();

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

    public void TriggerSingleGrowth()
    {
        if (targetPlant != null)
        {
            PlayGrowthAudio();
            targetPlant.AdvanceStage();
        }
    }

    public void TriggerClusterGrowth()
    {
        if (targetCluster != null)
        {
            PlayGrowthAudio();
            targetCluster.AdvanceAllPlants();
        }
    }

    public void TriggerSingleGrowthReverse()
    {
        if (targetPlant != null)
        {
            targetPlant.RegressStage();
        }
    }

    public void TriggerClusterGrowthReverse()
    {
        if (targetCluster != null)
        {
            targetCluster.RegressAllPlants();
        }
    }

    private void AutoAssignTargets()
    {
        if (targetPlant == null)
        {
            targetPlant = GetComponentInChildren<GrowthPlant>();
            if (targetPlant == null)
            {
                targetPlant = FindFirstObjectByType<GrowthPlant>();
            }
        }

        if (targetCluster == null)
        {
            targetCluster = GetComponentInChildren<GrowthCluster>();
            if (targetCluster == null)
            {
                targetCluster = FindFirstObjectByType<GrowthCluster>();
            }
        }
    }

    private void PlayGrowthAudio()
    {
        WonderfulWorld.Audio.WonderlandAudioOneShotPlayer.PlayAt("WW_SFX_GrowthRustle", transform.position, volumeScale: 1f, maxVoices: 4);
    }
}
