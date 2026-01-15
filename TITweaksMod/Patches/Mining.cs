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
            if (!Main.enabled || Main.Settings == null)
                return; // keep original
            Settings settings = Main.Settings.mineSettings;

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
            if (!Main.enabled || Main.Settings == null)
                return; // keep original
            Main.Logger?.Log($"GetCurrentMiningMultiplierFromOrgsAndEffects() - {__instance}");

            Settings settings = Main.Settings.mineSettings;
            if (settings.globalMineProductionMultiplier != 1.0f)
            {
                if (
                    (settings.globalMineProductionMultiplierForPlayer && __instance.isActivePlayer)
                    || (
                        settings.globalMineProductionMultiplierForHumans
                        && __instance.IsActiveHumanFaction
                        && !__instance.isActivePlayer
                    )
                    || (
                        settings.globalMineProductionMultiplierForAliens
                        && __instance.IsAlienFaction
                    )
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
        private static bool firstRun = true;
        static HashSet<TIFactionState>? dirtyFactions;

        static void Postfix(TIFactionState __instance)
        {
            if (!Main.enabled || Main.Settings == null)
                return; // keep original
            Settings settings = Main.Settings.mineSettings;

            if (firstRun)
            {
                needUpdate = false;
                firstRun = false;
            }

            if (needUpdate)
            {
                if (dirtyFactions == null)
                    dirtyFactions = [.. GameStateManager.AllFactions()];
                else
                    dirtyFactions.UnionWith(GameStateManager.AllFactions());
                Main.Logger?.Log("GetYearlyIncome - all dirty");
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
                Main.Logger?.Log($"GetYearlyIncome - setting dirty {__instance}");
                dirtyFactions.Remove(__instance);
            }
        }
    }

    internal static class UI
    {
        private struct MineProdSettingsSnapshot
        {
            internal float multiplier;
            internal bool playerEnabled;
            internal bool humansEnabled;
            internal bool aliensEnabled;
        }

        private static MineProdSettingsSnapshot? MineProdSettingsAtGuiOpen;
        private static bool firstFrame = false;

        internal static void OnGUI(Settings settings, in SettingsUIContext context)
        {
            if (firstFrame || MineProdSettingsAtGuiOpen == null)
            {
                MineProdSettingsAtGuiOpen = new MineProdSettingsSnapshot
                {
                    multiplier = settings.globalMineProductionMultiplier,
                    playerEnabled = settings.globalMineProductionMultiplierForPlayer,
                    humansEnabled = settings.globalMineProductionMultiplierForHumans,
                    aliensEnabled = settings.globalMineProductionMultiplierForAliens,
                };
                firstFrame = false;
            }

            // box group
            GUILayout.BeginVertical(context.groupStyle);

            // group label
            GUILayout.Label("Mining tweaks", UnityModManager.UI.h2);

            // TWEAK: linear cost per mine above free cap
            GUILayout.Space(15);
            GUILayout.BeginHorizontal();
            GUILayout.Label("1. Linear cost above free limit (default: off):");
            GUILayout.Space(15);
            settings.linearMineMCCostEnabled = GUILayout.Toggle(
                settings.linearMineMCCostEnabled,
                "Enable",
                context.toggleStyle
            );
            GUILayout.FlexibleSpace();
            GUILayout.Label("Cost per mine:");
            float sliderValue = GUILayout.HorizontalSlider(
                settings.linearMCCostPerMine,
                1f,
                15f,
                context.sliderLayout
            );
            settings.linearMCCostPerMine = Mathf.Clamp(Mathf.RoundToInt(sliderValue), 1, 15);
            GUILayout.Label(settings.linearMCCostPerMine.ToString(), context.sliderLabelLayout);
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
                context.sliderLayout
            );
            settings.globalMineMCCostMultiplier = Mathf.Clamp(
                (float)Math.Round(sliderValue, 1),
                0f,
                2f
            );
            GUILayout.Label(
                settings.globalMineMCCostMultiplier.ToString("0.0"),
                context.sliderLabelLayout
            );
            GUILayout.EndHorizontal();

            // TWEAK: global mine productivity multiplier
            GUILayout.Space(15);
            GUILayout.BeginHorizontal();
            GUILayout.Label("3. Mine productivity multiplier (default: 1.0, change to enable):");
            GUILayout.Space(15);
            settings.globalMineProductionMultiplierForPlayer = GUILayout.Toggle(
                settings.globalMineProductionMultiplierForPlayer,
                "Player",
                context.toggleStyle
            );
            GUILayout.Space(5);
            settings.globalMineProductionMultiplierForHumans = GUILayout.Toggle(
                settings.globalMineProductionMultiplierForHumans,
                "Other Humans",
                context.toggleStyle
            );
            GUILayout.Space(5);
            settings.globalMineProductionMultiplierForAliens = GUILayout.Toggle(
                settings.globalMineProductionMultiplierForAliens,
                "Aliens",
                context.toggleStyle
            );
            GUILayout.FlexibleSpace();
            sliderValue = GUILayout.HorizontalSlider(
                settings.globalMineProductionMultiplier,
                0f,
                10f,
                context.sliderLayout
            );
            settings.globalMineProductionMultiplier = Mathf.Clamp(
                (float)Math.Round(sliderValue, 1),
                0f,
                10f
            );
            GUILayout.Label(
                settings.globalMineProductionMultiplier.ToString("0.0"),
                context.sliderLabelLayout
            );
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        internal static void OnHideGUI(Settings settings)
        {
            if (MineProdSettingsAtGuiOpen != null)
            {
                if (
                    MineProdSettingsAtGuiOpen.Value.multiplier
                        != settings.globalMineProductionMultiplier
                    || MineProdSettingsAtGuiOpen.Value.playerEnabled
                        != settings.globalMineProductionMultiplierForPlayer
                    || MineProdSettingsAtGuiOpen.Value.humansEnabled
                        != settings.globalMineProductionMultiplierForHumans
                    || MineProdSettingsAtGuiOpen.Value.aliensEnabled
                        != settings.globalMineProductionMultiplierForAliens
                )
                {
                    PeriodicallyCheckMineProductivitySettingsPatch.needUpdate = true;
                }
                MineProdSettingsAtGuiOpen = null;
            }
            firstFrame = true;
        }
    }

    public class Settings : UnityModManager.ModSettings
    {
        public bool linearMineMCCostEnabled = false;
        public int linearMCCostPerMine = 6;
        public float globalMineMCCostMultiplier = 1f;
        public float globalMineProductionMultiplier = 1f;
        public bool globalMineProductionMultiplierForAliens = false;
        public bool globalMineProductionMultiplierForHumans = false;
        public bool globalMineProductionMultiplierForPlayer = false;
    }
}
