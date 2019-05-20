using BattleTech;
using BattletechModUtilities;
using Harmony;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace RangedPermanentEvasion
{
    public class RangedPermanentEvasion
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
            [DefaultValue(true)] [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
            public readonly bool Debug;

            [DefaultValue(0)] [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
            public readonly int RangedPipsLost;

            [DefaultValue(0)] [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
            public readonly int RangedDamagePipsLost;

            [DefaultValue(1)] [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
            public readonly int MeleePipsLost;

            [DefaultValue(2)] [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
            public readonly int MeleeDamagePipsLost;

            [DefaultValue(2)] [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
            public readonly int UnsteadyPipsLost;

            [JsonConstructor]
            public Settings(
                bool debug,
                int rangedPipsLost,
                int rangedDamagePipsLost,
                int meleePipsLost,
                int meleeDamagePipsLost,
                int unsteadyPipsLost)
            {
                Debug = debug;
                RangedPipsLost = rangedPipsLost;
                RangedDamagePipsLost = rangedDamagePipsLost;
                MeleePipsLost = meleePipsLost;
                MeleeDamagePipsLost = meleeDamagePipsLost;
                UnsteadyPipsLost = unsteadyPipsLost;
            }

            public bool DebugOn
            {
                get { return Debug; }
            }
        }

        [HarmonyPatch(typeof(AbstractActor), "ResolveAttackSequence")]
        public static class PatchAbstractActorConsumeEvasivePip
        {
            public static bool Prefix(AbstractActor __instance, string sourceID, int sequenceID, int stackItemID,
                AttackDirection attackDirection)
            {
                return _harmonyManager.PrefixLogExceptions(() =>
                {
                    AttackDirector.AttackSequence attackSequence =
                        __instance.Combat.AttackDirector.GetAttackSequence(sequenceID);

                    int pipsToRemove =
                        attackSequence == null
                            ? 0
                            : attackSequence.isMelee
                                ? attackSequence.attackDidDamage
                                    ? _harmonyManager.Settings.MeleeDamagePipsLost
                                    : _harmonyManager.Settings.MeleePipsLost
                                : attackSequence.attackDidDamage
                                    ? _harmonyManager.Settings.RangedDamagePipsLost
                                    : _harmonyManager.Settings.RangedPipsLost;

                    __instance.ConsumeEvasivePips(pipsToRemove);

                    if (attackSequence != null && attackSequence.attackDidDamage)
                    {
                        List<Effect> list = __instance.Combat.EffectManager.GetAllEffectsTargeting(__instance)
                            .FindAll((Effect x) =>
                                x.EffectData.targetingData.effectTriggerType == EffectTriggerType.OnDamaged);
                        for (int i = 0; i < list.Count; i++)
                        {
                            list[i].OnEffectTakeDamage(attackSequence.attacker, __instance);
                        }

                        if (attackSequence.isMelee)
                        {
                            int value = attackSequence.attacker.StatCollection.GetValue<int>("MeleeHitPushBackPhases");
                            if (value > 0)
                            {
                                for (int j = 0; j < value; j++)
                                {
                                    __instance.ForceUnitOnePhaseDown(sourceID, stackItemID, false);
                                }
                            }
                        }
                    }
                });
            }
        }

        [HarmonyPatch(typeof(AbstractActor), "ApplyUnsteady")]
        public static class PatchAbstractActorApplyUnsteady
        {
            public static bool Prefix(AbstractActor __instance)
            {
                return _harmonyManager.PrefixLogExceptions(() => {
                    __instance.IsUnsteady = true;
                    __instance.ConsumeEvasivePips(_harmonyManager.Settings.UnsteadyPipsLost);
                    __instance.Combat.MessageCenter.PublishMessage(new UnsteadyChangedMessage(__instance.GUID));
                });
            }
        }
    }
}