using System;
using BattleTech;
using BattleTech.UI;
using System.Collections.Generic;
using BattleTech.Data;

namespace BattletechModUtilities
{
    public static class BattletechUtils
    {
        public static SimGameState GetSimGameState(this Starmap starmap)
        {
            return starmap.GetFieldValue<SimGameState>("sim");
        }

        public static SimGameState GetSimGameState(this StarmapRenderer starmapRenderer)
        {
            return starmapRenderer.starmap.GetSimGameState();
        }

        public static SimGameState GetSimGameState(this SGNavigationScreen sgNavigationScreen)
        {
            return sgNavigationScreen.GetFieldValue<SimGameState>("simState");
        }

        public static void OnDayPassed(this SimGameState simGameState, int timelapse)
        {
            simGameState.InvokeMethod("OnDayPassed", timelapse);
        }

        public static void RefreshSystemIndicators(this SGNavigationScreen sgNavigationScreen)
        {
            sgNavigationScreen.InvokeMethod("RefreshSystemIndicators");
        }

        public static List<string> GetVisitedStarSystems(this SimGameState simGameState)
        {
            return simGameState.GetFieldValue<List<string>>("VisitedStarSystems");
        }

        public static bool HasStarSystemBeenVisited(this SimGameState simGameState, string systemID)
        {
            return simGameState.GetVisitedStarSystems().Contains(systemID);
        }

        public static bool HasStarSystemBeenVisited(this Starmap starmap, string systemID)
        {
            return starmap.GetSimGameState().HasStarSystemBeenVisited(systemID);
        }

        public static bool HasStarSystemBeenVisited(this Starmap starmap, StarSystemNode node)
        {
            return starmap.HasStarSystemBeenVisited(node.System.ID);
        }

        public static int GetSystemMaxContracts(this StarSystem starSystem)
        {
            return (int) starSystem.InvokeMethod("GetSystemMaxContracts");
        }

        public static void SetLastRefreshDay(this StarSystem starSystem, int lastRefreshDay)
        {
            starSystem.SetFieldValue("LastRefreshDay", lastRefreshDay);
        }

        public static void SetCurMaxContracts(this StarSystem starSystem, float curMaxContracts)
        {
            starSystem.SetFieldValue("CurMaxContracts", curMaxContracts);
        }

        public static void ConsumeEvasivePips(this AbstractActor __instance, int pipsToRemove)
        {
            int pipsRemoved = 0;
            while (__instance.EvasivePipsCurrent > 0 && pipsRemoved < pipsToRemove)
            {
                __instance.ConsumeEvasivePip(true);
                ++pipsRemoved;
            }

            if (pipsRemoved > 0 && !__instance.IsDead && !__instance.IsFlaggedForDeath)
            {
                __instance.Combat.MessageCenter.PublishMessage(new FloatieMessage(__instance.GUID, __instance.GUID, "-" + pipsRemoved + " EVASION", FloatieMessage.MessageNature.Debuff));
            }
        }

        public static T LoadResource<T>(BattleTechResourceType resourceType, string resourceId, TimeSpan? timeout = null) 
            where T : class
        {
            TimeSpan timeoutNotNull = timeout ?? TimeSpan.FromMinutes(1);

            DateTime errorTime = DateTime.Now + timeoutNotNull;

            T resource = null;

            GameInstance gameInstance = UnityGameInstance.BattleTechGame;

            DataManager dataManager = gameInstance.DataManager;

            LoadRequest loadRequest = dataManager.CreateLoadRequest(null, false);
            loadRequest.AddLoadRequest<T>(resourceType, resourceId,
                (s, resourceReturned) =>
                {
                    resource = resourceReturned;
                }, false);
            loadRequest.ProcessRequests(10u);
            do
            {
                dataManager.Update(UnityEngine.Time.deltaTime);
                if (DateTime.Now > errorTime)
                {
                    throw new Exception("Failed to load resource: " + resourceId);
                }
            } while (resource is null);

            return resource;
        }

        public static MechDef LoadStockMechNotCached(string resourceId, TimeSpan timeout)
        {
            return LoadResource<MechDef>(BattleTechResourceType.MechDef, resourceId, timeout);
        }

        public static MechDef LoadStockMechNotCached(string resourceId)
        {
            return LoadResource<MechDef>(BattleTechResourceType.MechDef, resourceId);
        }

        public static Func<string, MechDef> LoadStockMechFunc = CachedFunction.CacheFunction<string, MechDef>(LoadStockMechNotCached);

        public static MechDef LoadStockMech(string mechDefString)
        {
            return LoadStockMechFunc(mechDefString);
        }

        public static MechDef LoadStockMech(MechDef mechDef)
        {
            return LoadStockMech(mechDef.ChassisID.Replace("chassisdef", "mechdef"));
        }

        public static void SetDropErrorMessage(
            this MechLabLocationWidget mechLabLocationWidget,
            string msg, 
            params object[] args)
        {
            mechLabLocationWidget.InvokeMethod("SetDropErrorMessage", msg, args);
        }

        public static void Finish(this LoadRequest loadRequest)
        {
            loadRequest.InvokeMethod("Finish");
        }

        public static void CalculateTonnage(this MechLabMechInfoWidget mechLabMechInfoWidget)
        {
            mechLabMechInfoWidget.InvokeMethod("CalculateTonnage");
        }
    }
}
