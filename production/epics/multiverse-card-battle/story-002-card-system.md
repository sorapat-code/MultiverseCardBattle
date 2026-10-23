# Story 002: Card system, keywords and chain

> **Epic**: Multiverse Card Battle
> **Status**: Done — retroactive (implemented before story tracking; ACs not re-verified in the editor)
> **Layer**: Core
> **Type**: Logic
> **Estimate**: L
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-10-23

## Context

**GDD**: `design/game-brief.md`
**Requirement**: Brief MVP feature 2

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.5.7f1 | **Risk**: HIGH
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

- [ ] Cards are Unit, Spell, Equipment or Leader, with rarity Common to Secret and a universe/faction
- [ ] More than 20 keywords are defined (e.g. Taunt, Charge, Rush, Shield, Lifesteal, Stealth, DoubleStrike, Pierce, Ward, Immune, Reborn) and each applies its documented rule
- [ ] Card abilities are data: `Trigger [conditions] @Target -> Effect(args)` scripts parse into actions
- [ ] Cards load in order: assets in `Resources/Cards`, then CSV, then code; on a duplicate ID the one loaded first wins
- [ ] A bad CSV row is skipped and reported as an error without blocking the other rows
- [ ] A Fast spell can be played on the opponent's turn and answers on the chain
- [ ] A chain is at most 12 deep
- [ ] A `Unique` card is limited to one copy per deck

---

## Implementation Notes

Effects run through `EffectExecutor`; targets through `TargetResolver`; triggers through `TriggerManager`; statuses through `StatusProcessor`. CSV format: `docs/CardDatabase.md`.

---

## Out of Scope

- Story 003: Leader skills
- Story 005: owning cards and building decks

---

## Files

- `Assets/_Project/Scripts/Cards/*`
- `Assets/_Project/Scripts/Effects/*`
- `Assets/_Project/Scripts/Conditions/*`
- `Assets/_Project/Scripts/Targeting/*`
- `Assets/_Project/Scripts/Battle/{TriggerManager,StatusProcessor,ChainLink}.cs`
- `Assets/_Project/Scripts/Data/Import/*`, `Data/CardRegistry.cs`

---

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: existing tests (not re-run for this record):
`Assets/_Project/Tests/PlayMode/{KeywordStatusTests,ChainTests,EquipmentTests,TriggerSetTests,RosterEffectTests}.cs`,
`Assets/_Project/Tests/EditMode/{CardImportTests,KeywordDataTests}.cs`

**Status**: [x] Tests exist — pass/fail not established

---

## Dependencies

- Depends on: `story-001-battle-core.md`
- Unlocks: `story-003-leaders.md`, `story-005-deck-collection-shop.md`
