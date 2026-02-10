/// <summary>
/// Production-related (resources, research) patches for Terra Invicta Tweaks Mod
/// </summary>
using HarmonyLib;
using PavonisInteractive.TerraInvicta;
using UnityEngine;
using UnityModManagerNet;

namespace TITweaksMod.EconomyPatches
{
    [HarmonyPatch(
        typeof(TIFactionState),
        nameof(TIFactionState.GetMissionControlRequirementFromMineNetwork)
    )]
    internal static class TIFactionState_GetMissionControlRequirementFromMineNetwork_Patch
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
            if (!Main.enabled || Main.Settings?.economySettings is null)
                return;
            EconomySettings settings = Main.Settings.economySettings;

            // if linear cost is enabled, override original calculation and result
            if (settings.linearMineMCCost_Enabled)
            {
                int mineNetworkSize = __state;
                if (mineNetworkSize < 0)
                    mineNetworkSize = __instance.MineNetworkSize;

                mineNetworkSize -= __instance.SafeMineNextworkSize;
                __result = mineNetworkSize > 0 ? mineNetworkSize * settings.linearMineMCCost : 0;
            }

            // apply global cost multiplier if set
            if (
                settings.globalMineMCCostMultiplier_Enabled
                && settings.globalMineMCCostMultiplier != 1.0f
            )
            {
                __result = Mathf.RoundToInt(__result * settings.globalMineMCCostMultiplier);
            }
        }
    }

    [HarmonyPatch(
        typeof(TIFactionState),
        nameof(TIFactionState.GetCurrentMiningMultiplierFromOrgsAndEffects)
    )]
    internal static class TIFactionState_GetCurrentMiningMultiplierFromOrgsAndEffects_Patch
    {
        /// <summary>
        /// Postfix patch for the in-game TIFactionState.GetCurrentMiningMultiplierFromOrgsAndEffects()
        /// method that applies a multiplier to the mining productivity of selected factions.
        /// </summary>
        /// <param name="__instance">Active game faction.</param>
        /// <param name="__result">Tweaked mining productivity.</param>
        static void Postfix(TIFactionState __instance, ref float __result)
        {
            if (!Main.enabled || Main.Settings?.economySettings is null)
                return;

            EconomySettings settings = Main.Settings.economySettings;
            if (settings.globalMineProductionMultiplier != 1.0f)
            {
                TargetGroups targets = settings.globalMineProductionMultiplier_Targets;
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
    internal static class TIFactionState_GetYearlyIncome_Patch
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
            if (!Main.enabled || Main.Settings?.economySettings is null)
                return; // keep original
            EconomySettings settings = Main.Settings.economySettings;

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

    [HarmonyPatch(typeof(TIGlobalResearchState), nameof(TIGlobalResearchState.Leader))]
    internal static class TIGlobalResearchState_Leader_Patch
    {
        static bool Prefix(TIGlobalResearchState __instance, int slot, ref TIFactionState __result)
        {
            if (!Main.enabled || Main.Settings?.economySettings is null)
                return true;

            EconomySettings settings = Main.Settings.economySettings;

            // Skip if tweak disabled or tech isn't complete
            if (
                !settings.alwaysLeadResearch_Enabled
                || __instance.GetTechProgress(slot).remainingResearch > 0
            )
                return true;

            // playerFaction should never be null, but check it just in case
            TIFactionState? playerFaction = GameStateManager
                .AllFactions()
                .FirstOrDefault(f => f.isActivePlayer);
            if (playerFaction is null)
                return true;

            __result = playerFaction;
            return false;
        }
    }

    internal static class ResearchManager
    {
        private static TechProgress?[] globalResearch = { null, null, null };
        private static ProjectProgress?[] projectResearch = { null, null, null };
        private static TIFactionState? playerFaction = null;
        internal static string?[] globalResearchLabels { get; private set; } = { null, null, null };
        internal static string?[] projectResearchLabels { get; private set; } =
            { null, null, null };
        internal static bool anyResearch =>
            globalResearch[0] is not null
            || globalResearch[1] is not null
            || globalResearch[2] is not null;
        internal static bool anyProject =>
            projectResearch[0] is not null
            || projectResearch[1] is not null
            || projectResearch[2] is not null;

        internal static void completeResearch(int slot)
        {
            if (slot < 0 || slot > 2 || playerFaction is null || globalResearch[slot] is null)
                return;
            TechProgress tech = globalResearch[slot]!;
            TIGlobalResearchState? tIGlobalResearchState = GameStateManager.GlobalResearch();
            if (tIGlobalResearchState is null || tech.remainingResearch == 0)
                return;
            tIGlobalResearchState.AddResearchToTech(
                slot,
                tech.remainingResearch + 1,
                playerFaction
            );
            Update();
        }

        internal static void completeProject(int slot)
        {
            if (slot < 0 || slot > 2 || playerFaction is null || projectResearch[slot] is null)
                return;
            ProjectProgress project = projectResearch[slot]!;
            if (project.SufficientResearchAccumulated(playerFaction) || project.completed)
                return;
            var remainingRP =
                project.projectTemplate.GetResearchCost(playerFaction)
                - project.accumulatedResearch
                + 1;
            playerFaction.AddResearchToProject(slot + 3, remainingRP);
            Update();
        }

        internal static void Update()
        {
            TIGlobalResearchState? tIGlobalResearchState = GameStateManager.GlobalResearch();
            playerFaction = GameStateManager.AllFactions().FirstOrDefault(f => f.isActivePlayer);
            if (tIGlobalResearchState is null || playerFaction is null)
            {
                for (int i = 0; i <= 2; i++)
                {
                    globalResearch[i] = null;
                    projectResearch[i] = null;
                    globalResearchLabels[i] = null;
                    projectResearchLabels[i] = null;
                }
            }
            else
            {
                for (int i = 0; i <= 2; i++)
                {
                    TechProgress tech = tIGlobalResearchState.GetTechProgress(i);
                    TITechTemplate? template = tech.techTemplate;
                    if (
                        template is not null
                        && tech.remainingResearch > 0
                        && tech.accumulatedResearch > 0
                    )
                    {
                        globalResearch[i] = tech;
                        globalResearchLabels[i] = template.displayName;
                    }
                    else
                    {
                        globalResearch[i] = null;
                        globalResearchLabels[i] = null;
                    }

                    ProjectProgress? project = playerFaction.GetProjectProgressInSlot(i + 3);
                    if (
                        project is not null
                        && !project.SufficientResearchAccumulated(playerFaction)
                        && !project.completed
                    )
                    {
                        projectResearch[i] = project;
                        projectResearchLabels[i] = project.projectTemplate.displayName;
                    }
                    else
                    {
                        projectResearch[i] = null;
                        projectResearchLabels[i] = null;
                    }
                }
            }
        }
    }

    internal static class ResourceManager
    {
        internal static void AddResource(Resource resource, int amount)
        {
            FactionResource res = resource switch
            {
                Resource.Money => FactionResource.Money,
                Resource.Influence => FactionResource.Influence,
                Resource.Operations => FactionResource.Operations,
                Resource.Research => FactionResource.Research,
                Resource.Boost => FactionResource.Boost,
                Resource.Water => FactionResource.Water,
                Resource.Volatiles => FactionResource.Volatiles,
                Resource.Metals => FactionResource.Metals,
                Resource.NobleMetals => FactionResource.NobleMetals,
                Resource.Fissiles => FactionResource.Fissiles,
                Resource.Antimatter => FactionResource.Antimatter,
                Resource.Exotics => FactionResource.Exotics,
                _ => FactionResource.None,
            };
            var playerFaction = GameStateManager
                .AllFactions()
                .FirstOrDefault(f => f.isActivePlayer);
            if (res != FactionResource.None && playerFaction is not null && amount != 0)
                playerFaction.AddToCurrentResource(amount, res);
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

    public enum Resource
    {
        Money,
        Influence,
        Operations,
        Boost,
        Research,
        Water,
        Volatiles,
        Metals,
        NobleMetals,
        Fissiles,
        Antimatter,
        Exotics,
    }

    internal static class UI
    {
        private static readonly string[] resourceLabels = Enum.GetNames(typeof(Resource));

        private readonly struct MineProdSettingsSnapshot(float multiplier, TargetGroups targets)
        {
            internal readonly float Multiplier = multiplier;
            internal readonly TargetGroups Targets = targets;
        }

        private static MineProdSettingsSnapshot MineProdSettingsAtGuiOpen;
        private static bool firstOnGUI = true;
        private static Resource selectedResource = Resource.Money;

        internal static void OnGUI(
            EconomySettings settings,
            in SettingsUIContext context,
            bool show
        )
        {
            if (firstOnGUI)
            {
                firstOnGUI = false;
                MineProdSettingsAtGuiOpen = new(
                    multiplier: settings.globalMineProductionMultiplier,
                    targets: settings.globalMineProductionMultiplier_Targets
                );
                ResearchManager.Update();
            }

            if (show)
            {
                // box group
                GUILayout.BeginVertical(context.GroupStyle);

                // group label
                GUILayout.Label("Economy / Research", UnityModManager.UI.h2);

                // TWEAK: linear cost per mine above free cap
                GUILayout.Space(15);
                GUILayout.BeginHorizontal();
                GUILayout.Label("1. Linear mine MC cost above free limit:");
                GUILayout.Space(10);
                settings.linearMineMCCost_Enabled = context.OnOffToggle(
                    settings.linearMineMCCost_Enabled
                );
                GUILayout.FlexibleSpace();
                GUILayout.Label("Cost per mine:");
                settings.linearMineMCCost = context.IntHorizontalSlider(
                    settings.linearMineMCCost,
                    1,
                    15
                );
                GUILayout.EndHorizontal();

                // TWEAK: global mine cost multiplier
                GUILayout.Space(15);
                GUILayout.BeginHorizontal();
                GUILayout.Label("2. Global mine MC cost multiplier:");
                GUILayout.Space(10);
                settings.globalMineMCCostMultiplier_Enabled = context.OnOffToggle(
                    settings.globalMineMCCostMultiplier_Enabled
                );
                GUILayout.FlexibleSpace();
                settings.globalMineMCCostMultiplier = context.FloatHorizontalSlider(
                    settings.globalMineMCCostMultiplier,
                    0f,
                    2f,
                    1f
                );
                GUILayout.EndHorizontal();

                // TWEAK: global mine productivity multiplier
                GUILayout.Space(15);
                GUILayout.BeginHorizontal();
                GUILayout.Label("3. Mine productivity multiplier:");
                GUILayout.Space(10);
                TargetGroups oldTargets = settings.globalMineProductionMultiplier_Targets;
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
                settings.globalMineProductionMultiplier_Targets = newTargets;
                GUILayout.FlexibleSpace();
                settings.globalMineProductionMultiplier = context.FloatHorizontalSlider(
                    settings.globalMineProductionMultiplier,
                    0f,
                    10f,
                    1f
                );
                GUILayout.EndHorizontal();

                // TWEAK: always treat player faction as highest contributor to finished research
                GUILayout.Space(15);
                GUILayout.BeginHorizontal();
                GUILayout.Label(
                    "4. Always treat player faction as highest contributor to finished research:"
                );
                GUILayout.Space(10);
                settings.alwaysLeadResearch_Enabled = context.OnOffToggle(
                    settings.alwaysLeadResearch_Enabled
                );
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                // TWEAK: instant complete research by adding player research contribution
                GUILayout.Space(15);
                GUILayout.Label("5. Complete research:");
                GUILayout.Space(10);
                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                GUILayout.BeginVertical();
                {
                    // Global Research
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Global Reserarch:");
                    GUILayout.Space(10);
                    if (ResearchManager.anyResearch)
                    {
                        for (int i = 0; i < ResearchManager.globalResearchLabels.Length; i++)
                        {
                            if (i > 0)
                                GUILayout.Space(5);
                            var label = ResearchManager.globalResearchLabels[i];
                            if (label is null)
                                GUI.enabled = false;
                            if (GUILayout.Button(label ?? "", GUILayout.MinWidth(150f)))
                                ResearchManager.completeResearch(i);
                            if (label is null)
                                GUI.enabled = true;
                        }
                    }
                    else
                        GUILayout.Label("no active research", context.redLabel);
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                    GUILayout.Space(10);
                    GUILayout.Label(
                        "Only uncompleted global research with at least 1 point progress is shown here."
                    );

                    // Faction Projects
                    GUILayout.Space(10);
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Faction Projects:");
                    GUILayout.Space(10);
                    if (ResearchManager.anyProject)
                    {
                        for (int i = 0; i < ResearchManager.projectResearchLabels.Length; i++)
                        {
                            if (i > 0)
                                GUILayout.Space(5);
                            var label = ResearchManager.projectResearchLabels[i];
                            if (label is null)
                                GUI.enabled = false;
                            if (GUILayout.Button(label ?? "", GUILayout.MinWidth(150f)))
                                ResearchManager.completeProject(i);
                            if (label is null)
                                GUI.enabled = true;
                        }
                    }
                    else
                        GUILayout.Label("no active project", context.redLabel);
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();

                // TWEAK: add resources
                GUILayout.Space(15);
                GUILayout.Label("6. Add resources:");
                GUILayout.Space(10);
                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                GUILayout.BeginVertical();
                {
                    selectedResource = (Resource)
                        GUILayout.SelectionGrid(
                            (int)selectedResource,
                            resourceLabels,
                            5,
                            context.ToolbarStyle
                        );
                    GUILayout.Space(10);

                    GUILayout.BeginHorizontal();
                    int amount = 0;
                    GUILayout.Space(5);
                    if (GUILayout.Button("+10"))
                        amount += 10;
                    GUILayout.Space(5);
                    if (GUILayout.Button("+100"))
                        amount += 100;
                    GUILayout.Space(5);
                    if (GUILayout.Button("+1000"))
                        amount += 1000;
                    GUILayout.Space(5);
                    if (GUILayout.Button("+10000"))
                        amount += 10000;
                    GUILayout.EndHorizontal();
                    ResourceManager.AddResource(selectedResource, amount);
                }
                GUILayout.EndVertical();
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.EndVertical();
            }
        }

        internal static void OnHideGUI(EconomySettings settings)
        {
            if (!firstOnGUI)
            {
                // Indicate need for recalculation of mine production values if settings have changed
                if (
                    MineProdSettingsAtGuiOpen.Multiplier != settings.globalMineProductionMultiplier
                    || MineProdSettingsAtGuiOpen.Targets
                        != settings.globalMineProductionMultiplier_Targets
                )
                    TIFactionState_GetYearlyIncome_Patch.needUpdate = true;
            }
            firstOnGUI = true;
        }
    }

    public class EconomySettings : UnityModManager.ModSettings
    {
        public bool linearMineMCCost_Enabled = false;
        public int linearMineMCCost = 6;
        public bool globalMineMCCostMultiplier_Enabled = false;
        public float globalMineMCCostMultiplier = 1f;
        public TargetGroups globalMineProductionMultiplier_Targets = TargetGroups.None;
        public float globalMineProductionMultiplier = 1f;
        public bool alwaysLeadResearch_Enabled = false;
    }
}
