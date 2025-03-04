using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime.Attributes;

namespace FeralTweaks.Mods.Charts
{
    /// <summary>
    /// Abstract for making custom chart def components, automatically picked up by FTL
    /// </summary>
    public abstract class FeralTweaksChartDefComponent : DefComponent
    {
        public FeralTweaksChartDefComponent()
        {
        }

        public FeralTweaksChartDefComponent(IntPtr pointer) : base(pointer)
        {
        }

        /// <summary>
        /// Called to deserialize the component
        /// </summary>
        /// <param name="componentJson">Component JSON data</param>
        [HideFromIl2Cpp]
        public abstract void Deserialize(Dictionary<string, object> componentJson);

    }
}