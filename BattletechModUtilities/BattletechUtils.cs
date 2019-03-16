using BattleTech;
using BattleTech.UI;
using ClintonMead;
using System.Collections.Generic;

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
    }
}
