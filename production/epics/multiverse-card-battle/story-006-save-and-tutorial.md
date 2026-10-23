# Story 006: Save profile and tutorial

> **Epic**: Multiverse Card Battle
> **Status**: Done — retroactive (implemented before story tracking; ACs not re-verified in the editor)
> **Layer**: Foundation
> **Type**: Integration
> **Estimate**: M
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-10-23

## Context

**GDD**: `design/game-brief.md`
**Requirement**: Brief MVP feature 6

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.5.7f1 | **Risk**: HIGH
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

- [ ] The player profile is saved as JSON in `Application.persistentDataPath` and loaded on start
- [ ] A save from an older version is upgraded to `SaveData.CurrentVersion` on load
- [ ] Audio (master/music/SFX), fullscreen, VSync, frame-rate cap, animation speed, AI speed and text size (Normal / Large / Extra Large) persist in the save
- [ ] A new profile sees the tutorial marked `(!)` in the menu until `TutorialDone` is set
- [ ] The tutorial runs a scripted battle with a coach overlay

---

## Implementation Notes

Read and write the profile through `SaveManager.Profile`, and call `Save()` after any change.

---

## Out of Scope

- Cloud save

---

## Files

- `Assets/_Project/Scripts/Core/{SaveManager,GameSettings}.cs`
- `Assets/_Project/Scripts/Data/PlayerProfile.cs`
- `Assets/_Project/Scripts/Battle/TutorialSetup.cs`
- `Assets/_Project/Scripts/Screens/{TutorialCoach,BattleSettingsPanel}.cs`

---

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: playtest doc or integration test. Existing tests (not re-run): `Assets/_Project/Tests/EditMode/DataTests.cs`

**Status**: [ ] No save round-trip test identified

---

## Dependencies

- Depends on: None
- Unlocks: None
