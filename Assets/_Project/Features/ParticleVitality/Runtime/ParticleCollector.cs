using UnityEngine;
using UnityEngine.InputSystem;

public class ParticleCollector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ParticleShapeSystem shapeSystem;
    [SerializeField] private Transform leftHandTransform;
    [SerializeField] private Transform debugSource;
    [SerializeField] private ParticleSystem absorbParticleSystem;

    [Header("Collection")]
    [SerializeField] private float collectRadius = 1.5f;
    [SerializeField] private int minParticlesPerBurst = 1;
    [SerializeField] private int maxParticlesPerBurst = 5;
    [SerializeField] private float burstIntervalSeconds = 0.12f;
    [SerializeField] private bool requireDebugSource = true;

    [Header("Absorb Buildup")]
    [SerializeField] private float absorbBuildupSeconds = 1.8f;

    [Header("Wind VFX")]
    [SerializeField] private float blossomFieldWidth = 1.25f;
    [SerializeField] private float blossomFieldHeight = 0.7f;
    [SerializeField] private float blossomFieldDepth = 0.6f;
    [SerializeField] private Color blossomColor = new(1f, 0.45f, 0.82f, 1f);
    [SerializeField] private float minEmissionRate = 3f;
    [SerializeField] private float maxEmissionRate = 18f;
    [SerializeField] private float minStartSpeed = 0.002f;
    [SerializeField] private float maxStartSpeed = 0.015f;
    [SerializeField] private float minStartLifetime = 3.2f;
    [SerializeField] private float maxStartLifetime = 5.2f;
    [SerializeField] private float minStartSize = 0.018f;
    [SerializeField] private float maxStartSize = 0.032f;
    [SerializeField] private float minNoiseStrength = 0.1f;
    [SerializeField] private float maxNoiseStrength = 0.22f;
    [SerializeField] private float minOrbitalVelocity = 0.01f;
    [SerializeField] private float maxOrbitalVelocity = 0.08f;
    [SerializeField] private float minRadialVelocity = -0.005f;
    [SerializeField] private float maxRadialVelocity = -0.035f;
    [SerializeField] private float minUpwardVelocity = 0.01f;
    [SerializeField] private float maxUpwardVelocity = 0.035f;
    [SerializeField] private float sideDriftStrength = 0.03f;
    [SerializeField] private float forwardDriftStrength = 0.012f;
    [SerializeField] private float minColorAlpha = 0.35f;
    [SerializeField] private float maxColorAlpha = 0.85f;

    [Header("Prompt UI")]
    [SerializeField] private float promptAutoHideSeconds = 5f;
    [SerializeField] private float warningDurationSeconds = 2f;
    [SerializeField] private bool enableDebugLogs = true;

    private bool showCollectPrompt;
    private bool collectPromptConfirmed;
    private bool wasWithinCollectRangeLastFrame;
    private bool shouldShowShapeSelectionOnRelease;
    private string warningMessage;
    private float collectPromptHideTime;
    private float warningHideTime;
    private float nextBurstTime;
    private float absorbHoldStartTime = -1f;

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

    private void Update()
    {
        if (shapeSystem == null || leftHandTransform == null)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        UpdateCollectPromptState();
        UpdateWindVfx();

        if (keyboard.gKey.wasPressedThisFrame)
        {
            if (shapeSystem.CurrentShape != ParticleShapeSystem.ShapeType.HoldCloud
                && shapeSystem.ActiveParticleCount > 0)
            {
                shapeSystem.ClearParticles();
                LogDebug("Existing shaped particles cleared. Starting a new absorb cycle.");
            }

            nextBurstTime = 0f;
            shouldShowShapeSelectionOnRelease = false;
            absorbHoldStartTime = Time.time;
        }

        if (keyboard.gKey.isPressed)
        {
            bool startedAbsorption = TryCollectWhileHeld();
            shouldShowShapeSelectionOnRelease |= startedAbsorption;
        }

        if (keyboard.gKey.wasReleasedThisFrame && shouldShowShapeSelectionOnRelease)
        {
            shapeSystem.ShowShapeSelection();
            shouldShowShapeSelectionOnRelease = false;
            nextBurstTime = 0f;
            absorbHoldStartTime = -1f;
            StopWindVfxEmission();
        }
        else if (keyboard.gKey.wasReleasedThisFrame)
        {
            absorbHoldStartTime = -1f;
            StopWindVfxEmission();
        }

        if (keyboard.tabKey.wasPressedThisFrame)
        {
            shapeSystem.CycleShape();
        }
    }

    private void UpdateCollectPromptState()
    {
        bool isWithinRange = IsWithinCollectRange;

        if (isWithinRange && !wasWithinCollectRangeLastFrame)
        {
            showCollectPrompt = true;
            collectPromptConfirmed = false;
            collectPromptHideTime = Time.time + promptAutoHideSeconds;
            warningMessage = string.Empty;
            warningHideTime = 0f;
            LogDebug($"Entered collect range. Prompt shown. Distance={GetDistanceToSource():F3}");
        }

        if (!isWithinRange)
        {
            if (showCollectPrompt)
            {
                LogDebug($"Left collect range. Prompt cleared. Distance={GetDistanceToSource():F3}");
            }

            showCollectPrompt = false;
            collectPromptConfirmed = false;
        }
        else if (showCollectPrompt && Time.time >= collectPromptHideTime)
        {
            showCollectPrompt = false;
            LogDebug("Collect prompt auto-hidden after timeout.");
        }

        if (!string.IsNullOrEmpty(warningMessage) && Time.time >= warningHideTime)
        {
            warningMessage = string.Empty;
            warningHideTime = 0f;
        }

        wasWithinCollectRangeLastFrame = isWithinRange;
    }

    private bool TryCollectWhileHeld()
    {
        if (!shapeSystem.CanAcceptParticle)
        {
            ShowWarning("Particle capacity is full.");
            LogDebug("Press G ignored: particle capacity is full.");
            return false;
        }

        Transform source = GetActiveSource();
        if (source == null)
        {
            ShowWarning("No particle source is assigned.");
            LogDebug("Press G ignored: no active particle source.");
            return false;
        }

        if (!IsWithinCollectRange)
        {
            ShowWarning("Out of range. Move closer to the source first.");
            LogDebug($"Press G ignored: out of range. Distance={GetDistanceToSource():F3}, Radius={collectRadius:F3}");
            return false;
        }

        if (Time.time < nextBurstTime)
        {
            return false;
        }

        shapeSystem.BeginGathering();

        float absorbStrength = GetAbsorbStrength01();
        int logicalBurst = Mathf.RoundToInt(Mathf.Lerp(minParticlesPerBurst, maxParticlesPerBurst, absorbStrength));
        int spawnCount = Mathf.Min(logicalBurst, shapeSystem.RemainingCapacity);
        for (int i = 0; i < spawnCount; i++)
        {
            shapeSystem.SpawnParticle(source.position);
        }

        nextBurstTime = Time.time + burstIntervalSeconds;

        LogDebug($"Absorb started. Spawned {spawnCount} particles from source.");

        return spawnCount > 0;
    }

    private void UpdateWindVfx()
    {
        if (absorbParticleSystem == null)
        {
            return;
        }

        Transform source = GetActiveSource();
        if (source == null || leftHandTransform == null)
        {
            StopWindVfxEmission();
            return;
        }

        absorbParticleSystem.transform.position = source.position;
        absorbParticleSystem.transform.rotation = source.rotation;

        bool isActive = Keyboard.current != null && Keyboard.current.gKey.isPressed && IsWithinCollectRange;
        float strength = isActive ? GetAbsorbStrength01() : 0f;

        var emission = absorbParticleSystem.emission;
        emission.rateOverTimeMultiplier = Mathf.Lerp(0f, maxEmissionRate, strength);

        var main = absorbParticleSystem.main;
        main.startSpeed = Mathf.Lerp(minStartSpeed, maxStartSpeed, strength);
        main.startLifetime = Mathf.Lerp(minStartLifetime, maxStartLifetime, strength);
        main.startSize = Mathf.Lerp(minStartSize, maxStartSize, strength);
        Color currentColor = blossomColor;
        currentColor.a = Mathf.Lerp(minColorAlpha, maxColorAlpha, strength);
        main.startColor = currentColor;

        var velocityOverLifetime = absorbParticleSystem.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
        velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-sideDriftStrength, sideDriftStrength);
        float upwardVelocity = Mathf.Lerp(minUpwardVelocity, maxUpwardVelocity, strength);
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(upwardVelocity, upwardVelocity);
        velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(-forwardDriftStrength, forwardDriftStrength);

        var limitVelocityOverLifetime = absorbParticleSystem.limitVelocityOverLifetime;
        limitVelocityOverLifetime.enabled = true;
        limitVelocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
        limitVelocityOverLifetime.limit = Mathf.Lerp(0.12f, 0.55f, strength);

        var noise = absorbParticleSystem.noise;
        noise.enabled = true;
        noise.strength = Mathf.Lerp(minNoiseStrength, maxNoiseStrength, strength);
        noise.frequency = Mathf.Lerp(0.08f, 0.22f, strength);
        noise.scrollSpeed = Mathf.Lerp(0.05f, 0.2f, strength);

        var forceOverLifetime = absorbParticleSystem.forceOverLifetime;
        forceOverLifetime.enabled = true;
        forceOverLifetime.space = ParticleSystemSimulationSpace.Local;
        forceOverLifetime.x = new ParticleSystem.MinMaxCurve(0f);
        forceOverLifetime.y = new ParticleSystem.MinMaxCurve(Mathf.Lerp(0.01f, 0.04f, strength));
        forceOverLifetime.z = new ParticleSystem.MinMaxCurve(0f);

        var rotationOverLifetime = absorbParticleSystem.rotationOverLifetime;
        rotationOverLifetime.enabled = true;
        rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(Mathf.Lerp(0.15f, 0.8f, strength));

        var velocityBySpeed = absorbParticleSystem.velocityOverLifetime;
        velocityBySpeed.orbitalY = new ParticleSystem.MinMaxCurve(Mathf.Lerp(minOrbitalVelocity, maxOrbitalVelocity, strength));
        velocityBySpeed.radial = new ParticleSystem.MinMaxCurve(Mathf.Lerp(minRadialVelocity, maxRadialVelocity, strength));

        if (isActive)
        {
            if (!absorbParticleSystem.isPlaying)
            {
                absorbParticleSystem.Play();
            }
        }
        else if (absorbParticleSystem.isPlaying)
        {
            absorbParticleSystem.Stop(false, ParticleSystemStopBehavior.StopEmitting);
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
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.gravityModifier = 0f;
        main.startSpeed = minStartSpeed;
        main.startLifetime = minStartLifetime;
        main.startSize = minStartSize;
        main.maxParticles = Mathf.Max(main.maxParticles, 2048);

        var emission = absorbParticleSystem.emission;
        emission.enabled = true;
        emission.rateOverTimeMultiplier = 0f;

        var shape = absorbParticleSystem.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(blossomFieldWidth, blossomFieldHeight, blossomFieldDepth);

        var velocityOverLifetime = absorbParticleSystem.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
        velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-sideDriftStrength, sideDriftStrength);
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(minUpwardVelocity, minUpwardVelocity);
        velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(-forwardDriftStrength, forwardDriftStrength);
        velocityOverLifetime.orbitalY = new ParticleSystem.MinMaxCurve(minOrbitalVelocity);
        velocityOverLifetime.radial = new ParticleSystem.MinMaxCurve(minRadialVelocity);

        var forceOverLifetime = absorbParticleSystem.forceOverLifetime;
        forceOverLifetime.enabled = true;
        forceOverLifetime.space = ParticleSystemSimulationSpace.Local;
        forceOverLifetime.x = new ParticleSystem.MinMaxCurve(0f);
        forceOverLifetime.y = new ParticleSystem.MinMaxCurve(0.01f);
        forceOverLifetime.z = new ParticleSystem.MinMaxCurve(0f);

        var limitVelocityOverLifetime = absorbParticleSystem.limitVelocityOverLifetime;
        limitVelocityOverLifetime.enabled = true;
        limitVelocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
        limitVelocityOverLifetime.limit = 0.12f;

        var inheritVelocity = absorbParticleSystem.inheritVelocity;
        inheritVelocity.enabled = false;

        var noise = absorbParticleSystem.noise;
        noise.enabled = true;
        noise.strength = minNoiseStrength;
        noise.frequency = 0.08f;
        noise.scrollSpeed = 0.05f;

        var rotationOverLifetime = absorbParticleSystem.rotationOverLifetime;
        rotationOverLifetime.enabled = true;
        rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(0.15f);

        absorbParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private float GetAbsorbStrength01()
    {
        if (absorbHoldStartTime < 0f)
        {
            return 0f;
        }

        return Mathf.Clamp01((Time.time - absorbHoldStartTime) / Mathf.Max(0.01f, absorbBuildupSeconds));
    }

    private void StopWindVfxEmission()
    {
        if (absorbParticleSystem == null)
        {
            return;
        }

        var emission = absorbParticleSystem.emission;
        emission.rateOverTimeMultiplier = 0f;
    }

    private void ShowWarning(string message)
    {
        warningMessage = message;
        warningHideTime = Time.time + warningDurationSeconds;
        LogDebug($"Warning shown: {message}");
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

    private void OnDrawGizmosSelected()
    {
        if (leftHandTransform == null)
        {
            return;
        }

        Gizmos.color = IsWithinCollectRange ? Color.green : Color.red;
        Gizmos.DrawWireSphere(leftHandTransform.position, collectRadius);

        if (debugSource != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(leftHandTransform.position, debugSource.position);
        }
    }

    private void OnGUI()
    {
        if (!showCollectPrompt && string.IsNullOrEmpty(warningMessage))
        {
            return;
        }

        Camera camera = Camera.main;
        if (camera == null)
        {
            return;
        }

        Vector3 uiWorldPosition = camera.transform.position + camera.transform.forward * 0.75f + Vector3.up * 0.08f;
        Vector3 screenPosition = camera.WorldToScreenPoint(uiWorldPosition);
        if (screenPosition.z <= 0f)
        {
            return;
        }

        const float width = 320f;
        float height = showCollectPrompt ? 95f : 70f;
        Rect panel = new Rect(
            screenPosition.x - width * 0.5f,
            Screen.height - screenPosition.y - height * 0.5f,
            width,
            height);

        GUILayout.BeginArea(panel, GUI.skin.box);
        if (showCollectPrompt)
        {
            GUILayout.Label("Particle source detected.");
            float secondsLeft = Mathf.Max(0f, collectPromptHideTime - Time.time);
            GUILayout.Label($"Hold G to absorb particles. Hint hides in {secondsLeft:F1}s");

            if (!collectPromptConfirmed)
            {
                if (GUILayout.Button("Confirm"))
                {
                    collectPromptConfirmed = true;
                    showCollectPrompt = false;
                    LogDebug("Collect prompt confirmed by user.");
                }
            }
        }

        if (!string.IsNullOrEmpty(warningMessage))
        {
            GUILayout.Label(warningMessage);
        }

        GUILayout.EndArea();
    }

}
