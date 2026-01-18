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
            if (!Main.enabled || Main.Settings is null)
                return;

            NationSettings settings = Main.Settings.nationSettings;

            if (settings.unrestOffset != 0 && __instance.extant)
                __result = Mathf.Clamp(__result + settings.unrestOffset, 0f, 10f);
        }
    }

    [HarmonyPatch(typeof(TINationState))]
    [HarmonyPatch(nameof(TINationState.unrestRestState_unclamped), MethodType.Getter)]
    internal static class TINationState_unrestRestState_unclamped_Patch
    {
        internal static void Postfix(TINationState __instance, ref float __result)
        {
            if (!Main.enabled || Main.Settings is null)
                return;

            NationSettings settings = Main.Settings.nationSettings;

            if (settings.unrestOffset != 0 && __instance.extant)
                __result = __result + settings.unrestOffset;
        }
    }

    [HarmonyPatch(typeof(TINationState))]
    [HarmonyPatch(nameof(TINationState.cohesionRestState), MethodType.Getter)]
    internal static class TINationState_cohesionRestState_Patch
    {
        internal static void Postfix(TINationState __instance, ref float __result)
        {
            if (!Main.enabled || Main.Settings is null)
                return;

            NationSettings settings = Main.Settings.nationSettings;

            if (settings.cohesionOffset != 0 && __instance.extant)
                __result = Mathf.Clamp(__result + settings.cohesionOffset, 0f, 10f);
        }
    }

    [HarmonyPatch(typeof(TINationState), nameof(TINationState.ClaimWillBeHostile))]
    internal static class TINationState_ClaimWillBeHostile_Patch
    {
        internal static void Postfix(TINationState __instance, ref bool __result)
        {
            if (!Main.enabled || Main.Settings is null)
                return;
            NationSettings settings = Main.Settings.nationSettings;

            if (
                settings.ignoreHostileClaims == ExclusiveTargets.All
                || (
                    settings.ignoreHostileClaims == ExclusiveTargets.PlayerOnly
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
            if (!Main.enabled || Main.Settings is null)
                return;
            NationSettings settings = Main.Settings.nationSettings;

            if (
                settings.ignoreDiploCooldowns == ExclusiveTargets.All
                || (
                    settings.ignoreDiploCooldowns == ExclusiveTargets.PlayerOnly
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
            if (!Main.enabled || Main.Settings is null)
                return;
            NationSettings settings = Main.Settings.nationSettings;

            if (
                settings.claimAllCapitals == ExclusiveTargets.All
                || (
                    settings.claimAllCapitals == ExclusiveTargets.PlayerOnly
                    && __instance.executiveFaction.isActivePlayer
                )
            )
                __result = true;
        }
    }

    public enum ExclusiveTargets
    {
        Off = 0,
        PlayerOnly = 1,
        All = 2,
    }

    internal static class UI
    {
        internal static string[] exclusiveTargetLabels = ["Off", "Player only", "All nations"];

        internal static void OnGUI(NationSettings settings, in SettingsUIContext context)
        {
            // group box
            GUILayout.BeginVertical(context.GroupStyle);
            {
                // group label
                GUILayout.Label("Nation tweaks", UnityModManager.UI.h2);

                // TWEAK: shift base unrest
                GUILayout.Space(15);
                GUILayout.BeginHorizontal();
                GUILayout.Label("1. Shift unrest rest state (default: 0.0, change to enable):");
                GUILayout.FlexibleSpace();
                settings.unrestOffset = context.FloatHorizontalSlider(
                    settings.unrestOffset,
                    -10f,
                    10f,
                    context.WideSliderLayout
                );
                GUILayout.EndHorizontal();

                // TWEAK: shift base cohesion
                GUILayout.Space(15);
                GUILayout.BeginHorizontal();
                GUILayout.Label("2. Shift cohesion rest state (default: 0.0, change to enable):");
                GUILayout.FlexibleSpace();
                settings.cohesionOffset = context.FloatHorizontalSlider(
                    settings.cohesionOffset,
                    -10f,
                    10f,
                    context.WideSliderLayout
                );
                GUILayout.EndHorizontal();

                // TWEAK: ignore hostile claims
                GUILayout.Space(15);
                GUILayout.BeginHorizontal();
                GUILayout.Label("3. All claims are non-hostile (default: off):");
                GUILayout.Space(10);
                settings.ignoreHostileClaims = (ExclusiveTargets)
                    GUILayout.Toolbar(
                        (int)settings.ignoreHostileClaims,
                        exclusiveTargetLabels,
                        context.ToolbarStyle
                    );
                GUILayout.EndHorizontal();

                // TWEAK: ignore diplomatic cooldowns
                GUILayout.Space(15);
                GUILayout.BeginHorizontal();
                GUILayout.Label("4. Ignore diplomatic cooldowns (default: off):");
                GUILayout.Space(10);
                settings.ignoreDiploCooldowns = (ExclusiveTargets)
                    GUILayout.Toolbar(
                        (int)settings.ignoreDiploCooldowns,
                        exclusiveTargetLabels,
                        context.ToolbarStyle
                    );
                GUILayout.EndHorizontal();

                // TWEAK: claim on all capitals
                GUILayout.Space(15);
                GUILayout.BeginHorizontal();
                GUILayout.Label("5. Claim on all capitals (default: off):");
                GUILayout.Space(10);
                settings.claimAllCapitals = (ExclusiveTargets)
                    GUILayout.Toolbar(
                        (int)settings.claimAllCapitals,
                        exclusiveTargetLabels,
                        context.ToolbarStyle
                    );
                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
        }
    }

    public class NationSettings : UnityModManager.ModSettings
    {
        public float unrestOffset = 0f;
        public float cohesionOffset = 0f;
        public ExclusiveTargets ignoreHostileClaims = ExclusiveTargets.Off;
        public ExclusiveTargets ignoreDiploCooldowns = ExclusiveTargets.Off;
        public ExclusiveTargets claimAllCapitals = ExclusiveTargets.Off;
    }
}
