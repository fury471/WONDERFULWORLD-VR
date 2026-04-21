using UnityEngine;
using System.Collections;

public class WaterDropSlide : MonoBehaviour
{
    [Header("Slide Properties")]
    [SerializeField] private float slideSpeed = 0.4f;
    [SerializeField] private float gravityStrength = 7.0f;
    
    // The calculated local radius of the leaf
    private float leafRadius; 
    private bool isSliding = false;
    private MeshRenderer leafMesh;

    /// <summary>
    /// Call this immediately after Instantiate to setup the droplet.
    /// This avoids expensive calculations in every frame.
    /// </summary>
    public void Initialize(MeshRenderer targetLeafMesh)
    {
        leafMesh = targetLeafMesh;
        
        if (leafMesh != null)
        {
            // 1. Get the world-space extents (half-size) of the leaf mesh
            // We use the maximum of X and Z to handle circular/irregular leaves
            float worldMaxExtent = Mathf.Max(leafMesh.bounds.extents.x, leafMesh.bounds.extents.z);
            
            // 2. Convert world distance to local distance
            // This ensures the radius stays correct even if the parent leaf is scaled
            float parentGlobalScale = transform.parent != null ? transform.parent.lossyScale.x : 1.0f;
            leafRadius = worldMaxExtent / parentGlobalScale;
        }
        else
        {
            // Fallback radius if no mesh is provided
            leafRadius = 0.5f;
        }
    }

    /// <summary>
    /// Starts the sliding behavior in a specific direction.
    /// </summary>
    public void StartSliding(Vector3 worldDirection)
    {
        if (isSliding) return;
        
        // Convert world direction to local space relative to the leaf
        Vector3 localDir = transform.parent.InverseTransformDirection(worldDirection);
        localDir.y = 0; // Keep movement strictly on the horizontal plane
        
        StartCoroutine(SlideRoutine(localDir.normalized));
    }

    private IEnumerator SlideRoutine(Vector3 localDir)
    {
        isSliding = true;

        // PHASE 1: Slide on the Leaf Surface
        // ---------------------------------------------------
        while (true)
        {
            // Calculate current distance from center (0,0,0) on the horizontal plane
            float currentDist = new Vector2(transform.localPosition.x, transform.localPosition.z).magnitude;

            // If we've reached the edge, break to start falling
            if (currentDist >= leafRadius) break;

            // Move the droplet
            transform.localPosition += localDir * slideSpeed * Time.deltaTime;
            
            yield return null;
        }

        // PHASE 2: Fall off the edge (Visual Polish)
        // ---------------------------------------------------
        float fallDuration = 0.8f;
        float elapsed = 0f;
        Vector3 fallVelocity = localDir * slideSpeed;

        while (elapsed < fallDuration)
        {
            elapsed += Time.deltaTime;

            // Apply simple simulated gravity to the local velocity
            fallVelocity.y -= gravityStrength * Time.deltaTime;
            
            // Update position and shrink
            transform.localPosition += fallVelocity * Time.deltaTime;
            transform.localScale *= 0.96f;

            yield return null;
        }

        // Cleanup
        Destroy(gameObject);
    }
}