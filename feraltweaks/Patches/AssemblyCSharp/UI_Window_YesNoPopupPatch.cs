using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace feraltweaks.Patches.AssemblyCSharp
{
    public class UI_Window_YesNoPopupPatch
    {
        public static Action SingleTimeYesButtonAction;
        public static Action SingleTimeNoButtonAction;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UI_Window_YesNoPopup), "BtnClicked_Response")]
        private static void BtnClicked_Response(ref UI_Window_YesNoPopup __instance, bool inResponse)
        {
            if (inResponse)
            {
                if (SingleTimeYesButtonAction != null)
                {
                    try
                    {
                        SingleTimeYesButtonAction();
                    }
                    finally
                    {
                        SingleTimeNoButtonAction = null;
                        SingleTimeYesButtonAction = null;
                    }
                }
            }
            else
            {
                if (SingleTimeNoButtonAction != null)
                {
                    try
                    {
                        SingleTimeNoButtonAction();
                    }
                    finally
                    {
                        SingleTimeNoButtonAction = null;
                        SingleTimeYesButtonAction = null;
                    }
                }
            }
        }
    }
}
