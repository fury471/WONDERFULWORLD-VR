using System.Reflection;
using UnityEngine;

namespace WonderfulWorld.Audio
{
    [DisallowMultipleComponent]
    public sealed class MountFootstepAudio : MonoBehaviour
    {
        private const string FootstepSourceName = "Audio_Mount_Footsteps";

        [SerializeField] private WonderlandAudioCue cue;
        [SerializeField] private Transform movementRoot;
        [SerializeField] private Transform emitter;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private bool useProfileOverride;
        [SerializeField] private MountFootstepProfile profileOverride = MountFootstepProfile.Cat;
        [SerializeField] private bool requireActiveRide = true;
        [SerializeField] private bool allowOverlap = false;
        [SerializeField, Min(0f)] private float minimumSpeed = 0.12f;
        [SerializeField, Min(0.01f)] private float walkStepInterval = 0.48f;
        [SerializeField, Min(0.01f)] private float runStepInterval = 0.22f;
        [SerializeField, Min(0.01f)] private float speedForRunInterval = 5f;
        [SerializeField, Range(0f, 1f)] private float volumeScale = 1f;
        [SerializeField, Min(0f)] private float startupRandomDelay = 0.2f;
        [SerializeField, Min(0f)] private float activationGraceSeconds = 0.25f;

        private Vector3 lastPosition;
        private float stepTimer;
        private float activatedAtTime;
        private float nextAllowedPlayTime;
        private bool hasLastPosition;
        private MonoBehaviour rideStateProvider;
        private PropertyInfo rideActiveProperty;
        private FieldInfo rideStateField;

        public bool HasProfileOverride => useProfileOverride;
        public MountFootstepProfile ProfileOverride => profileOverride;

        private void Reset()
        {
            movementRoot = transform;
            emitter = transform;
            audioSource = null;
        }

        private void Awake()
        {
            if (cue != null)
            {
                ConfigureSource();
            }

            PrimePosition();
        }

        private void OnEnable()
        {
            activatedAtTime = Time.time;
            PrimePosition();
        }

        public void Configure(WonderlandAudioCue cue, Transform movementRoot, Transform emitter, MountFootstepProfile profile)
        {
            this.cue = cue;
            this.movementRoot = movementRoot != null ? movementRoot : transform;
            this.emitter = emitter != null ? emitter : transform;
            useProfileOverride = true;
            profileOverride = profile;

            switch (profile)
            {
                case MountFootstepProfile.Horse:
                    minimumSpeed = 0.16f;
                    walkStepInterval = 0.42f;
                    runStepInterval = 0.18f;
                    speedForRunInterval = 5.6f;
                    volumeScale = 0.62f;
                    break;
                case MountFootstepProfile.Dog:
                    minimumSpeed = 0.15f;
                    walkStepInterval = 0.34f;
                    runStepInterval = 0.16f;
                    speedForRunInterval = 4.8f;
                    volumeScale = 0.58f;
                    break;
                default:
                    minimumSpeed = 0.12f;
                    walkStepInterval = 0.38f;
                    runStepInterval = 0.18f;
                    speedForRunInterval = 4.5f;
                    volumeScale = 0.5f;
                    break;
            }

            startupRandomDelay = 0.18f;
            requireActiveRide = true;
            allowOverlap = false;
            activatedAtTime = Time.time;
            ConfigureSource();
            CacheRideStateProvider();
            PrimePosition();
        }

        private void Update()
        {
            Transform root = movementRoot != null ? movementRoot : transform;
            if (!CanPlayForCurrentRideState())
            {
                StopCurrentFootstep();
                PrimePosition();
                return;
            }

            if (!hasLastPosition)
            {
                PrimePosition();
                return;
            }

            Vector3 currentPosition = root.position;
            Vector3 delta = currentPosition - lastPosition;
            delta.y = 0f;
            lastPosition = currentPosition;

            if (Time.time - activatedAtTime < activationGraceSeconds)
            {
                return;
            }

            float deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);
            float speed = delta.magnitude / deltaTime;
            if (speed < minimumSpeed)
            {
                StopCurrentFootstep();
                stepTimer = Mathf.Min(stepTimer, walkStepInterval);
                return;
            }

            stepTimer -= Time.deltaTime;
            if (stepTimer > 0f)
            {
                return;
            }

            PlayStep(speed);
            stepTimer = ResolveInterval(speed);
        }

        private void ConfigureSource()
        {
            GameObject sourceOwner = ResolveDedicatedSourceOwner();

            if (audioSource == null)
            {
                audioSource = sourceOwner.AddComponent<AudioSource>();
            }

            if (cue != null)
            {
                cue.ApplyTo(audioSource, assignClip: false);
            }

            audioSource.volume = 1f;
            audioSource.loop = false;
            audioSource.playOnAwake = false;
        }

        private GameObject ResolveDedicatedSourceOwner()
        {
            Transform ownerRoot = emitter != null ? emitter : transform;
            Transform sourceTransform = ownerRoot.Find(FootstepSourceName);
            if (sourceTransform == null)
            {
                GameObject sourceObject = new GameObject(FootstepSourceName);
                sourceTransform = sourceObject.transform;
                sourceTransform.SetParent(ownerRoot, false);
            }

            if (audioSource == null || audioSource.gameObject != sourceTransform.gameObject)
            {
                audioSource = sourceTransform.GetComponent<AudioSource>();
            }

            return sourceTransform.gameObject;
        }

        private void PrimePosition()
        {
            Transform root = movementRoot != null ? movementRoot : transform;
            lastPosition = root.position;
            hasLastPosition = true;
            stepTimer = startupRandomDelay > 0f ? Random.Range(0f, startupRandomDelay) : 0f;
        }

        private void PlayStep(float speed)
        {
            if (cue == null)
            {
                return;
            }

            AudioClip clip = cue.PickClip();
            if (clip == null)
            {
                return;
            }

            ConfigureSource();
            if (!allowOverlap && Time.time < nextAllowedPlayTime)
            {
                return;
            }

            if (!allowOverlap && audioSource.isPlaying)
            {
                return;
            }

            audioSource.pitch = cue.ResolvePitch();
            float speedScale = Mathf.Lerp(0.82f, 1f, Mathf.InverseLerp(minimumSpeed, speedForRunInterval, speed));
            audioSource.PlayOneShot(clip, cue.ResolveVolume(volumeScale * speedScale));
            if (!allowOverlap)
            {
                nextAllowedPlayTime = Time.time + Mathf.Max(0.05f, clip.length * 0.9f);
            }
        }

        private float ResolveInterval(float speed)
        {
            float t = Mathf.InverseLerp(minimumSpeed, speedForRunInterval, speed);
            return Mathf.Lerp(walkStepInterval, runStepInterval, t);
        }

        private bool CanPlayForCurrentRideState()
        {
            if (!requireActiveRide)
            {
                return true;
            }

            if (rideStateProvider == null)
            {
                CacheRideStateProvider();
            }

            if (rideStateProvider == null)
            {
                return false;
            }

            if (rideActiveProperty != null && rideActiveProperty.PropertyType == typeof(bool))
            {
                return (bool)rideActiveProperty.GetValue(rideStateProvider);
            }

            if (rideStateField != null)
            {
                object state = rideStateField.GetValue(rideStateProvider);
                return state != null && state.ToString() != "Idle";
            }

            return false;
        }

        private void CacheRideStateProvider()
        {
            rideStateProvider = null;
            rideActiveProperty = null;
            rideStateField = null;

            Transform root = movementRoot != null ? movementRoot : transform;
            MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null)
                {
                    continue;
                }

                System.Type type = behaviour.GetType();
                PropertyInfo property = type.GetProperty("IsRideActive", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (property != null && property.PropertyType == typeof(bool))
                {
                    rideStateProvider = behaviour;
                    rideActiveProperty = property;
                    return;
                }

                FieldInfo field = type.GetField("currentState", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null)
                {
                    rideStateProvider = behaviour;
                    rideStateField = field;
                }
            }
        }

        private void StopCurrentFootstep()
        {
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }

            nextAllowedPlayTime = 0f;
        }
    }
}
