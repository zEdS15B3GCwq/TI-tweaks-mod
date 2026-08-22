# TI-tweaks-mod

[![ci](https://github.com/zEdS15B3GCwq/TI-tweaks-mod/actions/workflows/ci.yml/badge.svg)](https://github.com/zEdS15B3GCwq/TI-tweaks-mod/actions/workflows/ci.yml)
[![CodeQL](https://github.com/zEdS15B3GCwq/TI-tweaks-mod/actions/workflows/github-code-scanning/codeql/badge.svg)](https://github.com/zEdS15B3GCwq/TI-tweaks-mod/actions/workflows/github-code-scanning/codeql)
[![NexusMods](https://img.shields.io/badge/Nexus_Mods-Cheats_and_Tweaks-orange?style=flat)](https://www.nexusmods.com/terrainvicta/mods/65)

Terra Invicta tweaks mod using Harmony for patching the game, featuring an
in-game menu. It requires Unity Mod Manager for loading the mod and for the GUI.

The mod was developed for and thoroughly tested with game version 1.0.28, and
Unity Mod Manager version 0.32.4.0. I've used it with game versions up to 1.0.32
without issues. It should probably keep working as long as the game's inner logic
isn't changed. If you notice any crashes or other problems submit an [issue](https://github.com/zEdS15B3GCwq/TI-tweaks-mod/issues).

## Table of Contents

- [Description](#description)
- [Installation](#installation)
- [Updates](#updates)
- [Support](#support)

## Description

Please visit the mod's [Nexus Mods page](https://www.nexusmods.com/terrainvicta/mods/65)
for an up-to-date description.

## Installation

1. Install [Unity Mod Manager](https://www.nexusmods.com/site/mods/21) onto the game.
2. Download the latest release from
   [Releases](https://github.com/zEdS15B3GCwq/TI-tweaks-mod/releases) and
   extract the downloaded `.zip` file into the game's `Mods\Enabled` folder.
   After extracting the zip file you should have a
   `Terra Invicta\Mods\Enabled\TITweaksMod` folder with the files.
3. Alternatively, you can install the mod with UMM.
4. Verify that Unity Mod Manager's window opens the next time you launch the
   game, that the `Tweaks and Cheats [FT]` mod is enabled, and that you can open
   its settings.
5. Enjoy!

## Updates

Download the latest .zip and repeat the installation process, overwriting previous content.

I haven't been able to make mod download and update work in UMM, and I don't
care enough to waste more time on trying.

## Known Bugs

The mod has a known bug where the councilor unturn disabler patch causes the
game to crash when the councilor window is opened. The patch prevents the
removal of an invalid councilor, so the list of turned councilors will have
an invalid pointer. This can be fixed by editing the save file or by disabling
the unturn patch before the error. If you encounter this bug, please install
the debug version of the mod, which logs additional information for the
unturn patch, and report the log in an issue.

## Support

The mod is absolutely free, but if you liked using it, please consider
supporting my efforts. It took me weeks to learn C#, learn how to disassemble
and patch Unity games, find out how the game works, and to test all the tweaks.

[![Support me on Ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/ftmods/tip)
