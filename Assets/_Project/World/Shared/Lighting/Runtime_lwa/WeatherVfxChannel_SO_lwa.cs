using System;
using UnityEngine;

namespace WonderfulWorld.World.Shared.VfxHooks
{
    [CreateAssetMenu(fileName = "WeatherVfxChannel_SO", menuName = VfxHookDefaults.WeatherMenuRoot + "/Event Channel")]
    public class WeatherVfxChannel_SO_lwa : ScriptableObject
    {
        public event Action<WeatherVfxEvent> Raised;

        public void Raise(WeatherVfxEvent evt)
        {
            Raised?.Invoke(evt);
        }
    }
}
