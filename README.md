# WayToManyDrugs 3.7.0

A content mod for **Schedule I** by **0.Seek**.

New drugs to cook, press, bake and grow, the suppliers who move them, a crew that runs weight
overseas, and the law, the heat and the snitches that come with all of it.

---

## Requirements

| | |
| --- | --- |
| Game | **Schedule I** (TVGS) — tested on 0.4.6f13, Unity 2022.3.62f2 |
| Mod loader | **MelonLoader 0.7.3** (Open Beta, .NET 6 runtime) |
| Required dependencies | **S1API** — the mod will not load without it |
| | **S1MAPI** — every custom product model is loaded through it: Xanax now, more to come |

> **S1API and S1MAPI are both required.** Starting with Xanax in 3.7.0, every custom product model this
> mod adds is loaded by **S1MAPI**'s GLB loader rather than by a loader of the mod's own, and the models
> still to come are modelled for the same path. Without S1MAPI installed, those models will not load.

## Installation

1. Install **MelonLoader 0.7.3** into your Schedule I folder and run the game once.
2. Put **S1API** into `Schedule I\Mods\`.
3. Put **S1MAPI** into `Schedule I\UserLibs\`.
4. Put **WayToManyDrugs.dll** from this zip into `Schedule I\Mods\`.
5. Start the game. The mod registers its products as it loads.

```
Schedule I\
├── Mods\
│   ├── S1API.Il2Cpp.MelonLoader.dll
│   └── WayToManyDrugs.dll
└── UserLibs\
    └── S1MAPI_Il2Cpp.dll
```

Nothing else in this zip needs to be installed - the README is for reading.

## What is new in 3.7.0

**Xanax** — a new product, in two forms, and the first product to arrive with a model of its own.
**Dr. Eleanor Scrivens** at PillVille skims unpressed **Xanax Powder** out of the pharmacy's shipments
and will move it for you, so order the powder on your phone and then press it into **Xanax bars** with
the brick press. She is also how you get in. Both the powder and the bars are models loaded through
**S1MAPI**, which is how the products modelled from here on will load too.

**Offshore selling** — Captain Declan Cross is at the docks. Hand over a consignment, wait out the
voyage (about three minutes) and collect the cash when the boat is back. The destinations pay far
better than the street, and there is heat attached to moving that much weight. A **run one up**
button on the phone starts the next run without waiting to be called again, with a cooldown so a
refused run cannot be pushed over and over.

**PillVille** — new supplier dialogue and a Xanax order/recipe hook, so powder and bars are part of
the supplier loop rather than a one-off.

**DMT trip audio** — the whisper that plays under a DMT trip now runs for the whole trip. It used to
be cut off a moment after the trip began, which left the trip itself silent.

**Performance** — the runaway stream of errors raised by DMT station items, tens of thousands of them
in a session, is fixed at the source, along with the UI drag handling behind it. Frame rate in and
around stations is where it should be again.

**Smaller things** — MDMA no longer replaces the player's pupils with hearts.

## What the mod adds

### Products

| Product | |
| --- | --- |
| **MDMA** | Molly, made at the cauldron and mixed from its own ingredients. |
| **DMT** | A trip of its own: colour, walls that breathe, and a whisper under it. |
| **THC Gummies** | Edible. |
| **THC Cookie** | Edible. |
| **Brownie** | Edible. |
| **Vape Cart** | Three grades: Vape Cart, Premium Vape Cart and Heavenly Vape Cart. |
| **Salvia** | Its own plant and its own product. |
| **Xanax** *(new)* | Pills and powder. The powder comes from PillVille, the bars come off the press. |

### Suppliers and bulk orders

Gus Fring, Marty Mellows, Remy Fogarty, Roscoe Bellweather, Sal Viah, Stella Vance, Damon Trey,
PillVille and the ring around them, each with their own dialogue, pricing and bulk-order meetups.

### Story, law and heat

A snitching storyline that runs through evidence, dead drops, phone deliveries, confrontations and
a few side quests, plus undercover stings, busts and a heat system to stay under.

### Quality of life

A brighter flashlight, NPC memory and customer loyalty, and guards around the game's own failure
paths so one bad item or clone cannot take the frame rate down with it.

## Controls

| Key | Action |
| --- | --- |
| **F2** | Request a bulk meetup with Damon Trey |
| **F5** | Request a bulk meetup with PillVille |
| **F6** | Request a bulk meetup with Sal Viah |
| **F7** | Request a bulk meetup with Roscoe Bellweather |
| **F12** | Admin menu (Escape or Enter to close) |

## Notes

- **Verbose logging** is off by default. Turn on `VerboseLogging` under the `WVC_Logging` category in
  `UserData\MelonPreferences.cfg` if you want the mod to narrate what it is doing.
- **Custom models need S1MAPI.** The mod no longer carries a model loader of its own: since 3.7.0 its
  models go through S1MAPI's GLB loader, starting with Xanax. Keep `S1MAPI_Il2Cpp.dll` in `UserLibs\`,
  or those models will not appear - the mod reports it in the log rather than failing quietly.
- The mod keeps a small cache file (`WVC_npc_activity_cache.txt`) beside itself in `Mods\`.
- A DMT trip is the heaviest thing here: a lot of colour, bloom and full-screen distortion. That is
  the look rather than a fault, but on a weaker card it will cost frames while it lasts.
- **Back up your save** before adding or removing any content mod, this one included.
- If something does go wrong, the mod says so in the MelonLoader console instead of failing quietly.
  The `[WVC ...]` lines in `MelonLoader\Latest.log` are the ones worth attaching to a bug report.

## Uninstalling

Delete `WayToManyDrugs.dll` from `Schedule I\Mods\`. Saves keep the items they already hold.
