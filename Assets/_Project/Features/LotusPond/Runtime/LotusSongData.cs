using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewSong", menuName = "Lotus/Song Data")]
public class LotusSongData : ScriptableObject 
{
    public string songName;
    [Tooltip("ID sequence (0=A, 1=B, 2=C...)")]
    public List<int> sequence; 
}
