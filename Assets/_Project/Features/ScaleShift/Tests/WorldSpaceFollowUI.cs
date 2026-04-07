using UnityEngine;

namespace WonderfulWorld.Features.Fireworks
{
    public class WorldSpaceFollowUI : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 localOffset = new Vector3(0f, -0.15f, 1.5f);
        [SerializeField] private bool followRotation = true;
        [SerializeField] private bool yawOnly = true;
        [SerializeField] private float followLerpSpeed = 8f;

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Quaternion targetRotation = ResolveRotation();
            Vector3 targetPosition = target.position + targetRotation * localOffset;

            float t = Mathf.Clamp01(followLerpSpeed * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, targetPosition, t);

            if (followRotation)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, t);
            }
        }

        private Quaternion ResolveRotation()
        {
            if (!followRotation)
            {
                return transform.rotation;
            }

            if (!yawOnly)
            {
                return target.rotation;
            }

            Vector3 forward = target.forward;
            forward.y = 0f;

            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = Vector3.forward;
            }

            return Quaternion.LookRotation(forward.normalized, Vector3.up);
        }
    }
}
