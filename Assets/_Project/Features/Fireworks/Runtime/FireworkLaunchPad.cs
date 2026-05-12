using System;
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

        private void Start()
        {
            if (controller == null)
            {
                controller = GetComponentInChildren<FireworkController>();
            }

            if (triggerOnStart)
            {
                TriggerLaunch();
            }
        }

        [ContextMenu("Showcase/Run Configured Sequence")]
        public void TriggerLaunch()
        {
            if (!CanTriggerShowcase())
            {
                return;
            }

            controller.PlaySequence();
        }

        [ContextMenu("Showcase/Run All Sequence")]
        public void TriggerAllShowcase()
        {
            if (!CanTriggerShowcase())
            {
                return;
            }

            controller.PlayAllSequence();
        }

        public void TriggerStar()
        {
            TriggerMathFirework(MathFireworkPattern.Star);
        }

        public void TriggerRing()
        {
            TriggerMathFirework(MathFireworkPattern.Ring);
        }

        public void TriggerHeart()
        {
            TriggerMathFirework(MathFireworkPattern.Heart);
        }

        public void TriggerFlower()
        {
            TriggerMathFirework(MathFireworkPattern.Flower);
        }

        public void TriggerSpiral()
        {
            TriggerMathFirework(MathFireworkPattern.Spiral);
        }

        public void TriggerTextFirework(string text)
        {
            if (!CanTriggerManualFirework())
            {
                return;
            }

            controller.LaunchTextFirework(text);
        }

        [ContextMenu("Showcase/Text")]
        public void TriggerShowcaseText()
        {
            if (!CanTriggerManualFirework())
            {
                return;
            }

            controller.LaunchShowcaseText();
        }

        [Obsolete("Use TriggerShowcaseText. This alias is kept only for older scene/menu bindings.")]
        public void TriggerDreamText()
        {
            TriggerShowcaseText();
        }

        [ContextMenu("Showcase/Math Heart")]
        public void TriggerMathHeart()
        {
            TriggerMathFirework(MathFireworkPattern.Heart);
        }

        [ContextMenu("Showcase/Math DNA Helix")]
        public void TriggerMathRing()
        {
            TriggerMathFirework(MathFireworkPattern.Ring);
        }

        [ContextMenu("Showcase/Math Spiral")]
        public void TriggerMathSpiral()
        {
            TriggerMathFirework(MathFireworkPattern.Spiral);
        }

        [ContextMenu("Showcase/Math Sphere")]
        public void TriggerMathSphere()
        {
            TriggerMathFirework(MathFireworkPattern.Sphere);
        }

        [ContextMenu("Showcase/Math Flower")]
        public void TriggerMathFlower()
        {
            TriggerMathFirework(MathFireworkPattern.Flower);
        }

        [ContextMenu("Showcase/Math Star")]
        public void TriggerMathStar()
        {
            TriggerMathFirework(MathFireworkPattern.Star);
        }

        [ContextMenu("Showcase/Math Mobius")]
        public void TriggerMathMobius()
        {
            TriggerMathFirework(MathFireworkPattern.Mobius);
        }

        public void TriggerPattern(int patternIndex)
        {
            if (!CanTriggerManualFirework())
            {
                return;
            }

            controller.PlayPattern(patternIndex);
        }

        private void TriggerMathFirework(MathFireworkPattern pattern)
        {
            if (!CanTriggerManualFirework())
            {
                return;
            }

            controller.LaunchMathFirework(pattern);
        }

        private bool CanTriggerShowcase()
        {
            if (!CanResolveController())
            {
                return false;
            }

            return !controller.IsShowcasePlaying || allowRetriggerWhilePlaying;
        }

        private bool CanTriggerManualFirework()
        {
            return CanResolveController();
        }

        private bool CanResolveController()
        {
            if (!Application.isPlaying)
            {
                Debug.Log("[Fireworks] Enter Play Mode to preview launches safely.");
                return false;
            }

            if (controller == null)
            {
                controller = GetComponentInChildren<FireworkController>();
            }

            if (controller == null)
            {
                Debug.LogWarning($"{nameof(FireworkLaunchPad)} on {name} has no {nameof(FireworkController)} assigned.", this);
                return false;
            }

            return true;
        }
    }
}
