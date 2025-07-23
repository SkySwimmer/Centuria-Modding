using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HarmonyLib;
using HarmonyLib.Public.Patching;
using Il2CppInterop.Runtime;
using MonoMod.Utils;

namespace FeralTweaksBootstrap.Patches
{
    public static class HarmonySupportPatch
    {
        private static Dictionary<string, Il2CppSystem.Type> KnownTypes = new Dictionary<string, Il2CppSystem.Type>();

        [HarmonyPostfix]
        [HarmonyPatch("Il2CppInterop.HarmonySupport.Il2CppDetourMethodPatcher", "GenerateNativeToManagedTrampoline")]
        private static void GenerateNativeToManagedTrampolineHook(MethodPatcher __instance, ref DynamicMethodDefinition __result, MethodInfo targetManagedMethodInfo)
        {
            if (__instance.Original.ReflectedType.IsGenericType && !__instance.Original.IsStatic)
            {
                // Check stack, dont recurse please
                // YES VERY UGLY IM AWARE
                // Ive run out of ideas is the issue
                StackTrace stack = new StackTrace();
                if (stack.GetFrames().Count(t => t.HasMethod() && t.GetMethod().ReflectedType != null && t.GetMethod().ReflectedType.IsAssignableTo(typeof(HarmonySupportPatch)) && t.GetMethod().Name == "GenerateNativeToManagedTrampolineHook") > 1)
                    return;

                // Fix issues with generic types by implementing a workaround

                // The issue is caused by how il2cpp has one shared method for
                // eg. CoreBase<SplashCore>.InitializeManagers and CoreBase<Core>.InitializeManagers,
                // harmony expects two separate ones right now, so when patchers are applied,
                // they end up applied to both methods, so when CoreBase<Core>.InitializeManagers is called,
                // both hooks would end up being executed
                //
                // A trycast wont resolve the issues caused by the underlying unboxing causing the runtime
                // to confuse the two generic types, which results in the game crashing with a cryptic error
                // where in reality, the runtime tried to pass SplashCore where it should have been Core
                // 
                // To resolve, we patch the bug by adding a check that compares the generic types    
                // FIXME: might not properly deal with inherited generic types, this needs analysis  
                // FIXME: cannot deal with cases where the method being patched is static          

                // Check type
                Il2CppSystem.Type type = Il2CppType.From(__instance.Original.ReflectedType);
                if (type == null)
                    return;
                if (!KnownTypes.ContainsKey(type.FullName))
                {
                    lock (KnownTypes)
                    {
                        KnownTypes[type.FullName] = type;
                    }
                }

                // Alter detour to ignore the wrong generic type
                int selfIndex = ((MethodInfo)__instance.Original).ReturnType.IsAssignableTo(typeof(Il2CppSystem.ValueType)) ? 1 : 0;

                // Generate detour method
                Mono.Cecil.Cil.Instruction[] insns = __result.Definition.Body.Instructions.ToArray();
                MethodInfo detourToHarmony = __result.Generate();

                // Generate
                TypeInfo t = (TypeInfo) __instance.GetType();
                MethodInfo mthP = t.DeclaredMethods.Where(t => t.Name == "GenerateNativeToManagedTrampoline").First();
                DynamicMethodDefinition origModified = __instance.CopyOriginal();
                origModified.Definition.Name = "UNPATCHEDFERALTWEAKS_" + origModified.Definition.Name;
                MethodInfo modInfo = origModified.Generate();
                DynamicMethodDefinition resT = (DynamicMethodDefinition) mthP.Invoke(__instance, new object[] { modInfo }); // FIXME: this does NOT work, i should just run the trampoline directly somehow
                MethodInfo detourToOrig = resT.Generate();

                // Create new DMD
                DynamicMethodDefinition mth = new DynamicMethodDefinition("(il2cpp -> managed)(prepatch) " + __instance.Original.Name, detourToHarmony.ReturnType, detourToHarmony.GetParameters().Select(t => t.ParameterType).ToArray());

                // Emit new IL
                // 
                // First we create the exit label that runs to return the method
                // 
                // Next up is the check to compare the types
                // First add a load argument for the self index
                // Then add the type name of the intended type
                // Then call comparison and go to exit if needed
                //
                // After that add a call to the original method
                // For this, first we load up all arguments
                // Then we call the method
                // And return if needed
                //
                // Then add the return code for failures
                // Simply invoke the alternate detour
                // 
                ILGenerator gen = mth.GetILGenerator();
                Label exitFallbackLbl = gen.DefineLabel(); // Define label for exit (doesnt enter it)
                gen.Emit(OpCodes.Ldarg, selfIndex); // Self index
                gen.Emit(OpCodes.Ldstr, type.FullName); // Intended class full name
                gen.Emit(OpCodes.Call, typeof(HarmonySupportPatch).GetMethod("__HandleObjectTypeCheck")); // Call type check
                gen.Emit(OpCodes.Brfalse, exitFallbackLbl); // If the method returns false, go to exit
                for (int i = 0; i < mth.Definition.Parameters.Count; i++)
                    gen.Emit(OpCodes.Ldarg, i); // Emit argument loads
                gen.Emit(OpCodes.Call, detourToHarmony); // Emit call to detour
                gen.Emit(OpCodes.Ret); // Emit return
                gen.MarkLabel(exitFallbackLbl); // Start fallback exit
                for (int i = 0; i < mth.Definition.Parameters.Count; i++)
                    gen.Emit(OpCodes.Ldarg, i); // Emit argument loads
                gen.Emit(OpCodes.Call, detourToOrig); // Emit call to detour
                gen.Emit(OpCodes.Ret); // Emit return

                // Done
                __result = mth;
            }
        }

        public static bool __HandleObjectTypeCheck(IntPtr obj, string intendedFullName)
        {
            // Convert to IL2CPP type
            if (!KnownTypes.ContainsKey(intendedFullName))
                return true; // Ignore
            Il2CppSystem.Type type = Il2CppType.TypeFromPointer(IL2CPP.il2cpp_object_get_class(obj));
            Il2CppSystem.Type typeIntended = KnownTypes[intendedFullName];

            // Check type
            if (!typeIntended.IsAssignableFrom(type))
            {
                // Incompatible
                return false;
            }

            // Return OK
            return true;
        }
    }
}