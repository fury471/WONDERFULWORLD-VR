using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace WonderfulWorld.Features.Fireworks
{
    [DisallowMultipleComponent]
    public class VRFireworkMenuController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private FireworkLaunchPad launchPad;
        [SerializeField] private GameObject menuRoot;
        [SerializeField] private GameObject keyboardRoot;
        [SerializeField] private InputField textInput;
        [SerializeField] private Text previewText;
        [SerializeField] private Transform followCamera;

        [Header("Input")]
        [SerializeField] private InputActionReference toggleMenuAction;

        [Header("Placement")]
        [SerializeField] private bool followCameraWhenOpen = true;
        [SerializeField] private float distanceFromCamera = 1.8f;
        [SerializeField] private Vector3 cameraLocalOffset = new Vector3(0f, -0.12f, 0f);
        [SerializeField] private float followSharpness = 10f;

        [Header("Defaults")]
        [SerializeField] private string defaultText = "DREAM";
        [SerializeField] private int maxTextLength = FireworkPointCloudGenerator.MaxTextLength;
        [SerializeField] private bool hideMenuAfterLaunch = true;

        private string currentText;

        private void Awake()
        {
            maxTextLength = Mathf.Max(maxTextLength, FireworkPointCloudGenerator.MaxTextLength);

            if (launchPad == null)
            {
                launchPad = FindFirstObjectByType<FireworkLaunchPad>();
            }

            ResolveFollowCamera();

            currentText = FireworkPointCloudGenerator.SanitizeText(defaultText);
            if (textInput != null && textInput.characterLimit > 0 && textInput.characterLimit < maxTextLength)
            {
                textInput.characterLimit = maxTextLength;
            }

            ApplyTextToInput();
            SetMenuVisible(menuRoot != null && menuRoot.activeSelf);
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
            if (toggleMenuAction != null && toggleMenuAction.action.WasPressedThisFrame())
            {
                ToggleMenu();
            }

            if (followCameraWhenOpen && menuRoot != null && menuRoot.activeSelf)
            {
                UpdateMenuPose();
            }
        }

        public void ToggleMenu()
        {
            bool visible = menuRoot == null || !menuRoot.activeSelf;
            SetMenuVisible(visible);
        }

        public void ShowMenu()
        {
            SetMenuVisible(true);
        }

        public void HideMenu()
        {
            SetMenuVisible(false);
        }

        public void ShowKeyboard()
        {
            if (keyboardRoot != null)
            {
                keyboardRoot.SetActive(true);
            }
        }

        public void HideKeyboard()
        {
            if (keyboardRoot != null)
            {
                keyboardRoot.SetActive(false);
            }
        }

        public void AppendCharacter(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            char c = char.ToUpperInvariant(value[0]);
            if (c < 'A' || c > 'Z')
            {
                return;
            }

            if (currentText.Length >= maxTextLength)
            {
                return;
            }

            currentText += c;
            ApplyTextToInput();
        }

        public void AddSpace()
        {
            if (currentText.Length >= maxTextLength)
            {
                return;
            }

            currentText += " ";
            ApplyTextToInput();
        }

        public void Backspace()
        {
            if (string.IsNullOrEmpty(currentText))
            {
                return;
            }

            currentText = currentText.Substring(0, currentText.Length - 1);
            ApplyTextToInput();
        }

        public void ClearText()
        {
            currentText = string.Empty;
            ApplyTextToInput();
        }

        public void LaunchTypedText()
        {
            SyncTextFromInput();
            launchPad?.TriggerText(currentText);
            CloseAfterLaunch();
        }

        public void LaunchConfiguredShowcase()
        {
            launchPad?.TriggerShowcase();
            CloseAfterLaunch();
        }

        public void LaunchShowcaseStep(int stepIndex)
        {
            launchPad?.TriggerShowcaseStep(stepIndex);
            CloseAfterLaunch();
        }

        private void SetMenuVisible(bool visible)
        {
            if (menuRoot != null)
            {
                menuRoot.SetActive(visible);
            }

            if (keyboardRoot != null)
            {
                keyboardRoot.SetActive(false);
            }

            if (visible)
            {
                UpdateMenuPose(force: true);
            }
        }

        private void CloseAfterLaunch()
        {
            if (hideMenuAfterLaunch)
            {
                HideMenu();
            }
        }

        private void SyncTextFromInput()
        {
            if (textInput != null)
            {
                currentText = FireworkPointCloudGenerator.SanitizeText(textInput.text);
            }
            else
            {
                currentText = FireworkPointCloudGenerator.SanitizeText(currentText);
            }

            ApplyTextToInput();
        }

        private void ApplyTextToInput()
        {
            currentText = currentText.Length > maxTextLength ? currentText.Substring(0, maxTextLength) : currentText;

            if (textInput != null && textInput.text != currentText)
            {
                textInput.text = currentText;
            }

            if (previewText != null)
            {
                previewText.text = string.IsNullOrWhiteSpace(currentText) ? defaultText : currentText;
            }
        }

        private void UpdateMenuPose(bool force = false)
        {
            if (followCamera == null)
            {
                ResolveFollowCamera();
            }

            if (followCamera == null || menuRoot == null)
            {
                return;
            }

            Vector3 targetPosition = followCamera.position
                + followCamera.forward * distanceFromCamera
                + followCamera.TransformVector(cameraLocalOffset);
            Quaternion targetRotation = Quaternion.LookRotation(targetPosition - followCamera.position, Vector3.up);

            Transform menuTransform = menuRoot.transform;
            if (force)
            {
                menuTransform.position = targetPosition;
                menuTransform.rotation = targetRotation;
                return;
            }

            float t = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
            menuTransform.position = Vector3.Lerp(menuTransform.position, targetPosition, t);
            menuTransform.rotation = Quaternion.Slerp(menuTransform.rotation, targetRotation, t);
        }

        private void ResolveFollowCamera()
        {
            if (followCamera == null)
            {
                followCamera = QuestInteractionUtils.FindHeadTransform();
            }
        }
    }
}
