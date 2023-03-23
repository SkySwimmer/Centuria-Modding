using System;
using FeralTweaks;
using FeralTweaks.Mods;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using FeralTweaks.Networking;
using Server;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;
using System.Threading.Tasks;
using System.Threading;

namespace TestFtlMod
{
    public class TestMod : NetworkedFeralTweaksMod
    {
        public override void Init()
        {
            Harmony.CreateAndPatchAll(typeof(TestPatches));
            RegisterPacket(new TestPacket());
        }

        public override void PostInit()
        {
            // Register classes
            ClassInjector.RegisterTypeInIl2Cpp<TestObject>();

            // Create object for the mod
            GameObject obj = new GameObject();
            GameObject.DontDestroyOnLoad(obj);
            obj.name = "TestMod";
            obj.AddComponent<TestObject>();
        }
    }

    public class TestObject : UnityEngine.MonoBehaviour
    {
        public TestObject() { }
        public TestObject(IntPtr ptr) : base(ptr) { }

        public void Awake()
        {
            // Called on start

            // Attach listener
            CoreMessageManager.RegisteredListener listener =
                new CoreMessageManager.RegisteredListener(CoreMessageManager.GetTypeName(Il2CppInterop.Runtime.Il2CppType.From(typeof(LoginCompleteMessage))), "",
                    GetIl2CppType().GetMethod("HandleLogin"), this, "", "", -1);
            CoreMessageManager.AddStaticRegisteredListener(listener);
        }

        public void HandleLogin(LoginCompleteMessage msg)
        {
            // Send packet
            TestPacket test = new TestPacket();
            test.payload = "Hello World";
            FeralTweaksLoader.GetLoadedMod<TestMod>().GetMessenger().SendPacket(test);
        }
    }

    public class TestPacket : IModNetworkPacket
    {
        public string ID => "test";
        public string payload;

        public IModNetworkPacket CreateInstance()
        {
            return new TestPacket();
        }

        public void Parse(INetMessageReader reader)
        {
            payload = reader.ReadString();
        }

        public void Write(INetMessageWriter writer)
        {
            writer.WriteString(payload);
        }

        public bool Handle()
        {
            return true;
        }
    }

    public static class TestPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(DownloadingBundle), "StartDownload")]
        public static void StartDownloadPatch(DownloadingBundle __instance)
        {
            __instance = __instance;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ManifestDef), "LoadEntry")]
        public static void LoadEntryPatch(ManifestDef __instance)
        {
            if (__instance.defID == "win32_actors_avatars_fox_bodyparts_ears_ears000_default_texture")
                __instance = __instance;
        }
    }
}
