using UnityEngine;
using UnityEngine.InputSystem;

public class LotusNoteTrigger : MonoBehaviour
{
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

    [Header("Debug Input")]
    [SerializeField] private bool enableKeyboardDebugTrigger;
    [SerializeField] private Key debugTriggerKey = Key.Z;

    private float nextAllowedTriggerTime;
    private bool objectStillInside;

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
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (rippleController == null)
        {
            rippleController = GetComponentInChildren<LotusRippleController>(true);
        }
    }

    private void Update()
    {
        if (!enableKeyboardDebugTrigger || Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current[debugTriggerKey].wasPressedThisFrame)
        {
            TriggerNote();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryTrigger(other);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!triggerOnlyOncePerStay)
        {
            TryTrigger(other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsAllowedCollider(other))
        {
            return;
        }

        objectStillInside = false;
    }

    private void TryTrigger(Collider other)
    {
        if (!IsAllowedCollider(other))
        {
            return;
        }

        if (triggerOnlyOncePerStay && objectStillInside)
        {
            return;
        }

        objectStillInside = true;
        TriggerNoteInternal($"LotusNoteTrigger fired by {other.name} on {name}");
    }

    public void TriggerNote()
    {
        objectStillInside = false;
        TriggerNoteInternal($"LotusNoteTrigger activated on {name}");
    }

    public void ConfigureDebug(AudioSource source, AudioClip clip)
    {
        audioSource = source;
        noteClip = clip;

        if (rippleController == null)
        {
            rippleController = GetComponentInChildren<LotusRippleController>(true);
        }
    }

    private void TriggerNoteInternal(string debugMessage)
    {
        if (Time.time < nextAllowedTriggerTime)
        {
            return;
        }

        nextAllowedTriggerTime = Time.time + cooldownSeconds;

        if (audioSource != null)
        {
            if (noteClip != null)
            {
                audioSource.PlayOneShot(noteClip);
            }
            else
            {
                audioSource.Play();
            }
        }

        if (rippleController != null)
        {
            rippleController.PlayRipple();
        }

        if (logDebugMessages)
        {
            Debug.Log(debugMessage);
        }

        if (audioSource == null)
        {
            Debug.LogWarning($"LotusNoteTrigger on {name} has no AudioSource assigned.");
        }

        if (noteClip == null && audioSource != null && audioSource.clip == null)
        {
            Debug.LogWarning($"LotusNoteTrigger on {name} has no AudioClip assigned.");
        }

        if (rippleController == null)
        {
            Debug.LogWarning($"LotusNoteTrigger on {name} has no LotusRippleController assigned.");
        }
    }

    private bool IsAllowedCollider(Collider other)
    {
        if (other == null)
        {
            return false;
        }

        if (allowedTags == null || allowedTags.Length == 0)
        {
            return true;
        }

        for (int i = 0; i < allowedTags.Length; i++)
        {
            string tagName = allowedTags[i];
            if (!string.IsNullOrWhiteSpace(tagName) && other.CompareTag(tagName))
            {
                return true;
            }
        }

        return false;
    }
}
