/// <summary>
/// Mining-related patches for Terra Invicta Tweaks Mod
/// </summary>
using HarmonyLib;
using PavonisInteractive.TerraInvicta;
using UnityEngine;
using UnityModManagerNet;

namespace TITweaksMod.MiningPatches
{
    [HarmonyPatch(
        typeof(TIFactionState),
        nameof(TIFactionState.GetMissionControlRequirementFromMineNetwork)
    )]
    internal static class MineMCCostPatch
    {
        /// <summary>
        /// Prefix patch for the TIFactionState.GetMissionControlRequirementFromMineNetwork() method that
        /// captures the original mineNetworkSize parameter for use in the Postfix patch. This is necessary
        /// as the parameter has a default value and is modified in the patched method, and Postfix only
        /// receives the last value of the variable, not the original input value.
        /// </summary>
        /// <param name="__state">Harmony variable that preserves the original mineNetworkSize for Postfix.</param>
        /// <param name="mineNetworkSize">Patched function parameter indicating number of mines.</param>
        /// <returns></returns>
        static bool Prefix(out int __state, int mineNetworkSize)
        {
            __state = mineNetworkSize;
            return true;
        }

        // Mine cost tweaks are applied in Postfix - this allows other mods to patch the same function.
        /// <summary>
        /// Postfix patch for the in-game TIFactionState.GetMissionControlRequirementFromMineNetwork() method
        /// that calculates the MC cost of mines. This patch applies two optional tweaks based on user settings:
        /// 1. Linear MC cost scaling - MC cost increases linearly per mine above a free limit.
        /// 2. Global MC cost multiplier - a multiplier applied to the final MC cost.
        /// </summary>
        /// <param name="__instance">Active game faction, the owner of the mine network.</param>
        /// <param name="__result">Tweaked total MC cost.</param>
        /// <param name="__state">The original mine network size captured from the Prefix patch.</param>
        static void Postfix(TIFactionState __instance, ref int __result, int __state)
        {
            if (!Main.enabled || Main.Settings is null)
                return; // keep original
            MiningSettings settings = Main.Settings.mineSettings;

            // if linear cost is enabled, override original calculation and result
            if (settings.linearMineMCCostEnabled)
            {
                int mineNetworkSize = __state;
                if (mineNetworkSize < 0)
                    mineNetworkSize = __instance.MineNetworkSize;

                mineNetworkSize -= __instance.SafeMineNextworkSize;
                __result = mineNetworkSize > 0 ? mineNetworkSize * settings.linearMCCostPerMine : 0;
            }

            // apply global cost multiplier if set
            if (settings.globalMineMCCostMultiplier != 1.0f)
            {
                __result = Mathf.RoundToInt(__result * settings.globalMineMCCostMultiplier);
            }
        }
    }

    [HarmonyPatch(
        typeof(TIFactionState),
        nameof(TIFactionState.GetCurrentMiningMultiplierFromOrgsAndEffects)
    )]
    internal static class MineProductivityPatch
    {
        /// <summary>
        /// Postfix patch for the in-game TIFactionState.GetCurrentMiningMultiplierFromOrgsAndEffects()
        /// method that applies a multiplier to the mining productivity of selected factions.
        /// </summary>
        /// <param name="__instance">Active game faction.</param>
        /// <param name="__result">Tweaked mining productivity.</param>
        static void Postfix(TIFactionState __instance, ref float __result)
        {
            if (!Main.enabled || Main.Settings is null)
                return; // keep original

            MiningSettings settings = Main.Settings.mineSettings;
            if (settings.globalMineProductionMultiplier != 1.0f)
            {
                TargetGroups targets = settings.globalMineProductionMultiplierTargets;
                if (
                    (targets & TargetGroups.Player) != 0 && __instance.isActivePlayer
                    || (
                        (targets & TargetGroups.Humans) != 0
                        && __instance.IsActiveHumanFaction
                        && !__instance.isActivePlayer
                    )
                    || ((targets & TargetGroups.Aliens) != 0 && __instance.IsAlienFaction)
                )
                {
                    __result *= settings.globalMineProductionMultiplier;
                }
            }
        }
    }

    [HarmonyPatch(typeof(TIFactionState), nameof(TIFactionState.GetYearlyIncome))]
    internal static class RecalcMineIncomeIfNeededPatch
    {
        /// <summary>
        /// Indicates that mining income recalculation is needed.
        /// This is set by the UI when the mine productivity settings are changed.
        /// </summary>
        internal static bool needUpdate = false;

        /// <summary>
        /// Set of factions that need to recalculate the mining income.
        /// </summary>
        static HashSet<TIFactionState>? dirtyFactions;

        /// <summary>
        /// The purpose of this Postfix patch is that the game only checks the mine productivity
        /// multiplier on certain events (e.g. loading the game, org with mining output bonus is
        /// activated or deactivated). This patch runs regularly when the game check a faction's
        /// mining income, and if the multiplier settings have changed, it forces the game to
        /// recalculate the mining income for that faction.
        ///
        /// The code to initiate recalculation is based on the game's own code when an org
        ///  that affects mining income is activated/deactivated.
        /// </summary>
        /// <param name="__instance"></param>
        static void Postfix(TIFactionState __instance)
        {
            if (!Main.enabled || Main.Settings is null)
                return; // keep original
            MiningSettings settings = Main.Settings.mineSettings;

            if (needUpdate)
            {
                if (dirtyFactions is null)
                    dirtyFactions = [.. GameStateManager.AllFactions()];
                else
                    dirtyFactions.UnionWith(GameStateManager.AllFactions());
                needUpdate = false;
            }

            if (dirtyFactions?.Contains(__instance) ?? false)
            {
                __instance.SetResourceIncomeDataDirty();
                __instance.habs.ForEach(
                    delegate(TIHabState x)
                    {
                        x.UpdateCurrentAnnualNetResourceIncomes();
                    }
                );
                dirtyFactions.Remove(__instance);
            }
        }
    }

    [Flags]
    public enum TargetGroups
    {
        None = 0,
        Player = 1 << 0,
        Humans = 1 << 1,
        Aliens = 1 << 2,
    }

    internal static class UI
    {
        private readonly struct MineProdSettingsSnapshot(float multiplier, TargetGroups targets)
        {
            internal readonly float Multiplier = multiplier;
            internal readonly TargetGroups Targets = targets;
        }

        private static MineProdSettingsSnapshot MineProdSettingsAtGuiOpen;
        private static bool firstFrame = true;

        /// <summary>
        /// Sub-section for mining tweaks in the mod's settings UI in Unity Mod Manager.
        /// </summary>
        /// <param name="settings">Mining related mod settings.</param>
        /// <param name="context">Context holding default UI styles and helper functions.</param>
        internal static void OnGUI(MiningSettings settings, in SettingsUIContext context)
        {
            if (firstFrame)
            {
                MineProdSettingsAtGuiOpen = new(
                    multiplier: settings.globalMineProductionMultiplier,
                    targets: settings.globalMineProductionMultiplierTargets
                );
                firstFrame = false;
            }

            // box group
            GUILayout.BeginVertical(context.GroupStyle);

            // group label
            GUILayout.Label("Mining tweaks", UnityModManager.UI.h2);

            // TWEAK: linear cost per mine above free cap
            GUILayout.Space(15);
            GUILayout.BeginHorizontal();
            GUILayout.Label("1. Linear cost above free limit (default: off):");
            GUILayout.Space(15);
            settings.linearMineMCCostEnabled = GUILayout.Toggle(
                settings.linearMineMCCostEnabled,
                settings.linearMineMCCostEnabled ? "on" : "off",
                context.ToggleStyle
            );
            GUILayout.FlexibleSpace();
            GUILayout.Label("Cost per mine:");
            float sliderValue = GUILayout.HorizontalSlider(
                settings.linearMCCostPerMine,
                1f,
                15f,
                context.SliderLayout
            );
            settings.linearMCCostPerMine = Mathf.Clamp(Mathf.RoundToInt(sliderValue), 1, 15);
            GUILayout.Label(settings.linearMCCostPerMine.ToString(), context.SliderLabelLayout);
            GUILayout.EndHorizontal();

            // TWEAK: global mine cost multiplier
            GUILayout.Space(15);
            GUILayout.BeginHorizontal();
            GUILayout.Label("2. Global mine cost multiplier (default: 1.0, change to enable):");
            GUILayout.FlexibleSpace();
            sliderValue = GUILayout.HorizontalSlider(
                settings.globalMineMCCostMultiplier,
                0f,
                2f,
                context.SliderLayout
            );
            settings.globalMineMCCostMultiplier = Mathf.Clamp(
                (float)Math.Round(sliderValue, 1),
                0f,
                2f
            );
            GUILayout.Label(
                settings.globalMineMCCostMultiplier.ToString("0.0"),
                context.SliderLabelLayout
            );
            GUILayout.EndHorizontal();

            // TWEAK: global mine productivity multiplier
            GUILayout.Space(15);
            GUILayout.BeginHorizontal();
            GUILayout.Label("3. Mine productivity multiplier (default: 1.0, change to enable):");
            GUILayout.Space(10);
            TargetGroups oldTargets = settings.globalMineProductionMultiplierTargets;
            TargetGroups newTargets = TargetGroups.None;
            if (
                GUILayout.Toggle(
                    (oldTargets & TargetGroups.Player) != 0,
                    "Player",
                    context.ToggleStyle
                )
            )
            {
                newTargets |= TargetGroups.Player;
            }
            GUILayout.Space(5);
            if (
                GUILayout.Toggle(
                    (oldTargets & TargetGroups.Humans) != 0,
                    "Other Humans",
                    context.ToggleStyle
                )
            )
            {
                newTargets |= TargetGroups.Humans;
            }
            GUILayout.Space(5);
            if (
                GUILayout.Toggle(
                    (oldTargets & TargetGroups.Aliens) != 0,
                    "Aliens",
                    context.ToggleStyle
                )
            )
            {
                newTargets |= TargetGroups.Aliens;
            }
            settings.globalMineProductionMultiplierTargets = newTargets;

            GUILayout.FlexibleSpace();
            sliderValue = GUILayout.HorizontalSlider(
                settings.globalMineProductionMultiplier,
                0f,
                10f,
                context.SliderLayout
            );
            settings.globalMineProductionMultiplier = Mathf.Clamp(
                (float)Math.Round(sliderValue, 1),
                0f,
                10f
            );
            GUILayout.Label(
                settings.globalMineProductionMultiplier.ToString("0.0"),
                context.SliderLabelLayout
            );
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        internal static void OnHideGUI(MiningSettings settings)
        {
            if (!firstFrame)
            {
                if (
                    MineProdSettingsAtGuiOpen.Multiplier != settings.globalMineProductionMultiplier
                    || MineProdSettingsAtGuiOpen.Targets
                        != settings.globalMineProductionMultiplierTargets
                )
                    RecalcMineIncomeIfNeededPatch.needUpdate = true;
            }
            firstFrame = true;
        }
    }

    public class MiningSettings : UnityModManager.ModSettings
    {
        public bool linearMineMCCostEnabled = false;
        public int linearMCCostPerMine = 6;
        public float globalMineMCCostMultiplier = 1f;
        public float globalMineProductionMultiplier = 1f;
        public TargetGroups globalMineProductionMultiplierTargets = TargetGroups.None;
    }
}
