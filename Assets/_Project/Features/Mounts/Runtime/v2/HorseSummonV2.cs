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
    [SerializeField] private float summonMoveSpeed = 5f;
    [SerializeField] private float summonRotateSpeed = 240f;
    [SerializeField] private float arriveDistance = 0.2f;
    [SerializeField] private float standFrontDistance = 2.0f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private bool isSummoning;
    private Vector3 summonTargetPosition;
    private Quaternion summonTargetRotation;

    private void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (!isSummoning && Keyboard.current[summonKey].wasPressedThisFrame)
        {
            StartSummon();
        }

        if (isSummoning)
        {
            UpdateSummon();
        }
    }

    private void StartSummon()
    {
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

        summonTargetPosition = ResolveTargetPosition();
        summonTargetRotation = ResolveFacingPlayerRotation(summonTargetPosition);

        if (summonTargetAnchor != null)
        {
            summonTargetAnchor.position = summonTargetPosition;
            summonTargetAnchor.rotation = summonTargetRotation;
        }

        isSummoning = true;

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

        Vector3 flatTargetPosition = new Vector3(
            summonTargetPosition.x,
            transform.position.y,
            summonTargetPosition.z);

        transform.position = Vector3.MoveTowards(
            transform.position,
            flatTargetPosition,
            summonMoveSpeed * Time.deltaTime);

        Vector3 direction = flatTargetPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                summonRotateSpeed * Time.deltaTime);
        }

        SetRunAnimation();

        if (Vector3.Distance(transform.position, flatTargetPosition) <= arriveDistance)
        {
            isSummoning = false;
            transform.position = flatTargetPosition;
            transform.rotation = summonTargetRotation;
            SetIdleAnimation();

            if (debugLogs)
            {
                Debug.Log("[HorseSummonV2] Summon complete.");
            }
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
