using UnityEngine;

public class SimpleBreathingFeedback : MonoBehaviour
{
    [Header("Debug")]
    public bool enableDebugLog = true;

    [Header("Dependencies")]
    public Transform playerCamera;

    [Header("Breathing Settings")]
    public float breatheSpeed = 3f;
    public float breatheAmplitude = 0.05f;
    public float gazeThreshold = 0.85f;
    
    private Vector3 initialScale;
    private float debugLogTimer = 0f;

    private bool isCompleted = false; 

    private void Start() 
    {
        initialScale = transform.localScale;
        // --- AUTOMATIC CAMERA LINK ---
        if (playerCamera == null)
        {
            GameObject mainCam = GameObject.FindGameObjectWithTag("MainCamera");
            if (mainCam != null)
            {
                playerCamera = mainCam.transform;
                if (enableDebugLog) Debug.Log($"[Breathing] Linked to Camera: {mainCam.name}");
            }
        }
    }

    private void Update()
    {
        if (isCompleted || playerCamera == null) return;

        Vector3 directionToObject = (transform.position - playerCamera.position).normalized;
        float lookAlignment = Vector3.Dot(playerCamera.forward, directionToObject);
        bool isGazed = lookAlignment >= gazeThreshold;

        // --- DEBUG LOGGING ---
        if (enableDebugLog)
        {
            debugLogTimer += Time.deltaTime;
            if (debugLogTimer >= 0.5f) 
            {
                Debug.Log($"[Breathing Feedback] Object: {gameObject.name} | Align: {lookAlignment:F2} | Gazing: {isGazed}");
                debugLogTimer = 0f;
            }
        }
        // ---------------------

        if (isGazed)
        {
            float scaleOffset = Mathf.Sin(Time.time * breatheSpeed) * breatheAmplitude;
            transform.localScale = initialScale + new Vector3(scaleOffset, scaleOffset, scaleOffset);
        }
        else
        {
            transform.localScale = initialScale;
        }
    }

    public void MarkAsCompleted()
    {
        isCompleted = true; 
        transform.localScale = initialScale; 
    }
}