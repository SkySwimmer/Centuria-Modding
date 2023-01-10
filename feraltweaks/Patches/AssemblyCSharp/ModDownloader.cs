using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace feraltweaks.Patches.AssemblyCSharp
{
    public class ModDownloader
    {
        private static List<ManagerBase> managersToInit = new List<ManagerBase>();
        private static List<Action> uiActions = new List<Action>();
        private static bool DownloadedMods = false;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UI_ProgressScreen), "Update")]
        public static void Update()
        {
            if (uiActions.Count != 0)
            {
                Action[] actions;
                while (true)
                {
                    try
                    {
                        actions = uiActions.ToArray();
                        break;
                    }
                    catch { }
                }
                foreach (Action ac in actions)
                {
                    uiActions.Remove(ac);
                    ac.Invoke();
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CoreBase<Core>), "Awake")]
        public static bool Awake(CoreBase<Core> __instance)
        {
            if (DownloadedMods)
                return true;
            Plugin.logger.LogInfo("test");
            Task.Run(() =>
            {
                while (UI_ProgressScreen.instance == null)
                    Thread.Sleep(100);
                uiActions.Add(() => {
                    UI_ProgressScreen.instance.SetProgressLabelWithIndex(0, "Downloading mods...");
                    UI_ProgressScreen.instance.SetProgressLabelWithIndex(1, "Contacting server...");
                });
                Thread.Sleep(15000);
                DownloadedMods = true;
                uiActions.Add(() =>
                {
                    __instance.Awake();
                });
            });
            return false;
        }
    }
}
