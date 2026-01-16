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
        // Prefix needs to capture the original mineNetworkSize parameter, as it has a default value,
        // and that's not passed to the Postfix as such. Postfix actually sees the last value of
        // mineNetworkSize in the original method, which is not the same as the input value.
        // This way the original method is unaffected and runs normally.
        static bool Prefix(out int __state, int mineNetworkSize)
        {
            __state = mineNetworkSize;
            return true;
        }

        // Mine cost tweaks are applied in Postfix - this allows other mods to patch the same function.
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
        static void Postfix(TIFactionState __instance, ref float __result)
        {
            if (!Main.enabled || Main.Settings is null)
                return; // keep original
            //Main.Logger?.Log($"GetCurrentMiningMultiplierFromOrgsAndEffects() - {__instance}");

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
        internal static bool needUpdate = false;
        static HashSet<TIFactionState>? dirtyFactions;

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
                //Main.Logger?.Log("GetYearlyIncome - all dirty");
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
                //Main.Logger?.Log($"GetYearlyIncome - setting dirty {__instance}");
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
            GUILayout.Space(10);
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
            GUILayout.Space(10);
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
