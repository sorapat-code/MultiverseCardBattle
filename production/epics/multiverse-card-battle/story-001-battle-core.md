# Story 001: Turn-based battle core

> **Epic**: Multiverse Card Battle
> **Status**: Done — retroactive (implemented before story tracking; ACs not re-verified in the editor)
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: L
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-10-23

## Context

**GDD**: `design/game-brief.md`
**Requirement**: Brief MVP feature 1

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.5.7f1 | **Risk**: HIGH
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From the brief's "Player goal & fail state" and MVP list; values read from the code:*

- [ ] Max Energy is `TurnNumber - EnergyTurnBase`, clamped to 1..10, and refills at turn start
- [ ] Overloaded energy is locked on the owner's next turn only
- [ ] The first player opens with 3 cards, the second with 4; a mulligan is offered
- [ ] The current player draws one card at turn start and their units can attack again
- [ ] A side holds at most 7 units on the field and 10 cards in hand; a card drawn into a full hand is burned
- [ ] A Leader at 0 HP loses
- [ ] A player whose deck has 0 cards loses immediately
- [ ] If both sides lose at once the battle ends with no winner (draw)
- [ ] Every action goes through a command (play card, attack, end turn, pass chain), validated by the shared `BattleRules`

---

## Implementation Notes

Rule checks are in the static `BattleRules` (no state changes) and shared by UI, AI and commands. Turn flow is `TurnManager`; victory is `BattleManager.CheckVictory`.

---

## Out of Scope

- Story 002: card effects, keywords, chain
- Story 003: Leader skills
- Story 004: the AI that issues the commands

---

## Files

- `Assets/_Project/Scripts/Battle/TurnManager.cs`
- `Assets/_Project/Scripts/Battle/BattleManager.cs`
- `Assets/_Project/Scripts/Battle/BattlePlayer.cs`
- `Assets/_Project/Scripts/Battle/BattleRules.cs`
- `Assets/_Project/Scripts/Battle/*Command.cs`
- `Assets/_Project/Scripts/Battle/{Deck,Hand}.cs`

---

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: existing tests (not re-run for this record):
`Assets/_Project/Tests/PlayMode/{BattleTests,MulliganTests,DeckOutSecretTests,SimulationTests}.cs`

**Status**: [x] Tests exist — pass/fail not established

---

## Dependencies

- Depends on: None
- Unlocks: `story-002-card-system.md`, `story-004-ai-and-story-mode.md`
