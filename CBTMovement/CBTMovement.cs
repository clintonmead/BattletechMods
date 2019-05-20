using BattleTech;
using BattletechModUtilities;
using Harmony;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Reflection;

namespace CBTMovement
{
    public class CBTMovement
    {
        private static HarmonyManager<Settings> _harmonyManager;

        public static void Init(string directory, string settingsJSON)
        {
            _harmonyManager = new HarmonyManager<Settings>(
                assembly: Assembly.GetExecutingAssembly(),
                settingsJSON: settingsJSON,
                harmonyUniqId: "io.github.clintonmead.FixRepShopAdjustment");
        }


        [HarmonyPatch(typeof(Pathing))]
        [HarmonyPatch(typeof(float), "GetAngleAvailable", typeof(float))]
        public static class PatchSimGameStateGetReputationShopAdjustment
        {
            public static bool Prefix(float costLeft, ref float __result)
            {
                // Movement units are in meters, and one MP is 30 meters which should provide 60 degrees of turning.
                // So times the meters by 2 to get the degrees of turning available.
                return _harmonyManager.PrefixPatch(ref __result, () => Math.Max(costLeft * 2f, 180) );
            }
        }

        [HarmonyPatch(typeof(ToHit), "GetAllModifiers")]
        public static class Patch_ToHit_GetAllModifiers
        {
            private static void Postfix(ref float __result, AbstractActor attacker)
            {
                _harmonyManager.PostfixPatch(ref __result,
                    result => result + ((attacker.HasMovedThisRound && attacker.JumpedLastRound) ? 2 : 0));
            }
        }
    }
}
