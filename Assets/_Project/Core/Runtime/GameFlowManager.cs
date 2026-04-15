using System.Collections.Generic;
using UnityEngine;

public class GameFlowManager : MonoBehaviour
{
    [Header("Bootstrap")]
    [SerializeField] private string startZoneId = "HumanEntry";
    [SerializeField] private bool onboardingCompleted;

    [Header("Attraction State")]
    [SerializeField] private List<ParkAttractionState> attractions = new List<ParkAttractionState>();
    [SerializeField] private string lastDiscoveredAttraction;
    [SerializeField] private string lastVisitedAttraction;

    public string StartZoneId => startZoneId;
    public bool OnboardingCompleted => onboardingCompleted;
    public IReadOnlyList<ParkAttractionState> Attractions => attractions;

    private void Reset()
    {
        EnsureDefaultAttractions();
    }

    private void Awake()
    {
        EnsureDefaultAttractions();
    }

    [ContextMenu("Reset Runtime State")]
    public void ResetRuntimeState()
    {
        onboardingCompleted = false;
        lastDiscoveredAttraction = string.Empty;
        lastVisitedAttraction = string.Empty;

        foreach (ParkAttractionState attraction in attractions)
        {
            attraction?.ResetRuntimeState();
        }

        Debug.Log("[GameFlow] Runtime state reset.");
    }

    public void CompleteOnboarding()
    {
        if (onboardingCompleted)
        {
            return;
        }

        onboardingCompleted = true;
        Debug.Log("[GameFlow] Onboarding completed. Player is now released into exploration.");
    }

    public void DiscoverAttraction(string attractionId)
    {
        ParkAttractionState state = GetOrCreateAttraction(attractionId);
        if (state.discovered)
        {
            return;
        }

        state.discovered = true;
        lastDiscoveredAttraction = attractionId;
        Debug.Log($"[GameFlow] Attraction discovered: {attractionId}");
    }

    public void VisitAttraction(string attractionId)
    {
        ParkAttractionState state = GetOrCreateAttraction(attractionId);
        state.visited = true;
        if (!state.discovered)
        {
            state.discovered = true;
        }

        lastVisitedAttraction = attractionId;
        Debug.Log($"[GameFlow] Attraction visited: {attractionId}");
    }

    public void CompleteAttraction(string attractionId)
    {
        ParkAttractionState state = GetOrCreateAttraction(attractionId);
        state.completed = true;
        state.visited = true;
        if (!state.discovered)
        {
            state.discovered = true;
        }

        Debug.Log($"[GameFlow] Attraction completed: {attractionId}");
    }

    public void SetHighlight(string attractionId, bool highlighted)
    {
        ParkAttractionState state = GetOrCreateAttraction(attractionId);
        state.highlighted = highlighted;
        Debug.Log($"[GameFlow] Attraction highlight {attractionId}: {highlighted}");
    }

    public bool TryGetAttraction(string attractionId, out ParkAttractionState state)
    {
        for (int i = 0; i < attractions.Count; i++)
        {
            ParkAttractionState current = attractions[i];
            if (current != null && current.attractionId == attractionId)
            {
                state = current;
                return true;
            }
        }

        state = null;
        return false;
    }

    private void EnsureDefaultAttractions()
    {
        AddIfMissing("HumanEntry", "Human Entry", true);
        AddIfMissing("FlowerField", "Flower Field", true);
        AddIfMissing("LotusPond", "Lotus Pond", true);
        AddIfMissing("CatRoute", "Cat Route", true);
        AddIfMissing("FireworksClearing", "Fireworks Clearing", true);
    }

    private void AddIfMissing(string attractionId, string displayName, bool availableAtStart)
    {
        if (TryGetAttraction(attractionId, out _))
        {
            return;
        }

        attractions.Add(new ParkAttractionState
        {
            attractionId = attractionId,
            displayName = displayName,
            availableAtStart = availableAtStart,
        });
    }

    private ParkAttractionState GetOrCreateAttraction(string attractionId)
    {
        if (TryGetAttraction(attractionId, out ParkAttractionState state))
        {
            return state;
        }

        state = new ParkAttractionState
        {
            attractionId = attractionId,
            displayName = attractionId,
            availableAtStart = false,
        };
        attractions.Add(state);
        return state;
    }
}
