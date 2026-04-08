using UnityEngine;
using UnityEngine.InputSystem;

public class LotusPondDebugKeyboard : MonoBehaviour
{
    [System.Serializable]
    private struct PadBinding
    {
        public string padName;
        public Key triggerKey;
        public float pitch;
    }

    [Header("Audio")]
    [SerializeField] private LotusScaleSettingsSO settings;
    [SerializeField] private AudioClip sharedNoteClip;
    [SerializeField] private float volume = 0.8f;
    [SerializeField] private float minDistance = 1f;
    [SerializeField] private float maxDistance = 8f;

    [Header("Bindings")]
    [SerializeField] private PadBinding[] padBindings =
    {
        new PadBinding { padName = "LotusPad_A", triggerKey = Key.Z, pitch = 0.84f },
        new PadBinding { padName = "LotusPad_B", triggerKey = Key.X, pitch = 0.94f },
        new PadBinding { padName = "LotusPad_C", triggerKey = Key.C, pitch = 1.00f },
        new PadBinding { padName = "LotusPad_D", triggerKey = Key.V, pitch = 1.12f },
        new PadBinding { padName = "LotusPad_E", triggerKey = Key.B, pitch = 1.26f },
    };

    private LotusNoteTrigger[] triggers;

    private void Awake()
    {
        triggers = new LotusNoteTrigger[padBindings.Length];

        for (int i = 0; i < padBindings.Length; i++)
        {
            Transform pad = FindChildRecursive(transform, padBindings[i].padName);
            if (pad == null)
            {
                Debug.LogWarning($"LotusPondDebugKeyboard: could not find {padBindings[i].padName}.");
                continue;
            }

            LotusNoteTrigger trigger = pad.GetComponent<LotusNoteTrigger>();
            if (trigger == null)
            {
                Debug.LogWarning($"LotusPondDebugKeyboard: {padBindings[i].padName} is missing LotusNoteTrigger.");
                continue;
            }

            AudioSource audioSource = pad.GetComponent<AudioSource>();
            if (audioSource != null)
            {
                if (settings != null)
                {
                    trigger.SetSettings(settings);
                }

                audioSource.pitch = padBindings[i].pitch;
                trigger.ConfigureDebug(audioSource, settings != null && settings.sharedNoteClip != null ? settings.sharedNoteClip : sharedNoteClip);
            }

            triggers[i] = trigger;
        }
    }

    private void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        for (int i = 0; i < padBindings.Length; i++)
        {
            if (triggers[i] == null)
            {
                continue;
            }

            if (Keyboard.current[padBindings[i].triggerKey].wasPressedThisFrame)
            {
                triggers[i].TriggerNote();
            }
        }
    }

    private static Transform FindChildRecursive(Transform root, string targetName)
    {
        if (root.name == targetName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildRecursive(root.GetChild(i), targetName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
