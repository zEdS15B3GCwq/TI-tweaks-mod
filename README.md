# TI-tweaks-mod

[![ci](https://github.com/zEdS15B3GCwq/TI-tweaks-mod/actions/workflows/ci.yml/badge.svg)](https://github.com/zEdS15B3GCwq/TI-tweaks-mod/actions/workflows/ci.yml)
[![CodeQL](https://github.com/zEdS15B3GCwq/TI-tweaks-mod/actions/workflows/github-code-scanning/codeql/badge.svg)](https://github.com/zEdS15B3GCwq/TI-tweaks-mod/actions/workflows/github-code-scanning/codeql)

Terra Invicta tweaks mod using Harmony for patching the game, featuring an
in-game menu. It requires Unity Mod Manager for loading the mod and for the GUI.

Tested for game version 1.0.28. If you notice any crashes or other problems
submit an [issue](https://github.com/zEdS15B3GCwq/TI-tweaks-mod/issues).

## Table of Contents

* [Table of Contents](#table-of-contents)
* [Available tweaks \& cheats](#available-tweaks--cheats)
  * [Mining tab](#mining-tab)
  * [Nation / Diplomacy tab](#nation--diplomacy-tab)
  * [Space Fleets and Combat tab](#space-fleets-and-combat-tab)
  * [Councilors and Missions tab](#councilors-and-missions-tab)
* [Installation](#installation)
* [Support](#support)

## Available tweaks & cheats

### Mining tab

1. Linear mine MC cost above free limit
2. Global mine MC cost multiplier
3. Mine productivity multiplier

Notes:

* These tweaks change mine production and upkeep. The vanilla MC cost for mines
  above the free limit increases quadratically - if you think this is too
  punishing or unreasonable, tweak 1 can change it to a linear cost function.
  Tweaks 2 and 3 are multipliers that apply to mine MC costs and productivity.
  Tweak 1 and 2 can be enabled or disabled globally (all players), while tweak 3
  can be toggled for player faction, other human factions and aliens.
* Tweak 1 is the same as in my
  [older mod on NexusMods](https://www.nexusmods.com/terrainvicta/mods/56),
  making it  unnecessary.

### Nation / Diplomacy tab

1. Unrest rest state offset
2. Cohesion rest state offset
3. All claims are non-hostile
4. Ignore diplomatic cooldowns
5. Add claim on all capitals
6. Information on MC-based alien hate towards factions
7. Hate matrix (diplomatic attitude) for all factions, and hate editor.
8. Discard all accumulated resources for a given faction.
9. Apply an influence drain on a given faction.

Notes:

* Tweaks 1 and 2 are useful for maintaining large end-game nations, as those
  tend to have low cohesion resulting in high unrest. Rest states are target
  values that current cohesion and unrest values trend towards. I've found that,
  together with my other mod that reduces cohesion maluses for large countries
  ([Robust Cohesion](https://www.nexusmods.com/terrainvicta/mods/57)), a small
  adjustment (-2 for unrest, +2 for cohesion rest state) is sufficient.
* Tweaks 3 to 5 allow quick unifications. It's still necessary to federate the
  nations that you want to unify, but once federated, they can be immediately
  unified. If timed well, alliance, federation and unification can be done in
  the same turn. The tweaks disable unification blockers such as cooldowns after
  federation and cooldown (tweak 4) and hostile claims both innate and due to
  democracy score differences (tweak 3). Furthermore, tweak 5 adds a claim on
  all federation member capitals, which means that any country in the federation
  can be unified.
* Tweaks 6 and 7 are about diplomatic attitudes, which in the game are expressed
  as hate, indicating how much factions hate each other. Point 6 shows the
  MC-based hate that aliens have towards human factions (basically a minimum
  point for the hate attitude), while tweak 7 shows the attitudes for all
  factions (rows - which faction feels the hate, column - towards which
  faction). Selected cells can be edited.
* Tweaks 8 and 9 can be used to actually deadlock a faction or factions into
  inactivity by emptying its stores and introducing a significant influence
  drain. First, defeat the faction by eliminating all its income (councilors,
  mines, nations), then when it's resource flows are negative, empty its
  stockpiles. Even in this state, a faction would still have some influence
  income allowing it to spawn new councilors indefinitely. To stop this
  annoyance, enable tweak 9. Tweak 8 has an instant effect while tweak 9 has to
  be turned on for the respective faction's button. These buttons primarily show
  the dataNames of factions (e.g. ResistCouncil), and the display name (the
  Resistance) if the faction is present in the current game.

### Space Fleets and Combat tab

1. Player ship invulnerability
2. Multiply damage dealt by player ships
3. Player ships do not deplete ammo
4. Selected fleet: instant arrive at destination; selected fleet or ship (in
   combat): refuel & rearm, repair

Notes:

* Tweak 1 makes ships invulnerable to direct, explosion and radiation damage.
  Tweak 2 increases damage dealt by player ships by a factor, and tweak 3 stops
  ammo decrease.
* These tweaks work reliably in space combat, seem to work (mostly) during
  bombardment, but do not appear to work (or at least not reliably) in
  auto-resolve.
* Selected individual ships in combat can be refuled, rearmed and repaired using
  tweak 4. When multiple ships are selected, it only applies to the first one in
  the group (I think).

### Councilors and Missions tab

1. Contested Mission Outcome settings
2. Increase detention duration
3. Player councilor tweaks
    1. add XP
    2. make younger
    3. edit attributes
    4. clear traits
    5. add / remove traits
4. Enemy councilor tweaks
    1. individual or all councilors: add max intel, detain, cancel mission
    2. selected enemy councilor: kill, retire, turn
5. Make it impossible for player's turned councilors to unturn

Notes:

* Tweak 1 is shown as an outcome matrix. Each cell represents a contested
  mission performed by an actor (row) on a target (column), and each cell can
  have a value of "default" (unchanged), crit. fail (critical fail), fail,
  success and crit. success. Contested mission means missions that can fail,
  such as assassinations, inspecting councilors, raising popularity, etc. Actors
  categories are: player faction, other human factions, and aliens; target
  categories are the same plus neutral targets, e.g. nations that are not
  controlled by a faction.
* When councilors are detained, they are locked for 1 game "turn", meaning until
  the next time the factions choose their actions. Tweak 2 can increase this, so
  that enemy councilors don't need to be detained again and again in each turn.
  The tweak also applies to repeated detain missions, so a second detain will
  double the detention duration.
* Tweaks under category 3 apply to player councilors, either all of them, or
  selected ones. When a councilor is selected in the game UI, the tweaks menu UI
  will automatically select the same person by default.
* The attribute editor (3.3) only shows its options when a councilor is
  selected. Attributes can be set within the game limits. The maximum limit of
  the trait can be set in `TIGlobalConfig.json`.
* The trait editor (3.4 and 3.5) can clear all, add and remove individual traits
  for all player councilors or a selected one. Traits can be selected from a
  list, and the list can be filtered using the search field. Be careful, some
  traits have multiple levels (e.g. astronomer, senior astronomer, chief
  astronomer) and the editor can add each level at the same time. I'm not sure
  what effect it can have on the game.
* Tweak category 4 applies to enemy councilors. Here, it's possible to select
  all enemy councilors or all councilors of a single enemy faction. A single
  enemy councilor can also be selected if first selected in the game's UI.
* Tweaks in group 4.1 can apply to individual or a group of councilors. In the
  game, intel can be specific to a region, a faction, a fleet or a councilor.
  The cheat "Max intel" adds the maximum possible level of councilor-specific
  intel (1.0) to the player's faction, meaning the location, all the attributes
  and missions of that councilor will be revealed. Careful with the "all enemy
  factions" button, as revealing alien councilors at the beginning of the game
  can affect normal mission progression.
* Group 4.2 needs an individual councilor to be targeted first in the game's UI,
  then that councilor will appear among the tweak menu's options. Councilors can
  be "killed" (revealing the player faction as perpetrator), "retired"
  anonymously, and "turned". The game code does not limit how many councilors
  are turned, but the councilor window crashes if there are more than two of
  them. To avoid crashing the game, the cheat menu only allows turning up to to
  2 councilors.
* Finally, tweak 5 makes it impossible for the player's turned councilors to
  "unturn", i.e. free themselves from the player. Retired, killed or released
  councilors are not affected.

## Installation

1. Install [Unity Mod Manager](https://www.nexusmods.com/site/mods/21) (version
   0.32.4.0) onto the game.
2. Create a folder named `Tweaks and Cheats [FT]` under
   `Terra Invicta\Mods\Enabled\`. You should have a
   `Terra Invicta\Mods\Enabled\Tweaks and Cheats [FT]` folder now.
3. Download the latest release from
   [Releases](https://github.com/zEdS15B3GCwq/TI-tweaks-mod/releases) and
   extract the downloaded `.zip` file into this folder. The `.zip` file contains
   a `TITweaksMod.dll` and a `ModInfo.json` file - they need to be present in
   the mod's folder. After launching the game with the mod enabled, there will
   be other files in this folder, such as `Settings.xml` for storing mod
   settings, and a `*.cache` file. If you download the mod from NexusMods, it
   already contains the folder.
4. Verify that Unity Mod Manager's window opens the next time you launch the
   game, that the `Tweaks & Cheats [FT]` mod is enabled, and that you can open
   its settings.
5. Enjoy!

## Support

The mod is absolutely free, but if you liked using it, please consider
supporting my efforts. It took me weeks to learn C#, learn how to disassemble
and patch Unity games, find out how the game works, and to test all the tweaks.

[![Support me on Ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/ftmods/tip)
