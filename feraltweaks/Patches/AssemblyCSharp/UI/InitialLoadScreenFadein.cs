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
    public static class InitialLoadScreenFadein
    {
        private static bool _doneLoadingPatch;
        private static long _timeInitialFadeStart;
        private static long _timeLastFrame;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UI_ProgressScreen), "Update")]
        public static void OnScreenUpdate()
        {
            if (_doneLoadingPatch)
                return;
            long timeBetweenFrames = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - _timeLastFrame;
            if (_timeInitialFadeStart == 0)
            {
                UI_ProgressScreen.instance._backgroundImage.color = UI_ProgressScreen.instance._transparent;
                _timeInitialFadeStart = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 500;
            }
            else
            {
                // Handle lag spikes
                if (timeBetweenFrames >= 200)
                    _timeInitialFadeStart += timeBetweenFrames;

                // Process fade
                long currentTimeSpent = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - _timeInitialFadeStart;
                if (currentTimeSpent < 0)
                    currentTimeSpent = 0;
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
                        (1f / 750) * (float)currentTimeSpent);
                }
            }
            _timeLastFrame = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

    }
}
