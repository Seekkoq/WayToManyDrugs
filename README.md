# WayToManyDrugs 3.9.1

A content mod for **Schedule I** by **0.Seek**.

New drugs to cook, press, bake and grow, the suppliers who move them, a crew that runs weight
overseas, and the law, the heat and the snitches that come with all of it.

---

## Requirements

| | |
| --- | --- |
| Game | **Schedule I** (TVGS) — tested on 0.4.7f12, Unity 2022.3.62f2 |
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

## What is new in 3.9.1

**Built for Schedule I 0.4.7** — the mod now targets the game's current assemblies and the matching
**S1API**, so everything 0.4.7 reworked is reconnected end to end. That includes the phone messaging
system: outbound texts send again, Declan Cross's reply options (status, details, offers, decline)
are back in the thread, and each custom NPC's thread shows their **real name** instead of a leaked
identifier or another NPC's.

**Pawnshop money laundering** — Mick Lubbin, at the Northtown pawn shop, will run **$20,000 of
cash through the shop and into your bank account**, 1:1 and fee-free: hand the cash over now,
come back **one in-game day** later and claim it. Tracked per save, refuses partial stacks and
second handovers while money is already waiting, and `wvc_pawn_dialogue off` keeps Mick's own
conversation clean.

**Casino laundering appends instead of replacing** — Philip Wentworth's own dialogue (the buy
option and the rest) stays exactly as the game ships it; the laundry is added next to it as one
extra choice. The payout is also a **claim flow** now: give him the $65,000, wait two in-game
days, then collect it into your bank. `wvc_casino_dialogue off` removes only the added choice.

**Offshore payouts wait too** — after Declan's voyage returns, the payout can be collected
**two in-game days** later instead of immediately.

**Debug commands** — six `wvc_casino_*` and six `wvc_pawn_*` console commands (plus `wvc_money`)
cover status, finding the NPC, handing money over, claiming, resetting, the dialogue append
switch and reading balances. They are listed under Console commands below.

**One NPC, one option** — the laundry choice is now attached to that NPC's own dialogue menu
through the game's own choice mechanism, so Philip only ever shows the cage option and Mick only
ever shows the shop option. When waiting money becomes collectible, the NPC handling it **texts
the player** a reminder.

**Weather chatter corrected** — greetings that belong to a different sky (a sun line cached while
the rain came in, a rain line with no rain outside) are reworded or forgotten now, and the mood
follows the weather profile the game is actually running before the blended conditions, so the
wrong-weather lines are much less likely.

**Weather dialogue no longer sticks after sleep** — sleeping no longer leaves you on the same
weather greeting; it re-rolls against the sky you actually wake up to.

**DMT comes down now** — the trip **fades out** over time instead of cutting off abruptly, and it
carries a few extra effects on the way.

**A change log on the main menu** — a **change-log panel** joins the version watermark on the main
menu, so what changed is readable before you load in. Both stay hidden until the boot disclaimer
clears.

**NPC faces on packaged product** — the face shown on packaged product was wrong; it now uses the
right one.

**More FPS drop fixes** — further guards around the heaviest paths, on top of the earlier
frame-rate work.

## What the mod adds

### Products

| Product | |
| --- | --- |
| **MDMA** | Molly, made at the cauldron and mixed from its own ingredients. |
| **DMT** | A trip of its own: colour, walls that breathe, and a whisper under it — and a **come-down** that fades out instead of cutting off. |
| **THC Gummies** | Edible. |
| **THC Cookie** | Edible. |
| **Brownie** | Edible. |
| **Vape Cart** | Three grades: Vape Cart, Premium Vape Cart and Heavenly Vape Cart. |
| **Salvia** | Its own plant and its own product. |
| **Xanax** *(new)* | Pills and powder. The powder comes from PillVille, the bars come off the press. |

### Suppliers and bulk orders

Gus Fring, Marty Mellows, Remy Fogarty, Roscoe Bellweather, Sal Viah, Stella Vance, Damon Trey,
PillVille and the ring around them, each with their own dialogue, pricing and bulk-order meetups.

### Casino money laundering

Philip Wentworth works the cage at the casino. Hand him **$65,000 in cash** now and the same
$65,000 can be collected into your **bank account** two in-game days later — no fee, no heat, no
police — tracked per save, one stack waiting at a time. The laundry is **appended to his own
dialogue** as one extra choice, so his vanilla conversation (buy and all) never changes;
`wvc_casino_dialogue off` removes the added choice, and `EnableCasinoLaundering` in the
`WVC_Casino` category of `MelonPreferences.cfg` turns the whole feature off.

### Pawnshop money laundering

Mick Lubbin runs the Northtown pawn shop. Hand him **$20,000 in cash** and collect the same
$20,000 into your **bank account** one in-game day later — the same quiet deal as the casino,
also appended to Mick's own dialogue rather than replacing it. `wvc_pawn_dialogue off` removes
the added choice, and `EnablePawnLaundering` in the `WVC_Pawnshop` category of
`MelonPreferences.cfg` turns the feature off.

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

## Console commands

Enable the console in **Settings -> Game** (tick "enable the console"), then press **~** during
play. Everything these commands do can also be reached in play through Philip or Mick.

| Command | Effect |
| --- | --- |
| `wvc_casino_status` | Feature flags, armed state, waiting money and days left, cash, bank and shortfall in one line. |
| `wvc_casino_find` | Logs where Philip Wentworth stands and warps the player to him. |
| `wvc_casino_launder [amount] [force]` | Hands cash to Philip without the dialogue. Defaults to $65,000; `force` skips the waiting-money check only — the cash still has to exist. |
| `wvc_casino_claim [force]` | Collects the waiting money into the bank. `force` skips the two-day wait only. |
| `wvc_casino_reset` | Clears the waiting money for the active save. |
| `wvc_casino_dialogue on\|off` | On appends the laundry to Philip's own dialogue, off removes it again. |
| `wvc_pawn_status` | The pawnshop equivalent of `wvc_casino_status`. |
| `wvc_pawn_find` | Logs where Mick Lubbin stands and warps the player to him. |
| `wvc_pawn_launder [amount] [force]` | Hands cash to Mick without the dialogue. Defaults to $20,000; `force` skips the waiting-money check only. |
| `wvc_pawn_claim [force]` | Collects the waiting money into the bank. `force` skips the one-day wait only. |
| `wvc_pawn_reset` | Clears the waiting money for the active save. |
| `wvc_pawn_dialogue on\|off` | On appends the laundry to Mick's own dialogue, off removes it again. |
| `wvc_money` | Shows cash and bank balances. |

## Known issues

- **Loading times are inconsistent** between runs.

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
