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

        public HarmonyManager(Assembly assembly, string settingsJSON, string harmonyUniqId = null, bool debugDefault = true)
        {
            try
            {
                Settings = JsonConvert.DeserializeObject<TSettings>(settingsJSON);

                IDebug iDebug = Settings as IDebug;
                DebugOn = iDebug == null || iDebug.DebugOn;

                if (harmonyUniqId == null)
                {
                    harmonyUniqId = assembly.FullName;
                }

                DebugLog("Attempting patch for Battletech mod " + harmonyUniqId);
                HarmonyInstance harmony = HarmonyInstance.Create(harmonyUniqId);
                harmony.PatchAll(assembly);
                DebugLog("Successfully patched mod " + harmonyUniqId);
                if (DebugOn)
                {
                    Log("List of patched methods: ");
                    IEnumerable<MethodBase> methods = harmony.GetPatchedMethods();
                    foreach (MethodBase method in methods)
                    {
                        Log(method.Name);
                    }
                    Log("End of list of patched methods and initialisation.");
                }
            }
            catch (Exception e)
            {
                Log("Battletech mod " + harmonyUniqId + " threw exception during initialisation:");
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
            FileLog.Log(DateTime.Now + ": " + s);
        }
    }
}
