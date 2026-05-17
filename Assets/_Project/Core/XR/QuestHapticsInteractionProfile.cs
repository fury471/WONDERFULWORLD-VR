using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Feedback;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

[DefaultExecutionOrder(-120)]
[DisallowMultipleComponent]
public sealed class QuestHapticsInteractionProfile : MonoBehaviour
{
    private const float DefaultFrequency = 0f;

    [Header("Haptic players")]
    [SerializeField] private HapticImpulsePlayer leftHapticPlayer = null;
    [SerializeField] private HapticImpulsePlayer rightHapticPlayer = null;
    [SerializeField, Range(0f, 1f)] private float controllerAmplitudeMultiplier = 0.55f;

    [Header("XRI feedback policy")]
    [SerializeField] private bool configureChildSimpleHaptics = true;
    [SerializeField] private bool disableRawHoverHaptics = true;
    [SerializeField] private bool disableTeleportInteractorHaptics = true;
    [SerializeField] private bool keepSelectEnteredHaptics = true;
    [SerializeField, Range(0f, 1f)] private float selectEnteredAmplitude = 0.18f;
    [SerializeField, Min(0f)] private float selectEnteredDuration = 0.025f;

    [Header("Stable hover affordance")]
    [SerializeField] private bool playStableHoverAffordancePulse = true;
    [SerializeField] private bool requireSelectableHoverTarget = true;
    [SerializeField, Range(0f, 1f)] private float stableHoverAmplitude = 0.08f;
    [SerializeField, Min(0f)] private float stableHoverDuration = 0.018f;
    [SerializeField, Min(0f)] private float stableHoverDwellTime = 0.18f;
    [SerializeField, Min(0f)] private float minimumHoverPulseSpacing = 0.35f;
    [SerializeField, Min(0f)] private float sameHoverTargetCooldown = 0.9f;
    [SerializeField] private string[] suppressedHoverNameContains =
    {
        "Particle",
        "Particles",
        "VFX",
        "Blowing_Flowers",
        "Falling_Leaves",
        "CherryPetals"
    };

    private readonly List<SimpleHapticFeedback> simpleHaptics = new List<SimpleHapticFeedback>(8);
    private readonly List<IXRHoverInteractor> subscribedHoverInteractors = new List<IXRHoverInteractor>(8);
    private readonly Dictionary<IXRHoverInteractor, HapticImpulsePlayer> hapticPlayersByInteractor = new Dictionary<IXRHoverInteractor, HapticImpulsePlayer>(8);
    private readonly Dictionary<IXRHoverInteractor, HoverCandidate> hoverCandidates = new Dictionary<IXRHoverInteractor, HoverCandidate>(8);
    private readonly Dictionary<int, float> lastStableHoverPulseTimes = new Dictionary<int, float>(16);
    private readonly Dictionary<int, float> lastStableHoverPulseByInteractor = new Dictionary<int, float>(8);

    private bool hoverSubscriptionsDirty = true;

    private struct HoverCandidate
    {
        public IXRHoverInteractable interactable;
        public HapticImpulsePlayer hapticPlayer;
        public float enteredTime;
        public bool pulsed;
    }

    private void Reset()
    {
        AutoWireReferences();
    }

    private void OnValidate()
    {
        controllerAmplitudeMultiplier = Mathf.Clamp01(controllerAmplitudeMultiplier);
        selectEnteredAmplitude = Mathf.Clamp01(selectEnteredAmplitude);
        selectEnteredDuration = Mathf.Max(0f, selectEnteredDuration);
        stableHoverAmplitude = Mathf.Clamp01(stableHoverAmplitude);
        stableHoverDuration = Mathf.Max(0f, stableHoverDuration);
        stableHoverDwellTime = Mathf.Max(0f, stableHoverDwellTime);
        minimumHoverPulseSpacing = Mathf.Max(0f, minimumHoverPulseSpacing);
        sameHoverTargetCooldown = Mathf.Max(0f, sameHoverTargetCooldown);

        AutoWireReferences();
    }

    private void Awake()
    {
        ApplyProfile();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        ApplyProfile();
        RebuildHoverSubscriptions();
    }

    private void Start()
    {
        ApplyProfile();
        RebuildHoverSubscriptions();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        UnsubscribeHoverInteractors();
    }

    private void Update()
    {
        if (hoverSubscriptionsDirty)
            RebuildHoverSubscriptions();

        UpdateStableHoverAffordancePulses();
    }

    [ContextMenu("Apply Quest Haptics Interaction Profile")]
    public void ApplyProfile()
    {
        AutoWireReferences();
        ConfigureHapticPlayers();
        ConfigureSimpleHapticFeedback();
        hoverSubscriptionsDirty = true;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyProfile();
    }

    private void AutoWireReferences()
    {
        if (leftHapticPlayer != null && rightHapticPlayer != null)
            return;

        HapticImpulsePlayer[] hapticPlayers = GetComponentsInChildren<HapticImpulsePlayer>(true);
        for (int i = 0; i < hapticPlayers.Length; i++)
        {
            HapticImpulsePlayer player = hapticPlayers[i];
            if (player == null)
                continue;

            string path = GetTransformPath(player.transform);
            if (leftHapticPlayer == null && ContainsOrdinalIgnoreCase(path, "Left Controller"))
            {
                leftHapticPlayer = player;
                continue;
            }

            if (rightHapticPlayer == null && ContainsOrdinalIgnoreCase(path, "Right Controller"))
                rightHapticPlayer = player;
        }
    }

    private void ConfigureHapticPlayers()
    {
        HapticImpulsePlayer[] hapticPlayers = GetComponentsInChildren<HapticImpulsePlayer>(true);
        for (int i = 0; i < hapticPlayers.Length; i++)
        {
            HapticImpulsePlayer player = hapticPlayers[i];
            if (player != null)
                player.amplitudeMultiplier = controllerAmplitudeMultiplier;
        }
    }

    private void ConfigureSimpleHapticFeedback()
    {
        if (!configureChildSimpleHaptics)
            return;

        simpleHaptics.Clear();
        hapticPlayersByInteractor.Clear();
        GetComponentsInChildren(true, simpleHaptics);

        for (int i = 0; i < simpleHaptics.Count; i++)
        {
            SimpleHapticFeedback feedback = simpleHaptics[i];
            if (feedback == null)
                continue;

            IXRInteractor source = feedback.GetInteractorSource();
            HapticImpulsePlayer player = feedback.hapticImpulsePlayer;
            if (source is IXRHoverInteractor hoverSource && player != null)
                hapticPlayersByInteractor[hoverSource] = player;

            bool isTeleportFeedback = IsTeleportInteractor(source) || TransformPathContains(feedback.transform, "Teleport");
            bool allowSelectEntered = keepSelectEnteredHaptics && !(disableTeleportInteractorHaptics && isTeleportFeedback);

            feedback.playSelectEntered = allowSelectEntered;
            ConfigureImpulseData(feedback.selectEnteredData, selectEnteredAmplitude, selectEnteredDuration);

            feedback.playSelectExited = false;
            ConfigureImpulseData(feedback.selectExitedData, selectEnteredAmplitude, selectEnteredDuration);

            feedback.playSelectCanceled = false;
            ConfigureImpulseData(feedback.selectCanceledData, selectEnteredAmplitude, selectEnteredDuration);

            if (disableRawHoverHaptics)
            {
                feedback.playHoverEntered = false;
                feedback.playHoverExited = false;
                feedback.playHoverCanceled = false;
                feedback.allowHoverHapticsWhileSelecting = false;
            }
        }
    }

    private void RebuildHoverSubscriptions()
    {
        hoverSubscriptionsDirty = false;
        UnsubscribeHoverInteractors();

        if (!isActiveAndEnabled || !playStableHoverAffordancePulse)
            return;

        MonoBehaviour[] behaviours = GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour == null || !(behaviour is IXRHoverInteractor hoverInteractor))
                continue;

            if (disableTeleportInteractorHaptics && IsTeleportInteractor(hoverInteractor))
                continue;

            hoverInteractor.hoverEntered.AddListener(OnHoverEntered);
            hoverInteractor.hoverExited.AddListener(OnHoverExited);
            subscribedHoverInteractors.Add(hoverInteractor);
        }
    }

    private void UnsubscribeHoverInteractors()
    {
        for (int i = 0; i < subscribedHoverInteractors.Count; i++)
        {
            IXRHoverInteractor hoverInteractor = subscribedHoverInteractors[i];
            if (hoverInteractor == null || (hoverInteractor is UnityEngine.Object unityObject && unityObject == null))
                continue;

            hoverInteractor.hoverEntered.RemoveListener(OnHoverEntered);
            hoverInteractor.hoverExited.RemoveListener(OnHoverExited);
        }

        subscribedHoverInteractors.Clear();
        hoverCandidates.Clear();
    }

    private void OnHoverEntered(HoverEnterEventArgs args)
    {
        if (args == null)
            return;

        IXRHoverInteractor interactor = args.interactorObject;
        IXRHoverInteractable interactable = args.interactableObject;
        if (!CanPulseForStableHover(interactor, interactable))
            return;

        HapticImpulsePlayer player = ResolveHapticPlayer(interactor);
        if (player == null)
            return;

        hoverCandidates[interactor] = new HoverCandidate
        {
            interactable = interactable,
            hapticPlayer = player,
            enteredTime = Time.unscaledTime,
            pulsed = false
        };
    }

    private void OnHoverExited(HoverExitEventArgs args)
    {
        if (args == null)
            return;

        IXRHoverInteractor interactor = args.interactorObject;
        if (interactor == null || !hoverCandidates.TryGetValue(interactor, out HoverCandidate candidate))
            return;

        if (ReferenceEquals(candidate.interactable, args.interactableObject))
            hoverCandidates.Remove(interactor);
    }

    private void UpdateStableHoverAffordancePulses()
    {
        if (!playStableHoverAffordancePulse || hoverCandidates.Count == 0)
            return;

        float now = Time.unscaledTime;
        List<IXRHoverInteractor> keysToRemove = null;
        List<IXRHoverInteractor> keysToUpdate = null;

        foreach (KeyValuePair<IXRHoverInteractor, HoverCandidate> entry in hoverCandidates)
        {
            IXRHoverInteractor interactor = entry.Key;
            HoverCandidate candidate = entry.Value;
            if (interactor == null || (interactor is UnityEngine.Object interactorObject && interactorObject == null) ||
                candidate.interactable == null || (candidate.interactable is UnityEngine.Object interactableObject && interactableObject == null) ||
                candidate.hapticPlayer == null)
            {
                AddKey(ref keysToRemove, interactor);
                continue;
            }

            if (!CanPulseForStableHover(interactor, candidate.interactable) || !interactor.IsHovering(candidate.interactable))
            {
                AddKey(ref keysToRemove, interactor);
                continue;
            }

            if (candidate.pulsed || now - candidate.enteredTime < stableHoverDwellTime)
                continue;

            int cooldownKey = MakeCooldownKey(interactor, candidate.interactable);
            if (lastStableHoverPulseTimes.TryGetValue(cooldownKey, out float lastPulseTime) &&
                now - lastPulseTime < sameHoverTargetCooldown)
            {
                AddKey(ref keysToUpdate, interactor);
                continue;
            }

            int interactorId = GetInstanceId(interactor);
            if (lastStableHoverPulseByInteractor.TryGetValue(interactorId, out float lastInteractorPulseTime) &&
                now - lastInteractorPulseTime < minimumHoverPulseSpacing)
            {
                continue;
            }

            candidate.hapticPlayer.SendHapticImpulse(stableHoverAmplitude, stableHoverDuration, DefaultFrequency);
            lastStableHoverPulseTimes[cooldownKey] = now;
            lastStableHoverPulseByInteractor[interactorId] = now;
            candidate.pulsed = true;

            AddKey(ref keysToUpdate, interactor);
        }

        if (keysToUpdate != null)
        {
            for (int i = 0; i < keysToUpdate.Count; i++)
            {
                IXRHoverInteractor interactor = keysToUpdate[i];
                if (interactor != null && hoverCandidates.TryGetValue(interactor, out HoverCandidate candidate))
                {
                    candidate.pulsed = true;
                    hoverCandidates[interactor] = candidate;
                }
            }
        }

        if (keysToRemove == null)
            return;

        for (int i = 0; i < keysToRemove.Count; i++)
            hoverCandidates.Remove(keysToRemove[i]);
    }

    private bool CanPulseForStableHover(IXRHoverInteractor interactor, IXRHoverInteractable interactable)
    {
        if (!playStableHoverAffordancePulse || interactor == null || interactable == null)
            return false;

        if (disableTeleportInteractorHaptics && IsTeleportInteractor(interactor))
            return false;

        if (IsTeleportInteractable(interactable))
            return false;

        if (requireSelectableHoverTarget &&
            !(interactable is IXRSelectInteractable) &&
            !(interactable is IXRActivateInteractable))
        {
            return false;
        }

        if (IsSuppressedVisualHoverTarget(interactable))
            return false;

        return true;
    }

    private HapticImpulsePlayer ResolveHapticPlayer(IXRHoverInteractor interactor)
    {
        if (interactor == null)
            return null;

        if (hapticPlayersByInteractor.TryGetValue(interactor, out HapticImpulsePlayer mappedPlayer) && mappedPlayer != null)
            return mappedPlayer;

        Component component = interactor as Component;
        if (component == null)
            return null;

        HapticImpulsePlayer parentPlayer = component.GetComponentInParent<HapticImpulsePlayer>(true);
        if (parentPlayer != null)
            return parentPlayer;

        string path = GetTransformPath(component.transform);
        if (ContainsOrdinalIgnoreCase(path, "Left Controller"))
            return leftHapticPlayer;

        if (ContainsOrdinalIgnoreCase(path, "Right Controller"))
            return rightHapticPlayer;

        return null;
    }

    private bool IsSuppressedVisualHoverTarget(IXRHoverInteractable interactable)
    {
        Component component = interactable as Component;
        if (component == null)
            return false;

        if (component.GetComponent<ParticleSystem>() != null || component.GetComponent<ParticleSystemRenderer>() != null)
            return true;

        string path = GetTransformPath(component.transform);
        for (int i = 0; i < suppressedHoverNameContains.Length; i++)
        {
            string token = suppressedHoverNameContains[i];
            if (!string.IsNullOrWhiteSpace(token) && ContainsOrdinalIgnoreCase(path, token))
                return true;
        }

        return false;
    }

    private static bool IsTeleportInteractor(IXRInteractor interactor)
    {
        Component component = interactor as Component;
        return component != null && TransformPathContains(component.transform, "Teleport");
    }

    private static bool IsTeleportInteractable(IXRHoverInteractable interactable)
    {
        Component component = interactable as Component;
        if (component == null)
            return false;

        return component.GetComponent<TeleportationArea>() != null ||
               component.GetComponent<TeleportationAnchor>() != null;
    }

    private static void ConfigureImpulseData(HapticImpulseData data, float amplitude, float duration)
    {
        if (data == null)
            return;

        data.amplitude = Mathf.Clamp01(amplitude);
        data.duration = Mathf.Max(0f, duration);
        data.frequency = DefaultFrequency;
    }

    private static void AddKey(ref List<IXRHoverInteractor> keys, IXRHoverInteractor interactor)
    {
        if (keys == null)
            keys = new List<IXRHoverInteractor>(4);

        keys.Add(interactor);
    }

    private static int MakeCooldownKey(IXRHoverInteractor interactor, IXRHoverInteractable interactable)
    {
        unchecked
        {
            int interactorId = GetInstanceId(interactor);
            int interactableId = GetInstanceId(interactable);
            return (interactorId * 397) ^ interactableId;
        }
    }

    private static int GetInstanceId(object value)
    {
        UnityEngine.Object unityObject = value as UnityEngine.Object;
        if (unityObject != null)
            return unityObject.GetInstanceID();

        return value != null ? value.GetHashCode() : 0;
    }

    private static bool TransformPathContains(Transform transform, string token)
    {
        return transform != null && ContainsOrdinalIgnoreCase(GetTransformPath(transform), token);
    }

    private static string GetTransformPath(Transform transform)
    {
        if (transform == null)
            return string.Empty;

        string path = transform.name;
        Transform parent = transform.parent;
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }

        return path;
    }

    private static bool ContainsOrdinalIgnoreCase(string value, string token)
    {
        return !string.IsNullOrEmpty(value) &&
               !string.IsNullOrEmpty(token) &&
               value.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
