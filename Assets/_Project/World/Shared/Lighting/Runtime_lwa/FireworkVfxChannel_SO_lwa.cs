using System;
using UnityEngine;

namespace WonderfulWorld.World.Shared.VfxHooks
{
    [CreateAssetMenu(fileName = "FireworkVfxChannel_SO", menuName = VfxHookDefaults.FireworksMenuRoot + "/Event Channel")]
    public class FireworkVfxChannel_SO_lwa : ScriptableObject
    {
        public event Action<FireworkVfxEvent> Raised;

        public void Raise(FireworkVfxEvent evt)
        {
            Raised?.Invoke(evt);
        }
    }
}
