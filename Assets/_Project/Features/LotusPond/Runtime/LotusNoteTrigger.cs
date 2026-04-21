using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class LotusNoteTrigger : MonoBehaviour
{
    public event System.Action<LotusNoteTrigger> NoteTriggered;

    public float CooldownSeconds => cooldownSeconds;

    [Header("Settings")]
    [SerializeField] private LotusScaleSettingsSO settings;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip noteClip;

    [Header("Ripple")]
    [SerializeField] private LotusRippleController rippleController;

    [Header("Trigger")]
    [SerializeField] private bool triggerOnlyOncePerStay = true;
    [SerializeField] private float cooldownSeconds = 1.0f;
    [SerializeField] private string[] allowedTags;
    [SerializeField] private bool logDebugMessages = true;

    [Header("Water Droplet Generation")]
    [SerializeField] private GameObject waterDropPrefab;
    [SerializeField] private int minDrops = 3;        
    [SerializeField] private int maxDrops = 7;          
    [SerializeField] private float spawnRadius = 0.4f;  

    private float nextAllowedTriggerTime;
    private bool objectStillInside;

    [Header("Wobble Settings (Physical Response)")]
    [SerializeField] private float wobbleIntensity = 5f; 
    [SerializeField] private float duration = 0.5f;      
    [SerializeField] private float stiffness = 200f;     // Higher = faster/snappier vibration
    [SerializeField] private float damping = 10f;        // Higher = stops faster (less like jelly)

    private Quaternion originalRotation;
    private Coroutine wobbleCoroutine;


    private void Start()
    {
       originalRotation = transform.localRotation;
       GenerateInitialDroplets();
    }
    private void Reset()
    {
        audioSource = GetComponent<AudioSource>();
        if (rippleController == null)
        {
            rippleController = GetComponentInChildren<LotusRippleController>(true);
        }
    }

    private void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (rippleController == null) rippleController = GetComponentInChildren<LotusRippleController>(true);

        ApplySettings();
    }
    private void OnValidate() => ApplySettings();

    private void OnTriggerEnter(Collider other) => TryTrigger(other);

    private void OnTriggerStay(Collider other)
    {
        if (!triggerOnlyOncePerStay) TryTrigger(other);
    }

    private void GenerateInitialDroplets()
    {
        if (waterDropPrefab == null) return;
        int count = Random.Range(minDrops, maxDrops + 1);
        // Get the MeshRenderer of the leaf itself
        MeshRenderer leafMesh = GetComponent<MeshRenderer>();

        for (int i = 0; i < count; i++)
        {
            Vector2 randomPoint = Random.insideUnitCircle * spawnRadius;
            Vector3 localPos = new Vector3(randomPoint.x, 1f, randomPoint.y); // Y微抬防止穿模

            GameObject drop = Instantiate(waterDropPrefab, transform);
            drop.transform.localPosition = localPos;

            float s = Random.Range(0.03f, 0.06f);
            
            drop.transform.localScale = new Vector3(s, s * 10f, s); 
            drop.transform.localRotation = Quaternion.Euler(0, Random.Range(0, 360), 0);

            WaterDropSlide slideScript = drop.GetComponent<WaterDropSlide>();
            if (slideScript != null && leafMesh != null)
            {
                slideScript.Initialize(leafMesh); // This sets the radius automatically!
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsAllowedCollider(other)) return;
        objectStillInside = false;
    }

    private void TryTrigger(Collider other)
    {
        if (!IsAllowedCollider(other)) return;
        if (triggerOnlyOncePerStay && objectStillInside) return;

        objectStillInside = true;
        // Default direction for physics trigger (from collider center)
        Vector3 hitDir = (other.transform.position - transform.position).normalized;
        TriggerNoteInternal($"LotusNoteTrigger fired by {other.name}", hitDir);
    }

    /// <summary>
    /// Public trigger for Raycasts. Call this and pass hit.point for point-of-impact deflection.
    /// </summary>
    public void TriggerNote(Vector3 worldHitPoint)
    {
        objectStillInside = false;
        // Calculate hit direction relative to the lotus center
        Vector3 localHitPoint = transform.InverseTransformPoint(worldHitPoint);
        Vector3 hitDir = new Vector3(localHitPoint.x, 0, localHitPoint.z).normalized;

        TriggerNoteInternal($"LotusNoteTrigger activated by Raycast at {worldHitPoint}", hitDir);
    }

    /// <summary>
    /// Overload for keyboard debug or simple triggers.
    /// </summary>
    public void TriggerNote()
    {
        objectStillInside = false;
        // Default deflection from the front
        TriggerNoteInternal($"LotusNoteTrigger activated via generic call", Vector3.forward);
    }

    private void TriggerNoteInternal(string debugMessage, Vector3 hitDir)
    {
        if (Time.time < nextAllowedTriggerTime) return;

        nextAllowedTriggerTime = Time.time + cooldownSeconds;

        // 1. Audio Logic
        if (audioSource != null)
        {
            if (noteClip != null) audioSource.PlayOneShot(noteClip);
            else audioSource.Play();
        }

        // 2. Visual Ripple Logic
        if (rippleController != null) rippleController.PlayRipple();

        // 3. Event Notification
        NoteTriggered?.Invoke(this);

        if (logDebugMessages) Debug.Log(debugMessage);

        // 4. Physical Wobble Logic
        if (wobbleCoroutine != null) StopCoroutine(wobbleCoroutine);
        wobbleCoroutine = StartCoroutine(DoPhysicalWobble(hitDir));

        // Validation Warnings
        if (audioSource == null) Debug.LogWarning($"No AudioSource on {name}");
        if (rippleController == null) Debug.LogWarning($"No RippleController on {name}");
    }

    private IEnumerator DoPhysicalWobble(Vector3 hitDir)
    {
        float elapsed = 0f;
        float velocity = wobbleIntensity * 10f; // Initial impulse velocity
        float currentAngle = 0f;

        // Calculate rotation axis perpendicular to hit direction (Cross product logic)
        // If hit on right (X+), rotate around Z- axis.
        Vector3 rotationAxis = new Vector3(hitDir.z, 0, -hitDir.x);

        WaterDropSlide[] drops = GetComponentsInChildren<WaterDropSlide>();
        foreach (var drop in drops)
        {
            drop.StartSliding(hitDir.normalized); 
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            // Spring Physics Calculation
            float force = -stiffness * currentAngle; 
            velocity += force * Time.deltaTime;
            velocity *= (1f - damping * Time.deltaTime); // Apply energy loss
            currentAngle += velocity * Time.deltaTime;

            // Apply rotation using AngleAxis along the calculated perpendicular axis
            transform.localRotation = originalRotation * Quaternion.AngleAxis(currentAngle, rotationAxis);

            yield return null;
            
            // Early exit if vibration is negligible
            if (Mathf.Abs(currentAngle) < 0.05f && elapsed > 0.2f) break;
        }

        transform.localRotation = originalRotation;
    }

    public void ConfigureDebug(AudioSource source, AudioClip clip)
    {
        audioSource = source;
        noteClip = clip;
        if (rippleController == null) rippleController = GetComponentInChildren<LotusRippleController>(true);
    }

    public void SetSettings(LotusScaleSettingsSO scaleSettings)
    {
        settings = scaleSettings;
        ApplySettings();
    }

    private bool IsAllowedCollider(Collider other)
    {
        if (other == null) return false;
        if (allowedTags == null || allowedTags.Length == 0) return true;

        for (int i = 0; i < allowedTags.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(allowedTags[i]) && other.CompareTag(allowedTags[i])) return true;
        }
        return false;
    }

    private void ApplySettings()
    {
        if (settings == null) return;

        cooldownSeconds = settings.cooldownSeconds;
        if (settings.sharedNoteClip != null) noteClip = settings.sharedNoteClip;

        if (audioSource != null)
        {
            audioSource.volume = settings.volume;
            audioSource.minDistance = settings.minDistance;
            audioSource.maxDistance = settings.maxDistance;
        }

        if (rippleController != null) rippleController.SetSettings(settings);
    }
}