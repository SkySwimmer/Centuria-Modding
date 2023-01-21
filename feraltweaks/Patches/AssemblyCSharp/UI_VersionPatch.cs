using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using WW.Waiters;

namespace feraltweaks.Patches.AssemblyCSharp
{
    public class UI_VersionPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(WaitController), "Update")]
        public static void Update()
        {
            if (UI_Version.instance != null)
            {
                // Hide outside of loading screen and escape menu
                if (((UI_ProgressScreen.instance != null && UI_ProgressScreen.instance.IsVisible && !UI_ProgressScreen.instance.IsFading)
                        || (RoomManager.instance != null && RoomManager.instance.CurrentLevelDefID == "58")
                        || (CoreWindowManager.coreInstance != null
                            && (CoreWindowManager.GetWindow<UI_Window_Login>() != null || CoreWindowManager.GetWindow<UI_Window_AccountCreation>() != null
                                || CoreWindowManager.GetWindow<UI_Window_Feedback>() != null
                                || CoreWindowManager.GetWindow<UI_Window_Settings>() != null))))
                {
                    UI_Version.instance._text.gameObject.SetActive(true);
                }
                else 
                {
                    UI_Version.instance._text.gameObject.SetActive(false);
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UI_Version), "Start")]
        public static bool Start(UI_Version __instance)
        {
            // Assign instance field
            if (UI_Version.instance == null)
            {
                UI_Version.instance = __instance;
                GameObject.DontDestroyOnLoad(__instance.gameObject);
            }

            // Assign label
            Plugin.actions.Add(() =>
            {
                if (CoreBase<Core>.Loaded)
                {
                    Plugin.uiActions.Add(() =>
                    {
                        // Update UI

                        // Create template
                        string lbl = "${global:7358}\n${game:version} (${game:build})";

                        // Load if present in config
                        if (Plugin.PatchConfig.ContainsKey("VersionLabel"))
                            lbl = Plugin.PatchConfig["VersionLabel"].Replace("\\n", "\n");

                        // Replace variables
                        bool inVar = false;
                        string varKey = "";
                        string varstart = "${";
                        string buffer = "";
                        string text = "";
                        int bI = 0;
                        foreach (char ch in lbl)
                        {

                            if (!inVar)
                            {
                                if (buffer.Length == varstart.Length)
                                {
                                    // Variable
                                    inVar = true;
                                    varKey += ch;
                                    buffer = "";
                                    bI = 0;
                                }
                                else if (varstart[bI] == ch)
                                {
                                    buffer += ch;
                                    bI++;
                                }
                                else
                                {
                                    if (buffer != "")
                                    {
                                        text += buffer;
                                        buffer = "";
                                    }
                                    text += ch;
                                }
                            }
                            else
                            {
                                if (ch == '}')
                                {
                                    // Variable close
                                    inVar = false;

                                    // Handle variable
                                    if (varKey.StartsWith("game:"))
                                    {
                                        // Game information
                                        switch (varKey.Substring(5))
                                        {
                                            case "version":
                                                {
                                                    text += CoreGlobalSettingsManager.coreInstance.version;
                                                    break;
                                                }
                                            case "build":
                                                {
                                                    text += CoreGlobalSettingsManager.coreInstance.currentBuildNumber;
                                                    break;
                                                }
                                        }
                                    }
                                    else if (varKey.StartsWith("global:"))
                                    {
                                        // String value from global chart
                                        string def = varKey.Substring("global:".Length);
                                        BaseDef defInfo = ChartDataManager.instance.globalChartData.GetDef(def);
                                        if (defInfo != null)
                                        {
                                            GlobalDefComponent comp = defInfo.GetComponent<GlobalDefComponent>();
                                            text += comp.stringValue;
                                        }
                                    }
                                    else if (varKey.StartsWith("localization:"))
                                    {
                                        // String value from localization chart
                                        string def = varKey.Substring("localization:".Length);
                                        text += ChartDataManager.instance.localizationChartData.Get(def);
                                    }

                                    // Clear
                                    varKey = "";
                                }
                                else
                                    varKey += ch;
                            }
                        }
                        if (buffer != "")
                            text += buffer;
                        __instance._text.text = text;
                    });
                    return true;
                }
                return false;
            });

            return false;
        }
    }
}