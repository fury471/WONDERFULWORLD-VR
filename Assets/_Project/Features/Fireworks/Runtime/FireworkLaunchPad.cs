using UnityEngine;

namespace WonderfulWorld.Features.Fireworks
{
    [DisallowMultipleComponent]
    public class FireworkLaunchPad : MonoBehaviour
    {
        [SerializeField] private FireworkController controller;
        [SerializeField] private bool triggerOnStart;
        [SerializeField] private bool allowRetriggerWhilePlaying;

        private void Reset()
        {
            controller = GetComponentInChildren<FireworkController>();
        }

        public bool IsShowcasePlaying
        {
            get
            {
                if (!ResolveController())
                {
                    return false;
                }

                return controller.IsShowcasePlaying;
            }
        }

        public bool CanTriggerShowcaseNow => CanTriggerShowcase();

        private void Start()
        {
            ResolveController();

            if (triggerOnStart)
            {
                TriggerShowcase();
            }
        }

        [ContextMenu("Fireworks/Trigger Showcase")]
        public void TriggerShowcase()
        {
            // 1. 如果有原配的 Controller，则执行原有逻辑
            if (controller != null || GetComponentInChildren<FireworkController>() != null)
            {
                if (ResolveController())
                {
                    if (!controller.IsShowcasePlaying || allowRetriggerWhilePlaying)
                    {
                        controller.PlaySequence();
                    }
                }
            }

       
        }

        public void TriggerShowcaseStep(int stepIndex)
        {
            if (!CanTriggerManualFirework()) return;
            controller.PlayShowcaseStep(stepIndex);
        }

        public void TriggerText(string text)
        {
            if (!CanTriggerManualFirework()) return;
            controller.LaunchTextFirework(text);
        }

        public void TriggerShowcaseText()
        {
            if (!CanTriggerManualFirework()) return;
            controller.LaunchShowcaseText();
        }

        public void StopShowcase()
        {
            if (ResolveController())
            {
                controller.StopSequence();
            }

         
        }

        private bool CanTriggerShowcase()
        {
            if (!ResolveController()) return false;
            return !controller.IsShowcasePlaying || allowRetriggerWhilePlaying;
        }

        private bool CanTriggerManualFirework()
        {
            return ResolveController();
        }

        private bool ResolveController()
        {
            if (controller == null)
            {
                controller = GetComponentInChildren<FireworkController>();
            }

            if (controller != null)
            {
                return true;
            }

        

            Debug.LogWarning($"{nameof(FireworkLaunchPad)} on {name} has no {nameof(FireworkController)} assigned.", this);
            return false;
        }
    }
}