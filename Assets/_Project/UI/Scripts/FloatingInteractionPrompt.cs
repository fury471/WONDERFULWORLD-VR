using UnityEngine;

public class FloatingInteractionPrompt : MonoBehaviour
{
    [Header("Debug")]
    [Tooltip("Check this to print distance and gaze data to the Console")]
    public bool enableDebugLog = true;

    [Header("UI References")]
    public CanvasGroup promptCanvasGroup;
    public Transform playerCamera;
   
   [Tooltip("The actual object to look at (e.g., the Mushroom). If null, uses UI's position.")]
    public Transform targetFocus;

    [Header("Display Rules")]
    public float triggerDistance = 20f;
    public float delayTime = 3f;
    public float fadeSpeed = 3f;
    public float gazeThreshold = 0.85f;

    [Header("Billboard & Float")]
    public bool faceCamera = true;
    public float yOffset = 0.0f;
    public bool floatUpDown = true;
    public float floatAmplitude = 0.03f;
    public float floatSpeed = 2.0f;

    private float currentTimer = 0f;
    private bool isCompleted = false;
    private Vector3 baseLocalPos;
    private float debugLogTimer = 0f; // Used to throttle the log output

    private void Start()
    {
        if (promptCanvasGroup != null) promptCanvasGroup.alpha = 0f;
        baseLocalPos = transform.localPosition;
        
        // If the Player Camera is not assigned in the Inspector (which is normal for prefabs)
        if (playerCamera == null)
        {
            // Automatically find the object with the "MainCamera" tag from your screenshot
            GameObject mainCam = GameObject.FindGameObjectWithTag("MainCamera");
            
            if (mainCam != null)
            {
                playerCamera = mainCam.transform;
                if (enableDebugLog) Debug.Log($"[Prompt] Successfully linked to: {mainCam.name}");
            }
            else
            {
                Debug.LogError("[Prompt] Cannot find an object with the 'MainCamera' tag in the scene!");
            }
        }
    }

    private void Update()
    {
        if (isCompleted || promptCanvasGroup == null || playerCamera == null)
        {
            if (enableDebugLog && playerCamera == null && Time.frameCount % 60 == 0) 
            {
                Debug.LogWarning("[Floating Prompt] Cannot run: Player Camera is missing!");
            }

            if (promptCanvasGroup != null) 
                promptCanvasGroup.alpha = Mathf.Lerp(promptCanvasGroup.alpha, 0f, Time.deltaTime * fadeSpeed);
            return;
        }

        Vector3 evaluationPoint = targetFocus != null ? targetFocus.position : transform.position;
        // 1. Calculate Distance and Gaze
        float distance = Vector3.Distance(playerCamera.position, evaluationPoint);
        Vector3 directionToObject = (evaluationPoint - playerCamera.position).normalized;
        float lookAlignment = Vector3.Dot(playerCamera.forward, directionToObject);
        bool isBeingLookedAt = lookAlignment >= gazeThreshold;

        // --- DEBUG LOGGING ---
        if (enableDebugLog)
        {
            debugLogTimer += Time.deltaTime;
            if (debugLogTimer >= 0.5f) // Print every 0.5 seconds to prevent spam
            {
                Debug.Log($"[Floating Prompt] Dist: {distance:F1}/{triggerDistance} | Align: {lookAlignment:F2}/{gazeThreshold} | Gazing: {isBeingLookedAt} | Timer: {currentTimer:F1}/{delayTime}");
                debugLogTimer = 0f;
            }
        }
        // ---------------------

        // 2. Apply Timer and Alpha
        if (distance <= triggerDistance && isBeingLookedAt)
        {
            currentTimer += Time.deltaTime;
            promptCanvasGroup.alpha = Mathf.Lerp(promptCanvasGroup.alpha, currentTimer >= delayTime ? 1f : 0f, Time.deltaTime * fadeSpeed);
        }
        else
        {
            currentTimer = 0f;
            promptCanvasGroup.alpha = Mathf.Lerp(promptCanvasGroup.alpha, 0f, Time.deltaTime * fadeSpeed);
        }
    }

    private void LateUpdate()
    {
        if (floatUpDown)
        {
            var p = baseLocalPos;
            p.y += Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
            transform.localPosition = p;
        }

        if (faceCamera && playerCamera != null)
        {
            var lookPos = playerCamera.position;
            lookPos.y = transform.position.y + yOffset;
            transform.LookAt(lookPos);
            transform.Rotate(0f, 180f, 0f);
        }
    }

    public void MarkAsCompleted() => isCompleted = true;
}