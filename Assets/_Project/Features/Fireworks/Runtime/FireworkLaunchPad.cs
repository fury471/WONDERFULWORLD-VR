using System.Collections.Generic;
using UnityEngine;

namespace WonderfulWorld.Features.Fireworks
{
    [DisallowMultipleComponent]
    public class FireworkLaunchPad : MonoBehaviour
    {
        [Header("Primary Showcase")]
        [SerializeField] private FireworkController controller;
        [SerializeField] private bool triggerOnStart;
        [SerializeField] private bool allowRetriggerWhilePlaying;

        [Header("Integrated Particle Fireworks")]
        [SerializeField] private bool triggerAdditionalParticlesWithShowcase = true;
        [SerializeField] private bool autoResolveAdditionalParticlePlayers = true;
        [SerializeField] private List<FireworkRandomParticlePlayer> additionalParticlePlayers = new();

        private void Reset()
        {
            controller = GetComponentInChildren<FireworkController>(true);
            ResolveAdditionalParticlePlayers();
        }

        public bool IsShowcasePlaying => IsControllerPlaying() || AreAdditionalParticlesPlaying();

        public bool CanTriggerShowcaseNow => CanTriggerShowcase();

        private void Awake()
        {
            ResolveController(logIfMissing: false);
            ResolveAdditionalParticlePlayers();
        }

        private void Start()
        {
            if (triggerOnStart)
            {
                TriggerShowcase();
            }
        }

        [ContextMenu("Fireworks/Trigger Showcase")]
        public void TriggerShowcase()
        {
            bool triggeredAny = false;

            if (ResolveController(logIfMissing: false) && (!controller.IsShowcasePlaying || allowRetriggerWhilePlaying))
            {
                controller.PlaySequence();
                triggeredAny = true;
            }

            if (triggerAdditionalParticlesWithShowcase)
            {
                ResolveAdditionalParticlePlayers();
                for (int i = 0; i < additionalParticlePlayers.Count; i++)
                {
                    FireworkRandomParticlePlayer player = additionalParticlePlayers[i];
                    if (player == null)
                    {
                        continue;
                    }

                    if (!player.IsPlaying || allowRetriggerWhilePlaying)
                    {
                        player.PlayContinuousSequence();
                        triggeredAny = true;
                    }
                }
            }

            if (!triggeredAny)
            {
                Debug.LogWarning($"{nameof(FireworkLaunchPad)} on {name} has no available firework output to trigger.", this);
            }
        }

        public void TriggerShowcaseStep(int stepIndex)
        {
            if (!CanTriggerManualFirework())
            {
                return;
            }

            controller.PlayShowcaseStep(stepIndex);
        }

        public void TriggerText(string text)
        {
            if (!CanTriggerManualFirework())
            {
                return;
            }

            controller.LaunchTextFirework(text);
        }

        public void TriggerShowcaseText()
        {
            if (!CanTriggerManualFirework())
            {
                return;
            }

            controller.LaunchShowcaseText();
        }

        public void StopShowcase()
        {
            if (ResolveController(logIfMissing: false))
            {
                controller.StopSequence();
            }

            ResolveAdditionalParticlePlayers();
            for (int i = 0; i < additionalParticlePlayers.Count; i++)
            {
                if (additionalParticlePlayers[i] != null)
                {
                    additionalParticlePlayers[i].StopSequence();
                }
            }
        }

        private bool CanTriggerShowcase()
        {
            if (allowRetriggerWhilePlaying)
            {
                return HasAnyOutput();
            }

            return HasAnyOutput() && !IsShowcasePlaying;
        }

        private bool CanTriggerManualFirework()
        {
            return ResolveController(logIfMissing: true);
        }

        private bool HasAnyOutput()
        {
            bool hasController = ResolveController(logIfMissing: false);
            ResolveAdditionalParticlePlayers();
            return hasController || additionalParticlePlayers.Count > 0;
        }

        private bool IsControllerPlaying()
        {
            return ResolveController(logIfMissing: false) && controller.IsShowcasePlaying;
        }

        private bool AreAdditionalParticlesPlaying()
        {
            ResolveAdditionalParticlePlayers();
            for (int i = 0; i < additionalParticlePlayers.Count; i++)
            {
                if (additionalParticlePlayers[i] != null && additionalParticlePlayers[i].IsPlaying)
                {
                    return true;
                }
            }

            return false;
        }

        private bool ResolveController(bool logIfMissing)
        {
            if (controller == null)
            {
                controller = GetComponentInChildren<FireworkController>(true);
            }

            if (controller != null)
            {
                return true;
            }

            if (logIfMissing)
            {
                Debug.LogWarning($"{nameof(FireworkLaunchPad)} on {name} has no {nameof(FireworkController)} assigned.", this);
            }

            return false;
        }

        private void ResolveAdditionalParticlePlayers()
        {
            additionalParticlePlayers.RemoveAll(player => player == null);
            if (!autoResolveAdditionalParticlePlayers)
            {
                return;
            }

            Transform searchRoot = transform.parent != null ? transform.parent : transform;
            FireworkRandomParticlePlayer[] players = searchRoot.GetComponentsInChildren<FireworkRandomParticlePlayer>(true);
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i] != null && !additionalParticlePlayers.Contains(players[i]))
                {
                    additionalParticlePlayers.Add(players[i]);
                }
            }
        }
    }
}
