using UnityEngine;

namespace WonderfulWorld.Audio
{
    [DisallowMultipleComponent]
    public sealed class WonderlandMountAudioProfile : MonoBehaviour
    {
        [SerializeField] private MountFootstepProfile profile = MountFootstepProfile.Cat;
        [SerializeField] private Transform footstepEmitter;

        public MountFootstepProfile Profile => profile;
        public Transform FootstepEmitter => footstepEmitter;
    }
}
