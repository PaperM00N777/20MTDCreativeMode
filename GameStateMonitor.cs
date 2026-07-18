using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace StateLogger // update in class 1, rather than being a separate plugin?
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class GameStateMonitor : BaseUnityPlugin
    {
        private const string PluginGuid = "lucas.statelogger";
        private const string PluginName = "State Logger";
        private const string PluginVersion = "2.0.0";

        internal static ManualLogSource Log;

        public static State CurrentState;

        private Harmony harmony;

        private void Awake()
        {
            Log = base.Logger;
            harmony = new Harmony(PluginGuid);

            PatchAllStateSubclasses();

        }

        private void PatchAllStateSubclasses()
        {
            var stateType = typeof(State);
            var patchedMethods = new HashSet<MethodInfo>();

            var enterPostfix = new HarmonyMethod(
                typeof(GameStateMonitor).GetMethod(nameof(EnterPostfix), BindingFlags.Static | BindingFlags.NonPublic));
            var exitPostfix = new HarmonyMethod(
                typeof(GameStateMonitor).GetMethod(nameof(ExitPostfix), BindingFlags.Static | BindingFlags.NonPublic));

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    types = e.Types;
                }
                if (types == null)
                    continue;

                foreach (var type in types)
                {
                    if (type == null || type.IsAbstract || !stateType.IsAssignableFrom(type))
                        continue;

                    TryPatchMethod(type, "Enter", enterPostfix, patchedMethods);
                    TryPatchMethod(type, "Exit", exitPostfix, patchedMethods);
                }
            }

            
        }

        private void TryPatchMethod(Type type, string methodName, HarmonyMethod postfix, HashSet<MethodInfo> patched)
        {
            MethodInfo method;
            try
            {
                method = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            }
            catch
            {
                return;
            }

            if (method == null || method.IsAbstract || patched.Contains(method))
                return;

            try
            {
                harmony.Patch(method, postfix: postfix);
                patched.Add(method);
            }
            catch (Exception e)
            {
                Log.LogWarning($"Failed to patch {type.FullName}.{methodName}: {e.Message}");
            }
        }

        private static void EnterPostfix(State __instance)
        {
            CurrentState = __instance;
        }

        private static void ExitPostfix(State __instance)
        {
            if (CurrentState == __instance)
                CurrentState = null;

        }
    }
}