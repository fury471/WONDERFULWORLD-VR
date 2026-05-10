using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class LotusMusicStaff : MonoBehaviour
{
    [Header("Transition Animation (Open/Close)")]
    public float transitionDuration = 0.5f; 
    public Vector3 normalScale = Vector3.one; 

    [Header("Note Prefab & Spacing")]
    public GameObject notePrefab;
    public float noteZSpacing = 1.5f; 
    public float horizontalOffset = -10.0f; 
    public float lerpSpeed = 10f; 

    [Header("Pitch Alignment")]
    public float firstLineY = 0f; 
    public float halfStepY = 0.05f; 

    [Header("Idle Animation (Bobbing & Breathing)")]
    public float bobSpeed = 3f;          
    public float bobAmplitude = 0.03f;   
    public float scaleAmplitude = 0.08f; 

    // NEW: Note Colors customization in Inspector
    [Header("Colors & Styling")]
    public Color pastNoteColor = new Color(1f, 1f, 1f, 0.2f); // Faded White
    public Color currentNoteColor = new Color(1f, 0.84f, 0f, 1f); // Default to a warm Gold/Yellow
    public Color futureNoteColor = new Color(1f, 1f, 1f, 0.7f); // Semi-transparent White

    // Object Pooling and Tracking
    private List<GameObject> pool = new List<GameObject>();
    private Dictionary<int, GameObject> noteMap = new Dictionary<int, GameObject>();
    private Dictionary<GameObject, Coroutine> activeCoroutines = new Dictionary<GameObject, Coroutine>();
    private Coroutine transitionCoroutine;

    private float GetYPosition(int noteId)
    {
        return firstLineY + noteId * halfStepY;
    }

    public void OpenStaff(List<int> sequence, int currentStep)
    {
        gameObject.SetActive(true);
        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
        if (transform.localScale == normalScale) transform.localScale = Vector3.zero;
        transitionCoroutine = StartCoroutine(AnimateScale(normalScale, true));
        RefreshStaff(sequence, currentStep);
    }

    public void CloseStaff()
    {
        if (!gameObject.activeInHierarchy) return;
        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
        transitionCoroutine = StartCoroutine(AnimateScale(Vector3.zero, false));
    }

    private IEnumerator AnimateScale(Vector3 targetScale, bool stayActive)
    {
        Vector3 startScale = transform.localScale;
        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);
            float smoothT = t * t * (3f - 2f * t); 
            transform.localScale = Vector3.Lerp(startScale, targetScale, smoothT);
            yield return null;
        }
        transform.localScale = targetScale;
        if (!stayActive) gameObject.SetActive(false);
    }

    public void RefreshStaff(List<int> sequence, int currentStep)
    {
        int startOffset = -5;
        int endOffset = 5;

        List<int> keysToRemove = new List<int>();
        foreach (var index in noteMap.Keys)
        {
            if (index < currentStep + startOffset || index > currentStep + endOffset)
            {
                GameObject oldNote = noteMap[index];
                if (activeCoroutines.ContainsKey(oldNote) && activeCoroutines[oldNote] != null)
                {
                    StopCoroutine(activeCoroutines[oldNote]);
                    activeCoroutines.Remove(oldNote);
                }
                oldNote.SetActive(false);
                pool.Add(oldNote);
                keysToRemove.Add(index);
            }
        }
        foreach (var key in keysToRemove) noteMap.Remove(key);

        for (int i = startOffset; i <= endOffset; i++)
        {
            int noteIndex = currentStep + i;
            if (noteIndex >= 0 && noteIndex < sequence.Count)
            {
                GameObject noteIcon;
                if (!noteMap.ContainsKey(noteIndex))
                {
                    noteIcon = GetFromPool();
                    noteMap[noteIndex] = noteIcon;
                    float initialZ = (-(i + 1) * noteZSpacing) + horizontalOffset; 
                    noteIcon.transform.localPosition = new Vector3(0, GetYPosition(sequence[noteIndex]), initialZ);
                    noteIcon.transform.localScale = Vector3.one; 
                }
                else
                {
                    noteIcon = noteMap[noteIndex];
                }

                if (activeCoroutines.ContainsKey(noteIcon) && activeCoroutines[noteIcon] != null)
                {
                    StopCoroutine(activeCoroutines[noteIcon]);
                }
                activeCoroutines[noteIcon] = StartCoroutine(SmoothMove(noteIcon, i, sequence[noteIndex], noteIndex));
                ApplyNoteStyle(noteIcon, i);
            }
        }
    }

    private IEnumerator SmoothMove(GameObject note, int relativeStep, int noteId, int absoluteIndex)
    {
        float targetY = GetYPosition(noteId);
        Vector3 currentBasePos = new Vector3(0, targetY, note.transform.localPosition.z);
        float targetBaseScale = (relativeStep < 0) ? 0.8f : (relativeStep == 0) ? 1.2f : 1.0f;
        float currentBaseScale = note.transform.localScale.x; 
        float phaseOffset = absoluteIndex * 0.8f;

        while (note != null && note.activeInHierarchy)
        {
            float targetZ = (-relativeStep * noteZSpacing) + horizontalOffset;
            Vector3 targetBasePos = new Vector3(0, targetY, targetZ);
            
            currentBasePos = Vector3.Lerp(currentBasePos, targetBasePos, Time.deltaTime * lerpSpeed);
            currentBaseScale = Mathf.Lerp(currentBaseScale, targetBaseScale, Time.deltaTime * lerpSpeed);

            float sineWave = Mathf.Sin(Time.time * bobSpeed + phaseOffset);
            float bobOffset = sineWave * bobAmplitude;
            float breatheOffset = sineWave * scaleAmplitude;

            note.transform.localPosition = new Vector3(currentBasePos.x, currentBasePos.y + bobOffset, currentBasePos.z);
            note.transform.localScale = Vector3.one * (currentBaseScale + breatheOffset);
            yield return null;
        }
    }

    private GameObject GetFromPool()
    {
        if (pool.Count > 0)
        {
            GameObject obj = pool[0];
            pool.RemoveAt(0);
            obj.SetActive(true);
            return obj;
        }
        return Instantiate(notePrefab, transform);
    }

    private void ApplyNoteStyle(GameObject icon, int relativeStep)
    {
        SpriteRenderer sr = icon.GetComponent<SpriteRenderer>();
        if (sr == null) return;
        ParticleSystem ps = icon.GetComponentInChildren<ParticleSystem>();

        // Apply customized colors from the Inspector
        if (relativeStep < 0) 
        {
            // Gradually fade out past notes based on the inspector color
           if (sr != null)
            {
                float alphaFade = Mathf.Lerp(0.05f, pastNoteColor.a, (float)(relativeStep + 5) / 4f);
                sr.color = new Color(pastNoteColor.r, pastNoteColor.g, pastNoteColor.b, alphaFade);
            }
            if (ps != null && ps.isPlaying) ps.Stop();
        }
        else if (relativeStep == 0) 
        {
            sr.color = currentNoteColor; // Use the custom selected color
            if (ps != null && !ps.isPlaying) ps.Play();
        }
        else 
        {
            sr.color = futureNoteColor; // Use the custom future color
            if (ps != null && ps.isPlaying)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); 
            }
        }
    }
}