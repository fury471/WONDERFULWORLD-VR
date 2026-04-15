using System;

[Serializable]
public class ParkAttractionState
{
    public string attractionId;
    public string displayName;
    public bool availableAtStart;
    public bool discovered;
    public bool visited;
    public bool completed;
    public bool highlighted;
    public string notes;

    public void ResetRuntimeState()
    {
        discovered = false;
        visited = false;
        completed = false;
        highlighted = false;
        notes = string.Empty;
    }
}
