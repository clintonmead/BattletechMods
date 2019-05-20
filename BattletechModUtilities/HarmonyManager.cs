using BattleTech;
using Harmony;
using Newtonsoft.Json;
using Optional;
using System;
using System.Collections.Generic;
using System.Reflection;
using Optional.Unsafe;

namespace BattletechModUtilities
{
    public class HarmonyManager<TSettings>
    {
        public readonly TSettings Settings;
        public readonly bool DebugOn;
        public readonly string HarmonyUniqId;

        private HarmonyInstance HarmonyInstance
        {
            get { return HarmonyManager.HarmonyInstance; }
        }

        public HarmonyManager(Assembly assembly, string settingsJSON, string harmonyUniqId = null, bool debugDefault = true, bool explicitPatching = false)
        {
            try
            {
                HarmonyUniqId = harmonyUniqId ?? assembly.FullName;
                Settings = JsonConvert.DeserializeObject<TSettings>(settingsJSON);

                IDebug iDebug = Settings as IDebug;
                DebugOn = iDebug == null || iDebug.DebugOn;

                if (! explicitPatching)
                {
                    DebugLog("Attempting patch for Battletech mod");
                    HarmonyInstance.PatchAll(assembly);
                    DebugLog("Successfully patched");
                }
                else
                {
                    DebugLog("No automatic patching");
                }
                if (DebugOn)
                {
                    Log("List of patched methods: ");
                    IEnumerable<MethodBase> methods = HarmonyInstance.GetPatchedMethods();
                    foreach (MethodBase method in methods)
                    {
                        Log(method.Name);
                    }
                    Log("End of list of patched methods and initialisation.");
                }
            }
            catch (Exception e)
            {
                Log("Battletech mod threw exception during initialisation:");
                Log(e.ToString());
                throw;
            }
        }

        public void Patch(MethodInfo method, MethodInfo prefix = null, MethodInfo postfix = null)
        {
            HarmonyInstance.Patch(method, prefix.MapNull(x => new HarmonyMethod(x)), postfix.MapNull(x => new HarmonyMethod(x)));
        }

        public void DebugLog(string s)
        {
            if (DebugOn)
            {
                Log(s);
            }
        }
        public void Log(string s)
        {
            FileLog.Log(HarmonyUniqId + " - " + DateTime.Now + ": " + s);
        }

        public void Log(Exception e)
        {
            Log("\nMessage: " + e.Message + "\nSource: " + e.Source + "\nStacktrace:\n" + e.StackTrace);
        }

        public void LogExceptions(Action action)
        {
            PrefixLogExceptions(action);
        }

        public bool PrefixLogExceptions(Action action)
        {
            try
            {
                action();
                return false;
            }
            catch (Exception e)
            {
                Log(e);
                return true;
            }
        }

        public bool PrefixLogExceptions(Func<bool> func)
        {
            try
            {
                return func();
            }
            catch (Exception e)
            {
                Log(e);
                return true;
            }
        }

        public bool PrefixPatchOptionally<T>(ref T result, Func<Option<T>> func)
        {
            try
            {
                Option<T> funcResult = func();
                if (funcResult.HasValue)
                {
                    // If we get a value, immediately return it.
                    result = funcResult.ValueOrDefault();
                    return false;
                }
                else
                {
                    // Otherwise call the original function.
                    return true;
                }
            }
            catch (Exception e)
            {
                Log(e);
                return true;
            }
        }

        public bool PrefixPatch<T>(ref T result, Func<T> func)
        {
            return PrefixPatchOptionally(ref result, () => func().Some());
        }

        public void PostfixPatch<T>(ref T result, Func<T, T> func)
        {
            try
            {
                result = func(result);
            }
            catch (Exception e)
            {
                Log(e);
            }
        }
    }

    public static class HarmonyManager
    {
        public static readonly HarmonyInstance HarmonyInstance;

        // Modtek just needs and Init to call by default on startup,
        // but the static constructor does the work here anyway.
        public static void Init(string directory, string settingsJSON)
        {
        }

        static HarmonyManager()
        {
            HarmonyInstance = HarmonyInstance.Create("BattletechModUtilitiesHarmonyInstance");
        }
    }
}
