using UnityEngine;
using UnityEngine.InputSystem;

namespace WonderfulWorld.Features.Fireworks
{
    [DisallowMultipleComponent]
    public class FireworkKeyboardTester : MonoBehaviour
    {
        [SerializeField] private FireworkLaunchPad launchPad;
        [SerializeField] private FireworkController controller;
        [SerializeField] private bool enableKeyboardShortcuts = true;
        [SerializeField] private bool logHelpOnStart = true;
        [SerializeField] private string keyboardTextOverride = string.Empty;

        private void Reset()
        {
            AutoAssignReferences();
        }

        private void Awake()
        {
            AutoAssignReferences();
        }

        private void Start()
        {
            if (logHelpOnStart)
            {
                Debug.Log("[FireworksKeyboard] T=text, C=configured sequence, A=all sequence, 1=heart, 2=DNA helix, 3=spiral, 4=sphere, 5=flower, 6=star, 7=mobius, Esc=stop.", this);
            }
        }

        private void Update()
        {
            if (!enableKeyboardShortcuts)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.tKey.wasPressedThisFrame)
            {
                TriggerText();
            }
            else if (keyboard.cKey.wasPressedThisFrame)
            {
                launchPad?.TriggerLaunch();
            }
            else if (keyboard.aKey.wasPressedThisFrame)
            {
                launchPad?.TriggerAllShowcase();
            }
            else if (keyboard.digit1Key.wasPressedThisFrame)
            {
                launchPad?.TriggerMathHeart();
            }
            else if (keyboard.digit2Key.wasPressedThisFrame)
            {
                launchPad?.TriggerMathRing();
            }
            else if (keyboard.digit3Key.wasPressedThisFrame)
            {
                launchPad?.TriggerMathSpiral();
            }
            else if (keyboard.digit4Key.wasPressedThisFrame)
            {
                launchPad?.TriggerMathSphere();
            }
            else if (keyboard.digit5Key.wasPressedThisFrame)
            {
                launchPad?.TriggerMathFlower();
            }
            else if (keyboard.digit6Key.wasPressedThisFrame)
            {
                launchPad?.TriggerMathStar();
            }
            else if (keyboard.digit7Key.wasPressedThisFrame)
            {
                launchPad?.TriggerMathMobius();
            }
            else if (keyboard.escapeKey.wasPressedThisFrame)
            {
                controller?.StopSequence();
            }
        }

        [ContextMenu("Fireworks Test/Text")]
        public void TriggerText()
        {
            if (launchPad == null)
            {
                AutoAssignReferences();
            }

            if (string.IsNullOrWhiteSpace(keyboardTextOverride))
            {
                launchPad?.TriggerShowcaseText();
            }
            else
            {
                launchPad?.TriggerTextFirework(keyboardTextOverride);
            }
        }

        [ContextMenu("Fireworks Test/Configured Sequence")]
        public void TriggerConfiguredSequence()
        {
            launchPad?.TriggerLaunch();
        }

        [ContextMenu("Fireworks Test/All Sequence")]
        public void TriggerAllSequence()
        {
            launchPad?.TriggerAllShowcase();
        }

        private void AutoAssignReferences()
        {
            if (launchPad == null)
            {
                launchPad = GetComponentInChildren<FireworkLaunchPad>(true);
                if (launchPad == null)
                {
                    launchPad = GetComponentInParent<FireworkLaunchPad>();
                }
            }

            if (controller == null)
            {
                controller = GetComponentInChildren<FireworkController>(true);
                if (controller == null)
                {
                    controller = GetComponentInParent<FireworkController>();
                }
            }
        }
    }
}
