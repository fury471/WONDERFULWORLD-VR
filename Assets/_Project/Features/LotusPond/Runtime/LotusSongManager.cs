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
    // Default to false: allows Free Play mode
    private bool isSongActive = false; 
    private Dictionary<int, LotusGlowController> padMap = new Dictionary<int, LotusGlowController>();

    public bool IsSongActive => isSongActive;
    public LotusSongData CurrentSong => currentSong;

    void Start()
    {
        // Find and map all controllers in the scene
        LotusGlowController[] allControllers = FindObjectsOfType<LotusGlowController>();

        foreach (var ctrl in allControllers)
        {
            padMap[ctrl.noteId] = ctrl;

            // Subscribe to the note trigger event
            LotusNoteTrigger trigger = ctrl.GetComponent<LotusNoteTrigger>();
            if (trigger != null)
            {
                // The event will fire in both Free Play and Song Mode
                trigger.NoteTriggered += (t) => OnPadHit(ctrl.noteId);
            }
        }
    }

    void Update()
    {
        // Debug toggle: Press Space to toggle between Free Play and Song Mode
        if (enableKeyboardDebug && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (!isSongActive)
            {
                if (currentSong != null) StartSong(currentSong);
                else Debug.LogWarning("[LotusSongManager] No Song Data assigned!");
            }
            else
            {
                StopSong();
            }
        }
    }

    /// <summary>
    /// Activates Song Mode and starts the guidance.
    /// </summary>
    public void StartSong(LotusSongData song)
    {
        if (isSongActive) 
        {
            Debug.Log("[LotusSongManager] Song already in progress. Ignoring restart.");
            return; 
        }

        if (song == null) return;
        currentSong = song;
        currentStep = 0;
        isSongActive = true;
        
        HideAllGlows();
        ShowCurrentStepHint();
        
        Debug.Log($"[LotusSongManager] Song Mode Started: {song.songName}");
    }

    /// <summary>
    /// Deactivates Song Mode and returns to Free Play.
    /// </summary>
    public void StopSong()
    {
        isSongActive = false;
        currentStep = 0;
        HideAllGlows();
        Debug.Log("[LotusSongManager] Song Mode Stopped. Free Play enabled.");
    }

    private void OnPadHit(int hitIndex)
    {
        // If not in Song Mode, we do nothing and let the user play freely
        if (!isSongActive || currentSong == null || currentStep >= currentSong.sequence.Count) return;

        int targetIndex = currentSong.sequence[currentStep];

        // Song Mode Logic: Only proceed if the hit index matches the sequence
        if (hitIndex == targetIndex)
        {
            Debug.Log($"[CORRECT] Hit Note ID: {hitIndex}");
            padMap[hitIndex].SetGlowActive(false);

            currentStep++;

            if (currentStep >= currentSong.sequence.Count)
            {
                Debug.Log("[SONG FINISHED] Sequence complete!");
                StopSong(); // Automatically return to Free Play
            }
            else
            {
                ShowCurrentStepHint();
            }
        }
        else
        {
            // Optional: Error feedback for wrong notes during Song Mode
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