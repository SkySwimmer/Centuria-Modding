using System.Reflection;
using HarmonyLib;
using Il2CppSystem;
using Il2CppSystem.Collections.Generic;
using Il2CppInterop.Runtime;
using LitJson;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Iss;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes;
using System.IO;
using FeralTweaks.Mods;
using FeralTweaks;
using System.Linq;
using StrayTech;
using UnityEngine;
using UniRx.Async;

namespace feraltweaks.Patches.AssemblyCSharp
{
    public static class DecreePatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(DecreeSrvHandler), "RequestDecrees")]
        public static bool RequestDecrees(ref UniTask<ListDecreesResponse> __result)
        {
            // Get def id list
            List<BaseDef> defs = ListChartData.instance.GetDef("12803").GetComponent<ListDefComponent>().Defs;

            // Create list
            System.Collections.Generic.List<DecreeDefComponent> itms = new System.Collections.Generic.List<DecreeDefComponent>();
            foreach (BaseDef def in defs)
            {
                // Check availability
                if (def.HasComponent<AvailabilityDefComponent>())
                {
                    // Skip if unavailable
                    if (!def.GetComponent<AvailabilityDefComponent>().chartDateAvailability.IsAvailable)
                        continue;
                }

                // Add decree
                DecreeDefComponent decree = def.GetComponent<DecreeDefComponent>();
                if (decree != null)
                    itms.Add(decree);
            }

            // Find dynamic decrees
            System.Collections.Generic.List<DecreeData> dynDecrees = new System.Collections.Generic.List<DecreeData>();
            foreach (BaseDef def in ChartPatches.DefCache.Values)
            {
                DecreeDateDefComponent decreeDate = def.GetComponent<DecreeDateDefComponent>();
                DecreeDefComponent decree = def.GetComponent<DecreeDefComponent>();
                if (decreeDate != null && decree != null)
                {
                    AvailabilityDefComponent availability = def.GetComponent<AvailabilityDefComponent>();
                    dynDecrees.Add(new DecreeData()
                    {
                        def = def,
                        decree = decree,
                        date = decreeDate,
                        availability = availability,
                        ignoreYear = availability != null && availability.chartDateAvailability.DateStart != null && availability.chartDateAvailability.DateStart != "" && availability.chartDateAvailability._ignoreYear
                    });
                }
            }

            // Move defs that are tied to date
            DecreeDefComponent[] arr = itms.ToArray();
            foreach (DecreeDefComponent decree in arr)
            {
                BaseDef def = decree.def;
                AvailabilityDefComponent availability = def.GetComponent<AvailabilityDefComponent>();
                if (availability != null && availability.chartDateAvailability != null)
                {
                    // Check
                    if (availability.chartDateAvailability.DateStart != null && availability.chartDateAvailability.DateStart != "")
                    {
                        // Remove from list
                        itms.Remove(decree);

                        // Add to dynamic list
                        dynDecrees.Add(new DecreeData()
                        {
                            def = def,
                            decree = decree,
                            date = new DecreeDateDefComponent()
                            {
                                decreeDate = availability.chartDateAvailability._dateStart
                            },
                            availability = availability,
                            ignoreYear = availability.chartDateAvailability.IgnoreYear
                        });
                    }
                }
            }

            // Load dates of dynamic decrees
            foreach (DecreeData dynDecree in dynDecrees)
            {
                // Check availability
                if (dynDecree.availability == null || dynDecree.availability.chartDateAvailability == null || dynDecree.availability.chartDateAvailability.IsAvailable)
                {
                    // Parse
                    Nullable<DateTime> dateN = ChartDate.ParseDateOrNull(dynDecree.date.decreeDate);
                    if (dateN.HasValue)
                    {
                        DateTime date = dateN.Value;
                        if (dynDecree.ignoreYear)
                            date = date.AddYears(DateTime.Now.Year - date.Year);
                        dynDecree.decreeDate = date;
                    }
                }
            }

            // Add dynamic decrees
            System.Collections.Generic.List<DecreeDefComponent> itemListNew = new System.Collections.Generic.List<DecreeDefComponent>();
            for (int i = 0; i < dynDecrees.Count; i++)
            {
                // Find first newest decree thats not already in the list
                DecreeData newest = null;
                foreach (DecreeData dynDecree in dynDecrees)
                {
                    if (dynDecree.decreeDate != null && !itemListNew.Contains(dynDecree.decree))
                    {
                        // Check age
                        if (dynDecree.decreeDate != null)
                        {
                            if (newest == null || dynDecree.decreeDate > newest.decreeDate)
                            {
                                newest = dynDecree;
                            }
                        }
                    }
                }

                // Add
                if (newest != null)
                    itemListNew.Add(newest.decree);
            }
            itemListNew.AddRange(itms);

            // Assign items
            Il2CppReferenceArray<DecreeDefComponent> lst = new Il2CppReferenceArray<DecreeDefComponent>((long)itemListNew.Count);
            for (int i = 0; i < itemListNew.Count ; i++)
            {
                lst[i] = itemListNew[i];
            }
            ListDecreesResponse resp = new ListDecreesResponse(lst);
            __result = UniTask.FromResult(resp);
            return false;
        }

        private class DecreeData
        {
            public BaseDef def;
            public DecreeDateDefComponent date;
            public DecreeDefComponent decree;
            public AvailabilityDefComponent availability;
            public DateTime decreeDate;
            public bool ignoreYear;
        }
    }
}