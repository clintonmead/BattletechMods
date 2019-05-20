using BattleTech;
using BattleTech.Data;
using BattletechModUtilities;
using Harmony;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BattleTech.UI;
using TMPro;
using UnityEngine.EventSystems;

namespace StrictHardpoints
{
    public static class StrictHardpoints
    {
        private static HarmonyManager<Settings> _harmonyManager;

        public static void Init(string directory, string settingsJSON)
        {
            MethodInfo original = typeof(MechStatisticsRules).GetMethod("GetHardpointCountForLocation", new Type[]
            {
                typeof(MechDef),
                typeof(ChassisLocations),
                typeof(int).MakeByRefType(),
                typeof(int).MakeByRefType(),
                typeof(int).MakeByRefType(),
                typeof(int).MakeByRefType()
            });
            MethodInfo prefix =
                typeof(Patch_MechStatisticsRules).GetMethod(nameof(Patch_MechStatisticsRules
                    .GetHardpointCountForLocation_Prefix));

            _harmonyManager = new HarmonyManager<Settings>(
                assembly: Assembly.GetExecutingAssembly(),
                settingsJSON: settingsJSON,
                harmonyUniqId: "io.github.clintonmead.StrictHardpoints");

            _harmonyManager.Patch(original, prefix: prefix);
        }

        public struct Settings : IDebug
        {
            [DefaultValue(true)] [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
            public readonly bool Debug;

            [JsonConstructor]
            public Settings(bool debug)
            {
                Debug = debug;
            }

            public bool DebugOn
            {
                get { return Debug; }
            }
        }

        public struct HardpointInfo
        {
            public readonly int NumBallistic;
            public readonly int NumEnergy;
            public readonly int NumMissile;
            public readonly int NumSmall;
            public readonly int NumMissileTubes;
            public readonly int NumEnergyLarge;
            public readonly int NumJumpJets;

            public HardpointInfo(int numBallistic, int numEnergy, int numMissile, int numSmall, int numMissileTubes,
                int numEnergyLarge, int numJumpJets)
            {
                NumBallistic = numBallistic;
                NumEnergy = numEnergy;
                NumMissile = numMissile;
                NumSmall = numSmall;
                NumMissileTubes = numMissileTubes;
                NumEnergyLarge = numEnergyLarge;
                NumJumpJets = numJumpJets;
            }

            public float HardpointMass
            {
                get
                {
                    const float ballisticHardpointMass = 3f;
                    const float largeEnergyHardpointMass = 2f;
                    const float energyHardpointMass = 0.5f;
                    const float smallHardpointMass = 0.25f;
                    const float missileTubeHardpointMass = 0.25f;
                    const float jumpJetHardpointMass = 0.25f;
                    const float jumpJetAdditionalPortMass = 0.125f;

                    const float largeEnergyExtraHardpointMass = largeEnergyHardpointMass - energyHardpointMass;

                    return
                        NumBallistic * ballisticHardpointMass +
                        NumEnergyLarge * largeEnergyExtraHardpointMass +
                        NumEnergy * energyHardpointMass +
                        NumMissileTubes * missileTubeHardpointMass +
                        NumSmall * smallHardpointMass +
                        (NumJumpJets >= 0 ? jumpJetHardpointMass : -jumpJetAdditionalPortMass) * NumJumpJets;
                }
            }

            public HardpointInfo(MechComponentRef componentRef) : this(componentRef.Def)
            {
            }

            public HardpointInfo(MechComponentDef def)
            {
                NumBallistic = 0;
                NumEnergy = 0;
                NumMissile = 0;
                NumSmall = 0;
                NumMissileTubes = 0;
                NumEnergyLarge = 0;
                NumJumpJets = 0;
                if (def.ComponentType == ComponentType.JumpJet)
                {
                    ++NumJumpJets;
                }
                else
                {
                    WeaponDef weaponDef = def as WeaponDef;
                    if (!(weaponDef is null))
                    {
                        switch (weaponDef.Category)
                        {
                            case WeaponCategory.Ballistic:
                                NumBallistic++;
                                break;
                            case WeaponCategory.Energy:
                                NumEnergy++;
                                if (weaponDef.Tonnage >= 3)
                                {
                                    ++NumEnergyLarge;
                                }

                                break;
                            case WeaponCategory.Missile:
                                NumMissileTubes += weaponDef.ShotsWhenFired;
                                NumMissile++;
                                break;
                            case WeaponCategory.AntiPersonnel:
                                NumSmall++;
                                break;
                            case WeaponCategory.NotSet:
                                break;
                            case WeaponCategory.AMS:
                                break;
                            case WeaponCategory.Melee:
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                }
            }

            public static HardpointInfo operator +(HardpointInfo x, HardpointInfo y)
            {
                return new HardpointInfo(
                    x.NumBallistic + y.NumBallistic,
                    x.NumEnergy + y.NumEnergy,
                    x.NumMissile + y.NumMissile,
                    x.NumSmall + y.NumSmall,
                    x.NumMissileTubes + y.NumMissileTubes,
                    x.NumEnergyLarge + y.NumEnergyLarge,
                    x.NumJumpJets + y.NumJumpJets);
            }

            public static HardpointInfo operator -(HardpointInfo x)
            {
                return new HardpointInfo(
                    -x.NumBallistic,
                    -x.NumEnergy,
                    -x.NumMissile,
                    -x.NumSmall,
                    -x.NumMissileTubes,
                    -x.NumEnergyLarge,
                    -x.NumJumpJets);
            }

            public static HardpointInfo operator -(HardpointInfo x, HardpointInfo y)
            {
                return x + (-y);
            }
        }

        private static HardpointInfo GetStockHardpointsByLocationFunc(MechDef mechDef, ChassisLocations chassisLocation)
        {
            return BattletechUtils.LoadStockMech(mechDef)
                .Inventory
                .Where(x => x.MountedLocation == chassisLocation)
                .Select(x => x.Def)
                .Select(x => new HardpointInfo(x))
                .Aggregate(new HardpointInfo(), (x, y) => x + y);
        }

        private static readonly Func<MechDef, ChassisLocations, HardpointInfo> GetStockHardpointsByLocation =
            CachedFunction.CacheFunction<MechDef, ChassisLocations, HardpointInfo>(GetStockHardpointsByLocationFunc);

        private static HardpointInfo GetHardpoints(MechDef mechDef)
        {
            return mechDef
                .Inventory
                .Select(x => x.Def)
                .Select(x => new HardpointInfo(x))
                .Aggregate(new HardpointInfo(), (x, y) => x + y);
        }

        private static HardpointInfo GetStockHardpointsNonCached(MechDef mechDef)
        {
            return GetHardpoints(BattletechUtils.LoadStockMech(mechDef));
        }

        private static readonly Func<MechDef, HardpointInfo> GetStockHardpointsFunc =
            CachedFunction.CacheFunction<MechDef, HardpointInfo>(GetStockHardpointsNonCached);

        private static HardpointInfo GetStockHardpoints(this MechDef mechDef)
        {
            return GetStockHardpointsFunc(mechDef);
        }

        public static class Patch_MechStatisticsRules
        {
            public static bool GetHardpointCountForLocation_Prefix(MechDef mechDef, ChassisLocations loc,
                ref int numBallistic, ref int numEnergy, ref int numMissile, ref int numSmall)
            {
                try
                {
                    HardpointInfo stockHardpoints = GetStockHardpointsByLocation(mechDef, loc);

                    numBallistic += stockHardpoints.NumBallistic;
                    numEnergy += stockHardpoints.NumEnergy;
                    numMissile += stockHardpoints.NumMissile;
                    numSmall += stockHardpoints.NumSmall;

                    return false;
                }
                catch (Exception e)
                {
                    _harmonyManager.Log(e);
                    return true;
                }
            }
        }

        [HarmonyPatch(typeof(MechLabLocationWidget))]
        [HarmonyPatch("ValidateAdd")]
        [HarmonyPatch(new[] {typeof(MechComponentDef)})]
        public static class Patch_MechLabLocationWidget_ValidateAddSimple
        {
            public static void Postfix(
                MechLabLocationWidget __instance,
                MechComponentDef newComponentDef,
                MechLabPanel ___mechLab,
                LocationDef ___chassisLocationDef,
                List<MechLabItemSlotElement> ___localInventory,
                TextMeshProUGUI ___locationName,
                ref bool __result)
            {
                try
                {
                    if (__result)
                    {
                        ChassisLocations location = ___chassisLocationDef.Location;
                        MechDef mechDef = ___mechLab.activeMechDef;
                        HardpointInfo stockHardpointInfo = GetStockHardpointsByLocation(mechDef, location);
                        HardpointInfo localHardpointInfo = ___localInventory
                            .Select(x => x.ComponentRef.Def)
                            .Concat(new[] {newComponentDef})
                            .Select(x => new HardpointInfo(x))
                            .Aggregate((x, y) => x + y);

                        _harmonyManager.DebugLog("Hardpoint info: " + localHardpointInfo.NumMissileTubes + ", " +
                                                 stockHardpointInfo.NumMissileTubes + ", " +
                                                 localHardpointInfo.NumEnergyLarge + ", " +
                                                 stockHardpointInfo.NumEnergyLarge);

                        if (localHardpointInfo.NumMissileTubes > stockHardpointInfo.NumMissileTubes)
                        {
                            __instance.SetDropErrorMessage(
                                "Cannot add {0} to {1}: Over allocated {2}/{3} missile tubes.",
                                newComponentDef.Description.Name,
                                ___locationName.text,
                                localHardpointInfo.NumMissileTubes,
                                stockHardpointInfo.NumMissileTubes);
                            __result = false;
                        }
                        else if (localHardpointInfo.NumEnergyLarge > stockHardpointInfo.NumEnergyLarge)
                        {
                            __instance.SetDropErrorMessage(
                                "Cannot add {0} to {1}: Over allocated {2}/{3} large energy weapons.",
                                newComponentDef.Description.Name,
                                ___locationName.text,
                                localHardpointInfo.NumEnergyLarge,
                                stockHardpointInfo.NumEnergyLarge);
                            __result = false;
                        }
                        else
                        {
                            int allowedJumpJets = mechDef.AllowedJumpJets();
                            int currentJumpJets = localHardpointInfo.NumJumpJets;
                            if (currentJumpJets > allowedJumpJets)
                            {
                                __instance.SetDropErrorMessage(
                                    "Cannot add {0} to {1}: Over allocated {2}/{3} jump jets.",
                                    newComponentDef.Description.Name,
                                    ___locationName.text,
                                    localHardpointInfo.NumEnergyLarge,
                                    stockHardpointInfo.NumEnergyLarge);
                                __result = false;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    _harmonyManager.Log(e);
                }
            }
        }

        [HarmonyPatch(typeof(MechLabLocationWidget))]
        [HarmonyPatch("OnMechLabDrop")]
        public static class Patch_MechLabLocationWidget_OnMechLabDrop
        {
            // Copied from OnMechLabDrop() but with inplace replacing of parts removed
            public static bool Prefix(
                MechLabLocationWidget __instance,
                MechLabPanel ___mechLab,
                Localize.Text ____dropErrorMessage,
                PointerEventData eventData,
                MechLabDropTargetType addToType)
            {
                return _harmonyManager.PrefixLogExceptions(() =>
                {
                    if (!___mechLab.Initialized)
                    {
                        return;
                    }

                    if (___mechLab.DragItem == null)
                    {
                        return;
                    }

                    IMechLabDraggableItem dragItem = ___mechLab.DragItem;
                    bool flag = __instance.ValidateAdd(dragItem.ComponentRef);
                    if (!flag)
                    {
                        ___mechLab.ShowDropErrorMessage(____dropErrorMessage);
                        ___mechLab.OnDrop(eventData);
                        return;
                    }

                    bool clearOriginalItem = __instance.OnAddItem(dragItem, true);
                    if (__instance.Sim != null)
                    {
                        WorkOrderEntry_InstallComponent subEntry =
                            __instance.Sim.CreateComponentInstallWorkOrder(___mechLab.baseWorkOrder.MechID,
                                dragItem.ComponentRef, __instance.loadout.Location, dragItem.MountedLocation);
                        ___mechLab.baseWorkOrder.AddSubEntry(subEntry);
                    }

                    dragItem.MountedLocation = __instance.loadout.Location;
                    ___mechLab.ClearDragItem(clearOriginalItem);
                    __instance.RefreshHardpointData();
                    ___mechLab.ValidateLoadout(false);
                });
            }
        }

        [HarmonyPatch(typeof(MechStatisticsRules))]
        [HarmonyPatch("CalculateTonnage")]
        public static class Patch_MechStatisticsRules_CalculateTonnage
        {
            public static void Postfix(MechDef mechDef, ref float currentValue)
            {
                currentValue += GetEmptyHardpointMass(mechDef);
            }
        }

        public static float GetEmptyHardpointMass(MechDef mechDef)
        {
            if (!_inFinish)
            {
                HardpointInfo stockHardpoints = GetStockHardpoints(mechDef);
                HardpointInfo currentHardpoints = GetHardpoints(mechDef);
                HardpointInfo emptyHardpoints = stockHardpoints - currentHardpoints;
                return emptyHardpoints.HardpointMass;
            }
            else
            {
                return 0;
            }
        }

        [ThreadStatic] private static bool _inFinish;

        [HarmonyPatch(typeof(LoadRequest))]
        [HarmonyPatch("Finish")]
        public static class Patch_LoadRequest_Finish
        {
            /*
             * The whole point of this patching is to ensure "Patch_MechStatisticsRules_CalculateTonnage"
             * doesn't loop when loading a stock mech. Without it a stock mech will continually check itself.
             *
             * So we mark when we're coming through this method. The first time we indicate we're in the method,
             * and call ourselves. When we're in the method, and called, we then return true so we actually
             * run the original method. Then we come back to the first method call of ourselves, and return false
             * so we don't loop forever.
             */
            public static bool Prefix(LoadRequest __instance)
            {
                return _harmonyManager.PrefixLogExceptions(() =>
                {
                    if (!_inFinish)
                    {
                        try
                        {
                            _inFinish = true;
                            __instance.Finish();
                        }
                        finally
                        {
                            _inFinish = false;
                        }

                        return false;
                    }
                    else
                    {
                        return true;
                    }
                });
            }
        }

        [ThreadStatic] private static bool _inCalculateTonnage;
        [ThreadStatic] private static float _unusedHardpointMass;

        [HarmonyPatch(typeof(MechLabMechInfoWidget))]
        [HarmonyPatch("CalculateTonnage")]
        public static class Patch_MechLabMechInfoWidget_CalculateTonnage
        {
            public static bool Prefix(MechLabMechInfoWidget __instance, MechLabPanel ___mechLab)
            {
                return _harmonyManager.PrefixLogExceptions(() =>
                {
                    if (!_inCalculateTonnage)
                    {
                        try
                        {
                            _inCalculateTonnage = true;
                            _unusedHardpointMass = GetEmptyHardpointMass(___mechLab.CreateMechDef());
                            __instance.CalculateTonnage();
                        }
                        finally
                        {
                            _inCalculateTonnage = false;
                        }

                        return false;
                    }
                    else
                    {
                        return true;
                    }
                });
            }

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                int callvirts = 0;

                foreach (CodeInstruction instruction in instructions)
                {
                    yield return instruction;
                    if (instruction.opcode == OpCodes.Callvirt)
                    {
                        ++callvirts;
                    }

                    if (callvirts == 4)
                    {
                        yield return new CodeInstruction(OpCodes.Ldsfld,
                            AccessTools.Field(typeof(StrictHardpoints), nameof(_unusedHardpointMass)));
                        yield return new CodeInstruction(OpCodes.Add);
                        ++callvirts;
                    }
                }
            }
        }

        private static ValueTuple<int, int> EquipedJumpjets(this MechDef mechDef)
        {
            int numJumpJets = GetStockHardpoints(mechDef).NumJumpJets;
            return (numJumpJets, numJumpJets > 0 ? mechDef.Chassis.MaxJumpjets : 0);
        }

        private static int AllowedJumpJets(this MechDef mechDef)
        {
            (int currentJumpJets, int allowedJumpJets) = mechDef.EquipedJumpjets();
            return allowedJumpJets;
        }
        
        [HarmonyPatch(typeof(MechBayMechInfoWidget))]
        [HarmonyPatch("SetHardpoints")]
        public static class Patch_MechBayChassisInfoWidget_SetHardpoints
        {
            public static void Postfix(MechDef ___selectedMech, TextMeshProUGUI ___jumpjetHardpointText)
            {
                _harmonyManager.LogExceptions(() =>
                {
                    if (!(___selectedMech is null))
                    {
                        (int currentJumpJets, int allowedJumpJets) = ___selectedMech.EquipedJumpjets();
                        ___jumpjetHardpointText.SetText("{0}/{1}", currentJumpJets, allowedJumpJets);
                    }
                });
            }
        }
    }
}
