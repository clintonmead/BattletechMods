using BattleTech;
using BattleTech.UI;
using BattletechModUtilities;
using ClintonMead;
using Harmony;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace EnhancedStarMap
{
    public static class EnhancedStarMap
    {
        private static HarmonyManager<Settings> HarmonyManager;

        public static void Init(string directory, string settingsJSON)
        {
            HarmonyManager = new HarmonyManager<Settings>(
                Assembly.GetExecutingAssembly(),
                settingsJSON,
                "io.github.clintonmead.EnhancedStarMap");
        }

        private enum MapType { None, Difficulty, Visited };

        private static MapType CurrentMapType = MapType.None;

        private struct Settings : IDebug
        {
            [DefaultValue(true)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
            public readonly bool Debug;
            public readonly DifficultyColors DifficultyColors;
            public readonly VisitedColors VisitedColors;

            [JsonConstructor]
            public Settings(bool debug, DifficultyColors difficultyColors, VisitedColors visitedColors)
            {
                Debug = debug;
                DifficultyColors = difficultyColors;
                VisitedColors = visitedColors;
            }

            public bool DebugOn
            {
                get { return Debug; }
            }
        }

        private struct RGBColor<T>
        {
            public readonly T Red;
            public readonly T Green;
            public readonly T Blue;

            [JsonConstructor]
            public RGBColor(T red, T green, T blue)
            {
                Red = red;
                Green = green;
                Blue = blue;
            }
        }

        private static Color FromRGBColor(this RGBColor<float> rgbColor)
        {
            return new Color(rgbColor.Red, rgbColor.Green, rgbColor.Blue);
        }

        private struct DifficultyColors
        {
            [DefaultValue(1)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
            public readonly int MinimumDifficulty;

            public readonly RGBColor<float>[] Colors;

            [JsonConstructor]
            public DifficultyColors(int minDifficulty, IEnumerable<RGBColor<float>> colors)
            {
                MinimumDifficulty = minDifficulty;
                Colors = colors.ToArray();
            }
        }

        private class VisitedColors
        {
            public readonly RGBColor<float> NotVisitedColor;
            public readonly RGBColor<float> VisitedColor;

            [JsonConstructor]
            public VisitedColors(RGBColor<float> visitedColor, RGBColor<float> notVisitedColor)
            {
                if (visitedColor.Equals(default(RGBColor<float>)))
                {
                    visitedColor = new RGBColor<float>(0, 1, 0);
                }
                if (notVisitedColor.Equals(default(RGBColor<float>)))
                {
                    notVisitedColor = new RGBColor<float>(1, 0, 0);
                }
                VisitedColor = visitedColor;
                NotVisitedColor = notVisitedColor;
            }
        }

        [HarmonyPatch(typeof(StarmapRenderer))]
        [HarmonyPatch("InitializeSysRenderer")]
        private static class PatchStarmapRendererInitializeSysRenderer
        {
            public static bool Prefix(
                StarmapRenderer __instance,
                StarSystemNode node,
                StarmapSystemRenderer renderer)
            {
                return HarmonyManager.LogExceptions(() =>
                {
                    if (Input.GetKeyUp(KeyCode.F1))
                    {
                        CurrentMapType = MapType.None;
                    }
                    else if (Input.GetKeyUp(KeyCode.F1))
                    {
                        CurrentMapType = MapType.Difficulty;
                    }

                    bool flag = __instance.starmap.CanTravelToNode(node, false);
                    RGBColor<float> color = new RGBColor<float>(1,1,1);

                    VisitedColors visitedColors = HarmonyManager.Settings.VisitedColors;

                    switch (CurrentMapType)
                    {
                        case MapType.None:
                            return true;
                        case MapType.Difficulty:
                            color = GetDifficultyColor(__instance, node);
                            break;
                        case MapType.Visited:
                            color = __instance.starmap.HasStarSystemBeenVisited(node) 
                                        ? visitedColors.VisitedColor 
                                        : visitedColors.NotVisitedColor;
                            break;
                        default:
                            throw new InvalidEnumArgumentException("'CurrentMapType' has invalid value. This should never happen. Please report this as a bug.");

                    }
                    if (renderer.Init(node, flag ? color.FromRGBColor() : __instance.unavailableColor, flag))
                    {
                        __instance.RefreshBorders();
                    }

                    // Don't call the original method, we've replaced it.
                    return false;
                });
            }

            private static RGBColor<float> GetDifficultyColor(StarmapRenderer starmapRenderer, StarSystemNode starSystemNode)
            {
                SimGameState.SimGameType gameType = starmapRenderer.GetSimGameState().SimGameMode;
                int difficulty = starSystemNode.System.Def.GetDifficulty(gameType);

                return GetDifficultyColor(difficulty);
            }

            private static RGBColor<float> GetDifficultyColor(int difficulty)
            {
                DifficultyColors difficultyColorSettings = HarmonyManager.Settings.DifficultyColors;
                RGBColor<float>[] rgbColors = difficultyColorSettings.Colors;
                int minimumDifficulty = difficultyColorSettings.MinimumDifficulty;

                int index = difficulty - minimumDifficulty;

                index = index.Clamp(0, rgbColors.Length - 1);

                return rgbColors[index];
            }
        }

        [HarmonyPatch(typeof(StarmapRenderer))]
        [HarmonyPatch("Update")]
        public static class PatchStarmapRendererUpdate
        {
            public static void Postfix(StarmapRenderer __instance)
            {
                HarmonyManager.LogExceptions(() =>
                {
                    MapType? newMapType = null;
                    if (Input.GetKeyUp(KeyCode.F1))
                    {
                        newMapType = MapType.None;
                    }
                    else if (Input.GetKeyUp(KeyCode.F2))
                    {
                        newMapType = MapType.Difficulty;
                    }
                    else if (Input.GetKeyUp(KeyCode.F3))
                    {
                        newMapType = MapType.Visited;
                    }

                    if (newMapType.HasValue && newMapType.Value != CurrentMapType)
                    {
                        CurrentMapType = newMapType.Value;
                        __instance.RefreshSystems();
                    }
                });
            }
        }
    }
}
