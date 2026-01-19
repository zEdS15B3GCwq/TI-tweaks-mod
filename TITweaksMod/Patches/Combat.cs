using HarmonyLib;
using PavonisInteractive.TerraInvicta;
using UnityEngine;
using UnityModManagerNet;

/// <summary>
/// Patches targeting space combat
///
/// 1. Combat damage
///
/// Patched in-game method: TISpaceShipState.ApplyDamage
///
/// This game method runs when a ship receives damage, on the receiving ship. It is used
/// in space combat and in autoresolve.
///
/// Patched to either nullify damage to, or modify damage dealt by player ships. The patch
/// skips the original method to nullify damage to player ships, which is perhaps not
/// optimal for future safety. Damage modification is done by changing input parameters
/// and letting the original method run.
///
/// 2. Ammo usage
///
/// Patched in-game method: TISpaceShipState.ChangeAmmoValue
///
/// This runs when a ship's ammo changes.
///
/// Patched here to disable ammo decrease for player ships, by skipping the game's
/// method when the ammo change is negative (usage). This is not optimal for future safety,
/// but it's simple and it works.
///
/// 3. Also investigated:
///
/// Not patched in-game methods:
///     - (TIMissileTemplate|TIProjectileWeaponTemplate).EstimateChanceToHit: does not appear
///       immediately useful as the method is called many times outside of combat.
///     - TIShipWeaponTemplate.BaseDamageAtRange_points: similar
///     - TIProjectileWeaponTemplate.GetComplexDamage(): similar
///     - SpaceCombat.(MissileController|BallisticProjectileController) seem to have lots of
///       interesting functions for calculating hits etc.
/// </summary>
namespace TITweaksMod.CombatPatches
{
    [HarmonyPatch(typeof(TISpaceShipState), nameof(TISpaceShipState.ApplyDamage))]
    internal static class TISpaceShipState_ApplyDamage_Patch
    {
        internal static bool Prefix(
            TISpaceShipState __instance,
            //TIShipWeaponTemplate attackingWeapon,
            //ArmorFacing facing,
            //float range_km,
            ref float damageAmount,
            ref float chippingAmount,
            //DamageType damageType,
            //float angle,
            TIFactionState attackingFaction,
            ref float internalDamageAssessedHere,
            ref float appliedRadiationDamage,
            ref int shreddingAmount
        )
        {
            if (!Main.enabled || (Main.Settings?.combatSettings is null))
                return true;

            CombatSettings settings = Main.Settings.combatSettings;

            if (settings.playerShipsInvulnerable)
            {
                var ownerFaction = __instance.GetFaction();
                if (ownerFaction.isActivePlayer)
                {
                    internalDamageAssessedHere = 0f;
                    appliedRadiationDamage = 0f;
                    return false;
                }
            }

            if (settings.multiplyPlayerDamage != 1f)
            {
                if (attackingFaction.isActivePlayer)
                {
                    var multiplier = settings.multiplyPlayerDamage;
                    damageAmount *= multiplier;
                    chippingAmount *= multiplier;
                    shreddingAmount = (int)(shreddingAmount * multiplier);
                }
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(TISpaceShipState), nameof(TISpaceShipState.ChangeAmmoValue))]
    internal static class TISpaceShipState_ChangeAmmoValue_Patch
    {
        internal static bool Prefix(TISpaceShipState __instance, ModuleDataEntry module, int delta)
        {
            if (!Main.enabled || (Main.Settings?.combatSettings is null))
                return true;

            CombatSettings settings = Main.Settings.combatSettings;

            if (settings.playerShipsDontUseAmmo)
            {
                // Setting delta to 0 (needs to be ref to work) would perhaps be more future-safe,
                // but simply skipping the ammo decrease works for now.
                if (__instance.GetFaction().isActivePlayer && delta < 0)
                {
                    return false;
                }
            }

            return true;
        }
    }

    internal static class UI
    {
        internal static void OnGUI(CombatSettings settings, in SettingsUIContext context)
        {
            // group box
            GUILayout.BeginVertical(context.GroupStyle);
            {
                // group label
                GUILayout.Label("Combat / Autoresolve", UnityModManager.UI.h2);

                // TWEAK: disable combat damage to player ships
                GUILayout.Space(15);
                GUILayout.BeginHorizontal();
                GUILayout.Label("1. Player ship invulnerability (default: off):");
                GUILayout.Space(5);
                settings.playerShipsInvulnerable = context.OnOffToggle(
                    settings.playerShipsInvulnerable
                );
                GUILayout.EndHorizontal();

                // TWEAK: multiply damage dealt by player ships
                GUILayout.Space(15);
                GUILayout.BeginHorizontal();
                GUILayout.Label("2. Multiply damage dealt by player ships:");
                GUILayout.Space(5);
                settings.multiplyPlayerDamage_Enable = context.OnOffToggle(
                    settings.multiplyPlayerDamage_Enable
                );
                GUILayout.FlexibleSpace();
                settings.multiplyPlayerDamage = context.FloatHorizontalSlider(
                    settings.multiplyPlayerDamage,
                    0f,
                    10f,
                    context.WideSliderLayout
                );
                GUILayout.EndHorizontal();
            }

            // TWEAK: disable player ammo decrease
            GUILayout.Space(15);
            GUILayout.BeginHorizontal();
            GUILayout.Label("3. Player ships do not use ammo:");
            GUILayout.Space(5);
            settings.playerShipsDontUseAmmo = context.OnOffToggle(settings.playerShipsDontUseAmmo);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }
    }

    public class CombatSettings : UnityModManager.ModSettings
    {
        public bool playerShipsInvulnerable = false;
        public bool multiplyPlayerDamage_Enable = false;
        public float multiplyPlayerDamage = 1f;
        public bool playerShipsDontUseAmmo = false;
    }
}
