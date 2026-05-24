using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

#pragma warning disable 0649

namespace Wonderland.UI
{
    [DisallowMultipleComponent]
    public sealed class WelcomeFlowController : MonoBehaviour
    {
        [System.Serializable]
        public struct LocalizedLabel
        {
            public TMP_Text target;
            [TextArea(1, 3)] public string english;
            [TextArea(1, 3)] public string chineseSimplified;
            [TextArea(1, 3)] public string swedish;
        }

        [Header("Panel")]
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Buttons")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button englishButton;
        [SerializeField] private Button chineseButton;
        [SerializeField] private Button swedishButton;

        [Header("Localized Labels (auto-attaches LocalizedUIText)")]
        [SerializeField] private LocalizedLabel[] labels;

        [Header("Wiring")]
        [SerializeField] private QuestLocomotionComfortProfile locomotionProfile;
        [SerializeField] private ScaleManager scaleManager;
        [SerializeField] private GameFlowManager gameFlow;

        [Header("Placement (matches LocalizedNoticeBoardPanel defaults)")]
        [SerializeField] private Transform followCamera;
        [SerializeField] private float distanceFromCamera = 1.6f;
        [SerializeField] private Vector3 cameraLocalOffset = new Vector3(0f, -0.06f, 0f);
        [SerializeField, Min(0f)] private float followSharpness = 18f;
        [SerializeField] private Vector3 panelWorldScale = new Vector3(0.00135f, 0.00135f, 0.00135f);

        [Header("Animation")]
        [SerializeField, Min(0f)] private float fadeSharpness = 20f;
        [SerializeField, Min(0f)] private float postClickDestroyDelay = 0.6f;

        [Header("Behavior")]
        [SerializeField] private bool lockLocomotionWhileShown = true;
        [SerializeField] private bool disableThumbstickScaleWhileShown = true;
        [SerializeField] private bool destroyAfterStart = true;

        [Header("Language Button State")]
        [SerializeField] private Color selectedColor = new Color(0.18f, 0.58f, 0.72f, 1f);
        [SerializeField] private Color normalColor = new Color(0.90f, 0.95f, 0.94f, 1f);
        [SerializeField] private Color selectedTextColor = Color.white;
        [SerializeField] private Color normalTextColor = new Color(0.08f, 0.12f, 0.14f, 1f);

        private bool dismissed;
        private bool targetVisible;
        private bool locksApplied;

        private void Reset()
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        private void Awake()
        {
            ResolveReferences();
            EnsureLanguageService();
            EnsureXrCanvasInteractivity();
            AttachLocalizedTexts();
            ApplyLocalizedFontToChildren();

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }
        }

        private void OnEnable()
        {
            UILanguageService.LanguageChanged += RefreshLanguageButtons;
            RefreshLanguageButtons(UILanguageService.GetCurrentOrDefault());
        }

        private void Start()
        {
            WireButtons();
            ShowAndLock();
        }

        private void OnDisable()
        {
            UILanguageService.LanguageChanged -= RefreshLanguageButtons;
        }

        private void OnDestroy()
        {
            ReleaseExperienceLocks();
        }

        private void LateUpdate()
        {
            UpdatePose(force: false);
            FadeTowards(targetVisible ? 1f : 0f);
        }

        public void OnStartClicked()
        {
            if (dismissed)
            {
                return;
            }

            dismissed = true;
            targetVisible = false;

            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            ReleaseExperienceLocks();

            if (gameFlow != null)
            {
                gameFlow.CompleteOnboarding();
            }

            if (destroyAfterStart)
            {
                StartCoroutine(DestroyAfterFade());
            }
        }

        private void ResolveReferences()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (followCamera == null)
            {
                followCamera = QuestInteractionUtils.FindHeadTransform();
            }

            if (locomotionProfile == null)
            {
#if UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
                locomotionProfile = FindFirstObjectByType<QuestLocomotionComfortProfile>(FindObjectsInactive.Include);
#else
#pragma warning disable CS0618
                locomotionProfile = FindObjectOfType<QuestLocomotionComfortProfile>(true);
#pragma warning restore CS0618
#endif
            }

            if (scaleManager == null)
            {
#if UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
                scaleManager = FindFirstObjectByType<ScaleManager>(FindObjectsInactive.Include);
#else
#pragma warning disable CS0618
                scaleManager = FindObjectOfType<ScaleManager>(true);
#pragma warning restore CS0618
#endif
            }

            if (gameFlow == null)
            {
#if UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
                gameFlow = FindFirstObjectByType<GameFlowManager>(FindObjectsInactive.Include);
#else
#pragma warning disable CS0618
                gameFlow = FindObjectOfType<GameFlowManager>(true);
#pragma warning restore CS0618
#endif
            }
        }

        private void EnsureXrCanvasInteractivity()
        {
            Canvas[] canvases = GetComponentsInChildren<Canvas>(true);
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas canvas = canvases[i];
                if (canvas == null)
                {
                    continue;
                }

                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = null;

                if (canvas.GetComponent<GraphicRaycaster>() == null)
                {
                    canvas.gameObject.AddComponent<GraphicRaycaster>();
                }

                if (canvas.GetComponent<TrackedDeviceGraphicRaycaster>() == null)
                {
                    canvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
                }
            }
        }

        private void AttachLocalizedTexts()
        {
            if (labels == null)
            {
                return;
            }

            for (int i = 0; i < labels.Length; i++)
            {
                LocalizedLabel entry = labels[i];
                if (entry.target == null)
                {
                    continue;
                }

                LocalizedUIText localized = entry.target.GetComponent<LocalizedUIText>();
                if (localized == null)
                {
                    localized = entry.target.gameObject.AddComponent<LocalizedUIText>();
                }

                localized.SetTexts(entry.english, entry.chineseSimplified, entry.swedish);
            }
        }

        private void ApplyLocalizedFontToChildren()
        {
            TMP_FontAsset localizedFont = LocalizedUIFontProvider.GetBestLocalizedFont();
            TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];
                if (text == null)
                {
                    continue;
                }

                if (localizedFont != null)
                {
                    text.font = localizedFont;
                }

                text.raycastTarget = false;
            }
        }

        private void WireButtons()
        {
            WireButton(startButton, OnStartClicked);
            WireButton(englishButton, SetEnglish);
            WireButton(chineseButton, SetChineseSimplified);
            WireButton(swedishButton, SetSwedish);
        }

        private static void WireButton(Button button, UnityAction action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        private static void SetEnglish() => SetLanguage(UILanguage.English);
        private static void SetChineseSimplified() => SetLanguage(UILanguage.ChineseSimplified);
        private static void SetSwedish() => SetLanguage(UILanguage.Swedish);

        private static void SetLanguage(UILanguage language)
        {
            UILanguageService service = EnsureLanguageService();
            if (service != null)
            {
                service.SetLanguage(language);
            }
        }

        private static UILanguageService EnsureLanguageService()
        {
            if (UILanguageService.Instance != null)
            {
                return UILanguageService.Instance;
            }

#if UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
            UILanguageService service = FindFirstObjectByType<UILanguageService>(FindObjectsInactive.Include);
#else
#pragma warning disable CS0618
            UILanguageService service = FindObjectOfType<UILanguageService>(true);
#pragma warning restore CS0618
#endif
            if (service != null)
            {
                return service;
            }

            GameObject systemRoot = GameObject.Find("WW_UI_System");
            if (systemRoot == null)
            {
                systemRoot = new GameObject("WW_UI_System");
            }

            return systemRoot.AddComponent<UILanguageService>();
        }

        private void RefreshLanguageButtons(UILanguage language)
        {
            ApplyButtonState(englishButton, language == UILanguage.English);
            ApplyButtonState(chineseButton, language == UILanguage.ChineseSimplified);
            ApplyButtonState(swedishButton, language == UILanguage.Swedish);
        }

        private void ApplyButtonState(Button button, bool selected)
        {
            if (button == null)
            {
                return;
            }

            if (button.targetGraphic != null)
            {
                button.targetGraphic.color = selected ? selectedColor : normalColor;
            }

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.color = selected ? selectedTextColor : normalTextColor;
            }
        }

        private void ShowAndLock()
        {
            targetVisible = true;

            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = true;
            }

            if (lockLocomotionWhileShown && locomotionProfile != null)
            {
                locomotionProfile.SetRuntimeLocomotionLocked(true);
                locksApplied = true;
            }

            if (disableThumbstickScaleWhileShown && scaleManager != null)
            {
                scaleManager.SetThumbstickScaleEnabled(false);
                locksApplied = true;
            }

            UpdatePose(force: true);
        }

        private void ReleaseExperienceLocks()
        {
            if (!locksApplied)
            {
                return;
            }

            if (lockLocomotionWhileShown && locomotionProfile != null)
            {
                locomotionProfile.SetRuntimeLocomotionLocked(false);
            }

            if (disableThumbstickScaleWhileShown && scaleManager != null)
            {
                scaleManager.SetThumbstickScaleEnabled(true);
            }

            locksApplied = false;
        }

        private IEnumerator DestroyAfterFade()
        {
            float deadline = Time.unscaledTime + postClickDestroyDelay;
            while (Time.unscaledTime < deadline && canvasGroup != null && canvasGroup.alpha > 0.02f)
            {
                yield return null;
            }

            Destroy(gameObject);
        }

        private void FadeTowards(float target)
        {
            if (canvasGroup == null)
            {
                return;
            }

            float t = 1f - Mathf.Exp(-fadeSharpness * Time.unscaledDeltaTime);
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, target, t);
        }

        private void UpdatePose(bool force)
        {
            if (followCamera == null)
            {
                followCamera = QuestInteractionUtils.FindHeadTransform();
                if (followCamera == null)
                {
                    return;
                }
            }

            transform.localScale = panelWorldScale;

            Vector3 targetPosition = followCamera.position
                + followCamera.forward * distanceFromCamera
                + followCamera.TransformVector(cameraLocalOffset);

            Vector3 lookDirection = targetPosition - followCamera.position;
            if (lookDirection.sqrMagnitude < 0.0001f)
            {
                lookDirection = followCamera.forward;
            }

            Quaternion targetRotation = Quaternion.LookRotation(lookDirection, Vector3.up);

            if (force)
            {
                transform.SetPositionAndRotation(targetPosition, targetRotation);
                return;
            }

            float t = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, targetPosition, t);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, t);
        }
    }
}

#pragma warning restore 0649
