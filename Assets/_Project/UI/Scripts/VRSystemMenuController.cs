using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR;
using XRCommonUsages = UnityEngine.XR.CommonUsages;
using XRInputDevice = UnityEngine.XR.InputDevice;

#pragma warning disable 0649

namespace Wonderland.UI
{
    [DisallowMultipleComponent]
    public sealed class VRSystemMenuController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject settingsPanel;

        [Header("Buttons")]
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private Button backButton;

        [Header("Input")]
        [SerializeField] private InputActionReference toggleMenuAction;
        [SerializeField] private bool useLeftHandMenuFallback = true;
        [SerializeField] private bool enableKeyboardFallback = true;
        [SerializeField] private Key keyboardFallbackKey = Key.Escape;

        [Header("Placement")]
        [SerializeField] private Transform followCamera;
        [SerializeField] private float distanceFromCamera = 1.35f;
        [SerializeField] private Vector3 cameraLocalOffset = new Vector3(-0.35f, -0.18f, 0f);
        [SerializeField] private float followSharpness = 18f;
        [SerializeField] private Vector3 worldScale = new Vector3(0.0014f, 0.0014f, 0.0014f);

        [Header("Events")]
        public UnityEvent opened;
        public UnityEvent closed;

        private bool visible;
        private bool lastMenuButtonState;

        public bool IsVisible => visible;

        private void Reset()
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        private void Awake()
        {
            ResolveReferences();
            ApplyLocalizedFontToChildren();
            WireButtons();
            SetVisible(false, true);
            ShowMainPanel();
        }

        private void OnEnable()
        {
            toggleMenuAction?.action?.Enable();
        }

        private void OnDisable()
        {
            toggleMenuAction?.action?.Disable();
        }

        private void Update()
        {
            if (WasTogglePressedThisFrame())
            {
                ToggleMenu();
            }
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

        public void ToggleMenu()
        {
            if (visible)
            {
                CloseMenu();
            }
            else
            {
                OpenMenu();
            }
        }

        public void OpenMenu()
        {
            ResolveReferences();
            ShowMainPanel();
            SetVisible(true, true);
            UpdatePose(true);
            opened?.Invoke();
        }

        public void CloseMenu()
        {
            if (!visible)
            {
                return;
            }

            SetVisible(false, false);
            closed?.Invoke();
        }

        public void ShowSettingsPanel()
        {
            if (mainPanel != null) mainPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(true);
        }

        public void ShowMainPanel()
        {
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (mainPanel != null) mainPanel.SetActive(true);
        }

        public void ExitExperience()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void ResolveReferences()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
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

        private void WireButtons()
        {
            if (settingsButton != null) settingsButton.onClick.AddListener(ShowSettingsPanel);
            if (cancelButton != null) cancelButton.onClick.AddListener(CloseMenu);
            if (exitButton != null) exitButton.onClick.AddListener(ExitExperience);
            if (backButton != null) backButton.onClick.AddListener(ShowMainPanel);
        }

        private void SetVisible(bool shouldShow, bool immediate)
        {
            visible = shouldShow;
            gameObject.SetActive(true);

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

            float t = 1f - Mathf.Exp(-20f * Time.unscaledDeltaTime);
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, t);
        }

        private void UpdatePose(bool force = false)
        {
            transform.localScale = worldScale;

            if (followCamera == null)
            {
                return;
            }

            Vector3 targetPosition = followCamera.position
                + followCamera.forward * distanceFromCamera
                + followCamera.TransformVector(cameraLocalOffset);
            Quaternion targetRotation = Quaternion.LookRotation(targetPosition - followCamera.position, Vector3.up);

            if (force)
            {
                transform.SetPositionAndRotation(targetPosition, targetRotation);
                return;
            }

            float t = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, targetPosition, t);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, t);
        }

        private bool WasTogglePressedThisFrame()
        {
            if (toggleMenuAction != null && toggleMenuAction.action != null && toggleMenuAction.action.WasPressedThisFrame())
            {
                return true;
            }

            if (enableKeyboardFallback && Keyboard.current != null && Keyboard.current[keyboardFallbackKey].wasPressedThisFrame)
            {
                return true;
            }

            if (!useLeftHandMenuFallback)
            {
                return false;
            }

            XRInputDevice leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            bool pressed = false;
            if (leftHand.isValid)
            {
                leftHand.TryGetFeatureValue(XRCommonUsages.menuButton, out pressed);
            }

            bool pressedThisFrame = pressed && !lastMenuButtonState;
            lastMenuButtonState = pressed;
            return pressedThisFrame;
        }
    }
}

#pragma warning restore 0649
