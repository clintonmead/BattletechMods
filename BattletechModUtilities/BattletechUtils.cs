﻿using BattleTech;
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
    }
}
