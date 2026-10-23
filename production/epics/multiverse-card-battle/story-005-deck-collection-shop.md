# Story 005: Deck builder, collection, shop and craft

> **Epic**: Multiverse Card Battle
> **Status**: Done — retroactive (implemented before story tracking; ACs not re-verified in the editor)
> **Layer**: Feature
> **Type**: UI
> **Estimate**: L
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-10-23

## Context

**GDD**: `design/game-brief.md`
**Requirement**: Brief MVP feature 5

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.5.7f1 | **Risk**: HIGH
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

- [ ] A deck needs at least 30 cards, with up to 3 copies of a card
- [ ] The Collection screen lists owned cards, with a filter bar and a detail modal
- [ ] A pack costs 100 gold (default) and holds 5 cards, drawn Common 68 / Rare 22 / Epic 8 / Legendary 2 (weights)
- [ ] Cards can be crafted for a rarity-based cost
- [ ] Starter decks are available to a new profile
- [ ] Gold comes from play only; there is no real-money purchase
- [ ] Daily login (7-day cycle), daily quests (3 a day), achievements and mastery grant rewards

---

## Implementation Notes

`PackService` rolls packs, `CardEconomy` holds craft costs, `DeckBuilder` validates decks. The pack price can differ per set (`CardSetInfo.PackPrice`).

---

## Out of Scope

- Story 004: earning gold from battles

---

## Files

- `Assets/_Project/Scripts/Deck/DeckBuilder.cs`
- `Assets/_Project/Scripts/Data/{PackService,CardEconomy,StarterDecks,DailyService,AchievementService,MasteryService,MilestoneService}.cs`
- `Assets/_Project/Scripts/Screens/{DeckBuilderScreen,CollectionScreen,CardFilterBar,CardDetailModal,MainMenuScreen}.cs`

---

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: UI
**Required evidence**: a retained screenshot of each screen touched, in `production/qa/evidence/`
Existing tests (not re-run): `Assets/_Project/Tests/EditMode/{EconomyTests,DataTests,DailyMasteryTests}.cs`

**Status**: [ ] Screenshots not yet created

---

## Dependencies

- Depends on: `story-002-card-system.md`
- Unlocks: None
