using BattleTech;
using ClintonMead;
using Harmony;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;

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

        public HarmonyManager(Assembly assembly, string settingsJSON, string harmonyUniqId = null, bool debugDefault = true)
        {
            try
            {
                HarmonyUniqId = harmonyUniqId ?? assembly.FullName;
                Settings = JsonConvert.DeserializeObject<TSettings>(settingsJSON);

                IDebug iDebug = Settings as IDebug;
                DebugOn = iDebug == null || iDebug.DebugOn;

                DebugLog("Attempting patch for Battletech mod");
                HarmonyInstance.PatchAll(assembly);
                DebugLog("Successfully patched");
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

        public void LogExceptions(Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                Log(e.ToString());
            }
        }

        public bool LogExceptions(Func<bool> func)
        {
            try
            {
                return func();
            }
            catch (Exception e)
            {
                Log(e.ToString());
                return true;
            }
        }
    }

    public static class HarmonyManager
    {
        public static readonly HarmonyInstance HarmonyInstance;

        static HarmonyManager()
        {
            HarmonyInstance = HarmonyInstance.Create("BattletechModUtilitiesHarmonyInstance");
            HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}
