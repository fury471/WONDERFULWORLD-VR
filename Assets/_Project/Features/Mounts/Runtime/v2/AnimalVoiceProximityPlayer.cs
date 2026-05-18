using UnityEngine;

public class AnimalVoiceProximityPlayer : MonoBehaviour
{
    [SerializeField] private AudioSource targetAudioSource;
    [SerializeField] private bool logDebug = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other == null)
        {
            return;
        }

        CharacterController playerController = other.GetComponentInParent<CharacterController>();
        if (playerController == null)
        {
            return;
        }

        if (targetAudioSource == null)
        {
            return;
        }

        if (!targetAudioSource.isPlaying)
        {
            targetAudioSource.Play();

            if (logDebug)
            {
                Debug.Log("[AnimalVoiceProximityPlayer] Started animal voice playback.");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other == null)
        {
            return;
        }

        CharacterController playerController = other.GetComponentInParent<CharacterController>();
        if (playerController == null)
        {
            return;
        }

        if (targetAudioSource == null)
        {
            return;
        }

        if (targetAudioSource.isPlaying)
        {
            targetAudioSource.Stop();

            if (logDebug)
            {
                Debug.Log("[AnimalVoiceProximityPlayer] Stopped animal voice playback.");
            }
        }
    }
}