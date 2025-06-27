using System;
using System.Collections.Generic;
using CustomizationChat.Actions;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CustomizationChat.Patches
{
    public class OpenCreatureMenuHook
    {
        public class EditedButtonIniter  : MonoBehaviour
        {
            private static bool injected;

            public static void Init()
            {
                if (!injected)
                    ClassInjector.RegisterTypeInIl2Cpp<EditedButtonIniter>();
                injected = true;
            }

            public EditedButtonIniter() : base() { }
            public EditedButtonIniter(IntPtr ptr) : base(ptr) { }

            public bool VisibleState;
            private bool _done;
            public void Update()
            {
                if (_done)
                    return;
                _done = true;
                gameObject.SetActive(VisibleState);
            }
        }

        public class ChatButtonRunner  : MonoBehaviour
        {
            private static bool injected;

            public static void Init()
            {
                if (!injected)
                    ClassInjector.RegisterTypeInIl2Cpp<ChatButtonRunner>();
                injected = true;
            }

            public ChatButtonRunner() : base() { }
            public ChatButtonRunner(IntPtr ptr) : base(ptr) { }

            public Action onUpdate;
            public void Update()
            {
                onUpdate();
            }
        }

		[HarmonyPostfix]
		[HarmonyPatch(typeof(UI_Window_CreatureCustomization), "OnOpen")]
		public static void OnShow(ref UI_Window_CreatureCustomization __instance) {
            EditedButtonIniter.Init();
            ChatButtonRunner.Init();
            GameObject chatWindow = GameObject.Find("CanvasRoot/WindowCanvas/UI_Window_Chat");
			if (chatWindow != null) {
				// Bring to front
				UI_Window_Chat chatController = chatWindow.GetComponent<UI_Window_Chat>();
				chatController.Hide();
				Transform canvas = chatWindow.transform.parent;
				chatWindow.transform.SetParent(null);
				chatWindow.transform.SetParent(canvas);
				chatController.Show();
			}
			GameObject vchatWindow = GameObject.Find("CanvasRoot/WindowCanvas/UI_Window_VoiceChat");
			if (vchatWindow != null) {
				// Bring to front
				UI_Window_VoiceChat vchatController = vchatWindow.GetComponent<UI_Window_VoiceChat>();
				vchatController.Hide();
				Transform canvas = vchatWindow.transform.parent;
				vchatWindow.transform.SetParent(null);
				vchatWindow.transform.SetParent(canvas);
				vchatController.Show();
			}
            GameObject safeArea = GetChild(__instance.gameObject, "Body_SafeArea");
			GameObject chatButton = GameObject.Find("CanvasRoot/WindowCanvas/UI_Window_HUD/Group_HUD/Body/StandaloneOnly/Button_Chat");
			if (chatButton != null) {
				// Create new
				GameObject copy = GameObject.Instantiate(chatButton);
                copy.name = "Button_Chat";
                copy.transform.SetParent(safeArea.transform);
				copy.transform.localPosition = chatButton.transform.localPosition;
				copy.transform.localScale = chatButton.transform.localScale;
				copy.transform.localRotation = chatButton.transform.localRotation;
				bool wasActive = copy.activeSelf;
                copy.SetActive(true);
                GetChild(copy, "ButtonGraphics/NotificationCount").SetActive(true);
                copy.AddComponent<ChatButtonRunner>().onUpdate = () =>
                {
                    RectTransform transf = copy.transform.Cast<RectTransform>();
                    transf.anchoredPosition = new Vector2(-60, 20);
                    transf.anchorMax = new Vector2(1, 0);
                    transf.anchorMin = new Vector2(1, 0);
                };
                copy.AddComponent<EditedButtonIniter>().VisibleState = wasActive;
                copy.GetComponent<FeralButton>().onClick.AddListener(new Action(() =>
                {
                    GameObject chatWindowz = GameObject.Find("CanvasRoot/WindowCanvas/UI_Window_Chat");
                    if (chatWindowz != null)
                    {
                        UI_Window_Chat chatController = chatWindowz.GetComponent<UI_Window_Chat>();
                        chatController.Show();
                    }
                    else
                    {
                        FeralTweaksActionManager.ScheduleDelayedActionForUnity(() =>
                        {
                            GameObject chatWindowz = GameObject.Find("CanvasRoot/WindowCanvas/UI_Window_Chat");
                            if (chatWindowz != null)
                            {
                                UI_Window_Chat chatController = chatWindowz.GetComponent<UI_Window_Chat>();
                                chatController.Show();
                                return true;    
                            }
                            return false;
                        });
                    }
                }));
            }
			GameObject vchatButton = GameObject.Find("CanvasRoot/WindowCanvas/UI_Window_HUD/Group_HUD/Body/StandaloneOnly/Button_Voice");
			if (vchatButton != null) {
				// Create new
				GameObject copy = GameObject.Instantiate(vchatButton);
                copy.name = "Button_Voice";
                copy.transform.SetParent(safeArea.transform);
				copy.transform.localPosition = vchatButton.transform.localPosition;
				copy.transform.localScale = vchatButton.transform.localScale;
				copy.transform.localRotation = vchatButton.transform.localRotation;
				bool wasActive = copy.activeSelf;
                copy.SetActive(true);
                copy.AddComponent<ChatButtonRunner>().onUpdate = () =>
                {
                    RectTransform transf = copy.transform.Cast<RectTransform>();
                    transf.anchoredPosition = new Vector2(-60, 80);
                    transf.anchorMax = new Vector2(1, 0);
                    transf.anchorMin = new Vector2(1, 0);
                };
                copy.AddComponent<EditedButtonIniter>().VisibleState = wasActive;
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
