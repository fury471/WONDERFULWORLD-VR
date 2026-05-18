using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WonderfulWorld.Features.Fireworks
{
    [DisallowMultipleComponent]
    public sealed class FireworkRandomParticlePlayer : MonoBehaviour
    {
        [SerializeField] private List<GameObject> fireworkLoopGroups = new List<GameObject>();
        [SerializeField] private bool autoPlayOnStart = true;
        [SerializeField] private float totalPlayDuration = 27f;
        [SerializeField] private float minDelayBetween = 1.5f;
        [SerializeField] private float maxDelayBetween = 3f;
        [SerializeField] private bool autoStopLoopingEffects = true;
        [SerializeField] private float effectPlayDuration = 5.5f;

        private Coroutine continuousRoutine;
        private Dictionary<GameObject, float> cooldownTimers = new Dictionary<GameObject, float>();

        private void Awake()
        {
            if (fireworkLoopGroups == null || fireworkLoopGroups.Count == 0)
            {
                CacheAndResetAllEffects();
            }
        }

        private void Start()
        {
            if (autoPlayOnStart)
            {
                PlayContinuousSequence();
            }
        }

        [ContextMenu("AutoFetch")]
        public void CacheAndResetAllEffects()
        {
            fireworkLoopGroups.Clear();
            foreach (Transform child in transform)
            {
                fireworkLoopGroups.Add(child.gameObject);
                child.gameObject.SetActive(false);
            }
        }

        public void PlayContinuousSequence()
        {
            if (continuousRoutine != null)
            {
                StopCoroutine(continuousRoutine);
            }
            continuousRoutine = StartCoroutine(ContinuousLaunchRoutine());
        }

        private IEnumerator ContinuousLaunchRoutine()
        {
            if (fireworkLoopGroups == null || fireworkLoopGroups.Count == 0) yield break;

            float elapsedTime = 0f;
            cooldownTimers.Clear();

            while (elapsedTime < totalPlayDuration)
            {
                List<GameObject> availableGroups = new List<GameObject>();
                foreach (var group in fireworkLoopGroups)
                {
                    if (group != null)
                    {
                        if (!cooldownTimers.ContainsKey(group) || Time.time >= cooldownTimers[group])
                        {
                            availableGroups.Add(group);
                        }
                    }
                }

                if (availableGroups.Count > 0)
                {
                    int randomIndex = Random.Range(0, availableGroups.Count);
                    GameObject selectedGroup = availableGroups[randomIndex];

                    cooldownTimers[selectedGroup] = Time.time + 3f;
                    StartCoroutine(PlaySingleFireworkGroup(selectedGroup));
                }

                float delay = Random.Range(minDelayBetween, maxDelayBetween);
                yield return new WaitForSeconds(delay);

                elapsedTime += delay;
            }
            continuousRoutine = null;
        }

        private IEnumerator PlaySingleFireworkGroup(GameObject groupObj)
        {
            groupObj.SetActive(true);

            ParticleSystem[] allParticles = groupObj.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in allParticles)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Play(true);
            }

            AudioSource[] allAudio = groupObj.GetComponentsInChildren<AudioSource>(true);
            foreach (var audio in allAudio)
            {
                audio.Stop();
                audio.Play();
            }

            if (autoStopLoopingEffects)
            {
                yield return new WaitForSeconds(effectPlayDuration);
                foreach (var ps in allParticles)
                {
                    if (ps != null) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }
            }
        }
    }
}