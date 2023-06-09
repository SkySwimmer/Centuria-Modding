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
    public class UI_Window_OkPopupPatch
    {
        public static Action SingleTimeOkButtonAction;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UI_Window_OkPopup), "OnClose")]
        private static void Close()
        {
            SingleTimeOkButtonAction = null;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UI_Window_OkPopup), "BtnClicked_Ok")]
        private static void BtnClicked_Ok(ref UI_Window_OkPopup __instance)
        {
            if (SingleTimeOkButtonAction != null)
            {
                try
                {
                    SingleTimeOkButtonAction();
                }
                finally
                {
                    SingleTimeOkButtonAction = null;
                }
            }
        }
    }
}
