# Story 003: Leaders and leader skills

> **Epic**: Multiverse Card Battle
> **Status**: Done — retroactive (implemented before story tracking; ACs not re-verified in the editor)
> **Layer**: Feature
> **Type**: Logic
> **Estimate**: M
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-10-23

## Context

**GDD**: `design/game-brief.md`
**Requirement**: Brief MVP feature 3

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.5.7f1 | **Risk**: HIGH
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

- [ ] Each player may have a Leader with its own skill, a Power that accumulates, and a once-per-turn skill use
- [ ] A Leader's finisher card is given once (`LeaderFinisherGiven`)
- [ ] A Leader can wear gear for a limited number of turns
- [ ] Leaders are defined in `LeaderLibrary` (e.g. Goku, Naruto, Gojo, Aemeath) and unlock under their unlock rules
- [ ] Leader-specific mechanics behave as their tests expect (Aemeath, Ainz, Artorias, Vergil, Zhongli shield-heal once per turn)

---

## Implementation Notes

Leader state is on `BattlePlayer`; skills run through `LeaderSystem`.

---

## Out of Scope

- Story 002: the generic effect system leaders reuse

---

## Files

- `Assets/_Project/Scripts/Battle/LeaderSystem.cs`
- `Assets/_Project/Scripts/Data/LeaderLibrary.cs`
- `Assets/_Project/Scripts/Screens/{LeaderInfoPanel,AemeathScene,VergilScene}.cs`

---

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: existing tests (not re-run for this record):
`Assets/_Project/Tests/PlayMode/{AemeathTests,AinzTests,ArtoriasTests,VergilTests}.cs`,
`Assets/_Project/Tests/EditMode/LeaderUnlockTests.cs`

**Status**: [x] Tests exist — pass/fail not established

---

## Dependencies

- Depends on: `story-002-card-system.md`
- Unlocks: None
