# Family-Class-Names Feature: Implementation Spec

Part of the **ImmersiveFamilies** mod. A Harmony postfix that dynamically renames families based on their assigned family class and nation. When "Randomize Families" shuffles class assignments, families get historically appropriate names for their new class instead of keeping their default names.

## Problem

Each nation has 4 family slots, each with a default family class. Rome's Julii are Statesmen, the Fabii are Champions, etc. When the game setting "Randomize Families" is enabled, the 4 family slots are randomly assigned from the 10 available family classes. The Julii slot might become Riders instead of Statesmen — but it's still called "Julii," even though the name no longer fits the class. Meanwhile, a different slot might become Statesmen, but it won't be called "Julii" either.

This mod assigns a historically appropriate family name to every family class for each nation. Rome's Statesmen are always the Julii, Rome's Riders are always the Senecans, and so on for all 10 classes. The 4 default classes use their vanilla names. The other 6 names only appear when Randomize Families is enabled and the game assigns one of those classes — but when it does, the family is properly named rather than keeping an unrelated default name.

## Architecture

**C# harness + XML config.** The C# is ~100 lines of plumbing that never changes. The content work (choosing names for up to 130 nation/class combinations) is pure XML — no recompilation needed.

Why not pure C#: Hardcoding 130 name mappings means recompiling for every tweak.
Why not pure XML: Old World's XML modding can change a family's default class or name, but has no mechanism for "if this family got assigned class X at runtime, display name Y." That conditional logic requires code.

## Mod File Structure

```
ImmersiveFamilies/
├── Source/
│   └── ModEntryPoint.cs             # Entry point + Harmony patch
├── Infos/
│   ├── family-class-names.xml       # Config: (nation, familyClass) → text key
│   └── text-family-class-names.xml  # Display names for each mapping
├── ImmersiveFamilies.csproj         # Project file
├── ModInfo.xml                      # Mod manifest
└── bin/
    ├── ImmersiveFamilies.dll        # Built output
    └── 0Harmony.dll                 # Bundled (game doesn't ship it)
```

## Hook Point: `Game.loadFamilyClass`

```csharp
// Reference/Source/Base/Game/GameCore/Game.cs:6528
public virtual void loadFamilyClass(FamilyType eIndex, FamilyClassType eNewValue)
{
    if (getFamilyClass(eIndex) != eNewValue)
    {
        updateLastData(DirtyType.maeFamilyClass, mpCurrentData.maeFamilyClass, ref mpLastUpdateData.maeFamilyClass);
        mpCurrentData.maeFamilyClass[(int)eIndex] = eNewValue;
    }
}
```

This method is called in two places:

1. **New game setup** (`setupNew()`, Game.cs:11832) — after the Randomize Families logic has already decided the class assignment. The call receives the final result.
2. **Save game loading** (Game.cs:2327) — during deserialization, restoring persisted class assignments.

A **Harmony Postfix** on this method fires after every class assignment, in both paths. Zero per-frame overhead — it runs only when classes are assigned (~40 times at game start, once per family on load).

### What the postfix does

1. Look up the family's nation from `InfoFamily.mabNation`
2. Look up the config mapping for `(nation, familyClass)`
3. If a mapping exists, set `InfoFamily.meName` to the custom `TextType`
4. If no mapping exists, restore `InfoFamily.meName` to the original value captured at init

Step 4 ensures partial coverage is safe across game sessions. Since `InfoFamily` data is not re-read from XML between games (see below), without the restore, an unmapped family could retain a custom name from a previous game's class assignment.

After this, the entire game naturally displays the correct name everywhere — tooltips, family screens, notifications — because all UI reads `InfoFamily.meName`.

### Why `InfoFamily.meName` is directly assignable

`InfoFamily` (`Reference/Source/Base/Game/GameCore/InfoBase.cs:2927`):

```csharp
public class InfoFamily : InfoBase<FamilyType>
{
    public TextType meName = TextType.NONE;           // Public field, directly writable
    public FamilyClassType meFamilyClass = FamilyClassType.NONE;
    public List<bool> mabNation = new List<bool>();    // Indexed by NationType
    // ...
}
```

`meName` is a public field (not a property), so the postfix can write to it directly via `infos.family(eIndex).meName = newTextType`.

### Why mutating `InfoFamily.meName` is safe across game sessions

`meName` lives on the `Infos` object, which is shared state — not per-game instance data. However, this is safe because:

1. **Per-game mod isolation.** Each game session gets its own `ModSettings` and `Infos` instance based on the mods selected for that game (via `ModSettings.Init()` → `Factory.CreateInfos()`). Save files store the exact mod list in their Version attribute; loading a save reconstructs that mod list and creates a fresh `Infos`. A game without this mod loaded gets a clean `Infos` with unmodified `meName` values.

2. **Re-application on every game start.** `loadFamilyClass` fires for every family during both `setupNew()` (new game) and save deserialization. When this mod is loaded, the postfix re-sets all `meName` values for the current game's class assignments. Stale values from a previous game session (with the same mod list) are overwritten before the player sees anything.

3. **InfoFamily data is NOT re-read from XML between games** with the same mod list. `Infos.PreCreateGame()` (called before each game) only re-reads info types with the `StrictModeDeferred` flag. `InfoFamily` has no flags, so its data persists from the initial `init()` load. This is why the postfix must always set the correct name — we can't rely on XML reload to reset defaults.

## Config XML Schema

The mod reads a custom XML file mapping (nation, familyClass) pairs to text keys:

```xml
<!-- Infos/family-class-names.xml -->
<FamilyClassNames>
  <!-- Rome: 4 default classes + 6 extras for Randomize Families -->
  <Entry>
    <Nation>NATION_ROME</Nation>
    <FamilyClass>FAMILYCLASS_STATESMEN</FamilyClass>
    <Name>TEXT_IMMFAM_ROME_STATESMEN</Name>
  </Entry>
  <Entry>
    <Nation>NATION_ROME</Nation>
    <FamilyClass>FAMILYCLASS_CHAMPIONS</FamilyClass>
    <Name>TEXT_IMMFAM_ROME_CHAMPIONS</Name>
  </Entry>
  <Entry>
    <Nation>NATION_ROME</Nation>
    <FamilyClass>FAMILYCLASS_LANDOWNERS</FamilyClass>
    <Name>TEXT_IMMFAM_ROME_LANDOWNERS</Name>
  </Entry>
  <Entry>
    <Nation>NATION_ROME</Nation>
    <FamilyClass>FAMILYCLASS_PATRONS</FamilyClass>
    <Name>TEXT_IMMFAM_ROME_PATRONS</Name>
  </Entry>
  <Entry>
    <Nation>NATION_ROME</Nation>
    <FamilyClass>FAMILYCLASS_RIDERS</FamilyClass>
    <Name>TEXT_IMMFAM_ROME_RIDERS</Name>
  </Entry>
  <!-- ... up to 10 classes per nation, 13 nations = up to 130 entries -->
</FamilyClassNames>
```

**Partial coverage is fine.** If a (nation, familyClass) pair has no entry, the family keeps its default name. Content authors can provide entries for just a few nations, or just a few classes within a nation — they don't need to cover all 10 classes.

**Entries for default assignments are optional.** Without them, the default family names remain (e.g., Rome's Statesmen stay "Julii" as in vanilla). Including them is recommended so the mod is the single source of truth for all family naming, but functionally the vanilla defaults already match.

## Text XML

The display names, using Old World's standard text format. The tilde-separated forms are: singular, plural, possessive (for English).

```xml
<!-- Infos/text-family-class-names.xml -->
<Root>
  <!-- Rome -->
  <Entry>
    <zType>TEXT_IMMFAM_ROME_STATESMEN</zType>
    <en-US>Julii~Julii~Julii</en-US>
  </Entry>
  <Entry>
    <zType>TEXT_IMMFAM_ROME_CHAMPIONS</zType>
    <en-US>Fabii~Fabii~Fabii</en-US>
  </Entry>
  <Entry>
    <zType>TEXT_IMMFAM_ROME_LANDOWNERS</zType>
    <en-US>Claudii~Claudii~Claudii</en-US>
  </Entry>
  <Entry>
    <zType>TEXT_IMMFAM_ROME_PATRONS</zType>
    <en-US>Valerii~Valerii~Valerii</en-US>
  </Entry>
  <Entry>
    <zType>TEXT_IMMFAM_ROME_RIDERS</zType>
    <en-US>Senecans~Senecans~Senecans</en-US>
  </Entry>
  <!-- etc. -->
</Root>
```

**Localization:** Add additional language tags (`<es>`, `<de>`, `<fr>`, etc.) per entry for multi-language support. See `Reference/XML/Infos/text-family.xml` for the full format including Russian case inflections.

## C# Implementation

### Entry Point

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
using HarmonyLib;
using TenCrowns.AppCore;
using TenCrowns.GameCore;
using UnityEngine;

namespace ImmersiveFamilies
{
    public class ModEntryPoint : ModEntryPointAdapter
    {
        private static Harmony _harmony;
        private const string HarmonyId = "com.spiderj90.immersivefamilies";

        // (nationZType, familyClassZType) → textZType
        internal static Dictionary<(string, string), string> NameMappings;

        // Original meName per family, captured at init before any mutations
        internal static Dictionary<FamilyType, TextType> OriginalNames;

        public override void Initialize(ModSettings modSettings)
        {
            base.Initialize(modSettings);

            if (_harmony != null) return; // Triple-load guard

            try
            {
                // Capture original family names before any postfix mutations
                Infos infos = modSettings.Infos;
                OriginalNames = new Dictionary<FamilyType, TextType>();
                for (FamilyType f = 0; f < infos.familiesNum(); f++)
                {
                    OriginalNames[f] = infos.family(f).meName;
                }

                NameMappings = LoadConfig();

                _harmony = new Harmony(HarmonyId);
                _harmony.PatchAll(typeof(ModEntryPoint).Assembly);
                Debug.Log($"[ImmersiveFamilies] Loaded {NameMappings.Count} name mappings, captured {OriginalNames.Count} original names.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ImmersiveFamilies] Failed to initialize: {ex}");
            }
        }

        public override void Shutdown()
        {
            _harmony?.UnpatchAll(HarmonyId);
            _harmony = null;
            NameMappings = null;
            OriginalNames = null;
            base.Shutdown();
        }

        private static Dictionary<(string, string), string> LoadConfig()
        {
            var mappings = new Dictionary<(string, string), string>();

            // Derive mod directory from the DLL location.
            // IModPath does not expose a filesystem path; its API is for
            // mod list management (loading, CRC, etc.), not file access.
            string dllPath = Assembly.GetExecutingAssembly().Location;
            string modDir = Path.GetDirectoryName(dllPath);
            string configPath = Path.Combine(modDir, "Infos", "family-class-names.xml");

            if (!File.Exists(configPath))
            {
                Debug.LogWarning("[ImmersiveFamilies] No config file found at: " + configPath);
                return mappings;
            }

            var doc = new XmlDocument();
            doc.Load(configPath);

            foreach (XmlNode entry in doc.SelectNodes("//Entry"))
            {
                string nation = entry.SelectSingleNode("Nation")?.InnerText;
                string familyClass = entry.SelectSingleNode("FamilyClass")?.InnerText;
                string name = entry.SelectSingleNode("Name")?.InnerText;

                if (nation != null && familyClass != null && name != null)
                {
                    mappings[(nation, familyClass)] = name;
                }
            }

            return mappings;
        }
    }
}
```

### Harmony Patch

```csharp
namespace ImmersiveFamilies
{
    [HarmonyPatch(typeof(Game), nameof(Game.loadFamilyClass))]
    public static class PatchLoadFamilyClass
    {
        public static void Postfix(Game __instance, FamilyType eIndex, FamilyClassType eNewValue)
        {
            if (ModEntryPoint.NameMappings == null) return;
            if (eNewValue == FamilyClassType.NONE) return;

            Infos infos = __instance.infos();
            InfoFamily familyInfo = infos.family(eIndex);
            if (familyInfo == null) return;

            // Find which nation this family belongs to
            string nationZType = null;
            for (NationType n = 0; (int)n < familyInfo.mabNation.Count; n++)
            {
                if (familyInfo.mabNation[(int)n])
                {
                    nationZType = infos.nation(n).mzType;
                    break;
                }
            }
            if (nationZType == null) return;

            // Look up the family class zType
            string classZType = infos.familyClass(eNewValue).mzType;

            if (ModEntryPoint.NameMappings.TryGetValue(
                    (nationZType, classZType), out string textZType))
            {
                // Mapped — set custom name
                TextType newName = infos.getType<TextType>(textZType);
                if (newName != TextType.NONE)
                {
                    familyInfo.meName = newName;
                }
                else
                {
                    Debug.LogWarning($"[ImmersiveFamilies] Text key not found: {textZType}");
                }
            }
            else if (ModEntryPoint.OriginalNames != null
                     && ModEntryPoint.OriginalNames.TryGetValue(eIndex, out TextType originalName))
            {
                // Unmapped — restore vanilla default
                familyInfo.meName = originalName;
            }
        }
    }
}
```

### Key Implementation Notes

**`mzType` field:** `InfoBase<T>` (the base class of `InfoFamily`, `InfoFamilyClass`, `InfoNation`) exposes `mzType` as a public string field containing the XML `zType` value (e.g., `"NATION_ROME"`, `"FAMILYCLASS_STATESMEN"`). This is how we bridge between runtime objects and the config XML's string keys.

**`infos.getType<TextType>(string)`:** Converts a text key string (e.g., `"TEXT_IMMFAM_ROME_STATESMEN"`) to the corresponding `TextType` enum value. There is a one-argument overload (Infos.cs:3173) that delegates to the two-argument version with `showError: true`. The text entries must be loaded first via the mod's `text-family-class-names.xml` — Old World's mod loader automatically loads `text-*.xml` files from the mod's `Infos/` directory.

**Nation lookup from `mabNation`:** Each family has a `List<bool>` indexed by `NationType`. Most families belong to exactly one nation (one `true` entry). The loop finds the first matching nation.

**`Game __instance`:** Harmony's `__instance` parameter gives access to the `Game` object, from which `infos()` provides the full `Infos` lookup table (Game.cs:2843). Since `Game.loadFamilyClass` is in `TenCrowns.GameCore.dll` (not `Assembly-CSharp`), we can reference the type directly with `[HarmonyPatch]` attributes — no runtime type resolution needed.

**Config file path:** `IModPath` does not expose a filesystem path (its API is for mod list management: loading, CRC, compatibility checks). To find the mod's own directory, we use `Assembly.GetExecutingAssembly().Location` to get the DLL path and derive the mod root from there.

## Project File and ModInfo.xml

The project file (`ImmersiveFamilies.csproj`) and mod manifest (`ModInfo.xml`) already exist in the project root. See `docs/modding-guide-csharp.md` for the standard structure.

Build: `dotnet build -p:OldWorldPath="/path/to/Old World"`

## Edge Cases

### Save/Load

Handled automatically. `loadFamilyClass` is called during save deserialization (Game.cs:2327), so the postfix re-applies name overrides on every load. No data is persisted by the mod — it's purely a display-time transformation.

### Cross-Game Isolation

The mod mutates `InfoFamily.meName` on the shared `Infos` object. This does not leak to games without the mod because:

- **Different mod list → different Infos.** Each game session gets its own `ModSettings` with its own `Infos` instance, built from that game's selected mods (`ModSettings.Init()` → `Factory.CreateInfos()`). Save files store the exact mod list in their Version attribute; loading a save reconstructs that list and creates a fresh `Infos` with data read from XML.
- **Same mod list → postfix re-applies.** If the mod stays loaded across game sessions, `loadFamilyClass` fires for every family on each new game / load, so the postfix always sets the correct names for the current game's assignments.

Note: `InfoFamily` data is only read from XML during `Infos.init()` (when the `Infos` object is first created). It is NOT re-read by `Infos.PreCreateGame()` between games — `PreCreateGame` only re-reads info types with the `StrictModeDeferred` flag, which `InfoFamily` does not have.

### Multiplayer

Safe. The mod only mutates `InfoFamily.meName` (a display-only field). It does not change gameplay state (`maeFamilyClass`, opinions, effects). All players with the mod see the same names because the mapping is deterministic from (nation, familyClass).

### Nations with Fewer Than 10 Family Classes

Most nations have 4 families. With Randomize Families, those 4 families get 4 of the 10 available classes. The config only needs entries for the classes that might actually appear. Missing entries gracefully fall through to the default name.

### DLC Nations

The same mechanism works for DLC nations (Aksum, Maurya, Yuezhi, Tamil, Hittite, Kush). Content authors add entries for whichever nations they want to support.

### Multiple Families Per Nation Getting Same Config

Not possible. The game's `setupNew()` logic (Game.cs:11790-11834) tracks `seClassChosen` per nation and never assigns the same class to two families of the same nation.

## Default Family-Class Assignments (Reference)

For content authors — the vanilla mappings. With Randomize Families off, these are always used:

| Nation | Family | Default Class |
|--------|--------|---------------|
| Assyria | Sargonid | Champions |
| Assyria | Tudiya | Hunters |
| Assyria | Adasi | Patrons |
| Assyria | Erishum | Clerics |
| Babylonia | Kassite | Hunters |
| Babylonia | Chaldean | Artisans |
| Babylonia | Isin | Traders |
| Babylonia | Amorite | Sages |
| Carthage | Barcid | Riders |
| Carthage | Magonid | Artisans |
| Carthage | Hannonid | Traders |
| Carthage | Didonian | Statesmen |
| Egypt | Ramesside | Riders |
| Egypt | Saite | Landowners |
| Egypt | Amarna | Clerics |
| Egypt | Thutmosid | Sages |
| Greece | Argead | Champions |
| Greece | Cypselid | Artisans |
| Greece | Seleucid | Patrons |
| Greece | Alcmaeonid | Sages |
| Persia | Sasanid | Clerics |
| Persia | Mihranid | Hunters |
| Persia | Arsacid | Riders |
| Persia | Achaemenid | Statesmen |
| Rome | Fabius | Champions |
| Rome | Claudius | Landowners |
| Rome | Valerius | Patrons |
| Rome | Julius | Statesmen |
| Hittite | Kussaran | Riders |
| Hittite | Nenassan | Landowners |
| Hittite | Zalpuwan | Patrons |
| Hittite | Hattusan | Traders |
| Kush | Yam | Hunters |
| Kush | Irtjet | Artisans |
| Kush | Wawat | Traders |
| Kush | Setju | Landowners |
| Aksum | Agaw | Champions |
| Aksum | Agazi | Traders |
| Aksum | Tigrayan | Clerics |
| Aksum | Barya | Patrons |
| Maurya | Magadha | Champions |
| Maurya | Gandhara | Hunters |
| Maurya | Kamboja | Riders |
| Maurya | Avanti | Sages |
| Maurya | Vatsa | Patrons |
| Maurya | Kosala | Statesmen |
| Yuezhi | Xiumi | Riders |
| Yuezhi | Shuangmi | Champions |
| Yuezhi | Xidun | Clerics |
| Yuezhi | Dumi | Traders |
| Tamil | Pandya | Traders |
| Tamil | Chola | Artisans |
| Tamil | Chera | Landowners |

Note: Maurya has 6 families (the only nation with more than 4). Tamil has 3.

## Source References

| What | File | Line |
|------|------|------|
| `loadFamilyClass` | `Reference/Source/Base/Game/GameCore/Game.cs` | 6528 |
| `getFamilyClass` | `Reference/Source/Base/Game/GameCore/Game.cs` | 6520 |
| `setupNew` (randomization) | `Reference/Source/Base/Game/GameCore/Game.cs` | 11786 |
| `loadFamilyClass` call in setup | `Reference/Source/Base/Game/GameCore/Game.cs` | 11832 |
| `loadFamilyClass` call on load | `Reference/Source/Base/Game/GameCore/Game.cs` | 2327 |
| `Game.infos()` | `Reference/Source/Base/Game/GameCore/Game.cs` | 2843 |
| `InfoFamily` class | `Reference/Source/Base/Game/GameCore/InfoBase.cs` | 2927 |
| `Infos.family()` | `Reference/Source/Base/Game/GameCore/Infos.cs` | 3533 |
| `Infos.getType<T>(string)` (one-arg) | `Reference/Source/Base/Game/GameCore/Infos.cs` | 3173 |
| `Infos.getType<T>(string, bool)` | `Reference/Source/Base/Game/GameCore/Infos.cs` | 3187 |
| `Infos.init()` (XML loading) | `Reference/Source/Base/Game/GameCore/Infos.cs` | 1128 |
| `Infos.PreCreateGame()` | `Reference/Source/Base/Game/GameCore/Infos.cs` | 802 |
| `Infos.ReadInfoListData()` | `Reference/Source/Base/Game/GameCore/Infos.cs` | 809 |
| `InfoFamily` in info list (no flags) | `Reference/Source/Base/Game/GameCore/Infos.cs` | 585 |
| `ModSettings.Init()` | `Reference/Source/Base/Game/GameCore/ModSettings.cs` | 75 |
| `ModSettings.OnModChanged()` | `Reference/Source/Base/Game/GameCore/ModSettings.cs` | 111 |
| `ModSettings.CreateServerGame()` | `Reference/Source/Base/Game/GameCore/ModSettings.cs` | 129 |
| `IModPath` interface | `Reference/Source/Base/Game/GameCore/IModPath.cs` | 65 |
| `IModEntryPoint` | `Reference/Source/Base/Game/AppCore/IModEntryPoint.cs` | 1 |
| Family XML | `Reference/XML/Infos/family.xml` | — |
| Family class XML | `Reference/XML/Infos/familyClass.xml` | — |
| Family text XML | `Reference/XML/Infos/text-family.xml` | — |
| C# modding guide | `docs/modding-guide-csharp.md` | — |
