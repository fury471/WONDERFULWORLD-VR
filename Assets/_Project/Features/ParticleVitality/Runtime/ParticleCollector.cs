using UnityEngine;
using UnityEngine.InputSystem;

public class ParticleCollector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ParticleShapeSystem shapeSystem;
    [SerializeField] private Transform leftHandTransform;
    [SerializeField] private Transform debugSource;
    [SerializeField] private ParticleSystem absorbParticleSystem;
    [SerializeField] private InputActionReference collectAction;

    [Header("Collection")]
    [SerializeField] private float collectRadius = 0.1f;
    [SerializeField] private bool requireDebugSource = true;

    [Header("Absorb Buildup")]
    [SerializeField] private float absorbBuildupSeconds = 1.8f;

    [Header("Prompt State")]
    [SerializeField] private float warningDurationSeconds = 2f;
    [SerializeField] private bool enableDebugLogs = true;

    private bool showCollectPrompt;
    private bool collectPromptConfirmed;
    private string warningMessage;
    private float collectPromptHideTime;
    private float warningHideTime;

    private float absorbHoldStartTime = -1f;

    private float simulationSpeed = -1f;

    private void Awake()
    {
        ConfigureAbsorbParticleSystem();
    }

    public bool IsWithinCollectRange
    {
        get
        {
            Transform source = GetActiveSource();
            return source != null
                && leftHandTransform != null
                && Vector3.Distance(leftHandTransform.position, source.position) <= collectRadius;
        }
    }

    private void OnEnable()
    {
        collectAction?.action?.Enable();
    }

    private void OnDisable()
    {
        collectAction?.action?.Disable();
    }

    private void Update()
    {
        if (shapeSystem == null || leftHandTransform == null)
        {
            return;
        }

        UpdateWindVfx();

        InputAction action = collectAction != null ? collectAction.action : null;
        if (action == null)
        {
            return;
        }

        if (action.WasPressedThisFrame())
        {

            shapeSystem.ClearParticles();
            LogDebug("Existing shaped particles cleared. Starting a new absorb cycle.");
            absorbHoldStartTime = Time.time;
        }


        if (action.WasReleasedThisFrame())
        {
            LogDebug("action.WasReleasedThisFrame.");
            shapeSystem.CaptureParticlesFromSystem(absorbParticleSystem);
            shapeSystem.CycleShape();
            StopWindVfxEmission();
        }
        if(!IsWithinCollectRange)
        {
            StopWindVfxEmission();
            shapeSystem.ClearParticles();
        }
    }


    private void UpdateWindVfx()
    {

        InputAction action = collectAction != null ? collectAction.action : null;
        bool isActive = action != null && action.IsPressed();
        
        if (isActive && IsWithinCollectRange )
        {
            // 只有当它还没播放时，才调用 Play()，避免每一帧都重复触发
            float holdDuration = Time.time - absorbHoldStartTime;
            float t = Mathf.Clamp01(holdDuration / absorbBuildupSeconds);
            var emission = absorbParticleSystem.emission;
            var main = absorbParticleSystem.main;


            if (!absorbParticleSystem.isPlaying)
            {
                main.simulationSpeed = 1f;
                absorbParticleSystem.Play();
                LogDebug("Particle System Started Playing.");
            }
            main.simulationSpeed = Mathf.Lerp(1.0f, 2.0f, t);
            emission.rateOverTimeMultiplier = 5f * Mathf.Pow(100f / 5f, t);
            main.gravityModifier = Mathf.Lerp(0.1f, 0.5f, t);
            main.startSpeed = Mathf.Lerp(2.0f, 8.0f, t);
            
        }
        else
        {
            // 只有当它正在播放时，才调用 Stop()
            if (absorbParticleSystem.isPlaying)
            {
                absorbParticleSystem.Stop(false, ParticleSystemStopBehavior.StopEmitting);
                var main = absorbParticleSystem.main;
                simulationSpeed = main.simulationSpeed;
                main.simulationSpeed = 0f;

            }
            if (action.WasReleasedThisFrame())
            {
                absorbHoldStartTime = -1f;
            }
        }
    }

    private void ConfigureAbsorbParticleSystem()
    {
        if (absorbParticleSystem == null)
        {
            return;
        }

        var main = absorbParticleSystem.main;
        main.playOnAwake = false;
    }

    private void StopWindVfxEmission()
    {
        if (absorbParticleSystem == null)
        {
            return;
        }

        var emission = absorbParticleSystem.emission;
        emission.rateOverTimeMultiplier = 0f;
        absorbHoldStartTime = -1f;
        absorbParticleSystem.Clear();
    }

    private float GetDistanceToSource()
    {
        Transform source = GetActiveSource();
        if (source == null || leftHandTransform == null)
        {
            return -1f;
        }

        return Vector3.Distance(leftHandTransform.position, source.position);
    }

    private void LogDebug(string message)
    {
        if (!enableDebugLogs)
        {
            return;
        }

        Debug.Log($"[ParticleCollector] {message}", this);
    }

    private Transform GetActiveSource()
    {
        if (debugSource != null)
        {
            return debugSource;
        }

        return requireDebugSource ? null : leftHandTransform;
    }


}