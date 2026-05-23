using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using XRCommonUsages = UnityEngine.XR.CommonUsages;
using XRInputDevice = UnityEngine.XR.InputDevice;

#pragma warning disable 0649

namespace Wonderland.UI
{
    [DisallowMultipleComponent]
    public sealed class NoticeBoardHotspot : MonoBehaviour
    {
        [Header("Content")]
        [SerializeField] private LocalizedNoticeBoardContent content;
        [SerializeField] private LocalizedNoticeBoardPanel panel;
        [SerializeField] private Transform panelAnchor;
        [SerializeField] private bool useBoardAnchorForPopup;
        [SerializeField] private Vector3 panelWorldScale = new Vector3(0.00125f, 0.00125f, 0.00125f);

        [Header("Input")]
        [SerializeField] private XRSimpleInteractable interactable;
        [SerializeField] private bool openOnSelect = true;
        [SerializeField] private bool openOnActivate = true;
        [SerializeField] private bool openWithRightIndexTrigger = true;
        [SerializeField] private Transform rightRayOrigin;
        [SerializeField] private float rightRayDistance = 8f;
        [SerializeField] private LayerMask rightRayMask = ~0;
        [SerializeField] private bool enableMouseFallback = true;

        [Header("Events")]
        public UnityEvent<LocalizedNoticeBoardContent> opened;

        private bool lastRightTriggerState;

        private void Reset()
        {
            interactable = GetComponent<XRSimpleInteractable>();
        }

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();

            if (interactable != null)
            {
                interactable.selectEntered.AddListener(HandleSelectEntered);
                interactable.activated.AddListener(HandleActivated);
            }
        }

        private void OnDisable()
        {
            if (interactable != null)
            {
                interactable.selectEntered.RemoveListener(HandleSelectEntered);
                interactable.activated.RemoveListener(HandleActivated);
            }
        }

        private void Update()
        {
            if (openWithRightIndexTrigger && WasRightIndexTriggerPressedThisFrame() && IsRightRayPointingAtBoard())
            {
                Open();
            }
        }

        private void OnMouseDown()
        {
            if (enableMouseFallback)
            {
                Open();
            }
        }

        public void SetContent(LocalizedNoticeBoardContent noticeContent)
        {
            content = noticeContent;
        }

        public void SetPanel(LocalizedNoticeBoardPanel noticePanel)
        {
            panel = noticePanel;
        }

        public void Open()
        {
            ResolveReferences();

            if (content == null)
            {
                Debug.LogWarning("[NoticeBoardHotspot] Cannot open: content is missing.", this);
                return;
            }

            if (panel == null)
            {
                Debug.LogWarning("[NoticeBoardHotspot] Cannot open: panel is missing.", this);
                return;
            }

            Transform popupAnchor = useBoardAnchorForPopup ? panelAnchor : null;
            panel.Show(content, popupAnchor, panelWorldScale);
            opened?.Invoke(content);
        }

        public void Close()
        {
            panel?.Hide();
        }

        private void ResolveReferences()
        {
            if (interactable == null)
            {
                interactable = GetComponent<XRSimpleInteractable>();
            }

            if (panel == null)
            {
                panel = FindFirstObjectByType<LocalizedNoticeBoardPanel>(FindObjectsInactive.Include);
            }

            if (rightRayOrigin == null)
            {
                rightRayOrigin = FindRightRayOrigin();
            }
        }

        private void HandleSelectEntered(SelectEnterEventArgs args)
        {
            if (openOnSelect)
            {
                Open();
            }
        }

        private void HandleActivated(ActivateEventArgs args)
        {
            if (openOnActivate)
            {
                Open();
            }
        }

        private bool WasRightIndexTriggerPressedThisFrame()
        {
            XRInputDevice rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            bool pressed = false;
            if (rightHand.isValid)
            {
                rightHand.TryGetFeatureValue(XRCommonUsages.triggerButton, out pressed);
            }

            bool pressedThisFrame = pressed && !lastRightTriggerState;
            lastRightTriggerState = pressed;
            return pressedThisFrame;
        }

        private bool IsRightRayPointingAtBoard()
        {
            ResolveReferences();

            Transform origin = rightRayOrigin;
            if (origin == null && Camera.main != null)
            {
                origin = Camera.main.transform;
            }

            if (origin == null)
            {
                return false;
            }

            if (!Physics.Raycast(origin.position, origin.forward, out RaycastHit hit, rightRayDistance, rightRayMask, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            Transform hitTransform = hit.collider.transform;
            return hitTransform == transform || hitTransform.IsChildOf(transform) || transform.IsChildOf(hitTransform);
        }

        private static Transform FindRightRayOrigin()
        {
            string[] candidateNames =
            {
                "Right Controller Stabilized Attach",
                "Right Controller Teleport Stabilized Origin",
                "Right Controller",
                "RightHand Controller",
                "Right Hand"
            };

            for (int i = 0; i < candidateNames.Length; i++)
            {
                GameObject candidate = GameObject.Find(candidateNames[i]);
                if (candidate != null)
                {
                    return candidate.transform;
                }
            }

            return null;
        }
    }
}

#pragma warning restore 0649
