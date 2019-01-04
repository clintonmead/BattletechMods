using BattleTech;
using BattleTech.UI;
using BattletechModUtilities;
using Harmony;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Reflection;

namespace ContractTime
{
    public static class ContractTime
    {
        private static HarmonyManager<Settings> HarmonyManager;

        private static int _catchUpDays = 0;

        public static void Init(string directory, string settingsJSON)
        {
            HarmonyManager = new HarmonyManager<Settings>(
                assembly: Assembly.GetExecutingAssembly(),
                settingsJSON: settingsJSON,
                harmonyUniqId: "io.github.clintonmead.ContractTime");
        }

        public struct Settings : IDebug
        {
            [DefaultValue(1)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
            public readonly int ContractDays;

            [DefaultValue(true)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
            public readonly bool Debug;

            [JsonConstructor]
            public Settings(int contractDays, bool debug)
            {
                ContractDays = contractDays;
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

        [HarmonyPatch(typeof(SimGameState))]
        [HarmonyPatch("ResolveCompleteContract")]
        public static class PatchSimGameStateUpdate
        {
            public static void Postfix(SimGameState __instance)
            {
                HarmonyManager.LogExceptions(() =>
                {
                    int daysToSkip = HarmonyManager.Settings.ContractDays;
                    HarmonyManager.DebugLog("Contract finished, skipping " + daysToSkip + "day(s)");
                    if (daysToSkip > 0)
                    {
                        __instance.OnDayPassed(daysToSkip);
                    }
                });
            }
        }

    }
}
