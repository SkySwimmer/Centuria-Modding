using System;
using Newtonsoft.Json;
using FeralTweaks.Mods.Charts;
using System.Collections.Generic;

public class DecreeDateDefComponent : FeralTweaksChartDefComponent
{
    public DecreeDateDefComponent()
    {
    }

    public DecreeDateDefComponent(IntPtr pointer) : base(pointer)
    {
    }

    public override void Deserialize(Dictionary<string, object> json)
    {
        decreeDate = json["decreeDate"].ToString();
    }

    public string decreeDate;
}