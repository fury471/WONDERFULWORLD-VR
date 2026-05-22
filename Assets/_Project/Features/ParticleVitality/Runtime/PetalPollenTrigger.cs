using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PetalPollenTrigger : MonoBehaviour
{
    [SerializeField] private List<PetalPollenSource> sources = new List<PetalPollenSource>();
    [SerializeField] private bool autoDiscoverChildSources = true;
    [SerializeField] private bool randomizeSources = true;

    private int nextSourceIndex;

    public PetalPollenSource PrimarySource
    {
        get
        {
            RefreshSourcesIfNeeded();
            return TryGetSource(0, out PetalPollenSource source) ? source : null;
        }
    }

    public Vector3 InteractionPosition => transform.position;

    public PetalPollenSource PickSource()
    {
        RefreshSourcesIfNeeded();
        RemoveMissingSources();

        if (sources.Count == 0)
        {
            return null;
        }

        if (randomizeSources)
        {
            return sources[Random.Range(0, sources.Count)];
        }

        PetalPollenSource source = sources[nextSourceIndex % sources.Count];
        nextSourceIndex++;
        return source;
    }

    public bool ContainsSource(PetalPollenSource source)
    {
        if (source == null)
        {
            return false;
        }

        RefreshSourcesIfNeeded();
        return sources.Contains(source);
    }

    public void SetInteractionFocus(float amount)
    {
        RefreshSourcesIfNeeded();
        for (int i = sources.Count - 1; i >= 0; i--)
        {
            PetalPollenSource source = sources[i];
            if (source == null)
            {
                sources.RemoveAt(i);
                continue;
            }

            source.SetInteractionFocus(amount);
        }
    }

    private void RefreshSourcesIfNeeded()
    {
        if (!autoDiscoverChildSources)
        {
            return;
        }

        for (int i = 0; i < sources.Count; i++)
        {
            if (sources[i] != null)
            {
                return;
            }
        }

        sources.Clear();
        sources.AddRange(GetComponentsInChildren<PetalPollenSource>(true));
    }

    private bool TryGetSource(int index, out PetalPollenSource source)
    {
        RemoveMissingSources();
        if (index < 0 || index >= sources.Count)
        {
            source = null;
            return false;
        }

        source = sources[index];
        return source != null;
    }

    private void RemoveMissingSources()
    {
        for (int i = sources.Count - 1; i >= 0; i--)
        {
            if (sources[i] == null)
            {
                sources.RemoveAt(i);
            }
        }
    }
}
