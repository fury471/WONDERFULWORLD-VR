using UnityEngine;
using UnityEngine.InputSystem;

public class HorseSummonV2 : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CatRideControllerV2 rideController;
    [SerializeField] private CatIdlePaceV2 idlePaceController;
    [SerializeField] private Animator horseAnimator;
    [SerializeField] private Transform playerRigRoot;
    [SerializeField] private Transform playerView;
    [SerializeField] private Transform summonTargetAnchor;


    [Header("Summon")]
    [SerializeField] private Key summonKey = Key.X;
    [SerializeField] private InputActionReference summonAction;
    [SerializeField] private bool enableQuestLeftXButton = true;
    [SerializeField] private float summonMoveSpeed = 7.8125f;
    [SerializeField] private float summonRotateSpeed = 240f;
    [SerializeField] private float arriveDistance = 0.2f;
    [SerializeField] private float standFrontDistance = 2.0f;

    [Header("Terrain Motion")]
    [SerializeField] private bool projectSummonMotionToGround = true;
    [SerializeField] private LayerMask summonGroundMask = ~0;
    [SerializeField] private float summonGroundProbeHeight = 3f;
    [SerializeField] private float summonGroundProbeDistance = 12f;
    [SerializeField] private float summonGroundOffset = 0f;
    [SerializeField] private float summonMaxStepUp = 1.5f;
    [SerializeField] private float summonMaxStepDown = 5f;
    [SerializeField] private bool alignSummonToGroundNormal = true;
    [SerializeField] private Transform summonVisualTiltRoot;
    [SerializeField] private float summonGroundAlignSpeed = 240f;
    [SerializeField] private float summonMaxGroundTiltAngle = 32f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private readonly RaycastHit[] summonGroundHitBuffer = new RaycastHit[8];
    private bool isSummoning;
    private bool questLeftXWasPressed;
    private Vector3 summonTargetPosition;
    private Quaternion summonTargetRotation;
    private Vector3 lastSummonGroundNormal = Vector3.up;
    private bool hasSummonGroundNormal;

    public bool IsFootstepMotionActive => isSummoning || (rideController != null && rideController.IsRideActive);

    private void Awake()
    {
        AutoAssignReferences(includeSceneReferences: true);
    }

    private void OnValidate()
    {
        AutoAssignReferences(includeSceneReferences: false);
    }

    private void Update()
    {
        if (!isSummoning && WasSummonPressedThisFrame())
        {
            StartSummon();
        }

        if (isSummoning)
        {
            UpdateSummon();
        }
    }

    private void AutoAssignReferences(bool includeSceneReferences)
    {
        if (rideController == null)
        {
            rideController = GetComponent<CatRideControllerV2>();
            if (rideController == null)
            {
                rideController = GetComponentInParent<CatRideControllerV2>();
            }
        }

        if (idlePaceController == null)
        {
            idlePaceController = GetComponent<CatIdlePaceV2>();
        }

        if (horseAnimator == null)
        {
            horseAnimator = GetComponentInChildren<Animator>(true);
        }

        if (summonVisualTiltRoot == null)
        {
            summonVisualTiltRoot = ResolveVisualTiltRoot();
        }

        if (!includeSceneReferences)
        {
            return;
        }

        if (playerView == null)
        {
            playerView = QuestInteractionUtils.FindHeadTransform();
        }

        if (playerRigRoot == null && playerView != null)
        {
            playerRigRoot = playerView.root;
        }
    }

    private bool WasSummonPressedThisFrame()
    {
        if (summonAction != null && summonAction.action != null && summonAction.action.WasPressedThisFrame())
        {
            return true;
        }

        if (Keyboard.current != null && Keyboard.current[summonKey].wasPressedThisFrame)
        {
            return true;
        }

        if (!enableQuestLeftXButton)
        {
            questLeftXWasPressed = false;
            return false;
        }

        bool pressed = QuestInteractionUtils.TryReadPrimaryButton(false, out bool leftPrimaryPressed) && leftPrimaryPressed;
        bool pressedThisFrame = pressed && !questLeftXWasPressed;
        questLeftXWasPressed = pressed;
        return pressedThisFrame;
    }

    private void StartSummon()
    {
        AutoAssignReferences(includeSceneReferences: true);

        if (rideController != null && rideController.IsRideActive)
        {
            return;
        }

        if (playerRigRoot == null)
        {
            return;
        }

        if (idlePaceController != null)
        {
            idlePaceController.enabled = false;
        }

        transform.position = ResolveGroundedPosition(transform.position, true);
        RefreshSummonTarget();

        isSummoning = true;
        WonderfulWorld.Audio.WonderlandMountAudioAutoBinder.PlayVoice(gameObject, volumeScale: 1f, maxVoices: 2);

        if (debugLogs)
        {
            Debug.Log("[HorseSummonV2] Summon started.");
        }
    }

    private void UpdateSummon()
    {
        if (playerRigRoot == null)
        {
            return;
        }

        RefreshSummonTarget();

        Vector3 flatTargetPosition = new Vector3(
            summonTargetPosition.x,
            transform.position.y,
            summonTargetPosition.z);

        Vector3 nextPosition = Vector3.MoveTowards(
            transform.position,
            flatTargetPosition,
            summonMoveSpeed * Time.deltaTime);
        transform.position = ResolveGroundedPosition(nextPosition, false);

        Vector3 direction = summonTargetPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                summonRotateSpeed * Time.deltaTime);
        }

        AlignSummonToGround(false);
        SetRunAnimation();

        if (HorizontalDistance(transform.position, summonTargetPosition) <= arriveDistance)
        {
            isSummoning = false;
            transform.position = ResolveGroundedPosition(summonTargetPosition, true);
            transform.rotation = summonTargetRotation;
            AlignSummonToGround(true);
            SetIdleAnimation();

            if (debugLogs)
            {
                Debug.Log("[HorseSummonV2] Summon complete.");
            }
        }
    }

    private void RefreshSummonTarget()
    {
        AutoAssignReferences(includeSceneReferences: true);
        summonTargetPosition = ResolveGroundedPosition(ResolveTargetPosition(), true);
        summonTargetRotation = ResolveFacingPlayerRotation(summonTargetPosition);

        if (summonTargetAnchor != null)
        {
            summonTargetAnchor.position = summonTargetPosition;
            summonTargetAnchor.rotation = summonTargetRotation;
        }
    }

    private Vector3 ResolveTargetPosition()
    {
        Transform directionSource = playerView != null ? playerView : playerRigRoot;

        Vector3 forward = directionSource.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = Vector3.forward;
        }

        forward.Normalize();

        Vector3 basePosition = playerRigRoot.position;
        return basePosition + forward * standFrontDistance;
    }

    private Quaternion ResolveFacingPlayerRotation(Vector3 horseWorldPosition)
    {
        if (playerRigRoot == null)
        {
            return transform.rotation;
        }

        Vector3 directionToPlayer = playerRigRoot.position - horseWorldPosition;
        directionToPlayer.y = 0f;

        if (directionToPlayer.sqrMagnitude < 0.0001f)
        {
            return transform.rotation;
        }

        return Quaternion.LookRotation(directionToPlayer.normalized, Vector3.up);
    }

    private Vector3 ResolveGroundedPosition(Vector3 desiredPosition, bool ignoreStepLimit)
    {
        if (!projectSummonMotionToGround)
        {
            hasSummonGroundNormal = false;
            return desiredPosition;
        }

        Vector3 origin = desiredPosition + Vector3.up * Mathf.Max(0.1f, summonGroundProbeHeight);
        float distance = Mathf.Max(0.1f, summonGroundProbeDistance);
        int hitCount = Physics.RaycastNonAlloc(
            origin,
            Vector3.down,
            summonGroundHitBuffer,
            distance,
            summonGroundMask,
            QueryTriggerInteraction.Ignore);

        float bestDistance = float.PositiveInfinity;
        bool foundGround = false;
        Vector3 groundPoint = desiredPosition;
        Vector3 groundNormal = Vector3.up;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = summonGroundHitBuffer[i];
            Collider hitCollider = hit.collider;
            if (hitCollider == null || IsIgnoredGroundCollider(hitCollider))
            {
                continue;
            }

            if (hit.distance < bestDistance)
            {
                bestDistance = hit.distance;
                groundPoint = hit.point;
                groundNormal = hit.normal.sqrMagnitude > 0.0001f ? hit.normal.normalized : Vector3.up;
                foundGround = true;
            }
        }

        if (!foundGround)
        {
            hasSummonGroundNormal = false;
            return desiredPosition;
        }

        float deltaY = groundPoint.y - transform.position.y;
        if (!ignoreStepLimit && (deltaY > Mathf.Max(0f, summonMaxStepUp) || deltaY < -Mathf.Max(0f, summonMaxStepDown)))
        {
            hasSummonGroundNormal = false;
            return desiredPosition;
        }

        desiredPosition.y = groundPoint.y + summonGroundOffset;
        lastSummonGroundNormal = ClampGroundNormalTilt(groundNormal);
        hasSummonGroundNormal = true;
        return desiredPosition;
    }

    private bool IsIgnoredGroundCollider(Collider candidate)
    {
        if (candidate == null || candidate.isTrigger)
        {
            return true;
        }

        if (candidate.transform.IsChildOf(transform))
        {
            return true;
        }

        return playerRigRoot != null && candidate.transform.IsChildOf(playerRigRoot);
    }

    private Vector3 ClampGroundNormalTilt(Vector3 normal)
    {
        if (normal.sqrMagnitude < 0.0001f)
        {
            return Vector3.up;
        }

        normal.Normalize();
        float angle = Vector3.Angle(Vector3.up, normal);
        float maxAngle = Mathf.Max(0f, summonMaxGroundTiltAngle);
        if (angle <= maxAngle || angle <= 0.001f)
        {
            return normal;
        }

        return Vector3.Slerp(Vector3.up, normal, maxAngle / angle).normalized;
    }

    private void AlignSummonToGround(bool immediate)
    {
        if (!alignSummonToGroundNormal || !hasSummonGroundNormal)
        {
            return;
        }

        Transform tiltRoot = summonVisualTiltRoot != null ? summonVisualTiltRoot : transform;
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, lastSummonGroundNormal);
        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = Vector3.ProjectOnPlane(tiltRoot.forward, lastSummonGroundNormal);
        }

        if (forward.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(forward.normalized, lastSummonGroundNormal);
        if (immediate)
        {
            tiltRoot.rotation = targetRotation;
            return;
        }

        tiltRoot.rotation = Quaternion.RotateTowards(
            tiltRoot.rotation,
            targetRotation,
            Mathf.Max(0f, summonGroundAlignSpeed) * Time.deltaTime);
    }

    private Transform ResolveVisualTiltRoot()
    {
        if (horseAnimator != null && horseAnimator.transform != transform)
        {
            return horseAnimator.transform;
        }

        Renderer renderer = GetComponentInChildren<Renderer>(true);
        if (renderer != null && renderer.transform != transform)
        {
            return renderer.transform;
        }

        return null;
    }

    private static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private void SetIdleAnimation()
    {
        if (horseAnimator == null)
        {
            return;
        }

        horseAnimator.SetFloat("Vert", 0f);
        horseAnimator.SetFloat("State", 0f);
    }

    private void SetRunAnimation()
    {
        if (horseAnimator == null)
        {
            return;
        }

        horseAnimator.SetFloat("Vert", 1f);
        horseAnimator.SetFloat("State", 1f);
    }
}
