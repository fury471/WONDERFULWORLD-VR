using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScaleTransitionController : MonoBehaviour
{
    [Header("Blink Fade")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private Color fadeColor = Color.black;
    [SerializeField] private float fadeOutDuration = 0.08f;
    [SerializeField] private float blackHoldDuration = 0.05f;
    [SerializeField] private float fadeInDuration = 0.12f;

    private Canvas fadeCanvas;
    private bool isInitialized;

    public IEnumerator PlayBlink(float duration, Camera targetCamera = null)
    {
        EnsureFadeVisual(targetCamera);

        if (fadeImage == null)
        {
            yield return new WaitForSeconds(duration);
            yield break;
        }

        float outDuration = fadeOutDuration;
        float holdDuration = blackHoldDuration;
        float inDuration = fadeInDuration;

        if (duration > 0f)
        {
            float configuredTotal = fadeOutDuration + blackHoldDuration + fadeInDuration;
            if (configuredTotal > 0f)
            {
                float scale = duration / configuredTotal;
                outDuration = Mathf.Max(0.01f, fadeOutDuration * scale);
                holdDuration = Mathf.Max(0f, blackHoldDuration * scale);
                inDuration = Mathf.Max(0.01f, fadeInDuration * scale);
            }
        }

        yield return FadeTo(1f, outDuration);

        if (holdDuration > 0f)
            yield return new WaitForSeconds(holdDuration);

        yield return FadeTo(0f, inDuration);
    }

    public void ConfigureTimings(float fadeOut, float blackHold, float fadeIn)
    {
        fadeOutDuration = Mathf.Max(0.01f, fadeOut);
        blackHoldDuration = Mathf.Max(0f, blackHold);
        fadeInDuration = Mathf.Max(0.01f, fadeIn);
    }

    private void EnsureFadeVisual(Camera targetCamera)
    {
        if (isInitialized && fadeImage != null)
        {
            if (targetCamera != null)
                UpdateFadeCanvasForCamera(targetCamera);
            return;
        }

        GameObject canvasObject = new GameObject("ScaleShiftFadeCanvas");
        canvasObject.transform.SetParent(targetCamera != null ? targetCamera.transform : transform, false);

        fadeCanvas = canvasObject.AddComponent<Canvas>();
        fadeCanvas.renderMode = targetCamera != null ? RenderMode.WorldSpace : RenderMode.ScreenSpaceOverlay;
        fadeCanvas.worldCamera = targetCamera;
        fadeCanvas.sortingOrder = short.MaxValue;

        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject imageObject = new GameObject("FadeImage");
        imageObject.transform.SetParent(canvasObject.transform, false);

        RectTransform rect = imageObject.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        fadeImage = imageObject.AddComponent<Image>();
        fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
        fadeImage.raycastTarget = false;

        if (targetCamera != null)
            UpdateFadeCanvasForCamera(targetCamera);

        isInitialized = true;
    }

    private void UpdateFadeCanvasForCamera(Camera targetCamera)
    {
        if (fadeCanvas == null || targetCamera == null)
            return;

        float distance = Mathf.Max(targetCamera.nearClipPlane + 0.02f, 0.08f);
        float height = 2f * distance * Mathf.Tan(targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float width = height * targetCamera.aspect;

        RectTransform canvasRect = fadeCanvas.transform as RectTransform;
        if (canvasRect != null)
        {
            canvasRect.localPosition = new Vector3(0f, 0f, distance);
            canvasRect.localRotation = Quaternion.identity;
            canvasRect.sizeDelta = new Vector2(width, height);
        }
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        Color color = fadeImage.color;
        float startAlpha = color.a;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            color.a = Mathf.Lerp(startAlpha, targetAlpha, t);
            fadeImage.color = color;
            yield return null;
        }

        color.a = targetAlpha;
        fadeImage.color = color;
    }
}
