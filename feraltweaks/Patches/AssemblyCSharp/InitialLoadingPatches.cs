using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace feraltweaks.Patches.AssemblyCSharp
{
    public static class InitialLoadingPatches
    {
        private static bool _doneLoadingPatch;
        private static long _timeInitialFadeStart;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UI_ProgressScreen), "Update")]
        public static void OnScreenUpdate()
        {
            if (_doneLoadingPatch)
                return;
            if (_timeInitialFadeStart == 0)
            {
                UI_ProgressScreen.instance._backgroundImage.color = UI_ProgressScreen.instance._transparent;
                _timeInitialFadeStart = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 800;
            }
            else
            {
                long currentTimeSpent = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - _timeInitialFadeStart;
                if (currentTimeSpent >= 750)
                {
                    UI_ProgressScreen.instance._backgroundImage.color = UI_ProgressScreen.instance._opaque;
                    _doneLoadingPatch = true;
                }
                else
                {
                    // Fade
                    UI_ProgressScreen.instance._backgroundImage.color = new Color(UI_ProgressScreen.instance._opaque.r,
                        UI_ProgressScreen.instance._opaque.g,
                        UI_ProgressScreen.instance._opaque.b,
                        (1f / 750f) * (float)currentTimeSpent);
                }
            }
        }

    }
}
