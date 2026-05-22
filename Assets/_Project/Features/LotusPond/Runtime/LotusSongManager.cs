using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class LotusSongManager : MonoBehaviour
{
    [Header("Song Data Assets")]
    public LotusSongData currentSong;
    [Tooltip("Pool of songs randomly chosen by StartRandomSong (used by lotus trigger).")]
    [SerializeField] private LotusSongData[] songPool;

    [Header("Feedback")]
    [SerializeField] private AudioSource errorAudioSource;
    [SerializeField] private AudioClip errorClip;
    
    [Header("Debug Controls")]
    [SerializeField] private bool enableKeyboardDebug = true;
    [SerializeField] private bool logDebugMessages;

    private int currentStep = 0;
    // Default to false: allows Free Play mode
    private bool isSongActive = false; 
    private int anticipatedStep = -1;
    private int anticipatedSourceNote = -1;
    private Dictionary<int, LotusGlowController> padMap = new Dictionary<int, LotusGlowController>();
    private readonly Dictionary<int, Queue<int>> pendingActivationStepsByNote = new Dictionary<int, Queue<int>>();
    private readonly HashSet<int> resolvedPendingSteps = new HashSet<int>();

    public bool IsSongActive => isSongActive;
    public LotusSongData CurrentSong => currentSong;

    public LotusMusicStaff musicStaff;

    void Start()
    {
        // Find and map all controllers in the scene
        LotusGlowController[] allControllers = FindObjectsByType<LotusGlowController>(FindObjectsSortMode.InstanceID);

        foreach (var ctrl in allControllers)
        {
            padMap[ctrl.noteId] = ctrl;

            // Subscribe to the note trigger event
            LotusNoteTrigger trigger = ctrl.GetComponent<LotusNoteTrigger>();
            if (trigger != null)
            {
                // The event will fire in both Free Play and Song Mode
                trigger.NoteActivationStarted += (t) => OnPadActivationStarted(ctrl.noteId);
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
                else if (logDebugMessages) Debug.LogWarning("[LotusSongManager] No song data assigned.");
            }
            else
            {
                StopSong();
            }
        }
    }

    /// <summary>
    /// Picks a random song from <see cref="songPool"/> and starts it.
    /// Bound to UnityEvents (e.g. lotus onTriggered) so the same trigger can present different scores.
    /// Falls back to <see cref="currentSong"/> if the pool is empty.
    /// </summary>
    public void StartRandomSong()
    {
        if (isSongActive) return;

        int poolSize = songPool != null ? songPool.Length : 0;
        if (poolSize == 0)
        {
            Debug.LogWarning($"[LotusSongManager] StartRandomSong: songPool is empty on '{name}'. " +
                             "Falling back to currentSong. Populate Song Pool in the Inspector to enable random selection.");
        }

        LotusSongData chosen = PickRandomSong();
        if (chosen == null)
        {
            Debug.LogWarning("[LotusSongManager] StartRandomSong: no song available (pool empty and currentSong null).");
            return;
        }

        if (logDebugMessages)
        {
            Debug.Log($"[LotusSongManager] StartRandomSong picked '{chosen.songName}' from pool of {poolSize}.");
        }

        StartSong(chosen);
    }

    private LotusSongData PickRandomSong()
    {
        if (songPool == null || songPool.Length == 0)
        {
            return currentSong;
        }

        int validCount = 0;
        for (int i = 0; i < songPool.Length; i++)
        {
            if (songPool[i] != null) validCount++;
        }

        if (validCount == 0) return currentSong;

        int pick = Random.Range(0, validCount);
        for (int i = 0; i < songPool.Length; i++)
        {
            if (songPool[i] == null) continue;
            if (pick == 0) return songPool[i];
            pick--;
        }

        return currentSong;
    }

    /// <summary>
    /// Activates Song Mode and starts the guidance.
    /// </summary>
    public void StartSong(LotusSongData song)
    {
        if (isSongActive) 
        {
            if (logDebugMessages)
            {
                Debug.Log("[LotusSongManager] Song already in progress. Ignoring restart.");
            }
            return; 
        }

        if (song == null)
        {
            return;
        }

        currentSong = song;

        if (musicStaff != null)
        {
            // Pass the current song data and the starting step (usually 0)
            musicStaff.OpenStaff(currentSong.sequence, 0);
            
            // You might also want to play a "Start" sound effect here
            if (logDebugMessages)
            {
                Debug.Log("[LotusSongManager] Music mode activated.");
            }
        }

        currentStep = 0;
        anticipatedStep = -1;
        anticipatedSourceNote = -1;
        pendingActivationStepsByNote.Clear();
        resolvedPendingSteps.Clear();
        isSongActive = true;
        
        HideAllGlows();
        ShowCurrentStepHint();
        
        if (logDebugMessages)
        {
            Debug.Log($"[LotusSongManager] Song mode started: {song.songName}");
        }
    }

    /// <summary>
    /// Deactivates Song Mode and returns to Free Play.
    /// </summary>
    public void StopSong()
    {
        isSongActive = false;
        currentStep = 0;
        anticipatedStep = -1;
        anticipatedSourceNote = -1;
        pendingActivationStepsByNote.Clear();
        resolvedPendingSteps.Clear();
        HideAllGlows();
        if (musicStaff != null) musicStaff.CloseStaff(); 
        if (logDebugMessages)
        {
            Debug.Log("[LotusSongManager] Song mode stopped. Free play enabled.");
        }
    }

    private void OnPadHit(int hitIndex)
    {
        // If not in Song Mode, we do nothing and let the user play freely
        if (!isSongActive || currentSong == null || currentStep >= currentSong.sequence.Count) return;

        if (TryConsumePendingActivation(hitIndex, out int resolvedStep))
        {
            resolvedPendingSteps.Add(resolvedStep);
            AdvanceResolvedSongSteps();
            return;
        }

        int targetIndex = currentSong.sequence[currentStep];

        // Song Mode Logic: Only proceed if the hit index matches the sequence
        if (hitIndex == targetIndex)
        {
            if (logDebugMessages)
            {
                Debug.Log($"[LotusSongManager] Correct note: {hitIndex}");
            }
            Vector3 bubbleTransferStart = padMap[hitIndex].CurrentBubbleWorldPosition;
            bool currentHintAlreadyTransferred = anticipatedSourceNote == hitIndex && anticipatedStep == currentStep + 1;
            if (!currentHintAlreadyTransferred)
            {
                padMap[hitIndex].SetGlowActive(false, true);
            }

            currentStep++;
            bool nextHintAlreadyAnticipated = anticipatedStep == currentStep;
            anticipatedStep = -1;
            anticipatedSourceNote = -1;
            if (musicStaff != null)
            {
                musicStaff.RefreshStaff(currentSong.sequence, currentStep);
            }

            if (currentStep >= currentSong.sequence.Count)
            {
                if (logDebugMessages)
                {
                    Debug.Log("[LotusSongManager] Song sequence complete.");
                }
                StopSong(); // Automatically return to Free Play
            }
            else if (!nextHintAlreadyAnticipated)
            {
                ShowCurrentStepHint(bubbleTransferStart);
            }
        }
        else
        {
            // Optional: Error feedback for wrong notes during Song Mode
            if (logDebugMessages)
            {
                Debug.Log($"[LotusSongManager] Wrong note. Expected {targetIndex}, hit {hitIndex}.");
            }
            if (errorAudioSource != null && errorClip != null)
            {
                errorAudioSource.PlayOneShot(errorClip);
            }
        }
    }

    private void ShowCurrentStepHint(Vector3? transferStartWorldPosition = null)
    {
        int nextId = currentSong.sequence[currentStep];
        if (padMap.ContainsKey(nextId))
        {
            if (transferStartWorldPosition.HasValue)
            {
                padMap[nextId].SetGlowActiveFrom(transferStartWorldPosition.Value);
            }
            else
            {
                padMap[nextId].SetGlowActive(true);
            }
        }
    }

    private void OnPadActivationStarted(int hitIndex)
    {
        if (!isSongActive || currentSong == null || currentStep >= currentSong.sequence.Count) return;

        if (!TryResolveActivationStep(hitIndex, out int activationStep)) return;
        if (!padMap.ContainsKey(hitIndex)) return;

        RegisterPendingActivation(hitIndex, activationStep);

        int nextStep = activationStep + 1;
        anticipatedStep = nextStep;
        anticipatedSourceNote = hitIndex;
        if (nextStep >= currentSong.sequence.Count)
        {
            padMap[hitIndex].SetGlowActive(false, false);
            return;
        }

        int nextId = currentSong.sequence[nextStep];
        if (!padMap.ContainsKey(nextId)) return;

        if (nextId == hitIndex)
        {
            padMap[hitIndex].PlaySameNoteHop();
            return;
        }

        Vector3 transferStart = padMap[hitIndex].CurrentBubbleWorldPosition;
        padMap[hitIndex].SetGlowActive(false, false);
        padMap[nextId].SetGlowActiveFrom(transferStart);
    }

    private bool TryResolveActivationStep(int hitIndex, out int activationStep)
    {
        activationStep = anticipatedStep >= currentStep ? anticipatedStep : currentStep;
        if (activationStep >= currentSong.sequence.Count)
        {
            return false;
        }

        return currentSong.sequence[activationStep] == hitIndex;
    }

    private void RegisterPendingActivation(int noteId, int step)
    {
        if (!pendingActivationStepsByNote.TryGetValue(noteId, out Queue<int> steps))
        {
            steps = new Queue<int>();
            pendingActivationStepsByNote[noteId] = steps;
        }

        steps.Enqueue(step);
    }

    private bool TryConsumePendingActivation(int noteId, out int step)
    {
        step = -1;
        if (!pendingActivationStepsByNote.TryGetValue(noteId, out Queue<int> steps) || steps.Count == 0)
        {
            return false;
        }

        step = steps.Dequeue();
        return true;
    }

    private void AdvanceResolvedSongSteps()
    {
        while (currentStep < currentSong.sequence.Count && resolvedPendingSteps.Remove(currentStep))
        {
            currentStep++;
        }

        if (musicStaff != null)
        {
            musicStaff.RefreshStaff(currentSong.sequence, currentStep);
        }

        if (currentStep >= currentSong.sequence.Count)
        {
            if (logDebugMessages)
            {
                Debug.Log("[LotusSongManager] Song sequence complete.");
            }
            StopSong();
            return;
        }

        if (anticipatedStep < currentStep)
        {
            anticipatedStep = -1;
            anticipatedSourceNote = -1;
            ShowCurrentStepHint();
        }
    }

    private void HideAllGlows()
    {
        foreach (var pad in padMap.Values) pad.SetGlowActive(false, false);
    }
}
