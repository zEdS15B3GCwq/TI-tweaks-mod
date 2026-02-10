using HarmonyLib;
using PavonisInteractive.TerraInvicta;
using UnityEngine;
using UnityModManagerNet;

/// <summary>
/// Patches targeting TINationState, i.e. nation-level properties
///
/// 1. Unrest and cohesion
///
/// Unrest and cohesion both have a "rest state" value, which the actual value tends towards over time.
/// These patches allow shifting the base rest state by a configurable offset. Changes take effect
/// immediately, and are shown in the detailed breakdown tooltips.
///
/// Note: base unrest/cohesion values are constants used in the calculation of the rest state. At
///       first, I considered patching those, but the constants are baked-in into the code, and it
///       would've required IL-level patching, which is more complex and more susceptible to code
///       changes.
///
/// Relevant in-game methods:
///     - TINationState.unrestRestState (getter): rest state for unrest, clamped 0-10.
///                                               Patched to add an offset, clamped.
///     - TINationState.unrestRestState_unclamped (getter): rest state for unrest, unclamped
///                                                         Patched to add an offset.
///     - TINationState.cohesionRestState (getter): rest state for cohesion, clamped 0-10
///                                                 Patched to add an offset, clamped.
///     - TINationState.unrestRestStateDetail (getter): detailed unrest rest state breakdown (not patched)
///     - TINationState.CohesionRestStateDetail (getter): detailed cohesion rest state breakdown (not patched)
///
/// 2. Hostile claims from democracy difference and innate hostility
///
/// In principle, unifying two nations is allowed if one has a claim on the other's capital, and they
/// have been in a federation long enough. However, a hostile claim can prevent unification in this way.
/// Claims can be hostile either due to being innately so, or due to the absorber country having a
/// much lower democracy score than the other. Plus there is at least one special case with breakaway
/// regions where they cannot join a federation, but can unify when both nations are controlled.
///
/// The democracy score difference limit can be set in TIGlobalConfig.json, so there is not reason to
/// patch just that in game code. The patches below allow ignoring hostile claims from either source.
///
/// Relevant in-game methods:
///     - TINationState.ClaimWillBeHostile: true if claim is not hostile due to democracy or innate hostility.
///                                         The alien nation's claims are always not hostile (false).
///                                         Patched to return false.
///     - TINationState.candidateUnifications (getter): list of unification candidates, uses ClaimWillBeHostile. (not patched)
///     - TINationState.eligibleUnifications (getter): list of allowed unifications, uses ClaimWillBeHostile. (not patched)
///     - TINationState.CanUnifyFeedback: textual details about all factors affecting unification (not patched)
///     - TINationState.CanImproveRelationsYet: true if relations not on cooldown, blocks diplomatic actions.
///                                             Ignores unification requirements for federation / alliance duration
///                                             if patched to return true.
///     - TINationState.MyClaimOnOtherCapital: true if this nation has a claim on the capital of another nation.
///                                            If patched to always return true, this allows unification with all
///                                            federation members.
/// </summary>
namespace TITweaksMod.NationPatches
{
    [HarmonyPatch(typeof(TINationState))]
    [HarmonyPatch(nameof(TINationState.unrestRestState), MethodType.Getter)]
    internal static class TINationState_unrestRestState_Patch
    {
        internal static void Postfix(TINationState __instance, ref float __result)
        {
            if (!Main.enabled || Main.Settings?.nationSettings is null)
                return;

            NationSettings settings = Main.Settings.nationSettings;

            if (settings.unrestOffset_Enable && settings.unrestOffset != 0 && __instance.extant)
                __result = Mathf.Clamp(__result + settings.unrestOffset, 0f, 10f);
        }
    }

    [HarmonyPatch(typeof(TINationState))]
    [HarmonyPatch(nameof(TINationState.unrestRestState_unclamped), MethodType.Getter)]
    internal static class TINationState_unrestRestState_unclamped_Patch
    {
        internal static void Postfix(TINationState __instance, ref float __result)
        {
            if (!Main.enabled || Main.Settings?.nationSettings is null)
                return;

            NationSettings settings = Main.Settings.nationSettings;

            if (settings.unrestOffset_Enable && settings.unrestOffset != 0 && __instance.extant)
                __result = __result + settings.unrestOffset;
        }
    }

    [HarmonyPatch(typeof(TINationState))]
    [HarmonyPatch(nameof(TINationState.cohesionRestState), MethodType.Getter)]
    internal static class TINationState_cohesionRestState_Patch
    {
        internal static void Postfix(TINationState __instance, ref float __result)
        {
            if (!Main.enabled || Main.Settings?.nationSettings is null)
                return;

            NationSettings settings = Main.Settings.nationSettings;

            if (settings.cohesionOffset_Enable && settings.cohesionOffset != 0 && __instance.extant)
                __result = Mathf.Clamp(__result + settings.cohesionOffset, 0f, 10f);
        }
    }

    [HarmonyPatch(typeof(TINationState), nameof(TINationState.ClaimWillBeHostile))]
    internal static class TINationState_ClaimWillBeHostile_Patch
    {
        internal static void Postfix(TINationState __instance, ref bool __result)
        {
            if (!Main.enabled || Main.Settings?.nationSettings is null)
                return;
            NationSettings settings = Main.Settings.nationSettings;

            if (__instance.executiveFaction is null)
                return;

            if (
                settings.ignoreHostileClaims == targetsOffPlayerGlobal.All
                || (
                    settings.ignoreHostileClaims == targetsOffPlayerGlobal.PlayerOnly
                    && __instance.executiveFaction.isActivePlayer
                )
            )
                __result = false;
        }
    }

    [HarmonyPatch(typeof(TINationState), nameof(TINationState.CanImproveRelationsYet))]
    internal static class TINationState_CanImproveRelationsYet_Patch
    {
        internal static void Postfix(TINationState __instance, ref bool __result)
        {
            if (!Main.enabled || Main.Settings?.nationSettings is null)
                return;
            NationSettings settings = Main.Settings.nationSettings;

            if (__instance.executiveFaction is null)
                return;

            if (
                settings.ignoreDiploCooldowns == targetsOffPlayerGlobal.All
                || (
                    settings.ignoreDiploCooldowns == targetsOffPlayerGlobal.PlayerOnly
                    && __instance.executiveFaction.isActivePlayer
                )
            )
                __result = true;
        }
    }

    [HarmonyPatch(typeof(TINationState), nameof(TINationState.MyClaimOnOtherCapital))]
    internal static class TINationState_MyClaimOnOtherCapital_Patch
    {
        internal static void Postfix(TINationState __instance, ref bool __result)
        {
            if (!Main.enabled || Main.Settings?.nationSettings is null)
                return;
            NationSettings settings = Main.Settings.nationSettings;

            if (__instance.executiveFaction is null)
                return;

            if (
                settings.claimAllCapitals == targetsOffPlayerGlobal.All
                || (
                    settings.claimAllCapitals == targetsOffPlayerGlobal.PlayerOnly
                    && __instance.executiveFaction.isActivePlayer
                )
            )
                __result = true;
        }
    }

    [HarmonyPatch(typeof(TIFactionState), nameof(TIFactionState.GetMonthlyIncomeFromHQ))]
    internal static class TINationState_GetMonthlyIncomeFromHQ_Patch
    {
        internal static void Postfix(
            TIFactionState __instance,
            ref float __result,
            FactionResource resource
        )
        {
            if (!Main.enabled || Main.Settings is null || resource != FactionResource.Influence)
                return;

            var settings = Main.Settings.nationSettings.influenceDrainSettings;

            if (!settings[__instance.templateName])
                return;

            var oldResult = __result;
            __result -= 2000;
            __instance.SetResourceIncomeDataDirty(resource);
        }
    }

    [HarmonyPatch(typeof(TIFactionState), nameof(TIFactionState.GetDailyIncomeFromHQ))]
    internal static class TINationState_GetDailyIncomeFromHQ_Patch
    {
        internal static void Postfix(
            TIFactionState __instance,
            ref float __result,
            FactionResource resourceType
        )
        {
            if (!Main.enabled || Main.Settings is null || resourceType != FactionResource.Influence)
                return;

            var settings = Main.Settings.nationSettings.influenceDrainSettings;

            if (!settings[__instance.templateName])
                return;

            var oldResult = __result;
            __result -= 66;
            __instance.SetResourceIncomeDataDirty(resourceType);
        }
    }

    [HarmonyPatch(typeof(TIFactionState), nameof(TIFactionState.GetYearlyIncomeFromHQ))]
    internal static class TINationState_GetYearlyIncomeFromHQ_Patch
    {
        internal static void Postfix(
            TIFactionState __instance,
            ref float __result,
            FactionResource resourceType
        )
        {
            if (!Main.enabled || Main.Settings is null || resourceType != FactionResource.Influence)
                return;

            var settings = Main.Settings.nationSettings.influenceDrainSettings;

            if (!settings[__instance.templateName])
                return;

            var oldResult = __result;
            __result -= 24000;
            __instance.SetResourceIncomeDataDirty(resourceType);
        }
    }

    internal static class NationManager
    {
        internal static TIFactionState[] factions { get; private set; } = [];
        internal static string[] factionLabels { get; private set; } = [];
        internal static float[,] hateMatrix { get; private set; } = new float[0, 0];
        internal static float[,] minimumHateMatrix { get; private set; } = new float[0, 0];
        internal static float[,] maximumHateMatrix { get; private set; } = new float[0, 0];
        internal static float[] MCBasedHate { get; private set; } = [];
        internal static readonly string[] factionTemplateNames =
        [
            "ResistCouncil",
            "DestroyCouncil",
            "ExploitCouncil",
            "SubmitCouncil",
            "AppeaseCouncil",
            "CooperateCouncil",
            "EscapeCouncil",
            "AlienCouncil",
        ];
        internal static string[] factionTemplateDisplayNames = new string[
            factionTemplateNames.Length
        ];

        //PlayerFaction = GameStateManager.AllFactions().FirstOrDefault(x => x.isActivePlayer);
        //enemyFactions = GameStateManager.AllFactions().Where(f => !f.isActivePlayer).ToArray();

        private static void UpdateHateMatrices()
        {
            var size = factions.Length;
            if (hateMatrix.GetLength(0) != size || hateMatrix.GetLength(1) != size)
                hateMatrix = new float[size, size];
            if (minimumHateMatrix.GetLength(0) != size || minimumHateMatrix.GetLength(1) != size)
                minimumHateMatrix = new float[size, size];
            if (maximumHateMatrix.GetLength(0) != size || maximumHateMatrix.GetLength(1) != size)
                maximumHateMatrix = new float[size, size];
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    if (i == j)
                    {
                        hateMatrix[i, j] = 0f;
                        minimumHateMatrix[i, j] = 0f;
                        maximumHateMatrix[i, j] = 0f;
                    }
                    else
                    {
                        hateMatrix[i, j] = factions[i].GetFactionHate(factions[j]);
                        minimumHateMatrix[i, j] = factions[i].MinimumFactionHate(factions[j]);
                        maximumHateMatrix[i, j] = factions[i].MaximumFactionHate(factions[j]);
                    }
                }
            }
        }

        private static void UpdateMCHate()
        {
            var size = factions.Length;
            if (MCBasedHate.Length != size)
                MCBasedHate = new float[size];
            if (size > 0)
            {
                var aliens = factions.First(c => c.IsAlienFaction);
                for (int i = 0; i < size; i++)
                    MCBasedHate[i] = aliens.MCBasedAlienHate(factions[i]);
            }
        }

        internal static void Update()
        {
            factions = GameStateManager.AllFactions().ToArray();
            factionLabels = factions.Select(f => f.displayName).ToArray();

            for (int i = 0; i < factionTemplateNames.Length; i++)
            {
                factionTemplateDisplayNames[i] =
                    factions
                        .FirstOrDefault(f => f.templateName == factionTemplateNames[i])
                        ?.displayName
                    ?? "n/a";
            }

            UpdateHateMatrices();
            UpdateMCHate();
        }

        internal static void SetHate(int factionIndex, int enemyIndex, float newHate)
        {
            if (
                factionIndex == enemyIndex
                || factionIndex < 0
                || factionIndex >= hateMatrix.GetLength(0)
                || enemyIndex < 0
                || enemyIndex >= hateMatrix.GetLength(1)
            )
                return;
            //newHate = Mathf.Clamp(newHate, minimumHateMatrix[factionIndex, enemyIndex], maximumHateMatrix[factionIndex, enemyIndex]);
            if (newHate < 0)
                newHate = 0;
            hateMatrix[factionIndex, enemyIndex] = newHate;
            factions[factionIndex].SetFactionHate(factions[enemyIndex], newHate);
        }

        internal static void BurnResourceStores(TIFactionState faction)
        {
            FactionResource[] targetResources =
            [
                FactionResource.Money,
                FactionResource.Operations,
                FactionResource.Influence,
                FactionResource.Boost,
                FactionResource.Water,
                FactionResource.Volatiles,
                FactionResource.Metals,
                FactionResource.NobleMetals,
                FactionResource.Fissiles,
                FactionResource.Exotics,
                FactionResource.Antimatter,
            ];
            foreach (var resource in targetResources)
                if (faction.resources.ContainsKey(resource))
                    faction.resources[resource] = 0f;
        }
    }

    public enum targetsOffPlayerGlobal
    {
        Off = 0,
        PlayerOnly = 1,
        All = 2,
    }

    internal static class UI
    {
        private static bool firstOnGUI = true;
        internal static string[] targetOffPlayerGlobalLabels = ["Off", "Player", "Global"];
        internal static (int, int) hateMatrixSelected = default;
        internal static int selectedFaction = 0;

        internal static void OnGUI(NationSettings settings, in SettingsUIContext context, bool show)
        {
            if (firstOnGUI)
            {
                firstOnGUI = false;
                NationManager.Update();
                if (hateMatrixSelected == default && NationManager.hateMatrix.GetLength(0) > 1)
                    hateMatrixSelected = (NationManager.hateMatrix.GetLength(0) - 1, 0);
            }

            if (show)
            {
                // group box
                GUILayout.BeginVertical(context.GroupStyle);
                {
                    // group label
                    GUILayout.Label("Nation / Diplomacy", UnityModManager.UI.h2);

                    // TWEAK: shift base unrest
                    GUILayout.Space(15);
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("1. Unrest rest state offset:");
                    GUILayout.Space(5);
                    settings.unrestOffset_Enable = context.OnOffToggle(
                        settings.unrestOffset_Enable
                    );
                    GUILayout.FlexibleSpace();
                    settings.unrestOffset = context.FloatHorizontalSlider(
                        settings.unrestOffset,
                        -10f,
                        10f,
                        0f,
                        context.WideSliderLayout
                    );
                    GUILayout.EndHorizontal();

                    // TWEAK: shift base cohesion
                    GUILayout.Space(15);
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("2. Cohesion rest state offset:");
                    GUILayout.Space(5);
                    settings.cohesionOffset_Enable = context.OnOffToggle(
                        settings.cohesionOffset_Enable
                    );
                    GUILayout.FlexibleSpace();
                    settings.cohesionOffset = context.FloatHorizontalSlider(
                        settings.cohesionOffset,
                        -10f,
                        10f,
                        0f,
                        context.WideSliderLayout
                    );
                    GUILayout.EndHorizontal();

                    // TWEAK: ignore hostile claims
                    GUILayout.Space(15);
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("3. All claims are non-hostile:");
                    GUILayout.FlexibleSpace();
                    settings.ignoreHostileClaims = (targetsOffPlayerGlobal)
                        GUILayout.Toolbar(
                            (int)settings.ignoreHostileClaims,
                            targetOffPlayerGlobalLabels,
                            context.ToolbarStyle
                        );
                    GUILayout.EndHorizontal();

                    // TWEAK: ignore diplomatic cooldowns
                    GUILayout.Space(15);
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("4. Ignore diplomatic cooldowns:");
                    GUILayout.FlexibleSpace();
                    settings.ignoreDiploCooldowns = (targetsOffPlayerGlobal)
                        GUILayout.Toolbar(
                            (int)settings.ignoreDiploCooldowns,
                            targetOffPlayerGlobalLabels,
                            context.ToolbarStyle
                        );
                    GUILayout.EndHorizontal();

                    // TWEAK: claim on all capitals
                    GUILayout.Space(15);
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("5. Claim on all capitals:");
                    GUILayout.FlexibleSpace();
                    settings.claimAllCapitals = (targetsOffPlayerGlobal)
                        GUILayout.Toolbar(
                            (int)settings.claimAllCapitals,
                            targetOffPlayerGlobalLabels,
                            context.ToolbarStyle
                        );
                    GUILayout.EndHorizontal();

                    var nFactions = NationManager.factionLabels.Length;

                    // INFO: MC-based alien hate
                    GUILayout.Space(15);
                    GUILayout.Label("6. MC-based Alien Hate:");
                    GUILayout.Space(10);
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    if (nFactions > 0)
                    {
                        var width = GUILayout.Width(200f);
                        var centered = new GUIStyle(GUI.skin.label)
                        {
                            alignment = TextAnchor.MiddleCenter,
                        };
                        for (int i = 0; i < NationManager.factionLabels.Length; i++)
                        {
                            GUILayout.BeginVertical();
                            GUILayout.Label(NationManager.factionLabels[i], centered, width);
                            GUILayout.Label(
                                NationManager.MCBasedHate[i].ToString("0.0"),
                                centered,
                                width
                            );
                            GUILayout.EndVertical();
                        }
                        GUILayout.FlexibleSpace();
                    }
                    else
                        GUILayout.Label("No factions", context.redLabel);
                    GUILayout.EndHorizontal();

                    // TWEAK: set faction hate
                    GUILayout.Space(15);
                    GUILayout.Label("7. Faction Hate:");
                    GUILayout.Space(10);
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    if (nFactions > 0)
                    {
                        GUILayout.BeginVertical();
                        {
                            string[] columnLabels =
                            [
                                "faction \\ enemy",
                                .. NationManager.factionLabels,
                            ];
                            hateMatrixSelected = context.Matrix(
                                rows: nFactions,
                                cols: nFactions,
                                columnLabels: columnLabels,
                                rowLabels: NationManager.factionLabels,
                                labelFor: (r, c) => NationManager.hateMatrix[r, c].ToString("0.0"),
                                selected: hateMatrixSelected,
                                isEnabled: (r, c) => r != c
                            );
                            var (r, c) = hateMatrixSelected;
                            var currentHate = NationManager.hateMatrix[r, c];
                            var newHate = currentHate;

                            GUILayout.Space(15);
                            GUILayout.BeginHorizontal();
                            GUILayout.Label(
                                $"Hate from \"{NationManager.factionLabels[r]}\" towards \"{NationManager.factionLabels[c]}\" "
                                    + $"(min: {NationManager.minimumHateMatrix[r, c]}, max: {NationManager.maximumHateMatrix[r, c]}):"
                            );
                            GUILayout.Space(10);
                            GUILayout.Label($"{newHate:0.0}", context.yellowLabel);
                            GUILayout.FlexibleSpace();
                            GUILayout.EndHorizontal();

                            GUILayout.Space(10);
                            GUILayout.BeginHorizontal();
                            if (GUILayout.Button("Set to 0"))
                                newHate = 0;
                            GUILayout.Space(5);
                            if (GUILayout.Button("Set to minimum"))
                                newHate = NationManager.minimumHateMatrix[r, c];
                            GUILayout.Space(5);
                            if (GUILayout.Button("-100"))
                                newHate -= 100;
                            GUILayout.Space(5);
                            if (GUILayout.Button("-10"))
                                newHate -= 10;
                            GUILayout.Space(5);
                            if (GUILayout.Button("+10"))
                                newHate += 10;
                            GUILayout.Space(5);
                            if (GUILayout.Button("+100"))
                                newHate += 100;
                            GUILayout.FlexibleSpace();
                            GUILayout.EndHorizontal();

                            if (newHate != currentHate)
                                NationManager.SetHate(r, c, newHate);
                        }
                        GUILayout.EndVertical();
                    }
                    else
                    {
                        GUILayout.Label("No factions", context.redLabel);
                    }
                    GUILayout.EndHorizontal();

                    // TWEAK: set nation resources
                    GUILayout.Space(15);
                    GUILayout.Label("8. Empty resource stores:");
                    GUILayout.Space(10);
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    if (nFactions > 0)
                    {
                        GUILayout.BeginVertical();

                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Select faction:");
                        GUILayout.Space(5);
                        selectedFaction = GUILayout.SelectionGrid(
                            selectedFaction,
                            NationManager.factionLabels,
                            4,
                            context.ToolbarStyle
                        );
                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();

                        GUILayout.Space(15);
                        GUILayout.BeginHorizontal();
                        if (GUILayout.Button("Burn Resources"))
                            NationManager.BurnResourceStores(
                                NationManager.factions[selectedFaction]
                            );
                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();

                        GUILayout.EndVertical();
                    }
                    else
                        GUILayout.Label("No factions", context.redLabel);
                    GUILayout.EndHorizontal();

                    // TWEAK: influence drain
                    GUILayout.Space(15);
                    GUILayout.Label("9. Enable substantial influence drain for faction:");
                    GUILayout.Space(15);
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    GUILayout.BeginVertical();
                    {
                        GUILayout.Label(
                            "This setting will only apply to a faction if that faction is present in the current game."
                        );

                        GUILayout.Space(15);

                        GUILayout.BeginHorizontal();
                        for (int i = 0, j = 0; i < NationManager.factionTemplateNames.Length; i++)
                        {
                            if (j > 0)
                                GUILayout.Space(5);
                            if (j == 3)
                            {
                                GUILayout.EndHorizontal();
                                GUILayout.Space(5);
                                GUILayout.BeginHorizontal();
                                j = 0;
                            }
                            var name = NationManager.factionTemplateNames[i];
                            settings.influenceDrainSettings[name] = GUILayout.Toggle(
                                settings.influenceDrainSettings[name],
                                $"{NationManager.factionTemplateDisplayNames[i]} ({name})",
                                context.ToggleStyle
                            );
                            j++;
                        }
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndVertical();
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
            }
        }

        internal static void OnHideGUI()
        {
            firstOnGUI = true;
        }
    }

    public class InfluenceDrainSettings
    {
        public bool ResistCouncil_Enabled = false;
        public bool DestroyCouncil_Enabled = false;
        public bool ExploitCouncil_Enabled = false;
        public bool SubmitCouncil_Enabled = false;
        public bool AppeaseCouncil_Enabled = false;
        public bool CooperateCouncil_Enabled = false;
        public bool EscapeCouncil_Enabled = false;
        public bool AlienCouncil_Enabled = false;

        public bool this[string name]
        {
            get =>
                name switch
                {
                    "ResistCouncil" => ResistCouncil_Enabled,
                    "DestroyCouncil" => DestroyCouncil_Enabled,
                    "ExploitCouncil" => ExploitCouncil_Enabled,
                    "SubmitCouncil" => SubmitCouncil_Enabled,
                    "AppeaseCouncil" => AppeaseCouncil_Enabled,
                    "CooperateCouncil" => CooperateCouncil_Enabled,
                    "EscapeCouncil" => EscapeCouncil_Enabled,
                    "AlienCouncil" => AlienCouncil_Enabled,
                    _ => false,
                };
            set
            {
                switch (name)
                {
                    case "ResistCouncil":
                        ResistCouncil_Enabled = value;
                        break;
                    case "DestroyCouncil":
                        DestroyCouncil_Enabled = value;
                        break;
                    case "ExploitCouncil":
                        ExploitCouncil_Enabled = value;
                        break;
                    case "SubmitCouncil":
                        SubmitCouncil_Enabled = value;
                        break;
                    case "AppeaseCouncil":
                        AppeaseCouncil_Enabled = value;
                        break;
                    case "CooperateCouncil":
                        CooperateCouncil_Enabled = value;
                        break;
                    case "EscapeCouncil":
                        EscapeCouncil_Enabled = value;
                        break;
                    case "AlienCouncil":
                        AlienCouncil_Enabled = value;
                        break;
                }
            }
        }
    }

    public class NationSettings : UnityModManager.ModSettings
    {
        public bool unrestOffset_Enable = false;
        public float unrestOffset = 0f;
        public bool cohesionOffset_Enable = false;
        public float cohesionOffset = 0f;
        public targetsOffPlayerGlobal ignoreHostileClaims = targetsOffPlayerGlobal.Off;
        public targetsOffPlayerGlobal ignoreDiploCooldowns = targetsOffPlayerGlobal.Off;
        public targetsOffPlayerGlobal claimAllCapitals = targetsOffPlayerGlobal.Off;
        public InfluenceDrainSettings influenceDrainSettings = new InfluenceDrainSettings();
    }
}
