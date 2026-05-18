using UnityEngine;
using UnityEngine.XR;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using XRInputDevice = UnityEngine.XR.InputDevice;
using XRCommonUsages = UnityEngine.XR.CommonUsages;

public class LotusEitherHandDriver : MonoBehaviour
{
    [Header("Ray Origins")]
    [SerializeField] private Transform leftRayOrigin;
    [SerializeField] private Transform rightRayOrigin;

    [Header("Raycast Settings")]
    [SerializeField] private float rayDistance = 20f;
    [SerializeField] private LayerMask rayMask = Physics.DefaultRaycastLayers;
    [SerializeField] private bool showDebugRays;

    [Header("Quest Ray Feedback")]
    [SerializeField] private bool showQuestRays = true;
    [SerializeField] private float rayWidth = 0.01f;
    [SerializeField] private Color idleRayColor = new Color(0.42f, 0.92f, 1f, 0.2f);
    [SerializeField] private Color hoverRayColor = new Color(0.45f, 1f, 0.95f, 0.82f);
    [SerializeField] private Color lotusHoverOutlineColor = new Color(0.38f, 0.95f, 1f, 0.62f);

    [Header("Input Logic")]
    [SerializeField] private bool useTriggerButton = true;
    [SerializeField] private bool enableMouseDebug = true;

    [Header("Debug")]
    [SerializeField] private bool logDebugMessages;

    private readonly RaycastHit[] hitBuffer = new RaycastHit[24];
    private XRInputDevice leftDevice;
    private XRInputDevice rightDevice;
    private bool leftPressedLastFrame;
    private bool rightPressedLastFrame;
    private HapticImpulsePlayer leftHaptics;
    private HapticImpulsePlayer rightHaptics;
    private LotusNoteTrigger leftHoveredLotus;
    private LotusNoteTrigger rightHoveredLotus;
    private QuestInteractableFeedback leftHoverFeedback;
    private QuestInteractableFeedback rightHoverFeedback;
    private Vector3 leftHoverPoint;
    private Vector3 rightHoverPoint;
    private bool leftHasHoverPoint;
    private bool rightHasHoverPoint;
    private LineRenderer leftQuestRay;
    private LineRenderer rightQuestRay;
    private Material runtimeRayMaterial;

    private void Awake()
    {
        AutoAssignRayOrigins();
    }

    private void OnDestroy()
    {
        if (runtimeRayMaterial != null)
        {
            Destroy(runtimeRayMaterial);
        }
    }

    private void Update()
    {
        AutoAssignRayOrigins();
        EnsureDevices();
        EnsureHaptics();

        UpdateHandHover(true);
        UpdateHandHover(false);

        if (showDebugRays)
        {
            DrawVisualRays();
        }

        bool leftTrigger = IsPressed(leftDevice);
        bool rightTrigger = IsPressed(rightDevice);
        bool mouseLeft = enableMouseDebug && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        bool mouseRight = enableMouseDebug && Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;

        if ((leftTrigger && !leftPressedLastFrame) || mouseLeft)
        {
            if (mouseLeft)
            {
                TryTriggerMouse();
            }
            else
            {
                TriggerHoveredLotus(true, "LeftHand");
            }
        }

        if ((rightTrigger && !rightPressedLastFrame) || mouseRight)
        {
            if (mouseRight)
            {
                TryTriggerMouse();
            }
            else
            {
                TriggerHoveredLotus(false, "RightHand");
            }
        }

        leftPressedLastFrame = leftTrigger;
        rightPressedLastFrame = rightTrigger;
    }

    private void UpdateHandHover(bool leftHand)
    {
        Transform origin = leftHand ? leftRayOrigin : rightRayOrigin;
        HapticImpulsePlayer haptics = leftHand ? leftHaptics : rightHaptics;
        if (origin == null)
        {
            SetHandHover(leftHand, null, Vector3.zero, false, haptics);
            UpdateQuestRay(leftHand, false, Vector3.zero);
            return;
        }

        Ray ray = new Ray(origin.position, origin.forward);
        bool hitLotus = TryResolveLotus(ray, out LotusNoteTrigger lotus, out Vector3 point);
        SetHandHover(leftHand, lotus, point, hitLotus, haptics);
        UpdateQuestRay(leftHand, hitLotus, hitLotus ? point : ray.origin + ray.direction.normalized * Mathf.Min(rayDistance, 8f));
    }

    private void SetHandHover(bool leftHand, LotusNoteTrigger lotus, Vector3 point, bool hasPoint, HapticImpulsePlayer haptics)
    {
        LotusNoteTrigger otherLotus = leftHand ? rightHoveredLotus : leftHoveredLotus;
        LotusNoteTrigger previous = leftHand ? leftHoveredLotus : rightHoveredLotus;
        QuestInteractableFeedback previousFeedback = leftHand ? leftHoverFeedback : rightHoverFeedback;

        if (previous == lotus)
        {
            if (leftHand)
            {
                leftHoverPoint = point;
                leftHasHoverPoint = hasPoint;
            }
            else
            {
                rightHoverPoint = point;
                rightHasHoverPoint = hasPoint;
            }

            previousFeedback?.SetHovered(lotus != null, haptics);
            return;
        }

        if (previousFeedback != null && previous != null && previous != otherLotus)
        {
            previousFeedback.SetHovered(false, haptics, false);
        }

        QuestInteractableFeedback feedback = lotus != null ? EnsureLotusFeedback(lotus) : null;
        feedback?.SetHovered(true, haptics);

        if (leftHand)
        {
            leftHoveredLotus = lotus;
            leftHoverFeedback = feedback;
            leftHoverPoint = point;
            leftHasHoverPoint = hasPoint;
        }
        else
        {
            rightHoveredLotus = lotus;
            rightHoverFeedback = feedback;
            rightHoverPoint = point;
            rightHasHoverPoint = hasPoint;
        }
    }

    private void TriggerHoveredLotus(bool leftHand, string label)
    {
        LotusNoteTrigger lotus = leftHand ? leftHoveredLotus : rightHoveredLotus;
        bool hasPoint = leftHand ? leftHasHoverPoint : rightHasHoverPoint;
        Vector3 point = leftHand ? leftHoverPoint : rightHoverPoint;
        Transform origin = leftHand ? leftRayOrigin : rightRayOrigin;
        if (lotus == null || origin == null || !hasPoint)
        {
            if (logDebugMessages)
            {
                Debug.Log($"[LotusDriver] {label} has no lotus selected.");
            }

            return;
        }

        lotus.TriggerNote(point, origin.position);
    }

    private void TryTriggerMouse()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            if (logDebugMessages)
            {
                Debug.LogWarning("[LotusDriver] Mouse debug has no Main Camera.");
            }

            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (TryResolveLotus(ray, out LotusNoteTrigger trigger, out Vector3 hitPoint))
        {
            trigger.TriggerNote(hitPoint, ray.origin);
        }
    }

    private bool TryResolveLotus(Ray ray, out LotusNoteTrigger trigger, out Vector3 hitPoint)
    {
        trigger = null;
        hitPoint = Vector3.zero;

        int hitCount = Physics.RaycastNonAlloc(ray, hitBuffer, rayDistance, rayMask, QueryTriggerInteraction.Collide);
        if (hitCount <= 0)
        {
            return false;
        }

        System.Array.Sort(hitBuffer, 0, hitCount, RaycastHitDistanceComparer.Instance);
        for (int i = 0; i < hitCount; i++)
        {
            Collider collider = hitBuffer[i].collider;
            if (collider == null)
            {
                continue;
            }

            LotusNoteTrigger candidate = collider.GetComponentInParent<LotusNoteTrigger>();
            if (candidate == null)
            {
                candidate = collider.GetComponentInChildren<LotusNoteTrigger>();
            }

            if (candidate == null)
            {
                continue;
            }

            trigger = candidate;
            hitPoint = hitBuffer[i].point;
            return true;
        }

        return false;
    }

    private QuestInteractableFeedback EnsureLotusFeedback(LotusNoteTrigger lotus)
    {
        QuestInteractableFeedback feedback = lotus.GetComponent<QuestInteractableFeedback>();
        if (feedback == null)
        {
            feedback = lotus.gameObject.AddComponent<QuestInteractableFeedback>();
        }

        feedback.Configure(lotusHoverOutlineColor, 0.018f);
        feedback.SetInteractable(true);
        return feedback;
    }

    private void UpdateQuestRay(bool leftHand, bool hover, Vector3 endPoint)
    {
        LineRenderer line = EnsureQuestRay(leftHand);
        Transform origin = leftHand ? leftRayOrigin : rightRayOrigin;
        // Yield the ray to whichever feature claimed it first this frame so multiple drivers
        // don't stack overlapping LineRenderers on the same controller.
        bool owned = QuestRayVisualBroker.TryClaim(this, !leftHand);
        if (!showQuestRays || line == null || origin == null || !owned)
        {
            if (line != null)
            {
                line.enabled = false;
            }

            return;
        }

        line.enabled = true;
        line.widthMultiplier = Mathf.Max(0.002f, rayWidth);
        Color color = hover ? hoverRayColor : idleRayColor;
        line.startColor = new Color(color.r, color.g, color.b, color.a * 0.2f);
        line.endColor = color;
        line.SetPosition(0, origin.position);
        line.SetPosition(1, endPoint);
    }

    private LineRenderer EnsureQuestRay(bool leftHand)
    {
        LineRenderer existing = leftHand ? leftQuestRay : rightQuestRay;
        if (existing != null)
        {
            return existing;
        }

        GameObject rayObject = new GameObject(leftHand ? "LeftLotusQuestRay" : "RightLotusQuestRay");
        rayObject.transform.SetParent(transform, false);
        LineRenderer line = rayObject.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.numCapVertices = 4;
        line.textureMode = LineTextureMode.Stretch;
        line.sharedMaterial = GetRuntimeRayMaterial();

        if (leftHand)
        {
            leftQuestRay = line;
        }
        else
        {
            rightQuestRay = line;
        }

        return line;
    }

    private Material GetRuntimeRayMaterial()
    {
        if (runtimeRayMaterial != null)
        {
            return runtimeRayMaterial;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        runtimeRayMaterial = new Material(shader);
        runtimeRayMaterial.renderQueue = 3050;
        if (runtimeRayMaterial.HasProperty("_Surface"))
        {
            runtimeRayMaterial.SetFloat("_Surface", 1f);
        }

        runtimeRayMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        runtimeRayMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
        runtimeRayMaterial.SetFloat("_ZWrite", 0f);
        runtimeRayMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        runtimeRayMaterial.EnableKeyword("_ALPHABLEND_ON");
        return runtimeRayMaterial;
    }

    private void DrawVisualRays()
    {
        if (leftRayOrigin != null)
        {
            Debug.DrawRay(leftRayOrigin.position, leftRayOrigin.forward * rayDistance, Color.green);
        }

        if (rightRayOrigin != null)
        {
            Debug.DrawRay(rightRayOrigin.position, rightRayOrigin.forward * rayDistance, Color.yellow);
        }
    }

    private void EnsureDevices()
    {
        if (!leftDevice.isValid)
        {
            leftDevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        }

        if (!rightDevice.isValid)
        {
            rightDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        }
    }

    private void EnsureHaptics()
    {
        if (leftHaptics == null)
        {
            leftHaptics = QuestInteractionUtils.FindHapticPlayer(false, leftRayOrigin);
        }

        if (rightHaptics == null)
        {
            rightHaptics = QuestInteractionUtils.FindHapticPlayer(true, rightRayOrigin);
        }
    }

    private bool IsPressed(XRInputDevice device)
    {
        if (!device.isValid)
        {
            return false;
        }

        return useTriggerButton && device.TryGetFeatureValue(XRCommonUsages.triggerButton, out bool pressed) && pressed;
    }

    private void AutoAssignRayOrigins()
    {
        if (leftRayOrigin == null)
        {
            leftRayOrigin = QuestInteractionUtils.FindControllerRayOrigin(false);
        }

        if (rightRayOrigin == null)
        {
            rightRayOrigin = QuestInteractionUtils.FindControllerRayOrigin(true);
        }
    }

    private sealed class RaycastHitDistanceComparer : System.Collections.IComparer
    {
        public static readonly RaycastHitDistanceComparer Instance = new RaycastHitDistanceComparer();

        public int Compare(object x, object y)
        {
            RaycastHit a = (RaycastHit)x;
            RaycastHit b = (RaycastHit)y;
            return a.distance.CompareTo(b.distance);
        }
    }
}
