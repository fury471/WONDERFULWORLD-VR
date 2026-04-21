using UnityEngine;

namespace WonderfulWorld.World.Shared.VfxHooks
{
    [DisallowMultipleComponent]
    public class LotusVfxHooks_lwa : MonoBehaviour
    {
        [Header("Source")]
        [SerializeField] private LotusNoteTrigger noteTrigger;
        [SerializeField] private LotusRippleController rippleController;

        [Header("Outputs")]
        [SerializeField] private bool setShaderGlobals = true;
        [SerializeField] private LotusVfxChannel_SO_lwa eventChannel;

        private void Reset()
        {
            noteTrigger = GetComponent<LotusNoteTrigger>();
            rippleController = GetComponentInChildren<LotusRippleController>(true);
        }

        private void OnEnable()
        {
            if (noteTrigger == null)
            {
                noteTrigger = FindFirstObjectByType<LotusNoteTrigger>();
            }

            if (rippleController == null)
            {
                rippleController = FindFirstObjectByType<LotusRippleController>();
            }

            if (noteTrigger != null)
            {
                noteTrigger.NoteTriggered += OnNoteTriggered;
            }

            if (rippleController != null)
            {
                rippleController.RippleStarted += OnRippleStarted;
            }
        }

        private void OnDisable()
        {
            if (noteTrigger != null)
            {
                noteTrigger.NoteTriggered -= OnNoteTriggered;
            }

            if (rippleController != null)
            {
                rippleController.RippleStarted -= OnRippleStarted;
            }
        }

        private void OnNoteTriggered(LotusNoteTrigger trigger)
        {
            Raise(trigger != null ? trigger.transform.position : transform.position, trigger != null ? trigger.CooldownSeconds : 0f);
        }

        private void OnRippleStarted()
        {
            Raise(rippleController != null ? rippleController.transform.position : transform.position, 0f);
        }

        private void Raise(Vector3 worldPosition, float cooldownSeconds)
        {
            LotusVfxEvent evt = new LotusVfxEvent
            {
                worldPosition = worldPosition,
                time = Time.time,
                cooldownSeconds = cooldownSeconds
            };

            if (setShaderGlobals)
            {
                WonderfulWorldVfxShaderGlobals_lwa.SetLastLotus(evt);
            }

            eventChannel?.Raise(evt);
        }
    }
}
