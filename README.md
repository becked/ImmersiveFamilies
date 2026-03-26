# Immersive Families

An Old World mod that gives every family class a historically appropriate name per nation. When "Randomize Families" is enabled, families are dynamically renamed based on their assigned class rather than keeping their default slot name.

Without this mod, Rome's Statesmen slot is always called "Julii" even if Randomize Families assigns it the Riders class. With this mod, any Rome family assigned Riders is called "Senecans," any assigned Statesmen is called "Julii," and so on for all 10 classes.

The 4 default family names appear as normal. The 6 additional names only show up when Randomize Families shuffles a non-default class into a family slot.

## Adding a nation

All content is defined in XML files under `Infos/`. No C# changes are needed to add nations or change names.

### 1. Define name mappings (`Infos/family-class-names.xml`)

Add one entry per (nation, familyClass) pair. The `Name` field references a text key defined in step 2.

```xml
<Entry>
  <Nation>NATION_ROME</Nation>
  <FamilyClass>FAMILYCLASS_RIDERS</FamilyClass>
  <Name>TEXT_IMMFAM_ROME_RIDERS</Name>
</Entry>
```

**Partial coverage is fine.** You don't need all 10 classes for a nation. Unmapped classes keep their vanilla family name.

**Entries for default classes are optional.** The vanilla defaults already display correctly without them.

### Valid values

**Nations:** `NATION_ROME`, `NATION_ASSYRIA`, `NATION_BABYLONIA`, `NATION_CARTHAGE`, `NATION_EGYPT`, `NATION_GREECE`, `NATION_PERSIA`, `NATION_HITTITE`, `NATION_KUSH`, `NATION_AKSUM`, `NATION_MAURYA`, `NATION_YUEZHI`, `NATION_TAMIL`

**Family classes:** `FAMILYCLASS_LANDOWNERS`, `FAMILYCLASS_CHAMPIONS`, `FAMILYCLASS_STATESMEN`, `FAMILYCLASS_PATRONS`, `FAMILYCLASS_CLERICS`, `FAMILYCLASS_SAGES`, `FAMILYCLASS_TRADERS`, `FAMILYCLASS_ARTISANS`, `FAMILYCLASS_RIDERS`, `FAMILYCLASS_HUNTERS`

### 2. Define display names (`Infos/text-family-class-names.xml`)

Add a text entry for each key referenced in step 1. The `<en-US>` value uses tilde-separated forms: `singular~plural~possessive`.

```xml
<Entry>
  <zType>TEXT_IMMFAM_ROME_RIDERS</zType>
  <en-US>Senecans~Senecans~Senecans</en-US>
</Entry>
```

Use the naming convention `TEXT_IMMFAM_{NATION}_{CLASS}` for consistency.

### 3. Define family colors (`Infos/color-add.xml`, `Infos/playerColor-add.xml`, `Infos/teamColor-change.xml`)

Each family class gets a consistent color per nation. The mod assigns a color index based on the family class (0-9, matching the order in `familyClass.xml`):

| Index | Class | Rome vanilla color |
|-------|-------|--------------------|
| 0 | Landowners | `#ffae8b` (salmon) |
| 1 | Champions | `#c9abff` (lavender) |
| 2 | Statesmen | `#6a86d7` (blue) |
| 3 | Patrons | `#e9bf4e` (gold) |
| 4 | Clerics | needs new color |
| 5 | Sages | needs new color |
| 6 | Traders | needs new color |
| 7 | Artisans | needs new color |
| 8 | Riders | needs new color |
| 9 | Hunters | needs new color |

To add colors for a new nation, you need entries in three files:

**`color-add.xml`** -- Two entries per new color (base + text variant):
```xml
<Entry>
  <zType>COLOR_NATION_ROME_FAMILY_05</zType>
  <zName>Rome Family 5</zName>
  <ColorClass>COLORCLASS_NATIONS</ColorClass>
  <zHexValue>#7ec8a4</zHexValue>
</Entry>
<Entry>
  <zType>COLOR_NATION_ROME_FAMILY_05_TEXT</zType>
  <zName>Rome Family 5 Text</zName>
  <ColorClass>COLORCLASS_NATIONS</ColorClass>
  <zHexValue>#7ec8a4</zHexValue>
</Entry>
```

**`playerColor-add.xml`** -- One entry per new color:
```xml
<Entry>
  <zType>PLAYERCOLOR_NATION_ROME_FAMILY_05</zType>
  <AssetColor>COLOR_NATION_ROME_FAMILY_05</AssetColor>
  <TextColor>COLOR_NATION_ROME_FAMILY_05_TEXT</TextColor>
  <BorderColor>COLOR_NATION_ROME_FAMILY_05</BorderColor>
  <CrestColor>COLOR_NATION_ROME_FAMILY_05</CrestColor>
</Entry>
```

**`teamColor-change.xml`** -- Extend the nation's player color and border pattern arrays to 10+ entries. This file replaces the vanilla arrays, so include all original entries plus new ones.

## Building

```sh
cp .env.example .env
# Edit .env with your Old World install and mods paths
./scripts/deploy.sh
```

## Default family-class assignments

For reference, the vanilla mappings when Randomize Families is off:

| Nation | Family | Default Class |
|--------|--------|---------------|
| Rome | Fabius | Champions |
| Rome | Claudius | Landowners |
| Rome | Valerius | Patrons |
| Rome | Julius | Statesmen |
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

Maurya has 6 families. Tamil has 3. All others have 4.
