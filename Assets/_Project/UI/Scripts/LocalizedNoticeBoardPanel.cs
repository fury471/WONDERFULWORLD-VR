using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

#pragma warning disable 0649

namespace Wonderland.UI
{
    [DisallowMultipleComponent]
    public sealed class LocalizedNoticeBoardPanel : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform panelRect;
        [SerializeField] private Image contentImage;
        [SerializeField] private Button backdropButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Transform followCamera;

        [Header("Placement")]
        [SerializeField] private bool followCameraWhenNoAnchor = true;
        [SerializeField] private float distanceFromCamera = 1.65f;
        [SerializeField] private Vector3 cameraLocalOffset = new Vector3(0f, -0.03f, 0f);
        [SerializeField] private float followSharpness = 18f;
        [SerializeField] private Vector3 defaultWorldScale = new Vector3(0.00125f, 0.00125f, 0.00125f);

        [Header("Animation")]
        [SerializeField] private float fadeSharpness = 20f;
        [SerializeField] private bool hideOnAwake = true;

        [Header("Events")]
        public UnityEvent<LocalizedNoticeBoardContent> shown;
        public UnityEvent hidden;

        private LocalizedNoticeBoardContent activeContent;
        private Transform activeAnchor;
        private bool visible;
        private Vector3 targetScale;

        public bool IsVisible => visible;

        private void Reset()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            panelRect = transform as RectTransform;
            contentImage = GetComponentInChildren<Image>(true);
        }

        private void Awake()
        {
            ResolveReferences();
            ApplyLocalizedFontToChildren();
            targetScale = defaultWorldScale;

            if (backdropButton != null)
            {
                backdropButton.onClick.AddListener(Hide);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Hide);
            }

            if (hideOnAwake)
            {
                SetVisible(false, true);
            }
        }

        private void OnEnable()
        {
            UILanguageService.LanguageChanged += HandleLanguageChanged;
            ApplyLanguage(UILanguageService.GetCurrentOrDefault());
        }

        private void OnDisable()
        {
            UILanguageService.LanguageChanged -= HandleLanguageChanged;
        }

        private void LateUpdate()
        {
            if (!visible)
            {
                FadeTowards(0f);
                return;
            }

            UpdatePose();
            FadeTowards(1f);
        }

        public void Show(LocalizedNoticeBoardContent content)
        {
            Show(content, null, defaultWorldScale);
        }

        public void Show(LocalizedNoticeBoardContent content, Transform anchor, Vector3 worldScale)
        {
            if (content == null)
            {
                Debug.LogWarning("[NoticeBoardPanel] Cannot show: content is missing.", this);
                return;
            }

            ResolveReferences();
            activeContent = content;
            activeAnchor = anchor;
            targetScale = worldScale == Vector3.zero ? defaultWorldScale : worldScale;
            ApplyLanguage(UILanguageService.GetCurrentOrDefault());
            SetVisible(true, true);
            UpdatePose(true);
            shown?.Invoke(activeContent);
        }

        public void Hide()
        {
            if (!visible)
            {
                return;
            }

            SetVisible(false, false);
            hidden?.Invoke();
        }

        public void Toggle(LocalizedNoticeBoardContent content, Transform anchor, Vector3 worldScale)
        {
            if (visible && activeContent == content)
            {
                Hide();
            }
            else
            {
                Show(content, anchor, worldScale);
            }
        }

        private void ResolveReferences()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (panelRect == null)
            {
                panelRect = transform as RectTransform;
            }

            if (contentImage == null)
            {
                Image[] images = GetComponentsInChildren<Image>(true);
                for (int i = 0; i < images.Length; i++)
                {
                    if (images[i].name.IndexOf("Poster", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                        images[i].name.IndexOf("Content", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        contentImage = images[i];
                        break;
                    }
                }
            }

            if (followCamera == null && Camera.main != null)
            {
                followCamera = Camera.main.transform;
            }
        }

        private void ApplyLocalizedFontToChildren()
        {
            TMP_FontAsset localizedFont = LocalizedUIFontProvider.GetBestLocalizedFont();
            if (localizedFont == null)
            {
                return;
            }

            TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                texts[i].font = localizedFont;
            }
        }

        private void SetVisible(bool shouldShow, bool immediate)
        {
            visible = shouldShow;

            if (gameObject.activeSelf != shouldShow)
            {
                gameObject.SetActive(true);
            }

            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.blocksRaycasts = shouldShow;
            canvasGroup.interactable = shouldShow;

            if (immediate)
            {
                canvasGroup.alpha = shouldShow ? 1f : 0f;
            }
        }

        private void FadeTowards(float targetAlpha)
        {
            if (canvasGroup == null)
            {
                return;
            }

            float t = 1f - Mathf.Exp(-fadeSharpness * Time.unscaledDeltaTime);
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, t);
        }

        private void UpdatePose(bool force = false)
        {
            Transform panelTransform = transform;
            panelTransform.localScale = targetScale;

            if (activeAnchor != null)
            {
                panelTransform.SetPositionAndRotation(activeAnchor.position, activeAnchor.rotation);
                return;
            }

            if (!followCameraWhenNoAnchor || followCamera == null)
            {
                return;
            }

            Vector3 targetPosition = followCamera.position
                + followCamera.forward * distanceFromCamera
                + followCamera.TransformVector(cameraLocalOffset);
            Quaternion targetRotation = Quaternion.LookRotation(targetPosition - followCamera.position, Vector3.up);

            if (force)
            {
                panelTransform.SetPositionAndRotation(targetPosition, targetRotation);
                return;
            }

            float t = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
            panelTransform.position = Vector3.Lerp(panelTransform.position, targetPosition, t);
            panelTransform.rotation = Quaternion.Slerp(panelTransform.rotation, targetRotation, t);
        }

        private void HandleLanguageChanged(UILanguage language)
        {
            ApplyLanguage(language);
        }

        private void ApplyLanguage(UILanguage language)
        {
            if (activeContent == null || contentImage == null)
            {
                return;
            }

            Sprite sprite = activeContent.GetSprite(language);
            contentImage.sprite = sprite;
            contentImage.enabled = sprite != null;
            contentImage.preserveAspect = true;
        }
    }
}

#pragma warning restore 0649
