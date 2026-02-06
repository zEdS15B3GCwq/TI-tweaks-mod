using HarmonyLib;
using PavonisInteractive.TerraInvicta;
using PavonisInteractive.TerraInvicta.Tasks;
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
namespace TITweaksMod.SpaceShipPatches
{
    [HarmonyPatch(typeof(TISpaceShipState), nameof(TISpaceShipState.ApplyDamage))]
    internal static class TISpaceShipState_ApplyDamage_Patch
    {
        internal static bool Prefix(
            TISpaceShipState __instance,
            ref float damageAmount,
            ref float chippingAmount,
            TIFactionState attackingFaction,
            ref float internalDamageAssessedHere,
            ref float appliedRadiationDamage,
            ref int shreddingAmount
        )
        {
            if (!Main.enabled || (Main.Settings?.combatSettings is null) || __instance.isDummy)
                return true;

            CombatSettings settings = Main.Settings.combatSettings;

            if (settings.playerShipsInvulnerable && __instance.faction.isActivePlayer)
            {
                internalDamageAssessedHere = 0f;
                appliedRadiationDamage = 0f;
                return false;
            }

            if (settings.multiplyPlayerDamage_Enable && settings.multiplyPlayerDamage != 1f)
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

    [HarmonyPatch(typeof(TISpaceShipState), nameof(TISpaceShipState.ApplyInternalDamage))]
    internal static class TISpaceShipState_ApplyInternalDamage_Patch
    {
        internal static bool Prefix(TISpaceShipState __instance)
        {
            if (!Main.enabled || (Main.Settings is null) || __instance.isDummy)
                return true;

            CombatSettings settings = Main.Settings.combatSettings;

            if (settings.playerShipsInvulnerable && __instance.faction.isActivePlayer)
                return false;

            return true;
        }
    }

    [HarmonyPatch(typeof(TISpaceShipState), nameof(TISpaceShipState.ApplyInternalRadiationDamage))]
    internal static class TISpaceShipState_ApplyInternalRadiationDamage_Patch
    {
        internal static bool Prefix(TISpaceShipState __instance, ref float __result)
        {
            if (!Main.enabled || (Main.Settings is null) || __instance.isDummy)
                return true;

            CombatSettings settings = Main.Settings.combatSettings;

            if (settings.playerShipsInvulnerable && __instance.faction.isActivePlayer)
            {
                __result = 0f;
                return false;
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

            // Setting delta to 0 (needs to be ref to work) would perhaps be more future-safe,
            // but simply skipping the ammo decrease works for now.
            if (
                settings.playerShipsDontUseAmmo
                && __instance.GetFaction().isActivePlayer
                && delta < 0
            )
            {
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(
        typeof(TISpaceShipTemplate),
        nameof(TISpaceShipTemplate.AreWeaponModulesValidForRefit)
    )]
    internal static class TISpaceShipTemplate_AreWeaponModulesValidForRefit_Patch
    {
        internal static void Postfix(TISpaceShipTemplate __instance, ref bool __result)
        {
            if (!Main.enabled || (Main.Settings?.combatSettings is null))
                return;

            // only affect player designs
            if ((!__instance.designingFaction?.isActivePlayer) ?? false)
                return;

            CombatSettings settings = Main.Settings.combatSettings;

            if (settings.allowWeaponRefit)
                __result = true;

            return;
        }
    }

    [HarmonyPatch(
        typeof(TISpaceShipTemplate),
        nameof(TISpaceShipTemplate.AreUtilityModulesValidForRefit)
    )]
    internal static class TISpaceShipTemplate_AreUtilityModulesValidForRefit_Patch
    {
        internal static void Postfix(TISpaceShipTemplate __instance, ref bool __result)
        {
            if (!Main.enabled || (Main.Settings?.combatSettings is null))
                return;

            // only affect player designs
            if ((!__instance.designingFaction?.isActivePlayer) ?? false)
                return;

            CombatSettings settings = Main.Settings.combatSettings;

            if (settings.allowUtilityRefit)
                __result = true;

            return;
        }
    }

    [HarmonyPatch(typeof(TIDriveTemplate), nameof(TIDriveTemplate.IsValidRefitPart))]
    internal static class TIDriveTemplate_IsValidRefitPart_Patch
    {
        internal static void Postfix(TISpaceShipTemplate oldShipTemplate, ref bool __result)
        {
            if (!Main.enabled || (Main.Settings?.combatSettings is null))
                return;

            // only affect player designs
            if ((!oldShipTemplate.designingFaction?.isActivePlayer) ?? false)
                return;

            CombatSettings settings = Main.Settings.combatSettings;

            if (settings.allowEnginePowerplantRefit)
                __result = true;

            return;
        }
    }

    [HarmonyPatch(typeof(TIPowerPlantTemplate), nameof(TIPowerPlantTemplate.IsValidRefitPart))]
    internal static class TIPowerPlantTemplate_IsValidRefitPart_Patch
    {
        internal static void Postfix(TISpaceShipTemplate originalShipTemplate, ref bool __result)
        {
            if (!Main.enabled || (Main.Settings?.combatSettings is null))
                return;

            // only affect player designs
            if ((!originalShipTemplate.designingFaction?.isActivePlayer) ?? false)
                return;

            CombatSettings settings = Main.Settings.combatSettings;

            if (settings.allowEnginePowerplantRefit)
                __result = true;

            return;
        }
    }

    [HarmonyPatch(typeof(TISpaceShipTemplate), nameof(TISpaceShipTemplate.IsAValidRefitFor))]
    internal static class TISpaceShipTemplate_IsAValidRefitFor_Patch
    {
        internal static bool Prefix(
            TISpaceShipTemplate __instance,
            ref string reason,
            ref bool __result
        )
        {
            if (!Main.enabled || (Main.Settings?.combatSettings is null))
                return true;

            // only affect player designs
            if ((!__instance.designingFaction?.isActivePlayer) ?? false)
                return true;

            CombatSettings settings = Main.Settings.combatSettings;

            // Allow any refit unconditionally, skip game method
            if (settings.allowAllRefit)
            {
                reason = string.Empty;
                __result = true;
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(StratCombatInitStrategy), nameof(StratCombatInitStrategy.SelectStance))]
    internal static class StratCombatInitStrategy_SelectStance_Patch
    {
        internal static bool Prefix(
            StratCombatInitStrategy __instance,
            TIFactionState faction,
            TISpaceCombatState combatState,
            out bool __state
        //ref CombatStance __result
        )
        {
            __state = false;

            if (!Main.enabled || (Main.Settings?.combatSettings is null))
                return true;

            // skip if attacker is not human or if defender is human
            // unsure if faction can be the player here - wouldn't humans be prompted in the UI instead?
            if (!combatState.attacker.faction.isActivePlayer || faction.isActivePlayer)
                return true;

            CombatSettings settings = Main.Settings.combatSettings;

            // if setting is enabled for active faction
            if (
                (!faction.IsAlienFaction && settings.combatAIHumanAlwaysDefendAgainstPlayer)
                || (faction.IsAlienFaction && settings.combatAIAlienAlwaysDefendAgainstPlayer)
            )
            {
                // always defend (not evade) if allowed
                // possible combat stances are set in TISpaceCombatState.InitializeCombat
                // we let Postfix() know that it should set Defend when necessary
                List<CombatStance> list = combatState.allowedStances[faction];
                if (list.Contains(CombatStance.Defend))
                {
                    //__result = CombatStance.Defend;
                    //return false;
                    __state = true;
                }
            }

            return true;
        }

        internal static void Postfix(
            //StratCombatInitStrategy __instance,
            //TIFactionState faction,
            //TISpaceCombatState combatState,
            bool __state,
            ref CombatStance __result
        )
        {
            // Prefix() is signaling us that we should set Defend,
            // but no need to set Defend if AI already wants to fight
            if (__state && __result != CombatStance.Defend && __result != CombatStance.Pursue)
            {
                //Main.Logger?.Log(
                //    $"SelectStance: faction {faction.displayName} vs attacker {combatState.attacker.faction.displayName}, original stance: {__result}, new stance: {CombatStance.Defend}"
                //);
                __result = CombatStance.Defend;
            }
        }
    }

    internal static class FleetManager
    {
        internal static TISpaceShipState? selectedPlayerShip { get; private set; } = null;

        //internal static TISpaceShipState? selectedEnemyShip { get; private set; } = null;
        internal static TISpaceFleetState? selectedPlayerFleet { get; private set; } = null;

        //internal static TISpaceFleetState? selectedEnemyFleet { get; private set; } = null;

        internal static void Update()
        {
            var selectedGameState = GeneralControlsController.UISelectedAssetState;
            //var otherSelectedState = GeneralControlsController.UIOtherSelectedState;

            // retrieving selection during combat, based on Debugging.TerminalFleetCommands.KillShip
            selectedPlayerShip = null;
            var spaceCombat = GameControl.spaceCombat;
            if (spaceCombat is not null && spaceCombat.enabled)
            {
                var shipController = spaceCombat.combatHUD.selectedFriendlyShip;
                if (shipController?.activePlayerShip ?? false)
                    selectedPlayerShip = shipController.ShipState;
                //selectedGameState is TISpaceShipState ? (TISpaceShipState)selectedGameState : null;
            }

            //selectedEnemyShip =
            //    otherSelectedState is TISpaceShipState
            //        ? (TISpaceShipState)otherSelectedState
            //        : null;

            selectedPlayerFleet =
                selectedGameState is TISpaceFleetState
                    ? (TISpaceFleetState)selectedGameState
                    : null;

            //selectedEnemyFleet =
            //    otherSelectedState is TISpaceFleetState
            //        ? (TISpaceFleetState)otherSelectedState
            //        : null;
        }

        internal static void ArriveFleet(TISpaceFleetState? fleet)
        {
            if (fleet is not null && fleet.trajectory is not null)
                fleet.ArriveFleet();
        }

        internal static void RefuelRearmFleet(TIGameState? shipOrFleet)
        {
            TISpaceShipState[] ships;
            switch (shipOrFleet)
            {
                case null:
                    return;
                case TISpaceShipState ship:
                    ships = [ship];
                    break;
                case TISpaceFleetState fleet:
                    ships = [.. fleet.ref_fleet.ships];
                    break;
                default:
                    return;
            }
            foreach (TISpaceShipState ship in ships)
            {
                // based on TISpaceShipState.InstantFullRepair
                ship.LoadAmmo();
                ship.RePropellantToMax();

                // below solution is based on PlannedResupplyAndRepair.ProcessResupplyAndRepair
                //if (ship.NeedsRefuel())
                //    ship.RefuelPropellant(ship.template.propellantMass_tons - ship.propellant_tons);

                //foreach (var weaponAmmo in ship.ammo)
                //{
                //    var maxAmmo =
                //        weaponAmmo.Key.moduleTemplate.ref_projectileWeapon.FullAmmoCount_Current(
                //            ship
                //        );
                //    var currentAmmo = weaponAmmo.Value;
                //    if (currentAmmo < maxAmmo)
                //        ship.ammo[weaponAmmo.Key] = maxAmmo;
                //}
            }
        }

        internal static void RepairFleet(TIGameState? shipOrFleet)
        {
            TISpaceShipState[] ships;
            switch (shipOrFleet)
            {
                case null:
                    return;
                case TISpaceShipState ship:
                    ships = [ship];
                    break;
                case TISpaceFleetState fleet:
                    ships = [.. fleet.ref_fleet.ships];
                    break;
                default:
                    return;
            }
            foreach (TISpaceShipState ship in ships)
            {
                // based on TISpaceShipState.InstantFullRepair
                //foreach (ArmorFacing key in ship.armor.Keys)
                //{
                //    ship.armor[key].RepairArmor();
                //}
                //ship.damagedParts.Clear();
                //ship.damagedSystems.Clear(); // private field can't be cleared like this

                // based on PlannedResupplyAndRepair.ProcessResupplyAndRepair
                // keps this to keep side-effects of methods called
                ShipSystem[] damagedSystems = [.. ship.DamagedSystems()];
                foreach (ShipSystem system in damagedSystems)
                    ship.RepairSystem(system);

                DamagedShipPartData[] damagedParts = [.. ship.damagedParts];
                foreach (DamagedShipPartData part in damagedParts)
                    ship.RepairPart(part);

                foreach (var armorData in ship.armor)
                {
                    if (armorData.Value.damaged)
                        ship.RepairArmorFacing(armorData.Key);
                }

                ship.visualizerLink.ModelController.OnWeaponsRepaired();
                ship.ClearShipDamageVisualizations();
            }
        }
    }

    internal static class UI
    {
        internal static bool firstOnGUI = true;

        internal static void OnGUI(CombatSettings settings, in SettingsUIContext context, bool show)
        {
            if (firstOnGUI)
            {
                firstOnGUI = false;
                FleetManager.Update();
            }

            if (show)
            {
                // group box
                GUILayout.BeginVertical(context.GroupStyle);
                {
                    // group label
                    GUILayout.Label("Space Fleets and Combat", UnityModManager.UI.h2);

                    // TWEAK: disable combat damage to player ships
                    GUILayout.Space(15);
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("1. Player ship invulnerability:");
                    GUILayout.Space(5);
                    settings.playerShipsInvulnerable = context.OnOffToggle(
                        settings.playerShipsInvulnerable
                    );
                    GUILayout.FlexibleSpace();
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
                        1f,
                        context.WideSliderLayout
                    );
                    GUILayout.EndHorizontal();

                    // TWEAK: disable player ammo decrease
                    GUILayout.Space(15);
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("3. Player ships do not use ammo:");
                    GUILayout.Space(5);
                    settings.playerShipsDontUseAmmo = context.OnOffToggle(
                        settings.playerShipsDontUseAmmo
                    );
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();

                    // TWEAKS affecting player fleets
                    GUILayout.Space(15);
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("4. Operations on selected player fleet or ship:");
                    GUILayout.Space(10);
                    TIGameState? selected = null;
                    if (FleetManager.selectedPlayerShip is not null)
                    {
                        selected = FleetManager.selectedPlayerShip;
                        GUILayout.Label(
                            FleetManager.selectedPlayerShip.displayName,
                            context.yellowLabel
                        );
                    }
                    else if (FleetManager.selectedPlayerFleet is not null)
                    {
                        selected = FleetManager.selectedPlayerFleet;
                        GUILayout.Label(
                            FleetManager.selectedPlayerFleet.activePlayerDisplayName,
                            context.blueLabel
                        );
                    }
                    else
                        GUILayout.Label("none selected", context.redLabel);
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();

                    // operation buttons
                    GUILayout.Space(15);
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    if (selected is not TISpaceFleetState)
                        GUI.enabled = false;
                    if (GUILayout.Button("Arrive at Destination") && selected is TISpaceFleetState)
                        FleetManager.ArriveFleet((TISpaceFleetState)selected);
                    if (selected is not TISpaceFleetState)
                        GUI.enabled = true;
                    GUILayout.Space(10);
                    if (GUILayout.Button("Refuel & Rearm"))
                        FleetManager.RefuelRearmFleet(selected);
                    GUILayout.Space(10);
                    if (GUILayout.Button("Repair"))
                        FleetManager.RepairFleet(selected);
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();

                    // TWEAK: allow refit
                    GUILayout.Space(15);
                    GUILayout.Label("5. Allow restricted refit (player designs only):");
                    GUILayout.Space(10);
                    GUILayout.BeginHorizontal();
                    if (settings.allowAllRefit)
                        GUI.enabled = false;
                    settings.allowEnginePowerplantRefit = GUILayout.Toggle(
                        settings.allowEnginePowerplantRefit,
                        "Engine and Powerplant",
                        context.ToggleStyle
                    );
                    GUILayout.Space(5);
                    settings.allowUtilityRefit = GUILayout.Toggle(
                        settings.allowUtilityRefit,
                        "Utilities",
                        context.ToggleStyle
                    );
                    GUILayout.Space(5);
                    settings.allowWeaponRefit = GUILayout.Toggle(
                        settings.allowWeaponRefit,
                        "Weapons",
                        context.ToggleStyle
                    );
                    if (settings.allowAllRefit)
                        GUI.enabled = true;
                    GUILayout.Space(5);
                    settings.allowAllRefit = GUILayout.Toggle(
                        settings.allowAllRefit,
                        "Any",
                        context.ToggleStyle
                    );
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                    GUILayout.Space(10);
                    GUILayout.Label(
                        "Careful! The Any option disables any restrictions on refitting, even sanity checks for duplicate"
                    );
                    GUILayout.Label(
                        "designs and hull changes. It may be useful for some specific cases, such as removing batteries."
                    );

                    // TWEAK: combat AI always defend
                    GUILayout.Space(15);
                    GUILayout.Label("6. Combat AI will always defend when attacked by player:");
                    GUILayout.Space(10);
                    GUILayout.BeginHorizontal();
                    settings.combatAIHumanAlwaysDefendAgainstPlayer = GUILayout.Toggle(
                        settings.combatAIHumanAlwaysDefendAgainstPlayer,
                        "Human AI",
                        context.ToggleStyle
                    );
                    GUILayout.Space(5);
                    settings.combatAIAlienAlwaysDefendAgainstPlayer = GUILayout.Toggle(
                        settings.combatAIAlienAlwaysDefendAgainstPlayer,
                        "Alien AI",
                        context.ToggleStyle
                    );
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

    public class CombatSettings : UnityModManager.ModSettings
    {
        public bool playerShipsInvulnerable = false;
        public bool multiplyPlayerDamage_Enable = false;
        public float multiplyPlayerDamage = 1f;
        public bool playerShipsDontUseAmmo = false;
        public bool allowUtilityRefit = false;
        public bool allowWeaponRefit = false;
        public bool allowEnginePowerplantRefit = false;
        public bool allowAllRefit = false;
        public bool combatAIHumanAlwaysDefendAgainstPlayer = false;
        public bool combatAIAlienAlwaysDefendAgainstPlayer = false;
    }
}
