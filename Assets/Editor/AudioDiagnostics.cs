using UnityEngine;

public class TreeGrowthController : MonoBehaviour
{
    public Material treeMaterial;
    public GameObject petalsObject;
    public float duration = 3.0f;
    public float maxGrowth = 8.0f;

    private float elapsed = 0;
    private bool startNow = false;

    void Start()
    {
        if (treeMaterial != null) treeMaterial.SetFloat("_Growth", 0);
        if (petalsObject != null) petalsObject.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            startNow = true;
        }

        if (startNow)
        {
            elapsed += Time.deltaTime;
            float ratio = Mathf.Clamp01(elapsed / duration);

            if (treeMaterial != null)
            {
                treeMaterial.SetFloat("_Growth", ratio * maxGrowth);
            }

            if (ratio >= 1.0f)
            {
                startNow = false;
                if (petalsObject != null) petalsObject.SetActive(true);
            }
        }
    }
}