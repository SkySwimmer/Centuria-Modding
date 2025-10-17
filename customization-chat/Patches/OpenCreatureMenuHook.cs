using System;
using System.Collections.Generic;
using CodeStage.AntiCheat.ObscuredTypes;
using CustomizationChat.Actions;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;
using UnityEngine.UI;

namespace CustomizationChat.Patches
{
    public class OpenCreatureMenuHook
    {
        public class ChatWindowVars_CCMMOD : MonoBehaviour
        {
            private static bool injected;

            public static void Init()
            {
                if (!injected)
                    ClassInjector.RegisterTypeInIl2Cpp<ChatWindowVars_CCMMOD>();
                injected = true;
            }

            public ChatWindowVars_CCMMOD() : base() { }
            public ChatWindowVars_CCMMOD(IntPtr ptr) : base(ptr) { }

            public bool _inited;
            public bool _blockOpenAnim;
            public bool _blockSfx;
            public bool _blockSavePositionAndSize;
            public bool _saveCustomizationPositionAndSize;
        }

        public class CustomizationChatController : MonoBehaviour
        {
            private static bool injected;

            public static void Init()
            {
                if (!injected)
                    ClassInjector.RegisterTypeInIl2Cpp<CustomizationChatController>();
                injected = true;
            }

            public CustomizationChatController() : base() { }
            public CustomizationChatController(IntPtr ptr) : base(ptr) { }

            public bool ChatHasBeenOpened;
            public bool WasChatOriginallyOpen;
            public bool WasVoiceOriginallyOpen;
            public bool WasMenuOpen;
            public bool WasChatAvailable;
            public bool WasVoiceAvailable;

            public Vector2 OriginalChatPosition;
            public Vector2 OriginalChatSize;
            public Vector2 OriginalChatPivot;
            public Vector2 OriginalChatAnchoredPosition;
            public Vector2 OriginalChatAnchorMin;
            public Vector2 OriginalChatAnchorMax;

            public Vector2 OriginalHidePositionChat;
            public Vector2 OriginalHidePositionChatAnchorMin;
            public Vector2 OriginalHidePositionChatAnchorMax;

            public Vector2 OriginalVoicePosition;
            public Vector2 OriginalVoicePivot;
            public Vector2 OriginalVoiceAnchoredPosition;
            public Vector2 OriginalVoiceAnchorMin;
            public Vector2 OriginalVoiceAnchorMax;

            public Vector2 OriginalHidePositionVoice;
            public Vector2 OriginalHidePositionVoiceAnchorMin;
            public Vector2 OriginalHidePositionVoiceAnchorMax;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UI_Window_Chat), "SaveWindowSize")]
        public static bool OnSave(ref UI_Window_Chat __instance)
        {
            ChatWindowVars_CCMMOD.Init();
            ChatWindowVars_CCMMOD vars = __instance.GetComponent<ChatWindowVars_CCMMOD>();
            if (vars == null)
                vars = __instance.gameObject.AddComponent<ChatWindowVars_CCMMOD>();
            if (vars._blockSavePositionAndSize)
                return false;

            // Default save logic
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UI_Window_VoiceChat), "SaveWindowSize")]
        public static bool OnSave(ref UI_Window_VoiceChat __instance)
        {
            ChatWindowVars_CCMMOD.Init();
            ChatWindowVars_CCMMOD vars = __instance.GetComponent<ChatWindowVars_CCMMOD>();
            if (vars == null)
                vars = __instance.gameObject.AddComponent<ChatWindowVars_CCMMOD>();
            if (vars._blockSavePositionAndSize)
                return false;

            // Default save logic
            return true;
        }
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CoreLocalSaveManager), "SetVector3")]
        public static void SetVector3(ref string inKey)
        {           
            // Check key
            if (inKey == "ChatWindowSize" || inKey == "ChatWindowPosition")
            {
                UI_Window_Chat wind = CoreWindowManager.GetWindow<UI_Window_Chat>();
                if (wind != null)
                {
                    ChatWindowVars_CCMMOD.Init();
                    ChatWindowVars_CCMMOD vars = wind.GetComponent<ChatWindowVars_CCMMOD>();
                    if (vars == null)
                        vars = wind.gameObject.AddComponent<ChatWindowVars_CCMMOD>();

                    if (vars._saveCustomizationPositionAndSize)
                    {
                        inKey = inKey + "_Customizer";
                    }
                }
            }
            else if (inKey == "VoiceChatWindowSize2" || inKey == "VoiceChatWindowPosition2")
            {
                UI_Window_VoiceChat wind = CoreWindowManager.GetWindow<UI_Window_VoiceChat>();
                if (wind != null)
                {
                    ChatWindowVars_CCMMOD.Init();
                    ChatWindowVars_CCMMOD vars = wind.GetComponent<ChatWindowVars_CCMMOD>();
                    if (vars == null)
                        vars = wind.gameObject.AddComponent<ChatWindowVars_CCMMOD>();

                    if (vars._saveCustomizationPositionAndSize)
                    {
                        inKey = inKey.Remove(inKey.LastIndexOf("2")) + "_Customizer2";
                    }
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CoreLocalSaveManager), "GetVector3", new Type[] { typeof(string), typeof(ObscuredVector3), typeof(bool) })]
        public static void GetVector3(ref string inKey)
        {      
            // Check key
            if (inKey == "ChatWindowSize" || inKey == "ChatWindowPosition")
            {
                UI_Window_Chat wind = CoreWindowManager.GetWindow<UI_Window_Chat>();
                if (wind != null)
                {
                    ChatWindowVars_CCMMOD.Init();
                    ChatWindowVars_CCMMOD vars = wind.GetComponent<ChatWindowVars_CCMMOD>();
                    if (vars == null)
                        vars = wind.gameObject.AddComponent<ChatWindowVars_CCMMOD>();

                    if (vars._saveCustomizationPositionAndSize)
                    {
                        inKey = inKey + "_Customizer";
                    }
                }
            }
            else if (inKey == "VoiceChatWindowSize2" || inKey == "VoiceChatWindowPosition2")
            {
                UI_Window_VoiceChat wind = CoreWindowManager.GetWindow<UI_Window_VoiceChat>();
                if (wind != null)
                {
                    ChatWindowVars_CCMMOD.Init();
                    ChatWindowVars_CCMMOD vars = wind.GetComponent<ChatWindowVars_CCMMOD>();
                    if (vars == null)
                        vars = wind.gameObject.AddComponent<ChatWindowVars_CCMMOD>();

                    if (vars._saveCustomizationPositionAndSize)
                    {
                        inKey = inKey.Remove(inKey.LastIndexOf("2")) + "_Customizer2";
                    }
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UI_Window_Chat), "PlayOpenAnimation")]
        public static bool OnAnim(ref UI_Window_Chat __instance)
        {
            ChatWindowVars_CCMMOD.Init();
            ChatWindowVars_CCMMOD vars = __instance.GetComponent<ChatWindowVars_CCMMOD>();
            if (vars == null)
                vars = __instance.gameObject.AddComponent<ChatWindowVars_CCMMOD>();
            if (vars._blockOpenAnim)
            {
                vars._blockOpenAnim = false;
                __instance.Show();
                __instance.OnOpenComplete();
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(FeralAudioEmitter), "Play", new Type[] { typeof(GameObject) })]
        public static bool PlayAudioA(ref FeralAudioEmitter __instance, GameObject inParent)
        {
            ChatWindowVars_CCMMOD.Init();
            ChatWindowVars_CCMMOD vars = __instance.GetComponent<ChatWindowVars_CCMMOD>();
            if (vars == null && __instance.gameObject.name == "Panel_Body")
                vars = __instance.transform.parent.parent.gameObject.GetComponent<ChatWindowVars_CCMMOD>();
            if (vars != null && vars._blockSfx)
            {
                vars._blockSfx = false;
                return false;
            }
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UI_Window_CreatureCustomization), "OnClose")]
        public static void OnClose(ref UI_Window_CreatureCustomization __instance)
        {
            // Restore chat to other position and restore previous chat state
            CustomizationChatController.Init();
            CustomizationChatController controller = __instance.gameObject.GetComponent<CustomizationChatController>();
            if (controller != null && controller.WasMenuOpen)
            {
                // Prevent dupe calls
                controller.WasMenuOpen = false;

                // Restore
                GameObject chatWin = GameObject.Find("CanvasRoot/WindowCanvas/UI_Window_Chat");
                GameObject vchatWin = GameObject.Find("CanvasRoot/WindowCanvas/UI_Window_VoiceChat");
                if (controller.WasChatAvailable && chatWin != null)
                {
                    // Restore close target
                    UI_Window_Chat chatController = chatWin.GetComponent<UI_Window_Chat>();
                    chatController.SaveWindowSize();
                    RectTransform chatCloseTarget = chatController._closeTarget.transform.Cast<RectTransform>();
                    chatCloseTarget.anchoredPosition = controller.OriginalHidePositionChat;
                    chatCloseTarget.anchorMax = controller.OriginalHidePositionChatAnchorMax;
                    chatCloseTarget.anchorMin = controller.OriginalHidePositionChatAnchorMin;

                    // Vars
                    ChatWindowVars_CCMMOD.Init();
                    ChatWindowVars_CCMMOD vars = chatController.GetComponent<ChatWindowVars_CCMMOD>();
                    if (vars == null)
                        vars = chatController.gameObject.AddComponent<ChatWindowVars_CCMMOD>();
                    vars._blockSavePositionAndSize = true;

                    // Restore visibility
                    if (controller.WasChatOriginallyOpen && !chatController.IsOpen)
                    {
                        // Restore state
                        vars._blockOpenAnim = true;
                        vars._blockSfx = true;
                        chatController.Open(false, false);
                    }
                    else if (!controller.WasChatOriginallyOpen && chatController.IsOpen)
                    {
                        // Restore state
                        vars._blockSfx = true;
                        chatController.Close(false, false);
                    }

                    // Restore position
                    RectTransform chatWinRect = chatController._body.transform.Cast<RectTransform>();
                    chatWinRect.anchoredPosition = controller.OriginalChatAnchoredPosition;
                    chatWinRect.anchorMax = controller.OriginalChatAnchorMax;
                    chatWinRect.anchorMin = controller.OriginalChatAnchorMin;
                    chatWinRect.pivot = controller.OriginalChatPivot;
                    chatController._body.gameObject.transform.localScale = new Vector3(1, 1, 1);
                    chatController._body.gameObject.transform.localPosition = controller.OriginalChatPosition;
                    chatController._body.gameObject.transform.Cast<RectTransform>().sizeDelta = controller.OriginalChatSize;
                    chatController.ClampWindowPosition();

                    // Bring behind window
                    __instance.transform.SetSiblingIndex(chatController.transform.GetSiblingIndex() + 1);
                    vars._saveCustomizationPositionAndSize = false;
                    vars._blockSavePositionAndSize = false;
                }
                if (controller.WasVoiceAvailable && vchatWin != null)
                {
                    // Restore close target
                    UI_Window_VoiceChat chatController = vchatWin.GetComponent<UI_Window_VoiceChat>();
                    chatController.SaveWindowSize();
                    RectTransform chatCloseTarget = chatController._closeTarget.transform.Cast<RectTransform>();
                    chatCloseTarget.anchoredPosition = controller.OriginalHidePositionVoice;
                    chatCloseTarget.anchorMax = controller.OriginalHidePositionVoiceAnchorMax;
                    chatCloseTarget.anchorMin = controller.OriginalHidePositionVoiceAnchorMin;

                    // Vars
                    ChatWindowVars_CCMMOD.Init();
                    ChatWindowVars_CCMMOD vars = chatController.GetComponent<ChatWindowVars_CCMMOD>();
                    if (vars == null)
                        vars = chatController.gameObject.AddComponent<ChatWindowVars_CCMMOD>();
                    vars._blockSavePositionAndSize = true;

                    // Restore visibility
                    if (controller.WasVoiceOriginallyOpen && !chatController.IsOpen)
                    {
                        // Restore state
                        vars._blockOpenAnim = true;
                        vars._blockSfx = true;
                        chatController.Open(false, false);
                    }
                    else if (!controller.WasVoiceOriginallyOpen && chatController.IsOpen)
                    {
                        // Restore state
                        vars._blockSfx = true;
                        chatController.Close(false, false);
                    }

                    // Restore position
                    RectTransform chatWinRect = chatController._body.transform.Cast<RectTransform>();
                    chatWinRect.anchoredPosition = controller.OriginalVoiceAnchoredPosition;
                    chatWinRect.anchorMax = controller.OriginalVoiceAnchorMax;
                    chatWinRect.anchorMin = controller.OriginalVoiceAnchorMin;
                    chatWinRect.pivot = controller.OriginalVoicePivot;
                    chatController._body.gameObject.transform.localScale = new Vector3(1, 1, 1);
                    chatController._body.gameObject.transform.localPosition = controller.OriginalVoicePosition;
                    chatController.ClampWindowPosition();

                    // Bring behind window
                    __instance.transform.SetSiblingIndex(chatController.transform.GetSiblingIndex() + 1);
                    vars._saveCustomizationPositionAndSize = false;
                    vars._blockSavePositionAndSize = false;
                }

                // Clean
                controller.WasChatAvailable = false;
                controller.WasVoiceAvailable = false;
                controller.WasChatOriginallyOpen = false;
                controller.WasVoiceOriginallyOpen = false;
                controller.ChatHasBeenOpened = false;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UI_Window_Chat), "OnOpen")]
        public static void OnChatOpen(ref UI_Window_Chat __instance)
        {
            ChatWindowVars_CCMMOD.Init();
            ChatWindowVars_CCMMOD vars = __instance.GetComponent<ChatWindowVars_CCMMOD>();
            if (vars == null)
                vars = __instance.gameObject.AddComponent<ChatWindowVars_CCMMOD>();
            if (vars._inited)
                return;
            vars._inited = true;

            // Cleaner chat close position
            RectTransform chatCloseTarget = __instance._closeTarget.transform.Cast<RectTransform>();
            chatCloseTarget.anchoredPosition = new Vector2(-75, 0);
            chatCloseTarget.anchorMax = new Vector2(0.5f, 0);
            chatCloseTarget.anchorMin = new Vector2(0.5f, 0);
            chatCloseTarget.pivot = new Vector2(0.5f, 0);

            // Re-setup if customization is open
            // Find customization menu
            UI_Window_CreatureCustomization menu = CoreWindowManager.GetWindow<UI_Window_CreatureCustomization>();
            if (menu != null)
            {
                CustomizationChatController.Init();
                CustomizationChatController controller = menu.gameObject.GetComponent<CustomizationChatController>();
                if (controller != null)
                {
                    // Setup
                    SetupChatWin(controller, __instance);
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UI_Window_VoiceChat), "OnOpen")]
        public static void OnVoiceChatOpen(ref UI_Window_VoiceChat __instance)
        {
            ChatWindowVars_CCMMOD.Init();
            ChatWindowVars_CCMMOD vars = __instance.GetComponent<ChatWindowVars_CCMMOD>();
            if (vars == null)
                vars = __instance.gameObject.AddComponent<ChatWindowVars_CCMMOD>();
            if (vars._inited)
                return;
            vars._inited = true;

            // Cleaner chat close position
            RectTransform chatCloseTarget = __instance._closeTarget.transform.Cast<RectTransform>();
            chatCloseTarget.anchoredPosition = new Vector2(75, 0);
            chatCloseTarget.anchorMax = new Vector2(0.5f, 0);
            chatCloseTarget.anchorMin = new Vector2(0.5f, 0);
            chatCloseTarget.pivot = new Vector2(0.5f, 0);

            // Re-setup if customization is open
            // Find customization menu
            UI_Window_CreatureCustomization menu = CoreWindowManager.GetWindow<UI_Window_CreatureCustomization>();
            if (menu != null)
            {
                CustomizationChatController.Init();
                CustomizationChatController controller = menu.gameObject.GetComponent<CustomizationChatController>();
                if (controller != null)
                {
                    // Setup
                    SetupVoiceChatWin(controller, __instance);
                }
            }
        }

        private static void SetupChatWin(CustomizationChatController controller, UI_Window_Chat chatController)
        {
            ChatWindowVars_CCMMOD.Init();
            ChatWindowVars_CCMMOD vars = chatController.GetComponent<ChatWindowVars_CCMMOD>();
            if (vars == null)
                vars = chatController.gameObject.AddComponent<ChatWindowVars_CCMMOD>();

            // Back up settings
            vars._blockSavePositionAndSize = true;
            controller.OriginalHidePositionChat = chatController._closeTarget.transform.Cast<RectTransform>().anchoredPosition;
            controller.OriginalHidePositionChatAnchorMin = chatController._closeTarget.transform.Cast<RectTransform>().anchorMin;
            controller.OriginalHidePositionChatAnchorMax = chatController._closeTarget.transform.Cast<RectTransform>().anchorMax;
            controller.OriginalChatSize = chatController._body.transform.Cast<RectTransform>().sizeDelta;
            controller.OriginalChatPivot = chatController._body.transform.Cast<RectTransform>().pivot;
            controller.OriginalChatAnchoredPosition = chatController._body.transform.Cast<RectTransform>().anchoredPosition;
            controller.OriginalChatAnchorMax = chatController._body.transform.Cast<RectTransform>().anchorMax;
            controller.OriginalChatAnchorMin = chatController._body.transform.Cast<RectTransform>().anchorMin;
            controller.OriginalChatPosition = chatController._body.transform.localPosition;
            controller.WasChatAvailable = true;

            // Reconfigure close position
            RectTransform chatCloseTarget = chatController._closeTarget.transform.Cast<RectTransform>();
            chatCloseTarget.anchoredPosition = new Vector2(-60, 80);
            chatCloseTarget.anchorMax = new Vector2(1, 0);
            chatCloseTarget.anchorMin = new Vector2(1, 0);

            // Save current window state
            controller.WasChatOriginallyOpen = chatController.IsOpen;

            // Hide
            if (controller.WasChatOriginallyOpen)
            {
                // Hide window
                vars._blockSfx = true;
                chatController.Close(false, false);
            }

            // Snap chat to different coordinate roots
            RectTransform chatWinRect = chatController._body.transform.Cast<RectTransform>();
            chatWinRect.anchorMax = new Vector2(1, 0.5f);
            chatWinRect.anchorMin = new Vector2(1, 0.5f);
            chatWinRect.pivot = new Vector2(1, 0.5f);
            chatWinRect.sizeDelta = new Vector2(300, 500);
            chatWinRect.localPosition = new Vector2(0, 0);
            chatWinRect.anchoredPosition = new Vector2(-30, 0);
            chatController.ClampWindowPosition();
            vars._saveCustomizationPositionAndSize = true;
            vars._blockSavePositionAndSize = false;

            // If needed, save
            if (!CoreLocalSaveManager.instance.HasKey("ChatWindowPosition_Customizer", true))
                chatController.SaveWindowSize();

            // Load
            Vector3 vn1 = new Vector3(0, 0);
            Vector3 vn2 = new Vector3(0, 0);
            chatController.LoadWindowSize(true, out vn1, out vn2);
            chatController.ClampWindowPosition();
        }

        private static void SetupVoiceChatWin(CustomizationChatController controller, UI_Window_VoiceChat chatController)
        {
            ChatWindowVars_CCMMOD.Init();
            ChatWindowVars_CCMMOD vars = chatController.GetComponent<ChatWindowVars_CCMMOD>();
            if (vars == null)
                vars = chatController.gameObject.AddComponent<ChatWindowVars_CCMMOD>();
                
            // Back up settings
            controller.OriginalHidePositionVoice = chatController._closeTarget.transform.Cast<RectTransform>().anchoredPosition;
            controller.OriginalHidePositionVoiceAnchorMin = chatController._closeTarget.transform.Cast<RectTransform>().anchorMin;
            controller.OriginalHidePositionVoiceAnchorMax = chatController._closeTarget.transform.Cast<RectTransform>().anchorMax;
            controller.OriginalVoicePosition = chatController._body.transform.localPosition;
            controller.OriginalVoicePivot = chatController._body.transform.Cast<RectTransform>().pivot;
            controller.OriginalVoiceAnchoredPosition = chatController._body.transform.Cast<RectTransform>().anchoredPosition;
            controller.OriginalVoiceAnchorMax = chatController._body.transform.Cast<RectTransform>().anchorMax;
            controller.OriginalVoiceAnchorMin = chatController._body.transform.Cast<RectTransform>().anchorMin;
            controller.WasVoiceAvailable = true;

            // Reconfigure close position
            RectTransform chatCloseTarget = chatController._closeTarget.transform.Cast<RectTransform>();
            chatCloseTarget.anchoredPosition = new Vector2(-60, 20);
            chatCloseTarget.anchorMax = new Vector2(1, 0);
            chatCloseTarget.anchorMin = new Vector2(1, 0);

            // Save current window state
            controller.WasVoiceOriginallyOpen = chatController.IsOpen;

            // Hide
            if (controller.WasVoiceOriginallyOpen)
            {
                // Hide window
                vars._blockSfx = true;
                chatController.Close(false, false);
            }

            // Snap chat to different coordinate roots
            RectTransform chatWinRect = chatController._body.transform.Cast<RectTransform>();
            chatWinRect.anchorMax = new Vector2(1, 0.5f);
            chatWinRect.anchorMin = new Vector2(1, 0.5f);
            chatWinRect.pivot = new Vector2(1, 0.5f);
            chatWinRect.localPosition = new Vector2(0, 0);
            chatWinRect.anchoredPosition = new Vector2(-30, 0);
            chatController.ClampWindowPosition();
            vars._saveCustomizationPositionAndSize = true;
            vars._blockSavePositionAndSize = false;

            // If needed, save
            if (!CoreLocalSaveManager.instance.HasKey("VoiceChatWindowPosition_Customizer2", true))
                chatController.SaveWindowSize();

            // Load
            Vector3 vn1 = new Vector3(0, 0);
            Vector3 vn2 = new Vector3(0, 0);
            chatController.LoadWindowSize(true, out vn1, out vn2);
            chatController.ClampWindowPosition();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UI_Window_CreatureCustomization), "OnOpen")]
        public static void OnShow(ref UI_Window_CreatureCustomization __instance)
        {
            // Init
            CustomizationChatController.Init();
            CustomizationChatController controller = __instance.gameObject.GetComponent<CustomizationChatController>();
            if (controller == null)
                controller = __instance.gameObject.AddComponent<CustomizationChatController>();
            if (!controller.WasMenuOpen)
            {
                // Mark open
                controller.WasMenuOpen = true;

                // Setup window settings
                GameObject chatWin = GameObject.Find("CanvasRoot/WindowCanvas/UI_Window_Chat");
                if (chatWin != null)
                {
                    UI_Window_Chat chatController = chatWin.GetComponent<UI_Window_Chat>();
                    SetupChatWin(controller, chatController);
                }
                GameObject vchatWin = GameObject.Find("CanvasRoot/WindowCanvas/UI_Window_VoiceChat");
                if (vchatWin != null)
                {
                    UI_Window_VoiceChat chatController = vchatWin.GetComponent<UI_Window_VoiceChat>();
                    SetupVoiceChatWin(controller, chatController);
                }

                // Bring windows to front
                GameObject chatWindow = GameObject.Find("CanvasRoot/WindowCanvas/UI_Window_Chat");
                if (chatWindow != null)
                {
                    // Bring to front
                    UI_Window_Chat chatController = chatWindow.GetComponent<UI_Window_Chat>();
                    chatController.Hide();
                    Transform canvas = chatWindow.transform.parent;
                    chatWindow.transform.SetParent(null);
                    chatWindow.transform.SetParent(canvas);
                    chatController.Show();
                }
                GameObject vchatWindow = GameObject.Find("CanvasRoot/WindowCanvas/UI_Window_VoiceChat");
                if (vchatWindow != null)
                {
                    // Bring to front
                    UI_Window_VoiceChat vchatController = vchatWindow.GetComponent<UI_Window_VoiceChat>();
                    vchatController.Hide();
                    Transform canvas = vchatWindow.transform.parent;
                    vchatWindow.transform.SetParent(null);
                    vchatWindow.transform.SetParent(canvas);
                    vchatController.Show();
                }

                // Create new buttons
                GameObject safeArea = GetChild(__instance.gameObject, "Body_SafeArea");
                GameObject chatButton = GameObject.Find("CanvasRoot/WindowCanvas/UI_Window_HUD/Group_HUD/Body/StandaloneOnly/Button_Chat");
                if (chatButton != null)
                {
                    // Create new
                    GameObject copy = GameObject.Instantiate(chatButton);
                    copy.name = "Button_Chat";
                    copy.transform.SetParent(safeArea.transform);
                    copy.transform.localPosition = chatButton.transform.localPosition;
                    copy.transform.localScale = chatButton.transform.localScale;
                    copy.transform.localRotation = chatButton.transform.localRotation;
                    RectTransform transf = copy.transform.Cast<RectTransform>();
                    transf.anchoredPosition = new Vector2(-60, 80);
                    transf.anchorMax = new Vector2(1, 0);
                    transf.anchorMin = new Vector2(1, 0);
                    copy.SetActive(true);
                    GetChild(copy, "ButtonGraphics/NotificationCount").SetActive(true);
                    copy.GetComponent<FeralButton>().onClick.AddListener(new Action(() =>
                    {
                        GameObject chatWindowz = GameObject.Find("CanvasRoot/WindowCanvas/UI_Window_Chat");
                        if (chatWindowz != null)
                        {
                            UI_Window_Chat chatController = chatWindowz.GetComponent<UI_Window_Chat>();
                            chatController.Show();
                            if (!controller.ChatHasBeenOpened)
                            {
                                chatController._publicChatPanel.SnapToBottom(true);
                                chatController._privateChatPanel.SnapToBottom(true);
                                controller.ChatHasBeenOpened = true;
                            }
                            controller.ChatHasBeenOpened = true;
                        }
                        else
                        {
                            FeralTweaksActionManager.ScheduleDelayedActionForUnity(() =>
                            {
                                GameObject chatWindowz = GameObject.Find("CanvasRoot/WindowCanvas/UI_Window_Chat");
                                if (chatWindowz != null)
                                {
                                    UI_Window_Chat chatController = chatWindowz.GetComponent<UI_Window_Chat>();
                                    if (!controller.ChatHasBeenOpened)
                                    {
                                        chatController._publicChatPanel.SnapToBottom(true);
                                        chatController._privateChatPanel.SnapToBottom(true);
                                        controller.ChatHasBeenOpened = true;
                                    }
                                    chatController.Show();
                                    return true;
                                }
                                return false;
                            });
                        }
                    }));
                }
                GameObject vchatButton = GameObject.Find("CanvasRoot/WindowCanvas/UI_Window_HUD/Group_HUD/Body/StandaloneOnly/Button_Voice");
                if (vchatButton != null)
                {
                    // Create new
                    GameObject copy = GameObject.Instantiate(vchatButton);
                    copy.name = "Button_Voice";
                    copy.transform.SetParent(safeArea.transform);
                    copy.transform.localPosition = vchatButton.transform.localPosition;
                    copy.transform.localScale = vchatButton.transform.localScale;
                    copy.transform.localRotation = vchatButton.transform.localRotation;
                    RectTransform transf = copy.transform.Cast<RectTransform>();
                    transf.anchoredPosition = new Vector2(-60, 20);
                    transf.anchorMax = new Vector2(1, 0);
                    transf.anchorMin = new Vector2(1, 0);
                    copy.SetActive(true);
                }
            }
        }

        private static GameObject GetChild(GameObject parent, string name)
        {
            if (name.Contains("/"))
            {
                string pth = name.Remove(name.IndexOf("/"));
                string ch = name.Substring(name.IndexOf("/") + 1);
                foreach (GameObject obj in GetChildren(parent))
                {
                    if (obj.name == pth)
                    {
                        GameObject t = GetChild(obj, ch);
                        if (t != null)
                            return t;
                    }
                }
                return null;
            }
            Transform tr = parent.transform;
            Transform[] trs = parent.GetComponentsInChildren<Transform>(true);
            foreach (Transform t in trs)
            {
                if (t.name == name && t.parent == tr.gameObject.transform)
                {
                    return t.gameObject;
                }
            }
            return null;
        }

        private static GameObject[] GetChildren(GameObject parent)
        {
            Transform tr = parent.transform;
            List<GameObject> children = new List<GameObject>();
            Transform[] trs = parent.GetComponentsInChildren<Transform>(true);
            foreach (Transform trCh in trs)
            {
                if (trCh.parent == tr.gameObject.transform)
                    children.Add(trCh.gameObject);
            }
            return children.ToArray();
        }
    }
}