using System.Drawing.Drawing2D;
using HarmonyLib;
using PavonisInteractive.TerraInvicta;
using UnityEngine;
using UnityModManagerNet;

namespace TITweaksMod.CouncilorPatches
{
    [HarmonyPatch(typeof(TIMissionState), nameof(TIMissionState.ResolveMission))]
    internal static class TIMissionState_ResolveMission_Patch
    {
        static bool Prefix(TIMissionState __instance)
        {
            if (!Main.enabled || Main.Settings is null)
                return true;

            Main.Logger?.Log(
                $"ResolveMission>> councilor={__instance.councilor.displayName}|faction={__instance.councilor.faction.displayName}|mission={__instance.missionTemplate.displayName}"
            );
            return true;
        }

        static void Postfix(TIMissionState __instance, MissionResult __result)
        {
            if (!Main.enabled || Main.Settings is null)
                return;
            Main.Logger?.Log($"ResolveMission>> result={__result.missionOutcome.ToString()}");
        }
    }

    [HarmonyPatch(
        typeof(TIMissionResolution_Contested),
        nameof(TIMissionResolution_Contested.GetMissionOutcome)
    )]
    internal static class TIMissionResolution_Contested_GetMissionOutcome_Patch
    {
        static bool Prefix(
            TIMissionResolution __instance,
            TIMissionTemplate mission,
            TICouncilorState councilor,
            TIGameState target
        )
        {
            if (!Main.enabled || Main.Settings is null)
                return true;

            Main.Logger?.Log(
                $"GetMissionOutcome>> councilor={councilor.displayName}|faction={councilor.faction.displayName}|target={target}|relevant_faction={mission.target.GetRelevantFaction(target).displayName}|mission={mission.displayName}"
            );
            return true;
        }

        static void Postfix(TIMissionResolution __instance, TIMissionResult __result)
        {
            if (!Main.enabled || Main.Settings is null)
                return;
            Main.Logger?.Log($"GetMissionOutcome>> result={__result.outcome}({__result.roll})");
        }
    }

    [HarmonyPatch(
        typeof(TIMissionResolution_Contested),
        nameof(TIMissionResolution_Contested.GetSuccessChance)
    )]
    internal static class TIMissionResolution_Contested_GetSuccessChance_Patch
    {
        static bool Prefix(
            TIMissionResolution __instance,
            TIMissionTemplate mission,
            TICouncilorState councilor,
            TIGameState target
        )
        {
            if (!Main.enabled || Main.Settings is null)
                return true;

            Main.Logger?.Log(
                $"GetSuccessChance>> councilor={councilor.displayName}|faction={councilor.faction.displayName}|target={target}|relevant_faction={mission.target.GetRelevantFaction(target).displayName}|mission={mission.displayName}"
            );
            return true;
        }

        static void Postfix(TIMissionResolution __instance, float __result)
        {
            if (!Main.enabled || Main.Settings is null)
                return;
            Main.Logger?.Log($"GetSuccessChance>> result={__result}");
        }
    }

    //internal static class TIMissionResolution_GetMissionOutcome_Patch
    //{
    //    static bool Prefix(TIMissionResolution __instance, TIMissionTemplate mission)
    //    {
    //        if (!Main.enabled || Main.Settings is null)
    //            return true;
    //        Main.Logger?.Log($"me={__instance}|mission={mission}");
    //        return true;
    //    }

    //    static void Postfix(TIMissionResolution __instance, TIMissionResult __result)
    //    {
    //        if (!Main.enabled || Main.Settings is null)
    //            return;
    //        Main.Logger?.Log($"me={__instance}|result={__result}");
    //    }

    //    public static void ApplyPatch(Harmony harmony)
    //    {
    //        var baseType = typeof(TIMissionResolution);
    //        foreach (var type in AccessTools.AllTypes())
    //        {
    //            if (!type.IsAbstract && baseType.IsAssignableFrom(type))
    //            {
    //                var method = AccessTools.Method(type, "GetMissionOutcome");
    //                if (method is not null)
    //                {
    //                    harmony.Patch(
    //                        method,
    //                        prefix: new HarmonyMethod(
    //                            typeof(TIMissionResolution_GetMissionOutcome_Patch),
    //                            nameof(Prefix)
    //                        ),
    //                        postfix: new HarmonyMethod(
    //                            typeof(TIMissionResolution_GetMissionOutcome_Patch),
    //                            nameof(Postfix)
    //                        )
    //                    );
    //                }
    //            }
    //        }
    //    }
    //}

    public enum MissionOutcome
    {
        Default = 0,
        CriticalFail = 1,
        Fail = 2,
        Succeed = 3,
        CriticalSucceed = 4,
    }

    internal static class UI
    {
        internal static readonly string[] MissionOutcomeNames =
        [
            "Default",
            "Critical Fail",
            "Fail",
            "Success",
            "Critical Success",
        ];

        internal static readonly string[] GroupNames = ["Player", "Other Humans", "Aliens"];

        internal static MissionOutcome OutcomeButton(in MissionOutcome oldValue)
        {
            return MissionOutcome.Default;
        }

        internal static void OnGUI(CouncilorSettings settings, in SettingsUIContext context)
        {
            // box group
            GUILayout.BeginVertical(context.GroupStyle);

            // group label
            GUILayout.Label("Councilor Settings", UnityModManager.UI.h2);

            // TWEAK: councilor mission success matrix
            GUILayout.Label(".1 Mission Outcome Matrix");
            var columnWidth = GUILayout.Width(100f);
            var matrix = settings.missionOutcomeMatrix;
            GUILayout.BeginHorizontal();
            GUILayout.Label(" ", columnWidth);
            for (int i = 0; i < GroupNames.Length; i++)
                GUILayout.Label(GroupNames[i], columnWidth);
            GUILayout.EndHorizontal();
            for (int row = 0; row < 3; row++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(GroupNames[row], columnWidth);
                for (int col = 0; col < 3; col++)
                {
                    //if (GUILayout.Button)
                }
            }

            GUILayout.EndVertical();
        }
    }

    public class MissionOutcomeMatrix
    {
        public Row Player { get; set; } = new();
        public Row OtherHumans { get; set; } = new();
        public Row Aliens { get; set; } = new();

        public class Row
        {
            public MissionOutcome Player { get; set; }
            public MissionOutcome OtherHumans { get; set; }
            public MissionOutcome Aliens { get; set; }

            public MissionOutcome this[int index] =>
                index switch
                {
                    0 => Player,
                    1 => OtherHumans,
                    2 => Aliens,
                    _ => throw new IndexOutOfRangeException(),
                };
        }

        public Row this[int index] =>
            index switch
            {
                0 => Player,
                1 => OtherHumans,
                2 => Aliens,
                _ => throw new IndexOutOfRangeException(),
            };
    }

    public class CouncilorSettings : UnityModManager.ModSettings
    {
        public bool dummy = false;

        //public MissionOutcomeMatrix missionOutcomeMatrix = new();
        public MissionOutcomeMatrix missionOutcomeMatrix;

        public CouncilorSettings()
        {
            //    foreach (FactionGroups a in Enum.GetValues(typeof(FactionGroups)))
            //    foreach (FactionGroups b in Enum.GetValues(typeof(FactionGroups)))
            //    {
            //        missionOutcomeMatrix.Values[(a, b)] = MissionOutcomes.Default;
            //    }

            missionOutcomeMatrix = new()
            {
                Player = new()
                {
                    Player = MissionOutcome.Default,
                    OtherHumans = MissionOutcome.Default,
                    Aliens = MissionOutcome.Default,
                },
                OtherHumans = new()
                {
                    Player = MissionOutcome.Default,
                    OtherHumans = MissionOutcome.Default,
                    Aliens = MissionOutcome.Default,
                },
                Aliens = new()
                {
                    Player = MissionOutcome.Default,
                    OtherHumans = MissionOutcome.Default,
                    Aliens = MissionOutcome.Default,
                },
            };
        }
    }
}
