using UnityEngine;
using WonderfulWorld.Features.Fireworks;

namespace WonderfulWorld.World.Shared.VfxHooks
{
    [DisallowMultipleComponent]
    public class FireworksVfxHooks_lwa : MonoBehaviour
    {
        [Header("Source")]
        [SerializeField] private FireworkController fireworkController;

        [Header("Outputs")]
        [SerializeField] private bool setShaderGlobals = true;
        [SerializeField] private FireworkVfxChannel_SO_lwa eventChannel;

        private int lastPatternIndex = -1;

        private void Reset()
        {
            fireworkController = GetComponent<FireworkController>();
        }

        private void OnEnable()
        {
            if (fireworkController == null)
            {
                fireworkController = FindFirstObjectByType<FireworkController>();
            }

            if (fireworkController != null)
            {
                fireworkController.PointCloudFireworkSpawned += OnPointCloudFireworkSpawned;
            }
        }

        private void OnDisable()
        {
            if (fireworkController != null)
            {
                fireworkController.PointCloudFireworkSpawned -= OnPointCloudFireworkSpawned;
            }
        }

        private void OnPointCloudFireworkSpawned(PointCloudFireworkRequest request, Vector3 worldPosition)
        {
            lastPatternIndex++;

            FireworkVfxEvent evt = new FireworkVfxEvent
            {
                patternName = string.IsNullOrWhiteSpace(request.displayName) ? request.kind.ToString() : request.displayName,
                patternIndex = lastPatternIndex,
                worldPosition = worldPosition,
                color = request.color,
                sizeMultiplier = request.scale,
                time = Time.time
            };

            if (setShaderGlobals)
            {
                WonderfulWorldVfxShaderGlobals_lwa.SetLastFirework(evt);
            }

            eventChannel?.Raise(evt);
        }
    }
}
