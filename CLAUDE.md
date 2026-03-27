# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a mod for Old World (turn-based strategy game) called **Immersive Families**. It dynamically renames families based on their assigned family class and nation using a Harmony postfix on `Game.loadFamilyClass`. It also assigns consistent colors per family class. The C# harness reads a custom XML config; content authors add nations by editing XML only.

## Architecture

- `Source/ModEntryPoint.cs` — Entry point + Harmony patch. Captures original `meName` and `miColorIndex` at init, sets them in the postfix based on config, restores originals for unmapped (nation, class) pairs.
- `Infos/family-class-names.xml` — Custom config mapping (nation, familyClass) → text key. Parsed manually by `LoadConfig()` (not auto-loaded by the game).
- `Infos/text-family-class-names.xml` — Display names. Auto-loaded by the game (all `text-*.xml` files are).
- `Infos/color-add.xml`, `playerColor-add.xml`, `teamColor-change.xml` — Extend nation color palettes from 4 to 10 entries so each family class gets a consistent color.

## Game Reference Data

`Reference/` is a symlink to the game's install directory containing:
- `Reference/Source/Base/` — C# game source (prefer over `decompiled/` directory)
- `Reference/XML/Infos/` — vanilla XML data (families, family classes, nations, colors, etc.)

Key source locations:
- `Game.loadFamilyClass` — `Reference/Source/Base/Game/GameCore/Game.cs:6528`
- `InfoFamily` class — `Reference/Source/Base/Game/GameCore/InfoBase.cs:2927`
- `Infos.getType<T>()` — `Reference/Source/Base/Game/GameCore/Infos.cs:3173`

### Setting Up the Reference Symlink

The symlink is machine-specific and not checked into git. Create it after cloning:

```bash
# macOS — adjust the path to your Old World installation
ln -s "$HOME/Library/Application Support/Steam/steamapps/common/Old World/Reference" Reference
```

## Deployment

**Local testing** (requires `.env` with `OLDWORLD_PATH` and `OLDWORLD_MODS_PATH`):
```bash
./scripts/deploy.sh
```

**Validation only:**
```bash
./scripts/validate.sh
```

## Critical: Text Files Need UTF-8 BOM

Text files (`text-*.xml`) **must** have a UTF-8 BOM (`ef bb bf`). Without it, the game silently fails to load text. The `scripts/validate.sh` and pre-commit hook catch missing BOMs.

## Key Technical Details

- **IModPath does not expose a filesystem path.** Use `ModRecord.ModdedPath` via `modSettings.ModPath.GetMods()` to find the mod's directory.
- **InfoFamily data is NOT re-read from XML between games** with the same mod list. Mutations to `meName` and `miColorIndex` persist on the shared `Infos` object. The postfix must always set or restore values — it cannot rely on XML reload to reset defaults.
- **Each game session with different mods gets a separate `Infos` instance**, so mutations never leak to games without this mod.
- **FamilyClassType enum values (0-9)** are stable, derived from XML load order in `familyClass.xml`. Used directly as `miColorIndex`.
- **`teamColor.aePlayerColors` and `aeBorderPatterns` are both indexed by `miColorIndex`** — both arrays must have enough entries for any index the postfix sets.

## Modding Lifecycle

- The game loads mod DLLs three times during initialization. The triple-load guard (`if (_harmony != null) return;`) prevents duplicate patching.
- `_harmony` is set early in `Initialize()` so the guard works even if subsequent initialization steps throw.
- `Shutdown()` calls `UnpatchAll()` to prevent patches persisting in games where the mod is disabled.

## XML Conventions

- Use `<en-US>` for English text (not `<English>`).
- Use the `TEXT_IMMFAM_{NATION}_{CLASS}` naming convention for text keys.
- `-add.xml` files add new entries. `-change.xml` files replace existing entries (must include all original values).

## Version Management

Single source of truth: `ModInfo.xml` `<modversion>` tag. When bumping the version, also add a section to `CHANGELOG.md`.
