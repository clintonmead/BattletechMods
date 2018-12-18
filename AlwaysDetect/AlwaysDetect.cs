using BattletechModUtilities;
using System.Reflection;
using Newtonsoft.Json;
using System.ComponentModel;
using Harmony;
using BattleTech;
using UnityEngine;
using System;

namespace AlwaysDetected
{
    public static class AlwaysDetect
    {
        public static HarmonyManager<AlwaysDetectSettings> HarmonyManager;

        public static void Init(string directory, string settingsJSON)
        {
            HarmonyManager = new HarmonyManager<AlwaysDetectSettings>(
                Assembly.GetExecutingAssembly(),
                settingsJSON,
                "io.github.clintonmead.AlwaysDetect");
        }
    }

    public class AlwaysDetectSettings : IDebug
    {
        [DefaultValue(float.PositiveInfinity)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public readonly float MaxAIDetectionDistance;
        [DefaultValue(0)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public readonly float MaxHumanDetectionDistance;
        [DefaultValue(true)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public readonly bool Debug;

        public bool DebugOn => Debug;
    }

    [HarmonyPatch(typeof(LineOfSight))]
    [HarmonyPatch("GetVisibilityToTargetWithPositionsAndRotations")]
    [HarmonyPatch(new[] { typeof(AbstractActor), typeof(Vector3), typeof(ICombatant), typeof(Vector3), typeof(Quaternion) })]
    public static class Patch 
    {
        public static void Postfix(ref VisibilityLevel __result, AbstractActor source, Vector3 sourcePosition, Vector3 targetPosition)
        {
            try
            {
                if (__result == VisibilityLevel.None)
                {
                    float maxDistance = source.team.TeamController == TeamController.Computer
                        ? AlwaysDetect.HarmonyManager.Settings.MaxAIDetectionDistance
                        : AlwaysDetect.HarmonyManager.Settings.MaxHumanDetectionDistance;
                    if (Vector3.Distance(sourcePosition, targetPosition) < maxDistance)
                    {
                        __result = VisibilityLevel.Blip0Minimum;
                    }
                }
            }
            catch (Exception e)
            {
                AlwaysDetect.HarmonyManager.Log(e.ToString());
                throw;
            }
        }
    }
}
