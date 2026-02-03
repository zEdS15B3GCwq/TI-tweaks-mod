using HarmonyLib;
using PavonisInteractive.TerraInvicta;
using UnityEngine;
using UnityModManagerNet;

/// <summary>
/// COMMENTS NEED TO BE UPDATED
/// Patches councilors and missions
///
/// 1. Contested mission outcomes
///
/// Patched in-game method: TIMissionResolution_Contested.GetMissionOutcome
///
/// This method is specific to contested missions, i.e. missions that can fail, unlike
/// defend asset and similar missions that always succeed. It returns a mission
/// result and roll value, which are used to determine success/failure.
///
/// The patch assigns an outcome based on who the actor and who the targets are,
/// using a simple 3x3 matrix defined in the mod settings. The outcome is calculated
/// in the Prefix patch and passed on to the Postfix, which overwrites the result.
/// I decided to use this pattern, as several parameters have default values, and
/// I've had mixed experience of such parameters being passed on to Postfix
/// correctly (if the in-game method assigns to them).
///
/// Relevant in-game methods:
///     - TIMissionState.ResolveMission: calls GetMissionOutcome to get the result
///           Unpatched, as it runs for all missions including uncontested ones,
///           and it is quite complex and may have a lot of side-effects, meaning
///           some code in the middle would need to be patched with a transpiler to
///           achieve the desired changes.
///     - TIMissionResolution_Contested.GetSuccessChance: calculates the chance of
///           succeeding at a mission. Similar to GetMissionOutcome, which uses
///           this internally, but since the chance is not directly used to determine
///           the outcome, it was not necessary to patch it. I haven't tested it,
///           but it's possible that the mission planning phase also shows these
///           chances when selecting missions. This is just a guess, but I didn't
///           want to mess with something like that.
/// </summary>
namespace TITweaksMod.CouncilorPatches
{
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
            TIGameState target,
            out MissionOutcome __state
        )
        {
            __state = MissionOutcome.Default;

            if (!Main.enabled || Main.Settings is null)
                return true;

            var councilorFaction = councilor.faction;
            var targetFaction = mission.target.GetRelevantFaction(target);

            __state = Main.Settings.councilorSettings.MissionOutcomeMatrix.GetOutcome(
                councilorFaction,
                targetFaction
            );
            return true;
        }

        static void Postfix(MissionOutcome __state, ref TIMissionResult __result)
        {
            if (!Main.enabled || Main.Settings is null)
                return;

            if (__state == MissionOutcome.Default)
                return;

            switch (__state)
            {
                case MissionOutcome.CriticalSucceed:
                    __result.outcome = TIMissionOutcome.CriticalSuccess;
                    __result.roll = 0f;
                    break;
                case MissionOutcome.Succeed:
                    __result.outcome = TIMissionOutcome.Success;
                    __result.roll = 0f;
                    break;
                case MissionOutcome.Fail:
                    __result.outcome = TIMissionOutcome.Failure;
                    __result.roll = 1f;
                    break;
                case MissionOutcome.CriticalFail:
                    __result.outcome = TIMissionOutcome.CriticalFailure;
                    __result.roll = 1f;
                    break;
            }
        }
    }

    [HarmonyPatch(typeof(TICouncilorState), nameof(TICouncilorState.DetainCouncilor))]
    internal static class TICouncilorState_DetainCouncilor_Patch
    {
        static bool Prefix(
            TICouncilorState __instance,
            TIFactionState newDetainingFaction,
            ref float baseDuration_Turns,
            ref float extendDuration_Turns
        )
        {
            if (!Main.enabled || Main.Settings is null)
                return true;

            var settings = Main.Settings.councilorSettings;
            switch (settings.LongerDetain_Enabled)
            {
                case OffPlayerAll.Off:
                    // tweak is disabled
                    return true;
                case OffPlayerAll.Player:
                    // skip missions by non-player factions and missions targeting player councilors
                    if (__instance.faction.isActivePlayer || !newDetainingFaction.isActivePlayer)
                        return true;
                    break;
                case OffPlayerAll.All:
                    break;
                default:
                    // ignore unknown value
                    return true;
            }

            // increase detention duration by modifying input parameters
            baseDuration_Turns += settings.LongerDetain_ExtraTurns;
            extendDuration_Turns += settings.LongerDetain_ExtraTurns;

            return true;
        }
    }

    [HarmonyPatch(typeof(TICouncilorState), nameof(TICouncilorState.UnTurnCouncilor))]
    internal static class TICouncilorState_UnTurnCouncilor_Patch
    {
        static bool Prefix(TICouncilorState __instance, bool dismissedByTurningFaction)
        {
            if (!Main.enabled || Main.Settings is null)
                return true;
            var settings = Main.Settings.councilorSettings;

            // Allow retired agents to be removed
            // Retired agents turn to null pointers so not allowing this would cause null pointer exceptions!
            if (__instance.archived)
            {
                //Main.Logger?.Log(
                //    $"UnTurnCouncilor patch: councilor {__instance.displayName}|archived {__instance.archived}, faction {__instance.agentForFaction?.displayName}"
                //);
                return true;
            }

            // Allow dismissing turned agents
            if (dismissedByTurningFaction)
                return true;

            // Prevent losing player's turned agents if settings is enabled
            if (
                settings.NeverLoseTurnedAgents
                && (__instance.agentForFaction?.isActivePlayer ?? false)
            )
                return false;

            // default
            return true;
        }
    }

    internal static class CouncilorManager
    {
        internal static TICouncilorState? selectedCouncilor { get; private set; } = null;
        internal static TICouncilorState? otherSelectedCouncilor { get; private set; } = null;
        internal static TIFactionState[] enemyFactions { get; private set; } = [];
        internal static TICouncilorState[] PlayerCouncilors { get; private set; } = [];
        internal static TIFactionState? PlayerFaction { get; private set; } = null;
        internal static string[] PlayerCouncilorNames =>
            PlayerCouncilors.Select(c => c.displayName).ToArray();

        internal static string[] enemyFactionNames =>
            enemyFactions.Select(f => f.displayName).ToArray();

        internal static int? SelectedCouncilorIndex =>
            selectedCouncilor is null ? null : PlayerCouncilors.IndexOf(selectedCouncilor);

        internal static TICouncilorState? GetCouncilorByIndex(int index) =>
            (uint)index < (uint)PlayerCouncilors.Length ? PlayerCouncilors[index] : null;

        internal static bool isCouncilor(TIGameState selectedGameState) =>
            selectedGameState is TICouncilorState;

        internal static bool isPlayerCouncilorSelected =>
            selectedCouncilor?.faction.isActivePlayer ?? false;

        internal static bool isPlayerCouncilor(TICouncilorState councilor) =>
            councilor.faction.isActivePlayer;

        internal static int? numTurnedCouncilors => PlayerFaction?.turnedCouncilors.Count();

        internal static void Update()
        {
            var selectedGameState = GeneralControlsController.UISelectedAssetState;
            var otherSelectedState = GeneralControlsController.UIOtherSelectedState;

            selectedCouncilor = isCouncilor(selectedGameState)
                ? (TICouncilorState)selectedGameState
                : null;

            otherSelectedCouncilor = isCouncilor(otherSelectedState)
                ? (TICouncilorState)otherSelectedState
                : null;

            PlayerFaction = GameStateManager.AllFactions().FirstOrDefault(x => x.isActivePlayer);
            enemyFactions = GameStateManager.AllFactions().Where(f => !f.isActivePlayer).ToArray();
            PlayerCouncilors = PlayerFaction?.councilors.ToArray() ?? [];
        }

        internal static void AddXPToPlayerCouncilor(
            int amount,
            TICouncilorState? councilor = null,
            bool allCouncilors = false
        )
        {
            if (allCouncilors)
            {
                foreach (var c in PlayerCouncilors)
                    c.ChangeXP(amount);
            }
            else if (councilor is not null)
            {
                if (isPlayerCouncilor(councilor))
                    councilor.ChangeXP(amount);
            }
        }

        internal static void RemovePlayerCouncilorTrait(
            TITraitTemplate trait,
            TICouncilorState? councilor = null,
            bool allCouncilors = false
        )
        {
            if (allCouncilors)
            {
                foreach (var c in PlayerCouncilors)
                    c.RemoveTrait(trait);
            }
            else if (councilor is not null)
            {
                if (isPlayerCouncilor(councilor))
                    councilor.RemoveTrait(trait);
            }
        }

        internal static void AddPlayerCouncilorTrait(
            TITraitTemplate trait,
            TICouncilorState? councilor = null,
            bool allCouncilors = false
        )
        {
            if (allCouncilors)
            {
                foreach (var c in PlayerCouncilors)
                    c.AddTrait(trait);
            }
            else if (councilor is not null)
            {
                if (isPlayerCouncilor(councilor))
                    councilor.AddTrait(trait);
            }
        }

        internal static void ClearPlayerCouncilorTraits(
            TICouncilorState? councilor = null,
            bool allCouncilors = false
        )
        {
            if (allCouncilors)
            {
                foreach (var c in PlayerCouncilors)
                {
                    List<TITraitTemplate> traits = [.. c.traits];
                    traits.ForEach(t => c.RemoveTrait(t));
                }
            }
            else if (councilor is not null)
            {
                if (isPlayerCouncilor(councilor))
                {
                    List<TITraitTemplate> traits = [.. councilor.traits];
                    traits.ForEach(t => councilor.RemoveTrait(t));
                }
            }
        }

        internal enum Operation
        {
            Kill,
            Detain,
            Retire,
            Intel,
            CancelMission,
            Turn,
            MakeYounger,
        }

        internal static void ApplyOperation(
            Operation op,
            TIFactionState? targetFaction = null,
            TICouncilorState? targetCouncilor = null,
            bool targetAllEnemyFactions = false
        )
        {
            if (PlayerFaction is null)
                return;

            // 1. Build the operation based on the UI option
            Action<TICouncilorState>? opAction = op switch
            {
                Operation.Kill => c =>
                {
                    if (PlayerFaction is not null)
                        c.KillCouncilor(true, PlayerFaction);
                },
                Operation.Detain => c =>
                {
                    if (PlayerFaction is not null)
                        c.DetainCouncilor(PlayerFaction, 1f, 1f, false);
                },
                Operation.Intel => c => PlayerFaction?.SetIntel(c, 1f),
                Operation.Retire => c => c.Retire(),
                Operation.CancelMission => c => c.activeMission?.ResolveMission(),
                Operation.Turn => c =>
                {
                    if (PlayerFaction is not null)
                        c.TurnCouncilor(PlayerFaction);
                },
                Operation.MakeYounger => c =>
                {
                    if (TITimeState.Now() is { } now)
                    {
                        var newBirthDate = new TIDateTime(c.dateBorn);
                        newBirthDate.AddYears(10);
                        int newAge = (int)now.DifferenceInJulianYears(newBirthDate);
                        if (newAge >= 10)
                            c.dateBorn = newBirthDate;
                    }
                },
                _ => null,
            };

            if (opAction is null)
                return;

            // 2. Apply to the correct scope

            // individual councilor
            if (targetCouncilor is not null)
            {
                opAction(targetCouncilor);
                return;
            }

            // all councilors in faction
            if (targetFaction is not null)
            {
                // need to make a copy in case the list is modified by an op
                TICouncilorState[] councilors = [.. targetFaction.councilors];
                foreach (var c in councilors)
                {
                    if (c.status == CouncilorStatus.Active)
                        opAction(c);
                }
                return;
            }

            // all councilors in all enemy factions
            if (targetAllEnemyFactions)
            {
                //otherSelectedCouncilor = null;
                foreach (var faction in enemyFactions)
                {
                    TICouncilorState[] councilors = [.. faction.councilors];
                    foreach (var c in councilors)
                    {
                        if (c.status == CouncilorStatus.Active)
                            opAction(c);
                        Main.Logger?.Log($"{c.displayName}|ref null:{c.ref_councilor is null}");
                    }
                }
            }
        }
    }

    internal static class TraitManager
    {
        internal readonly struct TraitEntry
        {
            internal TraitEntry(TITraitTemplate trait)
            {
                this.trait = trait;
                name = trait.displayName;
                nameLower = name.ToLowerInvariant();
            }

            internal TITraitTemplate trait { get; }
            internal string name { get; }
            internal string nameLower { get; }
        }

        private static TraitEntry[] _traits = [];
        private static int[] _filteredTraitIndices = [];
        internal static string[] FilteredTraitNames { get; private set; } = [];

        //internal static int FilteredTraitCount => _filteredTraitIndices.Length;

        internal static void Update()
        {
            _traits = TemplateManager
                .IterateByClass<TITraitTemplate>()
                .Where(t => t.dataName != "dummy")
                .Select(t => new TraitEntry(t))
                .ToArray();

            // Traits changed -> force filter rebuild
            UpdateTraitFilter(string.Empty);
        }

        internal static void UpdateTraitFilter(string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
            {
                _filteredTraitIndices = new int[_traits.Length];
                FilteredTraitNames = new string[_traits.Length];

                for (int i = 0; i < _traits.Length; i++)
                {
                    _filteredTraitIndices[i] = i;
                    FilteredTraitNames[i] = _traits[i].name;
                }
                return;
            }

            // 2-pass to avoid List allocations: count, then allocate exact arrays.
            int count = 0;
            bool[] keep = new bool[_traits.Length];
            for (int i = 0; i < _traits.Length; i++)
            {
                if (_traits[i].nameLower.Contains(filter))
                {
                    count++;
                    keep[i] = true;
                }
                else
                    keep[i] = false;
            }

            _filteredTraitIndices = new int[count];
            FilteredTraitNames = new string[count];

            for (int i = 0, write = 0; i < _traits.Length; i++)
            {
                if (keep[i])
                {
                    _filteredTraitIndices[write] = i;
                    FilteredTraitNames[write] = _traits[i].name;
                    write++;
                }
            }
        }

        internal static TITraitTemplate? GetTraitByFilteredIndex(int filteredIndex)
        {
            if ((uint)filteredIndex >= (uint)_filteredTraitIndices.Length)
                return null;

            int backingIndex = _filteredTraitIndices[filteredIndex];
            return _traits[backingIndex].trait;
        }
    }

    public enum OffPlayerAll
    {
        Off = 0,
        Player = 1,
        All = 2,
    }

    internal static class UI
    {
        //private static bool hidePanel = false;
        private static readonly string[] MissionOutcomeNames =
        [
            "Default",
            "Crit. Fail",
            "Fail",
            "Success",
            "Crit. Success",
        ];
        private static string[] OffPlayerAllLabels = ["Off", "Player", "Global"];
        private static readonly string[] MisionOutcomeGroupNames =
        [
            "Player",
            "Other Humans",
            "Aliens",
            "Neutral",
        ];
        private static string[] councilorSelectionLabels = [];
        private static bool firstOnGUI = true;
        private static bool stateDirty = false;
        private static int selectedCouncilorIndex = 0;
        private static int selectedTraitIndex = 0;
        private static string traitSearchTextLower = string.Empty;
        private static string[] enemySelectionLabels = [];
        private static int selectedEnemyIndex = 0;
        private static int maxCouncilorAttribute = 0;
        private static TICouncilorState? lastSelectedPlayerCouncilor = null;
        private static Dictionary<CouncilorAttribute, int> selectedPlayerCouncilorAttrs = new();
        private static bool selectedPlayerCouncilorAttrsDirty = true;

        private static void Update()
        {
            var councilorNames = CouncilorManager.PlayerCouncilorNames;
            councilorSelectionLabels = [$"All ({councilorNames.Length})", .. councilorNames];

            selectedCouncilorIndex = (CouncilorManager.SelectedCouncilorIndex ?? -1) + 1;
            if (CouncilorManager.selectedCouncilor is not null and var councilor)
            {
                lastSelectedPlayerCouncilor = councilor;
                selectedPlayerCouncilorAttrs = new(councilor.attributes);
                selectedPlayerCouncilorAttrsDirty = false;
            }
            maxCouncilorAttribute = TemplateManager.global.maxCouncilorAttribute;

            if (CouncilorManager.otherSelectedCouncilor is null)
            {
                enemySelectionLabels =
                [
                    "All enemy factions",
                    .. CouncilorManager.enemyFactionNames,
                ];
                selectedEnemyIndex = 0;
            }
            else
            {
                enemySelectionLabels =
                [
                    "All enemy factions",
                    .. CouncilorManager.enemyFactionNames,
                    CouncilorManager.otherSelectedCouncilor.displayName,
                ];
                selectedEnemyIndex = enemySelectionLabels.Length - 1;
            }
        }

        internal static void OnGUI(
            CouncilorSettings settings,
            in SettingsUIContext context,
            bool show
        )
        {
            if (firstOnGUI || stateDirty)
            {
                firstOnGUI = false;
                CouncilorManager.Update();
                TraitManager.Update();
                Update();
            }

            if (show)
            {
                // box group
                GUILayout.BeginVertical(context.GroupStyle);

                // group label
                GUILayout.Label("Councilors and Missions", UnityModManager.UI.h2);

                // TWEAK: councilor mission success matrix
                GUILayout.Space(15);
                GUILayout.Label("1. Contested Mission Outcome Matrix");
                var columnWidth = GUILayout.Width(300f);
                var matrix = settings.MissionOutcomeMatrix;
                var centered = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
                GUILayout.BeginHorizontal();
                GUILayout.Label("actor \\ target", centered, columnWidth);
                for (int i = 0; i < MisionOutcomeGroupNames.Length; i++)
                    GUILayout.Label(MisionOutcomeGroupNames[i], centered, columnWidth);
                GUILayout.EndHorizontal();
                for (int row = 0; row < 3; row++)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(MisionOutcomeGroupNames[row], columnWidth);
                    for (int col = 0; col < 4; col++)
                    {
                        matrix[row][col] = (MissionOutcome)
                            context.IncrementButton(
                                (int)matrix[row][col],
                                MissionOutcomeNames[(int)matrix[row][col]],
                                5,
                                columnWidth
                            );
                    }
                    GUILayout.EndHorizontal();
                }

                // TWEAK: increase detain duration
                GUILayout.Space(15);
                GUILayout.BeginHorizontal();
                GUILayout.Label("2. Increase detention duration:");
                GUILayout.Space(10);
                settings.LongerDetain_Enabled = (OffPlayerAll)
                    GUILayout.Toolbar(
                        (int)settings.LongerDetain_Enabled,
                        OffPlayerAllLabels,
                        context.ToolbarStyle
                    );
                GUILayout.FlexibleSpace();
                GUILayout.Label("Extra turns:");
                GUILayout.Space(10);
                settings.LongerDetain_ExtraTurns = context.IntHorizontalSlider(
                    settings.LongerDetain_ExtraTurns,
                    1,
                    20,
                    1
                );
                GUILayout.EndHorizontal();

                // TWEAK GROUP: Councilor tweaks that target player councilors
                GUILayout.Space(15);
                GUILayout.Label("3. Apply effect to player councilor(s)");

                // add indentation
                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                GUILayout.BeginVertical();

                // Select target councilor or all
                GUILayout.Space(15);
                GUILayout.BeginHorizontal();
                GUILayout.Label("Select target");
                GUILayout.Space(10);
                selectedCouncilorIndex = GUILayout.SelectionGrid(
                    selectedCouncilorIndex,
                    councilorSelectionLabels,
                    3,
                    context.ToolbarStyle
                );
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                var targetAllPlayerCouncilors = selectedCouncilorIndex == 0;
                TICouncilorState? targetCouncilor = targetAllPlayerCouncilors
                    ? null
                    : CouncilorManager.GetCouncilorByIndex(selectedCouncilorIndex - 1);
                if (targetCouncilor != lastSelectedPlayerCouncilor)
                {
                    selectedPlayerCouncilorAttrsDirty = true;
                    lastSelectedPlayerCouncilor = targetCouncilor;
                }

                // TWEAK: Add XP
                GUILayout.Space(15);
                GUILayout.BeginHorizontal();
                GUILayout.Label("3.1 Add XP:");
                GUILayout.Space(10);
                if (GUILayout.Button("10 XP"))
                    CouncilorManager.AddXPToPlayerCouncilor(
                        10,
                        targetCouncilor,
                        targetAllPlayerCouncilors
                    );
                GUILayout.Space(10);
                if (GUILayout.Button("100 XP"))
                    CouncilorManager.AddXPToPlayerCouncilor(
                        100,
                        targetCouncilor,
                        targetAllPlayerCouncilors
                    );
                GUILayout.Space(10);
                if (GUILayout.Button("500 XP"))
                    CouncilorManager.AddXPToPlayerCouncilor(
                        500,
                        targetCouncilor,
                        targetAllPlayerCouncilors
                    );
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.Space(15);
                GUILayout.BeginHorizontal();
                GUILayout.Label("3.2 Make younger:");
                GUILayout.Space(10);
                if (GUILayout.Button("Take 10 years"))
                    CouncilorManager.ApplyOperation(
                        op: CouncilorManager.Operation.MakeYounger,
                        targetFaction: targetAllPlayerCouncilors
                            ? CouncilorManager.PlayerFaction
                            : null,
                        targetCouncilor: targetCouncilor
                    );
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                // TWEAK: attribute editor
                GUILayout.Space(15);
                GUILayout.Label("3.3 Attribute editor:");
                GUILayout.Space(10);
                GUILayout.Label(
                    $"Selected councilor's raw attributes can be changed within allowed limits (0 - {maxCouncilorAttribute})."
                );
                GUILayout.Space(10);
                if (targetCouncilor is null)
                    GUILayout.Label("Select an individual councilor", context.redLabel);
                else
                {
                    if (selectedPlayerCouncilorAttrsDirty)
                    {
                        selectedPlayerCouncilorAttrs = new(targetCouncilor.attributes);
                        selectedPlayerCouncilorAttrsDirty = false;
                    }
                    GUILayout.BeginHorizontal();
                    GUILayout.BeginVertical();
                    foreach (var attr in selectedPlayerCouncilorAttrs)
                    {
                        var oldValue = attr.Value;
                        int newValue = oldValue;
                        GUILayout.BeginHorizontal();
                        GUILayout.Label($"{attr.Key.ToString()}");
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Set to 10"))
                            newValue = 10;
                        GUILayout.Space(5);
                        if (GUILayout.Button("-5"))
                            newValue -= 5;
                        GUILayout.Space(5);
                        if (GUILayout.Button("-1"))
                            newValue -= 1;
                        GUILayout.Space(5);
                        GUILayout.Label($"{attr.Value}", centered, GUILayout.Width(75f));
                        GUILayout.Space(5);
                        if (GUILayout.Button("+1"))
                            newValue += 1;
                        GUILayout.Space(5);
                        if (GUILayout.Button("+5"))
                            newValue += 5;
                        GUILayout.Space(5);
                        if (GUILayout.Button("Set to Max"))
                            newValue = maxCouncilorAttribute;
                        GUILayout.EndHorizontal();
                        if (oldValue != newValue)
                        {
                            targetCouncilor.ModifyAttribute(attr.Key, newValue - oldValue);
                            selectedPlayerCouncilorAttrsDirty = true;
                        }
                    }
                    GUILayout.EndVertical();
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                }

                // TWEAK: clear all traits
                GUILayout.Space(15);
                GUILayout.BeginHorizontal();
                GUILayout.Label("3.4 Clear all traits:");
                GUILayout.Space(10);
                if (GUILayout.Button("Clear"))
                    CouncilorManager.ClearPlayerCouncilorTraits(
                        targetCouncilor,
                        targetAllPlayerCouncilors
                    );
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                // TWEAK: add / remove traits
                GUILayout.Space(15);
                GUILayout.BeginHorizontal();
                GUILayout.Label("3.5 Add / remove selected trait:");
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.Space(15);
                GUILayout.BeginHorizontal();
                GUILayout.Label("Search:");
                GUILayout.Space(10);
                string newSearch = GUILayout
                    .TextField(traitSearchTextLower, GUILayout.Width(500f))
                    .ToLowerInvariant();
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                if (!string.Equals(newSearch, traitSearchTextLower, StringComparison.Ordinal))
                {
                    traitSearchTextLower = newSearch;
                    TraitManager.UpdateTraitFilter(traitSearchTextLower);
                    selectedTraitIndex = 0;
                }

                GUILayout.Space(15);
                GUILayout.BeginHorizontal();
                selectedTraitIndex = GUILayout.SelectionGrid(
                    selectedTraitIndex,
                    TraitManager.FilteredTraitNames,
                    5,
                    context.GridStyle,
                    GUILayout.Width(1500)
                );
                GUILayout.EndHorizontal();

                GUILayout.Space(15);
                GUILayout.Label(
                    "It's possible to add different tiers of the same trait. Who knows, it might break things."
                );

                GUILayout.Space(15);
                GUILayout.BeginHorizontal();
                GUILayout.Label("Click action:");
                GUILayout.Space(10);
                var selectedTrait = TraitManager.GetTraitByFilteredIndex(selectedTraitIndex);
                if (GUILayout.Button("Add Trait") && selectedTrait is not null)
                    CouncilorManager.AddPlayerCouncilorTrait(
                        selectedTrait,
                        targetCouncilor,
                        targetAllPlayerCouncilors
                    );
                if (GUILayout.Button("Remove Trait") && selectedTrait is not null)
                    CouncilorManager.RemovePlayerCouncilorTrait(
                        selectedTrait,
                        targetCouncilor,
                        targetAllPlayerCouncilors
                    );
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                // end indentation
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();

                // TWEAK GROUP: target enemy councilors
                GUILayout.Space(15);
                GUILayout.Label("4. Apply effect to enemy councilor(s)");

                // add indentation
                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                GUILayout.BeginVertical();

                // Select target councilor or faction(s)
                if (CouncilorManager.otherSelectedCouncilor is null)
                {
                    GUILayout.Space(15);
                    GUILayout.Label(
                        "(An individual enemy councilor selected in the game will appear here as an option.)"
                    );
                }

                GUILayout.Space(15);
                GUILayout.BeginHorizontal();
                selectedEnemyIndex = GUILayout.SelectionGrid(
                    selectedEnemyIndex,
                    enemySelectionLabels,
                    3,
                    context.ToolbarStyle
                );
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                // null is used to mean all enemy factions
                TICouncilorState? targetEnemyCouncilor = null;
                TIFactionState? targetEnemyFaction = null;
                bool targetAllEnemy = false;
                switch (selectedEnemyIndex)
                {
                    case 0:
                        targetAllEnemy = true;
                        break;
                    case var i
                        when CouncilorManager.otherSelectedCouncilor is not null
                            && i == enemySelectionLabels.Length - 1:
                        targetEnemyCouncilor = CouncilorManager.otherSelectedCouncilor;
                        break;
                    case > 0:
                        targetEnemyFaction = CouncilorManager.enemyFactions[selectedEnemyIndex - 1];
                        break;
                }
                // TWEAK: Max intel on enemy councilors
                GUILayout.Space(15);
                GUILayout.Label("4.1 Select operation (multiple or individual targets allowed):");
                GUILayout.Space(10);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Max Intel"))
                    CouncilorManager.ApplyOperation(
                        CouncilorManager.Operation.Intel,
                        targetEnemyFaction,
                        targetEnemyCouncilor,
                        targetAllEnemy
                    );
                GUILayout.Space(10);
                if (GUILayout.Button("Detain"))
                    CouncilorManager.ApplyOperation(
                        CouncilorManager.Operation.Detain,
                        targetEnemyFaction,
                        targetEnemyCouncilor,
                        targetAllEnemy
                    );
                GUILayout.Space(10);
                if (GUILayout.Button("Cancel Mission"))
                    CouncilorManager.ApplyOperation(
                        CouncilorManager.Operation.CancelMission,
                        targetEnemyFaction,
                        targetEnemyCouncilor,
                        targetAllEnemy
                    );
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.Space(15);
                GUILayout.Label("4.2 Operations on selected councilor:");
                GUILayout.Space(10);
                if (targetEnemyCouncilor is null)
                    GUILayout.Label("Select an individual enemy councilor", context.redLabel);
                else
                {
                    bool cannotTurn =
                        CouncilorManager.numTurnedCouncilors is not null
                        && CouncilorManager.numTurnedCouncilors == 2;
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Kill (by player faction)"))
                    {
                        CouncilorManager.ApplyOperation(
                            CouncilorManager.Operation.Kill,
                            null,
                            targetEnemyCouncilor
                        );
                        stateDirty = true; // force update as selection has changed
                    }
                    GUILayout.Space(10);
                    if (GUILayout.Button("Retire (anonymously)"))
                    {
                        CouncilorManager.ApplyOperation(
                            CouncilorManager.Operation.Retire,
                            null,
                            targetEnemyCouncilor
                        );
                        stateDirty = true; // force update as selection has changed
                    }
                    GUILayout.Space(10);
                    if (cannotTurn)
                        GUI.enabled = false;
                    if (GUILayout.Button("Turn"))
                        CouncilorManager.ApplyOperation(
                            CouncilorManager.Operation.Turn,
                            null,
                            targetEnemyCouncilor
                        );
                    if (cannotTurn)
                        GUI.enabled = true;
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                }
                GUILayout.Space(10);
                GUILayout.Label("Notes:");
                GUILayout.Label(
                    "Kill and Retire only work on a selected enemy councilor due to some in-game mechanism I have not figured out."
                );
                GUILayout.Label(
                    "Turn would work on any number of targets in theory, but opening the councilor screen with >2 turned agents crashes the game."
                );

                // end indentation
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();

                GUILayout.Space(15);
                GUILayout.BeginHorizontal();
                GUILayout.Label("5. Player's turned agents cannot unturn:");
                GUILayout.Space(10);
                settings.NeverLoseTurnedAgents = context.OnOffToggle(
                    settings.NeverLoseTurnedAgents
                );
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.EndVertical();
            }
        }

        internal static void OnHideGUI()
        {
            firstOnGUI = true;
        }
    }

    public enum MissionOutcome
    {
        Default = 0,
        CriticalFail = 1,
        Fail = 2,
        Succeed = 3,
        CriticalSucceed = 4,
    }

    public class MissionOutcomeMatrix
    {
        public Row Player { get; set; } = new();
        public Row OtherHumans { get; set; } = new();
        public Row Aliens { get; set; } = new();

        private int? FactionToIndex(TIFactionState? faction)
        {
            if (faction is null)
                return 3;
            if (faction.isActivePlayer)
                return 0;
            if (faction.IsActiveHumanFaction)
                return 1;
            if (faction.IsAlienFaction)
                return 2;
            else
                return null;
        }

        public MissionOutcome GetOutcome(
            TIFactionState councilorFaction,
            TIFactionState? targetFaction
        )
        {
            var councilorGroup = FactionToIndex(councilorFaction);
            var targetGroup = FactionToIndex(targetFaction);
            if (councilorGroup.HasValue && targetGroup.HasValue)
                return this[councilorGroup.Value][targetGroup.Value];
            return MissionOutcome.Default;
        }

        public class Row
        {
            public MissionOutcome Player { get; set; }
            public MissionOutcome OtherHumans { get; set; }
            public MissionOutcome Aliens { get; set; }
            public MissionOutcome Neutral { get; set; }

            public MissionOutcome this[int index]
            {
                get =>
                    index switch
                    {
                        0 => Player,
                        1 => OtherHumans,
                        2 => Aliens,
                        3 => Neutral,
                        _ => MissionOutcome.Default,
                    };
                set
                {
                    switch (index)
                    {
                        case 0:
                            Player = value;
                            break;
                        case 1:
                            OtherHumans = value;
                            break;
                        case 2:
                            Aliens = value;
                            break;
                        case 3:
                            Neutral = value;
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        public class DefaultRow : Row
        {
            public new MissionOutcome this[int index]
            {
                get => MissionOutcome.Default;
                set { }
            }
        }

        public Row this[int index] =>
            index switch
            {
                0 => Player,
                1 => OtherHumans,
                2 => Aliens,
                _ => new DefaultRow(),
            };
    }

    public class CouncilorSettings : UnityModManager.ModSettings
    {
        public MissionOutcomeMatrix MissionOutcomeMatrix = new()
        {
            Player = new()
            {
                Player = MissionOutcome.Default,
                OtherHumans = MissionOutcome.Default,
                Aliens = MissionOutcome.Default,
                Neutral = MissionOutcome.Default,
            },
            OtherHumans = new()
            {
                Player = MissionOutcome.Default,
                OtherHumans = MissionOutcome.Default,
                Aliens = MissionOutcome.Default,
                Neutral = MissionOutcome.Default,
            },
            Aliens = new()
            {
                Player = MissionOutcome.Default,
                OtherHumans = MissionOutcome.Default,
                Aliens = MissionOutcome.Default,
                Neutral = MissionOutcome.Default,
            },
        };
        public OffPlayerAll LongerDetain_Enabled = OffPlayerAll.Off;
        public int LongerDetain_ExtraTurns = 1;
        public bool NeverLoseTurnedAgents = false;
    }
}
