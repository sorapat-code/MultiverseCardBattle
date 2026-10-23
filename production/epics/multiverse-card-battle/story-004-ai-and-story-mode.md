# Story 004: AI opponent and Story Mode

> **Epic**: Multiverse Card Battle
> **Status**: Done — retroactive (implemented before story tracking; ACs not re-verified in the editor)
> **Layer**: Feature
> **Type**: Integration
> **Estimate**: L
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-10-23

## Context

**GDD**: `design/game-brief.md`
**Requirement**: Brief MVP feature 4

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.5.7f1 | **Risk**: HIGH
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

- [ ] The AI plans and plays a full turn using the same `BattleRules` as the player
- [ ] Difficulty has three levels: Easy, Normal, Hard
- [ ] AI action speed has three settings: Slow, Normal, Fast
- [ ] Story Mode has 10 campaign stages, from Training Grounds to Gods of the Multiverse, each with its own enemy
- [ ] Gold rewards: 120 first clear, 40 replay, 30 quick win, 10 loss
- [ ] Story dialogue plays between stages
- [ ] Quick Battle starts a match against the AI from the main menu

---

## Implementation Notes

`AIPlanner` builds a plan per turn; `CampaignLibrary` defines stages; `CampaignService` applies rewards.

---

## Out of Scope

- Story 001: the commands the AI issues
- Story 005: spending the rewarded gold

---

## Files

- `Assets/_Project/Scripts/Battle/AIPlanner.cs`
- `Assets/_Project/Scripts/Data/{AiDifficulty,CampaignLibrary,CampaignService,CampaignStage,StoryLibrary,StoryService}.cs`
- `Assets/_Project/Scripts/Screens/{CampaignScreen,StoryDialogue}.cs`

---

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: existing tests (not re-run for this record):
`Assets/_Project/Tests/EditMode/{StoryTests,StatsDifficultyTests}.cs`

**Status**: [x] Tests exist — pass/fail not established

---

## Dependencies

- Depends on: `story-001-battle-core.md`
- Unlocks: None
