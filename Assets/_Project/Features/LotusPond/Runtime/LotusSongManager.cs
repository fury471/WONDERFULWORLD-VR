using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class LotusSongManager : MonoBehaviour
{
    [Header("Song Data Assets")]
    public LotusSongData currentSong; 

    [Header("Feedback")]
    [SerializeField] private AudioSource errorAudioSource;
    [SerializeField] private AudioClip errorClip;
    
    [Header("Debug Controls")]
    [SerializeField] private bool enableKeyboardDebug = true;

    private int currentStep = 0;
    private bool isSongActive = false;
    private Dictionary<int, LotusGlowController> padMap = new Dictionary<int, LotusGlowController>();

    public bool IsSongActive => isSongActive;
    public LotusSongData CurrentSong => currentSong;

    void Start()
    {
        // Automatically find and map all LotusGlowControllers in the scene[cite: 3]
        LotusGlowController[] allControllers = FindObjectsOfType<LotusGlowController>();

        foreach (var ctrl in allControllers)
        {
            padMap[ctrl.noteId] = ctrl;

            // Subscribe to the existing NoteTriggered event from LotusNoteTrigger[cite: 3]
            LotusNoteTrigger trigger = ctrl.GetComponent<LotusNoteTrigger>();
            if (trigger != null)
            {
                trigger.NoteTriggered += (t) => OnPadHit(ctrl.noteId);
            }
        }
    }

    void Update()
    {
        // Temporary Keyboard Debug: Start music without UI
       if (enableKeyboardDebug && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (currentSong != null) 
            {
                Debug.Log("[LotusSongManager] Debug Start: Space Key Pressed.");
                StartSong(currentSong);
            }
            else 
            {
                Debug.LogWarning("[LotusSongManager] No Song Data assigned!");
            }
        }
    }

    public void StartSong(LotusSongData song)
    {
        currentSong = song;
        currentStep = 0;
        isSongActive = true;
        
        HideAllGlows();
        ShowCurrentStepHint();
        
        Debug.Log($"[LotusSongManager] Starting song: {song.songName}");
    }

    public void StopSong()
    {
        isSongActive = false;
        currentStep = 0;
        HideAllGlows();
        Debug.Log("[LotusSongManager] Stopped song / Free play mode.");
    }

    private void OnPadHit(int hitIndex)
    {
        if (!isSongActive || currentSong == null || currentStep >= currentSong.sequence.Count) return;

        int targetIndex = currentSong.sequence[currentStep];

        if (hitIndex == targetIndex)
        {
            Debug.Log($"[CORRECT] Hit Note ID: {hitIndex}");
            padMap[hitIndex].SetGlowActive(false);

            currentStep++;

            if (currentStep >= currentSong.sequence.Count)
            {
                Debug.Log("[SONG FINISHED] Well done!");
                isSongActive = false;
                HideAllGlows();
            }
            else
            {
                ShowCurrentStepHint();
            }
        }
        else
        {
            Debug.Log($"[MISTAKE] Expected: {targetIndex}, but hit: {hitIndex}");
            if (errorAudioSource != null && errorClip != null)
            {
                errorAudioSource.PlayOneShot(errorClip);
            }
        }
    }

    private void ShowCurrentStepHint()
    {
        int nextId = currentSong.sequence[currentStep];
        if (padMap.ContainsKey(nextId))
        {
            padMap[nextId].SetGlowActive(true);
        }
    }

    private void HideAllGlows()
    {
        foreach (var pad in padMap.Values) pad.SetGlowActive(false);
    }
}
