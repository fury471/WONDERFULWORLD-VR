using UnityEngine;

namespace WonderfulWorld.Features.Fireworks
{
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

        [ContextMenu("Trigger Launch")]
        public void TriggerLaunch()
        {
            if (controller == null)
            {
                Debug.LogWarning($"{nameof(FireworkLaunchPad)} on {name} has no {nameof(FireworkController)} assigned.", this);
                return;
            }

            if (controller.PatternCount < 3)
            {
                Debug.LogWarning($"{nameof(FireworkController)} should expose at least 3 patterns for M2 validation.", controller);
            }

            if (controller.IsPlaying && !allowRetriggerWhilePlaying)
            {
                return;
            }

            controller.PlaySequence();
        }

        public void TriggerStar()
        {
            TriggerPatternByShape(FireworkShape.Star);
        }

        public void TriggerRing()
        {
            TriggerPatternByShape(FireworkShape.Ring);
        }

        public void TriggerHeart()
        {
            TriggerPatternByShape(FireworkShape.Heart);
        }

        public void TriggerPattern(int patternIndex)
        {
            if (controller == null)
            {
                Debug.LogWarning($"{nameof(FireworkLaunchPad)} on {name} has no {nameof(FireworkController)} assigned.", this);
                return;
            }

            if (controller.IsPlaying && !allowRetriggerWhilePlaying)
            {
                return;
            }

            controller.PlayPattern(patternIndex);
        }

        private void TriggerPatternByShape(FireworkShape shape)
        {
            if (controller == null)
            {
                Debug.LogWarning($"{nameof(FireworkLaunchPad)} on {name} has no {nameof(FireworkController)} assigned.", this);
                return;
            }

            var patterns = controller.Patterns;
            for (int i = 0; i < patterns.Count; i++)
            {
                if (patterns[i].shape == shape)
                {
                    TriggerPattern(i);
                    return;
                }
            }

            Debug.LogWarning($"No firework pattern with shape {shape} was found on {nameof(FireworkController)}.", controller);
        }
    }
}
