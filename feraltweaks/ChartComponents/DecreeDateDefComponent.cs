using System;
using Newtonsoft.Json;
using FeralTweaks.Mods.Charts;
using System.Collections.Generic;
using Il2CppInterop.Runtime.Attributes;

public class DecreeDateDefComponent : FeralTweaksChartDefComponent
{
    public DecreeDateDefComponent()
    {
    }

    public DecreeDateDefComponent(IntPtr pointer) : base(pointer)
    {
    }

    [HideFromIl2Cpp]
    public override void Deserialize(Dictionary<string, object> json)
    {
        decreeDate = json["decreeDate"].ToString();
    }

    public string decreeDate;
}