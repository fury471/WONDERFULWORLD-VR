using UnityEngine;

[CreateAssetMenu(fileName = "GrowthProfile_SO", menuName = "Growth/Growth Profile")]
public class GrowthProfile_SO : ScriptableObject
{
    [System.Serializable]
    public struct PartState
    {
        [Range(0f, 1f)] public float time;
        public Vector3 localScale;
        public Vector3 localPosition;
    }

    [System.Serializable]
    public class PartProfile
    {
        public string partName;
        public PartState[] states;
    }

    [SerializeField] private PartProfile[] parts;
    public PartProfile[] Parts => parts;
}
