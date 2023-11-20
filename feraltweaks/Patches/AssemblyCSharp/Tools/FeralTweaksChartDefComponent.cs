using System;
using System.Collections.Generic;

namespace FeralTweaks.Mods
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
        public abstract void Deserialize(Dictionary<string, object> componentJson);

    }
}