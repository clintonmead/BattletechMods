using BattleTech;
using BattletechModUtilities;
using Harmony;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace FixContractRefreshRate
{
    public static class FixContractRefreshRate
    {
        private static HarmonyManager<Settings> HarmonyManager;

        public static void Init(string directory, string settingsJSON)
        {
            HarmonyManager = new HarmonyManager<Settings>(
                assembly: Assembly.GetExecutingAssembly(),
                settingsJSON: settingsJSON,
                harmonyUniqId: "io.github.clintonmead.ContractTime");
        }

        public struct Settings : IDebug
        {
            [DefaultValue(true)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
            public readonly bool Debug;

            [JsonConstructor]
            public Settings(bool debug)
            {
                Debug = debug;
            }

            public bool DebugOn
            {
                get
                {
                    return Debug;
                }
            }
        }

        [HarmonyPatch(typeof(StarSystem))]
        [HarmonyPatch("UpdateSystemDay")]
        public static class PatchUpdateSystemDay
        {
            public static bool Prefix(StarSystem __instance)
            {
                return HarmonyManager.PrefixLogExceptions(() =>
                {
                    int num = __instance.Sim.DaysPassed - __instance.LastRefreshDay;
                    if (num >= __instance.Sim.Constants.Story.DefaultContractRefreshRate)
                    {
                        __instance.SetLastRefreshDay(__instance.Sim.DaysPassed);
                        if (__instance.CurMaxContracts < __instance.GetSystemMaxContracts() &&
                            !__instance.Def.Depletable)
                        {
                            __instance.SetCurMaxContracts(Mathf.Min(__instance.GetSystemMaxContracts(),
                                __instance.CurMaxContracts +
                                __instance.Sim.Constants.Story.ContractRenewalPerWeek * num / 7));
                        }
                    }
                });
            }
        }
    }
}
