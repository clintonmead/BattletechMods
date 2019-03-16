using BattletechModUtilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using BattleTech;
using Harmony;
using Optional;

namespace FixRepShopAdjustment
{
    public static class FixRepShopAdjustment
    {
        private static HarmonyManager<Settings> _harmonyManager;

        public static void Init(string directory, string settingsJSON)
        {
            _harmonyManager = new HarmonyManager<Settings>(
                assembly: Assembly.GetExecutingAssembly(),
                settingsJSON: settingsJSON,
                harmonyUniqId: "io.github.clintonmead.FixRepShopAdjustment");
        }

        public struct Settings : IDebug
        {
            [DefaultValue(true)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
            public readonly bool Debug;

            [JsonConstructor]
            public Settings(int contractDays, bool debug)
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

        [HarmonyPatch(typeof(SimGameState))]
        [HarmonyPatch(typeof(float), "GetReputationShopAdjustment", new Type[] {typeof(SimGameReputation)})]
        public static class PatchSimGameStateGetReputationShopAdjustment
        {
            public static bool Prefix(SimGameState __instance, SimGameReputation rep, ref float __result)
            {
                return _harmonyManager.PrefixPatchAndReturn(ref __result, () =>
                {
                    if (rep == SimGameReputation.LOATHED)
                    {
                        return ((float) 1).Some();
                    }
                    else
                    {
                        return Option.None<float>();
                    }
                });
            }
        }
    }
}
