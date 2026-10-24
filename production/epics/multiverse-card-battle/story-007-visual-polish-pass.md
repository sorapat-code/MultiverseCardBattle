# Story 007: Visual polish pass — board, effects, cards, buttons

> **Epic**: Multiverse Card Battle
> **Status**: In Review — code changed, not yet run in the editor (no Unity on the cloud machine); waiting for screenshots
> **Layer**: Presentation
> **Type**: Visual/Feel
> **Estimate**: M
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-10-24

## Context

**GDD**: `design/game-brief.md`
**Requirement**: Art & audio direction (brief); user request 2026-10-24 to make the battle field, effects, cards, HUD and menus look better, with five reference screenshots

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.5.7f1 | **Risk**: HIGH
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

- [ ] Board surface is a darker, detailed grass that stays sharp at the battle camera angle (no stretched blur)
- [ ] The near/far turn glow no longer blooms into a bright cyan/pink band over the front row
- [ ] Damage/heal numbers pop in larger, then rise and fade; a white flash ring marks each damage hit
- [ ] Card name and rules text read clearly over any artwork
- [ ] Card art on small cards (hand, field) shows less aliasing after "Reimport All Card Art"
- [ ] Every code-built button has a deeper bevel, a drop shadow, and grows/glows on hover and dips on press
- [ ] Story Mode is the most prominent main-menu button
- [ ] Menu backdrops show a soft top light beam and faint rune rings instead of a flat gradient
- [ ] The battle HUD's right-hand buttons sit on one dark rail panel

---

## Implementation Notes

- Board: `BoardArt.BuildGrass` is now 1024×576 with mipmaps, trilinear and anisotropic filtering; glow strip colours and active alpha lowered in `BattleStage3D`
- Effects: `FloatingText` pop/ease; `BattleScreen.PlayImpact` adds a `UiFx.Ring` flash
- Cards: `CardView` name/body boxes more opaque; `CardArtImportSettings` enables mipmaps (reimport needed)
- Buttons: `UIShapes` bevel gradient; `UIFactory.CreateButton` adds `Shadow`, hover colours and the new `ButtonHoverFx`
- Menus: `UIShapes.BuildBackdrop` 640×360 with beam and rune rings; `UIFactory.Primary` used for Story Mode

---

## Out of Scope

- New card artwork (e.g. the blocky Trident `EQP_009`) — `card-art` agent
- Enemy portraits on Story Mode rows, shop pack art — need new image assets

---

## Files

- `Assets/_Project/Scripts/Screens/BoardArt.cs`
- `Assets/_Project/Scripts/Screens/BattleStage3D.cs`
- `Assets/_Project/Scripts/Screens/FloatingText.cs`
- `Assets/_Project/Scripts/Screens/BattleScreen.cs`
- `Assets/_Project/Scripts/Screens/UIShapes.cs`
- `Assets/_Project/Scripts/Screens/UIFactory.cs`
- `Assets/_Project/Scripts/Screens/ButtonHoverFx.cs` (new)
- `Assets/_Project/Scripts/Screens/MainMenuScreen.cs`
- `Assets/_Project/Scripts/Cards/CardView.cs`
- `Assets/_Project/Scripts/Editor/CardArtImportSettings.cs`

---

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: Visual/Feel
**Required evidence**: retained screenshots in `production/qa/evidence/` of main menu, Story Mode, shop, a battle (idle and mid-hit) and a card close-up, plus sign-off in `production/qa/evidence/story-007-visual-polish-pass-evidence.md`

**Status**: [ ] Not yet created — needs a run in the Unity editor

---

## Dependencies

- Depends on: None
- Unlocks: None
