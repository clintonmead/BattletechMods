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
        private static HarmonyManager<Settings> _harmonyManager;

        public static void Init(string directory, string settingsJSON)
        {
            _harmonyManager = new HarmonyManager<Settings>(
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
                _harmonyManager.LogExceptions(() =>
                {
                    int daysToSkip = _harmonyManager.Settings.ContractDays;
                    _harmonyManager.DebugLog("Contract finished, skipping " + daysToSkip + "day(s)");
                    if (daysToSkip > 0)
                    {
                        __instance.OnDayPassed(daysToSkip);
                    }
                });
            }
        }
    }
}
