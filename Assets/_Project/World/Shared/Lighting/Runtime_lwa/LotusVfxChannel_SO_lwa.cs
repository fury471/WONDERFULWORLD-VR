using System;
using UnityEngine;

namespace WonderfulWorld.World.Shared.VfxHooks
{
    [CreateAssetMenu(fileName = "LotusVfxChannel_SO", menuName = VfxHookDefaults.LotusMenuRoot + "/Event Channel")]
    public class LotusVfxChannel_SO_lwa : ScriptableObject
    {
        public event Action<LotusVfxEvent> Raised;

        public void Raise(LotusVfxEvent evt)
        {
            Raised?.Invoke(evt);
        }
    }
}
