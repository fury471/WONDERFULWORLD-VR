using System.Collections;
using UnityEngine;

public class WaterDropSlide : MonoBehaviour
{
    [Header("Slide Properties")]
    [SerializeField] private float slideSpeed = 0.4f;
    [SerializeField] private float gravityStrength = 7.0f;

    [Header("Splash Settings")]
    [SerializeField] private GameObject splashPrefab;
    [SerializeField] private float waterLevelY = 0.1f;

    private float leafRadius;
    private bool isSliding;
    private MeshRenderer leafMesh;

    public void Initialize(MeshRenderer targetLeafMesh)
    {
        leafMesh = targetLeafMesh;

        if (leafMesh != null)
        {
            float worldMaxExtent = Mathf.Max(leafMesh.bounds.extents.x, leafMesh.bounds.extents.z);
            float parentGlobalScale = transform.parent != null ? transform.parent.lossyScale.x : 1f;
            leafRadius = worldMaxExtent / Mathf.Max(0.0001f, parentGlobalScale);
        }
        else
        {
            leafRadius = 0.5f;
        }
    }

    public void StartSliding(Vector3 worldDirection)
    {
        if (isSliding || transform.parent == null)
        {
            return;
        }

        Vector3 localDir = transform.parent.InverseTransformDirection(worldDirection);
        localDir.y = 0f;
        if (localDir.sqrMagnitude < 0.0001f)
        {
            localDir = Vector3.forward;
        }

        StartCoroutine(SlideRoutine(localDir.normalized));
    }

    private IEnumerator SlideRoutine(Vector3 localDir)
    {
        isSliding = true;

        while (new Vector2(transform.localPosition.x, transform.localPosition.z).magnitude < leafRadius)
        {
            transform.localPosition += localDir * slideSpeed * Time.deltaTime;
            yield return null;
        }

        const float fallDuration = 0.8f;
        float elapsed = 0f;
        Vector3 fallVelocity = localDir * slideSpeed;

        while (elapsed < fallDuration)
        {
            elapsed += Time.deltaTime;
            fallVelocity.y -= gravityStrength * Time.deltaTime;
            transform.localPosition += fallVelocity * Time.deltaTime;
            transform.localScale *= 0.96f;

            if (transform.position.y <= waterLevelY)
            {
                SpawnSplashEffect();
                break;
            }

            yield return null;
        }

        Destroy(gameObject);
    }

    private void SpawnSplashEffect()
    {
        if (splashPrefab == null)
        {
            return;
        }

        Vector3 splashPosition = new Vector3(transform.position.x, waterLevelY, transform.position.z);
        GameObject splashInstance = Instantiate(splashPrefab, splashPosition, Quaternion.Euler(-90f, 0f, 0f));
        Destroy(splashInstance, 1.5f);
    }
}
