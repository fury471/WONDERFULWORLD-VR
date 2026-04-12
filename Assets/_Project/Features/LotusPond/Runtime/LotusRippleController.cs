using System.Collections;
using UnityEngine;

public class LotusRippleController : MonoBehaviour
{
    public event System.Action RippleStarted;
    public event System.Action RippleCompleted;

    [Header("Settings")]
    [SerializeField] private LotusScaleSettingsSO settings;

    [Header("Visual")]
    [SerializeField] private Transform rippleVisual;
    [SerializeField] private ParticleSystem rippleParticles;

    [Header("Animation")]
    [SerializeField] private Vector3 startScale = new Vector3(0.2f, 0.02f, 0.2f);
    [SerializeField] private Vector3 endScale = new Vector3(1.8f, 0.02f, 1.8f);
    [SerializeField] private float duration = 0.45f;
    [SerializeField] private bool hideWhenIdle = true;

    private Coroutine rippleRoutine;

    private void Awake()
    {
        ApplySettings();

        if (rippleVisual != null)
        {
            rippleVisual.localScale = startScale;
            if (hideWhenIdle)
            {
                rippleVisual.gameObject.SetActive(false);
            }
        }
    }

    private void OnValidate()
    {
        ApplySettings();
    }

    public void PlayRipple()
    {
        if (rippleRoutine != null)
        {
            StopCoroutine(rippleRoutine);
        }

        rippleRoutine = StartCoroutine(PlayRippleRoutine());
    }

    private IEnumerator PlayRippleRoutine()
    {
        RippleStarted?.Invoke();

        if (rippleParticles != null)
        {
            rippleParticles.Play();
        }

        if (rippleVisual == null)
        {
            RippleCompleted?.Invoke();
            yield break;
        }

        rippleVisual.gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, duration));
            rippleVisual.localScale = Vector3.Lerp(startScale, endScale, t);
            yield return null;
        }

        rippleVisual.localScale = startScale;

        if (hideWhenIdle)
        {
            rippleVisual.gameObject.SetActive(false);
        }

        RippleCompleted?.Invoke();
    }

    public void SetSettings(LotusScaleSettingsSO scaleSettings)
    {
        settings = scaleSettings;
        ApplySettings();
    }

    private void ApplySettings()
    {
        if (settings == null)
        {
            return;
        }

        startScale = settings.rippleStartScale;
        endScale = settings.rippleEndScale;
        duration = settings.rippleDuration;
        hideWhenIdle = settings.hideRippleWhenIdle;
    }
}
